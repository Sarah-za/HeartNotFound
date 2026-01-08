using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using System;

namespace AdministrationApp.Tests.SystemTests
{
    [TestClass]
    public class MonitorSystemTest
    {
        private string connString =
            "Host=db.inftech.hs-mannheim.de;Username=pms_hnf;Password=pms_hnf;Database=pms_hnf;SslMode=Require;Trust Server Certificate=true;";

        [TestMethod]
        public void AD20_MonitorKannAngelegtWerden()
        {
            // ---------- Arrange ----------
            string testModell = "TEST_MONITOR_" + Guid.NewGuid();

            int newMoid;

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                // ---------- Act ----------
                var insertMonitorCmd = new NpgsqlCommand(@"
                    INSERT INTO monitors (moid, model)
                    VALUES ((SELECT COALESCE(MAX(moid),0)+1 FROM monitors), @model)
                    RETURNING moid;", conn);

                insertMonitorCmd.Parameters.AddWithValue("@model", testModell);
                newMoid = (int)insertMonitorCmd.ExecuteScalar();

                // ---------- Assert ----------
                var selectCmd = new NpgsqlCommand(
                    "SELECT model FROM monitors WHERE moid = @moid", conn);
                selectCmd.Parameters.AddWithValue("@moid", newMoid);

                var result = selectCmd.ExecuteScalar();

                Assert.IsNotNull(result, "Monitor wurde nicht in der DB gespeichert.");
                Assert.AreEqual(testModell, result.ToString(), "Monitor-Modell stimmt nicht überein.");

                // ---------- Cleanup ----------
                var deleteCmd = new NpgsqlCommand(
                    "DELETE FROM monitors WHERE moid = @moid", conn);
                deleteCmd.Parameters.AddWithValue("@moid", newMoid);
                deleteCmd.ExecuteNonQuery();
            }
        }
    }
}

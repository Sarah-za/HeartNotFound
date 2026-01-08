using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using System;

namespace AdministrationApp.Tests.SystemTests
{
    [TestClass]
    public class ZuordnungSystemTest
    {
        private string connString =
            "Host=db.inftech.hs-mannheim.de;Username=pms_hnf;Password=pms_hnf;Database=pms_hnf;SslMode=Require;Trust Server Certificate=true;";

        [TestMethod]
        public void AD30_MonitorKannPatientZugeordnetWerden()
        {
            int pid;
            int moid;

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();

                // ---------- Arrange ----------
                string vorname = "TEST";
                string nachname = "PATIENT_" + Guid.NewGuid();
                int alter = 30;
                string geschlecht = "m";

                var insertPatientCmd = new NpgsqlCommand(@"
                    INSERT INTO patients (pid, vorname, name, alter, geschlecht)
                    VALUES ((SELECT COALESCE(MAX(pid),0)+1 FROM patients),
                            @v, @n, @a, @g)
                    RETURNING pid;", conn);

                insertPatientCmd.Parameters.AddWithValue("@v", vorname);
                insertPatientCmd.Parameters.AddWithValue("@n", nachname);
                insertPatientCmd.Parameters.AddWithValue("@a", alter);
                insertPatientCmd.Parameters.AddWithValue("@g", geschlecht);

                object? pidResult = insertPatientCmd.ExecuteScalar();
                Assert.IsNotNull(pidResult, "Patient konnte nicht angelegt werden.");
                pid = Convert.ToInt32(pidResult);

                string modell = "TEST_MONITOR_" + Guid.NewGuid();

                var insertMonitorCmd = new NpgsqlCommand(@"
                    INSERT INTO monitors (moid, model)
                    VALUES ((SELECT COALESCE(MAX(moid),0)+1 FROM monitors), @m)
                    RETURNING moid;", conn);

                insertMonitorCmd.Parameters.AddWithValue("@m", modell);

                object? moidResult = insertMonitorCmd.ExecuteScalar();
                Assert.IsNotNull(moidResult, "Monitor konnte nicht angelegt werden.");
                moid = Convert.ToInt32(moidResult);

                // ---------- Act ----------
                var assignCmd = new NpgsqlCommand(
                    "INSERT INTO belegung (moid, pid) VALUES (@m, @p)", conn);

                assignCmd.Parameters.AddWithValue("@m", moid);
                assignCmd.Parameters.AddWithValue("@p", pid);
                assignCmd.ExecuteNonQuery();

                // ---------- Assert ----------
                var checkCmd = new NpgsqlCommand(@"
                    SELECT COUNT(*) 
                    FROM belegung 
                    WHERE moid = @m AND pid = @p", conn);

                checkCmd.Parameters.AddWithValue("@m", moid);
                checkCmd.Parameters.AddWithValue("@p", pid);

                long count = (long)checkCmd.ExecuteScalar();

                Assert.AreEqual(1, count, "Monitor wurde nicht korrekt dem Patienten zugeordnet.");

                // ---------- Cleanup ----------
                new NpgsqlCommand("DELETE FROM belegung WHERE pid=@p", conn)
                { Parameters = { new NpgsqlParameter("@p", pid) } }
                    .ExecuteNonQuery();

                new NpgsqlCommand("DELETE FROM patients WHERE pid=@p", conn)
                { Parameters = { new NpgsqlParameter("@p", pid) } }
                    .ExecuteNonQuery();

                new NpgsqlCommand("DELETE FROM monitors WHERE moid=@m", conn)
                { Parameters = { new NpgsqlParameter("@m", moid) } }
                    .ExecuteNonQuery();
            }
        }
    }
}

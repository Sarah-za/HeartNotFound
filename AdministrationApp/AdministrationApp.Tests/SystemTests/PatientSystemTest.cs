using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace AdministrationApp.Tests.SystemTests
{
    [TestClass]
    public class PatientSystemTest
    {
        private string connString =
            "Host=db.inftech.hs-mannheim.de;Username=pms_hnf;Password=pms_hnf;Database=pms_hnf;SslMode=Require;Trust Server Certificate=true;";

        [TestMethod]
        public void AD10_PatientKannAngelegtWerden()
        {
            // Arrange
            int pid;
            using var conn = new NpgsqlConnection(connString);
            conn.Open();

            // Act
            var insert = new NpgsqlCommand(@"
                INSERT INTO patients (pid, vorname, name, alter, geschlecht)
                VALUES ((SELECT COALESCE(MAX(pid),0)+1 FROM patients),
                        'Test', 'Patient', 25, 'm')
                RETURNING pid;", conn);

            pid = (int)insert.ExecuteScalar();

            // Assert
            var check = new NpgsqlCommand(
                "SELECT COUNT(*) FROM patients WHERE pid=@p", conn);
            check.Parameters.AddWithValue("@p", pid);

            int count = (int)(long)check.ExecuteScalar();
            Assert.AreEqual(1, count);

            // Cleanup
            var cleanup = new NpgsqlCommand(
                "DELETE FROM patients WHERE pid=@p", conn);
            cleanup.Parameters.AddWithValue("@p", pid);
            cleanup.ExecuteNonQuery();
        }
    }
}

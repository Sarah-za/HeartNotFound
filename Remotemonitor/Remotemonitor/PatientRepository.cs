using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Remotemonitor
{
    public class PatientRepository
    {
        private readonly string _connString;

        public PatientRepository()
        {
            var server = (string)(System.Windows.Application.Current.Properties["DB_SERVER"] ?? "");
            var db = (string)(System.Windows.Application.Current.Properties["DB_NAME"] ?? "");
            var user = (string)(System.Windows.Application.Current.Properties["DB_USER"] ?? "");
            var pass = (string)(System.Windows.Application.Current.Properties["DB_PASS"] ?? "");

            _connString =
                $"Host={server};" +
                $"Database={db};" +
                $"Username={user};" +
                $"Password={pass};" +
                "SslMode=Require;Trust Server Certificate=true;";
        }

        public (int Pid, string FirstName, string LastName, int Age, string Gender)? GetPatientByMonitorId(int moid)
        {
            using var conn = new NpgsqlConnection(_connString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
            SELECT p.pid, p.vorname, p.name, p.alter, p.geschlecht
            FROM belegung b
            JOIN patients p ON b.pid = p.pid
            WHERE b.moid = @moid
            LIMIT 1;", conn);

            cmd.Parameters.AddWithValue("@moid", moid);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return (
                r.GetInt32(0),  // pid
                r.GetString(1), // vorname
                r.GetString(2), // name
                r.GetInt32(3),  // alter
                r.GetString(4)  // geschlecht
            );
        }

    }
}

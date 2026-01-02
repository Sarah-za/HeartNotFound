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
        private readonly string _connString =
            "Host=db.inftech.hs-mannheim.de;" +
            "Database=pms_hnf;" +
            "Username=pms_hnf;" +
            "Password=pms_hnf;" +
            "SslMode=Require;Trust Server Certificate=true;";

        public (string FirstName, string LastName, int Age, string Gender)? GetPatientByMonitorId(int moid)
        {
            using var conn = new NpgsqlConnection(_connString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
        SELECT p.vorname, p.name, p.alter, p.geschlecht
        FROM belegung b
        JOIN patients p ON b.pid = p.pid
        WHERE b.moid = @moid
        LIMIT 1;", conn);

            cmd.Parameters.AddWithValue("@moid", moid);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return (
                r.GetString(0),
                r.GetString(1),
                r.GetInt32(2),
                r.GetString(3)
            );
        }

    }
}

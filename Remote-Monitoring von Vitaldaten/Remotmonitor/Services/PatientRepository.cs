using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remotmonitor.Models;

using Npgsql;

namespace Remotmonitor.Services
{
    public class PatientRepository
    {
        private readonly string _connString =
            "Host=db.inftech.hs-mannheim.de;" +
            "Database=pms_hnf;" +
            "Username=pms_hnf;" +
            "Password=pms_hnf;" +
            "SslMode=Require;Trust Server Certificate=true;";

        public (string FirstName, string LastName)? GetPatientByMonitorId(string stationId)
        {
            if (!int.TryParse(stationId, out int moid))
                return null;

            using var conn = new NpgsqlConnection(_connString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT p.vorname, p.name
                FROM belegung b
                JOIN patients p ON b.pid = p.pid
                WHERE b.moid = @moid
                LIMIT 1;", conn);

            cmd.Parameters.AddWithValue("@moid", moid);

            using var r = cmd.ExecuteReader();
            if (!r.Read())
                return null;

            return (r.GetString(0), r.GetString(1));
        }
    }
}

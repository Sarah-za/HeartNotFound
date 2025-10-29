namespace Remotmonitor.Models;

public class VitalSample
{
    public string PatientId { get; set; } = "P-0001";
    public string MonitorId { get; set; } = "MON-01";
    public DateTime Ts { get; set; }
    public int Hr { get; set; }
    public int Spo2 { get; set; }
    public int Rr { get; set; }
    public double Temp { get; set; }
}



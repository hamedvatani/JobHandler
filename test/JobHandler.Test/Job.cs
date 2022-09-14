using System;

namespace JobHandler.Test;

public class Job
{
    public string Subject { get; set; } = "";
    public DateTime PublishTime { get; set; }
    public DateTime ConsumeTime { get; set; }
    public DateTime ExecuteTime { get; set; }
    public double Lag => (ConsumeTime - PublishTime).TotalMilliseconds;
}
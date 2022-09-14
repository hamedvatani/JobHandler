namespace JobHandler.Executor;

public class FuncResult
{
    public bool IsSuccess { get; set; }
    public bool IsTimeout { get; set; }
    public string Message { get; set; } = "";
    public DateTime TimeStamp { get; set; } = DateTime.Now;

    public static FuncResult Success()
    {
        return new FuncResult {IsSuccess = true};
    }

    public static FuncResult Timeout()
    {
        return new FuncResult {IsTimeout = true, Message = "Timeout"};
    }

    public static FuncResult Fail(string message)
    {
        return new FuncResult {IsSuccess = false, Message = message};
    }
}
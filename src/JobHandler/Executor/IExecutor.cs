namespace JobHandler.Executor;

public interface IExecutor<T>
{
    void StartExecution(Func<T, CancellationToken, FuncResult> executor, Action<T, List<FuncResult>>? failAction);
    void StopExecution();
}
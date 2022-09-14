using System;
using System.Collections.Generic;
using System.Threading;

namespace JobHandler.Executor
{
    public interface IExecutor<T>
    {
        void StartExecution(Func<T, CancellationToken, FuncResult> executor, Action<T, List<FuncResult>> failAction);
        void StopExecution();
    }
}
using System;
using System.Threading.Tasks;

namespace Assurance
{
    public static class Executor
    {
        public static async Task<(T aResult, T bResult, bool areSame)> CompareAsync<T>(Func<Task<T>> a, Func<Task<T>> b)
        {
            var taskA = a();
            var taskB = b();
            Task.WaitAll(taskA, taskB);

            return (taskA.Result, taskB.Result, taskA.Result.Equals(taskB.Result));
        }
    }
}

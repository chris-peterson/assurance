using System;
using System.Threading.Tasks;

namespace Assurance.UnitTests
{
    public class AsyncRunnerTestContext
    {
        public Func<Task<string>> Existing { get; set; }
        public Func<Task<string>> Replacement { get; set; }
        public RunResult<string> Result { get; set; }
    }
}

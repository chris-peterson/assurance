using System;

namespace Assurance.UnitTests
{
    public class RunnerTestContext
    {
        public Func<string> Existing { get; set; }
        public Func<string> Replacement { get; set; }
        public RunResult<string> Result { get; set; }
    }
}

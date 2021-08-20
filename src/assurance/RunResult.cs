using Spiffy.Monitoring;

namespace Assurance
{
    public class RunResult<T>
    {
        readonly EventContext _eventContext;
        public RunResult(T existing, T replacement, EventContext eventContext)
        {
            Existing = existing;
            Replacement = replacement;
            _eventContext = eventContext;
        }

        public T Existing { get; }
        public T Replacement { get; }
        public bool SameResult
        {
            get
            {
                if (Existing == null)
                    return Replacement == null;
                return Existing.Equals(Replacement);
            }
        }

        public T UseExisting()
        {
            _eventContext["Use"] = "existing";
            return Existing;
        }
        public T UseReplacement()
        {
            _eventContext["Use"] = "replacement";
            return Replacement;
        }
    }
}

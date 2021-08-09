namespace Assurance
{
    public class RunResult<T>
    {
        public RunResult(T existing, T replacement)
        {
            Existing = existing;
            Replacement = replacement;
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

        public T UseExisting() => Existing;
        public T UseReplacement() => Replacement;
    }
}

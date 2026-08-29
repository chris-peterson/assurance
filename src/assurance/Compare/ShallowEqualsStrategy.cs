namespace Assurance.Compare;

public class ShallowEqualsStrategy<T> : IComparisonStrategy<T>
{
    public ResultComparison Compare(T existing, T replacement)
    {
        bool areEqual = Equals(existing, replacement);
        return areEqual
            ? new ResultComparison(true)
            : new ResultComparison(
                false,
                $"Values differ: existing=<{LogSafeText.Escape(existing)}>, replacement=<{LogSafeText.Escape(replacement)}>");
    }
}

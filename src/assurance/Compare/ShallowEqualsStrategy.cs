namespace Assurance.Compare;

public class ShallowEqualsStrategy<T> : IComparisonStrategy<T>
{
    public ResultComparison Compare(T existing, T replacement)
    {
        return Equals(existing, replacement)
            ? new ResultComparison(true)
            : new ResultComparison(
                false,
                $"Values differ: existing=<{LogSafeText.Render(existing)}>, replacement=<{LogSafeText.Render(replacement)}>");
    }
}

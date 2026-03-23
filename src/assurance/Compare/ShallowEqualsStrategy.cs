namespace Assurance.Compare;

public class ShallowEqualsStrategy<T> : IComparisonStrategy<T>
{
    public ResultComparison Compare(T existing, T replacement)
    {
        bool areEqual = Equals(existing, replacement);
        string differences = areEqual ? "" : $"Values differ: existing=<{existing}>, replacement=<{replacement}>";
        return new ResultComparison(areEqual, differences);
    }
}

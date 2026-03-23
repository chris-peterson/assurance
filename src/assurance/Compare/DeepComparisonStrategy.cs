using KellermanSoftware.CompareNetObjects;

namespace Assurance.Compare;

public class DeepComparisonStrategy<T> : IComparisonStrategy<T>
{
    public ResultComparison Compare(T existing, T replacement)
    {
        var compareLogic = new CompareLogic { Config = { MaxDifferences = int.MaxValue } };
        var result = compareLogic.Compare(existing, replacement);
        return new ResultComparison(result.AreEqual, result.DifferencesString);
    }
}

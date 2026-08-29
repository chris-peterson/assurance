using System.Linq;
using KellermanSoftware.CompareNetObjects;

namespace Assurance.Compare;

public class DeepComparisonStrategy<T> : IComparisonStrategy<T>
{
    public ResultComparison Compare(T existing, T replacement)
    {
        var compareLogic = new CompareLogic { Config = { MaxDifferences = int.MaxValue } };
        var result = compareLogic.Compare(existing, replacement);

        return new ResultComparison(
            result.AreEqual,
            result.Differences.Select(Describe).ToArray());
    }

    // Difference.ToString restates the types and the Expected/Actual side names on every entry.
    // Where the types genuinely differ the library reports them as the values, so naming the
    // property and the two values loses nothing.
    static string Describe(Difference difference)
    {
        var values =
            $"{LogSafeText.Escape(difference.Object1Value)} != {LogSafeText.Escape(difference.Object2Value)}";

        // comparing two scalars directly, rather than a property of something, leaves no name
        return string.IsNullOrEmpty(difference.PropertyName)
            ? values
            : $"{difference.PropertyName}: {values}";
    }
}

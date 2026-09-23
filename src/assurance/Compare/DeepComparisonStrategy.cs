using System;
using System.Linq;
using KellermanSoftware.CompareNetObjects;

namespace Assurance.Compare;

public class DeepComparisonStrategy<T> : IComparisonStrategy<T>
{
    /// <summary>
    /// How many differences a single comparison reports before it stops looking. Two wide object
    /// graphs can differ in thousands of places, and an event carrying all of them is written and
    /// stored whether or not anyone reads past the first few.
    /// </summary>
    public const int DefaultMaxDifferences = 100;

    readonly int _maxDifferences;

    /// <param name="maxDifferences">
    /// How many differences to report before giving up on the comparison. A comparison that may
    /// report none cannot tell equal from different, so this has a floor of one.
    /// </param>
    public DeepComparisonStrategy(int maxDifferences = DefaultMaxDifferences)
    {
        if (maxDifferences < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDifferences), maxDifferences, "A comparison has to be allowed at least one difference.");
        }
        _maxDifferences = maxDifferences;
    }

    public ResultComparison Compare(T existing, T replacement)
    {
        var compareLogic = new CompareLogic { Config = { MaxDifferences = _maxDifferences } };
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
        string values =
            $"{LogSafeText.Render(difference.Object1Value)} != {LogSafeText.Render(difference.Object2Value)}";

        // comparing two scalars directly, rather than a property of something, leaves no name
        return string.IsNullOrEmpty(difference.PropertyName)
            ? values
            : $"{difference.PropertyName}: {values}";
    }
}

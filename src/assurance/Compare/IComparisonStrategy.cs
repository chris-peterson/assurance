namespace Assurance.Compare;

public interface IComparisonStrategy<in T>
{
    ResultComparison Compare(T existing, T replacement);
}

public class ResultComparison
{
    public bool AreEqual { get; }
    public DifferenceCollection Differences { get; }

    public ResultComparison(bool areEqual, params string[] differences)
    {
        AreEqual = areEqual;
        Differences = new DifferenceCollection(differences);
    }
}

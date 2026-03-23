namespace Assurance.Compare;

public interface IComparisonStrategy<in T>
{
    ResultComparison Compare(T existing, T replacement);
}

public class ResultComparison
{
    public bool AreEqual { get; }
    public string Differences { get; }

    public ResultComparison(bool areEqual, string differences = "")
    {
        AreEqual = areEqual;
        Differences = differences;
    }
}

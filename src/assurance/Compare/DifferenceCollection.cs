using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assurance.Compare;

/// <summary>
/// The differences a comparison found, one entry each, so a log can report a count or the first
/// entry alone. Rendering the collection itself gives every entry on one line, which is what a
/// log writes when it is handed the collection as a field value.
/// </summary>
/// <remarks>
/// Entries are escaped as they are stored, so a compared value carrying a line break cannot split
/// the event it is written into. Entries with no text are dropped, so a strategy that reports
/// equality with an empty placeholder still yields an empty collection.
/// </remarks>
public class DifferenceCollection : IReadOnlyList<string>
{
    readonly IReadOnlyList<string> _differences;

    public DifferenceCollection(params string[] differences)
    {
        _differences = differences?
            .Where(difference => !string.IsNullOrEmpty(difference))
            .Select(LogSafeText.Escape)
            .ToArray() ?? Array.Empty<string>();
    }

    public string this[int index] => _differences[index];

    public int Count => _differences.Count;

    public IEnumerator<string> GetEnumerator() => _differences.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // an event is read one line at a time, so every entry goes on one; a compared value can
    // contain a comma, so that cannot mark the boundary between entries
    public override string ToString() => string.Join("; ", _differences);
}

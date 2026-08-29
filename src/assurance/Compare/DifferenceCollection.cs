using System;
using System.Collections;
using System.Collections.Generic;

namespace Assurance.Compare;

/// <summary>
/// The differences a comparison found, one entry each, so a log can report a count or the first
/// entry alone. Rendering the collection itself gives every entry on one line, which is what a
/// log writes when it is handed the collection as a field value.
/// </summary>
public class DifferenceCollection : IReadOnlyList<string>
{
    readonly IReadOnlyList<string> _differences;

    public DifferenceCollection(params string[] differences)
    {
        _differences = differences ?? Array.Empty<string>();
    }

    public string this[int index] => _differences[index];

    public int Count => _differences.Count;

    public IEnumerator<string> GetEnumerator() => _differences.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // an event is read one line at a time, so every entry goes on one; a compared value can
    // contain a comma, so that cannot mark the boundary between entries
    public override string ToString() => string.Join("; ", _differences);
}

namespace Assurance.Compare;

static class LogSafeText
{
    /// <summary>
    /// An event is read one line at a time, so a compared value carrying a line break would split
    /// it in two. Escaping rather than stripping keeps the break visible and reversible; the
    /// backslash goes first so a value already containing "\n" stays distinguishable from one that
    /// contained a newline.
    /// </summary>
    public static string Escape(object value)
    {
        var text = value?.ToString();
        return text?
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}

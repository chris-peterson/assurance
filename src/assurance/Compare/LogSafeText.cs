namespace Assurance.Compare;

/// <summary>
/// Makes text safe to write as a single log field value. <see cref="DifferenceCollection"/> runs
/// every difference entry through <see cref="Escape"/> already; reach for it directly when a
/// custom <see cref="Assurance.Logging.ILogRun{T}"/> writes a compared value into a field of its
/// own, which nothing escapes for you.
/// </summary>
public static class LogSafeText
{
    const string NullMarker = "(null)";

    /// <summary>
    /// Escapes the line breaks that would otherwise split one event into two, since an event is
    /// read a line at a time. The backslash goes first so a value already containing "\n" stays
    /// distinguishable from one that contained a newline.
    /// </summary>
    /// <remarks>
    /// Quotes are left to Spiffy, which encapsulates a value by picking a quote the value does not
    /// contain. Escaping the quote here neither stops it running out of candidates nor survives a
    /// formatter that escapes quotes itself.
    /// </remarks>
    public static string Escape(string text)
    {
        return text?
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    /// <summary>
    /// Renders a compared value for a difference entry. A null reads as "(null)" so it stays
    /// distinguishable from an empty string.
    /// </summary>
    public static string Render(object value)
    {
        return value == null ? NullMarker : value.ToString();
    }
}

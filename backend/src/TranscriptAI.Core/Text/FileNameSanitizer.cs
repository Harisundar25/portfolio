using System.Text;
using System.Text.RegularExpressions;

namespace TranscriptAI.Core.Text;

/// <summary>
/// C# port of <c>sanitize_filename</c> from the original Python <c>run.py</c>:
/// <code>
/// name = unicodedata.normalize('NFKD', name)
/// return re.sub(r'[\\/*?:"&lt;&gt;|]', "", name)
/// </code>
/// It additionally strips control characters, trailing dots/spaces and reserved
/// Windows device names, which the Python version did not handle.
/// </summary>
public static partial class FileNameSanitizer
{
    private const int MaxLength = 150;

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    [GeneratedRegex(@"[\\/*?:""<>|]")]
    private static partial Regex InvalidWindowsCharacters();

    public static string Sanitize(string name, string fallback = "transcript")
    {
        ArgumentNullException.ThrowIfNull(name);

        // Normalize Unicode characters (e.g. full-width colons become ':' and are then removed).
        var normalized = name.Normalize(NormalizationForm.FormKD);

        // Remove invalid Windows filename characters — identical to the Python regex.
        var cleaned = InvalidWindowsCharacters().Replace(normalized, string.Empty);

        // Hardening beyond the Python version.
        var builder = new StringBuilder(cleaned.Length);
        foreach (var c in cleaned)
        {
            if (!char.IsControl(c))
            {
                builder.Append(c);
            }
        }

        var result = builder.ToString().Trim().TrimEnd('.', ' ');
        if (result.Length > MaxLength)
        {
            result = result[..MaxLength].TrimEnd('.', ' ');
        }

        if (result.Length == 0)
        {
            return fallback;
        }

        var stem = result.Split('.')[0];
        return ReservedNames.Contains(stem) ? $"_{result}" : result;
    }
}

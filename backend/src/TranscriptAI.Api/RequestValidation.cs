using System.Text.RegularExpressions;
using TranscriptAI.Core.Models;

namespace TranscriptAI.Api;

internal static partial class RequestValidation
{
    [GeneratedRegex("^[A-Za-z]{2,3}$")]
    private static partial Regex LanguageCode();

    /// <summary>Accepts "transcribe" (default) or "translate".</summary>
    public static bool TryParseTask(string? value, out TranscriptionTask task)
    {
        task = TranscriptionTask.Transcribe;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "transcribe":
                return true;
            case "translate":
                task = TranscriptionTask.Translate;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Empty / "auto" means auto-detect; otherwise an ISO-639-1 (or 3-letter) code.</summary>
    public static bool TryNormalizeLanguage(string? value, out string? language)
    {
        language = null;
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var trimmed = value.Trim();
        if (!LanguageCode().IsMatch(trimmed))
        {
            return false;
        }

        language = trimmed.ToLowerInvariant();
        return true;
    }

    public static IResult? Validate(string? task, string? language, out TranscriptionRequest request)
    {
        request = new TranscriptionRequest();
        var errors = new Dictionary<string, string[]>();

        if (!TryParseTask(task, out var parsedTask))
        {
            errors["task"] = ["Use 'transcribe' or 'translate'."];
        }

        if (!TryNormalizeLanguage(language, out var parsedLanguage))
        {
            errors["language"] = ["Use an ISO-639-1 language code such as 'en' or 'ta', or leave it empty to auto-detect."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        request = new TranscriptionRequest { Task = parsedTask, Language = parsedLanguage };
        return null;
    }
}

using TranscriptAI.Core.Models;

namespace TranscriptAI.Core.Transcription;

/// <summary>
/// A hosted open-source speech model. Replaces the Python script's local
/// <c>whisper.load_model("base")</c> + <c>model.transcribe(...)</c>.
/// </summary>
public interface ITranscriptionProvider
{
    string Name { get; }

    bool SupportsTranslation { get; }

    /// <summary>True when the API key/token for this provider is configured.</summary>
    bool IsConfigured { get; }

    string DefaultModel(TranscriptionTask task);

    /// <summary>Transcribes one audio file that is already within the upload limit.</summary>
    Task<ProviderTranscription> TranscribeAsync(
        string audioPath,
        TranscriptionRequest request,
        CancellationToken cancellationToken = default);
}

# TranscriptAI — C# / .NET 10

A C# port of my Python [Transcript-AI](https://github.com/HariSundar-GRL/Transcript-AI) script, built for personal learning.
It turns a **YouTube link or a local audio/video file** into a **timestamped transcript**, using open-source Whisper
models served by [Groq](https://console.groq.com)'s free API instead of a model running on the laptop.

| Project | What it is |
| --- | --- |
| `src/TranscriptAI.Core` | The pipeline: media source → audio preparation (FFmpeg, optional) → transcription provider → formatting. |
| `src/TranscriptAI.Cli` | `transcript-ai` command-line tool — the direct replacement for `run.py`. |
| `src/TranscriptAI.Api` | ASP.NET Core minimal API behind the portfolio's live demo (rate limited, CORS, problem details, OpenAPI). |
| `tests/TranscriptAI.Tests` | xUnit v3 unit and integration tests (151 tests). |

## From Python to C#

| `run.py` | C# |
| --- | --- |
| `yt_dlp` downloads `bestaudio` and converts it to 192 kbps MP3 | [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) downloads the smallest audio-only stream that is good enough for speech — no Python or yt-dlp needed |
| `whisper.load_model("base")` runs locally | `whisper-large-v3-turbo` on Groq (`whisper-large-v3` for translation); Hugging Face Inference as an alternative provider |
| FFmpeg path hard-coded to `E:\ffmpeg-8.0.1-full_build\bin` | Optional: found via `--ffmpeg`, `FFMPEG_PATH` or `PATH`; used to downsample to 16 kHz mono and split long recordings under the 25 MB API limit |
| `sanitize_filename` (NFKD + strip `\/*?:"<>|`) | `FileNameSanitizer` — same rules, plus control characters and reserved Windows names |
| `[start --> end] text` lines in `Transcripts/<title>.txt` | Identical output, plus optional SRT, WebVTT and JSON |

## Run it

Prerequisites: the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and a free Groq API key from
<https://console.groq.com/keys>. FFmpeg is optional.

**CLI** (PowerShell):

```powershell
$env:GROQ_API_KEY = "gsk_..."
dotnet run --project src/TranscriptAI.Cli -- "https://www.youtube.com/watch?v=jNQXAC9IVRw"
dotnet run --project src/TranscriptAI.Cli -- lecture.mp4 --language ta --format txt,srt
dotnet run --project src/TranscriptAI.Cli -- --help
```

Transcripts are written to `Transcripts/` and downloaded audio to `AudioFiles/`, like the Python script.

**API** (the portfolio's Vite dev server proxies `/api` to it):

```powershell
dotnet user-secrets set "Transcription:Groq:ApiKey" "gsk_..." --project src/TranscriptAI.Api
dotnet run --project src/TranscriptAI.Api        # http://localhost:5080
```

| Endpoint | Purpose |
| --- | --- |
| `GET /api/health` | Status and capabilities (provider, limits, accepted formats) — the demo UI adapts to it |
| `POST /api/transcriptions/youtube` | JSON `{ "url": "...", "task": "transcribe" \| "translate", "language": "ta" }` |
| `POST /api/transcriptions/file` | `multipart/form-data` with `file`, optional `task` and `language` |
| `GET /openapi/v1.json` | OpenAPI document |

Errors are [RFC 9457 problem details](https://www.rfc-editor.org/rfc/rfc9457); rate-limit responses include `Retry-After`.

**Tests:**

```powershell
dotnet test --solution TranscriptAI.slnx
```

## Configuration

Settings bind from `appsettings.json`, environment variables (`__` as the separator) or user secrets.

| Setting | Default | Notes |
| --- | --- | --- |
| `Transcription:Groq:ApiKey` | — | Required. `GROQ_API_KEY` also works. Never sent to the browser. |
| `Transcription:Provider` | `Groq` | `HuggingFace` uses `Transcription:HuggingFace:ApiKey` (or `HF_TOKEN`). |
| `Transcription:FFmpeg:Path` | `PATH` lookup | FFmpeg executable or its folder. |
| `Cors:AllowedOrigins` | dev: `localhost:5173` | The portfolio's URL in production, e.g. `Cors__AllowedOrigins__0`. |
| `Demo:MaxUploadMegabytes` | `10` | Largest upload accepted by the API. |
| `Demo:MaxDurationMinutes` | `15` | Longest YouTube video accepted. |
| `Demo:PermitLimitPerWindow` / `Demo:WindowMinutes` | `5` / `10` | Requests per visitor (IP) per window. |
| `Demo:MaxConcurrentJobs` | `2` | Jobs running at once (the same number can wait in a queue). |
| `Demo:DailyLimit` | `40` | Transcriptions started per UTC day across all visitors. |
| `Demo:AllowYouTube` | `true` | Turn off if YouTube blocks the server's IP range. |

The demo defaults suit Azure App Service's Free tier, whose 165 MB/day outbound quota also covers the audio forwarded to
Groq. Hosting steps: [docs/DEPLOYMENT.md](../docs/DEPLOYMENT.md).

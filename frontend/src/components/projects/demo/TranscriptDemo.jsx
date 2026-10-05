import { useCallback, useEffect, useRef, useState } from "react";
import { AlertTriangle, FileAudio, Loader2, RotateCcw, Sparkles, Upload, X } from "lucide-react";
import { SiYoutube } from "react-icons/si";
import Modal from "../../ui/Modal";
import TranscriptResult from "./TranscriptResult";
import { getHealth, transcribeFile, transcribeYouTube } from "../../../lib/transcriptApi";
import { formatWait } from "./format";

// "Me at the zoo" — the first video ever uploaded to YouTube (19 seconds).
const SAMPLE_VIDEO = "https://www.youtube.com/watch?v=jNQXAC9IVRw";

const YOUTUBE_LINK = /^(?:https?:\/\/)?(?:(?:www|m|music)\.)?(?:youtube\.com|youtu\.be|youtube-nocookie\.com)\/\S+$/i;

const LANGUAGES = [
  ["", "Auto-detect"],
  ["en", "English"],
  ["ta", "Tamil"],
  ["hi", "Hindi"],
  ["kn", "Kannada"],
  ["te", "Telugu"],
  ["ml", "Malayalam"],
  ["ja", "Japanese"],
  ["es", "Spanish"],
  ["fr", "French"],
  ["de", "German"],
];

const inputClass =
  "w-full rounded-lg border border-border-default bg-background-surface px-4 py-3 text-text-heading transition-all placeholder:text-text-disabled focus:border-accent-primary focus:shadow-[0_0_15px_rgba(0,212,255,0.2)] focus:ring-1 focus:ring-accent-primary focus:outline-none disabled:cursor-not-allowed disabled:opacity-50";

const segmentClass = (active) =>
  `flex flex-1 items-center justify-center gap-2 rounded-full px-4 py-2 text-sm font-semibold transition-colors ${
    active ? "bg-accent-primary/15 text-accent-primary" : "text-text-muted hover:text-text-heading"
  }`;

/** Checks the API once per attempt; reports "waking" while a sleeping free-tier app starts up. */
function useDemoHealth() {
  const [health, setHealth] = useState({ status: "checking", data: null });
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let disposed = false;
    const controller = new AbortController();
    const slow = setTimeout(() => setHealth((h) => (h.status === "checking" ? { ...h, status: "waking" } : h)), 2500);
    const giveUp = setTimeout(() => controller.abort(), 90_000);

    getHealth({ signal: controller.signal })
      .then((data) => setHealth({ status: data.providerConfigured ? "online" : "unconfigured", data }))
      .catch(() => !disposed && setHealth({ status: "offline", data: null }))
      .finally(() => {
        clearTimeout(slow);
        clearTimeout(giveUp);
      });

    return () => {
      disposed = true;
      controller.abort();
      clearTimeout(slow);
      clearTimeout(giveUp);
    };
  }, [attempt]);

  const retry = useCallback(() => {
    setHealth({ status: "checking", data: null });
    setAttempt((n) => n + 1);
  }, []);

  return { health, retry };
}

/** One transcription at a time; leaving the dialog cancels whatever is in flight. */
function useTranscriptionJob() {
  const [job, setJob] = useState({ phase: "idle" });
  const controllerRef = useRef(null);

  useEffect(() => () => controllerRef.current?.abort(), []);

  const start = useCallback(async (begin) => {
    const controller = new AbortController();
    controllerRef.current = controller;
    try {
      const result = await begin(controller.signal, Date.now());
      setJob({ phase: "done", result });
    } catch (error) {
      setJob(error.name === "AbortError" ? { phase: "idle" } : { phase: "error", error });
    } finally {
      if (controllerRef.current === controller) controllerRef.current = null;
    }
  }, []);

  const cancel = useCallback(() => controllerRef.current?.abort(), []);
  const reset = useCallback(() => setJob({ phase: "idle" }), []);

  return { job, setJob, start, cancel, reset };
}

function StatusBanner({ health, onRetry }) {
  const { status, data } = health;

  if (status === "online") {
    return (
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-status-success/25 bg-status-success/5 px-4 py-3 text-sm">
        <span className="flex items-center gap-2.5 text-text-heading">
          <span className="relative flex h-2 w-2">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-status-success opacity-75" />
            <span className="relative inline-flex h-2 w-2 rounded-full bg-status-success" />
          </span>
          Demo server online
          <span className="hidden font-mono text-xs text-text-muted sm:inline">
            {data.provider} · {data.model}
          </span>
        </span>
        <span className="font-mono text-xs text-text-muted">
          Files ≤ {data.maxUploadMegabytes} MB{data.youTubeEnabled && ` · videos ≤ ${data.maxDurationMinutes} min`}
        </span>
      </div>
    );
  }

  if (status === "checking" || status === "waking") {
    return (
      <div role="status" className="flex items-start gap-3 rounded-xl border border-border-soft bg-background-surface/60 px-4 py-3 text-sm text-text-body">
        <Loader2 size={18} className="mt-0.5 flex-shrink-0 animate-spin text-accent-primary" />
        {status === "checking"
          ? "Connecting to the demo server…"
          : "Waking up the demo server — it runs on a free tier that sleeps when idle, so the first request can take about 20 seconds."}
      </div>
    );
  }

  return (
    <div role="alert" className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-status-warning/30 bg-status-warning/5 px-4 py-3 text-sm text-text-body">
      <span className="flex items-start gap-3">
        <AlertTriangle size={18} className="mt-0.5 flex-shrink-0 text-status-warning" />
        {status === "unconfigured"
          ? "The demo server is up, but its AI provider isn't configured right now."
          : "The demo server is offline right now. The source code is on GitHub if you'd like to run it locally."}
      </span>
      <button
        type="button"
        onClick={onRetry}
        className="flex items-center gap-1.5 rounded-full border border-border-default px-3 py-1.5 text-xs font-semibold text-text-heading transition-colors hover:border-accent-primary hover:text-accent-primary"
      >
        <RotateCcw size={13} /> Retry
      </button>
    </div>
  );
}

function FileDrop({ file, onFile, accept, maxMegabytes, disabled }) {
  const [dragging, setDragging] = useState(false);
  const formats = accept.map((ext) => ext.replace(".", "").toUpperCase()).join(", ");

  return (
    <label
      onDragOver={(e) => {
        e.preventDefault();
        setDragging(true);
      }}
      onDragLeave={() => setDragging(false)}
      onDrop={(e) => {
        e.preventDefault();
        setDragging(false);
        if (e.dataTransfer.files?.[0]) onFile(e.dataTransfer.files[0]);
      }}
      className={`flex cursor-pointer flex-col items-center justify-center gap-2 rounded-xl border border-dashed px-6 py-8 text-center transition-colors ${
        dragging ? "border-accent-primary bg-accent-primary/5" : "border-border-default bg-background-surface/50 hover:border-accent-primary/60"
      }`}
    >
      <input
        type="file"
        className="sr-only"
        accept={accept.join(",")}
        disabled={disabled}
        onChange={(e) => onFile(e.target.files?.[0] ?? null)}
      />
      {file ? (
        <>
          <FileAudio size={28} className="text-accent-primary" />
          <span className="font-medium break-all text-text-heading">{file.name}</span>
          <span className="font-mono text-xs text-text-muted">{(file.size / 1_000_000).toFixed(1)} MB · click to change</span>
        </>
      ) : (
        <>
          <Upload size={28} className="text-text-muted" />
          <span className="font-medium text-text-heading">Drop an audio or video file here, or browse</span>
          <span className="font-mono text-xs text-text-muted">
            {formats} · up to {maxMegabytes} MB
          </span>
        </>
      )}
    </label>
  );
}

function Equalizer() {
  return (
    <span aria-hidden="true" className="flex h-8 items-end gap-1">
      {[0, 0.15, 0.3, 0.45, 0.6, 0.75, 0.9].map((delay) => (
        <span
          key={delay}
          style={{ animationDelay: `${delay}s` }}
          className="h-full w-1.5 origin-bottom animate-eq rounded-full bg-gradient-to-t from-accent-primary to-accent-secondary"
        />
      ))}
    </span>
  );
}

function Elapsed({ since }) {
  const [seconds, setSeconds] = useState(0);

  useEffect(() => {
    const id = setInterval(() => setSeconds(Math.round((Date.now() - since) / 1000)), 1000);
    return () => clearInterval(id);
  }, [since]);

  return <span className="font-mono text-sm text-text-muted tabular-nums">{seconds}s</span>;
}

function Progress({ job, onCancel }) {
  const uploading = job.phase === "uploading";
  const percent = Math.round((job.progress ?? 0) * 100);

  return (
    <div className="rounded-2xl border border-border-soft bg-background-surface/60 p-6">
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <Equalizer />
          <div aria-live="polite">
            <p className="font-semibold text-text-heading">
              {uploading
                ? `Uploading… ${percent}%`
                : job.source === "youtube"
                  ? "Fetching the audio and transcribing with Whisper…"
                  : "Transcribing with Whisper…"}
            </p>
            <p className="mt-0.5 text-xs text-text-muted">C# API → Groq → open-source Whisper model</p>
          </div>
        </div>
        <Elapsed since={job.startedAt} />
      </div>

      {uploading && (
        <div className="mt-5 h-1.5 overflow-hidden rounded-full bg-background-primary">
          <div className="h-full rounded-full bg-accent-primary transition-[width] duration-300" style={{ width: `${percent}%` }} />
        </div>
      )}

      <button
        type="button"
        onClick={onCancel}
        className="mt-6 flex items-center gap-2 rounded-lg border border-border-default px-4 py-2 text-sm font-semibold text-text-heading transition-colors hover:border-status-danger hover:text-status-danger"
      >
        <X size={16} /> Cancel
      </button>
    </div>
  );
}

function ErrorNotice({ error }) {
  const offline = error.status === 0;
  return (
    <div role="alert" className="flex items-start gap-3 rounded-xl border border-status-danger/30 bg-status-danger/5 px-4 py-3 text-sm text-text-body">
      <AlertTriangle size={18} className="mt-0.5 flex-shrink-0 text-status-danger" />
      <div>
        <p>{error.message}</p>
        {offline && <p className="mt-1 text-xs text-text-muted">It may be waking up — please try again in a moment.</p>}
        {error.retryAfter > 0 && !/try again/i.test(error.message) && (
          <p className="mt-1 text-xs text-text-muted">You can try again in about {formatWait(error.retryAfter)}.</p>
        )}
      </div>
    </div>
  );
}

function DemoBody() {
  const { health, retry } = useDemoHealth();
  const { job, setJob, start, cancel, reset } = useTranscriptionJob();
  const [source, setSource] = useState("youtube");
  const [task, setTask] = useState("transcribe");
  const [language, setLanguage] = useState("");
  const [url, setUrl] = useState("");
  const [file, setFile] = useState(null);
  const [fieldError, setFieldError] = useState(null);

  const caps = health.data;
  const online = health.status === "online";
  const activeSource = caps && !caps.youTubeEnabled ? "upload" : source;
  const busy = job.phase === "uploading" || job.phase === "transcribing";

  const chooseFile = (picked) => {
    setFieldError(null);
    setFile(picked);
    if (!picked || !caps) return;
    const extension = picked.name.slice(picked.name.lastIndexOf(".")).toLowerCase();
    if (!caps.acceptedExtensions.includes(extension)) {
      setFieldError(`That format isn't supported here. Use one of: ${caps.acceptedExtensions.join(", ")}.`);
    } else if (picked.size > caps.maxUploadMegabytes * 1_000_000) {
      setFieldError(`That file is ${(picked.size / 1_000_000).toFixed(1)} MB; the demo accepts up to ${caps.maxUploadMegabytes} MB.`);
    }
  };

  const submit = (event) => {
    event.preventDefault();
    if (busy || !online) return;
    const options = { task, language: task === "translate" ? "" : language };

    if (activeSource === "youtube") {
      const link = url.trim();
      if (!YOUTUBE_LINK.test(link)) {
        setFieldError("Paste a YouTube video link, e.g. https://www.youtube.com/watch?v=…");
        return;
      }
      setFieldError(null);
      const absolute = /^https?:\/\//i.test(link) ? link : `https://${link}`;
      start((signal, startedAt) => {
        setJob({ phase: "transcribing", source: "youtube", startedAt });
        return transcribeYouTube({ url: absolute, ...options }, { signal });
      });
      return;
    }

    if (!file) {
      setFieldError("Choose an audio or video file first.");
      return;
    }
    if (fieldError) return;
    start((signal, startedAt) => {
      setJob({ phase: "uploading", source: "upload", startedAt, progress: 0 });
      return transcribeFile(
        { file, ...options },
        {
          signal,
          onUploadProgress: (progress) =>
            setJob((current) =>
              current.phase !== "uploading"
                ? current
                : progress >= 1
                  ? { ...current, phase: "transcribing", progress: 1 }
                  : { ...current, progress },
            ),
        },
      );
    });
  };

  return (
    <div className="space-y-6">
      <StatusBanner health={health} onRetry={retry} />

      {job.phase === "done" ? (
        <TranscriptResult result={job.result} onReset={reset} />
      ) : busy ? (
        <Progress job={job} onCancel={cancel} />
      ) : (
        <form onSubmit={submit} noValidate className={`space-y-6 ${online ? "" : "pointer-events-none opacity-50"}`}>
          {(!caps || caps.youTubeEnabled) && (
            <div role="tablist" aria-label="Media source" className="flex gap-1 rounded-full border border-border-soft bg-background-surface/60 p-1">
              <button
                type="button"
                role="tab"
                aria-selected={activeSource === "youtube"}
                onClick={() => {
                  setSource("youtube");
                  setFieldError(null);
                }}
                className={segmentClass(activeSource === "youtube")}
              >
                <SiYoutube size={16} /> YouTube link
              </button>
              <button
                type="button"
                role="tab"
                aria-selected={activeSource === "upload"}
                onClick={() => {
                  setSource("upload");
                  setFieldError(null);
                }}
                className={segmentClass(activeSource === "upload")}
              >
                <Upload size={16} /> Upload a file
              </button>
            </div>
          )}

          {activeSource === "youtube" ? (
            <div>
              <label htmlFor="demo-url" className="mb-2 block text-sm font-medium text-text-body">
                YouTube video link
              </label>
              <input
                id="demo-url"
                type="url"
                inputMode="url"
                autoComplete="off"
                spellCheck={false}
                value={url}
                onChange={(e) => {
                  setUrl(e.target.value);
                  setFieldError(null);
                }}
                placeholder="https://www.youtube.com/watch?v=…"
                className={inputClass}
              />
              <button
                type="button"
                onClick={() => {
                  setUrl(SAMPLE_VIDEO);
                  setFieldError(null);
                }}
                className="mt-2 text-xs font-medium text-accent-primary hover:underline"
              >
                Try a 19-second sample: “Me at the zoo”
              </button>
            </div>
          ) : (
            <FileDrop
              file={file}
              onFile={chooseFile}
              accept={caps?.acceptedExtensions ?? [".mp3", ".m4a", ".wav"]}
              maxMegabytes={caps?.maxUploadMegabytes ?? 10}
            />
          )}

          <div className="grid gap-4 sm:grid-cols-2">
            <fieldset>
              <legend className="mb-2 block text-sm font-medium text-text-body">Output</legend>
              <div className="flex gap-1 rounded-full border border-border-soft bg-background-surface/60 p-1">
                <button type="button" aria-pressed={task === "transcribe"} onClick={() => setTask("transcribe")} className={segmentClass(task === "transcribe")}>
                  Original language
                </button>
                {(caps?.supportsTranslation ?? true) && (
                  <button type="button" aria-pressed={task === "translate"} onClick={() => setTask("translate")} className={segmentClass(task === "translate")}>
                    English translation
                  </button>
                )}
              </div>
            </fieldset>

            <div>
              <label htmlFor="demo-language" className="mb-2 block text-sm font-medium text-text-body">
                Spoken language
              </label>
              <select
                id="demo-language"
                value={task === "translate" ? "" : language}
                onChange={(e) => setLanguage(e.target.value)}
                disabled={task === "translate"}
                className={`${inputClass} py-2.5`}
              >
                {LANGUAGES.map(([code, name]) => (
                  <option key={code} value={code}>
                    {name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {fieldError && <p role="alert" className="text-sm text-status-danger">{fieldError}</p>}
          {job.phase === "error" && <ErrorNotice error={job.error} />}

          <button
            type="submit"
            disabled={!online}
            className="flex w-full items-center justify-center gap-2 rounded-lg bg-accent-primary px-6 py-3.5 font-bold text-background-primary shadow-[0_0_25px_rgba(0,212,255,0.25)] transition-colors hover:bg-accent-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            <Sparkles size={18} /> Generate transcript
          </button>

          <p className="text-center text-xs leading-relaxed text-text-muted">
            Your audio is sent to Groq for transcription and deleted from the demo server straight after. The demo shares a
            small free quota, so please keep clips short.
          </p>
        </form>
      )}
    </div>
  );
}

/** Live demo of the AI Transcript Generator, backed by the C# API in backend/. */
export default function TranscriptDemo({ isOpen, onClose }) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="AI Transcript Generator — live demo"
      subtitle="ASP.NET Core API (.NET 10) · open-source Whisper models on Groq"
    >
      <DemoBody />
    </Modal>
  );
}

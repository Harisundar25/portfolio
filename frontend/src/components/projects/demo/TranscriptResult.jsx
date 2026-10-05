import { motion } from "framer-motion";
import { Check, Copy, Download, RotateCcw } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { formatClock, titleCase } from "./format";

const DOWNLOADS = [
  { key: "txt", label: "TXT", type: "text/plain" },
  { key: "srt", label: "SRT", type: "application/x-subrip" },
  { key: "vtt", label: "VTT", type: "text/vtt" },
];

function saveFile(name, content, type) {
  const url = URL.createObjectURL(new Blob([content], { type: `${type};charset=utf-8` }));
  const link = Object.assign(document.createElement("a"), { href: url, download: name });
  document.body.append(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

function Meta({ label, value }) {
  return (
    <div className="rounded-lg border border-border-soft bg-background-surface/60 px-3 py-2">
      <dt className="font-mono text-[10px] tracking-[0.2em] text-text-muted uppercase">{label}</dt>
      <dd className="mt-0.5 text-sm font-medium text-text-heading">{value}</dd>
    </div>
  );
}

export default function TranscriptResult({ result, onReset }) {
  const [copied, setCopied] = useState(false);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(result.text);
      setCopied(true);
      toast.success("Transcript copied");
      setTimeout(() => setCopied(false), 2000);
    } catch {
      toast.error("Couldn't copy — please select the text instead.");
    }
  };

  const translated = result.task === "translate";

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} className="space-y-5">
      <div>
        <p className="font-mono text-xs tracking-[0.25em] text-status-success uppercase">Transcript ready</p>
        <h4 className="mt-1 text-xl font-bold break-words">{result.title}</h4>
      </div>

      <dl className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <Meta label={translated ? "Output" : "Language"} value={translated ? "English (translated)" : titleCase(result.language) || "Auto"} />
        <Meta label="Duration" value={result.durationSeconds != null ? formatClock(result.durationSeconds) : "—"} />
        <Meta label="Model" value={result.model} />
        <Meta
          label="Time taken"
          value={`${(result.elapsedMilliseconds / 1000).toFixed(1)} s${result.chunkCount > 1 ? ` · ${result.chunkCount} chunks` : ""}`}
        />
      </dl>

      <div className="max-h-[340px] overflow-y-auto overscroll-contain rounded-xl border border-border-soft bg-background-primary/60 p-4">
        {result.segments.length > 0 ? (
          <ol className="space-y-2.5">
            {result.segments.map((segment, index) => (
              <li
                key={`${segment.start}-${index}`}
                className="grid grid-cols-[3rem_1fr] gap-3 text-sm leading-relaxed sm:grid-cols-[4.5rem_1fr]"
              >
                <span className="pt-px font-mono text-xs text-accent-primary tabular-nums">{formatClock(segment.start)}</span>
                <span className="text-text-body">{segment.text}</span>
              </li>
            ))}
          </ol>
        ) : (
          <p className="text-sm text-text-muted">No speech was detected in this audio.</p>
        )}
      </div>

      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          onClick={copy}
          disabled={!result.text}
          className="flex items-center gap-2 rounded-lg bg-accent-primary px-4 py-2.5 text-sm font-bold text-background-primary transition-colors hover:bg-accent-primary-hover disabled:opacity-50"
        >
          {copied ? <Check size={16} /> : <Copy size={16} />} Copy text
        </button>
        {DOWNLOADS.map(({ key, label, type }) => (
          <button
            key={key}
            type="button"
            onClick={() => saveFile(`${result.fileName}.${key}`, result.documents[key], type)}
            className="flex items-center gap-2 rounded-lg border border-border-default bg-background-surface px-4 py-2.5 text-sm font-semibold text-text-heading transition-colors hover:border-accent-primary hover:text-accent-primary"
          >
            <Download size={16} /> {label}
          </button>
        ))}
        <button
          type="button"
          onClick={onReset}
          className="ml-auto flex items-center gap-2 rounded-lg border border-border-soft px-4 py-2.5 text-sm font-semibold text-text-muted transition-colors hover:text-text-heading"
        >
          <RotateCcw size={16} /> New transcript
        </button>
      </div>
    </motion.div>
  );
}

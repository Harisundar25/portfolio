import { buildCoilPath } from "../../lib/coil";

// Deterministic pseudo-random values keep renders pure and stable.
const noise = (i) => Math.abs(Math.sin(i * 12.9898) * 43758.5453) % 1;

const BARS = Array.from({ length: 56 }, (_, i) => ({
  height: 22 + noise(i) * 78,
  duration: 0.9 + noise(i + 99) * 1.1,
  delay: (i % 7) * 0.12,
}));

const COIL_PATH = buildCoilPath({ turns: 8, rMin: 18, rMax: 180, steps: 600 });

const PACKETS = [
  { bytes: ["03", "F6", "F5"], label: "Control Error" },
  { bytes: ["04", "80", "84"], label: "Received Power" },
  { bytes: ["01", "C8", "C9"], label: "Signal Strength" },
];

function Waveform() {
  return (
    <>
      <div className="absolute inset-x-6 top-6 bottom-12 flex items-center gap-[3px]">
        {BARS.map((bar, i) => (
          <span
            key={i}
            className="flex-1 animate-eq rounded-full bg-gradient-to-t from-accent-primary to-accent-secondary opacity-80"
            style={{ height: `${bar.height}%`, animationDuration: `${bar.duration}s`, animationDelay: `${bar.delay}s` }}
          />
        ))}
      </div>
      <p className="absolute right-6 bottom-4 left-6 truncate font-mono text-[11px] text-text-muted">
        <span className="text-accent-primary">[12.40 --&gt; 15.10]</span> Welcome back — today we're looking at how
        machines learn to listen…
      </p>
    </>
  );
}

function Coil() {
  return (
    <>
      <svg
        viewBox="-200 -200 400 400"
        aria-hidden="true"
        className="absolute top-1/2 -right-16 h-[360px] w-[360px] -translate-y-1/2 animate-[spin_90s_linear_infinite] opacity-70"
      >
        <defs>
          <linearGradient id="coil-card-gradient" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="#ff6a00" />
            <stop offset="100%" stopColor="#00d4ff" />
          </linearGradient>
        </defs>
        <path d={COIL_PATH} fill="none" stroke="url(#coil-card-gradient)" strokeWidth="1.4" strokeOpacity="0.55" />
      </svg>
      <ul className="absolute top-6 left-6 space-y-1.5 font-mono text-[12px]">
        {PACKETS.map((packet) => (
          <li key={packet.label} className="flex items-center gap-2">
            <span className="text-accent-primary">{packet.bytes[0]}</span>
            <span className="text-text-heading">{packet.bytes[1]}</span>
            <span className="text-accent-secondary">{packet.bytes[2]}</span>
            <span className="text-text-disabled">· {packet.label}</span>
          </li>
        ))}
      </ul>
      <div className="absolute bottom-4 left-6 flex gap-2">
        {["Qi v2.2", "Qi v2.3", "MPP"].map((tag) => (
          <span
            key={tag}
            className="rounded-full border border-accent-secondary/30 bg-accent-secondary/10 px-2.5 py-0.5 font-mono text-[11px] text-accent-secondary"
          >
            {tag}
          </span>
        ))}
      </div>
    </>
  );
}

/** Decorative banner at the top of each project card. */
export default function ProjectVisual({ type }) {
  const glow =
    type === "coil"
      ? "bg-[radial-gradient(circle_at_80%_50%,rgba(255,106,0,0.16),transparent_60%)]"
      : "bg-[radial-gradient(circle_at_20%_50%,rgba(0,212,255,0.16),transparent_60%)]";

  return (
    <div aria-hidden="true" className={`relative h-44 overflow-hidden border-b border-border-soft ${glow}`}>
      <div className="absolute inset-0 bg-dot-grid opacity-40" />
      {type === "coil" ? <Coil /> : <Waveform />}
    </div>
  );
}

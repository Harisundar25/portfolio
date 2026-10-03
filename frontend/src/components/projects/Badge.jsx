const TONES = {
  primary: "border-accent-primary/30 bg-accent-primary/10 text-accent-primary",
  secondary: "border-accent-secondary/30 bg-accent-secondary/10 text-accent-secondary",
};

export default function Badge({ tone = "primary", children }) {
  return (
    <span
      className={`inline-flex items-center rounded-full border px-3 py-1 text-xs font-semibold tracking-wide ${TONES[tone] ?? TONES.primary}`}
    >
      {children}
    </span>
  );
}

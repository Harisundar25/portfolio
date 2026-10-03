import { motion } from "framer-motion";

// Inline SVG noise (film grain) so the page has no third-party image dependency.
const NOISE =
  "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='160' height='160'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.8' numOctaves='3' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E\")";

export default function HeroAtmosphere() {
  return (
    <div aria-hidden="true" className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
      {/* Dotted grid fading out from the centre */}
      <div className="absolute inset-0 bg-dot-grid mask-radial opacity-70" />

      {/* Ambient primary glow (cyan) — drifting top left */}
      <motion.div
        animate={{ x: [0, 50, 0], y: [0, -30, 0], opacity: [0.3, 0.5, 0.3] }}
        transition={{ duration: 10, repeat: Infinity, ease: "easeInOut" }}
        className="absolute -top-24 -left-24 h-[420px] w-[420px] rounded-full bg-accent-primary opacity-30 mix-blend-screen blur-[120px] md:h-[520px] md:w-[520px]"
      />

      {/* Ambient secondary glow (orange) — drifting bottom right */}
      <motion.div
        animate={{ x: [0, -40, 0], y: [0, 40, 0], opacity: [0.2, 0.4, 0.2] }}
        transition={{ duration: 12, repeat: Infinity, ease: "easeInOut", delay: 2 }}
        className="absolute top-1/2 -right-24 h-[460px] w-[460px] rounded-full bg-accent-secondary opacity-20 mix-blend-screen blur-[140px] md:h-[620px] md:w-[620px]"
      />

      {/* Cinematic vignette */}
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,transparent_35%,rgba(5,11,20,0.85)_100%)]" />

      {/* Subtle film grain */}
      <div className="absolute inset-0 opacity-[0.035] mix-blend-overlay" style={{ backgroundImage: NOISE }} />
    </div>
  );
}

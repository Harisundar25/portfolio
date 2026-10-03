import { useEffect, useState } from "react";
import { motion, useReducedMotion } from "framer-motion";

// The Qi power-transfer state machine, borrowed as loading phases.
const PHASES = [
  { at: 0, label: "Selection" },
  { at: 18, label: "Digital Ping" },
  { at: 36, label: "Identification & Configuration" },
  { at: 58, label: "Negotiation" },
  { at: 76, label: "Calibration" },
  { at: 92, label: "Power Transfer" },
];

const ringVariants = {
  spin: { rotate: 360, transition: { duration: 3, repeat: Infinity, ease: "linear" } },
  spinReverse: { rotate: -360, transition: { duration: 5, repeat: Infinity, ease: "linear" } },
};

/**
 * Intro loader. There is no image sequence to preload any more, so progress
 * follows a short eased timeline that waits for web fonts before completing.
 */
export default function Loader({ onComplete }) {
  const reduceMotion = useReducedMotion();
  const [progress, setProgress] = useState(0);

  useEffect(() => {
    const duration = reduceMotion ? 300 : 1700;
    const start = performance.now();
    let fontsReady = false;
    let frame = 0;
    let finishTimer = 0;

    const fontsPromise = document.fonts?.ready ?? Promise.resolve();
    fontsPromise.then(() => {
      fontsReady = true;
    });
    const fontsFallback = setTimeout(() => {
      fontsReady = true;
    }, 3500);
    // Failsafe: requestAnimationFrame is paused in hidden tabs and some prerenderers,
    // so make sure the site content always mounts.
    const hardStop = setTimeout(onComplete, reduceMotion ? 1500 : 6000);

    const tick = (now) => {
      const t = Math.min(1, (now - start) / duration);
      const eased = 1 - Math.pow(1 - t, 3);
      const value = fontsReady ? Math.round(eased * 100) : Math.min(Math.round(eased * 100), 94);
      setProgress(value);
      if (value >= 100) {
        finishTimer = setTimeout(onComplete, reduceMotion ? 50 : 450);
        return;
      }
      frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);

    return () => {
      cancelAnimationFrame(frame);
      clearTimeout(fontsFallback);
      clearTimeout(hardStop);
      clearTimeout(finishTimer);
    };
  }, [onComplete, reduceMotion]);

  const phase = PHASES.reduce((current, p) => (progress >= p.at ? p : current), PHASES[0]);

  return (
    <motion.div
      role="status"
      aria-live="polite"
      aria-label={`Loading ${progress}%`}
      className="fixed inset-0 z-[10000] flex flex-col items-center justify-center overflow-hidden bg-[#030712] text-white"
      exit={{
        opacity: 0,
        scale: 1.05,
        filter: "blur(15px)",
        transition: { duration: 0.8, ease: [0.76, 0, 0.24, 1] },
      }}
    >
      {/* Ambience */}
      <div className="pointer-events-none absolute inset-0 overflow-hidden">
        <div className="absolute inset-0 z-10 bg-[radial-gradient(circle_at_50%_50%,rgba(5,11,20,0)_0%,#030712_100%)]" />
        <div className="absolute top-1/2 left-1/2 h-[80vw] w-[80vw] -translate-x-1/2 -translate-y-1/2 animate-pulse bg-[radial-gradient(circle,rgba(0,212,255,0.08)_0%,rgba(0,0,0,0)_60%)] blur-[120px]" />
        <div className="absolute top-1/2 left-1/2 h-[60vw] w-[60vw] -translate-x-1/2 -translate-y-1/2 bg-[radial-gradient(circle,rgba(255,106,0,0.05)_0%,rgba(0,0,0,0)_60%)] blur-[100px]" />
      </div>

      <div className="relative z-20 flex flex-col items-center justify-center">
        <div className="relative flex h-64 w-64 items-center justify-center md:h-80 md:w-80">
          <motion.div
            className="absolute inset-0 rounded-full border border-accent-primary/10 border-t-accent-primary/60 shadow-[0_0_30px_rgba(0,212,255,0.1)]"
            variants={ringVariants}
            animate="spin"
          />
          <motion.div
            className="absolute inset-8 rounded-full border border-accent-secondary/10 border-b-accent-secondary/60"
            variants={ringVariants}
            animate="spinReverse"
          />
          <div className="absolute inset-16 rounded-full border border-dashed border-white/5" />

          <div className="relative flex h-32 w-32 items-center justify-center">
            <div className="absolute inset-0 rounded-full bg-gradient-to-br from-accent-primary/20 to-accent-secondary/20 blur-xl" />
            <div className="relative z-10 font-heading text-5xl font-bold tracking-tighter text-white tabular-nums drop-shadow-[0_0_15px_rgba(255,255,255,0.5)] md:text-6xl">
              {progress}
              <span className="ml-1 align-top font-sans text-sm text-text-muted md:text-base">%</span>
            </div>
          </div>
        </div>

        <div className="mt-8 flex flex-col items-center gap-2">
          <motion.div
            className="text-xs font-medium tracking-[0.4em] text-accent-primary uppercase md:text-sm"
            animate={{ opacity: [0.4, 1, 0.4] }}
            transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
          >
            Establishing Qi link
          </motion.div>
          <div className="h-4 font-mono text-[11px] tracking-widest text-text-muted uppercase">
            {phase.label}
          </div>
          <div className="mt-2 h-[2px] w-32 overflow-hidden rounded-full bg-gray-900">
            <div
              className="h-full bg-gradient-to-r from-accent-primary to-accent-secondary transition-[width] duration-100 ease-linear"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>
      </div>
    </motion.div>
  );
}

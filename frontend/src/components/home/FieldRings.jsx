import { motion, useReducedMotion } from "framer-motion";
import { buildCoilPath } from "../../lib/coil";

const COIL_PATH = buildCoilPath();
const PULSES = [0, 1, 2, 3];

/** Decorative "magnetic field": a slowly rotating coil with pulses radiating outward. */
export default function FieldRings({ className = "" }) {
  const reduceMotion = useReducedMotion();

  return (
    <div
      aria-hidden="true"
      className={`pointer-events-none absolute inset-0 flex items-center justify-center ${className}`}
    >
      <svg viewBox="-200 -200 400 400" className="h-[min(135vmin,1150px)] w-[min(135vmin,1150px)] max-w-none">
        <defs>
          <linearGradient id="field-gradient" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="#00d4ff" />
            <stop offset="100%" stopColor="#ff6a00" />
          </linearGradient>
          <radialGradient id="field-core" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor="rgba(0,212,255,0.16)" />
            <stop offset="100%" stopColor="rgba(0,212,255,0)" />
          </radialGradient>
        </defs>

        <circle r="130" fill="url(#field-core)" />

        <motion.g
          animate={reduceMotion ? undefined : { rotate: 360 }}
          transition={{ duration: 140, repeat: Infinity, ease: "linear" }}
        >
          <path
            d={COIL_PATH}
            fill="none"
            stroke="url(#field-gradient)"
            strokeWidth="1"
            strokeOpacity="0.28"
            vectorEffect="non-scaling-stroke"
          />
        </motion.g>

        {!reduceMotion &&
          PULSES.map((i) => (
            <motion.circle
              key={i}
              r="42"
              fill="none"
              stroke="url(#field-gradient)"
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              initial={{ scale: 1, opacity: 0 }}
              animate={{ scale: [1, 4.6], opacity: [0, 0.55, 0] }}
              transition={{
                scale: { duration: 8, repeat: Infinity, delay: i * 2, ease: "easeOut" },
                opacity: { duration: 8, repeat: Infinity, delay: i * 2, times: [0, 0.15, 1] },
              }}
            />
          ))}
      </svg>
    </div>
  );
}

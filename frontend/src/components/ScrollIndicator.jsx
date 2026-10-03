import { motion } from "framer-motion";

export default function ScrollIndicator({ className = "" }) {
  return (
    <div
      aria-hidden="true"
      className={`flex flex-col items-center gap-2 opacity-80 mix-blend-plus-lighter ${className}`}
    >
      {/* Animated mouse icon */}
      <div className="relative flex h-10 w-6 justify-center rounded-full border-2 border-text-muted p-1">
        <motion.div
          animate={{ y: [0, 12, 0], opacity: [1, 0, 0] }}
          transition={{ duration: 1.5, repeat: Infinity, ease: "easeInOut" }}
          className="h-2 w-1 rounded-full bg-accent-primary"
        />
      </div>

      <div className="flex flex-col items-center gap-2">
        <span className="font-mono text-[10px] tracking-[0.3em] text-text-muted uppercase">
          Scroll
        </span>
        <div className="relative h-10 w-px overflow-hidden bg-gradient-to-b from-text-muted/20 to-transparent">
          <motion.div
            animate={{ y: ["-100%", "100%"] }}
            transition={{ duration: 2, repeat: Infinity, ease: "linear" }}
            className="absolute inset-0 bg-gradient-to-b from-transparent via-accent-primary to-transparent"
          />
        </div>
      </div>
    </div>
  );
}

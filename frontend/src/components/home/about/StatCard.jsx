import { useEffect, useRef } from "react";
import { animate, motion, useInView, useMotionValue, useReducedMotion, useTransform } from "framer-motion";

function CountUp({ value }) {
  const ref = useRef(null);
  const inView = useInView(ref, { once: true, margin: "-60px" });
  const reduceMotion = useReducedMotion();
  const count = useMotionValue(reduceMotion ? value : 0);
  const rounded = useTransform(count, (v) => Math.round(v));

  useEffect(() => {
    if (!inView || reduceMotion) return undefined;
    const controls = animate(count, value, { duration: 1.6, ease: [0.16, 1, 0.3, 1] });
    return () => controls.stop();
  }, [inView, reduceMotion, count, value]);

  return <motion.span ref={ref}>{rounded}</motion.span>;
}

const accents = [
  "hover:border-accent-primary/60 hover:shadow-[0_0_24px_rgba(0,212,255,0.12)]",
  "hover:border-accent-secondary/60 hover:shadow-[0_0_24px_rgba(255,106,0,0.12)]",
];

export default function StatCard({ stat, index }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 30, scale: 0.96 }}
      whileInView={{ opacity: 1, y: 0, scale: 1 }}
      viewport={{ once: true, margin: "-60px" }}
      transition={{ duration: 0.6, delay: index * 0.08 }}
      className={`flex flex-col justify-between rounded-2xl border border-border-soft bg-background-glass p-6 shadow-soft backdrop-blur-[18px] transition-all duration-300 ${accents[index % 2]}`}
    >
      <div className="bg-gradient-to-r from-accent-primary to-accent-secondary bg-clip-text font-heading text-5xl font-bold text-transparent tabular-nums md:text-6xl">
        <CountUp value={stat.value} />
        {stat.suffix}
      </div>
      <div className="mt-4">
        <p className="text-sm font-semibold tracking-wide text-text-heading uppercase">{stat.label}</p>
        {stat.note && <p className="mt-1 text-xs text-text-muted">{stat.note}</p>}
      </div>
    </motion.div>
  );
}

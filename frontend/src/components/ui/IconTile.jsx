import { motion } from "framer-motion";

/** Circular brand-icon tile with a spring pop-in and hover label (reference "Skills" style). */
export default function IconTile({ tool, index = 0, size = "lg" }) {
  const Icon = tool.icon;
  const dims = size === "lg" ? "h-20 w-20 md:h-24 md:w-24" : "h-14 w-14";
  const iconSize = size === "lg" ? "text-4xl md:text-5xl" : "text-2xl";

  return (
    <motion.li
      initial={{ opacity: 0, scale: 0 }}
      whileInView={{ opacity: 1, scale: 1 }}
      whileHover={{ scale: 1.15, zIndex: 10 }}
      viewport={{ once: true }}
      transition={{ delay: index * 0.03, type: "spring", stiffness: 260, damping: 20 }}
      className="group relative flex flex-col items-center p-2"
    >
      <div
        className={`relative flex ${dims} items-center justify-center rounded-full border border-transparent bg-background-surface/60 transition-colors duration-300 group-hover:border-border-soft group-hover:bg-background-secondary`}
      >
        <Icon
          aria-hidden="true"
          className={`${iconSize} transition-transform duration-300 group-hover:scale-110`}
          style={{ color: tool.color, filter: "drop-shadow(0 0 10px rgba(0,0,0,0.5))" }}
        />
        <span
          className="pointer-events-none absolute inset-0 rounded-full opacity-0 transition-opacity duration-300 group-hover:opacity-100"
          style={{ boxShadow: `0 0 28px ${tool.color}33` }}
        />
      </div>
      <span className="pointer-events-none z-20 mt-2 text-xs font-bold whitespace-nowrap text-text-heading transition-all md:absolute md:-bottom-8 md:translate-y-2 md:rounded-full md:border md:border-border-soft md:bg-background-secondary md:px-3 md:py-1.5 md:opacity-0 md:backdrop-blur-sm md:group-hover:translate-y-0 md:group-hover:opacity-100">
        {tool.name}
      </span>
    </motion.li>
  );
}

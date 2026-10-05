import { useRef } from "react";
import { motion, useScroll, useTransform } from "framer-motion";
import { Briefcase, GraduationCap } from "lucide-react";

function TimelineItem({ item }) {
  const Icon = item.education ? GraduationCap : Briefcase;

  return (
    <motion.li
      initial={{ opacity: 0, y: 30 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.6 }}
      className="relative grid gap-3 pl-12 md:grid-cols-[160px_1fr] md:gap-x-12 md:pl-0"
    >
      {/* Node on the line */}
      <span className="absolute top-1.5 left-[9px] flex h-3.5 w-3.5 items-center justify-center md:left-[177px]">
        {item.current && (
          <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-accent-primary opacity-60" />
        )}
        <span className="relative h-3.5 w-3.5 rounded-full border-2 border-accent-primary bg-background-primary shadow-[0_0_12px_rgba(0,212,255,0.6)]" />
      </span>

      <div className="md:pt-0.5 md:text-right">
        <p className="font-mono text-sm text-accent-primary">{item.period}</p>
        <p className="mt-1 text-xs text-text-muted">{item.location}</p>
      </div>

      <div className="group rounded-2xl border border-border-soft bg-background-glass p-6 backdrop-blur-[18px] transition-colors duration-300 hover:border-accent-primary/40">
        <div className="flex items-start gap-3">
          <span className="mt-0.5 rounded-lg bg-accent-primary/10 p-2 text-accent-primary">
            <Icon size={18} />
          </span>
          <div>
            <h4 className="text-xl font-bold">{item.role}</h4>
            <p className="text-sm text-text-muted">
              {item.company}
              {item.employmentType && ` · ${item.employmentType}`}
            </p>
          </div>
        </div>
        <ul className="mt-5 space-y-2.5">
          {item.points.map((point) => (
            <li key={point} className="flex gap-3 text-sm leading-relaxed text-text-body">
              <span className="mt-2 h-1.5 w-1.5 flex-shrink-0 rounded-full bg-accent-primary" />
              {point}
            </li>
          ))}
        </ul>
        {item.tags.length > 0 && (
          <div className="mt-5 flex flex-wrap gap-2">
            {item.tags.map((tag) => (
              <span
                key={tag}
                className="rounded-full border border-border-default bg-background-surface px-3 py-1 text-xs font-medium text-text-muted"
              >
                {tag}
              </span>
            ))}
          </div>
        )}
      </div>
    </motion.li>
  );
}

/** Vertical timeline whose progress line draws itself while scrolling. */
export default function ExperienceTimeline({ items }) {
  const ref = useRef(null);
  const { scrollYProgress } = useScroll({ target: ref, offset: ["start 0.75", "end 0.6"] });
  const lineScale = useTransform(scrollYProgress, [0, 1], [0, 1]);

  return (
    <div ref={ref} className="relative">
      <div className="absolute top-2 bottom-2 left-[15px] w-px bg-border-soft md:left-[183px]" />
      <motion.div
        style={{ scaleY: lineScale }}
        className="absolute top-2 bottom-2 left-[15px] w-px origin-top bg-gradient-to-b from-accent-primary via-accent-primary to-accent-secondary md:left-[183px]"
      />
      <ol className="space-y-10">
        {items.map((item) => (
          <TimelineItem key={`${item.role}-${item.period}`} item={item} />
        ))}
      </ol>
    </div>
  );
}

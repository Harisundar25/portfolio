import { useRef } from "react";
import { motion, useScroll, useTransform } from "framer-motion";

function Word({ children, progress, range, highlight }) {
  const opacity = useTransform(progress, range, [0.14, 1]);
  const y = useTransform(progress, range, [8, 0]);

  return (
    <span className="relative mr-[0.27em] inline-block">
      <motion.span
        style={{ opacity, y }}
        className={`inline-block ${highlight ? "text-gradient-brand" : "text-text-heading"}`}
      >
        {children}
      </motion.span>
    </span>
  );
}

/** Each word brightens in sequence as the paragraph scrolls through the viewport. */
export default function ScrollRevealText({ segments, className = "" }) {
  const ref = useRef(null);
  const { scrollYProgress } = useScroll({ target: ref, offset: ["start 0.9", "end 0.45"] });

  const words = segments.flatMap((segment) =>
    segment.text
      .split(" ")
      .filter(Boolean)
      .map((word) => ({ word, highlight: Boolean(segment.hl) })),
  );
  const fullText = segments.map((s) => s.text).join(" ");

  return (
    <p ref={ref} className={className}>
      <span className="sr-only">{fullText}</span>
      <span aria-hidden="true" className="flex flex-wrap">
        {words.map(({ word, highlight }, i) => {
          const start = i / words.length;
          return (
            <Word
              key={`${word}-${i}`}
              progress={scrollYProgress}
              range={[start, start + 1 / words.length]}
              highlight={highlight}
            >
              {word}
            </Word>
          );
        })}
      </span>
    </p>
  );
}

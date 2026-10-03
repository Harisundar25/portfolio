import { useEffect, useState } from "react";
import { motion, useMotionValue, useSpring } from "framer-motion";

const INTERACTIVE = "a, button, [role='button'], input, textarea, select, label";

const supportsFinePointer = () =>
  typeof window !== "undefined" && window.matchMedia("(pointer: fine)").matches;

export default function CustomCursor() {
  const [enabled, setEnabled] = useState(supportsFinePointer);
  const [isHovering, setIsHovering] = useState(false);

  // Motion values avoid a React re-render on every mouse move.
  const x = useMotionValue(-100);
  const y = useMotionValue(-100);
  const ringX = useSpring(x, { stiffness: 150, damping: 15, mass: 0.1 });
  const ringY = useSpring(y, { stiffness: 150, damping: 15, mass: 0.1 });
  const dotX = useSpring(x, { stiffness: 500, damping: 28 });
  const dotY = useSpring(y, { stiffness: 500, damping: 28 });

  useEffect(() => {
    const media = window.matchMedia("(pointer: fine)");
    const onChange = (e) => setEnabled(e.matches);
    media.addEventListener("change", onChange);
    return () => media.removeEventListener("change", onChange);
  }, []);

  useEffect(() => {
    if (!enabled) return undefined;

    const onMove = (e) => {
      x.set(e.clientX);
      y.set(e.clientY);
    };
    const onOver = (e) => {
      setIsHovering(Boolean(e.target instanceof Element && e.target.closest(INTERACTIVE)));
    };

    window.addEventListener("mousemove", onMove);
    window.addEventListener("mouseover", onOver);
    return () => {
      window.removeEventListener("mousemove", onMove);
      window.removeEventListener("mouseover", onOver);
    };
  }, [enabled, x, y]);

  if (!enabled) return null;

  return (
    <>
      <motion.div
        aria-hidden="true"
        className="pointer-events-none fixed top-0 left-0 z-[9999] -mt-4 -ml-4 h-8 w-8 rounded-full border"
        style={{ x: ringX, y: ringY, backdropFilter: "blur(2px)" }}
        animate={{
          scale: isHovering ? 1.5 : 1,
          opacity: isHovering ? 0.8 : 0.4,
          borderColor: isHovering ? "#00d4ff" : "#7c8ba1",
        }}
        transition={{ type: "spring", stiffness: 300, damping: 20 }}
      />
      <motion.div
        aria-hidden="true"
        className="pointer-events-none fixed top-0 left-0 z-[9999] -mt-1 -ml-1 h-2 w-2 rounded-full bg-accent-primary"
        style={{ x: dotX, y: dotY }}
      />
    </>
  );
}

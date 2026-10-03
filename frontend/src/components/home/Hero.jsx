import { useRef } from "react";
import { motion, useScroll, useTransform } from "framer-motion";
import { Link } from "react-router";
import { ArrowRight, Mail } from "lucide-react";
import HeroAtmosphere from "../HeroAtmosphere";
import ScrollIndicator from "../ScrollIndicator";
import Magnetic from "../Magnetic";
import FieldRings from "./FieldRings";
import { useSmoothScroll } from "../../context/SmoothScrollContext";
import { scrollToTarget } from "../../lib/scroll";
import { profile } from "../../data/profile";

const nameWords = profile.name.toUpperCase().split(" ");

const container = {
  hidden: {},
  visible: { transition: { staggerChildren: 0.05, delayChildren: 0.2 } },
};

const letter = {
  hidden: { opacity: 0, y: 40, filter: "blur(12px)" },
  visible: {
    opacity: 1,
    y: 0,
    filter: "blur(0px)",
    transition: { duration: 0.7, ease: [0.2, 0.65, 0.3, 0.9] },
  },
};

const fadeUp = (delay) => ({
  initial: { opacity: 0, y: 20, filter: "blur(8px)" },
  animate: { opacity: 1, y: 0, filter: "blur(0px)" },
  transition: { duration: 0.7, delay, ease: [0.2, 0.65, 0.3, 0.9] },
});

export default function Hero() {
  const ref = useRef(null);
  const { lenis } = useSmoothScroll();
  const { scrollYProgress } = useScroll({ target: ref, offset: ["start start", "end start"] });

  // Scroll-linked exit: content lifts, shrinks and fades while the field expands.
  const contentOpacity = useTransform(scrollYProgress, [0, 0.6], [1, 0]);
  const contentScale = useTransform(scrollYProgress, [0, 1], [1, 0.88]);
  const contentY = useTransform(scrollYProgress, [0, 1], [0, 140]);
  const fieldScale = useTransform(scrollYProgress, [0, 1], [1, 1.35]);
  const fieldOpacity = useTransform(scrollYProgress, [0, 0.9], [1, 0.2]);

  return (
    <section
      id="home"
      ref={ref}
      className="relative isolate flex min-h-[100svh] items-center justify-center overflow-hidden px-4 pt-24 pb-32 md:pt-32"
    >
      <HeroAtmosphere />
      <motion.div style={{ scale: fieldScale, opacity: fieldOpacity }} className="absolute inset-0 -z-10">
        <FieldRings />
      </motion.div>

      <motion.div
        style={{ opacity: contentOpacity, scale: contentScale, y: contentY }}
        className="relative z-10 mx-auto max-w-6xl text-center"
      >
        <motion.div
          {...fadeUp(0.05)}
          className="mb-8 inline-flex items-center gap-2.5 rounded-full border border-border-default bg-background-glass px-4 py-1.5 text-xs font-medium text-text-body backdrop-blur-md md:text-sm"
        >
          <span className="relative flex h-2 w-2">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-status-success opacity-75" />
            <span className="relative inline-flex h-2 w-2 rounded-full bg-status-success" />
          </span>
          {profile.role} · {profile.locationShort}
        </motion.div>

        <motion.h1
          variants={container}
          initial="hidden"
          animate="visible"
          aria-label={profile.name}
          className="font-heading text-[clamp(3.1rem,12vw,11.5rem)] leading-[0.85] font-bold tracking-tighter text-white drop-shadow-2xl select-none"
        >
          {nameWords.map((word, wi) => (
            <span key={word} aria-hidden="true" className="inline-block whitespace-nowrap">
              {word.split("").map((char, ci) => (
                <motion.span key={`${char}-${ci}`} variants={letter} className="inline-block">
                  {char}
                </motion.span>
              ))}
              {wi < nameWords.length - 1 && <span className="inline-block w-[0.28em]" />}
            </span>
          ))}
        </motion.h1>

        <motion.div {...fadeUp(0.75)} className="mt-8 flex items-center justify-center gap-4 md:mt-10">
          <div className="h-px w-8 bg-accent-primary/50 md:w-16" />
          <p className="font-sans text-[11px] font-medium tracking-[0.35em] text-accent-primary uppercase md:text-sm md:tracking-[0.45em]">
            Software Engineer · C# / .NET
          </p>
          <div className="h-px w-8 bg-accent-primary/50 md:w-16" />
        </motion.div>

        <motion.p
          {...fadeUp(0.9)}
          className="mx-auto mt-6 max-w-2xl text-base leading-relaxed text-text-body md:text-xl"
        >
          I build C#/.NET backend software for{" "}
          <span className="font-medium text-text-heading">WPC Qi wireless power</span> certification
          systems — turning binary protocol packets and specification clauses into reliable, tested code.
        </motion.p>

        <motion.div {...fadeUp(1.05)} className="mt-10 flex flex-wrap items-center justify-center gap-4">
          <Magnetic>
            <Link
              to="/projects"
              className="group inline-flex items-center gap-2 rounded-full bg-accent-primary px-7 py-3.5 font-bold text-background-primary shadow-[0_0_25px_rgba(0,212,255,0.35)] transition-colors hover:bg-accent-primary-hover"
            >
              View Projects
              <ArrowRight size={18} className="transition-transform group-hover:translate-x-1" />
            </Link>
          </Magnetic>
          <Magnetic>
            <a
              href="/#contact"
              onClick={(e) => {
                e.preventDefault();
                scrollToTarget(lenis, "contact");
              }}
              className="inline-flex items-center gap-2 rounded-full border border-border-default bg-background-glass px-7 py-3.5 font-semibold text-text-heading backdrop-blur-md transition-colors hover:border-accent-secondary hover:text-accent-secondary"
            >
              <Mail size={18} />
              Get in touch
            </a>
          </Magnetic>
        </motion.div>
      </motion.div>

      <motion.div
        style={{ opacity: contentOpacity }}
        className="absolute bottom-8 left-1/2 hidden -translate-x-1/2 md:block"
      >
        <ScrollIndicator />
      </motion.div>
    </section>
  );
}

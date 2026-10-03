import { motion } from "framer-motion";
import ScrollRevealText from "./about/ScrollRevealText";
import StatCard from "./about/StatCard";
import ExperienceTimeline from "./about/ExperienceTimeline";
import Credentials from "./about/Credentials";
import { aboutParagraphs, aboutStatement, experience, quickFacts, stats } from "../../data/profile";

export default function About() {
  return (
    <section id="about" className="relative z-10 overflow-hidden py-28 md:py-40">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute top-0 right-[-10%] h-[520px] w-[520px] rounded-full bg-[radial-gradient(circle,rgba(255,106,0,0.10)_0%,rgba(0,0,0,0)_70%)] blur-[80px]"
      />

      <div className="mx-auto max-w-6xl px-6">
        <motion.span
          initial={{ opacity: 0, y: 12 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          className="inline-block rounded-full border border-accent-primary/20 bg-accent-primary/10 px-3 py-1 text-sm font-medium text-accent-primary"
        >
          About Me
        </motion.span>

        <h2 className="sr-only">About Me</h2>
        <ScrollRevealText
          segments={aboutStatement}
          className="mt-8 font-heading text-[1.9rem] leading-[1.18] font-semibold tracking-tight md:text-5xl lg:text-[3.6rem]"
        />

        <div className="mt-20 grid items-start gap-12 lg:grid-cols-[1.1fr_1fr] lg:gap-16">
          <motion.div
            initial={{ opacity: 0, x: -40 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true, margin: "-80px" }}
            transition={{ duration: 0.8 }}
          >
            <div className="space-y-5 text-lg leading-relaxed text-text-body">
              {aboutParagraphs.map((paragraph) => (
                <p key={paragraph}>{paragraph}</p>
              ))}
            </div>
            <dl className="mt-8 grid gap-4 border-t border-border-soft pt-8 sm:grid-cols-3">
              {quickFacts.map((fact) => (
                <div key={fact.label}>
                  <dt className="font-mono text-[11px] tracking-[0.25em] text-text-muted uppercase">{fact.label}</dt>
                  <dd className="mt-1.5 text-sm font-medium text-text-heading">{fact.value}</dd>
                </div>
              ))}
            </dl>
          </motion.div>

          <div className="grid grid-cols-2 gap-4">
            {stats.map((stat, index) => (
              <StatCard key={stat.label} stat={stat} index={index} />
            ))}
          </div>
        </div>

        <div className="mt-28">
          <div className="mb-12 flex items-end justify-between gap-6">
            <h3 className="text-3xl font-bold tracking-tight md:text-5xl">
              Experience <span className="text-gradient-brand">&amp; education</span>
            </h3>
          </div>
          <ExperienceTimeline items={experience} />
        </div>

        <div className="mt-24">
          <Credentials />
        </div>
      </div>
    </section>
  );
}

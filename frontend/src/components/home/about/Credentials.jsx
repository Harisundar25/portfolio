import { motion } from "framer-motion";
import { ArrowUpRight, Award, BadgeCheck, Languages } from "lucide-react";
import { achievements, certifications, languages } from "../../../data/profile";

const LEVELS = { Native: 5, "Professional working": 4, Conversational: 3, Basic: 2 };

function Panel({ icon: Icon, title, children, delay = 0, accent = "primary" }) {
  const tone =
    accent === "primary"
      ? "bg-accent-primary/10 text-accent-primary"
      : "bg-accent-secondary/10 text-accent-secondary";

  return (
    <motion.div
      initial={{ opacity: 0, y: 30 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-60px" }}
      transition={{ duration: 0.6, delay }}
      className="rounded-2xl border border-border-soft bg-background-glass p-6 backdrop-blur-[18px] md:p-7"
    >
      <div className="mb-5 flex items-center gap-3">
        <span className={`rounded-lg p-2 ${tone}`}>
          <Icon size={18} />
        </span>
        <h4 className="text-lg font-bold">{title}</h4>
      </div>
      {children}
    </motion.div>
  );
}

export default function Credentials() {
  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <Panel icon={Award} title="Achievements" accent="secondary">
        <ul className="space-y-5">
          {achievements.map((a) => (
            <li key={a.title}>
              <p className="font-semibold text-text-heading">{a.title}</p>
              <p className="font-mono text-xs text-accent-secondary">{a.date}</p>
              <p className="mt-1 text-sm text-text-muted">{a.description}</p>
            </li>
          ))}
        </ul>
      </Panel>

      <Panel icon={BadgeCheck} title="Certifications" delay={0.08}>
        <ul className="space-y-4">
          {certifications.map((c) => (
            <li key={c.title}>
              <a
                href={c.url}
                target="_blank"
                rel="noopener noreferrer"
                className="group flex items-start justify-between gap-3"
              >
                <span>
                  <span className="block text-sm font-semibold text-text-heading transition-colors group-hover:text-accent-primary">
                    {c.title}
                  </span>
                  <span className="text-xs text-text-muted">
                    {c.issuer}
                    {c.date ? ` · ${c.date}` : ""}
                  </span>
                </span>
                <ArrowUpRight
                  size={16}
                  aria-label="Verify credential"
                  className="mt-0.5 flex-shrink-0 text-text-muted transition-all group-hover:translate-x-0.5 group-hover:-translate-y-0.5 group-hover:text-accent-primary"
                />
              </a>
            </li>
          ))}
        </ul>
      </Panel>

      <Panel icon={Languages} title="Languages" delay={0.16}>
        <ul className="space-y-4">
          {languages.map((l) => (
            <li key={l.name}>
              <div className="flex items-baseline justify-between">
                <span className="text-sm font-semibold text-text-heading">{l.name}</span>
                <span className="text-xs text-text-muted">{l.level}</span>
              </div>
              <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-background-surface" aria-hidden="true">
                <motion.div
                  initial={{ width: 0 }}
                  whileInView={{ width: `${((LEVELS[l.level] ?? 0) / 5) * 100}%` }}
                  viewport={{ once: true }}
                  transition={{ duration: 1, ease: [0.16, 1, 0.3, 1] }}
                  className="h-full rounded-full bg-gradient-to-r from-accent-primary to-accent-secondary"
                />
              </div>
            </li>
          ))}
        </ul>
      </Panel>
    </div>
  );
}

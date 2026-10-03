import { useId, useRef, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { Layers } from "lucide-react";
import { skillCategories, skillIcon } from "../../../data/skills";

const ALL = { id: "all", label: "All", icon: Layers, blurb: "Everything on my résumé, in one place." };
const TABS = [ALL, ...skillCategories];

function SkillChip({ name }) {
  const brand = skillIcon(name);
  const Icon = brand?.icon;
  return (
    <span className="inline-flex items-center gap-2 rounded-full border border-border-default bg-background-surface px-3.5 py-2 text-sm font-medium text-text-heading transition-colors hover:border-accent-primary/50 hover:text-accent-primary">
      {Icon && <Icon aria-hidden="true" style={{ color: brand.color }} className="text-base" />}
      {name}
    </span>
  );
}

export default function SkillExplorer() {
  const [activeId, setActiveId] = useState("backend");
  const tabRefs = useRef([]);
  const baseId = useId();

  const active = TABS.find((t) => t.id === activeId) ?? ALL;
  const visibleSkills =
    activeId === "all" ? skillCategories.flatMap((c) => c.skills) : active.skills;

  // Arrow-key navigation between tabs (WAI-ARIA tabs pattern).
  const onKeyDown = (e, index) => {
    if (e.key !== "ArrowRight" && e.key !== "ArrowLeft") return;
    e.preventDefault();
    const next = (index + (e.key === "ArrowRight" ? 1 : -1) + TABS.length) % TABS.length;
    setActiveId(TABS[next].id);
    tabRefs.current[next]?.focus();
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 30 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.6 }}
      className="rounded-3xl border border-border-soft bg-background-glass p-6 backdrop-blur-[18px] md:p-8"
    >
      <div className="flex items-center justify-between gap-4">
        <h3 className="text-2xl font-bold">Skill explorer</h3>
        <span className="font-mono text-xs text-text-muted">
          {visibleSkills.length.toString().padStart(2, "0")} skills
        </span>
      </div>

      <div role="tablist" aria-label="Skill categories" className="mt-6 flex flex-wrap gap-2">
        {TABS.map((tab, index) => {
          const selected = tab.id === activeId;
          const Icon = tab.icon;
          return (
            <button
              key={tab.id}
              ref={(el) => {
                tabRefs.current[index] = el;
              }}
              id={`${baseId}-tab-${tab.id}`}
              role="tab"
              type="button"
              aria-selected={selected}
              aria-controls={`${baseId}-panel`}
              tabIndex={selected ? 0 : -1}
              onClick={() => setActiveId(tab.id)}
              onKeyDown={(e) => onKeyDown(e, index)}
              className={`relative inline-flex items-center gap-2 rounded-full px-4 py-2 text-sm font-medium transition-colors ${
                selected ? "text-accent-primary" : "text-text-muted hover:text-text-body"
              }`}
            >
              {selected && (
                <motion.span
                  layoutId={`${baseId}-pill`}
                  className="absolute inset-0 rounded-full border border-accent-primary/30 bg-accent-primary/10"
                  transition={{ type: "spring", stiffness: 380, damping: 30 }}
                />
              )}
              <Icon size={15} className="relative" />
              <span className="relative">{tab.label}</span>
            </button>
          );
        })}
      </div>

      <div
        id={`${baseId}-panel`}
        role="tabpanel"
        aria-labelledby={`${baseId}-tab-${activeId}`}
        className="mt-6"
      >
        <AnimatePresence mode="wait">
          <motion.p
            key={activeId}
            initial={{ opacity: 0, y: 6 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -6 }}
            transition={{ duration: 0.2 }}
            className="text-sm text-text-muted"
          >
            {active.blurb}
          </motion.p>
        </AnimatePresence>

        <motion.ul layout className="mt-5 flex min-h-[9rem] flex-wrap content-start gap-2.5">
          <AnimatePresence mode="popLayout">
            {visibleSkills.map((skill) => (
              <motion.li
                key={skill}
                layout
                initial={{ opacity: 0, scale: 0.8 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.8 }}
                transition={{ type: "spring", stiffness: 420, damping: 30 }}
              >
                <SkillChip name={skill} />
              </motion.li>
            ))}
          </AnimatePresence>
        </motion.ul>
      </div>
    </motion.div>
  );
}

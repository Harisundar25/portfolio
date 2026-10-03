import { useRef } from "react";
import { motion, useInView } from "framer-motion";
import { ArrowRight, Lock, PlayCircle } from "lucide-react";
import { FaGithub } from "react-icons/fa";
import Badge from "./Badge";
import ProjectVisual from "./ProjectVisual";

const MAX_TECH = 6;

export default function ProjectCard({ project, index, onSelect, onDemo }) {
  const ref = useRef(null);
  const isInView = useInView(ref, { once: true, margin: "-100px" });
  const extraTech = project.tech.length - MAX_TECH;

  return (
    <motion.article
      ref={ref}
      initial={{ opacity: 0, y: 50 }}
      animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 50 }}
      transition={{ duration: 0.6, delay: index * 0.12 }}
      className="group relative flex h-full flex-col overflow-hidden rounded-2xl border border-border-soft bg-background-glass backdrop-blur-[18px] transition-all duration-500 hover:border-accent-primary/50 hover:shadow-[0_0_30px_rgba(0,212,255,0.15)]"
    >
      <ProjectVisual type={project.visual} />

      <div className="flex flex-1 flex-col p-6 md:p-8">
        <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
          <Badge tone={project.badge.tone}>{project.badge.label}</Badge>
          <span className="font-mono text-xs text-text-muted">{project.year}</span>
        </div>

        <h3 className="font-heading text-2xl font-bold tracking-tight transition-colors group-hover:text-accent-primary md:text-3xl">
          {project.title}
        </h3>
        <p className="mt-1 font-mono text-xs text-text-muted">{project.subtitle}</p>

        <p className="mt-5 line-clamp-5 text-sm leading-relaxed text-text-body">{project.description}</p>

        <div className="mt-6 flex flex-wrap gap-2">
          {project.tech.slice(0, MAX_TECH).map((t) => (
            <span
              key={t}
              className="rounded-full border border-border-default bg-background-surface px-3 py-1 text-xs font-medium text-text-muted transition-colors group-hover:border-accent-primary/30"
            >
              {t}
            </span>
          ))}
          {extraTech > 0 && <span className="px-2 py-1 text-xs text-text-muted">+{extraTech}</span>}
        </div>

        <div className="mt-auto flex flex-wrap gap-3 pt-8">
          <button
            type="button"
            onClick={() => onSelect(project)}
            className="flex flex-1 items-center justify-center gap-2 rounded-lg border border-border-default bg-background-surface px-4 py-2.5 text-sm font-semibold text-text-heading transition-all group-hover:-translate-y-0.5 hover:border-accent-primary hover:text-accent-primary"
          >
            Read More <ArrowRight size={16} />
          </button>

          {project.links.demo && onDemo && (
            <button
              type="button"
              onClick={onDemo}
              className="flex items-center justify-center gap-2 rounded-lg bg-accent-primary px-4 py-2.5 text-sm font-bold text-background-primary transition-colors hover:bg-accent-primary-hover"
            >
              <PlayCircle size={16} /> Live demo
            </button>
          )}

          {project.links.source ? (
            <a
              href={project.links.source}
              target="_blank"
              rel="noopener noreferrer"
              aria-label={`${project.title} source code on GitHub`}
              className="flex items-center justify-center rounded-lg border border-border-default bg-background-surface px-3.5 text-text-heading transition-colors hover:border-accent-primary hover:text-accent-primary"
            >
              <FaGithub size={18} />
            </a>
          ) : (
            <span
              title="Proprietary — source code isn't public"
              className="flex items-center gap-1.5 rounded-lg border border-border-soft px-3.5 text-xs font-medium text-text-muted"
            >
              <Lock size={14} /> Proprietary
            </span>
          )}
        </div>
      </div>
    </motion.article>
  );
}

import { motion } from "framer-motion";
import { Calendar, Info, Lock, PlayCircle, User } from "lucide-react";
import { FaGithub } from "react-icons/fa";
import Modal from "../ui/Modal";
import Badge from "./Badge";

export default function ProjectDetailModal({ project, isOpen, onClose, onDemo }) {
  return (
    <Modal isOpen={isOpen && Boolean(project)} onClose={onClose} title={project?.title} subtitle={project?.subtitle}>
      {project && (
        <div className="space-y-8">
          <div className="space-y-4">
            <Badge tone={project.badge.tone}>{project.badge.label}</Badge>
            <p className="text-lg leading-relaxed text-text-body">{project.description}</p>

            <div className="flex flex-wrap gap-x-6 gap-y-2 text-sm text-text-muted">
              <span className="flex items-center gap-2">
                <Calendar size={15} className="text-accent-primary" /> {project.year}
              </span>
              <span className="flex items-center gap-2">
                <User size={15} className="text-accent-primary" /> {project.role}
              </span>
            </div>

            <div className="flex flex-wrap gap-2 pt-1">
              {project.tech.map((t) => (
                <span
                  key={t}
                  className="rounded-full border border-border-default bg-background-surface px-3 py-1 text-xs font-medium text-text-heading"
                >
                  {t}
                </span>
              ))}
            </div>
          </div>

          {project.details && (
            <div className="grid gap-6 rounded-xl border border-border-soft bg-background-surface/50 p-6 md:grid-cols-2">
              <div className="space-y-2">
                <h4 className="font-bold text-accent-secondary">The Challenge</h4>
                <p className="text-sm leading-relaxed text-text-muted">{project.details.challenge}</p>
              </div>
              <div className="space-y-2">
                <h4 className="font-bold text-status-success">The Approach</h4>
                <p className="text-sm leading-relaxed text-text-muted">{project.details.solution}</p>
              </div>
            </div>
          )}

          {project.features?.length > 0 && (
            <div className="space-y-3">
              <h4 className="font-bold">{project.featuresTitle ?? "Key Features"}</h4>
              <ul className="grid gap-2.5">
                {project.features.map((feature, idx) => (
                  <motion.li
                    key={feature}
                    initial={{ opacity: 0, x: -10 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ delay: idx * 0.05 }}
                    className="flex items-start gap-3 text-sm leading-relaxed text-text-body"
                  >
                    <span className="mt-2 h-1.5 w-1.5 flex-shrink-0 rounded-full bg-accent-primary" />
                    {feature}
                  </motion.li>
                ))}
              </ul>
            </div>
          )}

          {project.notes?.length > 0 && (
            <aside className="rounded-xl border border-accent-secondary/30 bg-accent-secondary/5 p-5">
              <h4 className="mb-3 flex items-center gap-2 font-bold text-accent-secondary">
                <Info size={17} /> {project.notesTitle ?? "Notes"}
              </h4>
              <ul className="space-y-2">
                {project.notes.map((note) => (
                  <li key={note} className="flex items-start gap-3 text-sm leading-relaxed text-text-body">
                    <span className="mt-2 h-1.5 w-1.5 flex-shrink-0 rounded-full bg-accent-secondary" />
                    {note}
                  </li>
                ))}
              </ul>
            </aside>
          )}

          <div className="flex flex-col gap-4 border-t border-border-soft pt-6 sm:flex-row">
            {project.links.demo && onDemo && (
              <button
                type="button"
                onClick={onDemo}
                className="flex flex-1 items-center justify-center gap-2 rounded-lg bg-accent-primary px-6 py-3 font-bold text-background-primary transition-colors hover:bg-accent-primary-hover"
              >
                <PlayCircle size={18} /> Try the live demo
              </button>
            )}
            {project.links.source ? (
              <a
                href={project.links.source}
                target="_blank"
                rel="noopener noreferrer"
                className="flex flex-1 items-center justify-center gap-2 rounded-lg border border-border-default bg-background-surface px-6 py-3 font-semibold text-text-heading transition-colors hover:bg-background-secondary"
              >
                <FaGithub size={18} /> Source code
              </a>
            ) : (
              <p className="flex flex-1 items-center justify-center gap-2 rounded-lg border border-border-soft px-6 py-3 text-sm text-text-muted">
                <Lock size={16} /> Proprietary work — no public source or live demo
              </p>
            )}
          </div>
        </div>
      )}
    </Modal>
  );
}

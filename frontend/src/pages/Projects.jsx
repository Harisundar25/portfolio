import { useCallback, useState } from "react";
import { motion } from "framer-motion";
import { Link } from "react-router";
import { ArrowRight } from "lucide-react";
import HeroAtmosphere from "../components/HeroAtmosphere";
import SectionHeading from "../components/ui/SectionHeading";
import IconTile from "../components/ui/IconTile";
import ProjectCard from "../components/projects/ProjectCard";
import ProjectDetailModal from "../components/projects/ProjectDetailModal";
import TranscriptDemo from "../components/projects/demo/TranscriptDemo";
import { projects, projectTech } from "../data/projects";

export default function Projects() {
  // Keep the last project mounted while the modal plays its exit animation.
  const [selected, setSelected] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDemoOpen, setIsDemoOpen] = useState(false);
  const openProject = useCallback((project) => {
    setSelected(project);
    setIsModalOpen(true);
  }, []);
  const closeModal = useCallback(() => setIsModalOpen(false), []);
  const openDemo = useCallback(() => {
    setIsModalOpen(false);
    setIsDemoOpen(true);
  }, []);
  const closeDemo = useCallback(() => setIsDemoOpen(false), []);

  return (
    <>
      <title>Projects – Hari Sundar</title>

      {/* Page header */}
      <section className="relative isolate overflow-hidden px-6 pt-36 pb-10 md:pt-44">
        <HeroAtmosphere />
        <SectionHeading
          as="h1"
          eyebrow="Projects"
          title={
            <>
              Selected <span className="text-gradient-brand">Works</span>
            </>
          }
          description="Two builds in C# / .NET — a personal learning project, and my professional work at Granite River Labs."
        />
      </section>

      {/* Tech used across the projects — mirrors the reference "Skills" grid */}
      <section aria-labelledby="built-with" className="relative z-10 px-6 pt-10 pb-20">
        <motion.p
          id="built-with"
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true }}
          className="mb-8 text-center font-mono text-xs tracking-[0.3em] text-text-muted uppercase"
        >
          Built with
        </motion.p>
        <ul className="mx-auto flex max-w-5xl flex-wrap justify-center gap-3 md:gap-5">
          {projectTech.map((tool, index) => (
            <IconTile key={tool.name} tool={tool} index={index} />
          ))}
        </ul>
      </section>

      {/* Project cards — mirrors the reference "Selected Works" grid */}
      <section id="work" className="relative z-10 px-6 pb-28 md:px-12">
        <div className="mx-auto grid max-w-6xl grid-cols-1 gap-8 lg:grid-cols-2">
          {projects.map((project, index) => (
            <ProjectCard key={project.id} project={project} index={index} onSelect={openProject} onDemo={openDemo} />
          ))}
        </div>
      </section>

      {/* Closing call to action */}
      <section className="relative z-10 px-6 pb-32">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          className="mx-auto max-w-3xl rounded-3xl border border-border-soft bg-gradient-to-r from-background-surface to-background-secondary p-10 text-center"
        >
          <h2 className="text-3xl font-bold md:text-4xl">Want to go deeper?</h2>
          <p className="mx-auto mt-3 max-w-xl text-text-muted">
            Happy to walk through the design decisions behind either project — or talk about what you're building.
          </p>
          <Link
            to="/#contact"
            className="mt-8 inline-flex items-center gap-2 rounded-full border border-accent-primary px-7 py-3 font-bold text-accent-primary transition-all hover:bg-accent-primary hover:text-background-primary hover:shadow-[0_0_20px_rgba(0,212,255,0.3)]"
          >
            Get in touch <ArrowRight size={18} />
          </Link>
        </motion.div>
      </section>

      <ProjectDetailModal project={selected} isOpen={isModalOpen} onClose={closeModal} onDemo={openDemo} />
      <TranscriptDemo isOpen={isDemoOpen} onClose={closeDemo} />
    </>
  );
}

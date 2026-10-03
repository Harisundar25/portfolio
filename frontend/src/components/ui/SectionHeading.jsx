import { motion } from "framer-motion";

export default function SectionHeading({ eyebrow, title, description, align = "center", as = "h2" }) {
  const Heading = as;
  const alignment = align === "left" ? "text-left" : "text-center mx-auto";

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.6 }}
      className={`max-w-3xl ${alignment}`}
    >
      {eyebrow && (
        <span className="mb-6 inline-block rounded-full border border-accent-primary/20 bg-accent-primary/10 px-3 py-1 text-sm font-medium text-accent-primary">
          {eyebrow}
        </span>
      )}
      <Heading className="mb-4 text-4xl font-bold tracking-tight md:text-6xl">{title}</Heading>
      {description && (
        <p className={`text-lg text-text-muted ${align === "left" ? "" : "mx-auto"} max-w-2xl`}>
          {description}
        </p>
      )}
    </motion.div>
  );
}

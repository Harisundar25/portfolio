import IconTile from "../../ui/IconTile";
import { exploring, featuredTools } from "../../../data/skills";

export default function IconCloud({ className = "" }) {
  return (
    <div className={className}>
      <ul className="mx-auto flex max-w-5xl flex-wrap justify-center gap-3 md:gap-5">
        {featuredTools.map((tool, index) => (
          <IconTile key={tool.name} tool={tool} index={index} />
        ))}
      </ul>
      <div className="mt-10 flex flex-wrap items-center justify-center gap-3 text-sm text-text-muted">
        <span className="font-mono text-[11px] tracking-[0.25em] uppercase">Also explored in personal projects</span>
        <ul className="flex gap-1">
          {exploring.map((tool, index) => (
            <IconTile key={tool.name} tool={tool} index={index} size="sm" />
          ))}
        </ul>
      </div>
    </div>
  );
}

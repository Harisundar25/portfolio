import { allSkills } from "../../../data/skills";

const half = Math.ceil(allSkills.length / 2);
const ROWS = [
  { items: allSkills.slice(0, half), reverse: false },
  { items: allSkills.slice(half), reverse: true },
];

function MarqueeRow({ items, reverse }) {
  // The list is rendered twice; translating by -50% loops it seamlessly.
  return (
    <div className="group flex overflow-hidden">
      <div
        className={`flex w-max shrink-0 gap-4 pr-4 ${
          reverse ? "animate-marquee-reverse" : "animate-marquee"
        } group-hover:[animation-play-state:paused]`}
      >
        {[...items, ...items].map((skill, i) => (
          <span
            key={`${skill}-${i}`}
            className="rounded-full border border-border-soft bg-background-secondary/70 px-5 py-2.5 font-heading text-lg font-medium whitespace-nowrap text-text-body md:text-xl"
          >
            <span className="mr-3 text-accent-primary">✦</span>
            {skill}
          </span>
        ))}
      </div>
    </div>
  );
}

export default function SkillMarquee() {
  return (
    <div aria-hidden="true" className="mask-fade-x flex flex-col gap-4">
      {ROWS.map((row) => (
        <MarqueeRow key={row.reverse ? "reverse" : "forward"} {...row} />
      ))}
    </div>
  );
}

import SectionHeading from "../ui/SectionHeading";
import IconCloud from "./skills/IconCloud";
import SkillExplorer from "./skills/SkillExplorer";
import PacketDecoder from "./skills/PacketDecoder";
import SkillMarquee from "./skills/SkillMarquee";

export default function Skills() {
  return (
    <section id="skills" className="relative z-10 overflow-hidden py-28 md:py-40">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute top-1/3 left-[-10%] h-[520px] w-[520px] rounded-full bg-[radial-gradient(circle,rgba(0,212,255,0.10)_0%,rgba(0,0,0,0)_70%)] blur-[80px]"
      />

      <div className="mx-auto max-w-6xl px-6">
        <SectionHeading
          eyebrow="Skills"
          title={
            <>
              Engineering reliable systems in <span className="text-gradient-brand">C# and .NET</span>.
            </>
          }
          description="From ASP.NET Core APIs and SQL Server to binary protocol decoders — the tools I use to build reliable backend software in C#."
        />

        <IconCloud className="mt-16" />

        <div className="mt-20 grid gap-6 lg:grid-cols-[1.35fr_1fr]">
          <SkillExplorer />
          <PacketDecoder />
        </div>
      </div>

      <div className="mt-20">
        <SkillMarquee />
      </div>
    </section>
  );
}

import { Boxes, Bug, Cpu, Database, Server } from "lucide-react";
import { BsFiletypeCsv } from "react-icons/bs";
import { FaGitAlt, FaGithub } from "react-icons/fa";
import { MdOutlineWifiTethering } from "react-icons/md";
import {
  SiDotnet,
  SiGithubactions,
  SiJira,
  SiJson,
  SiPostman,
  SiPython,
  SiReact,
  SiXml,
} from "react-icons/si";
import { TbBrandCSharp, TbBrandVisualStudio, TbDatabase } from "react-icons/tb";

// Brand colours are lightened where needed so they stay legible on the navy background.
const ICONS = {
  "C# / .NET": { icon: TbBrandCSharp, color: "#B07FE0" },
  ".NET Core": { icon: SiDotnet, color: "#8C6CF2" },
  ".NET Framework": { icon: SiDotnet, color: "#8C6CF2" },
  "ASP.NET Core": { icon: SiDotnet, color: "#8C6CF2" },
  "SQL Server": { icon: TbDatabase, color: "#FF6B5E" },
  Git: { icon: FaGitAlt, color: "#F05032" },
  "GitHub Actions": { icon: SiGithubactions, color: "#4DA3FF" },
  "Visual Studio": { icon: TbBrandVisualStudio, color: "#B48CFF" },
  JIRA: { icon: SiJira, color: "#4C9AFF" },
  Postman: { icon: SiPostman, color: "#FF6C37" },
  JSON: { icon: SiJson, color: "#F8FAFF" },
  "JSON Schema": { icon: SiJson, color: "#F8FAFF" },
  XML: { icon: SiXml, color: "#FFB74D" },
  CSV: { icon: BsFiletypeCsv, color: "#7EE787" },
  "WPC Qi": { icon: MdOutlineWifiTethering, color: "#00D4FF" },
  Qi2: { icon: MdOutlineWifiTethering, color: "#00D4FF" },
};

export const skillIcon = (name) => ICONS[name] ?? null;

// Grouped exactly as in the résumé's "Technical Skills" section.
export const skillCategories = [
  {
    id: "backend",
    label: ".NET & Backend",
    icon: Server,
    blurb: "Backend services and APIs in modern C#.",
    skills: [
      "C# / .NET",
      ".NET Core",
      ".NET Framework",
      "ASP.NET Core",
      "Web API",
      "REST APIs",
      "OOP",
      "SOLID Principles",
      "Design Patterns",
      "LINQ",
      "async/await",
      "Dependency Injection",
    ],
  },
  {
    id: "databases",
    label: "Databases",
    icon: Database,
    blurb: "Relational data with SQL Server.",
    skills: ["SQL", "SQL Server"],
  },
  {
    id: "testing",
    label: "Testing & Tools",
    icon: Bug,
    blurb: "Shipping releases that hold up in certification labs.",
    skills: [
      "Unit Testing",
      "Regression & Integration Testing",
      "Test Automation",
      "Debugging",
      "Troubleshooting",
      "Git",
      "GitHub Actions",
      "CI/CD",
      "Visual Studio",
      "JIRA",
      "Postman",
    ],
  },
  {
    id: "device",
    label: "Device & Protocol",
    icon: Cpu,
    blurb: "Where wireless-power hardware meets software.",
    skills: [
      "Device Software",
      "Protocol Development",
      "WPC Qi",
      "Qi2",
      "MPP-TPT / MPP-TPR",
      "Binary Packet Decoding & Encoding",
      "Firmware Integration",
      "Hardware–Software Integration",
    ],
  },
  {
    id: "practices",
    label: "Data & Practices",
    icon: Boxes,
    blurb: "Data formats and team practices.",
    skills: ["JSON", "JSON Schema", "XML", "CSV", "Agile", "Scrum", "Code Reviews"],
  },
];

// Highlighted in the icon cloud at the top of the Skills section.
export const featuredTools = [
  { name: "C#", icon: TbBrandCSharp, color: "#B07FE0" },
  { name: ".NET", icon: SiDotnet, color: "#8C6CF2" },
  { name: "ASP.NET Core", icon: SiDotnet, color: "#A48BFF" },
  { name: "SQL Server", icon: TbDatabase, color: "#FF6B5E" },
  { name: "WPC Qi / Qi2", icon: MdOutlineWifiTethering, color: "#00D4FF" },
  { name: "Git", icon: FaGitAlt, color: "#F05032" },
  { name: "GitHub", icon: FaGithub, color: "#F8FAFF" },
  { name: "GitHub Actions", icon: SiGithubactions, color: "#4DA3FF" },
  { name: "Visual Studio", icon: TbBrandVisualStudio, color: "#B48CFF" },
  { name: "Postman", icon: SiPostman, color: "#FF6C37" },
  { name: "JIRA", icon: SiJira, color: "#4C9AFF" },
  { name: "JSON", icon: SiJson, color: "#F8FAFF" },
  { name: "XML", icon: SiXml, color: "#FFB74D" },
];

// Explored through personal projects (not part of the résumé's core skill list).
export const exploring = [
  { name: "Python", icon: SiPython, color: "#FFD43B" },
  { name: "React", icon: SiReact, color: "#61DAFB" },
];

export const allSkills = skillCategories.flatMap((c) => c.skills);

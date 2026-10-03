// Profile content sourced from Hari Sundar's résumé (Modern_CV_Template).
// Edit this file to update the About / Experience / Credentials sections.

export const profile = {
  name: "Hari Sundar",
  firstName: "Hari",
  role: "Software Engineer",
  tagline: "C# / .NET · ASP.NET Core · SQL Server · REST APIs",
  location: "Bengaluru, Karnataka, India",
  locationShort: "Bengaluru, India",
  email: "harisivakumar25@gmail.com",
  links: {
    github: "https://github.com/HariSundar-GRL",
    linkedin: "https://www.linkedin.com/in/hari-sundar-46548021a/",
    repo: "https://github.com/Harisundar25/portfolio",
  },
};

// Scroll-revealed statement at the top of the About section.
// Segments flagged with `hl` are rendered with the brand gradient.
export const aboutStatement = [
  { text: "I'm Hari — a software engineer who builds" },
  { text: "C#/.NET backend software", hl: true },
  { text: "for" },
  { text: "WPC Qi wireless power", hl: true },
  { text: "certification systems. I enjoy the space where" },
  { text: "hardware meets software:", hl: true },
  { text: "decoding binary protocol packets, turning specification clauses into" },
  { text: "test cases,", hl: true },
  { text: "and chasing down the bugs that live in between." },
];

export const aboutParagraphs = [
  "Since my internship in 2023 I've been at Granite River Labs in Bengaluru, working on automated compliance test systems for Qi wireless charging — software used to validate wireless chargers and devices against the Wireless Power Consortium (WPC) specification.",
  "Day to day that means ASP.NET Core and REST APIs, object-oriented modules that decode and encode binary packets from JSON API definitions, specification-based test cases for MPP-TPT / MPP-TPR systems, and a lot of packet-log and application-log analysis.",
  "Outside work I learn by building — most recently porting my Python transcript tool to C# and wiring it to open-source AI models.",
];

export const quickFacts = [
  { label: "Based in", value: "Bengaluru, India" },
  { label: "Education", value: "B.E. Electronics & Communication (81%)" },
  { label: "Languages", value: "English · Tamil · Kannada · Japanese (basic)" },
];

export const stats = [
  { value: 2, suffix: "+", label: "Years building C#/.NET software" },
  { value: 4, label: "Qi specification versions", note: "v2.0 – v2.3" },
  { value: 60, suffix: "%", label: "Fewer production defects", note: "contributed via pre-release validation" },
  { value: 60, suffix: "+", label: "Showstopper defects resolved" },
];

export const experience = [
  {
    role: "Software Engineer",
    company: "Granite River Labs Technology Pvt. Ltd.",
    location: "Bengaluru",
    period: "Jun 2024 – Present",
    current: true,
    points: [
      "Design and develop C#/.NET backend software for automated WPC Qi wireless power compliance test systems, delivering production releases (SW 3.220.x / 3.230.x) across Qi specification versions v2.0 – v2.3.",
      "Build object-oriented modules for binary protocol packet decoding and encoding driven by JSON API definitions, with field-level parsing, validation and diagnostic logging.",
      "Develop ASP.NET / REST API components linking backend services to the web-based tester application, and implement specification-based test cases for MPP-TPT / MPP-TPR systems.",
      "Derive test cases from functional requirements and run structured pre-release validation — contributing to a 60% reduction in production defects.",
      "Resolved 60+ showstopper defects through packet-log and application-log analysis and debugging.",
      "Supported the implementation of GitHub runners for the team's CI/CD pipeline; practise unit testing and Git code reviews.",
    ],
    tags: ["C#", ".NET", "ASP.NET Core", "REST APIs", "JSON", "GitHub Actions"],
  },
  {
    role: "Software Engineer Intern",
    company: "Granite River Labs Technology Pvt. Ltd.",
    location: "Bengaluru",
    period: "Sep 2023 – May 2024",
    points: [
      "Implemented and validated compliance test cases for the C3-TPT web application and fixed defects for WPC certification.",
    ],
    tags: ["C#", "Compliance testing", "WPC Qi"],
  },
  {
    role: "B.E. Electronics & Communication Engineering",
    company: "Knowledge Institute of Technology",
    location: "Salem, Tamil Nadu",
    period: "2020 – 2024",
    education: true,
    points: ["Graduated with 81%."],
    tags: [],
  },
];

export const achievements = [
  {
    title: "Certificate of Appreciation",
    date: "Feb 2024",
    description: "For delivery of the GRL-C3-MP-TPT wireless power test solution.",
  },
  {
    title: "Cross-Functional Impact Recognition",
    date: "Dec 2025",
    description: "From the FPGA and Hardware teams for debugging support.",
  },
];

export const certifications = [
  {
    title: "Foundational C# with Microsoft",
    issuer: "Microsoft · freeCodeCamp",
    url: "https://www.freecodecamp.org/certification/Hari_Sundar/foundational-c-sharp-with-microsoft",
  },
  {
    title: "Develop Generative AI Applications: Get Started",
    issuer: "IBM",
    date: "May 2026",
    url: "https://www.coursera.org/account/accomplishments/verify/FHK7LY0FEX7H",
  },
  {
    title: "Generative AI: Introduction and Applications",
    issuer: "IBM",
    date: "Jan 2026",
    url: "https://www.coursera.org/account/accomplishments/verify/NYWSFEQF8DKI",
  },
  {
    title: "Claude Code 101",
    issuer: "Anthropic Education",
    date: "May 2026",
    url: "https://verify.skilljar.com/c/qcrao5d96pz3",
  },
];

export const languages = [
  { name: "English", level: "Professional working" },
  { name: "Tamil", level: "Native" },
  { name: "Kannada", level: "Conversational" },
  { name: "Japanese", level: "Basic" },
];

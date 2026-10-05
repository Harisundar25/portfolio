import { AudioLines } from "lucide-react";
import { SiDotnet, SiFfmpeg, SiGithubactions, SiHuggingface, SiJson, SiReact, SiYoutube } from "react-icons/si";
import { TbBrandAzure, TbBrandCSharp, TbTestPipe } from "react-icons/tb";
import { isDemoConnected } from "../lib/transcriptApi";
import { profile } from "./profile";

// "Built with" icon grid on the Projects page (mirrors the reference site's Skills grid).
export const projectTech = [
  { name: "C#", icon: TbBrandCSharp, color: "#B07FE0" },
  { name: ".NET 10", icon: SiDotnet, color: "#8C6CF2" },
  { name: "ASP.NET Core", icon: SiDotnet, color: "#A48BFF" },
  { name: "Groq · Whisper", icon: AudioLines, color: "#F55036" },
  { name: "Hugging Face", icon: SiHuggingface, color: "#FFD21E" },
  { name: "YouTube", icon: SiYoutube, color: "#FF3B3B" },
  { name: "FFmpeg", icon: SiFfmpeg, color: "#5CB85C" },
  { name: "xUnit", icon: TbTestPipe, color: "#7EE787" },
  { name: "JSON", icon: SiJson, color: "#F8FAFF" },
  { name: "GitHub Actions", icon: SiGithubactions, color: "#4DA3FF" },
  { name: "Azure", icon: TbBrandAzure, color: "#3AA0F3" },
  { name: "React", icon: SiReact, color: "#61DAFB" },
];

export const projects = [
  {
    id: "transcript-ai",
    title: "AI Transcript Generator",
    subtitle: "C# · .NET 10 · Groq Whisper API",
    badge: { label: "Personal learning project", tone: "primary" },
    year: "2025 – 2026",
    role: "Solo developer · personal learning",
    visual: "waveform",
    description:
      "A C# / .NET 10 application that turns local video files, audio files or YouTube URLs into timestamped transcripts. I undertook this voice transcript tool for personal learning — porting my original Python Transcript-AI script to C# and replacing the local Whisper model with open-source Whisper models served through Groq's free API.",
    tech: [
      "C#",
      ".NET 10",
      "ASP.NET Core Minimal API",
      "Groq Whisper API",
      "Hugging Face Inference",
      "YoutubeExplode",
      "FFmpeg",
      "xUnit",
    ],
    details: {
      challenge:
        "The original Python version ran OpenAI's Whisper model locally: it needed a heavy model download, was slow on a laptop CPU, hard-coded an FFmpeg path and only accepted YouTube links.",
      solution:
        "I re-implemented the pipeline in C# around small interfaces — media source → audio preparation → transcription → formatting — and moved inference to open-source Whisper models on Groq's free tier. FFmpeg became optional: when present it downsamples and chunks long recordings; when absent, YouTube audio streams and small files go straight to the API.",
    },
    featuresTitle: "Key features",
    features: [
      "Accepts local audio/video files or YouTube URLs (YoutubeExplode — no Python or yt-dlp required)",
      "Inference on open-source Whisper models (whisper-large-v3-turbo / whisper-large-v3) through Groq's free API, with Hugging Face Inference as an alternative provider",
      "Same timestamped output as the Python original — [start --> end] text — plus SRT, WebVTT and JSON",
      "Optional translation to English and a manual source-language override",
      "FFmpeg downsampling (16 kHz mono) and automatic chunking to fit the API's 25 MB upload limit",
      "Retries with back-off on HTTP 429 rate limits, honouring the Retry-After header",
      "One core library shared by a CLI and an ASP.NET Core minimal API, covered by 150+ xUnit tests",
      "Public demo API hardened with per-visitor rate limits, a daily quota, CORS and RFC 9457 problem details",
    ],
    notesTitle: "Good to know",
    notes: [
      "Undertaken for personal learning — not a commercial product.",
      "The Groq API key stays on the server and is never sent to the browser.",
    ],
    links: {
      source: `${profile.links.repo}/tree/main/backend`,
      // The "Live demo" button appears once the API URL is configured (always in development).
      demo: isDemoConnected,
    },
  },
  {
    id: "grl-c3-mpp",
    title: "GRL C3 MPP Application",
    subtitle: "C# · .NET · Qi v2.2 / v2.3 compliance",
    badge: { label: "Professional work · Granite River Labs", tone: "secondary" },
    year: "2024 – Present",
    role: "Software Engineer · team contributor",
    visual: "coil",
    description:
      "Professional work at Granite River Labs on the GRL C3 test application for Qi Magnetic Power Profile (MPP-TPT / MPP-TPR) compliance testing. My focus is software design refactoring and keeping the C#/.NET software compliant with Qi specification versions 2.2 and 2.3.",
    tech: [
      "C#",
      ".NET",
      "ASP.NET",
      "REST APIs",
      "JSON / JSON Schema",
      "Unit Testing",
      "Git",
      "GitHub Actions",
    ],
    details: {
      challenge:
        "Each Qi specification release (v2.2, then v2.3) adds and changes compliance requirements. In a large, evolving test application that meant new test cases and protocol behaviour — without duplicating logic across the TPT and TPR test flows, and without breaking already-certified behaviour.",
      solution:
        "Refactored toward shared, metadata-driven components and applied OOP / SOLID design so the TPT and TPR flows reuse the same building blocks. New specification requirements were implemented as specification-based test cases and verified through structured pre-release validation before each production release.",
    },
    featuresTitle: "My contributions",
    features: [
      "Software design refactoring — consolidated duplicated TPT/TPR logic behind a unified metadata schema, applying OOP and SOLID principles",
      "Qi specification v2.2 and v2.3 compliance — implemented and updated specification-based test cases for MPP-TPT / MPP-TPR systems",
      "Contributed to production releases SW 3.220.x and 3.230.x",
      "Binary protocol packet decoding/encoding modules driven by JSON API definitions, with field-level validation and diagnostic logging",
      "ASP.NET / REST API components linking backend services to the web-based tester application",
      "Supported the implementation of the GitHub runner used in the team's CI/CD pipeline",
      "Resolved showstopper defects through packet-log and application-log analysis",
    ],
    notesTitle: "Scope & clarifications",
    notes: [
      "I supported the implementation of the GitHub runner as part of the team — I did not implement it independently.",
      "I was not the developer of the Eye Diagram or Exerciser Mode features.",
      "Proprietary software: source code and internal screenshots can't be shared publicly.",
    ],
    links: {},
  },
];

import { Mail } from "lucide-react";
import { FaGithub, FaLinkedinIn } from "react-icons/fa";
import { profile } from "../../data/profile";

const YEAR = new Date().getFullYear();

const socialLinks = [
  { icon: FaGithub, url: profile.links.github, label: "GitHub" },
  { icon: FaLinkedinIn, url: profile.links.linkedin, label: "LinkedIn" },
  { icon: Mail, url: `mailto:${profile.email}`, label: "Email" },
];

export default function Footer() {
  return (
    <footer className="relative z-10 border-t border-border-soft bg-background-primary pt-10 pb-[calc(110px+env(safe-area-inset-bottom))] text-center md:pb-12">
      <div className="flex justify-center gap-6">
        {socialLinks.map(({ icon: Icon, url, label }) => (
          <a
            key={label}
            href={url}
            target={url.startsWith("mailto:") ? undefined : "_blank"}
            rel="noopener noreferrer"
            className="text-text-muted transition-colors hover:text-accent-primary"
            aria-label={label}
          >
            <Icon size={20} />
          </a>
        ))}
      </div>
      <p className="mt-5 text-sm text-text-muted">
        © {YEAR} {profile.name}. Built with React, Tailwind CSS &amp; ASP.NET Core.
      </p>
      <p className="mt-2 text-xs text-text-disabled">
        Design adapted from{" "}
        <a
          href="https://github.com/JeevaVenkidu/awwwards-style-portfolio-2026"
          target="_blank"
          rel="noopener noreferrer"
          className="underline decoration-dotted underline-offset-4 transition-colors hover:text-text-muted"
        >
          awwwards-style-portfolio-2026
        </a>
      </p>
    </footer>
  );
}

import { Link } from "react-router";
import { ArrowLeft } from "lucide-react";
import HeroAtmosphere from "../components/HeroAtmosphere";

export default function NotFound() {
  return (
    <section className="relative isolate flex min-h-[85vh] flex-col items-center justify-center overflow-hidden px-6 text-center">
      <title>Page not found – Hari Sundar</title>
      <HeroAtmosphere />
      <p className="font-mono text-sm tracking-widest text-accent-secondary uppercase">404 · End Power Transfer</p>
      <h1 className="mt-4 text-5xl font-bold tracking-tight md:text-7xl">Signal lost.</h1>
      <p className="mt-4 max-w-md text-text-muted">The page you're looking for doesn't exist or has moved.</p>
      <Link
        to="/"
        className="mt-10 inline-flex items-center gap-2 rounded-full border border-accent-primary px-6 py-3 font-semibold text-accent-primary transition-colors hover:bg-accent-primary hover:text-background-primary"
      >
        <ArrowLeft size={18} /> Back home
      </Link>
    </section>
  );
}

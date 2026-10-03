import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import { useLocation, useNavigate } from "react-router";
import { Briefcase, Code, House, Mail, User } from "lucide-react";
import { useSmoothScroll } from "../../context/SmoothScrollContext";
import { scrollToTarget } from "../../lib/scroll";

const NAV_ITEMS = [
  { label: "Home", icon: House, section: "home", href: "/" },
  { label: "About", icon: User, section: "about", href: "/#about" },
  { label: "Skills", icon: Code, section: "skills", href: "/#skills" },
  { label: "Projects", icon: Briefcase, path: "/projects", href: "/projects" },
  { label: "Contact", icon: Mail, section: "contact", href: "/#contact" },
];

const SECTION_IDS = NAV_ITEMS.filter((item) => item.section).map((item) => item.section);

/** Scroll-spy: the active section is the last one whose top passed 35% of the viewport. */
function useActiveSection(enabled) {
  const [active, setActive] = useState(SECTION_IDS[0]);

  useEffect(() => {
    if (!enabled) return undefined;
    let frame = 0;

    const update = () => {
      frame = 0;
      const probe = window.innerHeight * 0.35;
      let current = SECTION_IDS[0];
      for (const id of SECTION_IDS) {
        const el = document.getElementById(id);
        if (el && el.getBoundingClientRect().top <= probe) current = id;
      }
      const atBottom =
        window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 4;
      setActive(atBottom ? SECTION_IDS[SECTION_IDS.length - 1] : current);
    };
    const schedule = () => {
      if (!frame) frame = requestAnimationFrame(update);
    };

    schedule();
    window.addEventListener("scroll", schedule, { passive: true });
    window.addEventListener("resize", schedule);
    return () => {
      window.removeEventListener("scroll", schedule);
      window.removeEventListener("resize", schedule);
      if (frame) cancelAnimationFrame(frame);
    };
  }, [enabled]);

  return active;
}

export default function Navbar() {
  const { lenis } = useSmoothScroll();
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const onHome = pathname === "/";
  const activeSection = useActiveSection(onHome);

  const isActive = (item) => (onHome ? item.section === activeSection : item.path === pathname);

  const handleClick = (e, item) => {
    if (e.metaKey || e.ctrlKey || e.shiftKey || e.button === 1) return; // let the browser open new tabs
    e.preventDefault();
    if (item.path) {
      navigate(item.path);
    } else if (onHome) {
      scrollToTarget(lenis, item.section);
    } else {
      navigate(item.href);
    }
  };

  return (
    <motion.nav
      aria-label="Primary"
      initial={{ y: -100, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      transition={{ duration: 0.8, delay: 0.6, type: "spring" }}
      className="fixed right-0 bottom-[calc(1.25rem+env(safe-area-inset-bottom))] left-0 z-50 mx-auto flex w-fit items-center gap-1 rounded-full border border-border-default bg-background-glass px-2 py-2 shadow-strong backdrop-blur-[18px] md:top-6 md:bottom-auto md:gap-2"
    >
      {NAV_ITEMS.map((item) => {
        const active = isActive(item);
        const Icon = item.icon;
        return (
          <a
            key={item.label}
            href={item.href}
            onClick={(e) => handleClick(e, item)}
            aria-label={item.label}
            aria-current={active ? "page" : undefined}
            className="relative z-10 flex items-center justify-center rounded-full px-4 py-3 text-sm font-medium transition-all duration-300 md:px-5 md:py-2"
          >
            {active && (
              <motion.span
                layoutId="active-tab"
                className="absolute inset-0 rounded-full border border-accent-primary/20 bg-accent-primary/10 shadow-[0_0_15px_rgba(0,212,255,0.25)] backdrop-blur-sm"
                transition={{ type: "spring", stiffness: 300, damping: 30 }}
              />
            )}
            <motion.span
              animate={{ scale: active ? 1.15 : 1, opacity: active ? 1 : 0.65 }}
              className="relative z-20 md:hidden"
            >
              <Icon
                className={`h-5 w-5 ${active ? "text-accent-primary drop-shadow-[0_0_8px_#00d4ff]" : "text-text-muted"}`}
              />
            </motion.span>
            <span
              className={`relative z-20 hidden transition-colors duration-300 md:block ${
                active ? "text-accent-primary" : "text-text-muted hover:text-text-body"
              }`}
            >
              {item.label}
            </span>
          </a>
        );
      })}
    </motion.nav>
  );
}

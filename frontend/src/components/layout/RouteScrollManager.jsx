import { useEffect } from "react";
import { useLocation } from "react-router";
import { useSmoothScroll } from "../../context/SmoothScrollContext";
import { scrollToTarget } from "../../lib/scroll";

/**
 * Resets scroll on route changes and honours "#section" hashes
 * (e.g. navigating from /projects to /#contact).
 */
export default function RouteScrollManager() {
  const { pathname, hash } = useLocation();
  const { lenis } = useSmoothScroll();

  useEffect(() => {
    if (!hash) {
      scrollToTarget(lenis, "top", { immediate: true });
      return undefined;
    }

    // The target section may not be mounted yet — retry briefly.
    let attempts = 0;
    let timer = 0;
    const attempt = () => {
      if (scrollToTarget(lenis, hash.slice(1)) || attempts++ > 20) return;
      timer = setTimeout(attempt, 50);
    };
    timer = setTimeout(attempt, 80);
    return () => clearTimeout(timer);
  }, [pathname, hash, lenis]);

  return null;
}

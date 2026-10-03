const easeOutExpo = (t) => Math.min(1, 1.001 - Math.pow(2, -10 * t));

/**
 * Scrolls to a section id (e.g. "about"), "home"/"top", or an element.
 * Uses Lenis when available, otherwise native scrolling.
 * Returns false when the target element does not exist (yet).
 */
export function scrollToTarget(lenis, target, { immediate = false, offset = -16 } = {}) {
  const isTop = target === "home" || target === "top" || target === 0;
  const element =
    typeof target === "string" && !isTop
      ? document.getElementById(target.replace(/^#/, ""))
      : target instanceof HTMLElement
        ? target
        : null;

  if (!isTop && !element) return false;

  if (lenis) {
    lenis.scrollTo(isTop ? 0 : element, {
      offset: isTop ? 0 : offset,
      immediate,
      duration: immediate ? 0 : 1.4,
      easing: easeOutExpo,
      force: true,
    });
  } else if (isTop) {
    window.scrollTo({ top: 0, behavior: immediate ? "auto" : "smooth" });
  } else {
    const top = element.getBoundingClientRect().top + window.scrollY + offset;
    window.scrollTo({ top, behavior: immediate ? "auto" : "smooth" });
  }
  return true;
}

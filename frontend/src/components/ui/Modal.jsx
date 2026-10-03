import { useEffect, useId } from "react";
import { createPortal } from "react-dom";
import { AnimatePresence, motion } from "framer-motion";
import { X } from "lucide-react";
import { useSmoothScroll } from "../../context/SmoothScrollContext";

export default function Modal({ isOpen, onClose, children, title, subtitle }) {
  const { lenis } = useSmoothScroll();
  const titleId = useId();

  // Freeze page scrolling (Lenis + native) while open; close on Escape.
  useEffect(() => {
    if (!isOpen) return undefined;
    lenis?.stop();
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    const onKey = (e) => e.key === "Escape" && onClose();
    window.addEventListener("keydown", onKey);
    return () => {
      lenis?.start();
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", onKey);
    };
  }, [isOpen, lenis, onClose]);

  return createPortal(
    <AnimatePresence>
      {isOpen && (
        <>
          <motion.div
            key="backdrop"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="fixed inset-0 z-[9998] bg-black/60 backdrop-blur-sm"
          />
          <div
            key="dialog"
            className="pointer-events-none fixed inset-0 z-[9999] flex items-center justify-center p-4 sm:p-6"
          >
            <motion.div
              role="dialog"
              aria-modal="true"
              aria-labelledby={titleId}
              initial={{ opacity: 0, scale: 0.95, y: 20 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.95, y: 20 }}
              transition={{ type: "spring", stiffness: 260, damping: 26 }}
              className="pointer-events-auto flex max-h-[90vh] w-full max-w-4xl flex-col overflow-hidden rounded-2xl border border-border-soft bg-background-secondary shadow-2xl"
            >
              <div className="flex items-start justify-between gap-4 border-b border-border-soft bg-background-primary p-6">
                <div>
                  <h3 id={titleId} className="text-xl font-bold md:text-2xl">
                    {title}
                  </h3>
                  {subtitle && <p className="mt-1 text-sm text-text-muted">{subtitle}</p>}
                </div>
                <button
                  type="button"
                  onClick={onClose}
                  aria-label="Close dialog"
                  className="rounded-full p-2 text-text-muted transition-colors hover:bg-background-surface hover:text-text-heading"
                >
                  <X size={22} />
                </button>
              </div>
              <div data-lenis-prevent className="flex-1 overflow-y-auto overscroll-contain p-6">
                {children}
              </div>
            </motion.div>
          </div>
        </>
      )}
    </AnimatePresence>,
    document.body,
  );
}

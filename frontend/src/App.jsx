import { useCallback, useEffect, useState } from "react";
import { AnimatePresence, MotionConfig } from "framer-motion";
import Lenis from "lenis";
import { Route, Routes } from "react-router";
import { Toaster } from "sonner";
import { SmoothScrollProvider, useSmoothScroll } from "./context/SmoothScrollContext";
import Loader from "./components/Loader";
import CustomCursor from "./components/CustomCursor";
import Navbar from "./components/layout/Navbar";
import Footer from "./components/layout/Footer";
import RouteScrollManager from "./components/layout/RouteScrollManager";
import Home from "./pages/Home";
import NotFound from "./pages/NotFound";

const prefersReducedMotion = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;

/** Creates the Lenis smooth-scroll instance (skipped for reduced-motion users). */
function useLenis() {
  const { setLenis } = useSmoothScroll();

  useEffect(() => {
    if (prefersReducedMotion()) return undefined;

    const lenis = new Lenis({
      duration: 1.2,
      easing: (t) => Math.min(1, 1.001 - Math.pow(2, -10 * t)),
      orientation: "vertical",
      gestureOrientation: "vertical",
      wheelMultiplier: 1,
      touchMultiplier: 2,
    });

    let frame = 0;
    const raf = (time) => {
      lenis.raf(time);
      frame = requestAnimationFrame(raf);
    };
    frame = requestAnimationFrame(raf);
    // Publish the instance on the next frame so consumers (navbar, modals) can drive it.
    const publish = requestAnimationFrame(() => setLenis(lenis));

    return () => {
      cancelAnimationFrame(frame);
      cancelAnimationFrame(publish);
      lenis.destroy();
      setLenis(null);
    };
  }, [setLenis]);
}

function Shell() {
  const [isLoading, setIsLoading] = useState(true);
  const handleLoaded = useCallback(() => setIsLoading(false), []);
  useLenis();

  return (
    <>
      <AnimatePresence mode="wait">
        {isLoading && <Loader key="loader" onComplete={handleLoaded} />}
      </AnimatePresence>

      {!isLoading && (
        <>
          <a
            href="#main"
            className="sr-only z-[10001] rounded-lg bg-accent-primary px-4 py-2 font-semibold text-background-primary focus:not-sr-only focus:fixed focus:top-4 focus:left-4"
          >
            Skip to content
          </a>
          <CustomCursor />
          <Navbar />
          <RouteScrollManager />
          <main id="main" className="relative bg-background-primary text-text-body">
            <Routes>
              <Route path="/" element={<Home />} />
              <Route path="*" element={<NotFound />} />
            </Routes>
          </main>
          <Footer />
        </>
      )}
    </>
  );
}

export default function App() {
  return (
    <MotionConfig reducedMotion="user">
      <SmoothScrollProvider>
        <Shell />
        <Toaster
          position="top-right"
          theme="dark"
          richColors
          toastOptions={{
            style: {
              background: "rgba(5, 5, 5, 0.85)",
              border: "1px solid rgba(255, 255, 255, 0.08)",
              backdropFilter: "blur(16px)",
              color: "#b8c4d9",
              fontFamily: "Inter, system-ui, sans-serif",
              borderRadius: "16px",
            },
          }}
        />
      </SmoothScrollProvider>
    </MotionConfig>
  );
}

import { useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { AlertCircle, Check, LoaderCircle, Mail, MapPin, Send } from "lucide-react";
import { FaGithub, FaLinkedinIn } from "react-icons/fa";
import { profile } from "../../data/profile";

const contactSchema = z.object({
  name: z.string().trim().min(2, "Name must be at least 2 characters"),
  email: z.email("Please enter a valid email address"),
  subject: z.string().trim().min(5, "Subject must be at least 5 characters"),
  message: z.string().trim().min(10, "Message must be at least 10 characters"),
  website: z.string().optional(), // honeypot — real visitors never see or fill it
});

const EMAILJS = {
  serviceId: import.meta.env.VITE_EMAILJS_SERVICE_ID,
  templateId: import.meta.env.VITE_EMAILJS_TEMPLATE_ID,
  publicKey: import.meta.env.VITE_EMAILJS_PUBLIC_KEY,
};
const emailJsConfigured = Boolean(EMAILJS.serviceId && EMAILJS.templateId && EMAILJS.publicKey);

const contactItems = [
  { icon: Mail, label: "Email", value: profile.email, href: `mailto:${profile.email}`, tone: "primary" },
  { icon: MapPin, label: "Location", value: profile.location, tone: "info" },
  { icon: FaLinkedinIn, label: "LinkedIn", value: "in/hari-sundar-46548021a", href: profile.links.linkedin, tone: "secondary" },
  { icon: FaGithub, label: "GitHub", value: "HariSundar-GRL", href: profile.links.github, tone: "primary" },
];

const toneClasses = {
  primary: "bg-accent-primary/10 text-accent-primary",
  secondary: "bg-accent-secondary/10 text-accent-secondary",
  info: "bg-status-info/10 text-status-info",
};

const inputClass =
  "w-full rounded-lg border border-border-default bg-background-surface px-4 py-3 text-text-heading transition-all placeholder:text-text-disabled focus:border-accent-primary focus:shadow-[0_0_15px_rgba(0,212,255,0.2)] focus:ring-1 focus:ring-accent-primary focus:outline-none disabled:cursor-not-allowed disabled:opacity-50";

export default function Contact() {
  const [status, setStatus] = useState("idle"); // idle | submitting | success | error
  const { register, handleSubmit, reset } = useForm({ resolver: zodResolver(contactSchema) });

  const resetStatusLater = () => setTimeout(() => setStatus("idle"), 3000);

  const onSubmit = async (data) => {
    if (status === "submitting") return;
    if (data.website) return; // bot filled the honeypot

    if (!emailJsConfigured) {
      const subject = encodeURIComponent(data.subject);
      const body = encodeURIComponent(`${data.message}\n\n— ${data.name} (${data.email})`);
      window.location.assign(`mailto:${profile.email}?subject=${subject}&body=${body}`);
      toast.info("Opening your email app…");
      return;
    }

    setStatus("submitting");
    try {
      const { default: emailjs } = await import("@emailjs/browser");
      await emailjs.send(
        EMAILJS.serviceId,
        EMAILJS.templateId,
        { from_name: data.name, from_email: data.email, subject: data.subject, message: data.message },
        { publicKey: EMAILJS.publicKey },
      );
      setStatus("success");
      toast.success("Message sent — thank you!");
      reset();
    } catch (error) {
      console.error("Email sending error:", error);
      setStatus("error");
      toast.error("Couldn't send your message. Please try again or email me directly.");
    } finally {
      resetStatusLater();
    }
  };

  const onInvalid = (errors) => {
    const first = Object.values(errors)[0];
    if (first?.message) toast.error(first.message);
  };

  const submitting = status === "submitting";

  return (
    <section id="contact" className="relative z-10 overflow-hidden py-28 md:py-36">
      <div aria-hidden="true" className="pointer-events-none absolute inset-0 overflow-hidden">
        <div className="absolute top-[-10%] right-[-5%] h-[500px] w-[500px] rounded-full bg-[radial-gradient(circle,rgba(0,212,255,0.15)_0%,rgba(0,0,0,0)_70%)] blur-[100px]" />
        <div className="absolute bottom-[-10%] left-[-5%] h-[500px] w-[500px] rounded-full bg-[radial-gradient(circle,rgba(255,106,0,0.15)_0%,rgba(0,0,0,0)_70%)] blur-[100px]" />
      </div>

      <div className="relative mx-auto max-w-6xl px-6">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          className="mb-16 text-center"
        >
          <h2 className="mb-6 text-4xl font-bold md:text-6xl">
            Get in <span className="text-gradient-brand">Touch</span>
          </h2>
          <p className="mx-auto max-w-2xl text-lg text-text-body">
            Have a role, a project, or a question about C# / .NET backend development? My inbox is open — I'll get
            back to you as soon as I can.
          </p>
        </motion.div>

        <div className="grid grid-cols-1 gap-12 lg:grid-cols-2 lg:gap-20">
          <motion.div
            initial={{ opacity: 0, x: -50 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true }}
            transition={{ delay: 0.2 }}
            className="space-y-8"
          >
            <div className="rounded-2xl border border-border-soft bg-background-secondary p-8 transition-colors duration-300 hover:border-accent-primary">
              <h3 className="mb-6 text-2xl font-bold">Contact Information</h3>
              <ul className="space-y-6">
                {contactItems.map(({ icon: Icon, label, value, href, tone }) => (
                  <li key={label} className="flex items-start gap-4">
                    <div className={`rounded-lg p-3 ${toneClasses[tone]}`}>
                      <Icon size={22} />
                    </div>
                    <div className="min-w-0">
                      <h4 className="mb-1 font-medium text-text-heading">{label}</h4>
                      {href ? (
                        <a
                          href={href}
                          target={href.startsWith("mailto:") ? undefined : "_blank"}
                          rel="noopener noreferrer"
                          className="break-all text-text-muted transition-colors hover:text-accent-primary"
                        >
                          {value}
                        </a>
                      ) : (
                        <span className="text-text-muted">{value}</span>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            </div>

            <div className="rounded-2xl border border-border-soft bg-gradient-to-r from-background-surface to-background-secondary p-8">
              <h3 className="mb-2 text-xl font-bold">Let's Connect</h3>
              <p className="text-text-muted">
                I'm always happy to talk about backend engineering, protocol work, test automation — or the projects on
                this site.
              </p>
            </div>
          </motion.div>

          <motion.div
            initial={{ opacity: 0, x: 50 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true }}
            transition={{ delay: 0.4 }}
          >
            <form
              noValidate
              onSubmit={handleSubmit(onSubmit, onInvalid)}
              className="space-y-6 rounded-2xl border border-border-soft bg-background-glass p-8 shadow-soft backdrop-blur-[18px] transition-all duration-300 hover:shadow-strong"
            >
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <div className="space-y-2">
                  <label htmlFor="name" className="text-sm font-medium text-text-body">
                    Name
                  </label>
                  <input id="name" autoComplete="name" {...register("name")} className={inputClass} placeholder="Jane Doe" disabled={submitting} />
                </div>
                <div className="space-y-2">
                  <label htmlFor="email" className="text-sm font-medium text-text-body">
                    Email
                  </label>
                  <input id="email" type="email" autoComplete="email" {...register("email")} className={inputClass} placeholder="jane@example.com" disabled={submitting} />
                </div>
              </div>

              <div className="space-y-2">
                <label htmlFor="subject" className="text-sm font-medium text-text-body">
                  Subject
                </label>
                <input id="subject" {...register("subject")} className={inputClass} placeholder="Opportunity / question" disabled={submitting} />
              </div>

              <div className="space-y-2">
                <label htmlFor="message" className="text-sm font-medium text-text-body">
                  Message
                </label>
                <textarea id="message" rows={5} {...register("message")} className={`${inputClass} resize-none`} placeholder="Tell me a little about it…" disabled={submitting} />
              </div>

              {/* Honeypot field, hidden from people and assistive tech */}
              <input type="text" tabIndex={-1} autoComplete="off" aria-hidden="true" className="hidden" {...register("website")} />

              <button
                type="submit"
                disabled={submitting}
                className={`flex w-full items-center justify-center gap-2 rounded-lg py-4 font-bold shadow-[0_0_25px_rgba(0,212,255,0.35)] transition-all hover:scale-[1.02] ${
                  status === "submitting"
                    ? "cursor-wait bg-background-surface text-text-muted opacity-80"
                    : status === "success"
                      ? "bg-status-success text-[#0a0a0a]"
                      : status === "error"
                        ? "bg-status-danger text-white"
                        : "bg-accent-primary text-background-primary hover:bg-accent-primary-hover"
                }`}
              >
                <AnimatePresence mode="wait" initial={false}>
                  <motion.span
                    key={status}
                    initial={{ opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -8 }}
                    className="flex items-center gap-2"
                  >
                    {status === "submitting" && (
                      <>
                        <LoaderCircle className="animate-spin" size={20} /> Sending…
                      </>
                    )}
                    {status === "success" && (
                      <>
                        <Check size={20} /> Sent
                      </>
                    )}
                    {status === "error" && (
                      <>
                        <AlertCircle size={20} /> Try again
                      </>
                    )}
                    {status === "idle" && (
                      <>
                        <Send size={20} /> {emailJsConfigured ? "Send Message" : "Send via email app"}
                      </>
                    )}
                  </motion.span>
                </AnimatePresence>
              </button>
            </form>
          </motion.div>
        </div>
      </div>
    </section>
  );
}

import { useEffect, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "framer-motion";
import { Cpu } from "lucide-react";

const hex = (byte) => byte.toString(16).toUpperCase().padStart(2, "0");
const toInt8 = (byte) => (byte > 127 ? byte - 256 : byte);

// Illustrative Qi packets: one header byte, one message byte and an XOR checksum.
const PACKETS = [
  {
    name: "Control Error",
    header: 0x03,
    message: [0xf6],
    field: "Control error value",
    value: (m) => `${toInt8(m[0])}`,
    note: "signed 8-bit",
  },
  {
    name: "Received Power (8-bit)",
    header: 0x04,
    message: [0x80],
    field: "Received power value",
    value: (m) => `${m[0]}`,
    note: "raw",
  },
  {
    name: "Signal Strength",
    header: 0x01,
    message: [0xc8],
    field: "Signal strength value",
    value: (m) => `${m[0]}`,
    note: "of 255",
  },
  {
    name: "End Power Transfer",
    header: 0x02,
    message: [0x01],
    field: "EPT code",
    value: (m) => `0x${hex(m[0])}`,
    note: "Charge Complete",
  },
];

const ROLE_STYLES = {
  Header: "border-accent-primary/50 bg-accent-primary/10 text-accent-primary",
  Message: "border-white/20 bg-white/5 text-text-heading",
  Checksum: "border-accent-secondary/50 bg-accent-secondary/10 text-accent-secondary",
};

export default function PacketDecoder() {
  const reduceMotion = useReducedMotion();
  const [index, setIndex] = useState(0);
  const [paused, setPaused] = useState(false);

  useEffect(() => {
    if (reduceMotion || paused) return undefined;
    const id = setInterval(() => setIndex((i) => (i + 1) % PACKETS.length), 3600);
    return () => clearInterval(id);
  }, [reduceMotion, paused]);

  const packet = PACKETS[index];
  const checksum = [packet.header, ...packet.message].reduce((acc, b) => acc ^ b, 0);
  const bytes = [
    { value: packet.header, role: "Header" },
    ...packet.message.map((value) => ({ value, role: "Message" })),
    { value: checksum, role: "Checksum" },
  ];

  return (
    <motion.div
      initial={{ opacity: 0, y: 30 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.6, delay: 0.1 }}
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      className="relative overflow-hidden rounded-3xl border border-border-soft bg-background-glass p-6 backdrop-blur-[18px] md:p-8"
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-20 -right-20 h-56 w-56 rounded-full bg-[radial-gradient(circle,rgba(255,106,0,0.18)_0%,rgba(0,0,0,0)_70%)]"
      />

      <div className="flex items-center gap-3">
        <span className="rounded-lg bg-accent-secondary/10 p-2 text-accent-secondary">
          <Cpu size={18} />
        </span>
        <div>
          <p className="font-mono text-[11px] tracking-[0.3em] text-accent-secondary uppercase">Live packet decode</p>
          <h3 className="text-xl font-bold">Qi packet → fields</h3>
        </div>
      </div>

      <div className="mt-6 min-h-[13.5rem]" aria-live="polite">
        <AnimatePresence mode="wait">
          <motion.div
            key={index}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.25 }}
          >
            <div className="flex gap-3 font-mono">
              {bytes.map((b, i) => (
                <motion.div
                  key={`${b.role}-${i}`}
                  initial={{ opacity: 0, y: 12 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: i * 0.14 }}
                  className="text-center"
                >
                  <div className={`rounded-lg border px-3.5 py-2 text-xl font-medium md:text-2xl ${ROLE_STYLES[b.role]}`}>
                    {hex(b.value)}
                  </div>
                  <div className="mt-2 text-[10px] tracking-widest text-text-muted uppercase">{b.role}</div>
                </motion.div>
              ))}
            </div>

            <motion.dl
              initial={{ opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.45 }}
              className="mt-6 space-y-2.5 font-mono text-[13px]"
            >
              <div className="flex justify-between gap-4 border-b border-border-soft pb-2">
                <dt className="text-text-muted">Packet</dt>
                <dd className="text-right text-text-heading">
                  {packet.name} <span className="text-accent-primary">(0x{hex(packet.header)})</span>
                </dd>
              </div>
              <div className="flex justify-between gap-4 border-b border-border-soft pb-2">
                <dt className="text-text-muted">{packet.field}</dt>
                <dd className="text-right text-text-heading">
                  {packet.value(packet.message)} <span className="text-text-muted">· {packet.note}</span>
                </dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-text-muted">Checksum</dt>
                <dd className="text-right text-text-heading">
                  0x{hex(packet.header)} ⊕ 0x{hex(packet.message[0])} = 0x{hex(checksum)}{" "}
                  <span className="text-status-success">✓</span>
                </dd>
              </div>
            </motion.dl>
          </motion.div>
        </AnimatePresence>
      </div>

      <div className="mt-2 flex items-center justify-between gap-4">
        <div className="flex gap-1.5" role="group" aria-label="Choose example packet">
          {PACKETS.map((p, i) => (
            <button
              key={p.name}
              type="button"
              onClick={() => setIndex(i)}
              aria-label={`Show ${p.name} packet`}
              aria-pressed={i === index}
              className={`h-1.5 rounded-full transition-all ${
                i === index ? "w-6 bg-accent-secondary" : "w-1.5 bg-text-disabled hover:bg-text-muted"
              }`}
            />
          ))}
        </div>
        <span className="font-mono text-[10px] tracking-widest text-text-disabled uppercase">
          {paused ? "paused" : "auto"}
        </span>
      </div>

      <p className="mt-5 text-xs leading-relaxed text-text-muted">
        Qi packets carry a header byte, message bytes and an XOR checksum. Parsing and validating binary data like
        this, field by field, is work I've done a lot of in C#.
      </p>
    </motion.div>
  );
}

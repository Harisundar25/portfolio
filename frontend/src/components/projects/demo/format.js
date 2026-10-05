const pad = (n) => String(n).padStart(2, "0");

/** 75 → "1:15", 3725 → "1:02:05". */
export function formatClock(totalSeconds) {
  const s = Math.max(0, Math.floor(totalSeconds ?? 0));
  const hours = Math.floor(s / 3600);
  const minutes = Math.floor((s % 3600) / 60);
  return hours > 0 ? `${hours}:${pad(minutes)}:${pad(s % 60)}` : `${minutes}:${pad(s % 60)}`;
}

/** Retry-After seconds → "30 seconds", "4 minutes", "3 hours". */
export function formatWait(seconds) {
  if (seconds < 90) return `${Math.max(1, Math.round(seconds))} seconds`;
  if (seconds < 90 * 60) return `${Math.round(seconds / 60)} minutes`;
  return `${Math.round(seconds / 3600)} hours`;
}

/** Whisper reports language names in lower case ("tamil"). */
export const titleCase = (text) => (text ? text.charAt(0).toUpperCase() + text.slice(1) : text);

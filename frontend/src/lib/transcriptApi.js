// Client for the TranscriptAI API (backend/src/TranscriptAI.Api).
// In development Vite proxies /api to http://localhost:5080; in production the API runs on
// Azure App Service and its URL is baked in at build time through VITE_API_BASE_URL.

const API_BASE = (import.meta.env.VITE_API_BASE_URL ?? "").trim().replace(/\/+$/, "");

/** False for production builds without a backend URL — the demo button is hidden then. */
export const isDemoConnected = API_BASE !== "" || import.meta.env.DEV;

export class ApiError extends Error {
  constructor(message, { status = 0, title, retryAfter = null } = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.title = title;
    this.retryAfter = retryAfter;
  }
}

const firstValidationError = (errors) => {
  const first = errors && Object.values(errors)[0];
  return Array.isArray(first) ? first[0] : undefined;
};

/** Builds an ApiError from an RFC 9457 problem-details body (or whatever the server sent). */
function problemToError(status, bodyText, retryAfterHeader) {
  let title;
  let detail;
  try {
    const problem = JSON.parse(bodyText);
    title = problem.title;
    detail = problem.detail ?? firstValidationError(problem.errors);
  } catch {
    // Not JSON — e.g. a platform error page.
  }

  const retryAfter = Number.parseInt(retryAfterHeader ?? "", 10);
  return new ApiError(detail || title || `The demo server returned HTTP ${status}.`, {
    status,
    title,
    retryAfter: Number.isFinite(retryAfter) ? retryAfter : null,
  });
}

async function request(path, { method = "GET", body, signal } = {}) {
  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method,
      signal,
      headers: { Accept: "application/json", ...(body ? { "Content-Type": "application/json" } : {}) },
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch (error) {
    if (error.name === "AbortError") throw error;
    throw new ApiError("Couldn't reach the demo server.");
  }

  if (!response.ok) {
    throw problemToError(response.status, await response.text(), response.headers.get("Retry-After"));
  }
  return response.json();
}

/** Status and limits (provider, max upload, accepted formats…). */
export const getHealth = ({ signal } = {}) => request("/api/health", { signal });

export const transcribeYouTube = ({ url, task, language }, { signal } = {}) =>
  request("/api/transcriptions/youtube", {
    method: "POST",
    body: { url, task, language: language || null },
    signal,
  });

/** Uploads with XMLHttpRequest rather than fetch so upload progress can be shown. */
export function transcribeFile({ file, task, language }, { signal, onUploadProgress } = {}) {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open("POST", `${API_BASE}/api/transcriptions/file`);
    xhr.setRequestHeader("Accept", "application/json");

    xhr.upload.addEventListener("progress", (event) => {
      if (event.lengthComputable) onUploadProgress?.(event.loaded / event.total);
    });
    xhr.upload.addEventListener("load", () => onUploadProgress?.(1));

    xhr.addEventListener("load", () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        try {
          resolve(JSON.parse(xhr.responseText));
        } catch {
          reject(new ApiError("The demo server sent an unreadable response.", { status: xhr.status }));
        }
        return;
      }
      reject(problemToError(xhr.status, xhr.responseText, xhr.getResponseHeader("Retry-After")));
    });
    xhr.addEventListener("error", () => reject(new ApiError("Couldn't reach the demo server.")));
    xhr.addEventListener("abort", () => reject(new DOMException("The upload was cancelled.", "AbortError")));
    signal?.addEventListener("abort", () => xhr.abort(), { once: true });

    const form = new FormData();
    form.append("file", file);
    form.append("task", task);
    if (language) form.append("language", language);
    xhr.send(form);
  });
}

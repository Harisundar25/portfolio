# Hari Sundar — Portfolio

Personal portfolio of **Hari Sundar**, Software Engineer (C# / .NET) in Bengaluru.

This repository is a small monorepo:

| Folder | What it is |
| --- | --- |
| [`frontend/`](frontend) | React 19 + Vite + Tailwind CSS 4 + Framer Motion portfolio site. Design adapted from [awwwards-style-portfolio-2026](https://github.com/JeevaVenkidu/awwwards-style-portfolio-2026) (MIT). |
| [`backend/`](backend) | **AI Transcript Generator**: a C# / .NET 10 port of my Python [Transcript-AI](https://github.com/HariSundar-GRL/Transcript-AI) script. It runs open-source Whisper models through the Groq API and ships as a CLI plus the ASP.NET Core API behind the site's live demo. See [backend/README.md](backend/README.md). |
| [`docs/`](docs) | [Deploying to Azure for free](docs/DEPLOYMENT.md): Static Web Apps + App Service F1 + GitHub Actions. |

## Run it locally

Requirements: Node.js 20.19+ (Node 24 LTS recommended) and the .NET 10 SDK.

```bash
# 1. The site — http://localhost:5173
cd frontend
npm install
npm run dev

# 2. Optional: the transcript API for the live demo — http://localhost:5080
#    Vite proxies /api to it during development. Get a free key at https://console.groq.com/keys
cd backend
dotnet user-secrets set "Transcription:Groq:ApiKey" "gsk_..." --project src/TranscriptAI.Api
dotnet run --project src/TranscriptAI.Api
```

Tests: `npm run lint` in `frontend/`, and `dotnet test --solution TranscriptAI.slnx` in `backend/`.

## Deploy

Every push to `main` runs CI. After the one-time Azure setup in [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md), the same
pushes also deploy the site to Azure Static Web Apps and the API to Azure App Service, both on free plans.

## License

[MIT](LICENSE). Portions of the frontend design are © 2026 Jeeva V, used under the MIT License.

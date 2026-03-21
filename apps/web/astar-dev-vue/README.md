# astar-dev-vue

A modern web application built with **Vue 3**, **Express**, and **TypeScript**, deployed to **Azure Web App** via GitHub Actions.

## Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Vue 3 (Composition API, `<script setup>`) |
| Build tool | Vite |
| Backend | Express 4 |
| Language | TypeScript 5 (client + server) |
| Security | Helmet middleware |
| CI/CD | GitHub Actions → Azure Web App |

## Project structure

```
├── client/          # Vue 3 + TypeScript frontend (Vite)
│   ├── src/
│   │   ├── App.vue
│   │   ├── main.ts
│   │   └── components/
│   │       ├── Hero.vue
│   │       ├── Features.vue
│   │       └── Footer.vue
│   ├── index.html
│   ├── vite.config.ts
│   └── tsconfig.json
├── server/          # Express + TypeScript backend
│   ├── src/
│   │   └── index.ts
│   └── tsconfig.json
├── .github/
│   └── workflows/
│       └── azure-deploy.yml
└── package.json     # Workspace root
```

## Getting started

```bash
# Install all dependencies
npm install

# Start the Vue dev server (hot reload)
npm run dev:client

# Start the Express dev server (ts-node-dev)
npm run dev:server

# Build everything for production
npm run build

# Run the production server (serves built Vue app)
npm start
```

## Azure deployment

1. Create an Azure Web App (Node 20 LTS runtime).
2. Download the **Publish Profile** from the Azure portal.
3. In your GitHub repository → **Settings → Secrets and variables → Actions**, add a secret named `AZURE_WEBAPP_PUBLISH_PROFILE` and paste the publish profile XML.
4. Update the `AZURE_WEBAPP_NAME` environment variable in `.github/workflows/azure-deploy.yml` to match your Azure Web App name.
5. Push to `main` – the GitHub Actions workflow will build and deploy automatically.

## API endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/health` | Health check – returns `{ status: "ok", timestamp }` |

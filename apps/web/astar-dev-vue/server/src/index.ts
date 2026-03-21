import express, { Request, Response, NextFunction } from 'express'
import helmet from 'helmet'
import rateLimit from 'express-rate-limit'
import fs from 'fs'
import path from 'path'

const app = express()
const PORT: number = parseInt(process.env.PORT ?? '3000', 10)

app.use(
  helmet({
    contentSecurityPolicy: {
      directives: {
        defaultSrc: ["'self'"],
        scriptSrc: ["'self'", "'unsafe-inline'"],
        styleSrc: ["'self'", "'unsafe-inline'"],
        imgSrc: ["'self'", 'data:', 'https:'],
      },
    },
  }),
)

const limiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 200,
  standardHeaders: true,
  legacyHeaders: false,
})
app.use(limiter)

app.use(express.json())

// Health-check endpoint
app.get('/api/health', (_req: Request, res: Response): void => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() })
})

const clientDistCandidates = [
  path.resolve(process.cwd(), 'client/dist'),
  path.resolve(process.cwd(), 'wwwroot/client/dist'),
  path.resolve(__dirname, '../../client/dist'),
  path.resolve(__dirname, '../client/dist'),
]

const clientDist =
  clientDistCandidates.find((candidate) => fs.existsSync(path.join(candidate, 'index.html'))) ??
  ''

if (clientDist) {
  app.use(express.static(clientDist))
}

if (clientDist) {
  app.use(express.static(clientDist))
}

app.get(/^\/(?!api).*/, (_req, res) => {
  if (!clientDist) {
    return res.status(503).json({
      error: 'Frontend Not Available',
      message: 'No built frontend found.',
      candidates: clientDistCandidates,
    })
  }

  res.sendFile(path.join(clientDist, 'index.html'))
})

app.use((err: Error, _req: Request, res: Response, _next: NextFunction): void => {
  const errorId = Date.now().toString(36)
  console.error(`[${errorId}]`, err.stack)
  const body: Record<string, string> = { error: 'Internal Server Error', errorId }
  if (process.env.NODE_ENV !== 'production') {
    body.message = err.message
  }
  res.status(500).json(body)
})

app.listen(PORT, () => {
  console.log(`Server running on http://localhost:${PORT}`)
})

export default app

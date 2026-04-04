## Context

The mono-repo already contains an Astro 5 + Vue + Node site (`apps/web/astar-dev`) deployed to Azure App Service — proving the stack works in this environment. `fab4kids` will follow the same pattern, substituting Vue islands for React islands (matching the placeholder's original intent and the repo's React test infrastructure for fab4kids).

The existing placeholder (`apps/web/fab4kids.placeholder`) is a React SPA with no SSR, no SEO, and no commerce. It is deleted wholesale; nothing is migrated from it.

Constraints:
- Single deployable unit (Astro/Node) on Azure App Service — no separate API service
- Near-zero cold start: product and category pages must be pre-rendered (SSG); only dynamic endpoints touch Node at runtime
- UK GDPR compliance required before launch
- VAT-exempt at launch; Stripe Tax must be wired in at 0% to allow future rate changes without code changes

## Goals / Non-Goals

**Goals:**
- Pre-rendered (SSG) homepage, category, and product pages for instant load and SEO
- Stripe hosted checkout (PCI compliance offloaded to Stripe)
- Digital delivery via signed Azure Blob Storage URLs emailed with Resend
- Sanity CMS for content management — product additions require no deployment
- Pagefind full-text search built at deploy time
- Three themes (Light / Dark / Colourful) via CSS custom properties + `data-theme` on `<html>`
- UK GDPR cookie consent gating Stripe JS and any analytics
- CI/CD via new `fab4kids-deploy.yml` GitHub Actions workflow

**Non-Goals:**
- User accounts / order history (post-MVP)
- Newsletter delivery integration (email capture only; provider chosen post-MVP)
- Physical goods fulfilment
- Multi-currency / non-GBP pricing
- Admin dashboard (Sanity Studio is the admin UI)

## Decisions

### 1. Astro 5 (SSG-first with SSR opt-in) over Next.js or plain React

**Why Astro:** Product pages, category pages, and the homepage carry no per-request dynamic data — they are ideal SSG targets. Astro pre-renders these at build time; Azure serves raw HTML from disk. Cold start is effectively zero for the majority of page views. Next.js SSR adds a Node warm-up cost on every cold request even for pages that could be static. The pattern is already validated in this repo (`astar-dev`).

**Alternatives considered:** Next.js (heavier cold start, less control over static vs dynamic per-route), plain React SPA (no SSO, no SEO).

### 2. React islands over Vue islands

The repo uses Vue for `astar-dev`. React is chosen here because the original placeholder was React, the QA test infrastructure for fab4kids targets `.test.tsx`, and polyglot co-existence is established repo practice. Interactive islands (cart drawer, theme switcher, cookie banner, newsletter form) are React components; everything else is Astro/HTML.

### 3. Sanity CMS over file-based content (MDX/YAML)

Product catalogue will grow to 100+ items per category quickly. File-based content requires a git commit + full redeploy (~10–15 min) to add a product. Sanity's free tier allows content editors to publish in seconds via the web studio. Astro's content layer fetches Sanity data at build time — no runtime dependency on Sanity during page serving.

**Migration path:** If Sanity becomes costly, the data shape (GROQ → TypeScript types → Astro pages) is identical to a local JSON/YAML source. Swapping the loader is a single file change.

### 4. Stripe hosted checkout over custom payment form

Stripe Checkout (hosted) keeps all card data off our infrastructure — PCI SAQ A compliance. The webhook pattern (Stripe calls our `/api/webhooks/stripe` endpoint on `checkout.session.completed`) is well-proven and straightforward to implement in an Astro API route.

**Stripe Tax at 0%:** A Stripe Tax rate object is created and attached to all line items at 0%. When VAT liability is triggered, the rate object is updated in the Stripe dashboard — no code change required.

### 5. Azure Blob Storage for digital asset delivery

Signed URLs with a short TTL (15 minutes) are generated server-side in the Stripe webhook handler and included in the Resend delivery email. Files are never publicly accessible. Azure Blob is the natural choice given the Azure App Service deployment target.

**Alternative:** Email the file as an attachment. Rejected — file sizes (PDF/Word) can exceed email attachment limits and create deliverability issues.

### 6. Pagefind for search

Pagefind indexes the static HTML output at build time and ships a small WASM bundle to the client. Zero server cost, zero external service dependency, sub-100ms search on the client. Works perfectly with Astro's static output.

**Alternative:** Algolia (free tier). Rejected for MVP — adds an external dependency and API key management for no benefit at current catalogue size.

### 7. CSS custom properties + `data-theme` for theming

Three theme values: `light`, `dark`, `colourful`. Applied as `data-theme="colourful"` on `<html>`. All colour tokens are CSS custom properties scoped per theme. Persisted to `localStorage`; initial value resolved from `localStorage` → `prefers-color-scheme` → `light`. A small inline `<script>` in `<head>` sets the attribute before first paint to prevent flash.

The `colourful` theme uses vibrant hue-shifted accent colours and a playful but accessible palette (WCAG AA contrast maintained).

### 8. URL structure: subject-first, key-stage as filter

```
/                          Homepage
/[subject]                 /maths  /english  /science  /history  /geography
/[subject]/[ks]            /maths/ks1  /maths/ks2  /maths/ks3  /maths/ks4
/product/[slug]            Product detail page
/search                    Pagefind search results
/checkout/success          Post-Stripe redirect + download instructions
/privacy-policy
/cookie-policy
/terms
```

Research confirms parents think subject-first ("my child needs maths help") then filter by year group. Subject pages show all KS products with KS filter tabs; KS sub-pages are statically generated for SEO.

## Risks / Trade-offs

**Sanity build-time coupling** → If Sanity is down at deploy time, the build fails. Mitigation: cache the last successful build artefact; add a Sanity health check step in CI.

**Stripe webhook reliability** → If the webhook call fails, the customer pays but receives no download link. Mitigation: Stripe retries webhooks for 72 hours. Log all webhook events; implement idempotency check on `checkout.session.id` to prevent duplicate emails on retry.

**Signed URL expiry** → 15-minute URLs expire before customer opens email on a slow connection. Mitigation: include a "resend download link" form on `/checkout/success` (submits order reference + email, re-generates URL). Post-MVP: account system replaces this.

**Azure App Service cold start for SSR routes** → Static pages have zero cold start. SSR routes (`/api/*`) may cold-start if the App Service instance has idled. Mitigation: configure Azure App Service "Always On" setting; SSR routes are only hit for cart/checkout actions where a brief delay is acceptable.

**Pagefind index staleness** → Search index is built at deploy time. New Sanity products are not searchable until next deploy. Mitigation: acceptable for MVP; post-MVP trigger a partial rebuild or switch to Algolia if catalogue velocity demands it.

## Migration Plan

1. Delete `apps/web/fab4kids.placeholder` directory
2. Scaffold `apps/web/fab4kids` as a new Astro 5 project
3. Configure Sanity project; seed with initial product data
4. Configure Azure Blob Storage container; upload initial product files
5. Configure Stripe products, prices, and Tax rate (0%)
6. Configure Resend domain and templates
7. Deploy to new Azure Web App `fab4kids` via `fab4kids-deploy.yml`
8. Smoke-test full purchase flow in staging before DNS cutover

Rollback: the placeholder is in git history; revert the deletion commit and redeploy.

## Open Questions

- **Newsletter storage**: Where are captured emails stored until a provider is chosen? Options: Sanity document, Azure Table Storage, or a simple Astro API route writing to a file. Needs a decision before the newsletter-capture spec is implemented.
- **Resend domain**: Is `fab4kids.co.uk` (or equivalent) available and DNS-configurable for Resend DKIM? Needs verification before email delivery is tested.
- **Azure Web App name**: Confirm `fab4kids` is available in the target Azure subscription before creating infrastructure.
- **Product file naming / organisation**: Define Azure Blob container structure (e.g., `/{subject}/{ks}/{slug}.pdf`) before upload tooling is built.

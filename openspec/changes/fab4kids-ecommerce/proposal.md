## Why

The current `apps/web/fab4kids.placeholder` is a React SPA with no SEO, no server rendering, and no commerce capability. fab4kids needs a production-grade storefront to sell educational materials (PDFs, Word docs) to parents of KS1–KS4 children — generating revenue while the brand grows.

## What Changes

- Replace `apps/web/fab4kids.placeholder` with `apps/web/fab4kids` — a full Astro 5 site
- Product catalogue managed in Sanity CMS (no redeploy required to add/edit products)
- Guest checkout via Stripe hosted checkout; Stripe Tax wired at 0% (VAT-exempt, toggle-ready)
- Post-purchase digital delivery: signed Azure Blob Storage URLs sent via Resend
- Newsletter email capture (provider TBD post-MVP)
- Full-text search via Pagefind (build-time, zero server cost)
- Three colour themes: Light, Dark, Colourful — persisted to `localStorage`
- UK GDPR compliance: cookie consent banner, privacy policy, explicit newsletter opt-in
- CI/CD: new `fab4kids-deploy.yml` workflow deploying to Azure App Service

## Capabilities

### New Capabilities

- `product-catalogue`: Browse products by subject (Maths, English, Science, History, Geography) and key stage (KS1–KS4); backed by Sanity CMS
- `product-purchase`: Guest checkout via Stripe hosted checkout; Stripe webhook triggers digital delivery
- `digital-delivery`: Signed Azure Blob Storage URL generated post-payment and emailed to customer via Resend
- `search`: Pagefind full-text search across all product and category pages
- `theme-switching`: Light / Dark / Colourful theme selector, persisted to localStorage, respects `prefers-color-scheme` default
- `newsletter-capture`: Email capture form with explicit GDPR opt-in; emails stored for future provider integration
- `gdpr-compliance`: UK GDPR cookie consent banner, privacy policy page, cookie policy page, terms page

### Modified Capabilities

## Impact

- **Removed**: `apps/web/fab4kids.placeholder` (all files)
- **New app**: `apps/web/fab4kids` — Astro 5, React islands, Node adapter, deployed to Azure App Service
- **New dependencies**: Astro, `@astrojs/react`, `@astrojs/node`, `@sanity/client`, `stripe`, `resend`, `@azure/storage-blob`, `pagefind`
- **New workflow**: `.github/workflows/fab4kids-deploy.yml`
- **New Azure resources**: Azure Blob Storage container for product file assets; Azure Web App `fab4kids`
- **Sanity project**: new Sanity dataset for product/category content
- **Stripe**: new Stripe product catalogue + webhook endpoint configured

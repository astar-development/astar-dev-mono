## 1. Project Bootstrap

- [x] 1.1 Create feature branch `feature/fab4kids-ecommerce` from `main`
- [x] 1.2 Delete `apps/web/fab4kids.placeholder` directory
- [x] 1.3 Scaffold `apps/web/fab4kids` as a new Astro 5 project with `@astrojs/react` and `@astrojs/node` (standalone) adapters
- [x] 1.4 Convert all `.jsx` files to `.tsx` and configure `tsconfig.json` with strict TypeScript
- [x] 1.5 Add `eslint.config.js` following repo JavaScript code-style rules
- [x] 1.6 Add `vitest.config.ts` and `src/__tests__/` directory structure per QA spec
- [x] 1.7 Confirm `npm run dev`, `npm run build`, and `npm run preview` all succeed

## 2. Sanity CMS Setup

- [ ] 2.1 Create a Sanity project and dataset (`production`)
- [x] 2.2 Define Sanity schema: `product` (title, slug, description, subject, keyStage[], fileFormats[], price, blobPath, image, publishedAt)
- [x] 2.3 Define Sanity schema: `subject` (title, slug, description, heroImage)
- [x] 2.4 Install `@sanity/client` and configure Astro content layer integration
- [ ] 2.5 Seed Sanity with at least two products per subject/KS combination for local dev
- [x] 2.6 Add `SANITY_PROJECT_ID`, `SANITY_DATASET`, and `SANITY_API_TOKEN` to `.env.example`

## 3. Product Catalogue — Failing Tests First

- [x] 3.1 Write failing tests: subject page renders product cards for each subject
- [x] 3.2 Write failing tests: key-stage sub-page shows only products for that KS
- [x] 3.3 Write failing tests: product detail page renders all required fields
- [x] 3.4 Write failing tests: non-existent product slug returns 404
- [x] 3.5 Implement `src/pages/[subject].astro` — subject hub page (SSG)
- [x] 3.6 Implement `src/pages/[subject]/[ks].astro` — key-stage sub-page (SSG)
- [x] 3.7 Implement `src/pages/product/[slug].astro` — product detail page (SSG)
- [x] 3.8 Implement `ProductCard` React component with title, KS badge, subject badge, format badges, price
- [x] 3.9 Implement site navigation with all five subject links
- [x] 3.10 Confirm all catalogue tests pass and `dotnet build` / `npm run build` succeed

## 4. Theme Switching — Failing Tests First

- [x] 4.1 Write failing tests: `useTheme` hook persists selection to `localStorage`
- [x] 4.2 Write failing tests: default theme resolved from `prefers-color-scheme`
- [x] 4.3 Write failing tests: `ThemeSwitcher` component emits correct theme on click
- [x] 4.4 Define CSS custom properties for `light`, `dark`, and `colourful` themes in `src/styles/themes.css`
- [x] 4.5 Implement `useTheme` React hook (read/write `localStorage`, expose `setTheme`)
- [x] 4.6 Implement `ThemeSwitcher` React island component
- [x] 4.7 Add inline theme-init `<script>` to base layout `<head>` to prevent FOCT
- [x] 4.8 Confirm all theme tests pass

## 5. Search — Failing Tests First

- [x] 5.1 Write failing tests: header search form submits to `/search?q=`
- [x] 5.2 Write failing tests: search page renders with query parameter
- [x] 5.3 Install `pagefind` and configure as post-build step in `package.json`
- [x] 5.4 Implement `src/pages/search.astro` with Pagefind UI component
- [x] 5.5 Add `data-pagefind-body` attributes to product and category page templates
- [x] 5.6 Add search input to site header component
- [x] 5.7 Confirm Pagefind index builds correctly and search returns results locally

## 6. Azure Blob Storage Setup

- [ ] 6.1 Create Azure Blob Storage account and private container `fab4kids-products`
- [x] 6.2 Define container path convention: `/{subject}/{ks}/{slug}.{ext}`
- [ ] 6.3 Upload at least two real product files for end-to-end testing
- [x] 6.4 Install `@azure/storage-blob` and add `AZURE_STORAGE_CONNECTION_STRING` to `.env.example`
- [x] 6.5 Write failing tests: signed URL generator returns a URL with correct TTL
- [x] 6.6 Implement `src/lib/storage.ts` — `generateSignedUrl(blobPath: string): Promise<string>` (15-min SAS)
- [x] 6.7 Confirm signed URL tests pass

## 7. Stripe & Checkout — Failing Tests First

- [x] 7.1 Write failing tests: `POST /api/checkout` creates a session with correct line items
- [x] 7.2 Write failing tests: webhook rejects invalid Stripe signature
- [x] 7.3 Write failing tests: webhook is idempotent on duplicate session ID
- [ ] 7.4 Create Stripe products and prices in Stripe dashboard (test mode)
- [ ] 7.5 Create Stripe Tax rate at 0% and note the rate ID
- [x] 7.6 Install `stripe` SDK and add `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET` to `.env.example`
- [x] 7.7 Implement `src/pages/api/checkout.ts` — creates Stripe Checkout session with Tax rate
- [x] 7.8 Implement cart React context (`CartContext`) with `localStorage` persistence
- [x] 7.9 Implement `CartDrawer` React island component (add/remove items, proceed to checkout)
- [x] 7.10 Implement `src/pages/api/webhooks/stripe.ts` — handles `checkout.session.completed`, idempotency check
- [x] 7.11 Implement `src/pages/checkout/success.astro` — confirm purchase, show resend form
- [x] 7.12 Implement `src/pages/api/resend-links.ts` — re-generates and re-emails signed URLs
- [x] 7.13 Confirm all Stripe/checkout tests pass

## 8. Email Delivery via Resend — Failing Tests First

- [x] 8.1 Write failing tests: delivery email sent to correct address with correct links
- [x] 8.2 Write failing tests: missing customer email logs error and does not send
- [x] 8.3 Install `resend` SDK and add `RESEND_API_KEY` and `RESEND_FROM_ADDRESS` to `.env.example`
- [ ] 8.4 Configure Resend domain and DKIM for the fab4kids sending domain
- [x] 8.5 Implement `src/lib/email.ts` — `sendDeliveryEmail(to, links)` using Resend
- [x] 8.6 Wire `sendDeliveryEmail` into the Stripe webhook handler
- [x] 8.7 Confirm all email delivery tests pass

## 9. Newsletter Capture — Failing Tests First

- [x] 9.1 Write failing tests: `POST /api/newsletter` stores email and timestamp
- [x] 9.2 Write failing tests: duplicate email returns success without storing duplicate
- [x] 9.3 Write failing tests: missing opt-in checkbox prevents form submission
- [x] 9.4 Decide newsletter email storage mechanism (resolve open question from design.md)
- [x] 9.5 Implement `src/pages/api/newsletter.ts` API route
- [x] 9.6 Implement `NewsletterForm` React island component with GDPR opt-in checkbox
- [x] 9.7 Add `NewsletterForm` to homepage
- [x] 9.8 Confirm all newsletter tests pass

## 10. GDPR & Legal Pages — Failing Tests First

- [x] 10.1 Write failing tests: cookie banner present on first visit, absent after consent
- [x] 10.2 Write failing tests: Stripe JS absent before consent, present after
- [x] 10.3 Implement `CookieBanner` React island component with accept/decline
- [x] 10.4 Implement consent-gated Stripe JS loader (inject after consent)
- [x] 10.5 Implement `src/pages/privacy-policy.astro` (all required UK GDPR sections)
- [x] 10.6 Implement `src/pages/cookie-policy.astro` (cookie table: name, purpose, duration)
- [x] 10.7 Implement `src/pages/terms.astro` (digital goods policy, IP, acceptable use, governing law)
- [x] 10.8 Add footer with links to all three legal pages on every page
- [x] 10.9 Confirm all GDPR tests pass

## 11. CI/CD & Deployment

- [ ] 11.1 Create Azure Web App `fab4kids` in the target subscription
- [x] 11.2 Create `.github/workflows/fab4kids-deploy.yml` following the `astar-dev-deploy.yml` pattern
- [ ] 11.3 Add all required secrets to GitHub Actions (`STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET`, `RESEND_API_KEY`, `AZURE_STORAGE_CONNECTION_STRING`, `SANITY_PROJECT_ID`, `SANITY_DATASET`, `SANITY_API_TOKEN`, `AZURE_WEBAPP_PUBLISH_PROFILE`)
- [ ] 11.4 Configure Azure App Service "Always On" to minimise cold starts on SSR routes
- [ ] 11.5 Trigger first deployment and verify site loads at Azure URL
- [ ] 11.6 Register Stripe webhook endpoint pointing to `https://<azure-url>/api/webhooks/stripe`
- [ ] 11.7 Run full end-to-end smoke test: browse → add to cart → checkout (test card) → confirm delivery email received → download link works

## 12. QA Review & Definition of Done

- [x] 12.1 Run `npm run build` — zero errors, zero warnings
- [x] 12.2 Run `npx vitest run` — all tests pass
- [ ] 12.3 Request review from `c-sharp-senior-qa-specialist` agent (JavaScript QA equivalent)
- [ ] 12.4 Resolve all Blocker and Major issues; raise GitHub issues for Minor/Info
- [ ] 12.5 Verify all pages score 90+ on Lighthouse SEO and Performance
- [ ] 12.6 Verify WCAG AA contrast on all three themes
- [ ] 12.7 Raise PR to `main` with conventional commit summary

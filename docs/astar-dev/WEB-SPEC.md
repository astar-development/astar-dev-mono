# AStar Development — Website Specification

> **Version:** 1.0
> **Date:** 2026-03-28
> **Status:** Approved for development
> **Author:** Business analysis session (stakeholder + BA)

---

## Table of Contents

- [1. Problem Statement](#1-problem-statement)
- [2. Goals & Success Metrics](#2-goals--success-metrics)
- [3. User Personas](#3-user-personas)
- [4. Site Map & Navigation](#4-site-map--navigation)
- [5. Framework & Architecture](#5-framework--architecture)
- [6. Hosting & Deployment](#6-hosting--deployment)
- [7. Phased Delivery Plan](#7-phased-delivery-plan)
- [8. Page Specifications](#8-page-specifications)
- [9. Theming System](#9-theming-system)
- [10. Blog Engine](#10-blog-engine)
- [11. NuGet Package Display](#11-nuget-package-display)
- [12. Contact Form & SendGrid](#12-contact-form--sendgrid)
- [13. Analytics — Azure Application Insights](#13-analytics--azure-application-insights)
- [14. Legal & Compliance (UK GDPR)](#14-legal--compliance-uk-gdpr)
- [15. Accessibility — WCAG 2.1 AA](#15-accessibility--wcag-21-aa)
- [16. Performance Budget](#16-performance-budget)
- [17. Mobile Responsiveness](#17-mobile-responsiveness)
- [18. Non-Functional Requirements](#18-non-functional-requirements)
- [19. Configuration & Secrets](#19-configuration--secrets)
- [20. Affected Files & Migration Notes](#20-affected-files--migration-notes)
- [21. Risks & Assumptions](#21-risks--assumptions)
- [22. Open Questions](#22-open-questions)

---

## 1. Problem Statement

The current site at `apps/web/astar-dev-web` is a placeholder single-page Vue 3 application with fictional content, a broken theme system (themes only apply to the hero section), a non-functional contact form, empty smoke tests, and no SEO support (the Vue SPA sends an empty `<div>` to search engine crawlers). It cannot serve its intended purpose: attracting engineering leaders and hiring managers to contact the site owner about professional opportunities.

The site must be rebuilt as a production-grade, SEO-friendly, multi-page professional portfolio and consultancy website that showcases real skills, real NuGet packages, and real (anonymised) project experience.

---

## 2. Goals & Success Metrics

| Goal                                   | Metric                                      | Target                                                          |
| -------------------------------------- | ------------------------------------------- | --------------------------------------------------------------- |
| Attract professional opportunities     | Contact form submissions per month          | ≥ 1 (from zero today)                                           |
| Search engine discoverability          | Google indexing of all public pages         | 100% of pages indexed within 30 days of launch                  |
| Fast load experience on budget hosting | Lighthouse mobile performance score         | ≥ 90                                                            |
| Accessible to all users                | WCAG 2.1 AA compliance                      | All pages, all themes pass automated + manual audit             |
| Minimise cold-start impact             | Time-to-first-byte on cold Azure Web App    | < 3 seconds                                                     |
| Professional credibility               | NuGet packages displayed with live metadata | All curated packages render with accurate version/download data |

---

## 3. User Personas

### Persona 1: The Engineering Leader

- **Role:** CTO, VP Engineering, Engineering Manager at a mid-size company
- **Goals:** Find a senior .NET consultant/contractor for architecture, backend, or mentoring work
- **Behaviour:** Arrives via search ("senior .NET consultant"), scans the homepage in < 30 seconds, looks at case studies for credibility, uses the contact form if impressed
- **Pain points:** Dislikes generic corporate sites; values evidence of real work over buzzwords
- **Device:** Likely desktop at work, but may share links via Slack/Teams to colleagues who open on mobile

### Persona 2: The Recruiter / Hiring Manager

- **Role:** Technical recruiter or hiring manager
- **Goals:** Quickly assess seniority, tech stack, and availability
- **Behaviour:** Arrives via LinkedIn profile link, wants to see skills demonstrated (packages, blog, case studies), looks for contact method
- **Pain points:** Needs to move fast; won't read long pages
- **Device:** Mix of desktop and mobile

### Persona 3: The Developer

- **Role:** .NET developer looking for useful packages
- **Goals:** Find, evaluate, and install AStar NuGet packages
- **Behaviour:** Arrives via NuGet.org link or search, wants to see documentation, download counts, and install commands
- **Pain points:** Hates outdated docs or broken links
- **Device:** Desktop (they're coding)

---

## 4. Site Map & Navigation

### Pages

```
/                   → Home
/packages           → All NuGet Packages (grouped by use case)
/blog               → Blog listing
/blog/[slug]        → Individual blog post
/case-studies       → Case Studies listing
/case-studies/[slug]→ Individual case study
/contact            → Contact form (SendGrid)
/privacy            → Privacy policy
```

### Persistent Navigation Bar

Present on every page. Contains:

- **Logo / brand** (links to home)
- **Page links:** Home, Packages, Blog, Case Studies, Contact
- **Theme switcher** (dropdown or segmented control)
- **External links:** GitHub icon, NuGet icon (open in new tab)

On mobile, the nav collapses into a hamburger menu.

---

## 5. Framework & Architecture

### Recommendation: Astro

**Why Astro over Vue SPA, React, or Next.js:**

| Criterion                   | Vue SPA (current)         | Next.js                     | Astro                                      |
| --------------------------- | ------------------------- | --------------------------- | ------------------------------------------ |
| SEO (server-rendered HTML)  | ✗ Empty div               | ✓ SSR                       | ✓ Static + SSR hybrid                      |
| Cold-start on Azure Web App | N/A (static files)        | Slow (Node.js SSR)          | Fast (static pages, minimal JS)            |
| Markdown blog support       | Manual setup              | MDX plugin                  | Built-in (native `.md`/`.mdx` collections) |
| JS shipped to browser       | Full Vue runtime (~50KB+) | Full React runtime (~80KB+) | Zero JS by default; opt-in per component   |
| Build-time data fetching    | Manual                    | `getStaticProps`            | Native `getStaticPaths` + `fetch`          |
| Learning curve from Vue     | New framework             | New framework               | Can use Vue components inside Astro        |
| Lighthouse score potential  | ~70-80 (SPA hydration)    | ~80-85 (hydration overhead) | ~95-100 (zero JS by default)               |

**Key advantage:** Astro ships **zero client-side JavaScript by default**. Interactive components (theme switcher, contact form) opt in to hydration with `client:load` or `client:visible` directives. This means:

- Static pages (blog posts, case studies, privacy policy) are pure HTML + CSS — fastest possible load
- Cold-start is irrelevant for most pages because they are pre-built static HTML
- The few interactive components hydrate only when needed

**Astro can use Vue components**, so existing component logic (theme switcher, composables) can be adapted rather than rewritten from scratch.

### Architecture Diagram

```
apps/web/astar-dev/
├── astro.config.mjs          # Astro configuration
├── package.json
├── public/                    # Static assets (logos, favicons)
│   └── assets/
├── src/
│   ├── components/            # Astro + Vue interactive components
│   │   ├── ThemeSwitcher.vue  # client:load — needs JS
│   │   ├── ContactForm.vue    # client:load — needs JS
│   │   ├── CookieConsent.vue  # client:load — needs JS
│   │   ├── Nav.astro          # Static nav bar
│   │   ├── Footer.astro       # Static footer
│   │   ├── Hero.astro
│   │   ├── ServiceCard.astro
│   │   ├── PackageCard.astro
│   │   └── CaseStudyCard.astro
│   ├── content/               # Astro Content Collections
│   │   ├── blog/              # Markdown blog posts
│   │   │   └── my-first-post.md
│   │   └── case-studies/      # HTML/Markdown case studies
│   │       └── anonymised-project-1.md
│   ├── data/
│   │   └── packages.json      # Curated NuGet package ID list
│   ├── layouts/
│   │   ├── BaseLayout.astro   # HTML shell, theme class, App Insights, cookie consent
│   │   ├── BlogLayout.astro   # Blog post layout with metadata
│   │   └── CaseStudyLayout.astro
│   ├── pages/
│   │   ├── index.astro        # Home
│   │   ├── packages.astro     # All packages
│   │   ├── blog/
│   │   │   ├── index.astro    # Blog listing
│   │   │   └── [...slug].astro # Dynamic blog post pages
│   │   ├── case-studies/
│   │   │   ├── index.astro    # Case studies listing
│   │   │   └── [...slug].astro
│   │   ├── contact.astro      # Contact form
│   │   └── privacy.astro      # Privacy policy
│   ├── styles/
│   │   ├── global.css         # Reset, typography, shared utilities
│   │   └── themes.css         # All four theme definitions (CSS custom properties)
│   └── lib/
│       ├── nuget.ts           # NuGet API fetch logic (build-time)
│       └── sendgrid.ts        # SendGrid email logic (server endpoint)
├── tests/
│   ├── components/            # Component tests (Vitest)
│   └── e2e/                   # End-to-end tests (Playwright)
└── .env.example               # Template for local env vars
```

### Server Endpoints (Astro API Routes)

Astro supports server-side API routes for the small amount of backend logic needed:

| Endpoint       | Method | Purpose                                                         |
| -------------- | ------ | --------------------------------------------------------------- |
| `/api/contact` | POST   | Receives contact form submission, validates, sends via SendGrid |

**The existing Express server (`server/`) is replaced entirely by Astro's built-in capabilities.** No separate Node.js server is needed.

---

## 6. Hosting & Deployment

| Setting           | Value                                                                    |
| ----------------- | ------------------------------------------------------------------------ |
| **Host**          | Azure Web App (existing, B1 tier, with custom domain)                    |
| **Runtime**       | Node.js LTS                                                              |
| **Build output**  | Astro SSG (static HTML) with SSR opt-in for `/api/contact` endpoint only |
| **CI/CD**         | GitHub Actions — build on push to `main`, deploy to Azure Web App        |
| **Custom domain** | Existing domain purchased from Microsoft — retain as-is                  |
| **SSL**           | Azure-managed certificate (free with custom domain on Web App)           |

### Astro Output Mode

Use `output: 'hybrid'` in `astro.config.mjs`:

- All pages are **pre-rendered at build time** (static HTML) by default
- The `/api/contact` endpoint opts into **server-side rendering** (runs on the Node.js adapter at request time)
- This gives the best cold-start performance: most requests serve static files, only the contact form submission hits the Node.js runtime

---

## 7. Phased Delivery Plan

### Phase 1 — Home + Foundation (MVP)

**Scope:**

- Astro project setup, folder structure, build pipeline
- `BaseLayout` with persistent nav, footer, theme system (all 4 themes, all pages, persisted via `localStorage`)
- Home page: Hero, Services summary (5 services), featured NuGet packages (3-5, build-time data), case study teasers (placeholder until Phase 2), external links (GitHub, NuGet)
- Privacy policy page (static content)
- Cookie consent banner
- WCAG 2.1 AA compliance across all themes
- Mobile-responsive layout
- Lighthouse mobile ≥ 90
- GitHub Actions CI/CD to Azure Web App
- Rename folder from `apps/web/astar-dev-web` to `apps/web/astar-dev`

**Exit criteria:**

- [ ] Site loads on custom domain with correct SSL
- [ ] All 4 themes apply to all sections on the home page
- [ ] Theme persists across page refresh and revisit
- [ ] Privacy policy page accessible via footer link
- [ ] Cookie consent banner appears on first visit, preference persisted
- [ ] Lighthouse mobile performance ≥ 90
- [ ] All text passes WCAG 2.1 AA contrast ratios in all themes
- [ ] Responsive layout tested at 320px, 768px, 1024px, 1440px widths
- [ ] Zero build warnings, all tests pass

### Phase 2 — Case Studies

**Scope:**

- Case studies listing page (`/case-studies`)
- Individual case study pages (2-3 anonymised engagements)
- Content structure: Problem → Approach → Outcome → Tech Stack
- Case study teaser cards on home page link to full pages
- Content stored in `src/content/case-studies/` as Markdown or HTML

**Exit criteria:**

- [ ] 2-3 case study pages render with correct layout
- [ ] Home page teasers link to full case study pages
- [ ] Themed correctly across all 4 themes
- [ ] Responsive on mobile

### Phase 3 — Contact Form (SendGrid)

**Scope:**

- Contact page with form (Name, Email, Message, "Send me a copy" checkbox)
- Honeypot hidden field for spam protection
- Client-side validation (required fields, email format)
- Server-side API route (`/api/contact`) validates input and sends email via SendGrid
- Success/error feedback on the page (no page reload)
- Rate limiting on the API route (prevent abuse)
- SendGrid account setup (free tier: 100 emails/day)

**Exit criteria:**

- [ ] Form submission sends email to configured address via SendGrid
- [ ] If "Send me a copy" is checked, sender receives a copy
- [ ] Honeypot field rejects bot submissions silently
- [ ] Client-side validation prevents submission of empty/invalid fields
- [ ] Server-side validation rejects malformed requests with appropriate error messages
- [ ] Rate limiting prevents more than 10 submissions per IP per 15 minutes
- [ ] Success message displayed after submission; form resets
- [ ] Error message displayed if SendGrid fails (generic message, no leak of internals)
- [ ] Accessible: form fields have labels, error messages associated with fields, focus management on submit

### Phase 4 — Packages Page

**Scope:**

- Full packages page (`/packages`) displaying all curated NuGet packages
- Grouped by use case (categories defined in `data/packages.json`)
- Each package card shows: name, description, latest version, total downloads, install command, link to NuGet.org
- Metadata fetched from NuGet v3 API at build time
- Home page featured packages (3-5) are a subset, also from the curated list
- Filtering or jump-to-category navigation if the list is long

**Exit criteria:**

- [ ] All curated packages render with correct metadata from NuGet API
- [ ] Packages are grouped by use case with clear headings
- [ ] Each card shows: name, description, version, downloads, `dotnet add package` command
- [ ] NuGet.org links open in new tab
- [ ] Build fails gracefully if NuGet API is unavailable (uses cached/fallback data)
- [ ] Themed and responsive

### Phase 5 — Blog

**Scope:**

- Blog listing page (`/blog`) showing all published posts in reverse chronological order
- Individual blog post pages (`/blog/[slug]`)
- Posts authored as Markdown files in `src/content/blog/`
- Frontmatter schema: `title`, `date`, `summary`, `tags`, `draft`, `readingTime` (auto-calculated)
- Posts with `draft: true` are excluded from production builds
- Tag-based filtering on the listing page
- Code syntax highlighting in blog posts (Astro built-in via Shiki)
- Open Graph meta tags for social sharing

**Exit criteria:**

- [ ] Blog posts render from Markdown with correct formatting
- [ ] Draft posts do not appear in production build
- [ ] Blog listing shows title, date, summary, tags, reading time
- [ ] Tag filtering works
- [ ] Code blocks have syntax highlighting
- [ ] Social sharing meta tags render correctly (testable via Open Graph debugger)
- [ ] Themed and responsive

---

## 8. Page Specifications

### 8.1 Home Page (`/`)

#### Layout (top to bottom)

1. **Hero section**
    - Logo (theme-appropriate variant)
    - Headline: Professional on dark/light themes; personality-driven on metal/polished themes
    - Subtitle: 1-2 sentences summarising who you are and what you do
    - Eyebrow text (e.g., skill tags: TDD, Clean Architecture, Observability)
    - CTA buttons: "Explore services" (anchor to services section), "Get in touch" (links to `/contact`)

2. **Services section**
    - Heading: contextual (e.g., "What I offer")
    - 5 service cards in a responsive grid:
        - Fullstack Development
        - Mentoring
        - Code Reviews
        - Architecture Design
        - Backend Development
    - Each card: title, short description (2-3 sentences)

3. **Featured NuGet Packages section**
    - Heading (e.g., "Open Source Packages")
    - 3-5 package cards (subset of curated list)
    - Each card: package name, short description, latest version, download count, install command
    - "View all packages →" link to `/packages`

4. **Case Study Highlights section**
    - Heading (e.g., "Selected Work")
    - 2-3 teaser cards linking to full case study pages
    - Each card: project title, one-sentence summary, tech stack tags
    - Links to `/case-studies/[slug]`

5. **Call-to-Action banner**
    - Short prompt (e.g., "Interested in working together?")
    - Button linking to `/contact`

6. **Footer**
    - Copyright notice
    - Links: Privacy Policy, GitHub, NuGet
    - Site attribution

#### Copy Variants by Theme

| Element  | Dark / Light                                             | Metal                                                               | Polished                                                                 |
| -------- | -------------------------------------------------------- | ------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Headline | Professional (e.g., "Senior .NET Developer & Architect") | Personality-driven (e.g., "Production systems that play at volume") | Personality-driven (e.g., "Precision-engineered .NET, polished to ship") |
| Subtitle | Straightforward summary of skills and services           | Band-energy metaphor                                                | K-pop precision metaphor                                                 |
| Eyebrow  | Skill keywords                                           | "VOLUME // ELEVEN"                                                  | "PRECISION // POLISH"                                                    |

### 8.2 Packages Page (`/packages`)

- Heading and short intro paragraph
- Packages grouped by use-case category (categories defined in `data/packages.json`)
- Category headings with optional one-line description
- Package cards within each category (same layout as home page cards but full-width grid)
- Jump-to-category navigation if > 3 categories

### 8.3 Blog Listing (`/blog`)

- Heading
- Optional tag filter bar (horizontal pills/chips)
- Post cards in a single-column or two-column list:
    - Title (links to post)
    - Date (formatted, e.g., "28 March 2026")
    - Summary / excerpt (from frontmatter)
    - Tags (pill badges)
    - Estimated reading time
- Posts ordered by date descending
- Drafts excluded

### 8.4 Blog Post (`/blog/[slug]`)

- Title
- Date, reading time, tags
- Markdown-rendered content with:
    - Headings, paragraphs, lists, blockquotes
    - Code blocks with syntax highlighting (Shiki)
    - Images (if included)
    - Links
- "Back to blog" navigation link
- Open Graph / Twitter Card meta tags

### 8.5 Case Studies Listing (`/case-studies`)

- Heading and intro paragraph
- 2-3 case study cards (title, one-line summary, tech stack tags, link to full page)

### 8.6 Case Study Page (`/case-studies/[slug]`)

- Title
- Structured sections:
    - **The Problem** — context and pain points (anonymised client)
    - **The Approach** — what was done, methodology, decisions
    - **The Outcome** — measurable results, improvements
    - **Tech Stack** — technologies used (displayed as tags/pills)
- "Back to case studies" navigation link

### 8.7 Contact Page (`/contact`)

- Heading and short intro (e.g., "Let's talk about your next project")
- Form fields:
    - Name (required, text)
    - Email (required, email validation)
    - Message (required, textarea)
    - "Send me a copy of this message" (optional, checkbox)
    - Honeypot field (hidden, `aria-hidden="true"`, `tabindex="-1"`)
- Submit button
- Success state: green confirmation message, form resets
- Error state: red error message with guidance
- Accessibility: all fields labelled, errors programmatically associated, focus moves to first error on validation failure

### 8.8 Privacy Policy (`/privacy`)

- Static page covering:
    - What data is collected (contact form submissions: name, email, message)
    - Analytics data collected (Azure Application Insights — anonymised usage data)
    - Cookie usage (theme preference in `localStorage`, App Insights cookies, consent cookie)
    - Data retention policy
    - User rights under UK GDPR (access, rectification, erasure, portability)
    - Contact details for data-related requests
    - Last updated date

---

## 9. Theming System

### Themes

| Theme    | Default?        | CSS Class        | Personality                           |
| -------- | --------------- | ---------------- | ------------------------------------- |
| Dark     | ✓ (first visit) | `theme-dark`     | Professional, clean, dark background  |
| Light    |                 | `theme-light`    | Professional, clean, light background |
| Metal    |                 | `theme-metal`    | High-energy, bold, Metallica-inspired |
| Polished |                 | `theme-polished` | Refined, precise, BlackPink-inspired  |

### Implementation

- All colours defined as **CSS custom properties** in `src/styles/themes.css`
- Theme class applied to `<html>` element
- Theme persisted in `localStorage` under key `theme`
- On first visit (no stored preference), default to `dark`
- On page load, theme is read from `localStorage` and applied **before first paint** (inline script in `<head>` to prevent flash of wrong theme)
- Theme switcher is a Vue component with `client:load` hydration directive
- **Every** colour in every component must use CSS custom properties — no hard-coded colour values anywhere

### WCAG 2.1 AA Contrast Requirements

Every theme palette must satisfy:

- **Normal text** (< 18pt / < 14pt bold): contrast ratio ≥ 4.5:1
- **Large text** (≥ 18pt / ≥ 14pt bold): contrast ratio ≥ 3:1
- **UI components and graphical objects**: contrast ratio ≥ 3:1

The existing theme colours must be audited and adjusted where they fail. In particular:

- Light theme: tag backgrounds need review (dark text on light bg)
- Metal theme: ensure blue accent on black background meets ratio
- Polished theme: ensure pink accent on white background meets ratio

### Theme-Dependent Copy

The Hero section renders different copy depending on the active theme:

- **Dark / Light:** Professional, straightforward messaging
- **Metal:** Band-energy metaphors, bold language
- **Polished:** Precision/polish metaphors, refined language

This is implemented by conditionally rendering copy blocks based on the current theme value, not by duplicating components.

---

## 10. Blog Engine

### Content Collection Schema

Blog posts live in `src/content/blog/` as `.md` files.

**Frontmatter schema:**

```yaml
---
title: "Post title" # Required, string
date: 2026-03-28 # Required, date (ISO 8601)
summary: "A short excerpt" # Required, string (used on listing page and Open Graph)
tags: ["dotnet", "tdd"] # Required, string array
draft: false # Optional, boolean, default false
---
```

**Derived fields (computed at build time):**

- `readingTime`: Estimated minutes, calculated from word count (~200 words/min)
- `slug`: Derived from filename

### Build Behaviour

- Posts with `draft: true` are **excluded** from production builds (`astro build`)
- Posts with `draft: true` are **included** in dev builds (`astro dev`) for preview
- Posts are sorted by `date` descending on the listing page

### Syntax Highlighting

Astro uses Shiki by default for code block highlighting. No additional configuration needed. Ensure the Shiki theme works acceptably across all four site themes (may need a neutral code theme like `github-dark-dimmed`).

---

## 11. NuGet Package Display

### Data Source

A curated list of NuGet package IDs is maintained in `src/data/packages.json`:

```json
{
    "featured": ["AStar.Dev.Package.One", "AStar.Dev.Package.Two", "AStar.Dev.Package.Three"],
    "categories": [
        {
            "name": "Data Access",
            "description": "Packages for database and data layer concerns",
            "packages": ["AStar.Dev.Package.One", "AStar.Dev.Package.Four"]
        },
        {
            "name": "HTTP & APIs",
            "description": "HTTP clients, middleware, and API utilities",
            "packages": ["AStar.Dev.Package.Two", "AStar.Dev.Package.Five"]
        }
    ]
}
```

### Build-Time Fetch

At build time, for each package ID in the curated list:

1. Fetch package metadata from the NuGet v3 API: `https://api.nuget.org/v3/registration5-gz-semver2/{id}/index.json`
2. Extract: latest stable version, description, total download count, project URL, tags
3. Cache the fetched data so the build is repeatable and does not fail if NuGet is temporarily unavailable

**Fallback:** If the NuGet API is unreachable during build, the build should:

- Use previously cached data if available (stored in a `.cache/` directory, gitignored)
- Log a warning but **not** fail the build
- If no cache exists and the API is down, fail the build with a clear error message

### Display

**Home page (featured):** 3-5 cards from the `featured` list.

**Packages page (all):** All curated packages, grouped by category.

**Each package card shows:**

- Package name
- Short description (from NuGet metadata, truncated if needed)
- Latest stable version (badge style)
- Total download count (formatted with commas/abbreviations)
- Install command: `dotnet add package {name}` (in a copyable code block)
- Link to NuGet.org page (opens in new tab)

---

## 12. Contact Form & SendGrid

### SendGrid Integration

- **Service tier:** SendGrid free tier (100 emails/day — more than sufficient)
- **API key:** Stored as Azure Web App Application Setting `SENDGRID_API_KEY`
- **Recipient email:** Stored as Application Setting `CONTACT_EMAIL` (placeholder during development)

### Form Submission Flow

```
User fills form → Client-side validation → POST /api/contact
  → Server validates input
  → Server checks honeypot field (if filled → 200 OK silently, no email sent)
  → Server sends email via SendGrid API
  → If "send me a copy" checked → send copy to user's email
  → Return success/error response
  → Client displays feedback
```

### Validation Rules

**Client-side (immediate feedback):**

| Field    | Rule                                                                   |
| -------- | ---------------------------------------------------------------------- |
| Name     | Required, non-empty after trim, max 200 characters                     |
| Email    | Required, valid email format                                           |
| Message  | Required, non-empty after trim, min 10 characters, max 5000 characters |
| Honeypot | Must be empty                                                          |

**Server-side (re-validate everything):**

- All client-side rules re-checked
- Email format validated with stricter regex
- Request body size limited (< 10KB)
- Rate limit: 10 requests per IP per 15 minutes

### Email Format

**To site owner:**

- From: configured SendGrid sender identity
- Subject: `[AStar.Dev Contact] Message from {name}`
- Body: Name, email, message, timestamp

**Copy to sender (if opted in):**

- From: configured SendGrid sender identity
- Subject: `Copy of your message to AStar Development`
- Body: Confirmation text + their original message

### Error Handling

| Scenario             | User sees                                                | Logged                          |
| -------------------- | -------------------------------------------------------- | ------------------------------- |
| Validation failure   | Specific field errors                                    | No (client-side)                |
| Honeypot triggered   | "Thank you" (fake success)                               | Yes (warning)                   |
| Rate limit exceeded  | "Too many requests, please try again later"              | Yes (warning)                   |
| SendGrid API failure | "Something went wrong. Please email [address] directly." | Yes (error + SendGrid response) |
| Unknown server error | "Something went wrong. Please try again later."          | Yes (error + stack trace)       |

---

## 13. Analytics — Azure Application Insights

### Integration

- Application Insights JavaScript SDK loaded via `BaseLayout.astro`
- Connection string stored as build-time environment variable `PUBLIC_APPINSIGHTS_CONNECTION_STRING` (Astro exposes `PUBLIC_` prefixed env vars to the client)
- **Only loaded after cookie consent is granted** (see Section 14)

### What Is Tracked

- Page views (automatic)
- Page load performance (automatic)
- Client-side exceptions (automatic)
- Custom events: contact form submission (success/failure)

### What Is NOT Tracked

- No personally identifiable information (PII) beyond what App Insights collects by default
- IP addresses should be anonymised (configure `disableCookiesUsage` until consent, `enableAutoRouteTracking` for SPA-like navigation)

---

## 14. Legal & Compliance (UK GDPR)

### Cookie Consent Banner

- Displayed on first visit (no consent cookie found)
- Positioned at bottom of viewport, does not obscure main content
- Text explains: "This site uses cookies for analytics and to remember your preferences."
- Two actions: **"Accept"** and **"Reject"** (must be equally prominent — no dark patterns)
- Consent choice stored in `localStorage` under key `cookie-consent` (`accepted` | `rejected`)
- If rejected: Application Insights is **not** loaded; theme preference still stored (localStorage is not a cookie and is functionally necessary)
- If accepted: Application Insights loads
- User can change their choice via a link in the footer ("Cookie preferences")

### Privacy Policy Page

Must cover (see Section 8.8 for full outline):

- Data controller identity and contact details
- What data is collected and why (lawful basis)
- Third-party processors (SendGrid for email, Microsoft for App Insights)
- Data retention periods
- User rights under UK GDPR
- How to make a subject access request
- Last updated date

### Data Processing

| Data                                | Purpose            | Lawful Basis            | Retention                                                        |
| ----------------------------------- | ------------------ | ----------------------- | ---------------------------------------------------------------- |
| Contact form (name, email, message) | Respond to enquiry | Legitimate interest     | Deleted after enquiry resolved or 12 months, whichever is sooner |
| Analytics (page views, performance) | Improve site       | Consent (cookie banner) | App Insights default (90 days)                                   |
| Theme preference                    | User experience    | Functional necessity    | Indefinite (localStorage, user can clear)                        |
| Cookie consent choice               | Legal compliance   | Legal obligation        | Indefinite (localStorage, user can change)                       |

---

## 15. Accessibility — WCAG 2.1 AA

### Requirements

All pages, across all four themes, must meet WCAG 2.1 Level AA. Key criteria:

| WCAG Criterion               | Requirement                                                          | Applies To                 |
| ---------------------------- | -------------------------------------------------------------------- | -------------------------- |
| 1.1.1 Non-text Content       | All images have meaningful `alt` text or are marked decorative       | All pages                  |
| 1.3.1 Info and Relationships | Semantic HTML: headings in order, lists for lists, forms with labels | All pages                  |
| 1.4.3 Contrast (Minimum)     | Text contrast ≥ 4.5:1 (normal) / ≥ 3:1 (large)                       | All themes                 |
| 1.4.11 Non-text Contrast     | UI components and borders ≥ 3:1                                      | All themes                 |
| 2.1.1 Keyboard               | All interactive elements reachable and operable via keyboard         | Nav, forms, theme switcher |
| 2.4.1 Bypass Blocks          | Skip-to-content link                                                 | All pages                  |
| 2.4.2 Page Titled            | Unique, descriptive `<title>` per page                               | All pages                  |
| 2.4.4 Link Purpose           | Link text describes destination (no "click here")                    | All pages                  |
| 2.4.7 Focus Visible          | Visible focus indicator on all interactive elements                  | All themes                 |
| 3.1.1 Language of Page       | `lang="en"` on `<html>`                                              | All pages                  |
| 3.3.1 Error Identification   | Form errors identified and described in text                         | Contact form               |
| 3.3.2 Labels or Instructions | All form fields have visible labels                                  | Contact form               |
| 4.1.2 Name, Role, Value      | ARIA attributes where semantic HTML is insufficient                  | Theme switcher, mobile nav |

### Testing

- Automated: axe-core or Lighthouse accessibility audit in CI (score ≥ 90)
- Manual: keyboard-only navigation test, screen reader spot-check (VoiceOver or NVDA)

---

## 16. Performance Budget

### Targets

| Metric                            | Target                | Tool          |
| --------------------------------- | --------------------- | ------------- |
| Lighthouse Performance (mobile)   | ≥ 90                  | Lighthouse CI |
| Lighthouse Accessibility (mobile) | ≥ 90                  | Lighthouse CI |
| Lighthouse Best Practices         | ≥ 90                  | Lighthouse CI |
| Lighthouse SEO                    | ≥ 90                  | Lighthouse CI |
| First Contentful Paint (3G)       | < 2.0s                | Lighthouse    |
| Largest Contentful Paint (3G)     | < 2.5s                | Lighthouse    |
| Cumulative Layout Shift           | < 0.1                 | Lighthouse    |
| Total page weight (home)          | < 500KB (transferred) | DevTools      |
| JavaScript shipped (home)         | < 50KB (transferred)  | DevTools      |

### How Astro Achieves This

- Zero JS by default for static pages
- Only interactive components (theme switcher, contact form, cookie consent, App Insights) ship JavaScript
- Images optimised via Astro's built-in `<Image>` component (automatic WebP/AVIF, lazy loading, width/height attributes to prevent CLS)
- CSS inlined or loaded per-page (no monolithic bundle)
- Fonts: use system font stack or self-hosted subset — no Google Fonts CDN dependency

---

## 17. Mobile Responsiveness

### Breakpoints

| Name    | Width          | Target                 |
| ------- | -------------- | ---------------------- |
| Mobile  | < 640px        | Phones                 |
| Tablet  | 640px – 1024px | Tablets, small laptops |
| Desktop | > 1024px       | Laptops, desktops      |

### Key Responsive Behaviours

| Component        | Mobile              | Tablet                  | Desktop                        |
| ---------------- | ------------------- | ----------------------- | ------------------------------ |
| Navigation       | Hamburger menu      | Hamburger or horizontal | Horizontal bar                 |
| Hero             | Stacked, full-width | Stacked, constrained    | As designed                    |
| Service cards    | Single column       | 2 columns               | 3 or 5 columns (flexible grid) |
| Package cards    | Single column       | 2 columns               | 3 columns                      |
| Case study cards | Single column       | 2 columns               | 2-3 columns                    |
| Contact form     | Full-width, stacked | 2-column (text + form)  | 2-column                       |
| Blog listing     | Single column       | Single or 2 columns     | 2 columns                      |
| Footer           | Stacked links       | Horizontal              | Horizontal                     |

### Touch Targets

All interactive elements (buttons, links, form controls) must have a minimum touch target of 44×44 CSS pixels (WCAG 2.5.5).

---

## 18. Non-Functional Requirements

| Requirement               | Specification                                                                                                           |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| **Browser support**       | Latest 2 versions of Chrome, Firefox, Safari, Edge. No IE11.                                                            |
| **Build time**            | < 60 seconds for full production build                                                                                  |
| **Zero-downtime deploys** | GitHub Actions deploys to Azure Web App with slot swap or rolling update                                                |
| **Error handling**        | No stack traces or internal details exposed to users in production                                                      |
| **Security headers**      | CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy (configured via Astro middleware or Azure Web App config) |
| **Sitemap**               | Auto-generated `sitemap.xml` via `@astrojs/sitemap` integration                                                         |
| **robots.txt**            | Allow all crawlers, reference sitemap                                                                                   |
| **Open Graph tags**       | All pages have `og:title`, `og:description`, `og:image`, `og:url`                                                       |
| **Favicon**               | Properly configured with multiple sizes (Astro handles this)                                                            |
| **404 page**              | Custom styled 404 page matching the active theme                                                                        |

---

## 19. Configuration & Secrets

### Environment Variables

| Variable                               | Where Used               | Stored In                          | Example                      |
| -------------------------------------- | ------------------------ | ---------------------------------- | ---------------------------- |
| `SENDGRID_API_KEY`                     | Server (API route)       | Azure App Settings, `.env` locally | `SG.xxxx`                    |
| `CONTACT_EMAIL`                        | Server (API route)       | Azure App Settings, `.env` locally | `hello@example.com`          |
| `PUBLIC_APPINSIGHTS_CONNECTION_STRING` | Client (browser)         | Azure App Settings, `.env` locally | `InstrumentationKey=xxx;...` |
| `SITE_URL`                             | Build (sitemap, OG tags) | Azure App Settings, `.env` locally | `https://yourdomain.com`     |

### Local Development

- `.env.example` checked into repo with placeholder values and comments
- `.env` gitignored (contains real local values)
- Astro loads `.env` automatically in dev mode

### Azure Configuration

- Set via Azure Portal → Web App → Configuration → Application Settings
- GitHub Actions can set these during deployment via `az webapp config appsettings set` if preferred

---

## 20. Affected Files & Migration Notes

### Folder Rename

`apps/web/astar-dev-web` → `apps/web/astar-dev`

### Files Removed

The entire existing codebase is replaced. Key removals:

- `server/` directory (Express server — replaced by Astro server endpoints)
- `client/` directory (Vue SPA — replaced by Astro pages/components)
- Root `package.json` workspace configuration (no longer a monorepo workspace with client/server)
- `tests/client/smoke.test.ts` and `tests/server/smoke.test.ts` (empty smoke tests — replaced with real tests)

### Files / Patterns Carried Forward

- **Theme CSS variables** from `client/src/theme.css` — adapted and extended to cover all components (fixing the current bug where sections below the hero have hard-coded colours)
- **Theme switcher logic** from `client/src/composables/useTheme.ts` — adapted into a Vue component with Astro `client:load` hydration
- **Logo assets** from `client/public/assets/` — carried to `public/assets/`
- **Content structure** (services list, engagement tracks) — updated with real content

### CI/CD Updates

- GitHub Actions workflow must be updated to build Astro instead of Vue + Express
- Build command changes from `npm run build:client && npm run build:server` to `npm run build` (Astro)
- Deploy artefact changes from `server/dist` + `client/dist` to `dist/` (Astro output)

---

## 21. Risks & Assumptions

| #   | Risk / Assumption                                                                                                                                        | Type       | Impact                                                                                                                                                                                            | Mitigation                                                                                                       |
| --- | -------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| 1   | **Confirmed:** Azure Web App is on B1 tier, which supports Node.js SSR and "Always On" (currently off to save cost). Cold-start on B1 is typically 2-4s. | Resolved   | Only `/api/contact` hits Node.js; all other pages are static. Cold-start only affects first contact form submission after idle period. Consider enabling the Azure health check ping to mitigate. | N/A — confirmed                                                                                                  |
| 2   | **Assumption:** SendGrid free tier (100 emails/day) is sufficient                                                                                        | Assumption | Low — site is unlikely to receive 100 contact submissions per day                                                                                                                                 | Monitor usage; upgrade if needed                                                                                 |
| 3   | **Risk:** NuGet API is unavailable during build                                                                                                          | Risk       | Build uses stale data or fails                                                                                                                                                                    | Build-time cache with fallback (see Section 11)                                                                  |
| 4   | **Assumption:** Custom domain can remain on Azure Web App regardless of hosting changes                                                                  | Assumption | Medium — domain was purchased from Microsoft                                                                                                                                                      | Verify domain DNS configuration before any hosting changes                                                       |
| 5   | **Risk:** Theme colour palettes may not all pass WCAG 2.1 AA after audit                                                                                 | Risk       | Requires palette adjustments that may alter the visual identity of metal/polished themes                                                                                                          | Audit early in Phase 1; adjust specific shades while preserving the overall feel                                 |
| 6   | **Assumption:** Blog content will be authored by the site owner directly in Markdown via git commits                                                     | Assumption | Low — this is the agreed workflow                                                                                                                                                                 | If the workflow proves too cumbersome, a lightweight CMS (e.g., Tina) can be added later without re-architecting |
| 7   | **Risk:** Astro's Vue integration may have limitations for complex interactive components                                                                | Risk       | Low — only 3 components need hydration (theme switcher, contact form, cookie consent)                                                                                                             | These are simple components; Astro's Vue integration is mature for this scope                                    |
| 8   | **Assumption:** The site owner will provide real content (services descriptions, case studies, curated package list) before each phase launches          | Assumption | Phases blocked if content is not ready                                                                                                                                                            | Content templates provided in the spec; can launch with placeholder content and iterate                          |

---

## 22. Open Questions

- [ ] **What is the custom domain?** Needed for sitemap, OG tags, and `SITE_URL` configuration. — Owner: stakeholder
- [ ] **Which specific NuGet packages should be in the curated list and how should they be categorised?** A first draft of `packages.json` is needed before Phase 4. — Owner: stakeholder
- [ ] **What SendGrid sender identity / from-address should be used?** SendGrid requires a verified sender. — Owner: stakeholder (after creating SendGrid account)
- [x] **Are the existing logo assets (`astar.png`, `metal-logo.png`, `polished-logo.png`) final, or should new assets be created?** — Resolved: existing assets are fine for launch; new branding is out of budget.
- [x] **What is the Azure Web App plan tier?** — Resolved: B1 tier. Supports Node.js SSR, "Always On" available but currently off.
- [ ] **Should the GitHub Actions workflow be a new file or replace the existing `azure-deploy.yml` referenced in the README?** — Owner: stakeholder + developer

---

_End of specification._

# Design Specification — AStar Development (Phase 1 MVP)

> **Concept:** Precision
> **Scope:** Phase 1 only — Base Layout, Home page, Privacy Policy page, Cookie Consent banner.
> **Framework:** Astro (hybrid output). Static `.astro` components unless interactivity is required; interactive components are `.vue` with `client:load`.
> **Themes in scope for this document:** `theme-dark` and `theme-light`.

---

## Table of Contents

- [1. Design Tokens](#1-design-tokens)
- [2. Component Inventory](#2-component-inventory)
- [3. Persistent Navigation Bar](#3-persistent-navigation-bar)
- [4. Home Page](#4-home-page)
  - [4.1 Hero Section](#41-hero-section)
  - [4.2 Services Section](#42-services-section)
  - [4.3 Featured NuGet Packages Section](#43-featured-nuget-packages-section)
  - [4.4 Case Study Teasers Section](#44-case-study-teasers-section)
  - [4.5 CTA Banner Section](#45-cta-banner-section)
- [5. Footer](#5-footer)
- [6. Privacy Policy Page](#6-privacy-policy-page)
- [7. Cookie Consent Banner](#7-cookie-consent-banner)
- [8. Responsive Behaviour Summary](#8-responsive-behaviour-summary)
- [9. Theme Switching — No-Flash Strategy](#9-theme-switching--no-flash-strategy)
- [10. Open Questions](#10-open-questions)

---

## 1. Design Tokens

The existing `theme.css` uses hard-coded hex values in scoped component CSS. All colours must move to CSS custom properties before any component work begins. The following tokens are **additions** to the existing set and must be declared inside both `.theme-dark` and `.theme-light` in `src/styles/themes.css`.

| Token | Dark value | Light value | Purpose |
|---|---|---|---|
| `--surface-raised` | `#102233` *(existing `--panel-bg`)* | `#f5f5f5` *(existing `--panel-bg`)* | Alternating section background, card background |
| `--surface-base` | `#08131f` *(existing `--background`)* | `#ffffff` *(existing `--background`)* | Primary section background |
| `--text-muted` | `#8da2b7` | `#6b7280` | Secondary text, metadata rows |
| `--text-inverse` | `#08131f` | `#ffffff` | Text on filled accent buttons |
| `--border` | `#2a3f55` *(existing `--panel-border`)* | `#cccccc` *(existing `--panel-border`)* | Unified border token name |
| `--terminal-bg` | `#0a1a2a` | `#1a1a2e` | Code panel background — intentionally dark in both themes |
| `--terminal-text` | `#d5e3f0` | `#c9d6e3` | Code panel body text |
| `--terminal-accent` | `#2dd4bf` | `#4cc9f0` | Highlighted tokens inside the code panel |
| `--focus-ring` | `#2dd4bf` | `#0066cc` | Consistent focus indicator colour |
| `--btn-primary-bg` | `#2dd4bf` | `#0066cc` | Solid CTA button background |
| `--btn-primary-text` | `#08131f` | `#ffffff` | Solid CTA button label |
| `--badge-bg` | `rgba(45, 212, 191, 0.12)` | `rgba(0, 102, 204, 0.10)` | Status badge background |
| `--badge-border` | `rgba(45, 212, 191, 0.40)` | `rgba(0, 102, 204, 0.35)` | Status badge border |
| `--badge-text` | `#2dd4bf` | `#0055aa` | Status badge label — see contrast note below |
| `--cta-border-left` | `#2dd4bf` | `#0066cc` | CTA inset-box left accent border |
| `--active-indicator` | `#2dd4bf` | `#0066cc` | Nav active-page underline |
| `--nav-bg` | `#102233` | `#ffffff` | Nav bar background |
| `--nav-border` | `#2a3f55` | `#e5e7eb` | Nav bar bottom border |
| `--section-border` | `#1e3347` | `#e5e7eb` | Between-section `1px` rule |
| `--footer-bg` | `#091a2b` | `#f0f0f0` | Footer background — one step deeper than `--surface-raised` |
| `--copy-btn-hover-bg` | `rgba(45, 212, 191, 0.10)` | `rgba(0, 102, 204, 0.08)` | Copy command button hover state |

### Contrast Notes

> These must be verified in a contrast checker before implementation begins.

- **`--badge-text` light theme:** `#0055aa` (not `#0066cc`) against an effective background of approximately `#e6f0fa` (white + `rgba(0,102,204,0.10)` wash) achieves ~5.4:1. Using `#0066cc` on the same background achieves ~4.6:1 — both pass AA, but `#0055aa` provides more headroom.
- **`--btn-primary-text` dark theme:** `#08131f` on `#2dd4bf` — contrast ratio ~10.2:1. Excellent.
- **`--btn-primary-text` light theme:** `#ffffff` on `#0066cc` — contrast ratio ~4.56:1. Passes AA for normal text at button size.

---

## 2. Component Inventory

| Component | File type | Hydration directive | Reason for client boundary |
|---|---|---|---|
| `Nav.astro` | Astro static | None | Links are static HTML |
| `ThemeSwitcher.vue` | Vue interactive | `client:load` | Reads/writes `localStorage`, toggles `<html>` class |
| `MobileMenu.vue` | Vue interactive | `client:load` | Controls open/closed drawer state |
| `Hero.astro` | Astro static | None | Purely markup and CSS |
| `StatusBadge.astro` | Astro static | None | Informational display only |
| `ServiceCard.astro` | Astro static | None | No interaction |
| `PackageCard.astro` | Astro static | None | Shell is static; copy button is a nested client component |
| `CopyButton.vue` | Vue interactive | `client:visible` | Clipboard API; hydrates only when scrolled into view |
| `CaseStudyTeaser.astro` | Astro static | None | Placeholder content, no interaction in Phase 1 |
| `CtaBanner.astro` | Astro static | None | Static markup |
| `Footer.astro` | Astro static | None | Static markup |
| `CookieConsent.vue` | Vue interactive | `client:load` | Reads/writes `localStorage`, conditionally renders |
| `BaseLayout.astro` | Astro layout | None | Wraps interactive children; contains no-flash theme script |

---

## 3. Persistent Navigation Bar

**Components:** `Nav.astro`, `ThemeSwitcher.vue`, `MobileMenu.vue`

### Purpose

Present on every page. Tells the user where they are (active page indicator), provides global navigation across all five pages, and gives immediate access to the theme switcher.

### Layout

A single horizontal bar spanning the full viewport width. Three zones within a `1120px` max-width centred container with `24px` horizontal padding.

#### Left Zone — Brand

- AStar logo mark (inline SVG or Astro `<Image>` optimised), `40px` tall, `alt="AStar Development"`.
- To the right of the logo: "AStar Development" in `--text`, `font-weight: 600`; and beneath it "Software Architecture & Development" in `--text-muted`, `font-size: 0.75rem`.
- The entire brand group is wrapped in `<a href="/">`.
- **Note:** The brand identity moves from the hero topbar (where it lived in the Vue site) into this persistent nav — it is always visible regardless of scroll position.

#### Centre Zone — Page Links *(desktop only)*

Links: Home · Packages · Blog · Case Studies · Contact

- Each link: `<a>` in `--text`, `font-weight: 500`, `font-size: 0.9rem`.
- **Active state:** A `3px solid --active-indicator` bottom border flush with the bottom edge of the nav bar. To avoid layout shift, every link has `border-bottom: 3px solid transparent` as its default state, transitioning to `--active-indicator` when active. The active page is determined at build time in Astro via `Astro.url.pathname` — no JavaScript required.
- **Hover state:** `opacity: 0.75` on the link text. No colour change — avoids distraction.

#### Right Zone — Actions

- `ThemeSwitcher.vue`: a two-segment control (not a `<select>`). A moon icon button (dark theme) and a sun icon button (light theme). The active segment has `background: --surface-raised`, `border: 1px solid --border`. Inactive segment: transparent. Icons are inline SVGs, `16px`. The group wrapper has `role="group" aria-label="Choose colour theme"`. Each button has an `aria-label` ("Switch to dark theme" / "Switch to light theme") and `aria-pressed` to indicate the active state.
- A vertical `1px` separator in `--border` between the theme switcher and the icon links.
- GitHub icon link: `aria-label="GitHub profile, opens in new tab"`, `target="_blank" rel="noopener noreferrer"`.
- NuGet icon link: `aria-label="NuGet profile, opens in new tab"`, `target="_blank" rel="noopener noreferrer"`.
- Both icons: inline SVGs, `20px`, `color: --text-muted`, hover `color: --accent`.

#### Mobile (≤ 767px)

- Centre zone (page links) collapses completely.
- Right zone: only the hamburger menu button is visible.
- Hamburger button: `aria-label="Open navigation menu"`, `aria-expanded` toggled by `MobileMenu.vue`, `aria-controls` pointing to the drawer panel `id`.
- The drawer slides in from the right, full viewport height, `background: --nav-bg`, `border-left: 1px solid --border`. Contents (top to bottom): page links stacked with `min-height: 48px` touch targets; a `1px` divider in `--border`; theme switcher (horizontal pill group); GitHub and NuGet icon links.
- Drawer closes on: the close (×) button, pressing `Escape`, or clicking the scrim overlay (`background: rgba(0,0,0,0.4)`).
- Focus is trapped inside the drawer while open. On close, focus returns to the hamburger button.

### Nav Bar Container Styles

| Property | Value |
|---|---|
| Background | `--nav-bg` |
| Bottom border | `1px solid --nav-border` |
| Position | `sticky`, `top: 0`, `z-index: 100` |
| Height | `64px` desktop / `56px` mobile |
| Transition | `background 0.25s, border-color 0.25s` |

### States

| State | Description |
|---|---|
| Default | As described above |
| Active page | `--active-indicator` bottom border on the current link |
| Mobile menu open | Drawer visible, scrim overlay, hamburger becomes × button |
| Theme switching | All CSS custom property consumers transition at `0.25s` |

### Accessibility Notes

- All three zones sit inside `<nav aria-label="Main navigation">`.
- A visually hidden skip link — `<a href="#main-content" class="skip-link">Skip to main content</a>` — is the first focusable element in `BaseLayout.astro`. It becomes visible on focus. This is not part of `Nav.astro`.
- All icon-only interactive elements have explicit `aria-label` values.
- Mobile drawer is a disclosure widget: `aria-expanded` + `aria-controls` on the trigger; matching `id` on the panel.

---

## 4. Home Page

**Route:** `/`

### Purpose

Convert all three personas within 30 seconds. Engineering leaders assess credibility via services and case studies. Recruiters scan the headline and availability badges. Developers find packages.

### Rendering Strategy

Astro static (pre-rendered at build time). Client components: `ThemeSwitcher.vue` (`client:load`), `MobileMenu.vue` (`client:load`), `CopyButton.vue` instances inside package cards (`client:visible`), `CookieConsent.vue` (`client:load`). NuGet API calls for featured package metadata happen in the Astro page frontmatter — at build time, not in the browser.

### Page-Level Layout

Sections alternate between `--surface-base` and `--surface-raised` backgrounds. Each pair of adjacent sections is separated by a `1px solid --section-border` rule — not a margin gap. This creates clean visual chunking without shadow or large padding jumps.

```
[Nav bar — sticky, z-index: 100]
─────────────────────────────────────────────────────
[Hero          — background: --surface-base      ]
[Services      — background: --surface-raised    ]
[Packages      — background: --surface-base      ]
[Case Studies  — background: --surface-raised    ]
[CTA Banner    — background: --surface-base      ]
─────────────────────────────────────────────────────
[Footer        — background: --footer-bg         ]
```

All sections share a `1120px` max-width centred container with `24px` horizontal padding. Section vertical padding: `64px` top and bottom.

---

### 4.1 Hero Section

#### Layout

Two-column split on desktop (≥ 1024px): left column ~55% width, right column ~45%, `gap: 48px`. Single centred container, `max-width: 1120px`.

| Viewport | Layout |
|---|---|
| ≥ 1024px | Two-column split, 55 / 45 |
| 768px–1023px | Single column; terminal panel centred at ~80% width beneath the text block |
| < 768px | Single column; terminal panel hidden entirely |

The terminal panel is decorative context — hiding it on mobile loses no essential content.

#### Left Column — Text Block

Elements from top to bottom:

**1. Status Badges**

Two `StatusBadge.astro` components, `display: inline-flex`, `gap: 8px`.

- `● Available for contracts`
- `◆ Open source contributor`

Each badge:

| Property | Value |
|---|---|
| Background | `--badge-bg` |
| Border | `1px solid --badge-border` |
| Border radius | `999px` |
| Padding | `4px 10px` |
| Font size | `0.75rem` |
| Font weight | `600` |
| Color | `--badge-text` |

The bullet and diamond are decorative inline SVGs, `aria-hidden="true"`. The badge `<span>` elements are purely informational — no interactive role.

**2. H1 Headline**

| Property | Value |
|---|---|
| Font size | `clamp(2rem, 4vw, 3.2rem)` |
| Font weight | `700` |
| Line height | `1.15` |
| Color | `--text` |
| Alignment | Left |
| Max lines | 2 |

Copy (dark / light themes): **"Senior .NET Engineer & Architect"**

**3. Subtitle Paragraph**

| Property | Value |
|---|---|
| Font size | `1rem` |
| Line height | `1.7` |
| Color | `--text-muted` |
| Margin top | `16px` |
| Max sentences | 2 |

Example copy: *"I design and build production-grade .NET systems — architecture, pipelines, observability, and the team patterns that make them last. Available for contracts and consultancy engagements."*

**4. Skill Tags Row**

3–5 pill-shaped informational tags, `display: flex`, `flex-wrap: wrap`, `gap: 8px`, `margin-top: 24px`.

| Property | Value |
|---|---|
| Border | `1px solid --border` |
| Background | `transparent` |
| Color | `--text-muted` |
| Font size | `0.78rem` |
| Border radius | `999px` |
| Padding | `4px 12px` |

Tags are static `<span>` elements — not interactive filters. Example set: `TDD` · `Clean Architecture` · `Observability` · `CI/CD` · `.NET 9`

**5. CTA Button Row**

`display: flex`, `gap: 16px`, `flex-wrap: wrap`, `margin-top: 32px`, left-aligned.

**Primary button** — "View my services", links to `#services`:

| Property | Value |
|---|---|
| Background | `--btn-primary-bg` |
| Color | `--btn-primary-text` |
| Border | `2px solid --btn-primary-bg` |
| Border radius | `8px` *(rectangular with rounded corners — signals primary action more clearly than a pill)* |
| Padding | `12px 24px` |
| Font weight | `600` |
| Font size | `0.9rem` |
| Hover | `transform: translateY(-1px)` |

**Secondary button** — "Get in touch", links to `/contact`:

| Property | Value |
|---|---|
| Background | `transparent` |
| Color | `--text` |
| Border | `1px solid --border` |
| Border radius | `8px` |
| Padding | `12px 24px` |
| Font weight | `600` |
| Font size | `0.9rem` |
| Hover border | `--accent` |
| Hover color | `--accent` |
| Hover transform | `translateY(-1px)` |

> Both buttons: `transition: transform 0.15s ease`. Remove the transform entirely under `prefers-reduced-motion: reduce`.

#### Right Column — Terminal Code Panel

A stylised static code block mimicking a terminal session. Purely presentational — no actual shell execution.

**Container:**

| Property | Value |
|---|---|
| Background | `--terminal-bg` |
| Border | `1px solid rgba(255, 255, 255, 0.08)` *(subtle — `--terminal-bg` is dark in both themes)* |
| Border radius | `12px` |
| Padding | `20px 24px` |
| Font family | `ui-monospace, 'Cascadia Code', 'Fira Code', monospace` *(system stack — no web font load, consistent with Lighthouse ≥ 90 target)* |
| Font size | `0.82rem` |
| Line height | `1.6` |

**Traffic-light bar** (top of panel, `aria-hidden="true"`):

Three `8px` diameter circles in red / amber / green at `opacity: 0.6`. A `1px solid rgba(255,255,255,0.06)` border-bottom beneath this row acts as a title-bar separator. Purely decorative.

**Terminal content** (illustrative — real content to be confirmed against the actual NuGet package list):

```
$ dotnet add package AStar.Dev.Core

  Determining projects to restore...
  info : Adding PackageReference for
         'AStar.Dev.Core' to project.
  info : Restoring packages...
  info : Package 'AStar.Dev.Core' is
         compatible with all frameworks.

  Build succeeded.
  Package added: AStar.Dev.Core v2.3.1
  ✓  0 Warning(s)   0 Error(s)
```

**Token colour mapping inside the panel:**

| Text | Colour token |
|---|---|
| `$` prompt and command text | `--terminal-accent` |
| `info :` labels | `--text-muted` |
| "Build succeeded." and `✓` | `--terminal-accent` |
| "0 Warning(s)  0 Error(s)" | `--text-muted` |
| Package name and version | `--terminal-text` |

**Screen reader treatment:**

The terminal content is meaningful (it demonstrates a real package install), so it should **not** be `aria-hidden`. Place a visually hidden caption immediately before the panel:

```html
<p class="sr-only">
  Example: installing an AStar.Dev NuGet package via the .NET CLI
</p>
```

**Light theme note:** In light theme, `--terminal-bg` is `#1a1a2e` — a deep navy-purple, creating a deliberate dark island on the white page. Do not use a light-coloured terminal panel in light theme; the contrast between the white page and the dark panel is a feature, not a bug.

---

### 4.2 Services Section

**Anchor:** `id="services"` — target for the hero primary CTA.

**Background:** `--surface-raised`

#### Section Head

- `<h2>` "What I offer" — `font-size: clamp(1.4rem, 2.8vw, 2rem)`, `font-weight: 700`, `color: --text`
- `<p>` "Concrete outcomes for real teams. Typically 2–12 weeks, shaped around your constraints." — `color: --text-muted`, `font-size: 0.95rem`

#### Grid

| Viewport | Columns |
|---|---|
| ≥ 1024px | 3 columns |
| 768px–1023px | 2 columns |
| < 768px | 1 column |

5 cards total. On a 3-column desktop grid, the layout is 3 + 2 — the bottom row has two cards in the first two columns, leaving the third column empty. This is intentional and more readable than forcing 5 cards into an irregular layout.

#### `ServiceCard.astro` Anatomy

| Property | Value |
|---|---|
| Background | `--surface-base` |
| Border | `1px solid --border` |
| Border radius | `12px` |
| Padding | `20px 24px` |

Card contents (top to bottom):

1. **Icon** — inline SVG, `24×24px`, `color: --accent`, `margin-bottom: 12px`, `aria-hidden="true"`. One icon per service (see mapping below).
2. **`<h3>` title** — `font-size: 1rem`, `font-weight: 600`, `color: --text`, `margin-bottom: 8px`.
3. **`<p>` description** — `font-size: 0.9rem`, `color: --text-muted`, `line-height: 1.65`.

No links or buttons on service cards in Phase 1 — purely informational.

#### The Five Services

| # | Title | Description | Icon (Phosphor / Heroicons) |
|---|---|---|---|
| 1 | Fullstack Development | End-to-end feature delivery across .NET backends and modern frontends, with tests from the start. | Layers icon |
| 2 | Architecture Design | Clear boundaries, explicit contracts, and decision records that outlast the engagement. | Blueprint / grid icon |
| 3 | Backend Development | Performant, observable .NET services — APIs, workers, and integrations built to production standard. | Server stack icon |
| 4 | Code Reviews | Structured feedback that improves the code and develops the team. Not a bottleneck — a signal. | Magnifying glass over code icon |
| 5 | Mentoring | One-to-one and team mentoring grounded in real patterns, not theory. | Person with speech bubble icon |

> Copy above is illustrative. Stakeholder should confirm before Phase 1 ships — these descriptions are the primary credibility signal for Persona 1 (Engineering Leader).

---

### 4.3 Featured NuGet Packages Section

**Background:** `--surface-base`

#### Section Head

`display: flex`, `justify-content: space-between`, `align-items: baseline`, `margin-bottom: 28px`.

- **Left:** `<h2>` "Open Source Packages" + `<p>` "Production-tested patterns, published on NuGet." (`color: --text-muted`)
- **Right:** `<a href="/packages">` "View all packages →" — `color: --accent`, `font-size: 0.9rem`. Gives an immediate escape route to the full packages page without burying it at the bottom of the section.

#### Grid

| Viewport | Columns |
|---|---|
| ≥ 1024px | 3 columns |
| 768px–1023px | 2 columns |
| < 768px | 1 column |

3–5 cards (a curated subset; the full list is Phase 4). Metadata fetched from the NuGet v3 API at build time.

#### `PackageCard.astro` Anatomy

| Property | Value |
|---|---|
| Background | `--surface-raised` *(inverse of service cards — provides visual lift on the `--surface-base` section)* |
| Border | `1px solid --border` |
| Border radius | `12px` |
| Padding | `20px 24px` |

Card contents (top to bottom):

**1. Top row** — `display: flex`, `justify-content: space-between`, `align-items: center`

- Left: `<h3>` package name — `font-size: 0.95rem`, `font-weight: 600`, `color: --text`
- Right: "NuGet" badge pill — `border: 1px solid --border`, `color: --text-muted`, `font-size: 0.7rem`, `border-radius: 999px`, `padding: 2px 8px`

**2. Description** — `<p>`, `font-size: 0.88rem`, `color: --text-muted`, `line-height: 1.6`, `margin-top: 8px`. 1–2 sentences.

**3. Data row** — `display: flex`, `gap: 16px`, `margin-top: 12px`

| Item | Format | Style |
|---|---|---|
| Version | `v2.3.1` prefixed with a package icon (8px SVG, `aria-hidden`) | `--text-muted`, `font-size: 0.78rem` |
| Downloads | `14,200 downloads` prefixed with a download arrow icon (8px SVG, `aria-hidden`) | `--text-muted`, `font-size: 0.78rem` |

**4. Install command block** — a `<pre><code>` block

| Property | Value |
|---|---|
| Background | `--terminal-bg` |
| Border radius | `8px` |
| Padding | `10px 14px` |
| Font size | `0.78rem` |
| Color | `--terminal-text` |
| Margin top | `12px` |
| Overflow | `auto` |

Content: `dotnet add package [PackageName]`

The block is `position: relative` to contain the absolutely positioned `CopyButton.vue`.

**5. `CopyButton.vue`** — top-right corner of the install command block, `position: absolute`, `top: 8px`, `right: 8px`

| State | Description |
|---|---|
| Default | Clipboard icon, `16px`, `color: --text-muted` |
| Hover | `color: --accent`, `background: --copy-btn-hover-bg`, `border-radius: 4px` |
| Active (copied) | Checkmark icon, `color: --terminal-accent`, `aria-label="Copied!"` |
| After 2 seconds | Reverts to default state |

The button has `aria-label="Copy install command"`. An `aria-live="polite"` region (elsewhere in the card) announces "Copied to clipboard" to screen readers. Hydration: `client:visible` — only activates when the card scrolls into view. If `navigator.clipboard` is unavailable (non-secure context), the button hides itself rather than breaking.

**6. Footer row** — `display: flex`, `gap: 16px`, `margin-top: 16px`

- "View on NuGet.org ↗" — `<a target="_blank" rel="noopener noreferrer">`, `color: --accent`, `font-size: 0.82rem`
- "Documentation" (if applicable in Phase 1) — `color: --text-muted`, `font-size: 0.82rem`

---

### 4.4 Case Study Teasers Section

**Background:** `--surface-raised`

In Phase 1, real case study content does not exist. This section renders a deliberate **placeholder state** — styled to look intentional rather than broken or empty.

#### Section Head

- `<h2>` "Selected Work"
- `<p>` "Anonymised case studies from real engagements — publishing soon." — `color: --text-muted`

#### `CaseStudyTeaser.astro` Anatomy (Placeholder State)

2 cards, stacked vertically (full-width horizontal layout).

| Property | Value |
|---|---|
| Background | `--surface-base` |
| Border | `1px solid --border` |
| Border radius | `12px` |
| Padding | `24px 28px` |
| Layout | `display: flex`, `gap: 32px` on desktop; stacked on mobile |

**Left section (~60% width) — Text content:**

1. Content-type pill: "Case Study" — `border: 1px solid --border`, `color: --text-muted`, `font-size: 0.72rem`, `border-radius: 999px`, `padding: 2px 8px`. Establishes content type clearly.
2. `<h3>` title — `font-size: 1.1rem`, `font-weight: 600`, `color: --text`. Placeholder titles (credible and anonymised): "Enterprise Integration Overhaul" / "Distributed Pipeline Rebuild".
3. `<p>` one-sentence summary with a real metric, e.g. *"Reduced message-processing latency by 60% across a distributed .NET 8 pipeline serving 50M+ daily events."* — `color: --text-muted`, `font-size: 0.9rem`, `margin-top: 8px`.
4. "Full case study coming soon" — `color: --text-muted`, `font-size: 0.85rem`, `cursor: default`, `pointer-events: none`. Visually present but non-interactive.

**Right section (~40% width) — Tech Stack:**

- "Tech Stack" label — `color: --text-muted`, `font-size: 0.72rem`, `margin-bottom: 8px`
- 3–5 tech tag pills: e.g. `.NET 8` · `Azure Service Bus` · `SQL Server` · `OpenTelemetry`

Tag style: `border: 1px solid --border`, `background: transparent`, `color: --text-muted`, `font-size: 0.75rem`, `border-radius: 999px`, `padding: 3px 10px`

> **Phase 2 migration:** Replace placeholder copy, make the link functional (`<a href="/case-studies/[slug]">`), remove `pointer-events: none`. The component structure does not change.

---

### 4.5 CTA Banner Section

**Background:** `--surface-base`

No bottom border — the footer follows immediately and provides visual closure.

#### Layout

A centred inset container — not full-bleed, not a floating card.

| Property | Value |
|---|---|
| Max width | `680px`, centred |
| Border left | `3px solid --cta-border-left` |
| Border radius | `0 4px 4px 0` *(left side is flush with the border; right side has a subtle radius)* |
| Background | `--surface-raised` |
| Padding | `28px 32px` |

#### Content (top to bottom)

1. **Eyebrow** — `<p>` "READY TO TALK?" — all-caps, `color: --accent`, `font-size: 0.72rem`, `font-weight: 700`, `letter-spacing: 0.15em`, `margin-bottom: 12px`
2. **`<h2>`** "Interested in working together?" — `font-size: clamp(1.3rem, 2.5vw, 1.8rem)`, `font-weight: 700`, `color: --text`
3. **`<p>`** "Whether you have a specific project in mind or just want to explore what's possible — let's have a conversation." — `color: --text-muted`, `font-size: 0.95rem`, `margin-top: 8px`
4. **Primary button** (same style as hero primary button, `margin-top: 20px`) — "Get in touch", `href="/contact"`

---

## 5. Footer

**Component:** `Footer.astro`

### Container

| Property | Value |
|---|---|
| Background | `--footer-bg` |
| Border top | `1px solid --section-border` |
| Padding | `24px` |
| Landmark | `<footer>` |

### Layout

Single row on desktop (`display: flex`, `justify-content: space-between`, `align-items: center`); three stacked rows on mobile.

| Zone | Content | Style |
|---|---|---|
| Left | `© 2026 AStar Development. All rights reserved.` | `color: --text-muted`, `font-size: 0.83rem` |
| Centre | Privacy Policy link (`href="/privacy"`) | `color: --text-muted`, `font-size: 0.83rem`; hover: `color: --text` |
| Right | GitHub icon link + NuGet icon link | Inline SVGs, `20px`, `color: --text-muted`; hover: `color: --accent`; `gap: 16px` |

No `aria-label` on the `<footer>` element — there is only one footer landmark per page, so it is unambiguous.

---

## 6. Privacy Policy Page

**Route:** `/privacy`

### Purpose

Static legal page. UK GDPR compliance. Accessible via the footer link on every page.

### Rendering Strategy

Purely static Astro page. Zero JavaScript. No client components.

### Layout

`BaseLayout.astro` wrapper (nav + footer). A single centred reading column:

| Property | Value |
|---|---|
| Max width | `720px` |
| Horizontal padding | `24px` |
| Body font size | `1rem` |
| Body line height | `1.75` *(generous — legal text benefits from breathing room)* |
| Body color | `--text` |

### Page Structure

```
<h1> Privacy Policy
<p>  Last updated: [date]  — color: --text-muted, font-size: 0.85rem

<section>
  <h2> What data we collect
<section>
  <h2> How it is used
<section>
  <h2> Cookies
<section>
  <h2> Analytics (Azure Application Insights)
<section>
  <h2> Your rights under UK GDPR
<section>
  <h2> Contact for data requests
```

No decorative elements, panels, or icons — clean editorial text only.

### Accessibility Notes

- Heading hierarchy is strictly linear: `<h1>` → `<h2>` sections. No levels skipped.
- All external links: `target="_blank" rel="noopener noreferrer"` plus a visually hidden `<span class="sr-only">(opens in new tab)</span>` suffix, or an icon with an `aria-label` that includes the destination.

---

## 7. Cookie Consent Banner

**Component:** `CookieConsent.vue`, `client:load`

### Purpose

UK GDPR compliance. Appears on first visit. Stores user preference in `localStorage['cookie-consent']`. Controls whether Azure Application Insights is initialised.

### Layout

Fixed to the **bottom of the viewport**, full width, `z-index: 200` (above nav at `z-index: 100`).

| Property | Value |
|---|---|
| Background | `--nav-bg` |
| Border top | `1px solid --border` |
| Padding | `16px 24px` |
| Inner container | `max-width: 1120px`, centred |
| Inner layout | `display: flex`, `align-items: center`, `justify-content: space-between`, `flex-wrap: wrap`, `gap: 16px` |

### Content

**Text area** (left, `flex-grow: 1`):

`<p>` "We use cookies to remember your theme preference and collect anonymised usage analytics. " followed by an inline link: "Read our Privacy Policy." (`href="/privacy"`, `color: --accent`). `font-size: 0.85rem`, `color: --text-muted`.

**Button area** (right, `flex-shrink: 0`, `display: flex`, `gap: 12px`):

- "Accept" — primary button style, reduced size (`padding: 8px 20px`, `font-size: 0.85rem`)
- "Decline" — secondary ghost style, same sizing

### Behaviour

| Event | Action |
|---|---|
| First page load (no stored preference) | Banner renders and is visible |
| "Accept" clicked | Stores `{ analytics: true }` in `localStorage['cookie-consent']`; banner unmounts; App Insights initialises |
| "Decline" clicked | Stores `{ analytics: false }` in `localStorage['cookie-consent']`; banner unmounts; App Insights does **not** initialise |
| Subsequent page loads | `localStorage['cookie-consent']` exists; banner never renders; analytics state applied from stored value |

> Theme preference is stored separately under `localStorage['theme']` — it is a UX preference, not analytics data, and does not require consent.

### Accessibility Notes

- `role="dialog"`, `aria-label="Cookie consent"`, `aria-modal="false"` — the user can bypass the banner and continue using the page; it does not block interaction.
- Focus is **not** forced to the banner on appearance. Forcing focus would be disorienting for a user already reading the page.
- An `aria-live="polite"` region announces "Cookie preferences saved" when either button is clicked.
- Both buttons have explicit text labels — no icon-only controls here.

---

## 8. Responsive Behaviour Summary

| Viewport | Nav | Hero | Card grids | Terminal panel |
|---|---|---|---|---|
| 320px | Hamburger drawer | Single column, no terminal | 1 column | Hidden |
| 768px | Hamburger drawer | Single column, terminal below text at ~80% width | 2 columns | Visible, reduced |
| 1024px | Full links + theme switcher | Two-column split, 55 / 45 | 3 columns | Full size |
| 1440px | Full links + theme switcher | Two-column split, 55 / 45, wider gap | 3 columns | Full size |

**Shared constraints:**

- Max content width: `1120px` on all screens
- Horizontal section padding: `24px` at all sizes
- All interactive elements: minimum `44×44px` touch target on mobile
- All card grids: `gap: 20px`

---

## 9. Theme Switching — No-Flash Strategy

The current Vue site does not prevent a flash of the wrong theme on page load. This is a Phase 1 exit criterion and must be solved in `BaseLayout.astro`.

An **inline, synchronous script** (not `defer`, not `type="module"`) must be placed in the `<head>` before any stylesheets. It reads `localStorage['theme']`, falls back to `'dark'` if no value exists, and applies the class to `<html>` before the first paint.

The execution order is:

```
Inline script reads localStorage
  → applies theme class to <html>
    → browser resolves CSS custom properties
      → first paint (correct theme, no flash)
```

This script must remain inline — externalising it would allow the browser to defer or async-load it, reintroducing the flash.

---

## 10. Open Questions

The following decisions require stakeholder input before or during Phase 1 implementation.

1. **"Available for contracts" badge** — Is this currently accurate? If availability changes between deployments, the site will show stale information. Consider a boolean flag in the Astro config or a `.env` variable (`PUBLIC_AVAILABLE_FOR_CONTRACTS=true`) so it can be toggled without touching component code.

2. **Terminal panel package** — The illustrative content uses `AStar.Dev.Core`. This should reference a real, published package — ideally the most-recognisable one from the curated list. Confirm which package to feature before Phase 1 ships.

3. **Service copy** — The five service descriptions in [Section 4.2](#42-services-section) are illustrative. These are the primary credibility signal for Persona 1 (Engineering Leader) and must be confirmed or replaced by the stakeholder before launch.

4. **Badge contrast audit** — The `--badge-text` light-theme value (`#0055aa`) must be verified in a WCAG contrast checker against the effective badge background before implementation begins. See [Section 1](#1-design-tokens) for the detailed note.

5. **System monospace font** — The spec recommends a system monospace stack (`ui-monospace, 'Cascadia Code', 'Fira Code', monospace`) for the terminal panel to avoid a web font load and protect the Lighthouse ≥ 90 target. If a specific typeface is preferred (e.g. JetBrains Mono via `@fontsource`), the font-load cost must be assessed against the performance budget before committing.

6. **`CopyButton` in non-secure contexts** — `navigator.clipboard` requires HTTPS. In production this is fine (Azure Web App with SSL). Locally, developers running over HTTP will see the button hidden. Document this in the project README so contributors are not confused.

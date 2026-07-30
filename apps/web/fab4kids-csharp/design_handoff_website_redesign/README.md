# Handoff: fab4kids Website Redesign

## Overview
Redesign of fab-4-kids.co.uk, a UK site selling curriculum-aligned (KS1–KS4) educational PDFs, editable Word documents, and occasional physical materials. The redesign replaces a flat, boring "colourful" theme with a warmer, more playful brand: rounded display type, a rainbow accent palette, hover animations, and randomly-placed decorative shape/rainbow watermarks that sit behind content (never over text) for visual energy without hurting readability.

## About the Design Files
The `.dc.html` files in this bundle are **design references** built in an HTML prototyping tool (Design Components) — not production code to copy directly. They demonstrate exact layout, color, type, spacing, and interaction intent. The task is to **recreate these designs in the target codebase's existing environment** (the current fab4kids stack — check its framework, likely a JS site generator or React/Next — and match its conventions, routing and data layer) rather than shipping the HTML files as-is. If the existing stack is unsuited to this direction, pick the most sensible framework and implement there.

Note: these files use a `<x-dc>`/template-hole syntax (`{{ item.title }}`, `<sc-for>`, `<dc-import>`) specific to the prototyping tool — treat these purely as pseudocode for "loop over this list" / "reusable card component with these props." Translate them into the target framework's own component/loop syntax.

## Fidelity
**High-fidelity.** Final colors, typography, spacing and copy are as intended. Recreate pixel-perfect using the target codebase's component patterns.

## Design Tokens

### Colors
- Background (cream): `#FFFBF2`
- Section alt background (warm cream): `#FFF6E8`
- Ink / body text: `#2B2420`
- Secondary text: `#5A5148`
- Muted text: `#8A8072`
- Border / hairline: `#F3E9D6`
- Red (English / primary CTA): `#E8483A` — CTA shadow/hover: `#B9382D`
- Orange (History): `#F5A623`
- Green (Science): `#4CAF6D`
- Teal (Geography): `#2BB6A3`
- Blue (Maths): `#3B8FE0`
- Purple (accent): `#9B59D0`
- Yellow (accent / star): `#FFC93C`
- Dark footer/newsletter band: `#2B2420`

### Typography
- Display / headings: **Fredoka** (Google Font), weights 500/600/700 — rounded, chunky, playful. Used for h1–h4, nav logo, buttons, prices, badges.
- Body: **Nunito** (Google Font), weights 400/600/700/800 — used for paragraphs, nav links, labels.
- Scale: hero h1 56px/1.08; section h2 34–38px; card/product h3 16–20px; body 14–19px; small labels/badges 11–13px.

### Spacing / shape
- Card / panel border radius: 16–24px. Pills/badges/buttons: 12–16px (chip radius 999px for filter pills and nav basket badge).
- Section vertical padding: 56–96px; horizontal max-width container: 1200px (1000–1100px for narrower sections), 32px side gutters.
- Buttons use a "pressed" look: solid fill + `box-shadow: 0 6px 0 <darker-shade>` (e.g. red CTA `#E8483A` / shadow `#B9382D`).
- Card hover: `translateY(-6px)` + `box-shadow: 0 10px 0 #F3E9D6` (a "lift" effect, no blur — hard-edge shadow matching the chunky/pressed button style).

### Decorative watermark shapes ("fun" motif)
Random low-opacity (0.08–0.20) shapes placed behind content in most sections, `pointer-events: none`, never overlapping body text:
- Circles: plain `border-radius: 50%` divs.
- Diamonds/rotated squares: `border-radius` ~16–20%, `transform: rotate(45deg)` or similar.
- Triangles: `clip-path: polygon(50% 0%, 0% 100%, 100% 100%)`.
- Five-point stars: `clip-path: polygon(...)` star point-set (see hero/subjects sections in `Home.dc.html` for exact coordinates).
- Rainbow arches: 5 concentric semi-circle divs (red/orange/yellow/green/blue outer→inner), each `border-radius: 50% 50% 0 0`, stacked same bottom-center anchor, each ~30–35px narrower/shorter than the one behind it — creates a layered rainbow-arch silhouette (see hero section, bottom-right, in `Home.dc.html`).
- Each section places 2–4 of these at fixed but varied positions/sizes/rotations/colors so the page feels playfully scattered without being random-per-load (static, hand-placed randomness).

## Screens / Views

### 1. Home (`Home.dc.html`)
- **Purpose**: Landing page — brand intro, browse entry points, featured products, trust-building, newsletter capture.
- **Layout**: Single column of full-width sections, each with a `max-width: 1200px` centered content container.
  1. **Sticky header**: logo wordmark ("fab4kids", each syllable a different accent color), 5-item subject nav (Maths/English/Science/History/Geography), circular search icon button, rounded basket icon button (badge shape) on the right. Sticky, translucent cream background with blur on scroll.
  2. **Hero**: 2-column grid (1.1fr / 0.9fr). Left: eyebrow pill ("KS1–KS4 · Made with love"), h1 "Learning that feels like play" (last word red), supporting paragraph, two CTAs (solid red "Browse resources", outline "See subjects"). Right: soft irregular-blob background shape behind an image placeholder (child with printed worksheet), gentle float animation. Watermark circle/diamond/triangle/rainbow-arch shapes scattered around the section.
  3. **Trust strip**: white band, 4 short bullet claims spaced across a row (instant download / editable Word files / curriculum-mapped / made by parents & teachers).
  4. **Browse by subject**: centered heading + subhead, 5-column grid of subject cards. Each card: colored rounded-square badge with subject initial, subject name, one-line description. Hover: lift + hard shadow. Card colors map 1:1 to the palette above (Maths=blue, English=red, Science=green, History=orange, Geography=teal).
  5. **Featured resources**: warm-cream band, heading + "See all" link, 4-column grid of product cards (see Resource Card component below).
  6. **How it works**: centered 3-step row, each step a colored circle numbered 1/2/3 (blue/red/green) with a short title + description (Browse & pick / Checkout securely / Print & learn).
  7. **Testimonials**: white band, 3-column grid of quote cards (star rating row, quote, name) on cream cards with hairline border.
  8. **Newsletter**: dark band (`#2B2420`) with watermark shapes, centered heading/subhead, inline email input + yellow "Subscribe" button; on submit shows a small "🎉 You're on the list!" confirmation below the form.
  9. **Footer**: warm-cream band, 4-column layout — brand blurb, Subjects links, Support links, Legal links — hairline divider, copyright line.

### 2. Subject listing page (`Maths.dc.html`, template for all 5 subjects)
- **Purpose**: Browse/filter all resources within one subject.
- **Layout**: Same header/footer as Home.
  - **Subject hero band**: tinted background (subject's accent color at low opacity, e.g. light blue `#EAF3FE` for Maths), breadcrumb ("Home → Maths"), colored initial badge + h1 subject name, one-line description. Watermark shapes tinted to the subject color.
  - **Filter/sort bar**: horizontal row of pill-shaped filter buttons (All, KS1–KS4, PDF, Word, Physical); active filter is solid-filled in the subject color, inactive are white/outline. Filter state changes visually on click (single-select in this prototype).
  - **Resource grid**: 4-column grid of product cards, 8 example items shown.
  - **Load more** button, centered, outline style, below the grid.
- **Reuse note**: swap the accent color, badge letter, hero tint background, subject name/description and the resource list per subject to produce the other 4 subject pages.

### 3. Product / resource detail page (`Resource.dc.html`)
- **Purpose**: Single resource detail + purchase.
- **Layout**: header/footer shared.
  - **Detail section**: breadcrumb (Home → Subject → Product title). Below it, 2-column grid (0.9fr / 1.1fr): left = large rounded image placeholder (cover/sample pages); right = subject/format/key-stage pill badges, h1 title, description paragraph, large price, two buttons (solid red "Add to basket", outline "Preview sample"), and a bordered "What's included" checklist card (4 bullet lines with checkmarks).
  - **Related resources**: warm-cream band, heading "You might also like", 4-column grid reusing the product card component.

## Shared Components

### Subject Card (`SubjectCard.dc.html`)
Props: `label` (subject name), `letter` (single initial), `color` (hex), `desc` (one-line description), `href` (link target). White rounded card (20px radius, hairline border), centered content: colored initial badge (56×56px, 16px radius) → subject name (Fredoka 18px/600) → description (13px, muted). Hover: lift + hard shadow.

### Resource / Product Card (`ResourceCard.dc.html`)
Prop: `item` object — `{ title, subject, subjectColor, format, stage, price }`. White card, rounded (20px), hairline border, overflow hidden. Top: image placeholder area with two overlaid pill badges (top-left: subject name on `subjectColor` background; top-right: format label on dark `#2B2420` background). Body: title (Fredoka 16px/600), key-stage label (12px, muted, bold), then a bottom row with price (Fredoka 17px/700) left and a yellow "Add +" pill button right. Whole card is a link to the resource detail page. Hover: lift + hard shadow.

## Interactions & Behavior
- **Nav/basket/search icons**: currently visual-only placeholders in the prototype — wire to real search and basket/cart functionality.
- **Card hover**: all subject cards and resource cards lift 6px and gain a hard-edge shadow on hover (no blur, no scale) — implement as a CSS transition (~150–200ms) on `transform`/`box-shadow`.
- **Filter pills** (subject page): clicking a pill sets it active (solid subject-color fill, white text) and should re-query/filter the resource grid by that facet in production (prototype only demonstrates the visual toggle, not real filtering — implement actual filter logic: key-stage and format facets, `All` resets).
- **Newsletter form**: on submit, prevent default, show inline success message ("🎉 You're on the list!"); production should call the real subscription endpoint and handle error states (invalid email, already subscribed, network failure) — none are designed yet, ask if needed.
- **Hero blob**: background blob behind the hero image gently floats up/down in a slow (~6s) infinite loop — purely decorative `transform: translateY()` keyframe animation.
- **Responsive behavior**: not yet designed — the prototype is desktop-width only (grids assume ~1200px). Plan breakpoints for tablet/mobile (nav collapsing to a menu, grids reducing to 2-col/1-col, hero stacking to one column) before implementation.

## State Management
- Active filter selection on the subject page (currently local component state, single-select).
- Newsletter subscribe/submitted state (boolean, resets on reload in the prototype — production should persist subscription server-side).
- Basket/cart contents — not built in this prototype; will need real state (context/store) once "Add to basket" is wired up.
- Resource data (title, subject, format, key stage, price, images) is hardcoded sample data in the prototype's logic — replace with real content/CMS/API data per subject and per product.

## Assets
- Fonts: Google Fonts **Fredoka** and **Nunito** (loaded via `<link>` in each page's `<head>`; use the same weights: Fredoka 500/600/700, Nunito 400/600/700/800).
- All product/hero imagery are drag-and-drop placeholder slots in the prototype (`<image-slot>` custom element) — no real photography/asset files exist yet. Source real photos/cover mockups per resource before or during implementation.
- No icon font/library used — icons (search, basket, checkmarks) are hand-built from simple CSS shapes (circles/borders) or emoji (📥 ✏️ 🎯 💛 in the trust strip, ✓ in the "what's included" list, 🎉 in the newsletter confirmation, ★ in testimonial ratings). Keep or swap for a proper icon set at implementation time.

## Files
- `Home.dc.html` — homepage
- `Maths.dc.html` — subject listing page template (also represents English/Science/History/Geography with per-subject swaps)
- `Resource.dc.html` — product/resource detail page
- `SubjectCard.dc.html` — subject card component (used on Home)
- `ResourceCard.dc.html` — product card component (used on Home, subject page, and resource detail's "related" section)

Open any `.dc.html` file directly in a browser to view the live design.

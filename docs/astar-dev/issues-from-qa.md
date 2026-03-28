 ---
  Code Review — apps/web/astar-dev

  Files reviewed: all 38 source files plus astro.config.mjs, package.json, .env.example, .gitignore.
  Reference documents: WEB-DESIGN.md and WEB-SPEC.md.

  ---
  🔴 Security

  SEC-1 — rateLimitMap is never pruned (memory leak / DoS vector)

  File: src/pages/api/contact.ts (line 31)

  The rateLimitMap is a module-level Map that is never cleaned up. Expired entries are overwritten on revisit, but IPs that never revisit accumulate forever. On a long-lived Node process this is a slow but real memory leak, and with
  enough unique IPs it becomes a heap-exhaustion DoS vector.

  SEC-2 — x-forwarded-for is trusted without proxy verification

  File: src/pages/api/contact.ts (lines 165–168)

  const ip =
    request.headers.get('x-forwarded-for')?.split(',')[0]?.trim() ??
    request.headers.get('x-real-ip') ??
    'unknown';
  Any client can trivially set x-forwarded-for: 1.2.3.4 and bypass rate limiting entirely. The rate limiter is only meaningful if the value comes from a trusted reverse proxy that the app verifies. There is no such verification here,
  and no Azure App Service specific header (x-client-ip, X-ARR-LOG-ID) used as a more trustworthy fallback.

  SEC-3 — Content-Length body-size check can be spoofed

  File: src/pages/api/contact.ts (lines 156–162)

  The body-size guard reads the Content-Length request header, which a client can lie about. The actual body bytes are never measured. An attacker sends Content-Length: 5 with a multi-megabyte body, and the check passes. The body should
   be read and its byte length measured after request.json() (or the raw text should be limited before parsing).

  SEC-4 — localStorage read in ThemeSwitcher.vue onMounted is unguarded

  File: src/components/ThemeSwitcher.vue (line 22)

  const stored = localStorage.getItem(STORAGE_KEY) as Theme | null;
  This is outside a try/catch. In Firefox with "Delete cookies and site data when Firefox is closed" + strict mode, accessing localStorage throws a SecurityError. The applyTheme write path is guarded (line 14), but the read in onMounted
   is not. This will crash the component silently for those users, leaving the theme switcher non-functional.

  SEC-5 — No Content Security Policy

  No CSP headers are defined anywhere. The App Insights loader in BaseLayout.astro dynamically injects a <script src="https://js.monitor.azure.com/...">, and the contact form posts to a server endpoint. Without a CSP, there is no
  defence-in-depth against XSS if any other injection point is found.

  SEC-6 — .env.example key name does not match the code

  File: .env.example (line 9) vs src/layouts/BaseLayout.astro (line 21)

  .env.example declares APPLICATIONINSIGHTS_CONNECTION_STRING= but the code reads import.meta.env.PUBLIC_APPINSIGHTS_CONNECTION_STRING. A developer copying .env.example will silently get no analytics and no error —
  PUBLIC_APPINSIGHTS_CONNECTION_STRING will be undefined, appInsightsConnString will be '', and App Insights will never load. This is also a documentation/onboarding hazard.

  ---
  🟠 Performance

  PERF-1 — Global transition on *, *::before, *::after is not guarded by prefers-reduced-motion

  File: src/layouts/BaseLayout.astro (lines 120–126)

  *, *::before, *::after {
    transition-property: background-color, border-color, color, fill, stroke;
    transition-duration: 0.25s;
    ...
  }
  This is the broadest possible selector. On a blog post with many inline elements it fires hundreds of transitions simultaneously on every theme switch. More critically, there is no @media (prefers-reduced-motion: reduce) wrapper —
  users who opt out of motion still get all five properties transitioning on every element. WCAG 2.3.3 (AAA) aside, the reduced-motion omission is a UX regression for vestibular-disorder users.

  PERF-2 — BlogTagFilter.vue is loaded with client:load, not client:idle

  File: src/pages/blog/index.astro (line 35)

  Tag filtering is non-critical to first meaningful paint. client:load blocks the main thread immediately. client:idle would defer hydration until after the browser is idle, improving Time to Interactive for the blog page without
  visible user impact.

  PERF-3 — getFeaturedPackages uses Promise.all, which hard-fails if any single package has no cache

  File: src/lib/nuget.ts (lines 91–93)

  export async function getFeaturedPackages(ids: string[]): Promise<PackageData[]> {
    return Promise.all(ids.map((id) => getPackageData(id)));
  }
  getPackageData throws if the NuGet API is unreachable AND no local cache exists. Promise.all propagates that throw to the build, which will fail the entire home page build for a single missing package. WEB-SPEC Phase 4 exit criteria
  explicitly requires: "Build fails gracefully if NuGet API is unavailable (uses cached/fallback data)". Promise.allSettled with a fallback stub would satisfy this without crashing the build.

  PERF-4 — nuget.ts cache path relies on process.cwd()

  File: src/lib/nuget.ts (line 12)

  const CACHE_DIR = join(process.cwd(), '.cache', 'nuget');
  In CI, process.cwd() may differ from the project root depending on the runner's working directory. The cache exists inside the repo at .cache/nuget/ and is committed to source (three files are visible in .cache/nuget/). However, the
  .gitignore explicitly excludes .cache/. This is a contradiction: the cache exists on disk and is being used, but .gitignore prevents it from being committed, meaning CI will always miss the cache on a clean checkout and always hit the
   live NuGet API.

  ---
  🟡 Maintenance

  MAINT-1 — Zero project tests; no test infrastructure

  All files

  There are no test files, no vitest.config.ts, no @vue/test-utils, no Vitest dependency in package.json, and no test script. The WEB-SPEC exit criteria for every phase states "all tests pass". The project's own CLAUDE.md system prompt
  makes testing non-negotiable. All five phases are now implemented with zero test coverage.

  The tests/ directory described in WEB-SPEC §5 architecture diagram (tests/components/, tests/e2e/) has never been created.

  MAINT-2 — sr-only is duplicated across four files

  Files: src/styles/global.css (line 111), src/components/CopyButton.vue (line 121), src/components/CookieConsent.vue (line 168), src/components/PackageCard.astro (line 167)

  The .sr-only utility is defined once in global.css and then re-declared as local/scoped styles in three component files. Since scoped Vue styles don't affect .sr-only in the static HTML of Astro components, this is partially
  redundant. When the canonical definition needs updating (e.g., adding clip-path: inset(50%) for modern browsers), it will need updating in all four places.

  MAINT-3 — Button styles duplicated across three files

  Files: src/styles/global.css (.btn, .btn-primary, .btn-secondary), src/components/Hero.astro (.btn, .btn-primary, .btn-secondary), src/components/CtaBanner.astro (.btn, .btn-primary)

  The hero and CTA banner redefine button styles locally instead of relying on the global utilities. The CtaBanner.astro version even omits the background-color and border-color transitions from the transition shorthand that global.css
  includes, meaning theme-switching behaviour will differ slightly for the CTA button vs buttons on other pages.

  MAINT-4 — Hero.astro terminal panel uses hard-coded package name and version

  File: src/components/Hero.astro (lines 7–8)

  const terminalPackage = 'AStar.Dev.Utilities';
  const terminalVersion = '1.6.8';
  Both values are hard-coded constants. The version will silently fall out of date with every NuGet release. The WEB-SPEC's build-time NuGet data is already fetched in index.astro — the terminal package and version should come from that
   data.

  MAINT-5 — Inline event handler in BlogTagFilter.vue template

  File: src/components/BlogTagFilter.vue (line 68)

  @click="activeTag = null; $nextTick(() => { const u = new URL(window.location.href); u.searchParams.delete('tag'); window.history.replaceState({}, '', u.toString()); })"
  Multi-statement logic with a $nextTick call is embedded directly in the template expression. This is untestable (the method is anonymous), violates the single-responsibility pattern, and is harder to read and debug. The "All" tag and
  the individual tag buttons have diverged logic that should share the same selectTag method (with null as the argument, or a dedicated clearTag method).

  MAINT-6 — package.json has no test, lint, or type-check scripts

  File: package.json

  Only dev, build, and preview scripts exist. There is no way to run type checking (tsc --noEmit), linting, or tests from the standard npm run interface. The CLAUDE.md "Definition of Done" requires all tests to pass before committing.

  MAINT-7 — sendgrid.ts lib file described in spec but not created

  File: WEB-SPEC.md §5 architecture diagram vs actual src/lib/

  The spec shows src/lib/sendgrid.ts as a separate module for SendGrid logic. All SendGrid logic lives inline in src/pages/api/contact.ts. This is pragmatically fine, but it deviates from the spec's intended boundary, makes contact.ts
  harder to unit-test in isolation, and means buildOwnerEmail / buildCopyEmail / escapeHtml cannot be tested without importing the entire API route.

  ---
  🔵 Departures from WEB-DESIGN.md / WEB-SPEC.md

  SPEC-1 — ThemeSwitcher.vue uses emoji, not inline SVGs

  File: src/components/ThemeSwitcher.vue (lines 27–46)

  WEB-DESIGN.md §3 specifies: "a moon icon button (dark theme) and a sun icon button (light theme) … Icons are inline SVGs, 16px." The implementation uses emoji characters (🌙, ☀, ⚡, ◆). Emoji rendering varies by OS and font, they
  cannot be styled with color: var(--text-muted) the way SVGs can, and they are not pixel-consistent across platforms. The emoji also don't match the WEB-DESIGN moon/sun icon specification.

  SPEC-2 — ThemeSwitcher.vue replaces all <html> classes, not just the theme class

  File: src/components/ThemeSwitcher.vue (line 13)

  document.documentElement.className = `theme-${theme}`;
  document.documentElement.className is a full replacement, not a toggle. If any other class is ever placed on <html> (e.g., by an Astro integration, a font-loading signal, or a JS feature-detection script), it will be silently removed
  on every theme switch. The no-flash inline script in BaseLayout.astro does the same thing (line 37), so this is a systemic pattern. It should use classList with remove/add or a targeted class swap.

  SPEC-3 — CaseStudyLayout.astro does not enforce the required section structure

  File: src/layouts/CaseStudyLayout.astro
  Reference: WEB-SPEC.md §8.6

  The spec requires case study pages to have four named sections: "The Problem", "The Approach", "The Outcome", "Tech Stack". The layout renders a plain <article> slot with no section scaffolding. The content is entirely in the Markdown
   files, which do use the correct headings — but there is no schema enforcement, no visual section labels, and no guarantee that future posts will follow the structure.

  SPEC-4 — BlogLayout.astro does not emit og:image

  File: src/layouts/BlogLayout.astro
  Reference: WEB-SPEC Phase 5 exit criteria: "Social sharing meta tags render correctly (testable via Open Graph debugger)"

  The blog layout emits og:title, og:description, twitter:card, twitter:title, twitter:description — but no og:image. Without og:image, link previews on Slack, Twitter/X, LinkedIn, and Teams render as text-only cards. An OG image (even
  a branded static fallback) is expected for social sharing to work correctly.

  SPEC-5 — WEB-SPEC requires output: 'hybrid'; astro.config.mjs uses output: 'static'

  File: astro.config.mjs (line 11)
  Reference: WEB-SPEC.md §6: "Use output: 'hybrid' in astro.config.mjs"

  The comment in astro.config.mjs explains this is the Astro 5 equivalent (static + per-route prerender = false). This is technically correct for the runtime behaviour, but it deviates from the letter of the spec. If reviewers are
  checking spec compliance literally, this will be a flag. The comment should reference the spec section explicitly to clarify the intentional update.

  SPEC-6 — packages.astro collapses to 1-column at ≤639px, not ≤767px

  File: src/pages/packages.astro (line 197)

  The packages grid breaks to single-column at max-width: 639px. The rest of the site's responsive breakpoints are 767px (mobile) and 1023px (tablet). Using a non-standard third breakpoint creates an inconsistent in-between state at
  640–767px where the packages page is still two-column while every other page section has gone single-column.

  ---
  Summary Table

  ┌─────────┬───────────────┬──────────┬───────────────────────┬────────────────────────────────────────────────────────────────┐
  │    #    │   Category    │ Severity │         File          │                             Issue                              │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SEC-1   │ Security      │ High     │ api/contact.ts        │ rateLimitMap never pruned — memory leak / DoS                  │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SEC-2   │ Security      │ High     │ api/contact.ts        │ x-forwarded-for trusted without proxy verification             │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SEC-3   │ Security      │ Medium   │ api/contact.ts        │ Content-Length header check is spoofable                       │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SEC-4   │ Security      │ Medium   │ ThemeSwitcher.vue     │ localStorage.getItem unguarded in onMounted                    │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SEC-5   │ Security      │ Medium   │ BaseLayout.astro      │ No Content Security Policy defined                             │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SEC-6   │ Security      │ Low      │ .env.example          │ Env var key name mismatch with source code                     │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ PERF-1  │ Performance   │ High     │ BaseLayout.astro      │ Global transition not wrapped in prefers-reduced-motion        │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ PERF-2  │ Performance   │ Low      │ blog/index.astro      │ BlogTagFilter uses client:load instead of client:idle          │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ PERF-3  │ Performance   │ Medium   │ lib/nuget.ts          │ Promise.all hard-fails build on missing package + no cache     │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ PERF-4  │ Performance   │ Medium   │ lib/nuget.ts          │ .cache/ is gitignored, so CI always hits live NuGet API        │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ MAINT-1 │ Maintenance   │ Critical │ All                   │ Zero tests, no test infrastructure, no test script             │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ MAINT-2 │ Maintenance   │ Low      │ 4 files               │ .sr-only defined 4 times                                       │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ MAINT-3 │ Maintenance   │ Medium   │ 3 files               │ Button styles duplicated in Hero and CtaBanner                 │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ MAINT-4 │ Maintenance   │ Low      │ Hero.astro            │ Hard-coded terminal package name and version                   │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ MAINT-5 │ Maintenance   │ Medium   │ BlogTagFilter.vue     │ Multi-statement inline template handler, untestable            │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ MAINT-6 │ Maintenance   │ Medium   │ package.json          │ No test, lint, or type-check scripts                           │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ MAINT-7 │ Maintenance   │ Low      │ lib/                  │ sendgrid.ts described in spec but not created                  │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SPEC-1  │ WEB-DESIGN.md │ Medium   │ ThemeSwitcher.vue     │ Emoji used instead of inline SVGs                              │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SPEC-2  │ WEB-DESIGN.md │ Medium   │ ThemeSwitcher.vue     │ className = replaces all <html> classes                        │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SPEC-3  │ WEB-SPEC.md   │ Low      │ CaseStudyLayout.astro │ No enforcement of "Problem/Approach/Outcome/Stack" sections    │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SPEC-4  │ WEB-SPEC.md   │ Medium   │ BlogLayout.astro      │ Missing og:image meta tag                                      │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SPEC-5  │ WEB-SPEC.md   │ Low      │ astro.config.mjs      │ output: 'static' vs spec's output: 'hybrid' (needs annotation) │
  ├─────────┼───────────────┼──────────┼───────────────────────┼────────────────────────────────────────────────────────────────┤
  │ SPEC-6  │ WEB-SPEC.md   │ Low      │ packages.astro        │ Non-standard 639px breakpoint inconsistent with rest of site   │
  └─────────┴───────────────┴──────────┴───────────────────────┴────────────────────────────────────────────────────────────────┘

  The most urgent items to address before any pull request: MAINT-1 (no tests at all), SEC-1/SEC-2 (rate-limiter vulnerabilities on the live contact endpoint), and PERF-1 (prefers-reduced-motion omission on the global transition rule,
  which is a WCAG concern).

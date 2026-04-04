## ADDED Requirements

### Requirement: Pagefind index is built at deploy time
The system SHALL run the Pagefind indexer against the static HTML output as a post-build step. The generated index SHALL be included in the deployed artefact.

#### Scenario: Search index generated after build
- **WHEN** the Astro build completes
- **THEN** a Pagefind index exists in the `dist/pagefind/` directory before deployment

### Requirement: Search results page available at /search
The system SHALL serve a `/search` page that accepts a `q` query parameter and renders matching results using the Pagefind client-side WASM bundle.

#### Scenario: Search returns relevant results
- **WHEN** a visitor navigates to `/search?q=fractions`
- **THEN** product and category pages containing the word "fractions" are displayed as results

#### Scenario: Search with no matches shows empty state
- **WHEN** a visitor searches for a term with no matches
- **THEN** an empty-state message is displayed with a suggestion to browse by subject

### Requirement: Search is accessible from all pages
The system SHALL include a search input in the site header that submits to `/search?q=<term>` on form submission.

#### Scenario: Header search submits to search page
- **WHEN** a visitor types a query in the header search box and presses Enter
- **THEN** they are navigated to `/search?q=<term>`

### Requirement: Product pages and category pages are indexed
All statically generated product detail pages and subject/key-stage pages SHALL be included in the Pagefind index. The index SHALL capture: page title, subject, key stage, and product description.

#### Scenario: Product detail page appears in search results
- **WHEN** a visitor searches for text present in a product's description
- **THEN** the matching product detail page appears in search results with its title and an excerpt

## ADDED Requirements

### Requirement: Subject pages are statically generated
The system SHALL pre-render a page for each subject (Maths, English, Science, History, Geography) listing all products for that subject across all key stages.

#### Scenario: Subject page loads with products
- **WHEN** a visitor navigates to `/maths`
- **THEN** a pre-rendered HTML page is served listing all Maths products grouped by key stage, with no server-side rendering required

#### Scenario: Subject page with no products
- **WHEN** a subject has no published products in Sanity
- **THEN** the subject page renders with an empty-state message indicating products are coming soon

### Requirement: Key-stage sub-pages are statically generated
The system SHALL pre-render a page for each subject × key-stage combination (e.g. `/maths/ks1`) listing only products matching that subject and key stage.

#### Scenario: Key-stage page lists correct products
- **WHEN** a visitor navigates to `/english/ks2`
- **THEN** only English products tagged KS2 are displayed

#### Scenario: Key-stage page is linked from subject page
- **WHEN** a visitor views a subject page
- **THEN** key-stage filter tabs (KS1, KS2, KS3, KS4) are visible and each links to the corresponding key-stage sub-page

### Requirement: Product detail pages are statically generated
The system SHALL pre-render a detail page for each product at `/product/[slug]`.

#### Scenario: Product detail page displays all required fields
- **WHEN** a visitor navigates to `/product/year-3-maths-pack`
- **THEN** the page displays: title, description, subject, key stage, file format(s), price (GBP), and an "Add to cart" button

#### Scenario: Non-existent product slug returns 404
- **WHEN** a visitor navigates to `/product/does-not-exist`
- **THEN** a 404 page is returned

### Requirement: Products are managed in Sanity CMS
The system SHALL source all product and category data from Sanity at build time. Adding or updating a product in Sanity and triggering a deployment SHALL be the only mechanism required to publish new products.

#### Scenario: New product published in Sanity appears after next deploy
- **WHEN** a content editor publishes a new product in Sanity Studio
- **AND** the site is redeployed
- **THEN** the new product appears on the relevant subject and key-stage pages

#### Scenario: Unpublished Sanity product does not appear on site
- **WHEN** a product is saved as a draft in Sanity (not published)
- **THEN** it does not appear on any page after deployment

### Requirement: Product cards display essential information
Each product listed on subject or key-stage pages SHALL display a card containing: product title, key stage badge, subject badge, file format badge(s), price, and a link to the product detail page.

#### Scenario: Product card renders on category page
- **WHEN** a product is listed on a subject page
- **THEN** the card shows title, KS badge, subject badge, format badges, price in GBP, and a "View" link

### Requirement: Navigation reflects subject hierarchy
The site navigation SHALL expose all five subjects as top-level links. Each subject link SHALL link to the subject page.

#### Scenario: All subjects visible in primary navigation
- **WHEN** any page is loaded
- **THEN** the primary navigation contains links to Maths, English, Science, History, and Geography

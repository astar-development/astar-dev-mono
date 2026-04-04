## ADDED Requirements

### Requirement: Cart persists in browser state
The system SHALL maintain a client-side cart using React context and `localStorage`. The cart SHALL survive page navigation and browser refresh within the same session.

#### Scenario: Item added to cart persists on navigation
- **WHEN** a visitor adds a product to the cart
- **AND** navigates to another page
- **THEN** the cart still contains the added product

#### Scenario: Cart restored on page refresh
- **WHEN** a visitor has items in the cart and refreshes the page
- **THEN** the cart contents are restored from `localStorage`

#### Scenario: Duplicate item not added twice
- **WHEN** a visitor adds the same product to the cart a second time
- **THEN** the cart quantity for that product increments rather than adding a duplicate line

### Requirement: Stripe hosted checkout initiates payment
The system SHALL redirect visitors to Stripe Checkout when they proceed to pay. The checkout session SHALL be created server-side via an Astro API route (`POST /api/checkout`).

#### Scenario: Checkout session created with correct line items
- **WHEN** a visitor with items in the cart clicks "Checkout"
- **THEN** a Stripe Checkout session is created with line items matching the cart contents and the visitor is redirected to the Stripe hosted page

#### Scenario: Checkout includes Stripe Tax at 0%
- **WHEN** a checkout session is created
- **THEN** Stripe Tax is applied to all line items at the configured rate (currently 0%)

#### Scenario: Successful payment redirects to success page
- **WHEN** the visitor completes payment on Stripe
- **THEN** they are redirected to `/checkout/success?session_id={CHECKOUT_SESSION_ID}`

#### Scenario: Cancelled payment redirects back to cart
- **WHEN** the visitor cancels on the Stripe hosted page
- **THEN** they are redirected back to the cart page with their items intact

### Requirement: Stripe webhook processes completed payments
The system SHALL expose a webhook endpoint at `POST /api/webhooks/stripe` that handles the `checkout.session.completed` event. The handler SHALL be idempotent (duplicate events for the same session SHALL NOT trigger duplicate deliveries).

#### Scenario: Webhook triggers digital delivery on payment completion
- **WHEN** Stripe sends a `checkout.session.completed` event
- **AND** the session has not already been processed
- **THEN** the system generates signed download URLs and sends a delivery email via Resend

#### Scenario: Duplicate webhook event is ignored
- **WHEN** Stripe sends a `checkout.session.completed` event for a session already processed
- **THEN** no duplicate delivery email is sent

#### Scenario: Invalid webhook signature is rejected
- **WHEN** a request arrives at `/api/webhooks/stripe` with an invalid Stripe signature
- **THEN** the endpoint returns HTTP 400 and logs the rejection

### Requirement: Success page confirms purchase and provides download context
The system SHALL render a `/checkout/success` page that confirms the purchase and instructs the visitor to check their email for download links.

#### Scenario: Success page shown after payment
- **WHEN** a visitor lands on `/checkout/success` with a valid `session_id` query parameter
- **THEN** a confirmation message is displayed with the customer email address and instruction to check email

#### Scenario: Success page without valid session redirects home
- **WHEN** a visitor navigates to `/checkout/success` with no or invalid `session_id`
- **THEN** they are redirected to the homepage

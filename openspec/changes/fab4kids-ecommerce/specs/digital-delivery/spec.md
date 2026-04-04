## ADDED Requirements

### Requirement: Product files are stored in Azure Blob Storage
All purchasable digital files (PDF, Word) SHALL be stored in a private Azure Blob Storage container. Files SHALL NOT be publicly accessible without a signed URL.

#### Scenario: Direct URL access to blob is denied
- **WHEN** a request is made directly to a blob URL without a SAS token
- **THEN** Azure returns HTTP 404 or 403

### Requirement: Signed download URLs are generated per purchase
The system SHALL generate a time-limited (15-minute TTL) Azure Blob Storage SAS URL for each purchased file. URL generation SHALL occur inside the Stripe webhook handler, after payment confirmation.

#### Scenario: Signed URL generated for each purchased item
- **WHEN** a `checkout.session.completed` event is processed
- **THEN** a distinct signed URL is generated for each product in the order

#### Scenario: Signed URL expires after TTL
- **WHEN** a signed URL is accessed more than 15 minutes after generation
- **THEN** Azure returns HTTP 403 (URL expired)

### Requirement: Download links are delivered by email via Resend
The system SHALL send a transactional email via Resend to the customer's email address (taken from the Stripe session) containing the signed download URL(s) for all purchased items.

#### Scenario: Delivery email sent to Stripe session email
- **WHEN** a `checkout.session.completed` event is processed
- **THEN** an email is sent to `session.customer_details.email` containing download links for all purchased files

#### Scenario: Email contains one link per purchased product
- **WHEN** a customer purchases three products
- **THEN** the delivery email contains exactly three download links, one per product

#### Scenario: Email is not sent if Stripe email is absent
- **WHEN** the Stripe session contains no customer email
- **THEN** the webhook handler logs an error and does not attempt to send an email

### Requirement: Resend-link form available on success page
The system SHALL provide a form on `/checkout/success` that allows the customer to re-request download links by submitting their order reference and email address, in case the delivery email is delayed or expired.

#### Scenario: Resend form re-generates signed URLs
- **WHEN** a customer submits a valid order reference and matching email on the resend form
- **THEN** fresh signed URLs (15-minute TTL) are generated and a new delivery email is sent via Resend

#### Scenario: Mismatched email is rejected
- **WHEN** a customer submits a valid order reference but a non-matching email
- **THEN** no email is sent and an error message is displayed

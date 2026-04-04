# Infrastructure Setup — fab4kids

## 1. Azure Blob Storage

### Create (Azure Portal or CLI)

1. Create a **Storage Account** (LRS, Hot tier is fine)
2. Inside it, create a **container** named `fab4kids-products`
3. Set container access to **Private** (no anonymous access — SAS URLs handle delivery)
4. Upload product files under paths matching the pattern stored in Sanity, e.g.  
   `maths/ks1/year-3-maths-pack.pdf`

### Get the connection string

Portal → Storage Account → **Access keys** → copy **Connection string** for key1.

### App settings required

| Name | Value |
|---|---|
| `AZURE_STORAGE_CONNECTION_STRING` | `DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net` |
| `AZURE_STORAGE_CONTAINER` | `fab4kids-products` |
| `SIGNED_URL_TTL_MINUTES` | `15` (optional — defaults to 15 if omitted) |

---

## 2. Stripe

### Create in the Stripe Dashboard

1. **Products & Prices**  
   - For each educational pack: create a Product, add a one-time Price in pence (e.g. `299` = £2.99)  
   - Note the `price_xxxxx` ID — this goes into the Sanity `stripePriceId` field

2. **Tax rate** (if/when VAT registration is required)  
   - Billing → Tax rates → Create: 0 %, `inclusive`, UK VAT  
   - `automatic_tax` is already enabled in the checkout code; add the rate ID to prices when needed

3. **Webhook endpoint**  
   - Developers → Webhooks → Add endpoint  
   - URL: `https://fab4kids.co.uk/api/webhooks/stripe`  
   - Events to listen for: `checkout.session.completed`  
   - After saving, reveal and copy the **Signing secret** (`whsec_...`)

4. **API keys**  
   - Developers → API keys → copy the **Secret key** (`sk_live_...` / `sk_test_...`)

### App settings required

| Name | Value |
|---|---|
| `STRIPE_SECRET_KEY` | `sk_live_...` or `sk_test_...` |
| `STRIPE_WEBHOOK_SECRET` | `whsec_...` (from the webhook endpoint above) |
| `PUBLIC_SITE_URL` | `https://fab4kids.co.uk` (used for success/cancel redirect URLs) |

---

## 3. Resend

1. Add and verify your sending domain (DNS: SPF + DKIM records)
2. Developers → API Keys → Create key (send-only scope is sufficient)

### App settings required

| Name | Value |
|---|---|
| `RESEND_API_KEY` | `re_...` |
| `RESEND_FROM_ADDRESS` | `orders@fab4kids.co.uk` (must match verified domain) |

---

## 4. Sanity CMS

1. `npm create sanity@latest` — choose a new project, note the **Project ID**
2. Deploy the Studio: `npx sanity deploy`
3. Create schema types: `product` and `subject` (see `design.md` for field list)

### App settings required

| Name | Value |
|---|---|
| `SANITY_PROJECT_ID` | e.g. `abc12345` |
| `SANITY_DATASET` | `production` (default — omit to use default) |

---

## 5. Where to put the app settings

### Local dev — `.env` file (gitignored)

```
AZURE_STORAGE_CONNECTION_STRING=...
AZURE_STORAGE_CONTAINER=fab4kids-products
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
PUBLIC_SITE_URL=http://localhost:4321
RESEND_API_KEY=re_...
RESEND_FROM_ADDRESS=...
SANITY_PROJECT_ID=...
```

### Azure App Service — Application Settings

Portal → App Service `fab4kids` → **Configuration** → Application settings → add each key above.  
These are injected as environment variables at runtime and override anything in `.env`.

### GitHub Actions — Repository Secrets

Settings → Secrets and variables → Actions → add:

| Secret name |
|---|
| `FAB4KIDS_AZURE_STORAGE_CONNECTION_STRING` |
| `FAB4KIDS_SANITY_PROJECT_ID` |
| `FAB4KIDS_STRIPE_SECRET_KEY` |
| `FAB4KIDS_RESEND_API_KEY` |
| `FAB4KIDS_PUBLIC_SITE_URL` |
| `AZURE_WEBAPP_PUBLISH_PROFILE` (download from App Service → Get publish profile) |

The CI workflow (`fab4kids-deploy.yml`) already references these names.

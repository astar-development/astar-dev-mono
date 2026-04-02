# Contact Form Flow

```mermaid

flowchart TD
    A([User clicks Submit]) --> B[validateFields]
    B -->|invalid| C[Focus first invalid field\nstop]
    B -->|valid| D[POST /api/contact\nJSON body]

    D --> E{Content-Length\n≥ 10KB?}
    E -->|yes| F[400 body too large]
    E -->|no| G{checkRateLimit\nip}
    G -->|exceeded\n10 req / 15 min| H[429 too many requests]
    G -->|ok| I[Parse JSON body]
    I -->|parse error| J[400 invalid body]
    I -->|ok| K{Honeypot\nwebsite field set?}
    K -->|yes| L[200 silent drop\nno email sent]
    K -->|no| M[validateBody]
    M -->|errors| N[400 validation errors]
    M -->|ok| O{Env vars\npresent?}
    O -->|SENDGRID_API_KEY missing| P[500 config error]
    O -->|CONTACT_EMAIL or\nSENDGRID_FROM_EMAIL missing| Q[500 config error]
    O -->|all present| R[buildOwnerEmail]
    R --> S{sendCopy?}
    S -->|yes| T[buildCopyEmail]
    T --> U[sgMail.send\nboth messages]
    S -->|no| V[sgMail.send\nowner email only]
    U --> W{SendGrid\nresponse}
    V --> W
    W -->|error| X[500 SendGrid error]
    W -->|ok| Y[200 success]

    Y --> Z[Form reset\nstatus → success]
    H --> AA[statusMessage → rate limit text]
    X --> AB[statusMessage → fallback error]
    F & J & N --> AC[statusMessage → error text]
    Z & AA & AB & AC --> AD[Focus statusRef\nscreen reader announces]

```

## Azure Settings (App Settings / Env variables)

| Setting             | Purpose                                       |
| ------------------- | --------------------------------------------- |
| SENDGRID_API_KEY    | Authenticates to SendGrid API                 |
| CONTACT_EMAIL       | Recipient of inbound contact messages (owner) |
| SENDGRID_FROM_EMAIL | Verified sender address in SendGrid           |

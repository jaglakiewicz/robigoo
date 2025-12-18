Client-side Angular app for Robigoo.

Running locally

1. Install dependencies:

```bash
cd ClientApp
npm install
```

2. Start the dev server (opens browser on http://localhost:4200):

```bash
npm start
```

Notes
- The client POSTs to `/api/inspections`. Run the ASP.NET backend on http://localhost:5000 (or where it runs) and adjust proxies if needed.
- CORS is allowed by the API (any origin) so dev server can call it directly.

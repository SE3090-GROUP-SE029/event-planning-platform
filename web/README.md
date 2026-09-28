# Web app

The web client uses React, Vite, React Router, Material UI, and the shared Plan It theme.

## Mobile app download link

The public landing page loads its mobile download link at runtime from [`public/app-config.json`](./public/app-config.json). Set `mobileAppDownloadUrl` to the HTTPS download or app-store URL. The file is fetched without browser caching, so the deployed static file can be updated without rebuilding the client.

If the setting is empty, download buttons remain disabled and the page explains that the link is not available yet. If loading fails, the page shows an error and a retry control.

## Local development

```sh
npm install
npm run dev
npm test
npm run build
```

Landing-page illustrations are bundled under `public/assets/illustrations/` and sourced from Storyset's Amico set:

- [Team work](https://storyset.com/illustration/team-work/amico)
- [Business plan](https://storyset.com/illustration/business-plan/amico)
- [Mobile marketing](https://storyset.com/illustration/mobile-marketing/amico)

The landing page includes Storyset attribution. Update the source map in `src/features/landing/components/storysetIllustrations.js` when replacing artwork.

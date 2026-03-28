import { defineConfig } from 'astro/config';
import vue from '@astrojs/vue';
import node from '@astrojs/node';

// Static by default (Astro 5). Individual routes opt into SSR via
// `export const prerender = false` — the former "hybrid" behaviour is now
// the default when an adapter is present.
export default defineConfig({
  integrations: [vue()],
  adapter: node({ mode: 'standalone' }),
  output: 'static',
  site: 'https://astardevelopment.co.uk',
});

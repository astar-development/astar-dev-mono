import { defineConfig } from 'astro/config';
import vue from '@astrojs/vue';

// Phase 1: fully static. Phase 3 (contact form) will add output: 'server'
// and @astrojs/node when the /api/contact SSR route is needed.
export default defineConfig({
  integrations: [vue()],
  site: 'https://astar.dev',
});

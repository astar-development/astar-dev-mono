import { defineConfig } from 'astro/config';
import react from '@astrojs/react';
import node from '@astrojs/node';

export default defineConfig({
  integrations: [react()],
  adapter: node({ mode: 'standalone' }),
  output: 'static',
  site: 'https://fab4kids.co.uk',
});

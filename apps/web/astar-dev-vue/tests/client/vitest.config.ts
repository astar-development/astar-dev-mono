import { defineConfig } from 'vitest/config'

export default defineConfig({
  test: {
    environment: 'jsdom',
    include: ['tests/client/**/*.test.ts'],
    coverage: {
      reporter: ['text', 'lcov'],
      reportsDirectory: 'client/coverage',
    },
  },
})

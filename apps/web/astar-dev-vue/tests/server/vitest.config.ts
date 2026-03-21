import { defineConfig } from 'vitest/config'

export default defineConfig({
  test: {
    environment: 'node',
    include: ['tests/server/**/*.test.ts'],
    coverage: {
      reporter: ['text', 'lcov'],
      reportsDirectory: 'server/coverage',
    },
  },
})

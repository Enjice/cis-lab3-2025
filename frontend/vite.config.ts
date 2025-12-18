import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts', 'allure-vitest/setup'],
    reporters: [
      'default',
      [
        'allure-vitest/reporter',
        {
          resultsDir: './allure-results'
        }
      ]
    ]
  }
})


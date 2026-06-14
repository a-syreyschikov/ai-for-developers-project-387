import { defineConfig } from 'orval'

export default defineConfig({
  calendar: {
    input: {
      target: '../../contracts/openapi.yaml',
    },
    output: {
      target: './src/api/generated/calendar.ts',
      client: 'axios',
      mode: 'single',
      override: {
        mutator: {
          path: './src/api/http.ts',
          name: 'apiClient',
        },
      },
    },
  },
})

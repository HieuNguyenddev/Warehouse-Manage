import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import type { ServerResponse } from 'node:http'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5080',
        changeOrigin: true,
        timeout: 10_000,
        configure: (proxy) => {
          proxy.on('error', (err, _req, res) => {
            console.error('[vite proxy /api]', err.message)
            const response = res as ServerResponse | undefined
            if (!response || response.headersSent || typeof response.writeHead !== 'function') return
            try {
              response.writeHead(502, { 'Content-Type': 'application/json' })
              response.end(
                JSON.stringify({
                  success: false,
                  message: 'API chưa sẵn sàng (http://localhost:5080). Hãy chạy backend.',
                  data: null,
                }),
              )
            } catch {
              /* ignore */
            }
          })
        },
      },
      '/uploads': {
        target: 'http://localhost:5080',
        changeOrigin: true,
        timeout: 10_000,
        configure: (proxy) => {
          proxy.on('error', (err) => {
            console.error('[vite proxy /uploads]', err.message)
          })
        },
      },
    },
  },
})

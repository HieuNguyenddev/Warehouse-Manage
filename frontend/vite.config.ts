import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import type { ServerResponse } from 'node:http'

/** Proxy ổn định: dùng 127.0.0.1 (tránh lỗi localhost IPv4/IPv6 → ECONNREFUSED). */
function apiProxy(target: string) {
  return {
    target,
    changeOrigin: true,
    timeout: 15_000,
    configure: (proxy: {
      on: (event: string, listener: (...args: never[]) => void) => void
    }) => {
      proxy.on('error', ((err: Error, _req: unknown, res: unknown) => {
        console.error(`[vite proxy → ${target}]`, err.message)
        const response = res as ServerResponse | undefined
        if (!response || response.headersSent || typeof response.writeHead !== 'function') return
        try {
          response.writeHead(502, { 'Content-Type': 'application/json' })
          response.end(
            JSON.stringify({
              success: false,
              message: `API chưa sẵn sàng (${target}). Chạy: cd backend/src/WarehouseManagement.WebAPI && dotnet run --launch-profile http`,
              data: null,
            }),
          )
        } catch {
          /* ignore */
        }
      }) as (...args: never[]) => void)
    },
  }
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiTarget = env.VITE_DEV_PROXY_TARGET || 'http://127.0.0.1:5136'

  return {
    plugins: [
      react(),
      tailwindcss(),
      {
        name: 'check-backend-api',
        configureServer(server) {
          server.httpServer?.once('listening', () => {
            const health = `${apiTarget}/api/v1/health`
            void fetch(health)
              .then((r) => {
                if (r.ok) console.info(`[api] OK ${apiTarget}`)
                else console.warn(`[api] ${apiTarget} trả ${r.status}`)
              })
              .catch(() => {
                console.warn(
                  `\n[api] CHƯA CHẠY tại ${apiTarget}\n` +
                    `      → cd backend/src/WarehouseManagement.WebAPI && dotnet run --launch-profile http\n`,
                )
              })
          })
        },
      },
    ],
    server: {
      port: 5173,
      proxy: {
        '/api': apiProxy(apiTarget),
        '/uploads': apiProxy(apiTarget),
      },
    },
  }
})

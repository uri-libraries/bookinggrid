
import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig(({ mode }) => {
  // Load env file based on `mode` in the current working directory.
  const env = loadEnv(mode, process.cwd(), '')
  console.log('VITE_LIBCAL_BASE_URL:', env.VITE_LIBCAL_BASE_URL)
  console.log('VITE_LIBCAL_OAUTH_URL:', env.VITE_LIBCAL_OAUTH_URL)
  console.log('NODE_ENV:', env.NODE_ENV)
  console.log('MODE:', mode)

  // https://vitejs.dev/config/
  return {
    plugins: [vue()],
    base: '/',
    resolve: {
      alias: {
        '@': '/src',
      },
    },
    build: {
      assetsInlineLimit: 0 // Always copy assets, never inline as base64
    },
    server: {
      proxy: {
        '/api': {
          target: 'https://uri.libcal.com',
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/api/, '/api/1.1'),
          configure: (proxy, options) => {
            proxy.on('proxyReq', (proxyReq, req, res) => {
              if (req.headers.authorization) {
                proxyReq.setHeader('Authorization', req.headers.authorization);
              }
            });
          }
        },
        '/oauth': {
          target: 'https://uri.libcal.com',
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/oauth/, '/1.1/oauth')
        }
      }
    }
  }
})

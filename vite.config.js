import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

console.log('VITE_LIBCAL_BASE_URL:', process.env.VITE_LIBCAL_BASE_URL);
console.log('VITE_LIBCAL_OAUTH_URL:', process.env.VITE_LIBCAL_OAUTH_URL);
console.log('NODE_ENV:', process.env.NODE_ENV);
console.log('MODE:', process.env.MODE);

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  base: '/',
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
})

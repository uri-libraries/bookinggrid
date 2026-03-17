import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

console.log('ENV:', import.meta.env);

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  base: '/',
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

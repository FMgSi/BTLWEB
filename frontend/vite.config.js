import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5148',
        changeOrigin: true,
        secure: false
      },
      '/uploads': {
        target: 'http://localhost:5148',
        changeOrigin: true,
        secure: false
      },
      '/images': {
        target: 'http://localhost:5148',
        changeOrigin: true,
        secure: false
      }
    }
  }
})

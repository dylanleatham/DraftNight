import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig({
  base: '/',
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg', 'apple-touch-icon.png'],
      manifest: {
        name: 'MTG Draft Night',
        short_name: 'Draft Night',
        description: 'MTG Draft Night tournament manager',
        theme_color: '#0f0f12',
        background_color: '#0f0f12',
        icons: [
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png'
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png'
          }
        ]
      }
    })
  ],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5244',
        changeOrigin: true
      },
      '/hubs': {
        target: 'http://localhost:5244',
        changeOrigin: true,
        ws: true
      }
    }
  }
})

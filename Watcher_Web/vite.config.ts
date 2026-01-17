import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { viteStaticCopy } from 'vite-plugin-static-copy'

// https://vite.dev/config/
export default defineConfig({
  base: '/FF14_Watcher/',
  plugins: [
    react(),
    viteStaticCopy({
      targets: [
        {
          src: 'node_modules/@paddlejs-models/ocr/lib/*',
          dest: 'paddle_models'
        }
      ]
    })
  ],
  define: {
    // Polyfill Module for @paddlejs-models/ocr
    Module: {}
  }
})

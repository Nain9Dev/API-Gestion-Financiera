import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

// https://vitejs.dev/config/
export default defineConfig({
  base: './',
  plugins: [vue()],
  server: {
    port: 5375,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:8110',
        changeOrigin: true,
      },
      '/health': {
        target: 'http://localhost:8110',
        changeOrigin: true,
      },
    },
  },
  preview: {
    port: 5375,
    strictPort: true,
  },
});

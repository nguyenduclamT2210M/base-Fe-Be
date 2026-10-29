/**
 * ============================================================
 * FILE: vite.config.js
 * TÁC DỤNG:
 *   Cấu hình công cụ build Vite cho dự án.
 *   - Bật plugin Vue (biên dịch file .vue)
 *   - Alias "@" trỏ tới thư mục src/ → import ngắn gọn: '@/utils/formatter'
 *   - Cấu hình dev server (port, proxy tới backend thật nếu cần)
 * CÁCH DÙNG:
 *   - Không import file này trong code. Vite tự đọc khi chạy `npm run dev/build`.
 *   - Muốn gọi backend thật mà không bị lỗi CORS khi dev: bật khối `proxy` bên dưới,
 *     đặt VITE_API_URL=/api trong .env.development.
 * ============================================================
 */
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    open: false,
    // proxy: {
    //   '/api': {
    //     target: 'http://localhost:5000', // địa chỉ backend thật
    //     changeOrigin: true,
    //   },
    // },
  },
});

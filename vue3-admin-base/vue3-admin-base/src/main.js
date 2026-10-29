/**
 * ============================================================
 * FILE: src/main.js
 * TÁC DỤNG:
 *   Điểm khởi động ứng dụng:
 *   1. Nạp CSS toàn cục
 *   2. Tạo app Vue, cài plugin Pinia (store) rồi tới Router.
 *      Pinia phải cài TRƯỚC router vì navigation guard dùng useAuthStore().
 *   3. Bắt lỗi chưa xử lý trong component (errorHandler) để không trắng trang im lặng
 *   4. Gắn app vào thẻ #app trong index.html
 * CÁCH DÙNG:
 *   Thêm plugin toàn cục (vd i18n) bằng app.use(...) TRƯỚC app.mount().
 *   Không đăng ký component toàn cục ở đây: import trực tiếp trong từng file
 *   để IDE gợi ý được và bundle tự loại bỏ phần không dùng.
 * ============================================================
 */
import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import { useAppStore } from './stores/app';
import './assets/styles/base.css';

const app = createApp(App);

app.use(createPinia());
app.use(router);

/**
 * Bắt mọi lỗi chưa được xử lý trong component/lifecycle/watcher.
 * Lỗi từ API đã được interceptor toast → bỏ qua để không báo 2 lần.
 * @param {unknown} err
 * @param {import('vue').ComponentPublicInstance|null} _instance
 * @param {string} info - nơi phát sinh lỗi (vd "setup function")
 */
app.config.errorHandler = (err, _instance, info) => {
  console.error('[App error]', info, err);
  if (!err?.isAxiosError) useAppStore().toastError('Đã xảy ra lỗi không mong muốn');
};

app.mount('#app');

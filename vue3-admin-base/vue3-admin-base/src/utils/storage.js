/**
 * ============================================================
 * FILE: src/utils/storage.js
 * TÁC DỤNG:
 *   Bọc localStorage: tự chuyển JSON ↔ object, bắt lỗi (trình duyệt chặn storage,
 *   chế độ ẩn danh, dữ liệu hỏng) để app không bị crash.
 * CÁCH DÙNG:
 *   import storage from '@/utils/storage';
 *   import { STORAGE_KEYS } from '@/utils/constants';
 *   storage.set(STORAGE_KEYS.USER, { name: 'An' });
 *   const user = storage.get(STORAGE_KEYS.USER, null);
 *   storage.remove(STORAGE_KEYS.USER);
 * LƯU Ý:
 *   Không lưu dữ liệu nhạy cảm (mật khẩu, thông tin thẻ) vào localStorage.
 * ============================================================
 */

const storage = {
  /**
   * Đọc giá trị theo key.
   * @param {string} key
   * @param {any} [defaultValue=null] - giá trị trả về nếu không có hoặc lỗi
   * @returns {any}
   */
  get(key, defaultValue = null) {
    try {
      const raw = localStorage.getItem(key);
      return raw === null ? defaultValue : JSON.parse(raw);
    } catch {
      return defaultValue;
    }
  },

  /**
   * Ghi giá trị (tự JSON.stringify).
   * @param {string} key
   * @param {any} value
   */
  set(key, value) {
    try {
      localStorage.setItem(key, JSON.stringify(value));
    } catch {
      /* bỏ qua: storage đầy hoặc bị chặn */
    }
  },

  /**
   * Xoá một key.
   * @param {string} key
   */
  remove(key) {
    try {
      localStorage.removeItem(key);
    } catch {
      /* bỏ qua */
    }
  },
};

export default storage;

/**
 * ============================================================
 * FILE: src/utils/helpers.js
 * TÁC DỤNG:
 *   Hàm tiện ích chung không thuộc nhóm format/validate: debounce, deepClone...
 * CÁCH DÙNG:
 *   import { debounce, deepClone } from '@/utils/helpers';
 * ============================================================
 */

/**
 * Trì hoãn gọi hàm cho tới khi người dùng NGỪNG gọi trong `wait` ms.
 * Dùng cho ô tìm kiếm: gõ liên tục không gọi API liên tục, chỉ gọi khi ngừng gõ.
 * @param {Function} fn - hàm cần trì hoãn
 * @param {number} [wait=400] - thời gian chờ (ms)
 * @returns {Function} hàm đã bọc, có thêm `.cancel()` để huỷ lần gọi đang chờ
 * @example const onSearch = debounce(() => loadData(), 400);
 */
export const debounce = (fn, wait = 400) => {
  let timer = null;
  const debounced = (...args) => {
    clearTimeout(timer);
    timer = setTimeout(() => fn(...args), wait);
  };
  debounced.cancel = () => clearTimeout(timer);
  return debounced;
};

/**
 * Sao chép sâu một object/array — sửa bản sao không ảnh hưởng bản gốc.
 * Dùng khi mở form sửa: chỉnh trên bản sao, bấm Huỷ thì dòng trong bảng không đổi.
 * @template T
 * @param {T} value
 * @returns {T}
 */
export const deepClone = (value) => structuredClone(value);

/**
 * Tạo id ngẫu nhiên ngắn (dùng cho key toast, phần tử tạm thời).
 * KHÔNG dùng làm id bản ghi — id bản ghi do backend sinh.
 * @returns {string}
 */
export const uid = () => Math.random().toString(36).slice(2, 10);

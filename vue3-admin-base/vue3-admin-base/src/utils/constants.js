/**
 * ============================================================
 * FILE: src/utils/constants.js
 * TÁC DỤNG:
 *   Gom toàn bộ hằng số dùng chung (enum, key localStorage, kích thước trang...)
 *   về một chỗ → tránh "magic string" rải rác, đổi một nơi là đổi toàn app.
 * CÁCH DÙNG:
 *   import { GENDER_OPTIONS, STORAGE_KEYS } from '@/utils/constants';
 *   <ms-select :options="GENDER_OPTIONS" />
 * QUY ƯỚC:
 *   - Tên hằng: UPPER_SNAKE_CASE.
 *   - Danh sách cho dropdown dạng [{ value, label }] để dùng thẳng với MsSelect.
 *   - Dùng Object.freeze để không ai lỡ tay sửa giá trị lúc chạy.
 * ============================================================
 */

/** Key lưu trong localStorage */
export const STORAGE_KEYS = Object.freeze({
  TOKEN: 'app_token',
  USER: 'app_user',
  SIDEBAR_COLLAPSED: 'app_sidebar_collapsed',
});

/** Các lựa chọn số bản ghi / trang */
export const PAGE_SIZES = Object.freeze([10, 20, 50, 100]);

/** Số bản ghi / trang mặc định */
export const DEFAULT_PAGE_SIZE = 10;

/** Kiểu toast thông báo */
export const TOAST_TYPE = Object.freeze({
  SUCCESS: 'success',
  ERROR: 'error',
  WARNING: 'warning',
  INFO: 'info',
});

/** Giới tính (value lưu DB, label hiển thị) */
export const GENDER = Object.freeze({ MALE: 1, FEMALE: 2, OTHER: 3 });
export const GENDER_OPTIONS = Object.freeze([
  { value: GENDER.MALE, label: 'Nam' },
  { value: GENDER.FEMALE, label: 'Nữ' },
  { value: GENDER.OTHER, label: 'Khác' },
]);

/** Phòng ban (thực tế nên lấy từ API, ở đây để tĩnh cho ví dụ) */
export const DEPARTMENT_OPTIONS = Object.freeze([
  { value: 'DEV', label: 'Phát triển' },
  { value: 'QA', label: 'Kiểm thử' },
  { value: 'HR', label: 'Nhân sự' },
  { value: 'ACC', label: 'Kế toán' },
  { value: 'SALE', label: 'Kinh doanh' },
]);

/**
 * Lấy label từ value trong một danh sách option.
 * @param {Array<{value:any,label:string}>} options - danh sách option
 * @param {any} value - giá trị cần tra
 * @returns {string} label tương ứng, không thấy thì trả ''
 * @example getOptionLabel(GENDER_OPTIONS, 1) // 'Nam'
 */
export const getOptionLabel = (options, value) =>
  options.find((o) => o.value === value)?.label ?? '';

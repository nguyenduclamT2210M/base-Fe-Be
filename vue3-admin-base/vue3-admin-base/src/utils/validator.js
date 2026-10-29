/**
 * ============================================================
 * FILE: src/utils/validator.js
 * TÁC DỤNG:
 *   Các hàm kiểm tra dữ liệu (validate) phía giao diện.
 *   Gồm 2 lớp:
 *     1. Hàm kiểm tra đơn: isEmpty, isEmail, isPhone...  → trả true/false
 *     2. "Rule" + validateFields: mô tả luật theo từng trường, trả về object lỗi.
 * CÁCH DÙNG:
 *   import { rules, validateFields } from '@/utils/validator';
 *   const errors = validateFields(form, {
 *     Name:  [rules.required('Họ tên'), rules.maxLength('Họ tên', 100)],
 *     Email: [rules.email()],
 *   });
 *   // errors = { Name: 'Họ tên không được để trống' }  — rỗng {} là hợp lệ
 * LƯU Ý:
 *   Validate frontend chỉ để báo lỗi sớm cho người dùng. Backend BẮT BUỘC validate lại,
 *   vì ai cũng có thể gọi thẳng API mà bỏ qua giao diện.
 * ============================================================
 */

/**
 * Kiểm tra giá trị rỗng (null, undefined, chuỗi toàn khoảng trắng, mảng rỗng).
 * @param {any} value
 * @returns {boolean} true nếu rỗng
 */
export const isEmpty = (value) => {
  if (value === null || value === undefined) return true;
  if (typeof value === 'string') return value.trim() === '';
  if (Array.isArray(value)) return value.length === 0;
  return false;
};

/**
 * Kiểm tra định dạng email.
 * @param {string} value
 * @returns {boolean}
 */
export const isEmail = (value) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(value).trim());

/**
 * Kiểm tra số điện thoại Việt Nam (10 số, bắt đầu bằng 0; hoặc +84).
 * @param {string} value
 * @returns {boolean}
 */
export const isPhone = (value) => /^(0|\+84)\d{9}$/.test(String(value).replace(/\s/g, ''));

/**
 * Bộ "rule" tạo sẵn. Mỗi rule là một hàm (value) => chuỗi lỗi | '' (hợp lệ).
 * Các rule trừ `required` đều BỎ QUA khi giá trị rỗng → trường không bắt buộc vẫn hợp lệ.
 */
export const rules = {
  /** Bắt buộc nhập. @param {string} label - tên trường hiển thị trong thông báo */
  required: (label) => (v) => (isEmpty(v) ? `${label} không được để trống` : ''),

  /** Độ dài tối đa. @param {string} label @param {number} max */
  maxLength: (label, max) => (v) =>
    !isEmpty(v) && String(v).length > max ? `${label} tối đa ${max} ký tự` : '',

  /** Email hợp lệ. */
  email: () => (v) => (!isEmpty(v) && !isEmail(v) ? 'Email không đúng định dạng' : ''),

  /** Số điện thoại hợp lệ. */
  phone: () => (v) => (!isEmpty(v) && !isPhone(v) ? 'Số điện thoại không đúng định dạng' : ''),

  /** Số >= min. @param {string} label @param {number} min */
  min: (label, min) => (v) =>
    !isEmpty(v) && Number(v) < min ? `${label} phải lớn hơn hoặc bằng ${min}` : '',

  /** Ngày không được lớn hơn hôm nay. @param {string} label */
  notFutureDate: (label) => (v) =>
    !isEmpty(v) && new Date(v) > new Date() ? `${label} không được lớn hơn ngày hiện tại` : '',
};

/**
 * Chạy toàn bộ rule cho một object dữ liệu.
 * Mỗi trường dừng ở lỗi ĐẦU TIÊN gặp phải.
 * @param {Object} data - dữ liệu form
 * @param {Object<string, Array<Function>>} schema - { tênTrường: [rule1, rule2] }
 * @returns {Object<string,string>} object lỗi { tênTrường: 'thông báo' }, rỗng nếu hợp lệ
 */
export const validateFields = (data, schema) => {
  const errors = {};
  for (const [field, fieldRules] of Object.entries(schema)) {
    for (const rule of fieldRules) {
      const message = rule(data[field]);
      if (message) {
        errors[field] = message;
        break;
      }
    }
  }
  return errors;
};

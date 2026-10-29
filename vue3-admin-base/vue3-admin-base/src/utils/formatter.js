/**
 * ============================================================
 * FILE: src/utils/formatter.js
 * TÁC DỤNG:
 *   Các hàm định dạng dữ liệu để HIỂN THỊ (số, tiền, ngày, chữ).
 *   Không dùng để định dạng dữ liệu gửi lên API.
 * CÁCH DÙNG:
 *   import { formatCurrency, formatDate } from '@/utils/formatter';
 *   {{ formatCurrency(row.Salary) }}   →  15.000.000 ₫
 *   {{ formatDate(row.BirthDate) }}    →  15/01/1990
 *   MsTable tự gọi các hàm này theo `column.type`.
 * LƯU Ý:
 *   - Mọi hàm đều chịu được null/undefined → trả '' thay vì ném lỗi.
 *   - Locale cố định 'vi-VN' để hiển thị giống nhau trên mọi máy.
 * ============================================================
 */

const LOCALE = 'vi-VN';

/**
 * Định dạng số có phân tách hàng nghìn.
 * @param {number|string} value - giá trị số
 * @param {number} [fractionDigits=0] - số chữ số thập phân tối đa
 * @returns {string} vd: 1234567.8 → "1.234.568"
 */
export const formatNumber = (value, fractionDigits = 0) => {
  if (value === null || value === undefined || value === '') return '';
  const num = Number(value);
  if (Number.isNaN(num)) return String(value);
  return new Intl.NumberFormat(LOCALE, { maximumFractionDigits: fractionDigits }).format(num);
};

/**
 * Định dạng tiền tệ.
 * @param {number|string} value - số tiền
 * @param {string} [currency='VND'] - mã tiền tệ ISO (VND, USD...)
 * @returns {string} vd: 15000000 → "15.000.000 ₫"
 */
export const formatCurrency = (value, currency = 'VND') => {
  if (value === null || value === undefined || value === '') return '';
  const num = Number(value);
  if (Number.isNaN(num)) return String(value);
  return new Intl.NumberFormat(LOCALE, { style: 'currency', currency }).format(num);
};

/**
 * Chuyển chuỗi/Date thành đối tượng Date an toàn.
 * Chuỗi dạng "yyyy-MM-dd" được hiểu là giờ ĐỊA PHƯƠNG (new Date mặc định hiểu là UTC,
 * có thể lệch 1 ngày ở múi giờ âm).
 * @param {string|Date} value
 * @returns {Date|null} null nếu không hợp lệ
 */
export const parseDate = (value) => {
  if (!value) return null;
  if (value instanceof Date) return Number.isNaN(value.getTime()) ? null : value;
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  const date = m ? new Date(+m[1], +m[2] - 1, +m[3]) : new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
};

/**
 * Định dạng ngày để hiển thị.
 * @param {string|Date} value - ngày cần định dạng
 * @returns {string} "dd/MM/yyyy", vd: "15/01/1990"
 */
export const formatDate = (value) => {
  const date = parseDate(value);
  if (!date) return '';
  const dd = String(date.getDate()).padStart(2, '0');
  const mm = String(date.getMonth() + 1).padStart(2, '0');
  return `${dd}/${mm}/${date.getFullYear()}`;
};

/**
 * Định dạng ngày + giờ để hiển thị.
 * @param {string|Date} value
 * @returns {string} "dd/MM/yyyy HH:mm"
 */
export const formatDateTime = (value) => {
  const date = parseDate(value);
  if (!date) return '';
  const hh = String(date.getHours()).padStart(2, '0');
  const mi = String(date.getMinutes()).padStart(2, '0');
  return `${formatDate(date)} ${hh}:${mi}`;
};

/**
 * Chuyển Date thành chuỗi "yyyy-MM-dd" — đúng định dạng của <input type="date">
 * và định dạng thường dùng khi gửi API.
 * @param {string|Date} value
 * @returns {string}
 */
export const toISODate = (value) => {
  const date = parseDate(value);
  if (!date) return '';
  const mm = String(date.getMonth() + 1).padStart(2, '0');
  const dd = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${mm}-${dd}`;
};

/**
 * Chuyển giá trị bất kỳ thành chuỗi hiển thị.
 * @param {any} value
 * @returns {string} null/undefined → ''
 */
export const formatText = (value) => {
  if (value === null || value === undefined) return '';
  return String(value);
};

/**
 * Định dạng giá trị theo kiểu cột — MsTable dùng hàm này.
 * @param {any} value - giá trị ô
 * @param {'text'|'number'|'currency'|'date'|'datetime'} [type='text']
 * @returns {string}
 */
export const formatByType = (value, type = 'text') => {
  switch (type) {
    case 'number':
      return formatNumber(value);
    case 'currency':
      return formatCurrency(value);
    case 'date':
      return formatDate(value);
    case 'datetime':
      return formatDateTime(value);
    default:
      return formatText(value);
  }
};

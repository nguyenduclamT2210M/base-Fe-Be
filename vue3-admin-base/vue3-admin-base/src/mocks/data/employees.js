/**
 * ============================================================
 * FILE: src/mocks/data/employees.js
 * TÁC DỤNG:
 *   Sinh 57 nhân viên giả, đủ nhiều để thử phân trang, tìm kiếm, sắp xếp.
 *   Dữ liệu sinh theo công thức cố định (không random) → lần nào chạy cũng giống nhau,
 *   dễ tái hiện lỗi.
 * CÁCH DÙNG:
 *   Chỉ mockServer.js dùng. Không import file này ở view.
 * ============================================================
 */
import { DEPARTMENT_OPTIONS, GENDER } from '@/utils/constants';

const LAST_NAMES = ['Nguyễn', 'Trần', 'Lê', 'Phạm', 'Hoàng', 'Vũ', 'Đặng', 'Bùi'];
const MIDDLE_NAMES = ['Văn', 'Thị', 'Minh', 'Thu', 'Đức', 'Ngọc'];
const FIRST_NAMES = ['An', 'Bình', 'Chi', 'Dũng', 'Hà', 'Hùng', 'Lan', 'Long', 'Mai', 'Nam', 'Phương', 'Tuấn'];

/**
 * Sinh danh sách nhân viên giả.
 * @param {number} count - số nhân viên
 * @returns {Array<Object>}
 */
const generate = (count) =>
  Array.from({ length: count }, (_, i) => {
    const n = i + 1;
    const middle = MIDDLE_NAMES[n % MIDDLE_NAMES.length];
    return {
      Id: n,
      Code: `NV${String(n).padStart(3, '0')}`,
      FullName: `${LAST_NAMES[n % LAST_NAMES.length]} ${middle} ${FIRST_NAMES[(n * 7) % FIRST_NAMES.length]}`,
      Gender: middle === 'Thị' || middle === 'Thu' || middle === 'Ngọc' ? GENDER.FEMALE : GENDER.MALE,
      BirthDate: `${1980 + (n % 20)}-${String((n % 12) + 1).padStart(2, '0')}-${String((n % 27) + 1).padStart(2, '0')}`,
      Email: `nv${String(n).padStart(3, '0')}@company.vn`,
      Phone: `09${String(10000000 + n * 1234567).slice(0, 8)}`,
      Department: DEPARTMENT_OPTIONS[n % DEPARTMENT_OPTIONS.length].value,
      Salary: 8_000_000 + (n % 10) * 1_500_000,
      IsActive: n % 6 !== 0,
    };
  });

export const employees = generate(57);

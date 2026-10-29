/**
 * ============================================================
 * FILE: src/models/EmployeeModel.js
 * TÁC DỤNG:
 *   Định nghĩa CẤU TRÚC dữ liệu Nhân viên ở frontend:
 *   - constructor : chuẩn hoá dữ liệu (gán mặc định, ép kiểu) — dù API trả thiếu trường
 *                   thì form vẫn có đủ field, không bị undefined.
 *   - validate()  : luật kiểm tra dữ liệu của nhân viên (đặt cạnh model để dùng lại)
 *   - toPayload() : chuyển về đúng định dạng gửi API (bỏ trường thừa, ép kiểu số)
 * CÁCH DÙNG:
 *   const model = new EmployeeModel(rowFromApi); // tạo bản sao để sửa trong form
 *   const errors = model.validate();             // {} là hợp lệ
 *   await EmployeeAPI.update(model.Id, model.toPayload());
 *   EmployeeModel.empty()                        // bản ghi trống cho form Thêm mới
 * QUY ƯỚC:
 *   Tên trường dùng PascalCase cho khớp backend .NET. Backend dùng camelCase thì đổi đồng loạt.
 * ============================================================
 */
import { GENDER } from '@/utils/constants';
import { toISODate } from '@/utils/formatter';
import { rules, validateFields } from '@/utils/validator';

export class EmployeeModel {
  /**
   * @param {Object} [data={}] - dữ liệu thô (từ API hoặc form)
   */
  constructor(data = {}) {
    this.Id = data.Id ?? null;
    this.Code = data.Code ?? '';
    this.FullName = data.FullName ?? '';
    this.Gender = data.Gender ?? GENDER.MALE;
    this.BirthDate = toISODate(data.BirthDate); // "yyyy-MM-dd" cho <input type="date">
    this.Email = data.Email ?? '';
    this.Phone = data.Phone ?? '';
    this.Department = data.Department ?? '';
    this.Salary = data.Salary ?? 0;
    this.IsActive = data.IsActive ?? true;
  }

  /**
   * Tạo nhân viên trống cho form Thêm mới.
   * @returns {EmployeeModel}
   */
  static empty() {
    return new EmployeeModel();
  }

  /** true nếu là bản ghi đang sửa (đã có Id), false nếu đang thêm mới */
  get isEdit() {
    return this.Id !== null;
  }

  /**
   * Kiểm tra dữ liệu theo luật nghiệp vụ.
   * @returns {Object<string,string>} { tênTrường: 'lỗi' }, rỗng nếu hợp lệ
   */
  validate() {
    return validateFields(this, {
      Code: [rules.required('Mã nhân viên'), rules.maxLength('Mã nhân viên', 20)],
      FullName: [rules.required('Họ và tên'), rules.maxLength('Họ và tên', 100)],
      BirthDate: [rules.notFutureDate('Ngày sinh')],
      Email: [rules.email()],
      Phone: [rules.phone()],
      Department: [rules.required('Phòng ban')],
      Salary: [rules.min('Lương', 0)],
    });
  }

  /**
   * Chuyển về object thuần để gửi API: trim chuỗi, ép số, ngày rỗng → null.
   * @returns {Object}
   */
  toPayload() {
    return {
      Id: this.Id,
      Code: this.Code.trim(),
      FullName: this.FullName.trim(),
      Gender: Number(this.Gender),
      BirthDate: this.BirthDate || null,
      Email: this.Email.trim(),
      Phone: this.Phone.trim(),
      Department: this.Department,
      Salary: Number(this.Salary) || 0,
      IsActive: !!this.IsActive,
    };
  }
}

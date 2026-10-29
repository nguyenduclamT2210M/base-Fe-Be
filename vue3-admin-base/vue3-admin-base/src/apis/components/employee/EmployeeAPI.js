/**
 * ============================================================
 * FILE: src/apis/components/employee/EmployeeAPI.js
 * TÁC DỤNG:
 *   API cho tài nguyên Nhân viên. Kế thừa đủ CRUD từ BaseAPI,
 *   bổ sung các hàm nghiệp vụ riêng (kiểm tra trùng mã, thống kê).
 * CÁCH DÙNG:
 *   import EmployeeAPI from '@/apis/components/employee/EmployeeAPI';
 *   const { data, total } = await EmployeeAPI.paging({ pageIndex: 1, pageSize: 10 });
 *   await EmployeeAPI.create(model.toPayload());
 *   const { isDuplicate } = await EmployeeAPI.checkDuplicateCode('NV001', id);
 * ============================================================
 */
import BaseAPI from '@/apis/base/BaseAPI';

class EmployeeAPI extends BaseAPI {
  constructor() {
    super('employees');
  }

  /**
   * Kiểm tra mã nhân viên đã tồn tại chưa.
   * @param {string} code - mã cần kiểm tra
   * @param {string|number|null} [excludeId] - id bản ghi đang sửa (bỏ qua chính nó)
   * @returns {Promise<{isDuplicate:boolean}>}
   */
  checkDuplicateCode(code, excludeId = null) {
    return this.api.post(
      `${this.controller}/check-duplicate`,
      { code, excludeId },
      { silent: true },
    );
  }

  /**
   * Lấy số liệu thống kê cho Dashboard.
   * @returns {Promise<{total:number, totalSalary:number, avgSalary:number, byDepartment:Array}>}
   */
  getStatistics() {
    return this.api.get(`${this.controller}/statistics`);
  }
}

export default new EmployeeAPI();

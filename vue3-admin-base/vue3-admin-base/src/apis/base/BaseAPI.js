/**
 * ============================================================
 * FILE: src/apis/base/BaseAPI.js
 * TÁC DỤNG:
 *   Lớp cha chứa các hàm CRUD chung cho MỌI tài nguyên (resource) theo chuẩn REST:
 *     GET    /{controller}            → getAll
 *     GET    /{controller}/{id}       → getById
 *     POST   /{controller}/paging     → paging (lọc + phân trang + sắp xếp)
 *     POST   /{controller}            → create
 *     PUT    /{controller}/{id}       → update
 *     DELETE /{controller}/{id}       → delete
 *     POST   /{controller}/delete-many→ deleteMany
 *   Lớp con chỉ cần khai báo `controller` là có đủ CRUD.
 * CÁCH DÙNG (tạo API cho resource mới):
 *   class ProductAPI extends BaseAPI {
 *     constructor() { super('products'); }
 *     // thêm hàm nghiệp vụ riêng nếu cần
 *     getBestSellers() { return this.api.get(`${this.controller}/best-sellers`); }
 *   }
 *   export default new ProductAPI();   // export 1 instance dùng chung (singleton)
 * LƯU Ý:
 *   Nếu backend dùng URL khác chuẩn trên (vd: /products/update/{id}) thì sửa ở
 *   đây một lần cho cả dự án, hoặc override trong lớp con.
 * ============================================================
 */
import api from '@/apis/config/APIConfig';

export default class BaseAPI {
  /**
   * @param {string} controller - tên tài nguyên trên URL, vd 'employees'
   */
  constructor(controller) {
    if (!controller) throw new Error('BaseAPI: thiếu tên controller');
    /** Tên tài nguyên trên URL */
    this.controller = controller;
    /** Instance axios dùng chung (lớp con dùng để viết hàm riêng) */
    this.api = api;
  }

  /**
   * Lấy toàn bộ bản ghi (chỉ dùng cho danh mục nhỏ: phòng ban, chức vụ...).
   * @param {Object} [config] - tuỳ chọn axios: { silent, skipToast, params }
   * @returns {Promise<Array>}
   */
  getAll(config) {
    return api.get(this.controller, config);
  }

  /**
   * Lấy chi tiết một bản ghi.
   * @param {string|number} id
   * @param {Object} [config]
   * @returns {Promise<Object>}
   */
  getById(id, config) {
    return api.get(`${this.controller}/${id}`, config);
  }

  /**
   * Lấy dữ liệu phân trang có lọc và sắp xếp.
   * @param {{pageIndex:number, pageSize:number, keyword?:string, sortBy?:string, sortOrder?:'asc'|'desc', filters?:Object}} payload
   * @param {Object} [config]
   * @returns {Promise<{data:Array, total:number}>}
   */
  paging(payload, config) {
    return api.post(`${this.controller}/paging`, payload, config);
  }

  /**
   * Thêm mới.
   * @param {Object} body - dữ liệu bản ghi
   * @param {Object} [config]
   * @returns {Promise<Object>} bản ghi vừa tạo (có id do backend sinh)
   */
  create(body, config) {
    return api.post(this.controller, body, config);
  }

  /**
   * Cập nhật toàn bộ bản ghi.
   * @param {string|number} id
   * @param {Object} body
   * @param {Object} [config]
   * @returns {Promise<Object>} bản ghi sau khi cập nhật
   */
  update(id, body, config) {
    return api.put(`${this.controller}/${id}`, body, config);
  }

  /**
   * Xoá một bản ghi.
   * @param {string|number} id
   * @param {Object} [config]
   * @returns {Promise<any>}
   */
  delete(id, config) {
    return api.delete(`${this.controller}/${id}`, config);
  }

  /**
   * Xoá nhiều bản ghi trong một lần gọi.
   * @param {Array<string|number>} ids
   * @param {Object} [config]
   * @returns {Promise<{deleted:number}>}
   */
  deleteMany(ids, config) {
    return api.post(`${this.controller}/delete-many`, { ids }, config);
  }
}

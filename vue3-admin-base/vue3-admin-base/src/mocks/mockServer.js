/**
 * ============================================================
 * FILE: src/mocks/mockServer.js
 * TÁC DỤNG:
 *   Backend GIẢ chạy ngay trong trình duyệt. Thay "adapter" của axios
 *   (lớp thực sự gửi HTTP) bằng một hàm trả dữ liệu từ bộ nhớ.
 *   → Frontend làm song song khi backend chưa xong, demo không cần server.
 * CÁCH DÙNG:
 *   - Bật/tắt bằng VITE_USE_MOCK trong file .env (APIConfig.js tự gắn adapter).
 *   - Thêm API giả: thêm 1 dòng vào mảng `routes` bên dưới:
 *       { method: 'get', path: /^products$/, handler: () => ok(products) }
 *     `handler` nhận ({ body, params, match }) và trả ok(data) hoặc fail(status, message).
 *   - Tài khoản đăng nhập giả: admin / 123456
 * LƯU Ý:
 *   - Dữ liệu nằm trong bộ nhớ → F5 trang là quay về dữ liệu ban đầu.
 *   - Khi có backend thật: đặt VITE_USE_MOCK=false. Không cần xoá thư mục mocks.
 *   - Mock phải trả ĐÚNG định dạng backend thật sẽ trả, nếu không sẽ lỗi lúc ghép nối.
 * ============================================================
 */
import { employees } from './data/employees';
import { DEPARTMENT_OPTIONS } from '@/utils/constants';

const MOCK_DELAY = 300; // giả lập độ trễ mạng (ms)
let nextId = employees.length + 1;

/**
 * Tạo kết quả thành công.
 * @param {any} data - dữ liệu trả về
 * @param {number} [status=200]
 */
const ok = (data, status = 200) => ({ status, data });

/**
 * Tạo kết quả lỗi.
 * @param {number} status - mã HTTP
 * @param {string} message - thông báo lỗi
 */
const fail = (status, message) => ({ status, data: { message } });

/**
 * Lọc + sắp xếp + phân trang danh sách nhân viên (giống việc backend sẽ làm bằng SQL).
 * @param {{pageIndex:number,pageSize:number,keyword?:string,sortBy?:string,sortOrder?:string,filters?:Object}} payload
 * @returns {{data:Array,total:number}}
 */
const pagingEmployees = ({ pageIndex = 1, pageSize = 10, keyword = '', sortBy, sortOrder, filters = {} }) => {
  const kw = keyword.trim().toLowerCase();
  let list = employees.filter(
    (e) =>
      !kw ||
      e.Code.toLowerCase().includes(kw) ||
      e.FullName.toLowerCase().includes(kw) ||
      e.Email.toLowerCase().includes(kw),
  );
  if (filters.Department) list = list.filter((e) => e.Department === filters.Department);

  if (sortBy) {
    const dir = sortOrder === 'desc' ? -1 : 1;
    list = [...list].sort((a, b) => (a[sortBy] > b[sortBy] ? dir : a[sortBy] < b[sortBy] ? -dir : 0));
  }
  const start = (pageIndex - 1) * pageSize;
  return { data: list.slice(start, start + pageSize), total: list.length };
};

/** Bảng định tuyến API giả: method + regex đường dẫn (đã bỏ baseURL) → handler */
const routes = [
  {
    method: 'post',
    path: /^auth\/login$/,
    handler: ({ body }) =>
      body.username === 'admin' && body.password === '123456'
        ? ok({ token: 'mock-token-' + Date.now(), user: { Id: 1, FullName: 'Quản trị viên', Username: 'admin' } })
        : fail(400, 'Sai tên đăng nhập hoặc mật khẩu'),
  },
  {
    method: 'get',
    path: /^employees\/statistics$/,
    handler: () => {
      const totalSalary = employees.reduce((s, e) => s + e.Salary, 0);
      return ok({
        total: employees.length,
        active: employees.filter((e) => e.IsActive).length,
        totalSalary,
        avgSalary: employees.length ? Math.round(totalSalary / employees.length) : 0,
        byDepartment: DEPARTMENT_OPTIONS.map((d) => ({
          department: d.label,
          count: employees.filter((e) => e.Department === d.value).length,
        })),
      });
    },
  },
  { method: 'post', path: /^employees\/paging$/, handler: ({ body }) => ok(pagingEmployees(body)) },
  {
    method: 'post',
    path: /^employees\/check-duplicate$/,
    handler: ({ body }) =>
      ok({
        isDuplicate: employees.some(
          (e) => e.Code.toLowerCase() === String(body.code).toLowerCase() && e.Id !== body.excludeId,
        ),
      }),
  },
  {
    method: 'post',
    path: /^employees\/delete-many$/,
    handler: ({ body }) => {
      const before = employees.length;
      const ids = new Set(body.ids);
      for (let i = employees.length - 1; i >= 0; i--) if (ids.has(employees[i].Id)) employees.splice(i, 1);
      return ok({ deleted: before - employees.length });
    },
  },
  { method: 'get', path: /^employees$/, handler: () => ok(employees) },
  {
    method: 'get',
    path: /^employees\/(\d+)$/,
    handler: ({ match }) => {
      const item = employees.find((e) => e.Id === +match[1]);
      return item ? ok(item) : fail(404, 'Không tìm thấy nhân viên');
    },
  },
  {
    method: 'post',
    path: /^employees$/,
    handler: ({ body }) => {
      if (employees.some((e) => e.Code === body.Code)) return fail(409, `Mã ${body.Code} đã tồn tại`);
      const item = { ...body, Id: nextId++ };
      employees.unshift(item);
      return ok(item, 201);
    },
  },
  {
    method: 'put',
    path: /^employees\/(\d+)$/,
    handler: ({ match, body }) => {
      const idx = employees.findIndex((e) => e.Id === +match[1]);
      if (idx === -1) return fail(404, 'Không tìm thấy nhân viên');
      employees[idx] = { ...body, Id: +match[1] };
      return ok(employees[idx]);
    },
  },
  {
    method: 'delete',
    path: /^employees\/(\d+)$/,
    handler: ({ match }) => {
      const idx = employees.findIndex((e) => e.Id === +match[1]);
      if (idx === -1) return fail(404, 'Không tìm thấy nhân viên');
      employees.splice(idx, 1);
      return ok({ deleted: 1 });
    },
  },
];

/**
 * Adapter axios giả: nhận config request, tìm route khớp, trả response giống axios thật.
 * Status ngoài 2xx sẽ reject kèm `response` → interceptor xử lý lỗi như với backend thật.
 * @param {import('axios').InternalAxiosRequestConfig} config
 * @returns {Promise<import('axios').AxiosResponse>}
 */
export const mockAdapter = (config) =>
  new Promise((resolve, reject) => {
    const method = (config.method || 'get').toLowerCase();
    const path = (config.url || '').replace(/^\/+/, '').split('?')[0];
    const body = typeof config.data === 'string' ? JSON.parse(config.data || '{}') : config.data || {};

    let result = fail(404, `[Mock] Chưa định nghĩa API ${method.toUpperCase()} ${path}`);
    for (const route of routes) {
      const match = route.method === method && route.path.exec(path);
      if (match) {
        result = route.handler({ body, params: config.params || {}, match });
        break;
      }
    }

    const response = {
      data: structuredClone(result.data), // clone để view không sửa thẳng vào "database" giả
      status: result.status,
      statusText: String(result.status),
      headers: {},
      config,
      request: {},
    };

    setTimeout(() => {
      if (result.status >= 200 && result.status < 300) resolve(response);
      else reject(Object.assign(new Error(result.data.message), { config, response, isAxiosError: true }));
    }, MOCK_DELAY);
  });

# Vue 3 Admin Base

Base giao diện quản trị dùng chung: **Vue 3 + Vite + Vue Router + Pinia + Axios**.
Chạy được ngay với **mock API** (chưa cần backend). Có sẵn màn mẫu CRUD Nhân viên để copy khi làm màn mới.

Mọi file đều có **ghi chú ở đầu file** (tác dụng + cách dùng), mọi hàm đều có JSDoc.
Khi chưa rõ một file dùng để làm gì, mở file đó ra đọc phần đầu.

## 1. Chạy dự án

```bash
npm install
npm run dev        # http://localhost:5173  — tài khoản mock: admin / 123456
npm run build      # build production ra dist/
npm run lint       # kiểm tra + tự sửa lỗi code style
npm run format     # format code bằng Prettier
```

Yêu cầu Node.js 20.19+ hoặc 22.12+ (yêu cầu của Vite 7).

## 2. Cấu trúc thư mục

```
src/
├── apis/
│   ├── config/APIConfig.js        # axios instance + interceptor (token, loading, lỗi, 401)
│   ├── base/BaseAPI.js            # CRUD chung: getAll/getById/paging/create/update/delete/deleteMany
│   └── components/                # API theo từng resource, kế thừa BaseAPI
│       ├── auth/AuthAPI.js
│       └── employee/EmployeeAPI.js
├── assets/styles/
│   ├── variables.css              # design token: màu, khoảng cách, z-index
│   └── base.css                   # reset + class tiện ích (.page, .toolbar...)
├── components/                    # component dùng chung, tiền tố "ms-"
│   ├── ms-button  ms-input  ms-select  ms-checkbox  ms-icon
│   ├── ms-table   ms-paging ms-modal
│   └── ms-toast   ms-confirm ms-loading   # 3 component toàn cục, đặt trong App.vue
├── composables/
│   ├── useCrud.js                 # logic màn danh sách: tải, tìm, lọc, sắp xếp, phân trang, lưu, xoá
│   └── useConfirm.js              # hộp xác nhận dạng Promise: if (await confirm('...'))
├── layouts/MainLayout.vue         # sidebar + header, menu tự sinh từ router
├── mocks/                         # backend giả (bật/tắt bằng VITE_USE_MOCK)
├── models/EmployeeModel.js        # chuẩn hoá dữ liệu + validate + toPayload
├── router/index.js                # khai báo trang + guard đăng nhập
├── stores/                        # Pinia: app (toast, loading, sidebar), auth (token, user)
├── utils/                         # constants, formatter, validator, storage, helpers
├── views/                         # các trang: auth, dashboard, employee, error
├── App.vue
└── main.js
```

## 3. Luồng dữ liệu

```
View (EmployeeList.vue)
  │  gọi
  ▼
useCrud (state: rows, total, query, loading)        useConfirm / store app (toast)
  │  gọi
  ▼
EmployeeAPI  ──extends──▶  BaseAPI
  │  dùng
  ▼
APIConfig (axios + interceptor)  ──▶  Backend thật  |  mockServer (VITE_USE_MOCK=true)
        │ request : gắn token, bật loading
        │ response: tắt loading, trả về response.data
        └ lỗi     : toast thông báo, 401 → logout + về /login
```

## 4. Thêm một màn CRUD mới (vd: Sản phẩm)

| Bước | Việc cần làm | File |
|---|---|---|
| 1 | Tạo API: `class ProductAPI extends BaseAPI { constructor(){ super('products') } }` | `apis/components/product/ProductAPI.js` |
| 2 | Tạo model: constructor, `validate()`, `toPayload()` | `models/ProductModel.js` |
| 3 | Copy `EmployeeList.vue` + `EmployeeForm.vue`, đổi API, `columns` và các ô form | `views/product/` |
| 4 | Khai báo route con trong MainLayout, có `meta: { title, menu: true, icon }`. Menu tự xuất hiện | `router/index.js` |
| 5 | (Nếu dùng mock) thêm các route giả cho `products` | `mocks/mockServer.js` |

## 5. Ghép backend thật

1. Sửa `.env.development`: `VITE_USE_MOCK=false`, `VITE_API_URL=<địa chỉ backend>`.
2. Kiểm tra quy ước URL trong `BaseAPI.js` khớp backend (REST: `PUT /employees/{id}`...). Khác thì sửa ở đây một lần.
3. Backend bọc response dạng `{ success, data, message }` → sửa `handleResponse` trong `APIConfig.js`.
4. Paging cần backend nhận `{ pageIndex, pageSize, keyword, sortBy, sortOrder, filters }` và trả `{ data, total }`.
5. Lỗi CORS khi dev → bật `server.proxy` trong `vite.config.js`.

## 6. Quy ước

| Loại | Quy ước | Ví dụ |
|---|---|---|
| Thư mục | kebab-case | `ms-button/` |
| Component | PascalCase, component dùng chung có tiền tố `Ms` | `MsTable.vue`, `EmployeeForm.vue` |
| File JS | camelCase. Class/API dùng PascalCase | `formatter.js`, `EmployeeAPI.js` |
| Hằng số | UPPER_SNAKE_CASE, gom trong `utils/constants.js` | `PAGE_SIZES` |
| Màu, khoảng cách | Luôn dùng CSS variable, không viết mã màu cứng | `var(--color-primary)` |
| Comment | Đầu file: TÁC DỤNG + CÁCH DÙNG. Mỗi hàm: JSDoc. Code chia bằng `//#region` | |

## 7. Lưu ý khi mở rộng

- **Validate frontend chỉ để báo lỗi sớm.** Backend bắt buộc validate lại.
- **Token đang lưu localStorage** (đơn giản, phổ biến). Script độc hại trên trang (XSS) đọc được. Hệ thống yêu cầu bảo mật cao nên dùng cookie `HttpOnly` do backend cấp.
- **Phân quyền theo vai trò** (admin/user) chưa có. Hướng làm: thêm `meta.roles` cho route, kiểm tra trong `router.beforeEach`, rồi lọc menu theo quyền.
- **TypeScript**: nên chuyển khi team > 2–3 người hoặc dự án dài hạn, vì bắt được lỗi sai tên trường ngay lúc gõ.
- **Thư viện UI** (Element Plus, Ant Design Vue, PrimeVue): nếu dự án cần date picker, tree, upload, virtual scroll... thì dùng thư viện thay vì tự viết hết. Base này tự viết component để dễ đọc, dễ học.

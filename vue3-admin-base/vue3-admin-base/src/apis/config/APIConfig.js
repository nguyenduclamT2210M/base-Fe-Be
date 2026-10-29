/**
 * ============================================================
 * FILE: src/apis/config/APIConfig.js
 * TÁC DỤNG:
 *   Tạo MỘT instance axios dùng chung cho toàn bộ app và gắn "interceptor"
 *   (hàm chặn chạy trước/sau mọi request) để xử lý tập trung:
 *     - Request : gắn token Authorization, bật loading
 *     - Response: tắt loading, bóc lớp response → chỉ trả về `data`
 *     - Lỗi     : hiện toast theo mã lỗi, 401 thì đăng xuất và về trang Login
 *   Nếu VITE_USE_MOCK=true → chuyển sang mock adapter (không cần backend).
 * CÁCH DÙNG:
 *   Thường KHÔNG import trực tiếp ở view. Hãy dùng qua lớp API (BaseAPI/EmployeeAPI).
 *   Tuỳ chọn thêm cho từng request (truyền trong config axios):
 *     { silent: true }     → không bật loading toàn màn hình
 *     { skipToast: true }  → không tự hiện toast lỗi (nơi gọi tự xử lý)
 *   vd: api.get('/employees', { silent: true });
 * QUY ƯỚC RESPONSE:
 *   Interceptor đã trả về `response.data`, nên:  const list = await EmployeeAPI.getAll();
 *   (không cần .data nữa). Nếu backend bọc { success, data, message } thì sửa
 *   hàm `handleResponse` bên dưới cho khớp.
 * ============================================================
 */
import axios from 'axios';
import { useAppStore } from '@/stores/app';
import { useAuthStore } from '@/stores/auth';
import router from '@/router';
import { mockAdapter } from '@/mocks/mockServer';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  timeout: Number(import.meta.env.VITE_API_TIMEOUT) || 15000,
  headers: { 'Content-Type': 'application/json' },
});

if (import.meta.env.VITE_USE_MOCK === 'true') {
  api.defaults.adapter = mockAdapter;
}

/** Thông báo mặc định theo mã HTTP */
const HTTP_MESSAGES = {
  400: 'Dữ liệu gửi lên không hợp lệ',
  401: 'Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại',
  403: 'Bạn không có quyền thực hiện thao tác này',
  404: 'Không tìm thấy dữ liệu',
  409: 'Dữ liệu bị trùng',
  500: 'Lỗi hệ thống, vui lòng thử lại sau',
};

/**
 * Chạy TRƯỚC mỗi request: gắn token, bật loading.
 * @param {import('axios').InternalAxiosRequestConfig} config
 * @returns {import('axios').InternalAxiosRequestConfig}
 */
const handleRequest = (config) => {
  const auth = useAuthStore();
  if (auth.token) config.headers.Authorization = `Bearer ${auth.token}`;
  if (!config.silent) useAppStore().showLoading();
  return config;
};

/**
 * Chạy khi request THÀNH CÔNG (status 2xx): tắt loading, trả về phần data.
 * @param {import('axios').AxiosResponse} response
 * @returns {any} response.data
 */
const handleResponse = (response) => {
  if (!response.config.silent) useAppStore().hideLoading();
  return response.data;
};

/**
 * Chạy khi request LỖI: tắt loading, hiện toast, xử lý 401.
 * Luôn reject lại lỗi để nơi gọi có thể try/catch nếu cần xử lý riêng.
 * @param {import('axios').AxiosError} error
 * @returns {Promise<never>}
 */
const handleError = (error) => {
  const app = useAppStore();
  const config = error.config || {};
  if (!config.silent) app.hideLoading();

  const status = error.response?.status;
  // Ưu tiên thông báo backend trả về, sau đó tới thông báo mặc định theo mã lỗi
  const message =
    error.response?.data?.message ||
    HTTP_MESSAGES[status] ||
    (error.code === 'ECONNABORTED' ? 'Hết thời gian chờ phản hồi' : 'Không kết nối được máy chủ');

  if (status === 401) {
    useAuthStore().logout();
    const current = router.currentRoute.value;
    if (current.name !== 'login') router.push({ name: 'login', query: { redirect: current.fullPath } });
  }

  if (!config.skipToast) app.toastError(message);

  // Gắn message đã chuẩn hoá để nơi gọi dùng lại: catch (e) { e.displayMessage }
  error.displayMessage = message;
  return Promise.reject(error);
};

api.interceptors.request.use(handleRequest);
api.interceptors.response.use(handleResponse, handleError);

export default api;

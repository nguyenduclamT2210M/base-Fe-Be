/**
 * ============================================================
 * FILE: src/apis/components/auth/AuthAPI.js
 * TÁC DỤNG:
 *   API xác thực. Không kế thừa BaseAPI vì đăng nhập không phải CRUD.
 * CÁCH DÙNG:
 *   Không gọi trực tiếp ở view. Hãy dùng qua store: useAuthStore().login(...)
 * ============================================================
 */
import api from '@/apis/config/APIConfig';

const AuthAPI = {
  /**
   * Đăng nhập.
   * skipToast: màn hình Login tự hiển thị lỗi dưới form, không cần toast.
   * @param {{username:string, password:string}} credentials
   * @returns {Promise<{token:string, user:Object}>}
   */
  login(credentials) {
    return api.post('auth/login', credentials, { skipToast: true });
  },
};

export default AuthAPI;

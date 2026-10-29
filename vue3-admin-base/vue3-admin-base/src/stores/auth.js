/**
 * ============================================================
 * FILE: src/stores/auth.js
 * TÁC DỤNG:
 *   Store quản lý ĐĂNG NHẬP: token, thông tin user, login/logout.
 *   Token được lưu localStorage để F5 trang vẫn còn đăng nhập.
 * CÁCH DÙNG:
 *   import { useAuthStore } from '@/stores/auth';
 *   const auth = useAuthStore();
 *   await auth.login({ username, password });  // ném lỗi nếu sai
 *   auth.isLoggedIn   // true/false
 *   auth.user?.FullName
 *   auth.logout();
 * NƠI ĐANG DÙNG:
 *   - router/index.js  : chặn trang cần đăng nhập
 *   - APIConfig.js     : gắn token vào header, tự logout khi 401
 *   - MainLayout.vue   : hiển thị tên user, nút đăng xuất
 * ============================================================
 */
import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import storage from '@/utils/storage';
import { STORAGE_KEYS } from '@/utils/constants';
import AuthAPI from '@/apis/components/auth/AuthAPI';

export const useAuthStore = defineStore('auth', () => {
  //#region State
  const token = ref(storage.get(STORAGE_KEYS.TOKEN, ''));
  const user = ref(storage.get(STORAGE_KEYS.USER, null));
  //#endregion

  //#region Getters
  /** Đã đăng nhập hay chưa (dựa vào việc có token) */
  const isLoggedIn = computed(() => !!token.value);
  //#endregion

  //#region Actions
  /**
   * Đăng nhập: gọi API, lưu token + user vào store và localStorage.
   * @param {{username:string, password:string}} credentials
   * @returns {Promise<Object>} thông tin user
   * @throws lỗi từ API (sai tài khoản/mật khẩu...) để màn hình Login tự xử lý
   */
  const login = async (credentials) => {
    const res = await AuthAPI.login(credentials);
    token.value = res.token;
    user.value = res.user;
    storage.set(STORAGE_KEYS.TOKEN, res.token);
    storage.set(STORAGE_KEYS.USER, res.user);
    return res.user;
  };

  /**
   * Đăng xuất: xoá token + user ở store và localStorage.
   * Việc chuyển trang về /login do nơi gọi quyết định (router hoặc layout).
   */
  const logout = () => {
    token.value = '';
    user.value = null;
    storage.remove(STORAGE_KEYS.TOKEN);
    storage.remove(STORAGE_KEYS.USER);
  };
  //#endregion

  return { token, user, isLoggedIn, login, logout };
});

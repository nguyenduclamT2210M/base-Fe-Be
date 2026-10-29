/**
 * ============================================================
 * FILE: src/stores/app.js
 * TÁC DỤNG:
 *   Store (Pinia) chứa trạng thái GIAO DIỆN dùng chung toàn app:
 *   - Loading toàn màn hình (đếm số request đang chạy)
 *   - Danh sách toast thông báo
 *   - Trạng thái thu gọn sidebar (lưu localStorage để F5 không mất)
 * CÁCH DÙNG:
 *   import { useAppStore } from '@/stores/app';
 *   const app = useAppStore();
 *   app.toastSuccess('Lưu thành công');
 *   app.toastError('Có lỗi xảy ra');
 *   app.toggleSidebar();
 *   Thông thường KHÔNG cần gọi showLoading/hideLoading thủ công:
 *   APIConfig.js đã tự gọi cho mọi request.
 * VÌ SAO DÙNG STORE:
 *   Toast/loading được kích hoạt ở nhiều nơi (API, form, trang...) nhưng chỉ hiển thị
 *   một chỗ (App.vue). Store là "điểm trung gian" thay vì truyền props/emit qua nhiều tầng.
 * ============================================================
 */
import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import storage from '@/utils/storage';
import { STORAGE_KEYS, TOAST_TYPE } from '@/utils/constants';
import { uid } from '@/utils/helpers';

export const useAppStore = defineStore('app', () => {
  //#region State
  /** Số request đang chạy. >0 thì hiện loading. Dùng bộ đếm vì có thể nhiều request song song. */
  const loadingCount = ref(0);
  /** Danh sách toast đang hiển thị: [{ id, type, message }] */
  const toasts = ref([]);
  /** Sidebar có đang thu gọn không */
  const sidebarCollapsed = ref(storage.get(STORAGE_KEYS.SIDEBAR_COLLAPSED, false));
  //#endregion

  //#region Getters
  /** true khi có ít nhất 1 request đang chạy */
  const isLoading = computed(() => loadingCount.value > 0);
  //#endregion

  //#region Actions
  /** Tăng bộ đếm loading (gọi khi bắt đầu request). */
  const showLoading = () => {
    loadingCount.value++;
  };

  /** Giảm bộ đếm loading (gọi khi request kết thúc, kể cả lỗi). Không để âm. */
  const hideLoading = () => {
    loadingCount.value = Math.max(0, loadingCount.value - 1);
  };

  /**
   * Hiện một toast, tự ẩn sau `duration` ms.
   * @param {string} type - một giá trị trong TOAST_TYPE
   * @param {string} message - nội dung
   * @param {number} [duration=3000] - thời gian hiển thị (ms), 0 = không tự ẩn
   * @returns {string} id toast (dùng để đóng thủ công)
   */
  const showToast = (type, message, duration = 3000) => {
    const id = uid();
    toasts.value.push({ id, type, message });
    if (duration > 0) setTimeout(() => removeToast(id), duration);
    return id;
  };

  /**
   * Đóng toast theo id.
   * @param {string} id
   */
  const removeToast = (id) => {
    toasts.value = toasts.value.filter((t) => t.id !== id);
  };

  /** Lối tắt cho từng loại toast. @param {string} message */
  const toastSuccess = (message) => showToast(TOAST_TYPE.SUCCESS, message);
  const toastError = (message) => showToast(TOAST_TYPE.ERROR, message, 5000);
  const toastWarning = (message) => showToast(TOAST_TYPE.WARNING, message);
  const toastInfo = (message) => showToast(TOAST_TYPE.INFO, message);

  /** Đóng/mở sidebar và ghi nhớ lựa chọn. */
  const toggleSidebar = () => {
    sidebarCollapsed.value = !sidebarCollapsed.value;
    storage.set(STORAGE_KEYS.SIDEBAR_COLLAPSED, sidebarCollapsed.value);
  };
  //#endregion

  return {
    loadingCount,
    toasts,
    sidebarCollapsed,
    isLoading,
    showLoading,
    hideLoading,
    showToast,
    removeToast,
    toastSuccess,
    toastError,
    toastWarning,
    toastInfo,
    toggleSidebar,
  };
});

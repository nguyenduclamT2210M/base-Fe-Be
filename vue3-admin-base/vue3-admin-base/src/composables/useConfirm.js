/**
 * ============================================================
 * FILE: src/composables/useConfirm.js
 * TÁC DỤNG:
 *   Hộp thoại xác nhận dạng Promise — thay cho window.confirm (xấu, không tuỳ biến được).
 *   State dùng chung toàn app (khai báo ngoài hàm) → chỉ cần 1 <MsConfirm /> trong App.vue.
 * CÁCH DÙNG:
 *   import { useConfirm } from '@/composables/useConfirm';
 *   const { confirm } = useConfirm();
 *   if (await confirm('Bạn có chắc muốn xoá?')) { ...xoá... }
 *   // tuỳ biến:
 *   await confirm('Xoá 5 bản ghi?', { title: 'Xoá nhiều', okText: 'Xoá', type: 'danger' });
 * ============================================================
 */
import { reactive, readonly } from 'vue';

/** State dùng chung cho hộp thoại (singleton) */
const state = reactive({
  open: false,
  title: 'Xác nhận',
  message: '',
  okText: 'Đồng ý',
  cancelText: 'Huỷ',
  type: 'primary', // kiểu nút OK: primary | danger
});

/** Hàm resolve của Promise đang chờ người dùng bấm */
let resolver = null;

/**
 * Mở hộp thoại và chờ người dùng chọn.
 * @param {string} message - nội dung câu hỏi
 * @param {{title?:string, okText?:string, cancelText?:string, type?:'primary'|'danger'}} [options]
 * @returns {Promise<boolean>} true = Đồng ý, false = Huỷ/đóng
 */
const confirm = (message, options = {}) => {
  resolver?.(false); // nếu đang có hộp thoại cũ thì coi như Huỷ
  Object.assign(state, {
    title: 'Xác nhận',
    okText: 'Đồng ý',
    cancelText: 'Huỷ',
    type: 'primary',
    ...options,
    message,
    open: true,
  });
  return new Promise((resolve) => {
    resolver = resolve;
  });
};

/**
 * Đóng hộp thoại và trả kết quả (MsConfirm gọi hàm này khi bấm nút).
 * @param {boolean} result
 */
const answer = (result) => {
  state.open = false;
  resolver?.(result);
  resolver = null;
};

/**
 * @returns {{state: Readonly<typeof state>, confirm: typeof confirm, answer: typeof answer}}
 */
export const useConfirm = () => ({ state: readonly(state), confirm, answer });

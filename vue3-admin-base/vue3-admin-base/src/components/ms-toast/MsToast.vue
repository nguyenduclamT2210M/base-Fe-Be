<!--
============================================================
COMPONENT: MsToast
TÁC DỤNG:
  Hiển thị danh sách thông báo nổi (toast) góc trên bên phải.
  Đọc dữ liệu từ store app → mọi nơi trong app gọi toast được.
CÁCH DÙNG:
  - Đặt DUY NHẤT 1 lần trong App.vue:  <ms-toast />
  - Ở nơi khác chỉ cần gọi store:  useAppStore().toastSuccess('Đã lưu')
============================================================
-->
<template>
  <Teleport to="body">
    <div class="ms-toast" role="status" aria-live="polite">
      <TransitionGroup name="ms-toast">
        <div v-for="t in app.toasts" :key="t.id" :class="['ms-toast__item', `is-${t.type}`]">
          <ms-icon :name="ICON_BY_TYPE[t.type]" :size="18" />
          <span class="ms-toast__message">{{ t.message }}</span>
          <button class="ms-toast__close" aria-label="Đóng thông báo" @click="app.removeToast(t.id)">
            <ms-icon name="close" :size="14" />
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<script setup>
import MsIcon from '@/components/ms-icon/MsIcon.vue';
import { useAppStore } from '@/stores/app';
import { TOAST_TYPE } from '@/utils/constants';

const app = useAppStore();

/** Icon tương ứng từng loại toast */
const ICON_BY_TYPE = {
  [TOAST_TYPE.SUCCESS]: 'check',
  [TOAST_TYPE.ERROR]: 'alert',
  [TOAST_TYPE.WARNING]: 'alert',
  [TOAST_TYPE.INFO]: 'info',
};
</script>

<style scoped>
.ms-toast {
  position: fixed;
  top: var(--space-4);
  right: var(--space-4);
  z-index: var(--z-toast);
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  width: min(360px, calc(100vw - 32px));
  pointer-events: none;
}
.ms-toast__item {
  display: flex;
  align-items: flex-start;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius);
  border-left: 4px solid;
  background: var(--color-surface);
  box-shadow: var(--shadow-lg);
  pointer-events: auto;
}
.ms-toast__message {
  flex: 1;
  line-height: 1.4;
}
.ms-toast__close {
  display: flex;
  padding: 2px;
  border: none;
  background: none;
  color: var(--color-text-muted);
  cursor: pointer;
}
.is-success {
  border-color: var(--color-success);
  color: var(--color-success);
}
.is-error {
  border-color: var(--color-danger);
  color: var(--color-danger);
}
.is-warning {
  border-color: var(--color-warning);
  color: var(--color-warning);
}
.is-info {
  border-color: var(--color-info);
  color: var(--color-info);
}
.ms-toast__message {
  color: var(--color-text);
}

.ms-toast-enter-active,
.ms-toast-leave-active {
  transition: all 0.25s ease;
}
.ms-toast-enter-from,
.ms-toast-leave-to {
  opacity: 0;
  transform: translateX(30px);
}
</style>

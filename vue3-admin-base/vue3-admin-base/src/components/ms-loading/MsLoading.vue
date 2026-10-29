<!--
============================================================
COMPONENT: MsLoading
TÁC DỤNG:
  Lớp phủ loading toàn màn hình, tự hiện khi có request API đang chạy
  (APIConfig tăng/giảm bộ đếm trong store app).
  Chỉ hiện sau 200ms → request nhanh không làm màn hình "nháy".
CÁCH DÙNG:
  - Đặt DUY NHẤT 1 lần trong App.vue:  <ms-loading />
  - Request không muốn che màn hình: truyền { silent: true } khi gọi API.
============================================================
-->
<template>
  <Teleport to="body">
    <div v-if="visible" class="ms-loading" aria-busy="true" aria-label="Đang xử lý">
      <span class="ms-loading__spinner" />
    </div>
  </Teleport>
</template>

<script setup>
import { ref, watch } from 'vue';
import { useAppStore } from '@/stores/app';

const SHOW_DELAY = 200; // ms
const app = useAppStore();
const visible = ref(false);
let timer = null;

/** Hiện sau SHOW_DELAY nếu vẫn đang loading; tắt ngay khi hết loading. */
watch(
  () => app.isLoading,
  (loading) => {
    clearTimeout(timer);
    if (loading) timer = setTimeout(() => (visible.value = true), SHOW_DELAY);
    else visible.value = false;
  },
);
</script>

<style scoped>
.ms-loading {
  position: fixed;
  inset: 0;
  z-index: var(--z-loading);
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.5);
  cursor: progress;
}
.ms-loading__spinner {
  width: 40px;
  height: 40px;
  border: 3px solid var(--color-primary);
  border-right-color: transparent;
  border-radius: 50%;
  animation: ms-spin 0.7s linear infinite;
}
@keyframes ms-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>

<!--
============================================================
VIEW: NotFoundView  (route: mọi đường dẫn không khớp)
TÁC DỤNG: Trang 404 khi người dùng gõ sai URL hoặc vào link cũ.
CÁCH DÙNG: Router tự hiển thị nhờ route `/:pathMatch(.*)*` (đặt CUỐI danh sách route).
============================================================
-->
<template>
  <div class="not-found">
    <h1>404</h1>
    <p class="text-muted">Trang bạn tìm không tồn tại hoặc đã bị di chuyển.</p>
    <ms-button @click="router.push({ name: 'dashboard' })">Về trang chủ</ms-button>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router';
import MsButton from '@/components/ms-button/MsButton.vue';

const router = useRouter();
</script>

<style scoped>
.not-found {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--space-3);
  text-align: center;
}
.not-found h1 {
  font-size: 72px;
  color: var(--color-primary);
}
</style>

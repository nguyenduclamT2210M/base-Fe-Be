<!--
============================================================
COMPONENT GỐC: App.vue
TÁC DỤNG:
  Điểm gắn của toàn bộ giao diện:
  - <router-view />  : hiển thị trang theo URL (Login, hoặc MainLayout + trang con)
  - <ms-toast />     : thông báo nổi toàn cục
  - <ms-confirm />   : hộp thoại xác nhận toàn cục
  - <ms-loading />   : loading toàn màn hình khi gọi API
CÁCH DÙNG:
  Hiếm khi cần sửa. Không đặt logic nghiệp vụ ở đây.
  Mỗi component toàn cục chỉ được đặt 1 lần, tại file này.
============================================================
-->
<template>
  <router-view />
  <ms-toast />
  <ms-confirm />
  <ms-loading />
</template>

<script setup>
import MsConfirm from '@/components/ms-confirm/MsConfirm.vue';
import MsLoading from '@/components/ms-loading/MsLoading.vue';
import MsToast from '@/components/ms-toast/MsToast.vue';
</script>

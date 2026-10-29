<!--
============================================================
COMPONENT: MsIcon
TÁC DỤNG:
  Hiển thị icon dạng SVG nội tuyến — không cần thư viện icon ngoài (Font Awesome...),
  icon tự ăn màu chữ (currentColor) nên đổi màu bằng CSS `color`.
CÁCH DÙNG:
  <ms-icon name="plus" />
  <ms-icon name="trash" :size="18" />
  Tên có sẵn: xem object ICONS bên dưới.
THÊM ICON MỚI:
  Copy thuộc tính `d` của thẻ <path> từ bộ icon nét 24x24 (Lucide, Heroicons outline...)
  rồi thêm 1 dòng vào ICONS.
============================================================
-->
<template>
  <svg
    class="ms-icon"
    :width="size"
    :height="size"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="2"
    stroke-linecap="round"
    stroke-linejoin="round"
    aria-hidden="true"
  >
    <path :d="path" />
  </svg>
</template>

<script setup>
import { computed } from 'vue';

//#region Icons
/** Thư viện path SVG (nét 24x24) */
const ICONS = {
  plus: 'M12 5v14M5 12h14',
  edit: 'M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z',
  trash: 'M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6M10 11v6M14 11v6',
  search: 'M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16zM21 21l-4.3-4.3',
  close: 'M18 6 6 18M6 6l12 12',
  save: 'M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2zM17 21v-8H7v8M7 3v5h8',
  logout: 'M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9',
  users: 'M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM23 21v-2a4 4 0 0 0-3-3.9M16 3.1a4 4 0 0 1 0 7.8',
  dashboard: 'M3 3h7v9H3zM14 3h7v5h-7zM14 12h7v9h-7zM3 16h7v5H3z',
  menu: 'M3 12h18M3 6h18M3 18h18',
  'chevron-left': 'M15 18l-6-6 6-6',
  'chevron-right': 'M9 18l6-6-6-6',
  'arrow-up': 'M12 19V5M5 12l7-7 7 7',
  'arrow-down': 'M12 5v14M19 12l-7 7-7-7',
  refresh: 'M23 4v6h-6M1 20v-6h6M3.5 9a9 9 0 0 1 14.9-3.4L23 10M1 14l4.6 4.4A9 9 0 0 0 20.5 15',
  check: 'M20 6 9 17l-5-5',
  alert: 'M12 9v4M12 17h.01M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z',
  info: 'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20zM12 16v-4M12 8h.01',
};
//#endregion

//#region Props
const props = defineProps({
  /** Tên icon (key trong ICONS) */
  name: { type: String, required: true },
  /** Kích thước (px) */
  size: { type: [Number, String], default: 16 },
});
//#endregion

/** Path SVG ứng với tên icon. Tên sai → không vẽ gì và cảnh báo khi dev. */
const path = computed(() => {
  const d = ICONS[props.name];
  if (!d && import.meta.env.DEV) console.warn(`[MsIcon] Không có icon "${props.name}"`);
  return d || '';
});
</script>

<style scoped>
.ms-icon {
  display: inline-block;
  flex-shrink: 0;
  vertical-align: middle;
}
</style>

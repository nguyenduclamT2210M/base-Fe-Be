<!--
============================================================
COMPONENT: MsPaging
TÁC DỤNG:
  Thanh phân trang: tổng số bản ghi, chọn số dòng/trang, các nút số trang
  (rút gọn bằng "..." khi nhiều trang), nút trước/sau.
CÁCH DÙNG:
  <ms-paging
    :total="total" :page-index="query.pageIndex" :page-size="query.pageSize"
    @change="changePage"
  />
  Sự kiện `change` trả về { pageIndex, pageSize } — khớp hàm changePage của useCrud.
PROPS:
  total, pageIndex (bắt đầu từ 1), pageSize, pageSizes, maxButtons (số nút trang tối đa)
============================================================
-->
<template>
  <div v-if="total > 0" class="ms-paging">
    <div class="ms-paging__info">
      Hiển thị <b>{{ fromRecord }}–{{ toRecord }}</b> / <b>{{ total }}</b> bản ghi
    </div>

    <div class="ms-paging__right">
      <label class="ms-paging__size">
        Số dòng
        <select :value="pageSize" @change="emit('change', { pageSize: +$event.target.value })">
          <option v-for="size in pageSizes" :key="size" :value="size">{{ size }}</option>
        </select>
      </label>

      <nav class="ms-paging__pages" aria-label="Phân trang">
        <button :disabled="pageIndex <= 1" aria-label="Trang trước" @click="goTo(pageIndex - 1)">
          <ms-icon name="chevron-left" :size="14" />
        </button>
        <template v-for="(item, i) in pageItems" :key="i">
          <span v-if="item === '...'" class="ms-paging__dots">…</span>
          <button
            v-else
            :class="{ 'is-active': item === pageIndex }"
            :aria-current="item === pageIndex ? 'page' : undefined"
            @click="goTo(item)"
          >
            {{ item }}
          </button>
        </template>
        <button :disabled="pageIndex >= totalPages" aria-label="Trang sau" @click="goTo(pageIndex + 1)">
          <ms-icon name="chevron-right" :size="14" />
        </button>
      </nav>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import MsIcon from '@/components/ms-icon/MsIcon.vue';
import { PAGE_SIZES } from '@/utils/constants';

//#region Props & Emits
const props = defineProps({
  total: { type: Number, default: 0 },
  pageIndex: { type: Number, default: 1 },
  pageSize: { type: Number, default: 10 },
  pageSizes: { type: Array, default: () => PAGE_SIZES },
  maxButtons: { type: Number, default: 7 },
});

const emit = defineEmits(['change']);
//#endregion

/** Tổng số trang (tối thiểu 1) */
const totalPages = computed(() => Math.max(1, Math.ceil(props.total / props.pageSize)));
/** Số thứ tự bản ghi đầu/cuối của trang hiện tại */
const fromRecord = computed(() => (props.pageIndex - 1) * props.pageSize + 1);
const toRecord = computed(() => Math.min(props.pageIndex * props.pageSize, props.total));

/**
 * Danh sách nút hiển thị, vd trang 6/20 → [1, '...', 5, 6, 7, '...', 20].
 * Luôn có trang đầu, trang cuối và các trang quanh trang hiện tại.
 */
const pageItems = computed(() => {
  const total = totalPages.value;
  const current = props.pageIndex;
  if (total <= props.maxButtons) return Array.from({ length: total }, (_, i) => i + 1);

  const side = Math.floor((props.maxButtons - 3) / 2); // số trang mỗi bên trang hiện tại
  let start = Math.max(2, current - side);
  let end = Math.min(total - 1, current + side);
  if (current - side <= 2) end = props.maxButtons - 2;
  if (current + side >= total - 1) start = total - (props.maxButtons - 3);

  const items = [1];
  if (start > 2) items.push('...');
  for (let p = start; p <= end; p++) items.push(p);
  if (end < total - 1) items.push('...');
  items.push(total);
  return items;
});

/**
 * Chuyển tới trang `page` (bỏ qua nếu ngoài phạm vi hoặc đang ở trang đó).
 * @param {number} page
 */
const goTo = (page) => {
  if (page < 1 || page > totalPages.value || page === props.pageIndex) return;
  emit('change', { pageIndex: page });
};
</script>

<style scoped>
.ms-paging {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  margin-top: var(--space-4);
  flex-wrap: wrap;
  font-size: var(--font-size-sm);
}
.ms-paging__info {
  color: var(--color-text-muted);
}
.ms-paging__right {
  display: flex;
  align-items: center;
  gap: var(--space-4);
  flex-wrap: wrap;
}
.ms-paging__size {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  color: var(--color-text-muted);
}
.ms-paging__size select {
  height: 30px;
  padding: 0 var(--space-2);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius-sm);
  background: var(--color-surface);
}
.ms-paging__pages {
  display: flex;
  align-items: center;
  gap: var(--space-1);
}
.ms-paging__pages button {
  min-width: 30px;
  height: 30px;
  padding: 0 var(--space-2);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  background: var(--color-surface);
  cursor: pointer;
}
.ms-paging__pages button:hover:not(:disabled) {
  border-color: var(--color-primary);
  color: var(--color-primary);
}
.ms-paging__pages button:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
.ms-paging__pages button.is-active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: #fff;
}
.ms-paging__dots {
  padding: 0 var(--space-1);
  color: var(--color-text-muted);
}
</style>

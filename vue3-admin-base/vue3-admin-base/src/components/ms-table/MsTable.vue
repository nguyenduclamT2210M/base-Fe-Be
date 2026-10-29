<!--
============================================================
COMPONENT: MsTable
TÁC DỤNG:
  Bảng dữ liệu cấu hình bằng mảng `columns`:
  - Tự định dạng ô theo kiểu (text/number/currency/date/datetime)
  - Bấm tiêu đề để sắp xếp (cột có sortable: true)
  - Chọn nhiều dòng (checkbox + chọn tất cả) qua v-model:selected
  - Tuỳ biến ô bất kỳ bằng slot `cell-<key>`, cột thao tác bằng slot `actions`
  - Trạng thái đang tải và không có dữ liệu
  - Bảng KHÔNG tự gọi API: dữ liệu và sắp xếp do trang cha (useCrud) xử lý.
CÁCH DÙNG:
  <ms-table
    :columns="columns" :rows="rows" :loading="loading"
    selectable v-model:selected="selectedIds"
    :sort-by="query.sortBy" :sort-order="query.sortOrder" @sort="changeSort"
    @row-dblclick="openEdit"
  >
    <template #cell-FullName="{ row, value }"><b>{{ value }}</b></template>
    <template #actions="{ row }">
      <ms-button type="text" icon="edit" @click="openEdit(row)" />
    </template>
  </ms-table>
  columns: [{ key, label, type?, width?, align?: 'left'|'center'|'right', sortable?, formatter?(value,row) }]
PROPS:
  columns, rows, rowKey (mặc định 'Id'), loading, selectable, selected (v-model),
  sortBy, sortOrder, emptyText, actionsWidth
EVENTS:
  update:selected(ids), sort({ sortBy, sortOrder }), row-click(row), row-dblclick(row)
============================================================
-->
<template>
  <div class="ms-table">
    <div class="ms-table__scroll">
      <table>
        <thead>
          <tr>
            <th v-if="selectable" class="ms-table__check">
              <ms-checkbox
                :model-value="isAllSelected"
                :indeterminate="isSomeSelected"
                aria-label="Chọn tất cả"
                @update:model-value="toggleAll"
              />
            </th>
            <th
              v-for="col in columns"
              :key="col.key"
              :style="{ width: col.width, textAlign: getAlign(col) }"
              :class="{ 'is-sortable': col.sortable }"
              :aria-sort="ariaSort(col)"
              @click="col.sortable && handleSort(col.key)"
            >
              <span class="ms-table__th">
                {{ col.label }}
                <ms-icon
                  v-if="col.sortable && sortBy === col.key"
                  :name="sortOrder === 'desc' ? 'arrow-down' : 'arrow-up'"
                  :size="13"
                />
              </span>
            </th>
            <th v-if="$slots.actions" :style="{ width: actionsWidth }" class="ms-table__actions">
              Thao tác
            </th>
          </tr>
        </thead>

        <tbody>
          <tr
            v-for="row in rows"
            :key="row[rowKey]"
            :class="{ 'is-selected': selectedSet.has(row[rowKey]) }"
            @click="emit('row-click', row)"
            @dblclick="emit('row-dblclick', row)"
          >
            <td v-if="selectable" class="ms-table__check" @click.stop>
              <ms-checkbox
                :model-value="selectedSet.has(row[rowKey])"
                aria-label="Chọn dòng"
                @update:model-value="toggleRow(row[rowKey])"
              />
            </td>
            <td v-for="col in columns" :key="col.key" :style="{ textAlign: getAlign(col) }">
              <slot :name="`cell-${col.key}`" :row="row" :value="row[col.key]" :column="col">
                {{ formatCell(row, col) }}
              </slot>
            </td>
            <td v-if="$slots.actions" class="ms-table__actions" @click.stop @dblclick.stop>
              <slot name="actions" :row="row" />
            </td>
          </tr>
        </tbody>
      </table>

      <div v-if="!loading && !rows.length" class="ms-table__empty">
        <slot name="empty">{{ emptyText }}</slot>
      </div>
    </div>

    <div v-if="loading" class="ms-table__loading">
      <span class="ms-table__spinner" /> Đang tải...
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import MsCheckbox from '@/components/ms-checkbox/MsCheckbox.vue';
import MsIcon from '@/components/ms-icon/MsIcon.vue';
import { formatByType } from '@/utils/formatter';

//#region Props & Emits
const props = defineProps({
  columns: {
    type: Array,
    required: true,
    validator: (cols) => cols.every((c) => c.key && c.label),
  },
  rows: { type: Array, default: () => [] },
  rowKey: { type: String, default: 'Id' },
  loading: { type: Boolean, default: false },
  selectable: { type: Boolean, default: false },
  selected: { type: Array, default: () => [] },
  sortBy: { type: String, default: '' },
  sortOrder: { type: String, default: 'asc' },
  emptyText: { type: String, default: 'Không có dữ liệu' },
  actionsWidth: { type: String, default: '100px' },
});

const emit = defineEmits(['update:selected', 'sort', 'row-click', 'row-dblclick']);
//#endregion

//#region Selection
/** Set id đang chọn — tra cứu O(1) thay vì includes() trên mảng mỗi dòng */
const selectedSet = computed(() => new Set(props.selected));

/** Tất cả dòng của trang hiện tại đều đã chọn */
const isAllSelected = computed(
  () => props.rows.length > 0 && props.rows.every((r) => selectedSet.value.has(r[props.rowKey])),
);

/** Có chọn một phần (dùng cho trạng thái indeterminate của ô chọn tất cả) */
const isSomeSelected = computed(
  () => !isAllSelected.value && props.rows.some((r) => selectedSet.value.has(r[props.rowKey])),
);

/**
 * Bật/tắt chọn một dòng.
 * @param {string|number} id
 */
const toggleRow = (id) => {
  const next = new Set(props.selected);
  next.has(id) ? next.delete(id) : next.add(id);
  emit('update:selected', [...next]);
};

/**
 * Chọn/bỏ chọn tất cả dòng của TRANG HIỆN TẠI (giữ nguyên lựa chọn ở trang khác).
 * @param {boolean} checked
 */
const toggleAll = (checked) => {
  const next = new Set(props.selected);
  props.rows.forEach((r) => (checked ? next.add(r[props.rowKey]) : next.delete(r[props.rowKey])));
  emit('update:selected', [...next]);
};
//#endregion

//#region Sort & format
/**
 * Bấm tiêu đề cột: cột mới → tăng dần; bấm lại cột đang sắp → đổi chiều.
 * @param {string} key
 */
const handleSort = (key) => {
  const sortOrder = props.sortBy === key && props.sortOrder === 'asc' ? 'desc' : 'asc';
  emit('sort', { sortBy: key, sortOrder });
};

/**
 * Giá trị aria-sort cho trình đọc màn hình.
 * @param {Object} col
 * @returns {string|undefined}
 */
const ariaSort = (col) => {
  if (!col.sortable) return undefined;
  if (props.sortBy !== col.key) return 'none';
  return props.sortOrder === 'desc' ? 'descending' : 'ascending';
};

/**
 * Căn lề mặc định: số/tiền căn phải (dễ so sánh), còn lại căn trái.
 * @param {Object} col
 * @returns {string}
 */
const getAlign = (col) =>
  col.align || (['number', 'currency'].includes(col.type) ? 'right' : 'left');

/**
 * Nội dung hiển thị của ô: ưu tiên formatter riêng của cột, sau đó theo type.
 * @param {Object} row
 * @param {Object} col
 * @returns {string}
 */
const formatCell = (row, col) =>
  col.formatter ? col.formatter(row[col.key], row) : formatByType(row[col.key], col.type);
//#endregion
</script>

<style scoped>
.ms-table {
  position: relative;
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  background: var(--color-surface);
}
.ms-table__scroll {
  overflow-x: auto;
}
table {
  width: 100%;
  border-collapse: collapse;
}
th,
td {
  padding: 10px var(--space-3);
  border-bottom: 1px solid var(--color-border);
  white-space: nowrap;
}
th {
  position: sticky;
  top: 0;
  background: #f9fafb;
  font-weight: 600;
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  user-select: none;
}
th.is-sortable {
  cursor: pointer;
}
th.is-sortable:hover {
  color: var(--color-text);
}
.ms-table__th {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
}
tbody tr {
  transition: background var(--transition);
}
tbody tr:hover {
  background: var(--color-hover);
}
tbody tr.is-selected {
  background: var(--color-primary-soft);
}
tbody tr:last-child td {
  border-bottom: none;
}
.ms-table__check {
  width: 44px;
  text-align: center;
}
.ms-table__actions {
  text-align: center;
}
.ms-table__empty {
  padding: 40px;
  text-align: center;
  color: var(--color-text-muted);
}
.ms-table__loading {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  background: rgba(255, 255, 255, 0.7);
  color: var(--color-text-muted);
  min-height: 80px;
}
.ms-table__spinner {
  width: 18px;
  height: 18px;
  border: 2px solid var(--color-primary);
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

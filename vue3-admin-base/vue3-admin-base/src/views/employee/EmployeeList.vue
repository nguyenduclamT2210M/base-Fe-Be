<!--
============================================================
VIEW: EmployeeList  (route: /employees)
TÁC DỤNG:
  Màn hình quản lý Nhân viên — MẪU CHUẨN cho mọi màn danh sách CRUD:
  tìm kiếm, lọc phòng ban, sắp xếp, phân trang, chọn nhiều + xoá nhiều,
  thêm/sửa qua modal (bấm đúp dòng để sửa), xoá có xác nhận.
CÁCH DÙNG (làm màn mới, vd Sản phẩm):
  1. Copy file này + EmployeeForm.vue sang thư mục views/product/
  2. Đổi API (ProductAPI), `columns`, các slot cell-*, và form
  3. Toàn bộ logic tải/lưu/xoá đã nằm trong useCrud → không phải viết lại
CẤU TRÚC:
  page-header (tiêu đề + nút Thêm) → toolbar (tìm kiếm, lọc, xoá nhiều)
  → MsTable → MsPaging → EmployeeForm (modal)
============================================================
-->
<template>
  <div class="page">
    <div class="page-header">
      <h1>Danh sách nhân viên</h1>
      <ms-button icon="plus" @click="openCreate">Thêm nhân viên</ms-button>
    </div>

    <div class="toolbar">
      <ms-input
        v-model="crud.query.keyword"
        icon="search"
        placeholder="Tìm theo mã, tên, email..."
        clearable
        class="toolbar__search"
        @update:model-value="crud.search"
      />
      <ms-select
        :model-value="crud.query.filters.Department"
        :options="DEPARTMENT_OPTIONS"
        placeholder="Tất cả phòng ban"
        class="toolbar__filter"
        @change="(v) => crud.changeFilters({ Department: v })"
      />
      <div class="toolbar__spacer" />
      <ms-button
        v-if="crud.selectedIds.value.length"
        type="danger"
        icon="trash"
        @click="crud.removeSelected"
      >
        Xoá {{ crud.selectedIds.value.length }} mục
      </ms-button>
      <ms-button type="secondary" icon="refresh" title="Tải lại" @click="crud.loadData" />
    </div>

    <ms-table
      v-model:selected="crud.selectedIds.value"
      :columns="columns"
      :rows="crud.rows.value"
      :loading="crud.loading.value"
      :sort-by="crud.query.sortBy"
      :sort-order="crud.query.sortOrder"
      selectable
      @sort="crud.changeSort"
      @row-dblclick="openEdit"
    >
      <template #cell-FullName="{ row, value }">
        <div class="cell-name">
          <strong>{{ value }}</strong>
          <span class="text-muted">{{ row.Email }}</span>
        </div>
      </template>

      <template #cell-IsActive="{ value }">
        <span :class="['badge', value ? 'badge--success' : 'badge--muted']">
          {{ value ? 'Đang làm' : 'Đã nghỉ' }}
        </span>
      </template>

      <template #actions="{ row }">
        <ms-button type="text" size="sm" icon="edit" title="Sửa" @click="openEdit(row)" />
        <ms-button type="text" size="sm" icon="trash" title="Xoá" @click="crud.remove(row)" />
      </template>
    </ms-table>

    <ms-paging
      :total="crud.total.value"
      :page-index="crud.query.pageIndex"
      :page-size="crud.query.pageSize"
      @change="crud.changePage"
    />

    <employee-form v-model="isFormOpen" :employee="editingEmployee" :on-save="crud.save" />
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue';
import MsButton from '@/components/ms-button/MsButton.vue';
import MsInput from '@/components/ms-input/MsInput.vue';
import MsPaging from '@/components/ms-paging/MsPaging.vue';
import MsSelect from '@/components/ms-select/MsSelect.vue';
import MsTable from '@/components/ms-table/MsTable.vue';
import EmployeeForm from './EmployeeForm.vue';
import EmployeeAPI from '@/apis/components/employee/EmployeeAPI';
import { useCrud } from '@/composables/useCrud';
import { DEPARTMENT_OPTIONS, GENDER_OPTIONS, getOptionLabel } from '@/utils/constants';

//#region Columns
/** Cấu hình cột bảng. Cột có `formatter` tự đổi value → label hiển thị. */
const columns = [
  { key: 'Code', label: 'Mã NV', width: '90px', sortable: true },
  { key: 'FullName', label: 'Họ và tên', sortable: true },
  { key: 'Gender', label: 'Giới tính', width: '90px', formatter: (v) => getOptionLabel(GENDER_OPTIONS, v) },
  { key: 'BirthDate', label: 'Ngày sinh', type: 'date', width: '110px', sortable: true },
  { key: 'Phone', label: 'Điện thoại', width: '120px' },
  {
    key: 'Department',
    label: 'Phòng ban',
    width: '120px',
    formatter: (v) => getOptionLabel(DEPARTMENT_OPTIONS, v),
  },
  { key: 'Salary', label: 'Lương', type: 'currency', width: '140px', sortable: true },
  { key: 'IsActive', label: 'Trạng thái', width: '110px', align: 'center' },
];
//#endregion

//#region CRUD
/**
 * Toàn bộ state + hàm xử lý danh sách. Gọi qua `crud.xxx` để rõ nguồn gốc.
 * (ref trong object thường không tự unwrap ở template → dùng `.value`)
 */
const crud = useCrud(EmployeeAPI, {
  defaultSort: { sortBy: 'Code', sortOrder: 'asc' },
  defaultFilters: { Department: '' },
  entityName: 'nhân viên',
  getDisplayName: (row) => row.FullName,
});

onMounted(crud.loadData);
//#endregion

//#region Form
const isFormOpen = ref(false);
/** Bản ghi đang sửa; null = thêm mới */
const editingEmployee = ref(null);

/** Mở form thêm mới. */
const openCreate = () => {
  editingEmployee.value = null;
  isFormOpen.value = true;
};

/**
 * Mở form sửa với dữ liệu của dòng.
 * @param {Object} row
 */
const openEdit = (row) => {
  editingEmployee.value = row;
  isFormOpen.value = true;
};
//#endregion
</script>

<style scoped>
.toolbar__search {
  width: 300px;
  max-width: 100%;
}
.toolbar__filter {
  width: 190px;
}
.cell-name {
  display: flex;
  flex-direction: column;
  line-height: 1.3;
}
.cell-name .text-muted {
  font-size: 12px;
}
.badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 500;
}
.badge--success {
  background: var(--color-success-soft);
  color: var(--color-success);
}
.badge--muted {
  background: var(--color-hover);
  color: var(--color-text-muted);
}
</style>

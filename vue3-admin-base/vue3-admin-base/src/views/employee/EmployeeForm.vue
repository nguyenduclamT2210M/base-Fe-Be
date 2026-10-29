<!--
============================================================
VIEW: EmployeeForm  (modal, dùng trong EmployeeList)
TÁC DỤNG:
  Form Thêm / Sửa nhân viên trong modal:
  - Làm việc trên BẢN SAO dữ liệu (EmployeeModel) → bấm Huỷ không ảnh hưởng bảng
  - Validate khi bấm Lưu, sau đó validate lại từng ô khi người dùng sửa
  - Kiểm tra trùng mã nhân viên qua API trước khi lưu
  - Nút Lưu có loading, chặn bấm 2 lần
  - Lưu xong mới đóng; lỗi thì giữ form để người dùng sửa tiếp
  - Hỏi xác nhận khi đóng form có thay đổi chưa lưu
CÁCH DÙNG:
  <employee-form v-model="isOpen" :employee="row | null" :on-save="crud.save" />
  `onSave(payload)` là hàm async do cha truyền vào (thường là useCrud().save).
  Dùng prop hàm thay cho emit vì form cần CHỜ kết quả lưu để quyết định đóng hay không.
============================================================
-->
<template>
  <ms-modal
    :model-value="modelValue"
    :title="form.isEdit ? 'Sửa thông tin nhân viên' : 'Thêm mới nhân viên'"
    width="720px"
    :close-on-esc="!saving"
    @update:model-value="requestClose"
    @opened="codeRef?.focus()"
  >
    <form class="employee-form" novalidate @submit.prevent="handleSave">
      <ms-input
        ref="codeRef"
        v-model="form.Code"
        label="Mã nhân viên"
        required
        maxlength="20"
        :error="errors.Code"
      />
      <ms-input v-model="form.FullName" label="Họ và tên" required maxlength="100" :error="errors.FullName" />
      <ms-select v-model="form.Gender" :options="GENDER_OPTIONS" label="Giới tính" />
      <ms-input v-model="form.BirthDate" type="date" label="Ngày sinh" :error="errors.BirthDate" />
      <ms-input v-model="form.Email" type="email" label="Email" :error="errors.Email" />
      <ms-input v-model="form.Phone" type="tel" label="Số điện thoại" :error="errors.Phone" />
      <ms-select
        v-model="form.Department"
        :options="DEPARTMENT_OPTIONS"
        label="Phòng ban"
        placeholder="-- Chọn phòng ban --"
        required
        :error="errors.Department"
      />
      <ms-input v-model="form.Salary" type="number" label="Lương (VNĐ)" :min="0" :step="100000" :error="errors.Salary" />
      <ms-checkbox v-model="form.IsActive" label="Đang làm việc" class="employee-form__full" />
      <!-- nút submit ẩn: nhấn Enter trong ô nhập sẽ lưu form -->
      <button type="submit" hidden />
    </form>

    <template #footer>
      <ms-button type="secondary" :disabled="saving" @click="requestClose(false)">Huỷ</ms-button>
      <ms-button icon="save" :loading="saving" @click="handleSave">Lưu</ms-button>
    </template>
  </ms-modal>
</template>

<script setup>
import { ref, watch } from 'vue';
import MsButton from '@/components/ms-button/MsButton.vue';
import MsCheckbox from '@/components/ms-checkbox/MsCheckbox.vue';
import MsInput from '@/components/ms-input/MsInput.vue';
import MsModal from '@/components/ms-modal/MsModal.vue';
import MsSelect from '@/components/ms-select/MsSelect.vue';
import EmployeeAPI from '@/apis/components/employee/EmployeeAPI';
import { EmployeeModel } from '@/models/EmployeeModel';
import { useConfirm } from '@/composables/useConfirm';
import { DEPARTMENT_OPTIONS, GENDER_OPTIONS } from '@/utils/constants';

//#region Props & Emits
const props = defineProps({
  /** Mở/đóng modal (v-model) */
  modelValue: { type: Boolean, default: false },
  /** Nhân viên cần sửa; null = thêm mới */
  employee: { type: Object, default: null },
  /** Hàm lưu async do cha truyền vào, nhận payload, ném lỗi nếu thất bại */
  onSave: { type: Function, required: true },
});

const emit = defineEmits(['update:modelValue']);
//#endregion

const { confirm } = useConfirm();

//#region State
const form = ref(EmployeeModel.empty());
const errors = ref({});
const saving = ref(false);
/** Đã bấm Lưu ít nhất 1 lần → từ đó validate ngay khi sửa từng ô */
const submitted = ref(false);
/** Ảnh chụp dữ liệu lúc mở form, để biết người dùng đã sửa gì chưa */
let snapshot = '';
const codeRef = ref(null);
//#endregion

/**
 * Mỗi lần mở form: tạo bản sao mới từ `employee` và xoá trạng thái cũ.
 */
watch(
  () => props.modelValue,
  (open) => {
    if (!open) return;
    form.value = new EmployeeModel(props.employee || {});
    errors.value = {};
    submitted.value = false;
    snapshot = JSON.stringify(form.value.toPayload());
  },
);

/** Sau lần bấm Lưu đầu tiên: người dùng sửa ô nào thì cập nhật lỗi ngay. */
watch(
  form,
  () => {
    if (submitted.value) errors.value = form.value.validate();
  },
  { deep: true },
);

/**
 * Kiểm tra trùng mã qua API.
 * @returns {Promise<boolean>} true nếu KHÔNG trùng (được phép lưu)
 */
const checkCodeAvailable = async () => {
  const { isDuplicate } = await EmployeeAPI.checkDuplicateCode(form.value.Code.trim(), form.value.Id);
  if (isDuplicate) errors.value = { ...errors.value, Code: `Mã ${form.value.Code} đã tồn tại` };
  return !isDuplicate;
};

/**
 * Lưu: validate → kiểm tra trùng mã → gọi onSave → đóng form.
 * Lỗi API được interceptor toast, form vẫn mở để người dùng sửa.
 */
const handleSave = async () => {
  if (saving.value) return;
  submitted.value = true;
  errors.value = form.value.validate();
  if (Object.keys(errors.value).length) return;

  saving.value = true;
  try {
    if (!(await checkCodeAvailable())) return;
    await props.onSave(form.value.toPayload());
    emit('update:modelValue', false);
  } catch {
    /* toast lỗi đã hiển thị, giữ nguyên form */
  } finally {
    saving.value = false;
  }
};

/**
 * Yêu cầu đóng form (nút Huỷ, X, Esc). Có thay đổi chưa lưu thì hỏi trước.
 * @param {boolean} open - giá trị mới từ MsModal (luôn false khi đóng)
 */
const requestClose = async (open) => {
  if (open || saving.value) return;
  const dirty = JSON.stringify(form.value.toPayload()) !== snapshot;
  if (dirty && !(await confirm('Dữ liệu chưa được lưu. Bạn có chắc muốn đóng?', { okText: 'Đóng' }))) {
    return;
  }
  emit('update:modelValue', false);
};
</script>

<style scoped>
.employee-form {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-4);
}
.employee-form__full {
  grid-column: 1 / -1;
}
@media (max-width: 640px) {
  .employee-form {
    grid-template-columns: 1fr;
  }
}
</style>

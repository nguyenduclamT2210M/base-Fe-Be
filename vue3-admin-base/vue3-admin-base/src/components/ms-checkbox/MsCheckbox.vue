<!--
============================================================
COMPONENT: MsCheckbox
TÁC DỤNG:
  Ô tích chọn có nhãn. Hỗ trợ trạng thái "indeterminate" (chọn một phần) —
  dùng cho ô "chọn tất cả" của bảng khi mới chọn vài dòng.
CÁCH DÙNG:
  <ms-checkbox v-model="form.IsActive" label="Đang làm việc" />
  <ms-checkbox :model-value="allChecked" :indeterminate="someChecked" @update:model-value="toggleAll" />
============================================================
-->
<template>
  <label :class="['ms-checkbox', { 'is-disabled': disabled }]">
    <input
      ref="inputRef"
      type="checkbox"
      class="ms-checkbox__input"
      :checked="modelValue"
      :disabled="disabled"
      :aria-label="ariaLabel || label || undefined"
      @change="emit('update:modelValue', $event.target.checked)"
    />
    <span v-if="label" class="ms-checkbox__label">{{ label }}</span>
  </label>
</template>

<script setup>
import { onMounted, ref, watch } from 'vue';

//#region Props & Emits
const props = defineProps({
  modelValue: { type: Boolean, default: false },
  label: { type: String, default: '' },
  /** Nhãn cho trình đọc màn hình khi không có label hiển thị */
  ariaLabel: { type: String, default: '' },
  indeterminate: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
});

const emit = defineEmits(['update:modelValue']);
//#endregion

const inputRef = ref(null);

/** `indeterminate` không phải attribute HTML, chỉ gán được qua JS → đồng bộ thủ công. */
const syncIndeterminate = () => {
  if (inputRef.value) inputRef.value.indeterminate = props.indeterminate;
};
onMounted(syncIndeterminate);
watch(() => props.indeterminate, syncIndeterminate);
</script>

<style scoped>
.ms-checkbox {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  cursor: pointer;
  user-select: none;
}
.ms-checkbox.is-disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
.ms-checkbox__input {
  width: 16px;
  height: 16px;
  margin: 0;
  accent-color: var(--color-primary);
  cursor: inherit;
}
</style>

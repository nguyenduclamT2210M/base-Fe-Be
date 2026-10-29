<!--
============================================================
COMPONENT: MsSelect
TÁC DỤNG:
  Dropdown chọn 1 giá trị, cùng giao diện (label, *, lỗi) với MsInput.
  Dùng thẻ <select> gốc của trình duyệt → hỗ trợ bàn phím và điện thoại sẵn.
CÁCH DÙNG:
  <ms-select v-model="form.Department" :options="DEPARTMENT_OPTIONS" label="Phòng ban" required
             placeholder="-- Chọn phòng ban --" :error="errors.Department" />
  options dạng [{ value, label }]. Giá trị trả ra GIỮ ĐÚNG KIỂU (number vẫn là number).
PROPS:
  modelValue, options, label, placeholder, required, disabled, error
EVENTS:
  update:modelValue, change
============================================================
-->
<template>
  <div :class="['ms-select', { 'has-error': !!error }]">
    <label v-if="label" :for="selectId" class="ms-select__label">
      {{ label }}<span v-if="required" class="ms-select__required">*</span>
    </label>
    <select
      :id="selectId"
      :value="selectedIndex"
      :disabled="disabled"
      :aria-invalid="!!error"
      class="ms-select__control"
      @change="handleChange"
    >
      <option v-if="placeholder" value="-1">{{ placeholder }}</option>
      <option v-for="(opt, index) in options" :key="String(opt.value)" :value="index">
        {{ opt.label }}
      </option>
    </select>
    <p v-if="error" class="ms-select__error">{{ error }}</p>
  </div>
</template>

<script setup>
import { computed, useId } from 'vue';

//#region Props & Emits
const props = defineProps({
  modelValue: { type: [String, Number, Boolean], default: '' },
  options: { type: Array, default: () => [] },
  label: { type: String, default: '' },
  placeholder: { type: String, default: '' },
  required: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
  error: { type: String, default: '' },
  /** Giá trị phát ra khi chọn placeholder */
  emptyValue: { type: [String, Number], default: '' },
});

const emit = defineEmits(['update:modelValue', 'change']);
//#endregion

const selectId = useId();

/**
 * Vị trí option đang chọn. <select> chỉ làm việc với chuỗi, nên dùng index làm value
 * rồi tra ngược ra giá trị thật → giữ đúng kiểu dữ liệu.
 */
const selectedIndex = computed(() => props.options.findIndex((o) => o.value === props.modelValue));

/**
 * Phát giá trị thật của option được chọn.
 * @param {Event} event
 */
const handleChange = (event) => {
  const index = Number(event.target.value);
  const value = index >= 0 ? props.options[index].value : props.emptyValue;
  emit('update:modelValue', value);
  emit('change', value);
};
</script>

<style scoped>
.ms-select {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  min-width: 0;
}
.ms-select__label {
  font-size: var(--font-size-sm);
  font-weight: 500;
}
.ms-select__required {
  color: var(--color-danger);
  margin-left: 2px;
}
.ms-select__control {
  height: 36px;
  padding: 0 var(--space-3);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius);
  background: var(--color-surface);
  cursor: pointer;
}
.ms-select__control:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}
.has-error .ms-select__control {
  border-color: var(--color-danger);
}
.ms-select__error {
  margin: 0;
  font-size: 12px;
  color: var(--color-danger);
}
</style>

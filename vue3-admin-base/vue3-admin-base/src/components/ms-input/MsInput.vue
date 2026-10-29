<!--
============================================================
COMPONENT: MsInput
TÁC DỤNG:
  Ô nhập liệu chuẩn: có label, dấu * bắt buộc, thông báo lỗi, icon đầu ô.
  Hỗ trợ text / number / date / email / password / tel.
CÁCH DÙNG:
  <ms-input v-model="form.FullName" label="Họ và tên" required :error="errors.FullName" />
  <ms-input v-model="form.Salary" type="number" label="Lương" :min="0" />
  <ms-input v-model="keyword" icon="search" placeholder="Tìm kiếm..." clearable />
  Lấy focus từ cha:  <ms-input ref="nameRef" />  →  nameRef.value.focus()
PROPS:
  modelValue (v-model), label, type, placeholder, required, disabled, error,
  icon, clearable, maxlength, min, max, step, autocomplete
EVENTS:
  update:modelValue — type="number" tự chuyển sang Number (ô rỗng → null)
  blur, enter
============================================================
-->
<template>
  <div :class="['ms-input', { 'has-error': !!error, 'is-disabled': disabled }]">
    <label v-if="label" :for="inputId" class="ms-input__label">
      {{ label }}<span v-if="required" class="ms-input__required">*</span>
    </label>
    <div class="ms-input__wrapper">
      <ms-icon v-if="icon" :name="icon" class="ms-input__icon" />
      <input
        :id="inputId"
        ref="inputRef"
        :value="modelValue"
        :type="type"
        :placeholder="placeholder"
        :disabled="disabled"
        :maxlength="maxlength"
        :min="min"
        :max="max"
        :step="step"
        :autocomplete="autocomplete"
        :aria-invalid="!!error"
        :aria-describedby="error ? `${inputId}-error` : undefined"
        :class="['ms-input__control', { 'has-icon': icon, 'has-clear': clearable }]"
        @input="handleInput"
        @blur="emit('blur', $event)"
        @keyup.enter="emit('enter', $event)"
      />
      <button
        v-if="clearable && modelValue !== '' && modelValue !== null && !disabled"
        type="button"
        class="ms-input__clear"
        title="Xoá"
        @click="clear"
      >
        <ms-icon name="close" :size="14" />
      </button>
    </div>
    <p v-if="error" :id="`${inputId}-error`" class="ms-input__error">{{ error }}</p>
  </div>
</template>

<script setup>
import { ref, useId } from 'vue';
import MsIcon from '@/components/ms-icon/MsIcon.vue';

//#region Props & Emits
const props = defineProps({
  modelValue: { type: [String, Number], default: '' },
  label: { type: String, default: '' },
  type: { type: String, default: 'text' },
  placeholder: { type: String, default: '' },
  required: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
  error: { type: String, default: '' },
  icon: { type: String, default: '' },
  clearable: { type: Boolean, default: false },
  maxlength: { type: [Number, String], default: undefined },
  min: { type: [Number, String], default: undefined },
  max: { type: [Number, String], default: undefined },
  step: { type: [Number, String], default: undefined },
  autocomplete: { type: String, default: 'off' },
});

const emit = defineEmits(['update:modelValue', 'blur', 'enter']);
//#endregion

/** id duy nhất cho mỗi ô (useId của Vue 3.5) để <label for> trỏ đúng input */
const inputId = useId();
const inputRef = ref(null);

/**
 * Phát giá trị mới. type="number" thì ép về Number để khỏi bị chuỗi "15000".
 * @param {InputEvent} event
 */
const handleInput = (event) => {
  const raw = event.target.value;
  if (props.type === 'number') emit('update:modelValue', raw === '' ? null : Number(raw));
  else emit('update:modelValue', raw);
};

/** Xoá nội dung và focus lại ô. */
const clear = () => {
  emit('update:modelValue', props.type === 'number' ? null : '');
  inputRef.value?.focus();
};

/** Cho component cha gọi focus() qua ref. */
const focus = () => inputRef.value?.focus();
defineExpose({ focus });
</script>

<style scoped>
.ms-input {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  min-width: 0;
}
.ms-input__label {
  font-size: var(--font-size-sm);
  font-weight: 500;
}
.ms-input__required {
  color: var(--color-danger);
  margin-left: 2px;
}
.ms-input__wrapper {
  position: relative;
  display: flex;
  align-items: center;
}
.ms-input__control {
  width: 100%;
  height: 36px;
  padding: 0 var(--space-3);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius);
  background: var(--color-surface);
  transition:
    border-color var(--transition),
    box-shadow var(--transition);
}
.ms-input__control.has-icon {
  padding-left: 34px;
}
.ms-input__control.has-clear {
  padding-right: 32px;
}
.ms-input__control:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}
.ms-input__control:disabled {
  background: var(--color-hover);
  cursor: not-allowed;
}
.ms-input__icon {
  position: absolute;
  left: 11px;
  color: var(--color-text-muted);
  pointer-events: none;
}
.ms-input__clear {
  position: absolute;
  right: 6px;
  display: flex;
  padding: 4px;
  border: none;
  background: none;
  color: var(--color-text-muted);
  cursor: pointer;
  border-radius: var(--radius-sm);
}
.ms-input__clear:hover {
  background: var(--color-hover);
}
.has-error .ms-input__control {
  border-color: var(--color-danger);
}
.has-error .ms-input__control:focus {
  box-shadow: 0 0 0 3px var(--color-danger-soft);
}
.ms-input__error {
  margin: 0;
  font-size: 12px;
  color: var(--color-danger);
}
</style>

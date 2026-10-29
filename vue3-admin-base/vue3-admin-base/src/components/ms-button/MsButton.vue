<!--
============================================================
COMPONENT: MsButton
TÁC DỤNG:
  Nút bấm chuẩn của hệ thống: nhiều kiểu màu, kích thước, có icon, trạng thái loading/disabled.
CÁCH DÙNG:
  <ms-button @click="onAdd" icon="plus">Thêm mới</ms-button>
  <ms-button type="danger" icon="trash" :disabled="!selected.length">Xoá</ms-button>
  <ms-button type="secondary">Huỷ</ms-button>
  <ms-button :loading="saving" native-type="submit">Lưu</ms-button>
  <ms-button type="text" icon="close" title="Đóng" />          ← nút chỉ có icon
PROPS:
  type        : primary | secondary | danger | text   (mặc định primary)
  size        : sm | md                               (mặc định md)
  icon        : tên icon của MsIcon
  iconPosition: left | right
  loading     : hiện vòng xoay + chặn bấm (tránh bấm Lưu 2 lần tạo 2 bản ghi)
  disabled    : vô hiệu hoá
  nativeType  : button | submit | reset (mặc định "button" để không vô tình submit form)
EVENTS:
  click(event) — không phát khi đang loading/disabled
============================================================
-->
<template>
  <button
    :type="nativeType"
    :class="['ms-button', `ms-button--${type}`, `ms-button--${size}`, { 'is-icon-only': !$slots.default }]"
    :disabled="disabled || loading"
    @click="handleClick"
  >
    <span v-if="loading" class="ms-button__spinner" />
    <ms-icon v-else-if="icon && iconPosition === 'left'" :name="icon" />
    <span v-if="$slots.default" class="ms-button__content"><slot /></span>
    <ms-icon v-if="!loading && icon && iconPosition === 'right'" :name="icon" />
  </button>
</template>

<script setup>
import MsIcon from '@/components/ms-icon/MsIcon.vue';

//#region Props & Emits
const props = defineProps({
  type: {
    type: String,
    default: 'primary',
    validator: (v) => ['primary', 'secondary', 'danger', 'text'].includes(v),
  },
  size: { type: String, default: 'md', validator: (v) => ['sm', 'md'].includes(v) },
  icon: { type: String, default: '' },
  iconPosition: { type: String, default: 'left', validator: (v) => ['left', 'right'].includes(v) },
  loading: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
  nativeType: { type: String, default: 'button' },
});

const emit = defineEmits(['click']);
//#endregion

/**
 * Phát sự kiện click khi nút đang cho phép bấm.
 * @param {MouseEvent} event
 */
const handleClick = (event) => {
  if (props.loading || props.disabled) return;
  emit('click', event);
};
</script>

<style scoped>
.ms-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  border: 1px solid transparent;
  border-radius: var(--radius);
  cursor: pointer;
  font-weight: 500;
  white-space: nowrap;
  transition:
    background var(--transition),
    border-color var(--transition),
    color var(--transition);
}
.ms-button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
.ms-button:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}

/* Kích thước */
.ms-button--md {
  height: 36px;
  padding: 0 var(--space-4);
}
.ms-button--sm {
  height: 28px;
  padding: 0 var(--space-3);
  font-size: var(--font-size-sm);
}
.ms-button.is-icon-only.ms-button--md {
  width: 36px;
  padding: 0;
}
.ms-button.is-icon-only.ms-button--sm {
  width: 28px;
  padding: 0;
}

/* Kiểu màu */
.ms-button--primary {
  background: var(--color-primary);
  color: #fff;
}
.ms-button--primary:hover:not(:disabled) {
  background: var(--color-primary-hover);
}
.ms-button--secondary {
  background: var(--color-surface);
  border-color: var(--color-border-strong);
}
.ms-button--secondary:hover:not(:disabled) {
  background: var(--color-hover);
}
.ms-button--danger {
  background: var(--color-danger);
  color: #fff;
}
.ms-button--danger:hover:not(:disabled) {
  background: var(--color-danger-hover);
}
.ms-button--text {
  background: transparent;
  color: var(--color-text-muted);
}
.ms-button--text:hover:not(:disabled) {
  background: var(--color-hover);
  color: var(--color-text);
}

/* Vòng xoay loading */
.ms-button__spinner {
  width: 14px;
  height: 14px;
  border: 2px solid currentColor;
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

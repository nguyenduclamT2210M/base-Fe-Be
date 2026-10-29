<!--
============================================================
COMPONENT: MsModal
TÁC DỤNG:
  Khung hộp thoại (modal) chung: lớp phủ tối, tiêu đề, nút đóng, thân và chân tuỳ biến.
  - Teleport ra <body> → không bị overflow/z-index của cha che mất
  - Đóng bằng phím Esc, nút X, hoặc bấm ra ngoài (tắt được)
  - Khoá cuộn trang phía sau khi đang mở
CÁCH DÙNG:
  <ms-modal v-model="isOpen" title="Thêm nhân viên" width="720px">
    ...nội dung form...
    <template #footer>
      <ms-button type="secondary" @click="isOpen = false">Huỷ</ms-button>
      <ms-button @click="save">Lưu</ms-button>
    </template>
  </ms-modal>
PROPS:
  modelValue (v-model: mở/đóng), title, width, closeOnOverlay (mặc định false — tránh
  bấm nhầm ra ngoài làm mất dữ liệu đang nhập), closeOnEsc
EVENTS:
  update:modelValue, close (mọi cách đóng), opened (sau khi mở — dùng để focus ô đầu)
============================================================
-->
<template>
  <Teleport to="body">
    <Transition name="ms-modal" @after-enter="emit('opened')">
      <div v-if="modelValue" class="ms-modal" @mousedown.self="closeOnOverlay && close()">
        <div
          class="ms-modal__dialog"
          :style="{ width }"
          role="dialog"
          aria-modal="true"
          :aria-labelledby="titleId"
        >
          <header class="ms-modal__header">
            <h3 :id="titleId">{{ title }}</h3>
            <ms-button type="text" icon="close" title="Đóng (Esc)" @click="close" />
          </header>
          <div class="ms-modal__body">
            <slot />
          </div>
          <footer v-if="$slots.footer" class="ms-modal__footer">
            <slot name="footer" />
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { onBeforeUnmount, useId, watch } from 'vue';
import MsButton from '@/components/ms-button/MsButton.vue';

//#region Props & Emits
const props = defineProps({
  modelValue: { type: Boolean, default: false },
  title: { type: String, default: '' },
  width: { type: String, default: '560px' },
  closeOnOverlay: { type: Boolean, default: false },
  closeOnEsc: { type: Boolean, default: true },
});

const emit = defineEmits(['update:modelValue', 'close', 'opened']);
//#endregion

const titleId = useId();

/** Đóng modal (cha nhận qua v-model và sự kiện close). */
const close = () => {
  emit('update:modelValue', false);
  emit('close');
};

/**
 * Bắt phím Esc để đóng.
 * @param {KeyboardEvent} e
 */
const onKeydown = (e) => {
  if (e.key === 'Escape' && props.closeOnEsc) close();
};

/** Khi mở: khoá cuộn body + nghe phím. Khi đóng: trả lại như cũ. */
watch(
  () => props.modelValue,
  (open) => {
    document.body.style.overflow = open ? 'hidden' : '';
    if (open) document.addEventListener('keydown', onKeydown);
    else document.removeEventListener('keydown', onKeydown);
  },
  { immediate: true },
);

/** Dọn dẹp nếu component bị huỷ khi modal còn mở (vd chuyển trang). */
onBeforeUnmount(() => {
  document.body.style.overflow = '';
  document.removeEventListener('keydown', onKeydown);
});
</script>

<style scoped>
.ms-modal {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 6vh var(--space-4) var(--space-4);
  background: rgba(17, 24, 39, 0.5);
  overflow-y: auto;
}
.ms-modal__dialog {
  max-width: 100%;
  display: flex;
  flex-direction: column;
  max-height: 88vh;
  background: var(--color-surface);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
}
.ms-modal__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--space-4) var(--space-5);
  border-bottom: 1px solid var(--color-border);
}
.ms-modal__header h3 {
  font-size: 17px;
}
.ms-modal__body {
  padding: var(--space-5);
  overflow-y: auto;
}
.ms-modal__footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-5);
  border-top: 1px solid var(--color-border);
  background: #f9fafb;
  border-radius: 0 0 var(--radius-lg) var(--radius-lg);
}

/* Hiệu ứng mở/đóng */
.ms-modal-enter-active,
.ms-modal-leave-active {
  transition: opacity 0.2s ease;
}
.ms-modal-enter-active .ms-modal__dialog,
.ms-modal-leave-active .ms-modal__dialog {
  transition: transform 0.2s ease;
}
.ms-modal-enter-from,
.ms-modal-leave-to {
  opacity: 0;
}
.ms-modal-enter-from .ms-modal__dialog,
.ms-modal-leave-to .ms-modal__dialog {
  transform: translateY(-16px);
}

@media (max-width: 640px) {
  .ms-modal {
    padding: 0;
  }
  .ms-modal__dialog {
    width: 100% !important;
    max-height: 100vh;
    min-height: 100vh;
    border-radius: 0;
  }
}
</style>

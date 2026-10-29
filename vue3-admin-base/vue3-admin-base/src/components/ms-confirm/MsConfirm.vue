<!--
============================================================
COMPONENT: MsConfirm
TÁC DỤNG:
  Hộp thoại xác nhận toàn cục, điều khiển bởi composable useConfirm.
CÁCH DÙNG:
  - Đặt DUY NHẤT 1 lần trong App.vue:  <ms-confirm />
  - Nơi khác:  const { confirm } = useConfirm();  if (await confirm('Xoá?')) {...}
  Phím Enter = Đồng ý, Esc = Huỷ.
============================================================
-->
<template>
  <Teleport to="body">
    <Transition name="ms-confirm">
      <div v-if="state.open" class="ms-confirm" @mousedown.self="answer(false)">
        <div class="ms-confirm__dialog" role="alertdialog" aria-modal="true" :aria-labelledby="titleId">
          <h3 :id="titleId" class="ms-confirm__title">{{ state.title }}</h3>
          <p class="ms-confirm__message">{{ state.message }}</p>
          <div class="ms-confirm__actions">
            <ms-button type="secondary" @click="answer(false)">{{ state.cancelText }}</ms-button>
            <ms-button :type="state.type" @click="answer(true)">{{ state.okText }}</ms-button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { onBeforeUnmount, useId, watch } from 'vue';
import MsButton from '@/components/ms-button/MsButton.vue';
import { useConfirm } from '@/composables/useConfirm';

const { state, answer } = useConfirm();
const titleId = useId();

/**
 * Phím tắt khi hộp thoại mở: Enter = đồng ý, Esc = huỷ.
 * stopPropagation để Esc không đóng luôn modal phía dưới.
 * @param {KeyboardEvent} e
 */
const onKeydown = (e) => {
  if (e.key === 'Escape') {
    e.stopImmediatePropagation();
    answer(false);
  } else if (e.key === 'Enter') {
    e.preventDefault();
    answer(true);
  }
};

watch(
  () => state.open,
  (open) => {
    // capture: true → chạy trước listener Esc của MsModal
    if (open) document.addEventListener('keydown', onKeydown, true);
    else document.removeEventListener('keydown', onKeydown, true);
  },
);

onBeforeUnmount(() => document.removeEventListener('keydown', onKeydown, true));
</script>

<style scoped>
.ms-confirm {
  position: fixed;
  inset: 0;
  z-index: var(--z-confirm);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-4);
  background: rgba(17, 24, 39, 0.45);
}
.ms-confirm__dialog {
  width: 420px;
  max-width: 100%;
  padding: var(--space-6);
  background: var(--color-surface);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
}
.ms-confirm__title {
  font-size: 17px;
  margin-bottom: var(--space-2);
}
.ms-confirm__message {
  margin: 0 0 var(--space-6);
  color: var(--color-text-muted);
}
.ms-confirm__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-3);
}
.ms-confirm-enter-active,
.ms-confirm-leave-active {
  transition: opacity 0.15s ease;
}
.ms-confirm-enter-from,
.ms-confirm-leave-to {
  opacity: 0;
}
</style>

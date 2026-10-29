<!--
============================================================
VIEW: LoginView  (route: /login)
TÁC DỤNG:
  Màn hình đăng nhập. Validate form → gọi authStore.login → chuyển tới trang
  người dùng định vào trước đó (?redirect=...) hoặc trang Tổng quan.
CÁCH DÙNG:
  Router tự hiển thị khi chưa đăng nhập. Tài khoản mock: admin / 123456
LUỒNG XỬ LÝ:
  submit → validate → loading nút → login() ─ thành công → router.replace(redirect)
                                          └ lỗi       → hiện lỗi dưới form
============================================================
-->
<template>
  <div class="login">
    <form class="login__card" novalidate @submit.prevent="handleSubmit">
      <div class="login__head">
        <span class="login__logo">A</span>
        <h1>Đăng nhập</h1>
        <p class="text-muted">{{ appTitle }}</p>
      </div>

      <ms-input
        v-model="form.username"
        label="Tên đăng nhập"
        required
        autocomplete="username"
        :error="errors.username"
      />
      <ms-input
        v-model="form.password"
        type="password"
        label="Mật khẩu"
        required
        autocomplete="current-password"
        :error="errors.password"
      />

      <p v-if="serverError" class="login__error" role="alert">{{ serverError }}</p>

      <ms-button native-type="submit" :loading="submitting" class="login__submit">Đăng nhập</ms-button>
      <p class="login__hint text-muted">Tài khoản demo: <b>admin</b> / <b>123456</b></p>
    </form>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import MsButton from '@/components/ms-button/MsButton.vue';
import MsInput from '@/components/ms-input/MsInput.vue';
import { useAuthStore } from '@/stores/auth';
import { rules, validateFields } from '@/utils/validator';

const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const appTitle = import.meta.env.VITE_APP_TITLE || 'Admin';

//#region State
const form = reactive({ username: '', password: '' });
const errors = ref({});
const serverError = ref('');
const submitting = ref(false);
//#endregion

/**
 * Chỉ chấp nhận redirect là đường dẫn nội bộ (bắt đầu bằng "/" nhưng không phải "//")
 * → chặn kiểu tấn công "open redirect" dẫn người dùng sang trang lừa đảo.
 * @returns {string}
 */
const getSafeRedirect = () => {
  const r = route.query.redirect;
  return typeof r === 'string' && r.startsWith('/') && !r.startsWith('//') ? r : '/';
};

/** Validate → đăng nhập → chuyển trang. */
const handleSubmit = async () => {
  serverError.value = '';
  errors.value = validateFields(form, {
    username: [rules.required('Tên đăng nhập')],
    password: [rules.required('Mật khẩu')],
  });
  if (Object.keys(errors.value).length) return;

  submitting.value = true;
  try {
    await auth.login({ ...form });
    router.replace(getSafeRedirect()); // replace: bấm Back không quay lại trang login
  } catch (e) {
    serverError.value = e.displayMessage || 'Đăng nhập thất bại';
  } finally {
    submitting.value = false;
  }
};
</script>

<style scoped>
.login {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: var(--space-4);
  background: linear-gradient(135deg, #eff6ff, #f5f7fa);
}
.login__card {
  width: 380px;
  max-width: 100%;
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  padding: 32px;
  background: var(--color-surface);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
}
.login__head {
  text-align: center;
}
.login__head h1 {
  font-size: 22px;
  margin-top: var(--space-3);
}
.login__head p {
  margin: var(--space-1) 0 0;
}
.login__logo {
  display: inline-grid;
  place-items: center;
  width: 44px;
  height: 44px;
  border-radius: var(--radius-lg);
  background: var(--color-primary);
  color: #fff;
  font-size: 20px;
  font-weight: 700;
}
.login__error {
  margin: 0;
  padding: var(--space-2) var(--space-3);
  border-radius: var(--radius);
  background: var(--color-danger-soft);
  color: var(--color-danger);
}
.login__submit {
  width: 100%;
  height: 40px;
}
.login__hint {
  margin: 0;
  text-align: center;
  font-size: var(--font-size-sm);
}
</style>

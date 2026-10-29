<!--
============================================================
LAYOUT: MainLayout
TÁC DỤNG:
  Khung chung cho mọi trang sau đăng nhập: sidebar menu + header + vùng nội dung.
  - Menu sinh tự động từ route có meta.menu (router/index.js)
  - Thu gọn/mở rộng sidebar (ghi nhớ qua F5); màn hình nhỏ thì sidebar trượt ra
  - Header: tiêu đề trang hiện tại, tên user, nút đăng xuất
CÁCH DÙNG:
  Không dùng trực tiếp trong view. Router render layout này cho các route con,
  nội dung trang hiển thị tại <router-view />.
  Muốn trang KHÔNG có sidebar (vd Login, in ấn) → khai báo route ngoài MainLayout.
============================================================
-->
<template>
  <div :class="['layout', { 'is-collapsed': app.sidebarCollapsed, 'is-mobile-open': mobileOpen }]">
    <aside class="layout__sidebar">
      <div class="layout__brand">
        <span class="layout__logo">A</span>
        <span class="layout__brand-text">{{ appTitle }}</span>
      </div>
      <nav class="layout__menu">
        <router-link
          v-for="item in menuItems"
          :key="item.name"
          :to="{ name: item.name }"
          class="layout__menu-item"
          :title="app.sidebarCollapsed ? item.meta.title : undefined"
          @click="mobileOpen = false"
        >
          <ms-icon :name="item.meta.icon || 'dashboard'" :size="18" />
          <span class="layout__menu-text">{{ item.meta.title }}</span>
        </router-link>
      </nav>
    </aside>

    <div class="layout__overlay" @click="mobileOpen = false" />

    <div class="layout__main">
      <header class="layout__header">
        <ms-button type="text" icon="menu" title="Thu gọn / mở menu" @click="toggleMenu" />
        <h2 class="layout__title">{{ route.meta.title }}</h2>
        <div class="layout__user">
          <span class="layout__avatar">{{ userInitial }}</span>
          <span class="layout__username">{{ auth.user?.FullName }}</span>
          <ms-button type="text" icon="logout" title="Đăng xuất" @click="handleLogout" />
        </div>
      </header>

      <main class="layout__content">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import MsButton from '@/components/ms-button/MsButton.vue';
import MsIcon from '@/components/ms-icon/MsIcon.vue';
import { useAppStore } from '@/stores/app';
import { useAuthStore } from '@/stores/auth';
import { useConfirm } from '@/composables/useConfirm';

const app = useAppStore();
const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const { confirm } = useConfirm();

const appTitle = import.meta.env.VITE_APP_TITLE || 'Admin';
/** Sidebar trượt ra trên màn hình nhỏ */
const mobileOpen = ref(false);

/** Các mục menu: route con của layout có meta.menu = true */
const menuItems = computed(() =>
  (router.options.routes.find((r) => r.path === '/')?.children || []).filter((r) => r.meta?.menu),
);

/** Chữ cái đầu của TÊN (từ cuối, theo cách gọi tên tiếng Việt) cho avatar, viết hoa */
const userInitial = computed(
  () => (auth.user?.FullName?.trim().split(/\s+/).pop()?.[0] || 'U').toUpperCase(),
);

/** Màn hình nhỏ (≤ 900px) → trượt sidebar; màn hình lớn → thu gọn/mở rộng. */
const toggleMenu = () => {
  if (window.matchMedia('(max-width: 900px)').matches) mobileOpen.value = !mobileOpen.value;
  else app.toggleSidebar();
};

/** Hỏi xác nhận → đăng xuất → về trang Login. */
const handleLogout = async () => {
  if (!(await confirm('Bạn có muốn đăng xuất?', { okText: 'Đăng xuất' }))) return;
  auth.logout();
  router.push({ name: 'login' });
};
</script>

<style scoped>
.layout {
  display: flex;
  min-height: 100vh;
}

/* ===== Sidebar ===== */
.layout__sidebar {
  position: fixed;
  inset: 0 auto 0 0;
  z-index: var(--z-header);
  width: var(--sidebar-width);
  display: flex;
  flex-direction: column;
  background: var(--sidebar-bg);
  color: var(--sidebar-text);
  transition:
    width var(--transition),
    transform var(--transition);
  overflow: hidden;
}
.layout__brand {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  height: var(--header-height);
  padding: 0 var(--space-4);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  white-space: nowrap;
}
.layout__logo {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
  display: grid;
  place-items: center;
  border-radius: var(--radius);
  background: var(--sidebar-active);
  color: #fff;
  font-weight: 700;
}
.layout__brand-text {
  font-weight: 600;
  color: #fff;
}
.layout__menu {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: var(--space-3) var(--space-2);
}
.layout__menu-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  height: 40px;
  padding: 0 var(--space-3);
  border-radius: var(--radius);
  color: var(--sidebar-text);
  white-space: nowrap;
  transition: background var(--transition);
}
.layout__menu-item:hover {
  background: rgba(255, 255, 255, 0.06);
  color: #fff;
}
.layout__menu-item.router-link-active {
  background: var(--sidebar-active);
  color: #fff;
}

/* Thu gọn */
.is-collapsed .layout__sidebar {
  width: var(--sidebar-width-collapsed);
}
.is-collapsed .layout__brand-text,
.is-collapsed .layout__menu-text {
  display: none;
}
.is-collapsed .layout__menu-item {
  justify-content: center;
  padding: 0;
}

/* ===== Main ===== */
.layout__main {
  flex: 1;
  min-width: 0;
  margin-left: var(--sidebar-width);
  transition: margin-left var(--transition);
}
.is-collapsed .layout__main {
  margin-left: var(--sidebar-width-collapsed);
}
.layout__header {
  position: sticky;
  top: 0;
  z-index: calc(var(--z-header) - 1);
  display: flex;
  align-items: center;
  gap: var(--space-3);
  height: var(--header-height);
  padding: 0 var(--space-5) 0 var(--space-3);
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
}
.layout__title {
  flex: 1;
  font-size: 16px;
}
.layout__user {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}
.layout__avatar {
  width: 32px;
  height: 32px;
  display: grid;
  place-items: center;
  border-radius: 50%;
  background: var(--color-primary-soft);
  color: var(--color-primary);
  font-weight: 600;
}
.layout__content {
  padding: var(--space-5);
}
.layout__overlay {
  display: none;
}

/* ===== Màn hình nhỏ: sidebar trượt ra ===== */
@media (max-width: 900px) {
  .layout__sidebar,
  .is-collapsed .layout__sidebar {
    width: var(--sidebar-width);
    transform: translateX(-100%);
  }
  .is-collapsed .layout__brand-text,
  .is-collapsed .layout__menu-text {
    display: inline;
  }
  .is-collapsed .layout__menu-item {
    justify-content: flex-start;
    padding: 0 var(--space-3);
  }
  .is-mobile-open .layout__sidebar {
    transform: none;
  }
  .is-mobile-open .layout__overlay {
    display: block;
    position: fixed;
    inset: 0;
    z-index: calc(var(--z-header) - 1);
    background: rgba(0, 0, 0, 0.4);
  }
  .layout__main,
  .is-collapsed .layout__main {
    margin-left: 0;
  }
  .layout__content {
    padding: var(--space-3);
  }
  .layout__username {
    display: none;
  }
}
</style>

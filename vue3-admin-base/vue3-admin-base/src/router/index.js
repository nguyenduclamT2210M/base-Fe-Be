/**
 * ============================================================
 * FILE: src/router/index.js
 * TÁC DỤNG:
 *   Khai báo các trang (route) và "navigation guard" — hàm chạy trước mỗi lần chuyển trang:
 *   - Trang có meta.requiresAuth mà chưa đăng nhập → chuyển về /login?redirect=...
 *   - Đã đăng nhập mà vào /login → chuyển về trang chủ
 *   - Đổi tiêu đề tab trình duyệt theo meta.title
 *   Menu sidebar được SINH TỰ ĐỘNG từ các route có meta.menu (xem MainLayout.vue).
 * CÁCH DÙNG (thêm 1 trang mới):
 *   1. Tạo view: src/views/product/ProductList.vue
 *   2. Thêm vào mảng children của MainLayout:
 *      {
 *        path: 'products', name: 'products',
 *        component: () => import('@/views/product/ProductList.vue'),  // lazy load
 *        meta: { title: 'Sản phẩm', menu: true, icon: 'dashboard' },
 *      }
 *   3. Chuyển trang trong code:  router.push({ name: 'products' })
 *      Trong template:            <router-link :to="{ name: 'products' }">...</router-link>
 * META HỖ TRỢ:
 *   title (tiêu đề tab), menu (hiện trên sidebar), icon (tên MsIcon), requiresAuth
 * VÌ SAO LAZY LOAD:
 *   `() => import(...)` tách mỗi trang thành 1 file JS riêng, chỉ tải khi vào trang
 *   → lần mở app đầu tiên nhanh hơn.
 * ============================================================
 */
import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import MainLayout from '@/layouts/MainLayout.vue';

const APP_TITLE = import.meta.env.VITE_APP_TITLE || 'Admin';

export const routes = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/auth/LoginView.vue'),
    meta: { title: 'Đăng nhập', guestOnly: true },
  },
  {
    path: '/',
    component: MainLayout,
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: { name: 'dashboard' } },
      {
        path: 'dashboard',
        name: 'dashboard',
        component: () => import('@/views/dashboard/DashboardView.vue'),
        meta: { title: 'Tổng quan', menu: true, icon: 'dashboard' },
      },
      {
        path: 'employees',
        name: 'employees',
        component: () => import('@/views/employee/EmployeeList.vue'),
        meta: { title: 'Nhân viên', menu: true, icon: 'users' },
      },
    ],
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/views/error/NotFoundView.vue'),
    meta: { title: 'Không tìm thấy trang' },
  },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  /** Chuyển trang thì cuộn lên đầu; bấm Back thì về vị trí cũ. */
  scrollBehavior: (_to, _from, saved) => saved || { top: 0 },
});

/**
 * Guard chạy trước mỗi lần chuyển trang.
 * `to.matched.some(...)` để route con thừa hưởng requiresAuth của route cha.
 * @returns {true | import('vue-router').RouteLocationRaw} true = cho đi, object = chuyển hướng
 */
router.beforeEach((to) => {
  const auth = useAuthStore();
  const requiresAuth = to.matched.some((r) => r.meta.requiresAuth);

  if (requiresAuth && !auth.isLoggedIn) {
    return { name: 'login', query: { redirect: to.fullPath } };
  }
  if (to.meta.guestOnly && auth.isLoggedIn) {
    return { name: 'dashboard' };
  }
  return true;
});

/** Đặt tiêu đề tab sau khi chuyển trang xong. */
router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} | ${APP_TITLE}` : APP_TITLE;
});

export default router;

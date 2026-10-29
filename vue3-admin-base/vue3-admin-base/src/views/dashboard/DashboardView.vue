<!--
============================================================
VIEW: DashboardView  (route: /dashboard)
TÁC DỤNG:
  Trang tổng quan: thẻ số liệu (KPI) và biểu đồ cột đơn giản số nhân viên theo phòng ban.
  Biểu đồ vẽ bằng CSS thuần, không dùng thư viện. Cần biểu đồ phức tạp thì dùng
  ECharts hoặc Chart.js.
CÁCH DÙNG:
  Router tự hiển thị. Thêm thẻ KPI: thêm phần tử vào computed `cards`.
============================================================
-->
<template>
  <div class="dashboard">
    <div class="dashboard__cards">
      <div v-for="card in cards" :key="card.label" class="dashboard__card page">
        <span class="dashboard__card-label text-muted">{{ card.label }}</span>
        <span class="dashboard__card-value">{{ card.value }}</span>
      </div>
    </div>

    <section class="page">
      <div class="page-header">
        <h1>Nhân viên theo phòng ban</h1>
        <ms-button type="secondary" icon="refresh" size="sm" @click="loadStatistics">Tải lại</ms-button>
      </div>
      <div v-if="stats" class="dashboard__bars">
        <div v-for="item in stats.byDepartment" :key="item.department" class="dashboard__bar-row">
          <span class="dashboard__bar-label">{{ item.department }}</span>
          <div class="dashboard__bar-track">
            <div class="dashboard__bar" :style="{ width: barWidth(item.count) }" />
          </div>
          <span class="dashboard__bar-value">{{ item.count }}</span>
        </div>
      </div>
      <p v-else class="text-muted">Đang tải...</p>
    </section>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import MsButton from '@/components/ms-button/MsButton.vue';
import EmployeeAPI from '@/apis/components/employee/EmployeeAPI';
import { formatCurrency, formatNumber } from '@/utils/formatter';

/** Dữ liệu thống kê từ API */
const stats = ref(null);

/** Thẻ KPI tính từ stats */
const cards = computed(() => [
  { label: 'Tổng nhân viên', value: formatNumber(stats.value?.total) || '—' },
  { label: 'Đang làm việc', value: formatNumber(stats.value?.active) || '—' },
  { label: 'Tổng quỹ lương / tháng', value: formatCurrency(stats.value?.totalSalary) || '—' },
  { label: 'Lương trung bình', value: formatCurrency(stats.value?.avgSalary) || '—' },
]);

/** Giá trị lớn nhất để tính tỉ lệ độ dài cột */
const maxCount = computed(() => Math.max(1, ...(stats.value?.byDepartment || []).map((d) => d.count)));

/**
 * Độ rộng cột theo phần trăm so với cột lớn nhất.
 * @param {number} count
 * @returns {string} vd "62.5%"
 */
const barWidth = (count) => `${(count / maxCount.value) * 100}%`;

/** Gọi API lấy thống kê. Lỗi đã được interceptor báo bằng toast. */
const loadStatistics = async () => {
  try {
    stats.value = await EmployeeAPI.getStatistics();
  } catch {
    /* đã toast */
  }
};

onMounted(loadStatistics);
</script>

<style scoped>
.dashboard {
  display: flex;
  flex-direction: column;
  gap: var(--space-5);
}
.dashboard__cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: var(--space-4);
}
.dashboard__card {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}
.dashboard__card-label {
  font-size: var(--font-size-sm);
}
.dashboard__card-value {
  font-size: 24px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}
.dashboard__bars {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}
.dashboard__bar-row {
  display: grid;
  grid-template-columns: 110px 1fr 40px;
  align-items: center;
  gap: var(--space-3);
}
.dashboard__bar-track {
  height: 14px;
  background: var(--color-hover);
  border-radius: 7px;
  overflow: hidden;
}
.dashboard__bar {
  height: 100%;
  background: var(--color-primary);
  border-radius: 7px;
  transition: width 0.4s ease;
}
.dashboard__bar-value {
  text-align: right;
  font-variant-numeric: tabular-nums;
}
</style>

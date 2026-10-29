/**
 * ============================================================
 * FILE: src/composables/useCrud.js
 * TÁC DỤNG:
 *   Đóng gói toàn bộ LOGIC của một màn hình danh sách CRUD:
 *   tải dữ liệu phân trang, tìm kiếm (có debounce), sắp xếp, lọc,
 *   chọn nhiều dòng, thêm/sửa/xoá/xoá nhiều kèm xác nhận và toast.
 *   → Mỗi màn danh sách mới (Sản phẩm, Khách hàng...) chỉ còn viết phần giao diện.
 * CÁCH DÙNG:
 *   const crud = useCrud(EmployeeAPI, {
 *     defaultSort: { sortBy: 'Code', sortOrder: 'asc' },
 *     entityName: 'nhân viên',          // dùng trong câu thông báo
 *     getDisplayName: (row) => row.FullName,
 *   });
 *   onMounted(crud.loadData);
 *   // template: :rows="crud.rows.value" :loading="crud.loading.value" ...
 *   //           @update:keyword → crud.search()
 *   await crud.save(model.toPayload());   // tự biết create hay update theo Id
 *   await crud.remove(row);               // hỏi xác nhận → xoá → tải lại
 * VÌ SAO LÀ COMPOSABLE:
 *   Composable = hàm dùng Composition API để tái sử dụng logic có state.
 *   Mỗi lần gọi useCrud() tạo một bộ state RIÊNG, nên nhiều màn không ảnh hưởng nhau.
 * ============================================================
 */
import { reactive, ref } from 'vue';
import { useAppStore } from '@/stores/app';
import { useConfirm } from '@/composables/useConfirm';
import { debounce } from '@/utils/helpers';
import { DEFAULT_PAGE_SIZE } from '@/utils/constants';

/**
 * @param {import('@/apis/base/BaseAPI').default} api - instance API kế thừa BaseAPI
 * @param {Object} [options]
 * @param {{sortBy?:string, sortOrder?:'asc'|'desc'}} [options.defaultSort]
 * @param {Object} [options.defaultFilters] - bộ lọc ban đầu, vd { Department: '' }
 * @param {string} [options.entityName='bản ghi'] - tên đối tượng trong thông báo
 * @param {(row:Object)=>string} [options.getDisplayName] - lấy tên hiển thị khi hỏi xoá
 * @param {string} [options.idKey='Id'] - tên trường khoá chính
 */
export function useCrud(api, options = {}) {
  const {
    defaultSort = {},
    defaultFilters = {},
    entityName = 'bản ghi',
    getDisplayName = () => '',
    idKey = 'Id',
  } = options;

  const app = useAppStore();
  const { confirm } = useConfirm();

  //#region State
  /** Dữ liệu trang hiện tại */
  const rows = ref([]);
  /** Tổng số bản ghi (sau lọc) — dùng cho phân trang */
  const total = ref(0);
  /** Đang tải bảng (loading cục bộ của bảng, khác loading toàn màn hình) */
  const loading = ref(false);
  /** Danh sách id các dòng đang được chọn */
  const selectedIds = ref([]);
  /** Điều kiện truy vấn gửi lên API */
  const query = reactive({
    pageIndex: 1,
    pageSize: DEFAULT_PAGE_SIZE,
    keyword: '',
    sortBy: defaultSort.sortBy || '',
    sortOrder: defaultSort.sortOrder || 'asc',
    filters: { ...defaultFilters },
  });
  //#endregion

  //#region Load data
  /** Đánh số cho mỗi lần tải để bỏ kết quả của request cũ về muộn (race condition). */
  let requestSeq = 0;

  /**
   * Tải dữ liệu theo `query` hiện tại.
   * Dùng `silent` vì bảng có loading riêng, không cần che cả màn hình.
   * @returns {Promise<void>}
   */
  const loadData = async () => {
    const seq = ++requestSeq;
    loading.value = true;
    try {
      const res = await api.paging({ ...query }, { silent: true });
      if (seq !== requestSeq) return; // đã có lần tải mới hơn → bỏ kết quả này
      rows.value = res.data;
      total.value = res.total;
      // Trang hiện tại trống (vd vừa xoá hết dòng ở trang cuối) → lùi 1 trang
      if (!res.data.length && query.pageIndex > 1) {
        query.pageIndex--;
        return loadData();
      }
    } catch {
      if (seq === requestSeq) rows.value = [];
    } finally {
      if (seq === requestSeq) loading.value = false;
    }
  };

  /** Tìm kiếm: về trang 1 rồi tải lại. Có debounce 400ms cho ô gõ từ khoá. */
  const search = debounce(() => {
    query.pageIndex = 1;
    loadData();
  }, 400);

  /**
   * Đổi trang hoặc kích thước trang.
   * @param {{pageIndex?:number, pageSize?:number}} paging
   */
  const changePage = ({ pageIndex, pageSize }) => {
    if (pageSize && pageSize !== query.pageSize) {
      query.pageSize = pageSize;
      query.pageIndex = 1;
    } else if (pageIndex) {
      query.pageIndex = pageIndex;
    }
    loadData();
  };

  /**
   * Đổi sắp xếp (MsTable emit khi bấm tiêu đề cột).
   * @param {{sortBy:string, sortOrder:'asc'|'desc'}} sort
   */
  const changeSort = ({ sortBy, sortOrder }) => {
    query.sortBy = sortBy;
    query.sortOrder = sortOrder;
    loadData();
  };

  /**
   * Đổi bộ lọc rồi tải lại từ trang 1.
   * @param {Object} filters - các trường lọc cần đổi
   */
  const changeFilters = (filters) => {
    Object.assign(query.filters, filters);
    query.pageIndex = 1;
    loadData();
  };
  //#endregion

  //#region Mutations
  /**
   * Lưu bản ghi: có id → update, không có → create. Thành công thì toast + tải lại.
   * Lỗi được ném lại để form giữ nguyên trạng thái (không đóng).
   * @param {Object} payload - dữ liệu đã chuẩn hoá (model.toPayload())
   * @returns {Promise<Object>} bản ghi sau khi lưu
   */
  const save = async (payload) => {
    const isEdit = payload[idKey] !== null && payload[idKey] !== undefined;
    const result = isEdit ? await api.update(payload[idKey], payload) : await api.create(payload);
    app.toastSuccess(isEdit ? `Cập nhật ${entityName} thành công` : `Thêm ${entityName} thành công`);
    await loadData();
    return result;
  };

  /**
   * Xoá 1 dòng sau khi người dùng xác nhận.
   * @param {Object} row - dòng cần xoá
   * @returns {Promise<boolean>} true nếu đã xoá
   */
  const remove = async (row) => {
    const name = getDisplayName(row);
    const ok = await confirm(`Bạn có chắc muốn xoá ${entityName} ${name ? `"${name}"` : ''}?`, {
      title: `Xoá ${entityName}`,
      okText: 'Xoá',
      type: 'danger',
    });
    if (!ok) return false;
    try {
      await api.delete(row[idKey]);
      selectedIds.value = selectedIds.value.filter((id) => id !== row[idKey]);
      app.toastSuccess(`Đã xoá ${entityName}`);
      await loadData();
      return true;
    } catch {
      return false; // toast lỗi đã được interceptor hiển thị
    }
  };

  /**
   * Xoá tất cả dòng đang chọn sau khi xác nhận.
   * @returns {Promise<boolean>}
   */
  const removeSelected = async () => {
    const count = selectedIds.value.length;
    if (!count) return false;
    const ok = await confirm(`Bạn có chắc muốn xoá ${count} ${entityName} đã chọn?`, {
      title: 'Xoá nhiều',
      okText: 'Xoá',
      type: 'danger',
    });
    if (!ok) return false;
    try {
      await api.deleteMany(selectedIds.value);
      selectedIds.value = [];
      app.toastSuccess(`Đã xoá ${count} ${entityName}`);
      await loadData();
      return true;
    } catch {
      return false;
    }
  };
  //#endregion

  return {
    rows,
    total,
    loading,
    selectedIds,
    query,
    loadData,
    search,
    changePage,
    changeSort,
    changeFilters,
    save,
    remove,
    removeSelected,
  };
}

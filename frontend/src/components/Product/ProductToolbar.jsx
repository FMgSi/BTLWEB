import { FILTER_TAGS } from '../../data/mockData';

/**
 * Component thanh công cụ đầu danh sách sản phẩm:
 * - Hiển thị tiêu đề và Breadcrumb
 * - Các nút Tag danh mục nhanh (Tất cả, Gaming, Văn phòng, Đồ họa, Mỏng nhẹ)
 * - Dropdown chọn tiêu chí sắp xếp (Mới nhất, Giá tăng dần, Giá giảm dần)
 */
export default function ProductToolbar({ activeTag, setActiveTag, sortBy, setSortBy, onToggleMobileFilter }) {
  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-2">
        <h4 className="fw-bold m-0" style={{ fontSize: '18px' }}>SẢN PHẨM</h4>
        <nav aria-label="breadcrumb">
          <ol className="breadcrumb mb-0" style={{ fontSize: '11px' }}>
            <li className="breadcrumb-item"><a href="#" className="text-decoration-none text-muted">Trang chủ</a></li>
            <li className="breadcrumb-item active text-muted" aria-current="page">Sản phẩm</li>
          </ol>
        </nav>
      </div>

      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        {/* Nút Tag danh mục */}
        <div className="d-flex flex-wrap gap-2">
          {FILTER_TAGS.map((tag) => (
            <button
              key={tag}
              className={`filter-tag-btn ${activeTag === tag ? 'active' : ''}`}
              onClick={() => setActiveTag(tag)}
            >
              {tag}
            </button>
          ))}
        </div>

        {/* Nút mở bộ lọc mobile & Dropdown chọn kiểu sắp xếp */}
        <div className="d-flex align-items-center gap-2 ms-auto ms-sm-0">
          {onToggleMobileFilter && (
            <button
              type="button"
              className="btn btn-sm btn-outline-secondary d-md-none d-flex align-items-center gap-1"
              onClick={onToggleMobileFilter}
              title="Mở bộ lọc thương hiệu, giá, RAM, CPU"
            >
              <i className="bi bi-funnel"></i>
              <span>Lọc</span>
            </button>
          )}
          <select
            className="form-select form-select-sm"
            style={{ fontSize: '12px', minWidth: '140px' }}
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
          >
            <option value="newest">Sắp xếp: Mới nhất</option>
            <option value="price-asc">Giá tăng dần</option>
            <option value="price-desc">Giá giảm dần</option>
          </select>
        </div>
      </div>
    </>
  );
}
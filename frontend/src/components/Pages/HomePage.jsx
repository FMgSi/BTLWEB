import React from 'react';
import CategorySidebar from '../Sidebar/CategorySidebar';
import ProductCard from '../Product/ProductCard';
import CellphoneSArticleSection from './CellphoneSArticleSection';
import './Pages.css';

export default function HomePage({
  products = [],
  onSelectProduct,
  onAddToCart,
  onNavigateToProducts,
  onSelectCategory,
}) {
  // Lấy 5 sản phẩm khuyến mãi hot cho trang chủ
  const hotProducts = products.slice(0, 5);

  return (
    <div className="site-container py-3">
      {/* 1. KHU VỰC TOP: MENU PHÂN LOẠI + BANNER CHÍNH DARK + 2 BANNER PHỤ */}
      <div className="row g-3 mb-3">
        {/* Cột trái: Phân loại Laptop (5 danh mục có icon và mũi tên >) */}
        <div className="col-12 col-lg-auto d-none d-lg-block" style={{ width: '256px' }}>
          <CategorySidebar onSelectCategory={onSelectCategory || onNavigateToProducts} />
        </div>

        {/* Cột giữa: Banner Hero chính nền tối */}
        <div className="col-12 col-lg">
          <div
            className="rounded-3 p-4 text-center text-white d-flex flex-column align-items-center justify-content-center position-relative shadow-sm"
            style={{
              backgroundColor: '#18181b',
              minHeight: '260px',
              height: '100%',
              backgroundImage: 'radial-gradient(circle at 50% 50%, #27272a 0%, #18181b 100%)',
            }}
          >
            {/* Huy hiệu đỏ */}
            <span
              className="badge bg-danger rounded-pill px-3 py-1 mb-2 fw-semibold"
              style={{ fontSize: '11px', letterSpacing: '0.5px' }}
            >
              CHƯƠNG TRÌNH ƯU ĐÃI NĂM HỌC MỚI
            </span>

            {/* Tiêu đề vàng */}
            <h2
              className="fw-black mb-2"
              style={{ color: '#fbbf24', fontSize: '24px', letterSpacing: '-0.3px', maxWidth: '600px' }}
            >
              LAPTOP SINH VIÊN & VĂN PHÒNG GIÁ CỰC TỐT
            </h2>

            {/* Phụ đề */}
            <p className="text-light opacity-90 small mb-3">
              Giảm ngay 10% + Tặng Balo chính hãng & Chuột không dây
            </p>

            {/* Nút MUA NGAY đỏ */}
            <button
              type="button"
              className="btn btn-danger px-4 py-2 fw-bold rounded-2 shadow"
              style={{ backgroundColor: '#e11b1e', borderColor: '#e11b1e', fontSize: '13px' }}
              onClick={() => (onSelectCategory ? onSelectCategory('student') : onNavigateToProducts && onNavigateToProducts())}
            >
              MUA NGAY
            </button>
          </div>
        </div>

        {/* Cột phải: 2 Banner phụ (MacBook & Gaming) */}
        <div className="col-12 col-xl-auto d-none d-xl-flex flex-column justify-content-between gap-3" style={{ width: '270px' }}>
          {/* Banner phụ 1: MacBook Cho Sinh Viên */}
          <div
            className="rounded-3 p-3 text-white d-flex flex-column justify-content-center shadow-sm"
            style={{ backgroundColor: '#334155', height: 'calc(50% - 6px)', cursor: 'pointer' }}
            onClick={() => (onSelectCategory ? onSelectCategory('macbook') : onNavigateToProducts && onNavigateToProducts())}
          >
            <h6 className="fw-bold mb-1" style={{ color: '#38bdf8' }}>MacBook Cho Sinh Viên</h6>
            <div className="small text-light opacity-90">Trả góp 0% lãi suất</div>
          </div>

          {/* Banner phụ 2: Laptop Gaming Cực Khủng */}
          <div
            className="rounded-3 p-3 text-white d-flex flex-column justify-content-center shadow-sm"
            style={{ backgroundColor: '#2563eb', height: 'calc(50% - 6px)', cursor: 'pointer' }}
            onClick={() => (onSelectCategory ? onSelectCategory('gaming') : onNavigateToProducts && onNavigateToProducts())}
          >
            <h6 className="fw-bold mb-1 text-white">Laptop Gaming Cực Khủng</h6>
            <div className="small text-light opacity-90">Giảm sốc tới 30%</div>
          </div>
        </div>
      </div>

      {/* 2. 4 THẺ CAM KẾT DỊCH VỤ NGANG CHUẨN ẢNH 1 */}
      <div className="bg-white rounded-3 border p-3 mb-4 shadow-sm">
        <div className="row g-3 text-center text-md-start">
          <div className="col-6 col-md-3 border-end">
            <div className="d-flex align-items-center gap-2">
              <i className="bi bi-truck text-danger fs-4"></i>
              <div>
                <div className="fw-bold text-danger small">GIAO HÀNG TẬN NƠI</div>
                <div className="text-muted" style={{ fontSize: '11px' }}>Miễn phí toàn quốc đơn &gt; 5tr</div>
              </div>
            </div>
          </div>

          <div className="col-6 col-md-3 border-end">
            <div className="d-flex align-items-center gap-2">
              <i className="bi bi-shield-check text-danger fs-4"></i>
              <div>
                <div className="fw-bold text-danger small">BẢO HÀNH CHÍNH HÃNG</div>
                <div className="text-muted" style={{ fontSize: '11px' }}>Cam kết 100% linh kiện chuẩn</div>
              </div>
            </div>
          </div>

          <div className="col-6 col-md-3 border-end">
            <div className="d-flex align-items-center gap-2">
              <i className="bi bi-credit-card-2-front text-danger fs-4"></i>
              <div>
                <div className="fw-bold text-danger small">TRẢ GÓP 0% LÃI SUẤT</div>
                <div className="text-muted" style={{ fontSize: '11px' }}>Thủ tục nhanh gọn trong 5 phút</div>
              </div>
            </div>
          </div>

          <div className="col-6 col-md-3">
            <div className="d-flex align-items-center gap-2">
              <i className="bi bi-headset text-danger fs-4"></i>
              <div>
                <div className="fw-bold text-danger small">HỖ TRỢ KỸ THUẬT 24/7</div>
                <div className="text-muted" style={{ fontSize: '11px' }}>Cài đặt phần mềm miễn phí</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* 3. KHU VỰC LAPTOP KHUYẾN MÃI HOT */}
      <div className="mb-4">
        <div className="d-flex justify-content-between align-items-center mb-3">
          <h4 className="fw-bold text-danger m-0 d-flex align-items-center gap-2 fs-5">
            <i className="bi bi-fire"></i>
            <span>LAPTOP KHUYẾN MÃI HOT</span>
          </h4>

          <button
            type="button"
            className="btn btn-outline-danger btn-sm rounded-pill px-3"
            onClick={() => onNavigateToProducts && onNavigateToProducts()}
          >
            Xem tất cả sản phẩm <i className="bi bi-arrow-right ms-1"></i>
          </button>
        </div>

        {/* 5 Card sản phẩm khuyến mãi */}
        <div className="row row-cols-2 row-cols-md-3 row-cols-lg-5 g-2">
          {hotProducts.map((p) => (
            <div className="col" key={p.id}>
              <ProductCard
                item={p}
                onSelectProduct={onSelectProduct}
                onAddToCart={onAddToCart}
              />
            </div>
          ))}
        </div>
      </div>

      {/* 4. KHUNG BÀI VIẾT & MỤC LỤC NỘI DUNG CHÍNH CHUẨN CELLPHONES */}
      <CellphoneSArticleSection />
    </div>
  );
}

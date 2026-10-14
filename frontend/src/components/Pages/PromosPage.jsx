import React, { useState } from 'react';
import ProductCard from '../Product/ProductCard';
import './Pages.css';

export default function PromosPage({ products = [], onSelectProduct, onAddToCart, onNavigateToProducts }) {
  const [activeTab, setActiveTab] = useState('available');
  const [claimedCodes, setClaimedCodes] = useState(['LAPTOP2026']);

  const handleClaim = (code) => {
    if (!claimedCodes.includes(code)) {
      setClaimedCodes([...claimedCodes, code]);
    }
  };

  // Lấy các sản phẩm có giảm giá
  const promoProducts = products.filter((p) => p.discountPrice || p.originalPrice).slice(0, 5);

  return (
    <div className="site-container py-3">
      {/* 1. TOP BANNER KHUYẾN MÃI NỀN TỐI */}
      <div
        className="rounded-3 p-4 text-center text-white mb-4 shadow-sm"
        style={{
          backgroundColor: '#18181b',
          backgroundImage: 'radial-gradient(circle at 50% 50%, #27272a 0%, #18181b 100%)',
        }}
      >
        <span
          className="badge bg-danger rounded-pill px-3 py-1 mb-2 fw-semibold"
          style={{ fontSize: '11px', letterSpacing: '0.5px' }}
        >
          ★ TRUNG TÂM MÃ GIẢM GIÁ PCSTORE
        </span>

        <h1 className="fw-black mb-2" style={{ color: '#fbbf24', fontSize: '26px', letterSpacing: '-0.3px' }}>
          KHUYẾN MÃI ĐANG CÓ &amp; SẮP DIỄN RA
        </h1>

        <p className="text-light opacity-90 small mb-0">
          Lưu ngay các mã ưu đãi vào Ví Voucher của bạn để tự động trừ tiền khi thanh toán!
        </p>
      </div>

      {/* 2. THANH TAB CHỌN MÃ */}
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3 pb-2 border-bottom">
        <div className="d-flex gap-2">
          <button
            type="button"
            className={`btn btn-sm ${activeTab === 'available' ? 'btn-danger' : 'btn-light border'} fw-semibold px-3`}
            onClick={() => setActiveTab('available')}
            style={activeTab === 'available' ? { backgroundColor: '#e11b1e' } : {}}
          >
            <i className="bi bi-gift-fill me-1"></i> Mã Có Thể Nhận (3)
          </button>

          <button
            type="button"
            className={`btn btn-sm ${activeTab === 'claimed' ? 'btn-danger' : 'btn-light border'} fw-semibold px-3`}
            onClick={() => setActiveTab('claimed')}
            style={activeTab === 'claimed' ? { backgroundColor: '#e11b1e' } : {}}
          >
            <i className="bi bi-check-circle-fill me-1"></i> Mã Đã Nhận ({claimedCodes.length})
          </button>

          <button
            type="button"
            className={`btn btn-sm ${activeTab === 'upcoming' ? 'btn-danger' : 'btn-light border'} fw-semibold px-3`}
            onClick={() => setActiveTab('upcoming')}
            style={activeTab === 'upcoming' ? { backgroundColor: '#e11b1e' } : {}}
          >
            <i className="bi bi-clock-history me-1"></i> Sắp Mở Săn Mã (2)
          </button>
        </div>

        <div className="text-secondary small fw-semibold d-none d-md-block">
          <i className="bi bi-lightning-charge-fill text-warning"></i> Bấm Lấy Mã để sử dụng ngay
        </div>
      </div>

      {/* 3. LƯỚI 3 THẺ VOUCHER CHUẨN ẢNH 2 */}
      <div className="row g-3 mb-4">
        {/* Voucher 1: Sinh viên & Văn phòng */}
        <div className="col-12 col-md-4">
          <div className="bg-white rounded-3 border border-danger p-3 shadow-sm h-100 d-flex flex-column justify-content-between">
            <div>
              <div className="d-flex justify-content-between align-items-center mb-2">
                <span className="badge bg-danger">HOT</span>
                <span className="text-muted" style={{ fontSize: '11px' }}>HSD: 31/12/2026</span>
              </div>
              <h5 className="fw-bold text-danger mb-1">Giảm 10%</h5>
              <div className="small text-dark fw-semibold mb-1">Áp dụng cho tất cả dòng Laptop Sinh Viên &amp; Văn Phòng</div>
              <div className="text-muted" style={{ fontSize: '11px' }}>ⓘ Đơn từ 10.000.000đ</div>
            </div>

            <div className="d-flex justify-content-between align-items-center pt-3 border-top mt-2">
              <span className="badge bg-light text-dark border font-monospace px-2 py-1">LAPTOP2026</span>
              <button
                type="button"
                className="btn btn-sm btn-success px-3 fw-semibold"
                disabled
              >
                ✓ Đã Trong Ví
              </button>
            </div>
          </div>
        </div>

        {/* Voucher 2: Gaming */}
        <div className="col-12 col-md-4">
          <div className="bg-white rounded-3 border border-danger p-3 shadow-sm h-100 d-flex flex-column justify-content-between">
            <div>
              <div className="d-flex justify-content-between align-items-center mb-2">
                <span className="badge bg-danger">GAMING</span>
                <span className="text-muted" style={{ fontSize: '11px' }}>HSD: 15/11/2026</span>
              </div>
              <h5 className="fw-bold text-danger mb-1">Giảm 500.000đ</h5>
              <div className="small text-dark fw-semibold mb-1">Áp dụng khi mua Laptop Gaming ASUS, Acer, MSI</div>
              <div className="text-muted" style={{ fontSize: '11px' }}>ⓘ Đơn từ 18.000.000đ</div>
            </div>

            <div className="d-flex justify-content-between align-items-center pt-3 border-top mt-2">
              <span className="badge bg-light text-dark border font-monospace px-2 py-1">GAMING500K</span>
              {claimedCodes.includes('GAMING500K') ? (
                <button type="button" className="btn btn-sm btn-success px-3 fw-semibold" disabled>
                  ✓ Đã Trong Ví
                </button>
              ) : (
                <button
                  type="button"
                  className="btn btn-sm btn-outline-danger px-3 fw-semibold"
                  onClick={() => handleClaim('GAMING500K')}
                >
                  Lấy Mã
                </button>
              )}
            </div>
          </div>
        </div>

        {/* Voucher 3: Apple */}
        <div className="col-12 col-md-4">
          <div className="bg-white rounded-3 border border-danger p-3 shadow-sm h-100 d-flex flex-column justify-content-between">
            <div>
              <div className="d-flex justify-content-between align-items-center mb-2">
                <span className="badge bg-danger">APPLE</span>
                <span className="text-muted" style={{ fontSize: '11px' }}>HSD: 30/11/2026</span>
              </div>
              <h5 className="fw-bold text-danger mb-1">Giảm 2.000.000đ</h5>
              <div className="small text-dark fw-semibold mb-1">Trợ giá thu cũ đổi mới hoặc mua thẳng Apple MacBook M2/M3</div>
              <div className="text-muted" style={{ fontSize: '11px' }}>ⓘ Đơn từ 25.000.000đ</div>
            </div>

            <div className="d-flex justify-content-between align-items-center pt-3 border-top mt-2">
              <span className="badge bg-light text-dark border font-monospace px-2 py-1">MACBOOK2M</span>
              {claimedCodes.includes('MACBOOK2M') ? (
                <button type="button" className="btn btn-sm btn-success px-3 fw-semibold" disabled>
                  ✓ Đã Trong Ví
                </button>
              ) : (
                <button
                  type="button"
                  className="btn btn-sm btn-outline-danger px-3 fw-semibold"
                  onClick={() => handleClaim('MACBOOK2M')}
                >
                  Lấy Mã
                </button>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* 4. TẤT CẢ LAPTOP ĐANG GIẢM GIÁ */}
      <div className="mb-4">
        <div className="d-flex justify-content-between align-items-center mb-3">
          <h4 className="fw-bold text-danger m-0 d-flex align-items-center gap-2 fs-5">
            <i className="bi bi-fire"></i>
            <span>TẤT CẢ LAPTOP ĐANG GIẢM GIÁ</span>
          </h4>

          <button
            type="button"
            className="btn btn-outline-danger btn-sm rounded-pill px-3"
            onClick={() => onNavigateToProducts && onNavigateToProducts()}
          >
            Xem tất cả sản phẩm <i className="bi bi-arrow-right ms-1"></i>
          </button>
        </div>

        {/* 5 Card sản phẩm */}
        <div className="row row-cols-2 row-cols-md-3 row-cols-lg-5 g-2">
          {promoProducts.map((p) => (
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
    </div>
  );
}

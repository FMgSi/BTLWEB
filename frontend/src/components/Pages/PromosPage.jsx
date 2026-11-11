import React, { useState, useEffect } from 'react';
import ProductCard from '../Product/ProductCard';
import { formatVND } from '../../utils/formatters';
import './Pages.css';

export default function PromosPage({ products = [], onSelectProduct, onAddToCart, onNavigateToProducts }) {
  const [activeTab, setActiveTab] = useState('available'); // 'available' | 'claimed' | 'all'
  const [promotions, setPromotions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [copiedCode, setCopiedCode] = useState('');

  const [claimedCodes, setClaimedCodes] = useState(() => {
    try {
      const saved = localStorage.getItem('laptopworld_claimed_vouchers');
      return saved ? JSON.parse(saved) : ['LAPTOP2026'];
    } catch {
      return ['LAPTOP2026'];
    }
  });

  useEffect(() => {
    let isMounted = true;
    const fetchPromos = async () => {
      try {
        setLoading(true);
        const res = await fetch('/api/promotions');
        if (res.ok) {
          const data = await res.json();
          if (isMounted && data.success && Array.isArray(data.promotions)) {
            setPromotions(data.promotions);
          }
        }
      } catch (err) {
        console.warn('Lỗi khi tải khuyến mãi từ API:', err);
      } finally {
        if (isMounted) setLoading(false);
      }
    };

    fetchPromos();
    return () => {
      isMounted = false;
    };
  }, []);

  const handleClaim = (code) => {
    const nextClaimed = claimedCodes.includes(code) ? claimedCodes : [...claimedCodes, code];
    setClaimedCodes(nextClaimed);
    try {
      localStorage.setItem('laptopworld_claimed_vouchers', JSON.stringify(nextClaimed));
    } catch { }
  };

  const handleCopy = (code) => {
    handleClaim(code);
    navigator.clipboard.writeText(code).then(() => {
      setCopiedCode(code);
      setTimeout(() => setCopiedCode(''), 2500);
    }).catch(() => {
      prompt('Mã voucher của bạn là:', code);
    });
  };

  const availablePromos = promotions.filter((p) => !claimedCodes.includes(p.code));
  const claimedPromos = promotions.filter((p) => claimedCodes.includes(p.code));
  const displayPromos = activeTab === 'available'
    ? availablePromos
    : (activeTab === 'claimed' ? claimedPromos : promotions);

  const promoProducts = products.filter((p) => p.discountPrice || p.originalPrice).slice(0, 5);

  return (
    <div className="site-container py-3">
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
          ★ TRUNG TÂM MÃ GIẢM GIÁ LAPTOP WORLD
        </span>

        <h1 className="fw-black mb-2" style={{ color: '#fbbf24', fontSize: '26px', letterSpacing: '-0.3px' }}>
          KHUYẾN MÃI ĐANG CÓ &amp; ƯU ĐÃI ĐỘC QUYỀN
        </h1>

        <p className="text-light opacity-90 small mb-0">
          Lưu ngay các mã ưu đãi vào Ví Voucher của bạn để tự động trừ tiền khi thanh toán đơn hàng!
        </p>
      </div>

      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3 pb-2 border-bottom">
        <div className="d-flex gap-2">
          <button
            type="button"
            className={`btn btn-sm ${activeTab === 'available' ? 'btn-danger' : 'btn-light border'} fw-semibold px-3`}
            onClick={() => setActiveTab('available')}
            style={activeTab === 'available' ? { backgroundColor: '#e11b1e' } : {}}
          >
            <i className="bi bi-gift-fill me-1"></i> Mã Có Thể Nhận ({availablePromos.length})
          </button>

          <button
            type="button"
            className={`btn btn-sm ${activeTab === 'claimed' ? 'btn-danger' : 'btn-light border'} fw-semibold px-3`}
            onClick={() => setActiveTab('claimed')}
            style={activeTab === 'claimed' ? { backgroundColor: '#e11b1e' } : {}}
          >
            <i className="bi bi-check-circle-fill me-1"></i> Mã Trong Ví ({claimedPromos.length})
          </button>

          <button
            type="button"
            className={`btn btn-sm ${activeTab === 'all' ? 'btn-danger' : 'btn-light border'} fw-semibold px-3`}
            onClick={() => setActiveTab('all')}
            style={activeTab === 'all' ? { backgroundColor: '#e11b1e' } : {}}
          >
            <i className="bi bi-collection-fill me-1"></i> Tất Cả Ưu Đãi ({promotions.length})
          </button>
        </div>

        <div className="text-secondary small fw-semibold d-none d-md-block">
          <i className="bi bi-lightning-charge-fill text-warning"></i> Bấm "Lấy Mã" để lưu voucher vào ví
        </div>
      </div>

      {loading ? (
        <div className="text-center py-5">
          <div className="spinner-border text-danger" role="status">
            <span className="visually-hidden">Đang tải...</span>
          </div>
          <div className="text-muted small mt-2">Đang tải danh sách voucher từ hệ thống...</div>
        </div>
      ) : displayPromos.length > 0 ? (
        <div className="row g-3 mb-4">
          {displayPromos.map((p) => {
            const isClaimed = claimedCodes.includes(p.code);
            return (
              <div className="col-12 col-md-4" key={p.code}>
                <div className="bg-white rounded-3 border border-danger border-opacity-50 p-3 shadow-sm h-100 d-flex flex-column justify-content-between">
                  <div>
                    <div className="d-flex justify-content-between align-items-center mb-2">
                      <span className="badge bg-danger">{p.scopeBadge}</span>
                      <span className="text-muted" style={{ fontSize: '11px' }}>HSD: {p.endDateFormatted}</span>
                    </div>
                    <h5 className="fw-bold text-danger mb-1">{p.valueBadge}</h5>
                    <div className="small text-dark fw-semibold mb-1">{p.title}</div>
                    {p.description && (
                      <div className="text-muted small mb-2" style={{ fontSize: '11px' }}>{p.description}</div>
                    )}
                    <div className="text-muted" style={{ fontSize: '11px' }}>
                      ⓘ Đơn từ: <strong>{p.minOrderAmount > 0 ? formatVND(p.minOrderAmount) : 'Mọi đơn hàng'}</strong>
                      {p.maxDiscountAmount && (
                        <span> • Tối đa: <strong>{formatVND(p.maxDiscountAmount)}</strong></span>
                      )}
                    </div>
                  </div>

                  <div className="d-flex justify-content-between align-items-center pt-3 border-top mt-3">
                    <span className="badge bg-light text-dark border font-monospace px-2 py-1 fs-6">
                      {p.code}
                    </span>
                    <div className="d-flex gap-1">
                      {isClaimed ? (
                        <button
                          type="button"
                          className="btn btn-sm btn-success px-3 fw-semibold"
                          onClick={() => handleCopy(p.code)}
                          title="Bấm để sao chép mã"
                        >
                          {copiedCode === p.code ? '✓ Đã Chép' : '✓ Trong Ví'}
                        </button>
                      ) : (
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-danger px-3 fw-semibold"
                          onClick={() => handleCopy(p.code)}
                        >
                          {copiedCode === p.code ? '✓ Đã Chép' : 'Lấy Mã'}
                        </button>
                      )}
                      <button
                        type="button"
                        className="btn btn-sm btn-danger px-2 fw-semibold"
                        onClick={() => onNavigateToProducts && onNavigateToProducts()}
                        title="Dùng mã để mua hàng ngay"
                      >
                        Mua ngay
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <div className="bg-light rounded-3 p-5 text-center my-3 border">
          <i className="bi bi-ticket-perforated fs-1 text-muted d-block mb-2"></i>
          <h6 className="text-dark fw-bold">Chưa có voucher trong danh mục này</h6>
          <p className="text-muted small mb-3">Vui lòng kiểm tra các tab ưu đãi khác để nhận mã giảm giá!</p>
          <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => setActiveTab('all')}>
            Xem tất cả voucher
          </button>
        </div>
      )}

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

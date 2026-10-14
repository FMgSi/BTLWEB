import React, { useState } from 'react';
import './CartPage.css';

export default function CartPage({
  cartItems = [],
  onUpdateQuantity,
  onRemoveItem,
  onClearCart,
  onToggleCheck,
  onToggleAll,
  onGoHome,
  onSelectProduct,
  currentUser,
}) {
  const [showCheckoutModal, setShowCheckoutModal] = useState(false);
  const [orderSuccess, setOrderSuccess] = useState(null);

  // Form đặt hàng
  const [customerName, setCustomerName] = useState(currentUser?.fullName || currentUser?.username || '');
  const [customerPhone, setCustomerPhone] = useState('');
  const [customerAddress, setCustomerAddress] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('cod'); // 'cod' hoặc 'banking'
  const [orderNotes, setOrderNotes] = useState('');

  // Định dạng tiền tệ VNĐ
  const formatVND = (num) => {
    if (!num) return '0đ';
    return Number(num).toLocaleString('vi-VN') + 'đ';
  };

  // Các sản phẩm được chọn để thanh toán (mặc định nếu chưa set checked thì xem như được chọn)
  const checkedItems = cartItems.filter((i) => i.checked !== false);
  const isAllChecked = cartItems.length > 0 && checkedItems.length === cartItems.length;

  // Tính toán tổng tiền
  const totalPayable = checkedItems.reduce((sum, item) => sum + (item.price || 0) * (item.quantity || 1), 0);
  const totalOriginal = checkedItems.reduce((sum, item) => {
    const orig = item.originalPrice && item.originalPrice > item.price ? item.originalPrice : item.price;
    return sum + (orig || 0) * (item.quantity || 1);
  }, 0);
  const totalDiscount = Math.max(0, totalOriginal - totalPayable);
  // Điểm thưởng đã được loại bỏ theo yêu cầu

  // Xử lý gửi đơn hàng
  const handleConfirmOrder = (e) => {
    e.preventDefault();
    if (!customerName.trim() || !customerPhone.trim() || !customerAddress.trim()) {
      alert('Vui lòng điền đầy đủ Họ tên, Số điện thoại và Địa chỉ giao hàng!');
      return;
    }

    const orderId = 'PC' + Math.floor(100000 + Math.random() * 900000);
    setOrderSuccess({
      orderId,
      name: customerName,
      phone: customerPhone,
      address: customerAddress,
      total: totalPayable,
      items: [...checkedItems],
      paymentMethod: paymentMethod === 'cod' ? 'Thanh toán khi nhận hàng (COD)' : 'Chuyển khoản QR Ngân hàng',
    });

    // Xóa các sản phẩm đã đặt khỏi giỏ
    checkedItems.forEach((item) => {
      onRemoveItem(item.cartItemId || item.id);
    });

    setShowCheckoutModal(false);
  };

  return (
    <div className="cart-page-wrap py-4">
      <div className="site-container">
        {/* Breadcrumb theo mẫu FPT Shop */}
        <div className="cart-breadcrumb">
          <a
            href="/"
            onClick={(e) => {
              e.preventDefault();
              onGoHome();
            }}
          >
            Trang chủ
          </a>
          <span>/</span>
          <span className="text-dark fw-semibold">Giỏ hàng</span>
        </div>

        {/* =========================================================================
            THÔNG BÁO ĐẶT HÀNG THÀNH CÔNG NẾU VỪA XÁC NHẬN ĐƠN
        ========================================================================= */}
        {orderSuccess ? (
          <div className="bg-white rounded-3 border p-5 text-center shadow-sm my-3 mx-auto" style={{ maxWidth: '640px' }}>
            <div className="rounded-circle bg-success bg-opacity-10 text-success d-inline-flex align-items-center justify-content-center mb-3" style={{ width: '64px', height: '64px' }}>
              <i className="bi bi-check-lg fs-1"></i>
            </div>
            <h4 className="fw-bold text-dark mb-2">Đặt hàng thành công!</h4>
            <p className="text-muted small mb-3">
              Cảm ơn quý khách <strong>{orderSuccess.name}</strong> đã mua sắm tại <strong>PCStore</strong>.
              <br />
              Mã đơn hàng của bạn là: <span className="text-danger fw-bold fs-6">#{orderSuccess.orderId}</span>
            </p>
            <div className="bg-light p-3 rounded-3 text-start small mb-4">
              <div><strong>Giao đến:</strong> {orderSuccess.address}</div>
              <div><strong>Số điện thoại:</strong> {orderSuccess.phone}</div>
              <div><strong>Hình thức:</strong> {orderSuccess.paymentMethod}</div>
              <div className="mt-2 pt-2 border-top text-danger fw-bold fs-6">
                Tổng thanh toán: {formatVND(orderSuccess.total)}
              </div>
            </div>
            <button
              type="button"
              className="btn btn-danger px-4 py-2 fw-semibold"
              onClick={() => {
                setOrderSuccess(null);
                onGoHome();
              }}
            >
              Tiếp tục mua sắm
            </button>
          </div>
        ) : cartItems.length === 0 ? (
          /* =========================================================================
              TRƯỜNG HỢP GIỎ HÀNG TRỐNG
          ========================================================================= */
          <div className="cart-empty-box my-3">
            <div className="cart-empty-icon">
              <i className="bi bi-cart-x"></i>
            </div>
            <h5 className="fw-bold text-dark mb-2">Giỏ hàng của bạn đang trống</h5>
            <p className="text-muted small mb-3">Hãy dạo một vòng cửa hàng để chọn được chiếc laptop ưng ý nhất nhé!</p>
            <button
              type="button"
              className="cart-empty-btn"
              onClick={onGoHome}
            >
              <i className="bi bi-arrow-left"></i> Tiếp tục mua sắm
            </button>
          </div>
        ) : (
          /* =========================================================================
              GIAO DIỆN GIỎ HÀNG CHUẨN FPT SHOP (ẢNH 1 ĐÍNH KÈM)
          ========================================================================= */
          <div className="row g-4 align-items-start">
            {/* CỘT TRÁI: Danh sách sản phẩm trong giỏ */}
            <div className="col-12 col-lg-8">
              <div className="cart-items-card">
                {/* Thanh tiêu đề: Chọn tất cả & Nút xóa tất cả */}
                <div className="cart-header-row">
                  <label className="d-flex align-items-center gap-2 m-0" style={{ cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      className="cart-checkbox"
                      checked={isAllChecked}
                      onChange={(e) => onToggleAll(e.target.checked)}
                    />
                    <span>Chọn tất cả ({cartItems.length})</span>
                  </label>

                  <button
                    type="button"
                    className="btn btn-sm btn-link text-muted text-decoration-none p-0 d-flex align-items-center gap-1"
                    onClick={() => {
                      if (window.confirm('Bạn có chắc chắn muốn xóa tất cả sản phẩm khỏi giỏ hàng?')) {
                        onClearCart();
                      }
                    }}
                    title="Xóa toàn bộ giỏ hàng"
                  >
                    <i className="bi bi-trash3"></i>
                    <span>Xóa tất cả</span>
                  </button>
                </div>

                {/* Danh sách từng sản phẩm */}
                {cartItems.map((item) => {
                  const itemKey = item.cartItemId || item.id;
                  const isChecked = item.checked !== false;

                  return (
                    <div key={itemKey} className="cart-item-row">
                      {/* Checkbox chọn sản phẩm */}
                      <input
                        type="checkbox"
                        className="cart-checkbox"
                        checked={isChecked}
                        onChange={() => onToggleCheck(itemKey)}
                      />

                      {/* Ảnh sản phẩm */}
                      <div className="cart-item-thumb">
                        <img
                          src={item.image}
                          alt={item.name}
                          onError={(e) => {
                            e.target.onerror = null;
                            e.target.src = 'https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=200&q=80';
                          }}
                        />
                      </div>

                      {/* Thông tin sản phẩm */}
                      <div className="cart-item-info">
                        <a
                          href={`/product/${item.id}`}
                          className="cart-item-title"
                          onClick={(e) => {
                            e.preventDefault();
                            if (onSelectProduct) onSelectProduct(item.id);
                          }}
                        >
                          {item.name}
                        </a>

                        {/* Tùy chọn cấu hình RAM & Ổ cứng hiển thị chi tiết */}
                        <div className="cart-item-spec-tag">
                          <span>RAM: <strong className="text-dark">{item.selectedRam || '16 GB'}</strong></span>
                          <span className="mx-2 text-muted">•</span>
                          <span>Ổ cứng: <strong className="text-dark">{item.selectedStorage || '512 GB SSD'}</strong></span>
                        </div>

                        {/* Banner Mua kèm giá sốc giống FPT Shop */}
                        <div className="d-block mt-1">
                          <span className="cart-promo-tag">
                            <i className="bi bi-lightning-charge-fill text-danger"></i>
                            Giá sốc: Mua kèm với mức giá ưu đãi
                          </span>
                        </div>
                      </div>

                      {/* Giá tiền & Nút tăng giảm số lượng */}
                      <div className="cart-item-actions">
                        <div className="text-end">
                          <div className="cart-price-current">{formatVND(item.price)}</div>
                          {item.originalPrice && item.originalPrice > item.price && (
                            <div className="cart-price-original">{formatVND(item.originalPrice)}</div>
                          )}
                        </div>

                        <div className="d-flex align-items-center gap-2">
                          {/* Bộ tăng giảm số lượng [-] [1] [+] */}
                          <div className="cart-qty-ctrl">
                            <button
                              type="button"
                              className="cart-qty-btn"
                              onClick={() => onUpdateQuantity(itemKey, -1)}
                              title="Giảm số lượng"
                            >
                              <i className="bi bi-dash"></i>
                            </button>
                            <span className="cart-qty-val">{item.quantity || 1}</span>
                            <button
                              type="button"
                              className="cart-qty-btn"
                              onClick={() => onUpdateQuantity(itemKey, 1)}
                              title="Tăng số lượng"
                            >
                              <i className="bi bi-plus"></i>
                            </button>
                          </div>

                          {/* Nút xóa sản phẩm */}
                          <button
                            type="button"
                            className="cart-remove-btn"
                            onClick={() => onRemoveItem(itemKey)}
                            title="Xóa sản phẩm này"
                          >
                            <i className="bi bi-trash3"></i>
                          </button>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Nút quay lại mua thêm */}
              <div className="mt-3">
                <button
                  type="button"
                  className="btn btn-outline-secondary btn-sm d-inline-flex align-items-center gap-2"
                  onClick={onGoHome}
                >
                  <i className="bi bi-arrow-left"></i>
                  <span>Chọn thêm sản phẩm khác</span>
                </button>
              </div>
            </div>

            {/* CỘT PHẢI: Thông tin đơn hàng (FPT Shop Order Summary) */}
            <div className="col-12 col-lg-4">
              <div className="cart-summary-card">
                <h6 className="cart-summary-title">Thông tin đơn hàng</h6>

                <div className="cart-summary-line">
                  <span>Tổng tiền</span>
                  <span className="fw-semibold text-dark">{formatVND(totalOriginal)}</span>
                </div>

                <div className="cart-summary-line">
                  <span>Tổng khuyến mãi</span>
                  <span className="text-danger fw-semibold">-{formatVND(totalDiscount)}</span>
                </div>
                <div className="cart-summary-subline">
                  <span>• Giảm giá sản phẩm</span>
                  <span>{formatVND(totalDiscount)}</span>
                </div>
                <div className="cart-summary-subline">
                  <span>• Voucher</span>
                  <span>0đ</span>
                </div>

                <div className="cart-summary-total">
                  <span className="cart-summary-total-label">Cần thanh toán</span>
                  <span className="cart-summary-total-price">{formatVND(totalPayable)}</span>
                </div>



                {/* Nút Xác nhận đơn chuẩn mẫu FPT Shop */}
                <button
                  type="button"
                  className="cart-checkout-btn"
                  disabled={checkedItems.length === 0}
                  onClick={() => setShowCheckoutModal(true)}
                >
                  Xác nhận đơn ({checkedItems.length})
                </button>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* =========================================================================
          MODAL XÁC NHẬN ĐƠN HÀNG & THÔNG TIN GIAO HÀNG
      ========================================================================= */}
      {showCheckoutModal && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1060 }}>
          <div className="modal-dialog modal-dialog-centered" style={{ maxWidth: '480px' }}>
            <div className="modal-content border-0 shadow-lg rounded-4 p-3">
              <div className="modal-header border-0 pb-2">
                <h5 className="modal-title fw-bold text-dark">Xác nhận đặt hàng</h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setShowCheckoutModal(false)}
                ></button>
              </div>

              <form onSubmit={handleConfirmOrder}>
                <div className="modal-body py-2">
                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-secondary">
                      Họ và tên người nhận <span className="text-danger">*</span>
                    </label>
                    <input
                      type="text"
                      className="form-control"
                      placeholder="Nguyễn Văn A"
                      required
                      value={customerName}
                      onChange={(e) => setCustomerName(e.target.value)}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-secondary">
                      Số điện thoại nhận hàng <span className="text-danger">*</span>
                    </label>
                    <input
                      type="tel"
                      className="form-control"
                      placeholder="0987 654 321"
                      required
                      value={customerPhone}
                      onChange={(e) => setCustomerPhone(e.target.value)}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-secondary">
                      Địa chỉ nhận hàng chi tiết <span className="text-danger">*</span>
                    </label>
                    <input
                      type="text"
                      className="form-control"
                      placeholder="Số nhà, đường, phường/xã, quận/huyện..."
                      required
                      value={customerAddress}
                      onChange={(e) => setCustomerAddress(e.target.value)}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-secondary">Ghi chú giao hàng</label>
                    <textarea
                      className="form-control"
                      rows="2"
                      placeholder="Giao giờ hành chính, gọi trước khi đến..."
                      value={orderNotes}
                      onChange={(e) => setOrderNotes(e.target.value)}
                    ></textarea>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-semibold text-secondary d-block">
                      Phương thức thanh toán
                    </label>
                    <div className="form-check mb-2">
                      <input
                        className="form-check-input"
                        type="radio"
                        name="paymentMethod"
                        id="payCod"
                        checked={paymentMethod === 'cod'}
                        onChange={() => setPaymentMethod('cod')}
                      />
                      <label className="form-check-label small" htmlFor="payCod">
                        💵 Thanh toán tiền mặt khi nhận hàng (COD)
                      </label>
                    </div>
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="radio"
                        name="paymentMethod"
                        id="payBanking"
                        checked={paymentMethod === 'banking'}
                        onChange={() => setPaymentMethod('banking')}
                      />
                      <label className="form-check-label small" htmlFor="payBanking">
                        💳 Chuyển khoản ngân hàng (Quét mã VietQR)
                      </label>
                    </div>
                  </div>

                  <div className="p-3 bg-light rounded-3 d-flex justify-content-between align-items-center">
                    <span className="small fw-semibold text-secondary">Tổng thanh toán:</span>
                    <span className="fs-5 fw-bold text-danger">{formatVND(totalPayable)}</span>
                  </div>
                </div>

                <div className="modal-footer border-0 pt-2">
                  <button
                    type="button"
                    className="btn btn-light"
                    onClick={() => setShowCheckoutModal(false)}
                  >
                    Hủy bỏ
                  </button>
                  <button
                    type="submit"
                    className="btn btn-danger px-4 fw-bold"
                    style={{ backgroundColor: '#e11b1e' }}
                  >
                    Hoàn tất đặt hàng
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

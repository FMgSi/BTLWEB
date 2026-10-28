import React, { useState } from 'react';
import './CartPage.css';

// Danh sách ngân hàng phổ biến tại Việt Nam hỗ trợ tạo mã VietQR
const POPULAR_BANKS = [
  { id: 'MB', name: 'MBBank (Ngân hàng Quân Đội)' },
  { id: 'VCB', name: 'Vietcombank (Ngoại Thương)' },
  { id: 'TCB', name: 'Techcombank (Kỹ Thương)' },
  { id: 'VPB', name: 'VPBank (Việt Nam Thịnh Vượng)' },
  { id: 'ACB', name: 'ACB (Á Châu)' },
  { id: 'TPB', name: 'TPBank (Tiên Phong)' },
  { id: 'BIDV', name: 'BIDV (Đầu tư và Phát triển)' },
  { id: 'ICB', name: 'VietinBank (Công Thương)' },
  { id: 'VBA', name: 'Agribank (Nông Nghiệp)' },
];

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
  const [showQrModal, setShowQrModal] = useState(false);
  const [orderSuccess, setOrderSuccess] = useState(null);
  const [pendingOrder, setPendingOrder] = useState(null);
  const [copiedKey, setCopiedKey] = useState('');

  // Form đặt hàng
  const [customerName, setCustomerName] = useState(currentUser?.fullName || currentUser?.username || '');
  const [customerPhone, setCustomerPhone] = useState('');
  const [customerAddress, setCustomerAddress] = useState('');
  const [orderNotes, setOrderNotes] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('banking');

  // Thông tin tài khoản nhận tiền (cho phép tùy chỉnh tài khoản nhận của bạn)
  const [bankAccount, setBankAccount] = useState(() => {
    try {
      const saved = localStorage.getItem('pcstore_bank_account');
      return saved
        ? JSON.parse(saved)
        : {
            bankId: 'MB',
            bankName: 'MBBank (Ngân hàng Quân Đội)',
            accountNo: '0987654321',
            accountName: 'NGUYEN VAN A',
          };
    } catch (e) {
      return {
        bankId: 'MB',
        bankName: 'MBBank (Ngân hàng Quân Đội)',
        accountNo: '0987654321',
        accountName: 'NGUYEN VAN A',
      };
    }
  });
  const [showBankEdit, setShowBankEdit] = useState(false);

  // Định dạng số tiền sang chuẩn Việt Nam Đồng (VND)
  const formatVND = (num) => (num ? num.toLocaleString('vi-VN') + 'đ' : '0đ');

  // Các sản phẩm được chọn để thanh toán
  const checkedItems = cartItems.filter((i) => i.checked !== false);
  const isAllChecked = cartItems.length > 0 && checkedItems.length === cartItems.length;

  // Tính tổng tiền giỏ hàng
  const totalPayable = checkedItems.reduce((sum, item) => sum + (item.price || 0) * (item.quantity || 1), 0);
  const totalOriginal = checkedItems.reduce((sum, item) => {
    const orig = item.originalPrice && item.originalPrice > item.price ? item.originalPrice : item.price;
    return sum + (orig || 0) * (item.quantity || 1);
  }, 0);
  const totalDiscount = Math.max(0, totalOriginal - totalPayable);

  // Sao chép văn bản vào bộ nhớ tạm
  const handleCopy = (text, key) => {
    navigator.clipboard.writeText(text);
    setCopiedKey(key);
    setTimeout(() => setCopiedKey(''), 2000);
  };

  // Xử lý khi nhấn nút Hoàn tất đặt hàng - Kết nối Backend API
  const handleConfirmOrder = async (e) => {
    e.preventDefault();
    if (!customerName.trim() || !customerPhone.trim() || !customerAddress.trim()) {
      alert('Vui lòng điền đầy đủ Họ tên, Số điện thoại và Địa chỉ nhận hàng!');
      return;
    }

    try {
      // 1. Tạo payload gửi lên Backend ASP.NET Core
      const orderPayload = {
        customerName: customerName.trim(),
        phoneNumber: customerPhone.trim(),
        email: currentUser?.email || '',
        shippingAddress: customerAddress.trim(),
        notes: orderNotes.trim(),
        paymentMethod: paymentMethod === 'cod' ? 'COD' : 'VietQR',
        items: checkedItems.map((item) => ({
          productId: item.productId || item.id,
          quantity: item.quantity || 1,
          selectedSpecs: item.specs || (item.ram ? `RAM: ${item.ram} • ${item.storage}` : 'Cấu hình tiêu chuẩn'),
        })),
      };

      const res = await fetch('/api/orders', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(orderPayload),
      });

      const data = await res.json();

      if (!res.ok || !data.success) {
        alert(data.message || 'Đặt hàng thất bại. Vui lòng kiểm tra lại tồn kho sản phẩm!');
        return;
      }

      const realOrderCode = data.orderCode || data.order?.orderCode || ('PC' + Math.floor(100000 + Math.random() * 900000));

      const orderData = {
        orderId: realOrderCode,
        name: customerName,
        phone: customerPhone,
        address: customerAddress,
        notes: orderNotes,
        total: totalPayable,
        items: [...checkedItems],
        paymentMethod: paymentMethod === 'cod' ? 'Thanh toán tiền mặt khi nhận hàng (COD)' : `Chuyển khoản VietQR (${bankAccount.bankId})`,
      };

      setShowCheckoutModal(false);

      // Nếu chọn chuyển khoản ngân hàng -> Mở modal quét mã VietQR kèm mã đơn thật
      if (paymentMethod === 'banking') {
        setPendingOrder(orderData);
        setShowQrModal(true);
      } else {
        // Nếu chọn COD -> Hoàn tất đặt hàng trực tiếp và xóa các món đã mua khỏi giỏ
        checkedItems.forEach((item) => {
          onRemoveItem(item.cartItemId || item.id);
        });
        setOrderSuccess(orderData);
      }
    } catch (err) {
      console.warn('Lỗi gọi API /api/orders:', err);
      alert('Không thể kết nối đến máy chủ để tạo đơn hàng. Vui lòng thử lại sau!');
    }
  };

  // Xác nhận đã thanh toán xong từ màn hình VietQR
  const handleConfirmPaidQr = () => {
    if (pendingOrder) {
      checkedItems.forEach((item) => {
        onRemoveItem(item.cartItemId || item.id);
      });
      setOrderSuccess(pendingOrder);
      setPendingOrder(null);
    }
    setShowQrModal(false);
  };

  // Lưu thông tin tài khoản ngân hàng của bạn vào bộ nhớ trình duyệt
  const handleSaveBankAccount = (e) => {
    e.preventDefault();
    try {
      localStorage.setItem('pcstore_bank_account', JSON.stringify(bankAccount));
    } catch (err) {}
    setShowBankEdit(false);
  };

  // Đường link tạo mã QR VietQR tự động theo số tiền và mã đơn hàng
  const qrImageUrl = pendingOrder
    ? `https://img.vietqr.io/image/${bankAccount.bankId}-${bankAccount.accountNo}-compact2.png?amount=${pendingOrder.total}&addInfo=${pendingOrder.orderId}&accountName=${encodeURIComponent(bankAccount.accountName)}`
    : '';

  return (
    <div className="cart-page-wrap py-4">
      <div className="site-container">
        {/* Đường dẫn Breadcrumb */}
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

        {/* Thông báo đặt hàng thành công */}
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
          /* Trạng thái giỏ hàng trống */
          <div className="cart-empty-box my-4">
            <i className="bi bi-cart-x cart-empty-icon"></i>
            <h5 className="fw-bold text-dark mb-2">Giỏ hàng của bạn đang trống</h5>
            <p className="text-muted small mb-3">Hãy chọn thêm sản phẩm laptop để nhận nhiều ưu đãi hấp dẫn!</p>
            <button
              type="button"
              className="cart-empty-btn"
              onClick={() => onGoHome()}
            >
              <i className="bi bi-arrow-left"></i> Khám phá sản phẩm ngay
            </button>
          </div>
        ) : (
          /* Giao diện danh sách sản phẩm trong giỏ hàng */
          <div className="row g-4">
            <div className="col-12 col-lg-8">
              <div className="cart-items-card">
                <div className="cart-header-row">
                  <label className="cart-check-all">
                    <input
                      type="checkbox"
                      className="cart-checkbox"
                      checked={isAllChecked}
                      onChange={(e) => onToggleAll(e.target.checked)}
                    />
                    <span>Chọn tất cả ({cartItems.length} sản phẩm)</span>
                  </label>

                  <button
                    type="button"
                    className="cart-clear-btn"
                    onClick={() => {
                      if (window.confirm('Bạn có chắc muốn xóa tất cả sản phẩm trong giỏ hàng?')) {
                        onClearCart();
                      }
                    }}
                  >
                    <i className="bi bi-trash3 me-1"></i> Xóa tất cả
                  </button>
                </div>

                <div className="cart-items-list">
                  {cartItems.map((item) => {
                    const itemTotal = (item.price || 0) * (item.quantity || 1);
                    const isChecked = item.checked !== false;

                    return (
                      <div className="cart-item-row" key={item.cartItemId || item.id}>
                        <input
                          type="checkbox"
                          className="cart-checkbox"
                          checked={isChecked}
                          onChange={() => onToggleCheck(item.cartItemId || item.id)}
                        />

                        <div
                          className="cart-item-thumb"
                          onClick={() => onSelectProduct && onSelectProduct(item.id)}
                          title="Xem chi tiết sản phẩm"
                        >
                          <img
                            src={item.image || '/images/default.png'}
                            alt={item.name}
                          />
                        </div>

                        <div className="cart-item-info">
                          <div
                            className="cart-item-title"
                            onClick={() => onSelectProduct && onSelectProduct(item.id)}
                            title={item.name}
                          >
                            {item.name}
                          </div>

                          {(item.selectedRam || item.selectedStorage) && (
                            <div className="cart-item-spec-tag">
                              <i className="bi bi-cpu me-1"></i>
                              {item.selectedRam && <span>RAM: {item.selectedRam}</span>}
                              {item.selectedRam && item.selectedStorage && <span className="mx-1">•</span>}
                              {item.selectedStorage && <span>Ổ cứng: {item.selectedStorage}</span>}
                            </div>
                          )}

                          <div className="cart-item-price-row">
                            <span className="cart-price-current">{formatVND(item.price)}</span>
                            {item.originalPrice && item.originalPrice > item.price && (
                              <span className="cart-price-original">{formatVND(item.originalPrice)}</span>
                            )}
                          </div>
                        </div>

                        <div className="cart-item-actions">
                          <div className="cart-qty-ctrl">
                            <button
                              type="button"
                              className="cart-qty-btn"
                              disabled={(item.quantity || 1) <= 1}
                              onClick={() => onUpdateQuantity(item.cartItemId || item.id, -1)}
                            >
                              -
                            </button>
                            <span className="cart-qty-val">{item.quantity || 1}</span>
                            <button
                              type="button"
                              className="cart-qty-btn"
                              onClick={() => onUpdateQuantity(item.cartItemId || item.id, 1)}
                            >
                              +
                            </button>
                          </div>

                          <div className="cart-item-total">{formatVND(itemTotal)}</div>

                          <button
                            type="button"
                            className="cart-remove-btn"
                            title="Xóa sản phẩm"
                            onClick={() => onRemoveItem(item.cartItemId || item.id)}
                          >
                            <i className="bi bi-trash3"></i>
                          </button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>

            {/* Cột thông tin đơn hàng bên phải */}
            <div className="col-12 col-lg-4">
              <div className="cart-summary-card">
                <h6 className="cart-summary-title">Thông tin đơn hàng</h6>

                <div className="cart-summary-line">
                  <span>Tổng tiền hàng</span>
                  <span className="fw-semibold text-dark">{formatVND(totalOriginal)}</span>
                </div>

                <div className="cart-summary-line">
                  <span>Tổng khuyến mãi</span>
                  <span className="text-danger fw-semibold">-{formatVND(totalDiscount)}</span>
                </div>

                <div className="cart-summary-total">
                  <span className="cart-summary-total-label">Cần thanh toán</span>
                  <span className="cart-summary-total-price">{formatVND(totalPayable)}</span>
                </div>

                <button
                  type="button"
                  className="cart-checkout-btn"
                  disabled={checkedItems.length === 0}
                  onClick={() => setShowCheckoutModal(true)}
                >
                  <i className="bi bi-shield-check me-1"></i> Xác nhận đơn ({checkedItems.length})
                </button>

                <div className="cart-reassurance-box">
                  <div className="cart-reassurance-item">
                    <i className="bi bi-shield-fill-check"></i>
                    <span>Bảo hành chính hãng 12 - 24 tháng</span>
                  </div>
                  <div className="cart-reassurance-item">
                    <i className="bi bi-truck"></i>
                    <span>Giao hàng hỏa tốc toàn quốc</span>
                  </div>
                  <div className="cart-reassurance-item">
                    <i className="bi bi-arrow-repeat"></i>
                    <span>Lỗi 1 đổi 1 trong vòng 30 ngày</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* 1. MODAL XÁC NHẬN ĐẶT HÀNG & THÔNG TIN GIAO HÀNG */}
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

      {/* 2. MODAL THANH TOÁN QUÉT MÃ VIETQR ĐẾN TÀI KHOẢN NGÂN HÀNG CỦA BẠN */}
      {showQrModal && pendingOrder && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.6)', zIndex: 1070 }}>
          <div className="modal-dialog modal-dialog-centered" style={{ maxWidth: '500px' }}>
            <div className="modal-content border-0 shadow-lg rounded-4 overflow-hidden">
              {/* Header Modal */}
              <div className="modal-header bg-danger text-white border-0 py-3">
                <div>
                  <h5 className="modal-title fw-bold fs-6 m-0 d-flex align-items-center gap-2">
                    <i className="bi bi-qr-code-scan"></i> Quét mã VietQR để thanh toán
                  </h5>
                  <div className="small opacity-90" style={{ fontSize: '11px' }}>
                    Chuyển khoản nhanh 24/7 qua Napas & Ngân hàng
                  </div>
                </div>
                <button
                  type="button"
                  className="btn-close btn-close-white"
                  onClick={() => setShowQrModal(false)}
                ></button>
              </div>

              <div className="modal-body p-4 text-center">
                {/* Ảnh Mã QR tạo tự động */}
                <div className="bg-white p-2 rounded-3 border d-inline-block shadow-sm mb-3">
                  <img
                    src={qrImageUrl}
                    alt="Mã QR Chuyển khoản"
                    className="img-fluid"
                    style={{ width: '240px', height: '240px', objectFit: 'contain' }}
                  />
                </div>

                <p className="text-secondary small mb-3">
                  Mở ứng dụng ngân hàng bất kỳ (Vietcombank, MB, Techcombank, MoMo...) và chọn <strong>Quét mã QR</strong> để thanh toán.
                </p>

                {/* Bảng chi tiết thông tin chuyển khoản kèm nút sao chép */}
                <div className="bg-light p-3 rounded-3 text-start small border mb-3">
                  <div className="d-flex justify-content-between align-items-center py-1 border-bottom">
                    <span className="text-muted">Ngân hàng:</span>
                    <strong className="text-dark">{bankAccount.bankName}</strong>
                  </div>

                  <div className="d-flex justify-content-between align-items-center py-1 border-bottom">
                    <span className="text-muted">Số tài khoản:</span>
                    <div className="d-flex align-items-center gap-2">
                      <strong className="text-danger fs-6">{bankAccount.accountNo}</strong>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary py-0 px-2"
                        style={{ fontSize: '11px' }}
                        onClick={() => handleCopy(bankAccount.accountNo, 'acc')}
                      >
                        {copiedKey === 'acc' ? '✓ Đã chép' : 'Sao chép'}
                      </button>
                    </div>
                  </div>

                  <div className="d-flex justify-content-between align-items-center py-1 border-bottom">
                    <span className="text-muted">Chủ tài khoản:</span>
                    <strong className="text-dark">{bankAccount.accountName}</strong>
                  </div>

                  <div className="d-flex justify-content-between align-items-center py-1 border-bottom">
                    <span className="text-muted">Số tiền:</span>
                    <div className="d-flex align-items-center gap-2">
                      <strong className="text-danger">{formatVND(pendingOrder.total)}</strong>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary py-0 px-2"
                        style={{ fontSize: '11px' }}
                        onClick={() => handleCopy(pendingOrder.total.toString(), 'amount')}
                      >
                        {copiedKey === 'amount' ? '✓ Đã chép' : 'Sao chép'}
                      </button>
                    </div>
                  </div>

                  <div className="d-flex justify-content-between align-items-center py-1">
                    <span className="text-muted">Nội dung CK:</span>
                    <div className="d-flex align-items-center gap-2">
                      <strong className="text-primary">{pendingOrder.orderId}</strong>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary py-0 px-2"
                        style={{ fontSize: '11px' }}
                        onClick={() => handleCopy(pendingOrder.orderId, 'msg')}
                      >
                        {copiedKey === 'msg' ? '✓ Đã chép' : 'Sao chép'}
                      </button>
                    </div>
                  </div>
                </div>

                {/* Tùy chọn thay đổi thông tin STK của bạn */}
                <div className="text-start mb-3">
                  <button
                    type="button"
                    className="btn btn-link btn-sm text-decoration-none p-0 text-secondary"
                    style={{ fontSize: '12px' }}
                    onClick={() => setShowBankEdit(!showBankEdit)}
                  >
                    <i className="bi bi-gear me-1"></i>
                    {showBankEdit ? 'Ẩn cài đặt tài khoản nhận' : 'Thay đổi số tài khoản nhận tiền của bạn'}
                  </button>

                  {showBankEdit && (
                    <form onSubmit={handleSaveBankAccount} className="bg-white p-3 rounded-3 border mt-2">
                      <div className="mb-2">
                        <label className="form-label text-muted" style={{ fontSize: '11px' }}>Chọn Ngân hàng:</label>
                        <select
                          className="form-select form-select-sm"
                          value={bankAccount.bankId}
                          onChange={(e) => {
                            const b = POPULAR_BANKS.find((item) => item.id === e.target.value);
                            setBankAccount({
                              ...bankAccount,
                              bankId: e.target.value,
                              bankName: b ? b.name : e.target.value,
                            });
                          }}
                        >
                          {POPULAR_BANKS.map((b) => (
                            <option key={b.id} value={b.id}>
                              {b.name}
                            </option>
                          ))}
                        </select>
                      </div>

                      <div className="mb-2">
                        <label className="form-label text-muted" style={{ fontSize: '11px' }}>Số tài khoản nhận tiền:</label>
                        <input
                          type="text"
                          className="form-control form-control-sm"
                          value={bankAccount.accountNo}
                          onChange={(e) => setBankAccount({ ...bankAccount, accountNo: e.target.value })}
                          required
                        />
                      </div>

                      <div className="mb-2">
                        <label className="form-label text-muted" style={{ fontSize: '11px' }}>Tên chủ tài khoản (IN HOA KHÔNG DẤU):</label>
                        <input
                          type="text"
                          className="form-control form-control-sm"
                          value={bankAccount.accountName}
                          onChange={(e) => setBankAccount({ ...bankAccount, accountName: e.target.value.toUpperCase() })}
                          required
                        />
                      </div>

                      <button type="submit" className="btn btn-sm btn-dark w-100 mt-1">
                        Lưu thông tin tài khoản
                      </button>
                    </form>
                  )}
                </div>

                {/* Nút hoàn tất xác nhận thanh toán */}
                <div className="d-flex gap-2">
                  <button
                    type="button"
                    className="btn btn-light w-50"
                    onClick={() => setShowQrModal(false)}
                  >
                    Đóng
                  </button>
                  <button
                    type="button"
                    className="btn btn-danger w-50 fw-bold"
                    style={{ backgroundColor: '#e11b1e' }}
                    onClick={handleConfirmPaidQr}
                  >
                    <i className="bi bi-check-circle me-1"></i> Tôi đã chuyển khoản
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

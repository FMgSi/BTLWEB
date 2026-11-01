import { useState } from 'react';
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
  const [showQrModal, setShowQrModal] = useState(false);
  const [orderSuccess, setOrderSuccess] = useState(null);
  const [pendingOrder, setPendingOrder] = useState(null);
  const [copiedKey, setCopiedKey] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [phoneError, setPhoneError] = useState('');
  const [idempotencyKey, setIdempotencyKey] = useState('');

  // Form đặt hàng
  const [customerName, setCustomerName] = useState(currentUser?.fullName || currentUser?.username || '');
  const [customerPhone, setCustomerPhone] = useState('');
  const [customerAddress, setCustomerAddress] = useState('');
  const [orderNotes, setOrderNotes] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('banking');
  const [purchaseMode, setPurchaseMode] = useState('Full');
  const [termMonths, setTermMonths] = useState(6);

  const handleOpenCheckoutModal = () => {
    setIdempotencyKey(`IDEMP-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`);
    setPhoneError('');
    setShowCheckoutModal(true);
  };

  // Thông tin tài khoản ngân hàng nhận tiền chính thức của Cửa hàng
  const STORE_BANK_ACCOUNT = {
    bankId: 'MB',
    bankName: 'MBBank (Ngân hàng Quân Đội)',
    accountNo: '0988668866',
    accountName: 'HE THONG LAPTOP WORLD',
  };

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

  // Tính toán kế hoạch trả góp: trả trước đúng 30% làm tròn đến đồng, dồn lẻ kỳ cuối
  const prepayAmount = Math.round(totalPayable * 0.30);
  const financedAmount = totalPayable - prepayAmount;
  const basePeriodAmount = Math.floor(financedAmount / termMonths);
  const lastPeriodAmount = financedAmount - (basePeriodAmount * (termMonths - 1));

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

    const cleanPhone = customerPhone.replace(/[\s\-\.]/g, '');
    const vnPhoneRegex = /^(0|\+84)(3|5|7|8|9)[0-9]{8}$/;
    if (!vnPhoneRegex.test(cleanPhone)) {
      setPhoneError('Số điện thoại không đúng định dạng (cần đúng 10 số, bắt đầu bằng 03, 05, 07, 08, 09)');
      return;
    }
    setPhoneError('');

    if (isSubmitting) return;
    setIsSubmitting(true);

    try {
      // 1. Tạo payload gửi lên Backend ASP.NET Core
      const orderPayload = {
        customerName: customerName.trim(),
        phoneNumber: cleanPhone,
        email: currentUser?.email || '',
        shippingAddress: customerAddress.trim(),
        notes: orderNotes.trim(),
        paymentMethod: paymentMethod === 'cod' ? 'COD' : 'VietQR',
        purchaseMode: purchaseMode,
        termMonths: purchaseMode === 'Installment' ? termMonths : null,
        idempotencyKey: idempotencyKey || `IDEMP-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`,
        items: checkedItems.map((item) => ({
          productId: item.productId || item.id,
          variantId: item.variantId || null,
          colorName: item.colorName || item.color || null,
          quantity: item.quantity || 1,
          selectedSpecs: item.specs || (item.ram ? `RAM: ${item.ram} • ${item.storage}` : 'Cấu hình tiêu chuẩn'),
        })),
      };

      let res;
      try {
        res = await fetch('/api/orders', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
          body: JSON.stringify(orderPayload),
        });
      } catch (networkErr) {
        console.error('Lỗi mạng kết nối API /api/orders:', networkErr);
        alert('Không thể kết nối đến máy chủ backend (Cổng 5148 đang tắt hoặc mất mạng). Vui lòng thử lại!');
        return;
      }

      const contentType = res.headers.get('content-type') || '';
      let data = null;
      if (contentType.includes('application/json')) {
        try {
          data = await res.json();
        } catch (jsonErr) {
          console.error('Lỗi phân tích cú pháp JSON phản hồi:', jsonErr);
          alert('Máy chủ trả về phản hồi không hợp lệ. Vui lòng thử lại sau!');
          return;
        }
      } else {
        const text = await res.text();
        console.error('Máy chủ phản hồi nội dung non-JSON:', text);
        alert(`Máy chủ phản hồi mã trạng thái HTTP ${res.status}. Vui lòng kiểm tra lại dịch vụ backend!`);
        return;
      }

      if (!res.ok || !data.success) {
        let errorMsg = data.message;
        if (!errorMsg && data.errors) {
          errorMsg = Object.entries(data.errors)
            .map(([field, msgs]) => Array.isArray(msgs) ? msgs.join(', ') : msgs)
            .join('\n');
        }
        if (!errorMsg && data.title) {
          errorMsg = data.title;
        }
        alert(errorMsg || 'Đặt hàng thất bại. Vui lòng kiểm tra lại thông tin giỏ hàng!');
        return;
      }

      const realOrderCode = data.orderCode || data.order?.orderCode || 'PC000000';

      try {
        // Lấy chính xác tổng tiền đã được máy chủ Backend kiểm tra và chốt từ CSDL
        const confirmedTotal = (data.order && typeof data.order.totalAmount === 'number')
          ? data.order.totalAmount
          : (typeof data.totalAmount === 'number' ? data.totalAmount : totalPayable);

        const orderData = {
          orderId: realOrderCode,
          name: customerName,
          phone: cleanPhone,
          address: customerAddress,
          notes: orderNotes,
          total: confirmedTotal,
          purchaseMode: purchaseMode,
          termMonths: termMonths,
          prepayAmount: Math.round(confirmedTotal * 0.30),
          remainingDebt: confirmedTotal - Math.round(confirmedTotal * 0.30),
          items: (data.order?.items && data.order.items.length > 0)
            ? data.order.items.map((i) => ({
                ...i,
                name: i.productName,
                price: i.unitPrice,
                specs: i.selectedSpecs,
                image: i.thumbnailUrl,
              }))
            : [...checkedItems],
          paymentMethod: paymentMethod === 'cod'
            ? (purchaseMode === 'Installment'
                ? 'Trả góp - Tiền mặt 30% khi giao máy (COD)'
                : 'Thanh toán tiền mặt khi nhận hàng (COD)')
            : `Chuyển khoản VietQR (${STORE_BANK_ACCOUNT.bankId})`,
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
      } catch (uiErr) {
        console.error('Lỗi cập nhật giao diện sau khi tạo đơn:', uiErr);
        alert(`Đơn hàng của bạn đã được ghi nhận thành công với mã #${realOrderCode}! Bạn có thể tra cứu đơn hàng bằng số điện thoại.`);
      }
    } finally {
      setIsSubmitting(false);
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

  // Đường link tạo mã QR VietQR tự động theo số tiền đã chốt từ máy chủ và mã đơn hàng
  const amountToPayNow = pendingOrder?.purchaseMode === 'Installment'
    ? pendingOrder.prepayAmount
    : (pendingOrder?.total || totalPayable);

  const qrImageUrl = pendingOrder
    ? `https://img.vietqr.io/image/${STORE_BANK_ACCOUNT.bankId}-${STORE_BANK_ACCOUNT.accountNo}-compact2.png?amount=${amountToPayNow}&addInfo=${encodeURIComponent((pendingOrder.purchaseMode === 'Installment' ? 'TT TRAGOP ' : '') + pendingOrder.orderId)}&accountName=${encodeURIComponent(STORE_BANK_ACCOUNT.accountName)}`
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
              {orderSuccess.purchaseMode === 'Installment' ? (
                <div className="mt-2 pt-2 border-top">
                  <div className="d-flex justify-content-between text-muted">
                    <span>Tổng giá trị đơn hàng:</span>
                    <strong>{formatVND(orderSuccess.total)}</strong>
                  </div>
                  <div className="d-flex justify-content-between text-success fw-bold">
                    <span>Khoản trả trước 30%:</span>
                    <span>{formatVND(orderSuccess.prepayAmount)}</span>
                  </div>
                  <div className="d-flex justify-content-between text-muted">
                    <span>Còn lại ({orderSuccess.termMonths} kỳ):</span>
                    <span>{formatVND(orderSuccess.remainingDebt)}</span>
                  </div>
                  <div className="alert alert-info py-2 px-3 mt-2 mb-0 small">
                    <i className="bi bi-info-circle me-1"></i>
                    {orderSuccess.paymentMethod.includes('COD') || orderSuccess.paymentMethod.includes('tiền mặt')
                      ? `Cửa hàng sẽ liên hệ xác nhận và cử nhân viên giao máy tận nơi, hỗ trợ ký hợp đồng trả góp và thu khoản trả trước 30% (${formatVND(orderSuccess.prepayAmount)}) bằng tiền mặt.`
                      : `Hệ thống đã ghi nhận thanh toán khoản trả trước 30% (${formatVND(orderSuccess.prepayAmount)}). Nhân viên sẽ liên hệ và giao máy trong thời gian sớm nhất.`}
                  </div>
                </div>
              ) : (
                <div className="mt-2 pt-2 border-top text-danger fw-bold fs-6">
                  Tổng thanh toán: {formatVND(orderSuccess.total)}
                </div>
              )}
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

                          {(item.selectedRam || item.selectedStorage || item.colorName) && (
                            <div className="cart-item-spec-tag">
                              {item.colorName && (
                                <span className="badge bg-light text-dark border me-2 py-1 px-2" style={{ fontSize: '11px' }}>
                                  <span 
                                    className="d-inline-block rounded-circle me-1 border" 
                                    style={{ width: '8px', height: '8px', backgroundColor: item.colorHex || '#64748b' }}
                                  ></span>
                                  Màu: <strong>{item.colorName}</strong>
                                </span>
                              )}
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
                  onClick={handleOpenCheckoutModal}
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
                  {/* Chọn hình thức mua */}
                  <div className="mb-3 p-2 bg-light rounded-3 border">
                    <label className="form-label small fw-bold text-dark d-block mb-2">Hình thức mua hàng:</label>
                    <div className="d-flex gap-2">
                      <button
                        type="button"
                        className={`btn btn-sm flex-fill ${purchaseMode === 'Full' ? 'btn-primary' : 'btn-outline-secondary'}`}
                        onClick={() => setPurchaseMode('Full')}
                      >
                        Trả toàn bộ
                      </button>
                      <button
                        type="button"
                        className={`btn btn-sm flex-fill ${purchaseMode === 'Installment' ? 'btn-primary' : 'btn-outline-secondary'}`}
                        onClick={() => setPurchaseMode('Installment')}
                      >
                        Trả góp 0% (Trả trước 30%)
                      </button>
                    </div>

                    {purchaseMode === 'Installment' && (
                      <div className="mt-3 pt-2 border-top">
                        <label className="form-label small fw-semibold text-secondary mb-1">Chọn kỳ hạn:</label>
                        <div className="d-flex gap-1 mb-2">
                          {[3, 6, 9, 12].map((t) => (
                            <button
                              key={t}
                              type="button"
                              className={`btn btn-sm flex-fill ${termMonths === t ? 'btn-danger' : 'btn-outline-secondary'}`}
                              onClick={() => setTermMonths(t)}
                            >
                              {t}T
                            </button>
                          ))}
                        </div>
                        <div className="small bg-white p-2 rounded border">
                          <div className="d-flex justify-content-between text-muted">
                            <span>Trả trước (30%):</span>
                            <strong className="text-success">{formatVND(prepayAmount)}</strong>
                          </div>
                          <div className="d-flex justify-content-between text-muted">
                            <span>Còn lại ({termMonths} kỳ):</span>
                            <span>{formatVND(financedAmount)}</span>
                          </div>
                          <div className="d-flex justify-content-between text-dark fw-bold border-top mt-1 pt-1">
                            <span>Mỗi tháng:</span>
                            <span className="text-primary">{formatVND(basePeriodAmount)}/tháng</span>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
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
                      className={`form-control ${phoneError ? 'is-invalid border-danger' : ''}`}
                      placeholder="0987 654 321"
                      required
                      value={customerPhone}
                      onChange={(e) => {
                        setCustomerPhone(e.target.value);
                        if (phoneError) setPhoneError('');
                      }}
                    />
                    {phoneError && (
                      <div className="text-danger small mt-1 d-flex align-items-center gap-1">
                        <i className="bi bi-exclamation-circle-fill"></i>
                        <span>{phoneError}</span>
                      </div>
                    )}
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
                        {purchaseMode === 'Installment'
                          ? '💵 Tiền mặt khi nhận máy (Thu trước 30% khi giao máy & ký HĐ)'
                          : '💵 Thanh toán tiền mặt khi nhận hàng (COD)'}
                      </label>
                      {purchaseMode === 'Installment' && paymentMethod === 'cod' && (
                        <div className="form-text small text-primary mt-1">
                          <i className="bi bi-info-circle me-1"></i>
                          Nhân viên sẽ giao máy tận nơi, hỗ trợ hoàn tất hồ sơ trả góp và thu khoản trả trước 30% ({formatVND(prepayAmount)}) bằng tiền mặt.
                        </div>
                      )}
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
                        {purchaseMode === 'Installment'
                          ? '💳 Chuyển khoản VietQR (Thanh toán 30% trả trước qua mã QR)'
                          : '💳 Chuyển khoản ngân hàng (Quét mã VietQR)'}
                      </label>
                    </div>
                  </div>

                  <div className="p-3 bg-light rounded-3 d-flex justify-content-between align-items-center">
                    <span className="small fw-semibold text-secondary">
                      {purchaseMode === 'Installment' ? 'Cần trả ngay hôm nay (30%):' : 'Tổng thanh toán:'}
                    </span>
                    <span className="fs-5 fw-bold text-danger">
                      {formatVND(purchaseMode === 'Installment' ? prepayAmount : totalPayable)}
                    </span>
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
                    disabled={isSubmitting}
                  >
                    {isSubmitting ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                        Đang xử lý đơn...
                      </>
                    ) : (
                      'Hoàn tất đặt hàng'
                    )}
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
                    <strong className="text-dark">{STORE_BANK_ACCOUNT.bankName}</strong>
                  </div>

                  <div className="d-flex justify-content-between align-items-center py-1 border-bottom">
                    <span className="text-muted">Số tài khoản:</span>
                    <div className="d-flex align-items-center gap-2">
                      <strong className="text-danger fs-6">{STORE_BANK_ACCOUNT.accountNo}</strong>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary py-0 px-2"
                        style={{ fontSize: '11px' }}
                        onClick={() => handleCopy(STORE_BANK_ACCOUNT.accountNo, 'acc')}
                      >
                        {copiedKey === 'acc' ? '✓ Đã chép' : 'Sao chép'}
                      </button>
                    </div>
                  </div>

                  <div className="d-flex justify-content-between align-items-center py-1 border-bottom">
                    <span className="text-muted">Chủ tài khoản:</span>
                    <strong className="text-dark">{STORE_BANK_ACCOUNT.accountName}</strong>
                  </div>

                  <div className="d-flex justify-content-between align-items-center py-1 border-bottom">
                    <span className="text-muted">Số tiền:</span>
                    <div className="d-flex align-items-center gap-2">
                      <strong className="text-danger">{formatVND(amountToPayNow)}</strong>
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

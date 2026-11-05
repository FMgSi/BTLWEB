import { useState, useEffect } from 'react';
import { formatVND } from '../../utils/formatters';
import './CartPage.css';

export default function CartPage({
  cartItems = [],
  onUpdateQuantity,
  onRemoveItem,
  onRemovePurchasedItems,
  onClearCart,
  onToggleCheck,
  onToggleAll,
  onGoHome,
  onSelectProduct,
  currentUser,
  showToast,
  onNavigate,
}) {
  // Điều hướng bước giao diện: false = Giỏ hàng (Hình 1), true = Thanh toán (Hình 2)
  const [isCheckoutStep, setIsCheckoutStep] = useState(false);

  // Modals & States
  const [showQrModal, setShowQrModal] = useState(false);
  const [showVoucherModal, setShowVoucherModal] = useState(false);
  const [orderSuccess, setOrderSuccess] = useState(null);
  const [pendingOrder, setPendingOrder] = useState(null);
  const [copiedKey, setCopiedKey] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [phoneError, setPhoneError] = useState('');
  const [formError, setFormError] = useState('');
  const [idempotencyKey, setIdempotencyKey] = useState('');

  // 1. Quản lý Địa chỉ nhận hàng (Lưu 1 lần vào localStorage, các lần mua sau tự động add vào)
  const [savedAddress, setSavedAddress] = useState(() => {
    try {
      const saved = localStorage.getItem('pcstore_shipping_address');
      return saved ? JSON.parse(saved) : null;
    } catch {
      return null;
    }
  });

  const [customerName, setCustomerName] = useState(
    savedAddress?.name || currentUser?.fullName || currentUser?.username || ''
  );
  const [customerPhone, setCustomerPhone] = useState(
    savedAddress?.phone || currentUser?.phoneNumber || ''
  );
  const [customerAddress, setCustomerAddress] = useState(savedAddress?.address || '');
  const [isEditingAddress, setIsEditingAddress] = useState(!savedAddress?.address);
  const [orderNotes, setOrderNotes] = useState('');

  // 2. Phương thức thanh toán & Mua hàng
  // 'cod' | 'banking' (VietQR) | 'vnpay' | 'installment'
  const [paymentMethod, setPaymentMethod] = useState('cod');
  const [purchaseMode, setPurchaseMode] = useState('Full'); // 'Full' | 'Installment'
  const [termMonths, setTermMonths] = useState(6);
  const [serverInstallmentPlan, setServerInstallmentPlan] = useState(null);

  // 3. Voucher khuyến mãi liên kết trực tiếp với /api/promotions
  const [availablePromotions, setAvailablePromotions] = useState([]);
  const [appliedVoucher, setAppliedVoucher] = useState(null);
  const [voucherInputCode, setVoucherInputCode] = useState('');
  const [voucherError, setVoucherError] = useState('');
  const [isLoadingPromotions, setIsLoadingPromotions] = useState(false);

  // 4. Ưu đãi Hạng thành viên (Hạng Bạc - 5%, Hạng Vàng - 10%)
  const [memberTier, setMemberTier] = useState(() => {
    const rawTier = currentUser?.tier || currentUser?.memberTier || '';
    if (rawTier.includes('Vàng') || rawTier.includes('Gold') || rawTier.includes('Kim Cương')) return 'Vàng';
    if (rawTier.includes('Bạc') || rawTier.includes('Silver')) return 'Bạc';
    return 'Đồng';
  });

  // Tự động tải danh sách Voucher từ API Backend (/api/promotions)
  useEffect(() => {
    setIsLoadingPromotions(true);
    fetch('/api/promotions')
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data && data.success && Array.isArray(data.promotions)) {
          setAvailablePromotions(data.promotions);
        }
      })
      .catch((err) => console.warn('Không tải được danh sách voucher từ API:', err))
      .finally(() => setIsLoadingPromotions(false));
  }, []);

  // Thông tin tài khoản ngân hàng chính thức
  const STORE_BANK_ACCOUNT = {
    bankId: 'MB',
    bankName: 'MBBank (Ngân hàng Quân Đội)',
    accountNo: '0988668866',
    accountName: 'HE THONG LAPTOP WORLD',
  };

  // Các sản phẩm được chọn để mua
  const checkedItems = cartItems.filter((i) => i.checked !== false);
  const isAllChecked = cartItems.length > 0 && checkedItems.length === cartItems.length;

  // Tổng tiền hàng chưa giảm
  const totalPayable = checkedItems.reduce((sum, item) => sum + (item.price || 0) * (item.quantity || 1), 0);
  const totalOriginal = checkedItems.reduce((sum, item) => {
    const orig = item.originalPrice && item.originalPrice > item.price ? item.originalPrice : item.price;
    return sum + (orig || 0) * (item.quantity || 1);
  }, 0);
  const itemDiscountTotal = Math.max(0, totalOriginal - totalPayable);

  // Tính ưu đãi thành viên: Bạc = 5%, Vàng = 10%
  const memberDiscountRate = memberTier === 'Vàng' ? 0.10 : memberTier === 'Bạc' ? 0.05 : 0;
  const memberDiscountAmount = Math.round(totalPayable * memberDiscountRate);
  const afterMemberDiscount = Math.max(0, totalPayable - memberDiscountAmount);

  // Tính voucher giảm giá
  let voucherDiscountAmount = 0;
  if (appliedVoucher) {
    if (appliedVoucher.discountType === 'Percentage') {
      const calc = Math.round((afterMemberDiscount * appliedVoucher.discountValue) / 100);
      voucherDiscountAmount = appliedVoucher.maxDiscountAmount
        ? Math.min(calc, appliedVoucher.maxDiscountAmount)
        : calc;
    } else {
      voucherDiscountAmount = appliedVoucher.discountValue || 0;
    }
  }
  voucherDiscountAmount = Math.min(voucherDiscountAmount, afterMemberDiscount);

  // Số tiền cuối cùng cần thanh toán
  const finalPayable = Math.max(0, afterMemberDiscount - voucherDiscountAmount);

  // Đồng bộ phương án trả góp từ Backend API dựa trên finalPayable
  useEffect(() => {
    if (purchaseMode !== 'Installment' || finalPayable < 1000000) {
      setServerInstallmentPlan(null);
      return;
    }

    let isMounted = true;
    const fetchInstallmentPlan = async () => {
      try {
        const res = await fetch(`/api/orders/installment-preview?amount=${finalPayable}&termMonths=${termMonths}`);
        if (res.ok) {
          const data = await res.json();
          if (isMounted && data.success && data.plan) {
            setServerInstallmentPlan(data.plan);
          }
        }
      } catch (err) {
        console.warn('Không thể tải preview trả góp:', err);
      }
    };

    fetchInstallmentPlan();
    return () => {
      isMounted = false;
    };
  }, [purchaseMode, finalPayable, termMonths]);

  // Trả trước và các kỳ
  const prepayAmount = serverInstallmentPlan
    ? serverInstallmentPlan.prepayAmount
    : Math.round(finalPayable * 0.30);
  const financedAmount = serverInstallmentPlan
    ? serverInstallmentPlan.financedAmount
    : finalPayable - prepayAmount;
  const basePeriodAmount = serverInstallmentPlan
    ? serverInstallmentPlan.monthlyInstallmentAmount
    : Math.floor(financedAmount / termMonths);

  // Chuyển sang màn hình Mua hàng Shopee
  const handleProceedToCheckout = () => {
    if (checkedItems.length === 0) {
      if (showToast) showToast('Vui lòng chọn ít nhất 1 sản phẩm để mua hàng!');
      return;
    }
    setIdempotencyKey(`IDEMP-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`);
    setPhoneError('');
    setFormError('');
    setIsCheckoutStep(true);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Lưu địa chỉ vào localStorage để lần sau tự động nạp
  const handleSaveAddress = () => {
    if (!customerName.trim() || !customerPhone.trim() || !customerAddress.trim()) {
      setPhoneError('Vui lòng điền đầy đủ Họ tên, Số điện thoại và Địa chỉ chi tiết!');
      return;
    }
    const cleanPhone = customerPhone.replace(/[\s\-\.]/g, '');
    const vnPhoneRegex = /^(0|\+84)(3|5|7|8|9)[0-9]{8}$/;
    if (!vnPhoneRegex.test(cleanPhone)) {
      setPhoneError('Số điện thoại không đúng định dạng (cần đúng 10 số, bắt đầu bằng 03, 05, 07, 08, 09)');
      return;
    }

    const addrObj = {
      name: customerName.trim(),
      phone: cleanPhone,
      address: customerAddress.trim(),
    };
    try {
      localStorage.setItem('pcstore_shipping_address', JSON.stringify(addrObj));
    } catch {}
    setSavedAddress(addrObj);
    setIsEditingAddress(false);
    setPhoneError('');
    if (showToast) showToast('✓ Đã lưu địa chỉ nhận hàng mặc định!');
  };

  // Áp dụng Voucher
  const handleApplyVoucher = (v) => {
    if (!v) return;
    if (v.minOrderAmount && totalPayable < v.minOrderAmount) {
      setVoucherError(`Đơn hàng tối thiểu ${formatVND(v.minOrderAmount)} mới áp dụng được mã ${v.code}!`);
      return;
    }
    setAppliedVoucher(v);
    setVoucherError('');
    setShowVoucherModal(false);
    if (showToast) showToast(`✓ Đã áp dụng mã giảm giá: ${v.code}`);
  };

  // Áp dụng Voucher gõ tay
  const handleApplyCustomVoucher = () => {
    if (!voucherInputCode.trim()) return;
    const code = voucherInputCode.trim().toUpperCase();
    const found = availablePromotions.find((p) => p.code.toUpperCase() === code);
    if (found) {
      handleApplyVoucher(found);
    } else {
      setVoucherError(`Mã giảm giá "${code}" không tồn tại hoặc đã hết lượt áp dụng.`);
    }
  };

  // Xóa Voucher
  const handleRemoveVoucher = () => {
    setAppliedVoucher(null);
    setVoucherError('');
  };

  // Sao chép thông tin
  const handleCopy = (text, key) => {
    navigator.clipboard.writeText(text);
    setCopiedKey(key);
    setTimeout(() => setCopiedKey(''), 2000);
  };

  // Xử lý khi nhấn nút Đặt Hàng (Gửi lên Backend API)
  const handleConfirmOrder = async (e) => {
    if (e) e.preventDefault();

    if (!customerName.trim() || !customerPhone.trim() || !customerAddress.trim()) {
      setFormError('Vui lòng nhập đầy đủ thông tin Người nhận và Địa chỉ giao hàng!');
      setIsEditingAddress(true);
      return;
    }

    const address = customerAddress.trim();
    if (address.length < 5 || address.length > 255) {
      setFormError('Địa chỉ giao hàng tối thiểu 5 ký tự (gồm số nhà, tên đường, phường/xã, quận/huyện).');
      setIsEditingAddress(true);
      return;
    }

    const cleanPhone = customerPhone.replace(/[\s\-\.]/g, '');
    const vnPhoneRegex = /^(0|\+84)(3|5|7|8|9)[0-9]{8}$/;
    if (!vnPhoneRegex.test(cleanPhone)) {
      setPhoneError('Số điện thoại không đúng định dạng (cần đúng 10 số, bắt đầu bằng 03, 05, 07, 08, 09)');
      setIsEditingAddress(true);
      return;
    }
    setPhoneError('');
    setFormError('');

    if (isSubmitting) return;
    setIsSubmitting(true);

    try {
      // Tự động lưu địa chỉ vào localStorage để các đơn hàng sau tự nạp
      try {
        localStorage.setItem(
          'pcstore_shipping_address',
          JSON.stringify({ name: customerName.trim(), phone: cleanPhone, address: address })
        );
      } catch {}

      const orderPayload = {
        customerName: customerName.trim(),
        phoneNumber: cleanPhone,
        email: currentUser?.email?.trim() || null,
        shippingAddress: address,
        notes: orderNotes.trim(),
        paymentMethod: paymentMethod === 'cod' ? 'COD' : paymentMethod === 'vnpay' ? 'VNPAY' : 'VietQR',
        purchaseMode: purchaseMode,
        termMonths: purchaseMode === 'Installment' ? termMonths : null,
        discountCode: appliedVoucher?.code || null,
        memberTier: memberTier,
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
        setFormError('Không thể kết nối đến máy chủ backend (Cổng 5148 đang tắt hoặc mất mạng). Vui lòng thử lại!');
        return;
      }

      const contentType = res.headers.get('content-type') || '';
      let data = null;
      if (/^(?:application\/json|application\/[\w.+-]+\+json)(?:\s*;|$)/i.test(contentType)) {
        try {
          data = await res.json();
        } catch (jsonErr) {
          console.error('Lỗi JSON:', jsonErr);
          setFormError('Máy chủ trả về phản hồi không hợp lệ. Vui lòng thử lại sau!');
          return;
        }
      } else {
        setFormError(`Máy chủ phản hồi mã trạng thái HTTP ${res.status}. Vui lòng thử lại sau!`);
        return;
      }

      if (!res.ok || !data.success) {
        let errorMsg = data.message;
        if (!errorMsg && data.errors) {
          errorMsg = Object.values(data.errors)
            .map((msgs) => (Array.isArray(msgs) ? msgs.join(', ') : msgs))
            .join('\n');
        }
        setFormError(errorMsg || 'Đặt hàng thất bại. Vui lòng kiểm tra lại thông tin giỏ hàng!');
        return;
      }

      const realOrderCode = data.orderCode || data.order?.orderCode;
      if (!realOrderCode) {
        setFormError('Phản hồi từ máy chủ không chứa mã đơn hàng hợp lệ.');
        return;
      }

      const confirmedTotal =
        data.order && typeof data.order.totalAmount === 'number'
          ? data.order.totalAmount
          : typeof data.totalAmount === 'number'
          ? data.totalAmount
          : finalPayable;

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
        items:
          data.order?.items && data.order.items.length > 0
            ? data.order.items.map((i) => ({
                ...i,
                name: i.productName,
                price: i.unitPrice,
                specs: i.selectedSpecs,
                image: i.thumbnailUrl,
              }))
            : [...checkedItems],
        paymentMethod:
          paymentMethod === 'cod'
            ? purchaseMode === 'Installment'
              ? 'Trả góp - Tiền mặt 30% khi giao máy (COD)'
              : 'Thanh toán tiền mặt khi nhận hàng (COD)'
            : paymentMethod === 'vnpay'
            ? 'Thanh toán qua Ví điện tử VNPay'
            : `Chuyển khoản VietQR (${STORE_BANK_ACCOUNT.bankId})`,
      };

      // Xóa các sản phẩm đã mua khỏi giỏ hàng
      const purchasedKeys = checkedItems.map((item) => item.cartItemId || item.id);
      if (onRemovePurchasedItems) {
        onRemovePurchasedItems(purchasedKeys);
      } else {
        checkedItems.forEach((item) => {
          onRemoveItem(item.cartItemId || item.id);
        });
      }

      setIsCheckoutStep(false);
      setFormError('');

      if (paymentMethod === 'banking') {
        setPendingOrder(orderData);
        setShowQrModal(true);
      } else {
        setOrderSuccess(orderData);
        if (showToast) {
          showToast(`🎉 Đặt hàng thành công! Mã đơn: #${realOrderCode}`, 5000);
        }
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCloseQrModal = () => {
    const code = pendingOrder?.orderId || '';
    const completed = pendingOrder;
    setShowQrModal(false);
    setPendingOrder(null);
    if (completed) setOrderSuccess(completed);
    if (showToast && code) {
      showToast(`🎉 Đã tiếp nhận đơn hàng #${code}. Cửa hàng sẽ liên hệ xác nhận sớm nhất!`, 5000);
    }
  };

  const amountToPayNow =
    pendingOrder?.purchaseMode === 'Installment'
      ? pendingOrder.prepayAmount
      : pendingOrder?.total || finalPayable;

  const qrImageUrl = pendingOrder
    ? `https://img.vietqr.io/image/${STORE_BANK_ACCOUNT.bankId}-${STORE_BANK_ACCOUNT.accountNo}-compact2.png?amount=${amountToPayNow}&addInfo=${encodeURIComponent(
        (pendingOrder.purchaseMode === 'Installment' ? 'TT TRAGOP ' : '') + pendingOrder.orderId
      )}&accountName=${encodeURIComponent(STORE_BANK_ACCOUNT.accountName)}`
    : '';

  return (
    <div className="cart-page-wrap py-4" style={{ backgroundColor: '#f5f5f5', minHeight: '85vh' }}>
      <div className="site-container">
        {/* Breadcrumb */}
        <div className="cart-breadcrumb mb-3">
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
          <a
            href="/cart"
            onClick={(e) => {
              e.preventDefault();
              setIsCheckoutStep(false);
            }}
          >
            Giỏ hàng
          </a>
          {isCheckoutStep && (
            <>
              <span>/</span>
              <span className="text-dark fw-bold">Thanh toán (Shopee Style)</span>
            </>
          )}
        </div>

        {/* 1. MÀN HÌNH ĐẶT HÀNG THÀNH CÔNG */}
        {orderSuccess ? (
          <div className="bg-white rounded-3 border p-5 text-center shadow-sm my-3 mx-auto" style={{ maxWidth: '640px' }}>
            <div className="rounded-circle bg-success bg-opacity-10 text-success d-inline-flex align-items-center justify-content-center mb-3" style={{ width: '64px', height: '64px' }}>
              <i className="bi bi-check-lg fs-1"></i>
            </div>
            <h4 className="fw-bold text-dark mb-2">Đặt hàng thành công!</h4>
            <p className="text-muted small mb-3">
              Cảm ơn quý khách <strong>{orderSuccess.name}</strong> đã đặt mua tại <strong>Laptop World</strong>.
              <br />
              Mã đơn hàng: <span className="text-danger fw-bold fs-6">#{orderSuccess.orderId}</span>
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary ms-2 py-0 px-2"
                style={{ fontSize: '12px' }}
                onClick={() => {
                  navigator.clipboard?.writeText(orderSuccess.orderId);
                  setCopiedKey(orderSuccess.orderId);
                  setTimeout(() => setCopiedKey(''), 2500);
                }}
              >
                {copiedKey === orderSuccess.orderId ? '✓ Đã chép' : 'Sao chép mã'}
              </button>
            </p>
            <div className="bg-light p-3 rounded-3 text-start small mb-3">
              <div><strong>Giao đến:</strong> {orderSuccess.address}</div>
              <div><strong>Số điện thoại:</strong> {orderSuccess.phone}</div>
              <div><strong>Phương thức:</strong> {orderSuccess.paymentMethod}</div>
              {orderSuccess.purchaseMode === 'Installment' ? (
                <div className="mt-2 pt-2 border-top">
                  <div className="d-flex justify-content-between text-muted">
                    <span>Tổng đơn hàng:</span>
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
                </div>
              ) : (
                <div className="mt-2 pt-2 border-top text-danger fw-bold fs-6">
                  Tổng thanh toán: {formatVND(orderSuccess.total)}
                </div>
              )}
            </div>
            <div className="d-flex flex-wrap justify-content-center gap-2">
              <button
                type="button"
                className="btn btn-outline-danger px-4 py-2 fw-semibold"
                onClick={() => {
                  setOrderSuccess(null);
                  if (onNavigate) onNavigate('account');
                  else {
                    window.history.pushState({}, '', '/account');
                    window.dispatchEvent(new PopStateEvent('popstate'));
                  }
                }}
              >
                <i className="bi bi-person-circle me-1"></i> Theo dõi tại Tài khoản
              </button>
              <button
                type="button"
                className="btn btn-danger px-4 py-2 fw-semibold"
                onClick={() => {
                  setOrderSuccess(null);
                  onGoHome();
                }}
              >
                Tiếp tục mua hàng
              </button>
            </div>
          </div>
        ) : cartItems.length === 0 ? (
          /* Trạng thái giỏ rỗng */
          <div className="cart-empty-box my-4">
            <i className="bi bi-cart-x cart-empty-icon"></i>
            <h5 className="fw-bold text-dark mb-2">Giỏ hàng của bạn đang trống</h5>
            <p className="text-muted small mb-3">Hãy chọn thêm sản phẩm laptop để nhận nhiều ưu đãi hấp dẫn!</p>
            <button type="button" className="cart-empty-btn" onClick={() => onGoHome()}>
              <i className="bi bi-arrow-left"></i> Khám phá sản phẩm ngay
            </button>
          </div>
        ) : !isCheckoutStep ? (
          /* =========================================================================
             2. GIAO DIỆN GIỎ HÀNG SHOPEE (PDF HÌNH 1)
             ========================================================================= */
          <div>
            {/* Header bảng Shopee Cart */}
            <div className="shopee-table-header d-flex align-items-center mb-3">
              <div style={{ width: '45%' }} className="d-flex align-items-center gap-3">
                <input
                  type="checkbox"
                  className="cart-checkbox"
                  checked={isAllChecked}
                  onChange={(e) => onToggleAll(e.target.checked)}
                />
                <span className="fw-bold text-dark">Sản phẩm ({cartItems.length})</span>
              </div>
              <div style={{ width: '15%' }} className="text-center fw-semibold">Đơn Giá</div>
              <div style={{ width: '15%' }} className="text-center fw-semibold">Số Lượng</div>
              <div style={{ width: '15%' }} className="text-center fw-semibold">Số Tiền</div>
              <div style={{ width: '10%' }} className="text-center fw-semibold">Thao Tác</div>
            </div>

            {/* Danh sách các sản phẩm trong giỏ hàng kiểu Shopee */}
            <div className="bg-white rounded-3 border shadow-sm p-3 mb-4">
              {cartItems.map((item) => {
                const itemTotal = (item.price || 0) * (item.quantity || 1);
                const isChecked = item.checked !== false;

                return (
                  <div
                    key={item.cartItemId || item.id}
                    className="d-flex align-items-center py-3 border-bottom"
                    style={{ gap: '15px' }}
                  >
                    {/* Checkbox & Thông tin sản phẩm */}
                    <div style={{ width: '45%' }} className="d-flex align-items-center gap-3">
                      <input
                        type="checkbox"
                        className="cart-checkbox flex-shrink-0"
                        checked={isChecked}
                        onChange={() => onToggleCheck(item.cartItemId || item.id)}
                      />
                      <div
                        className="cart-item-thumb flex-shrink-0"
                        style={{ cursor: 'pointer', width: '70px', height: '70px' }}
                        onClick={() => onSelectProduct && onSelectProduct(item.productId || item.id)}
                        title="Bấm để xem lại thông số kỹ thuật sản phẩm"
                      >
                        <img
                          src={item.image || '/images/default.png'}
                          alt={item.name}
                          style={{ width: '100%', height: '100%', objectFit: 'contain' }}
                          className="rounded border"
                        />
                      </div>
                      <div className="flex-grow-1">
                        <div
                          className="fw-bold text-dark mb-1"
                          style={{ cursor: 'pointer', fontSize: '14px', lineHeight: '1.3' }}
                          onClick={() => onSelectProduct && onSelectProduct(item.productId || item.id)}
                          title="Bấm vào để xem lại chi tiết sản phẩm"
                        >
                          {item.name}
                        </div>
                        {(item.selectedRam || item.selectedStorage || item.colorName) && (
                          <div className="small text-muted" style={{ fontSize: '12px' }}>
                            {item.colorName && (
                              <span className="badge bg-light text-dark border me-1">
                                Màu: {item.colorName}
                              </span>
                            )}
                            {item.selectedRam && <span>RAM: {item.selectedRam}</span>}
                            {item.selectedRam && item.selectedStorage && <span> • </span>}
                            {item.selectedStorage && <span>SSD: {item.selectedStorage}</span>}
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Đơn giá */}
                    <div style={{ width: '15%' }} className="text-center">
                      <div className="fw-bold text-danger">{formatVND(item.price)}</div>
                      {item.originalPrice && item.originalPrice > item.price && (
                        <div className="text-muted text-decoration-line-through small" style={{ fontSize: '11px' }}>
                          {formatVND(item.originalPrice)}
                        </div>
                      )}
                    </div>

                    {/* Số lượng (+ / -) */}
                    <div style={{ width: '15%' }} className="d-flex justify-content-center">
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
                    </div>

                    {/* Số tiền (Thành tiền của từng dòng) */}
                    <div style={{ width: '15%' }} className="text-center fw-bold text-danger fs-6">
                      {formatVND(itemTotal)}
                    </div>

                    {/* Thao tác (Xóa) */}
                    <div style={{ width: '10%' }} className="text-center">
                      <button
                        type="button"
                        className="btn btn-sm btn-link text-danger text-decoration-none p-0"
                        onClick={() => onRemoveItem(item.cartItemId || item.id)}
                        title="Xóa sản phẩm này"
                      >
                        <i className="bi bi-trash3 me-1"></i>Xóa
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Thanh Shopee Sticky Footer tổng kết giỏ hàng */}
            <div className="shopee-cart-footer d-flex flex-wrap align-items-center justify-content-between gap-3">
              <div className="d-flex align-items-center gap-3">
                <label className="d-flex align-items-center gap-2 m-0 fw-semibold text-dark" style={{ cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    className="cart-checkbox"
                    checked={isAllChecked}
                    onChange={(e) => onToggleAll(e.target.checked)}
                  />
                  <span>Chọn Tất Cả ({checkedItems.length}/{cartItems.length})</span>
                </label>
                <button
                  type="button"
                  className="btn btn-sm btn-outline-danger"
                  onClick={() => {
                    if (window.confirm('Bạn có chắc muốn xóa tất cả sản phẩm trong giỏ hàng?')) {
                      onClearCart();
                    }
                  }}
                >
                  Xóa tất cả
                </button>
              </div>

              <div className="d-flex align-items-center gap-4">
                <div className="text-end">
                  <div className="text-muted small">
                    Tổng thanh toán ({checkedItems.length} sản phẩm):
                  </div>
                  <div className="fs-4 fw-bold text-danger lh-1">
                    {formatVND(totalPayable)}
                  </div>
                  {itemDiscountTotal > 0 && (
                    <div className="text-muted" style={{ fontSize: '11px' }}>
                      Tiết kiệm {formatVND(itemDiscountTotal)}
                    </div>
                  )}
                </div>

                <button
                  type="button"
                  className="shopee-btn-buy"
                  disabled={checkedItems.length === 0}
                  onClick={handleProceedToCheckout}
                >
                  Mua Hàng ({checkedItems.length})
                </button>
              </div>
            </div>
          </div>
        ) : (
          /* =========================================================================
             3. GIAO DIỆN THANH TOÁN SHOPEE (PDF HÌNH 2)
             ========================================================================= */
          <div>
            {/* Nút quay lại giỏ hàng */}
            <div className="mb-3">
              <button
                type="button"
                className="btn btn-outline-secondary btn-sm px-3"
                onClick={() => setIsCheckoutStep(false)}
              >
                <i className="bi bi-arrow-left me-1"></i> Quay lại giỏ hàng
              </button>
            </div>

            {/* KHỐI 1: ĐỊA CHỈ NHẬN HÀNG (LƯU TỰ ĐỘNG, NÚT THAY ĐỔI) */}
            <div className="shopee-address-card p-4 mb-3 border">
              <div className="shopee-address-ribbon mb-3"></div>
              <div className="d-flex align-items-center justify-content-between mb-2">
                <div className="fw-bold text-danger fs-6 d-flex align-items-center gap-2">
                  <i className="bi bi-geo-alt-fill fs-5"></i>
                  <span>Địa Chỉ Nhận Hàng</span>
                </div>
                {!isEditingAddress && (
                  <button
                    type="button"
                    className="btn btn-link text-primary fw-bold text-decoration-none p-0"
                    onClick={() => setIsEditingAddress(true)}
                  >
                    Thay Đổi
                  </button>
                )}
              </div>

              {isEditingAddress ? (
                /* Form nhập hoặc chỉnh sửa địa chỉ */
                <div className="bg-light p-3 rounded-3 border mt-2">
                  <div className="row g-2 mb-2">
                    <div className="col-md-6">
                      <label className="form-label small fw-semibold text-secondary mb-1">
                        Họ và tên người nhận <span className="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        className="form-control form-control-sm"
                        placeholder="Ví dụ: Nguyễn Văn A"
                        value={customerName}
                        onChange={(e) => setCustomerName(e.target.value)}
                      />
                    </div>
                    <div className="col-md-6">
                      <label className="form-label small fw-semibold text-secondary mb-1">
                        Số điện thoại nhận hàng <span className="text-danger">*</span>
                      </label>
                      <input
                        type="tel"
                        className={`form-control form-control-sm ${phoneError ? 'border-danger' : ''}`}
                        placeholder="Ví dụ: 0987 654 321"
                        value={customerPhone}
                        onChange={(e) => {
                          setCustomerPhone(e.target.value);
                          if (phoneError) setPhoneError('');
                        }}
                      />
                    </div>
                  </div>
                  <div className="mb-2">
                    <label className="form-label small fw-semibold text-secondary mb-1">
                      Địa chỉ nhận hàng chi tiết <span className="text-danger">*</span>
                    </label>
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      placeholder="Số nhà, ngõ/ngách, tên đường, phường/xã, quận/huyện, tỉnh/TP"
                      value={customerAddress}
                      onChange={(e) => setCustomerAddress(e.target.value)}
                    />
                  </div>
                  {phoneError && <div className="text-danger small mb-2">{phoneError}</div>}
                  <div className="d-flex gap-2 justify-content-end">
                    {savedAddress?.address && (
                      <button
                        type="button"
                        className="btn btn-sm btn-light"
                        onClick={() => setIsEditingAddress(false)}
                      >
                        Hủy
                      </button>
                    )}
                    <button
                      type="button"
                      className="btn btn-sm btn-danger px-3 fw-semibold"
                      onClick={handleSaveAddress}
                    >
                      Lưu địa chỉ
                    </button>
                  </div>
                </div>
              ) : (
                /* Hiển thị địa chỉ đã lưu (tự động điền) */
                <div className="d-flex flex-wrap align-items-center gap-3">
                  <strong className="text-dark">
                    {customerName} (+84) {customerPhone.replace(/^0/, '')}
                  </strong>
                  <span className="text-secondary">{customerAddress}</span>
                  <span className="badge bg-danger-subtle text-danger border border-danger-subtle px-2 py-1">
                    Mặc định
                  </span>
                </div>
              )}
            </div>

            {/* KHỐI 2: DANH SÁCH SẢN PHẨM ĐẶT MUA (SHOPEE STYLE) */}
            <div className="bg-white rounded-3 border p-4 mb-3 shadow-sm">
              <div className="d-flex align-items-center justify-content-between border-bottom pb-2 mb-3 text-secondary small fw-semibold">
                <span style={{ width: '50%' }}>Sản phẩm ({checkedItems.length})</span>
                <span style={{ width: '15%' }} className="text-center">Đơn giá</span>
                <span style={{ width: '15%' }} className="text-center">Số lượng</span>
                <span style={{ width: '20%' }} className="text-end">Thành tiền</span>
              </div>

              {checkedItems.map((item) => (
                <div key={item.cartItemId || item.id} className="d-flex align-items-center py-2 border-bottom">
                  <div style={{ width: '50%' }} className="d-flex align-items-center gap-3">
                    <img
                      src={item.image || '/images/default.png'}
                      alt={item.name}
                      style={{ width: '50px', height: '50px', objectFit: 'contain' }}
                      className="rounded border flex-shrink-0"
                    />
                    <div>
                      {/* Bấm vào chữ sản phẩm trên để tự động mở xem lại thông số */}
                      <div
                        className="fw-bold text-dark text-truncate"
                        style={{ cursor: 'pointer', maxWidth: '350px', fontSize: '14px' }}
                        onClick={() => onSelectProduct && onSelectProduct(item.productId || item.id)}
                        title="Bấm vào để xem lại chi tiết thông số kỹ thuật"
                      >
                        {item.name}
                      </div>
                      <div className="small text-muted" style={{ fontSize: '11px' }}>
                        Phân loại: {item.colorName || 'Mặc định'}
                        {item.selectedRam && ` • ${item.selectedRam}`}
                        {item.selectedStorage && ` • ${item.selectedStorage}`}
                      </div>
                    </div>
                  </div>
                  <div style={{ width: '15%' }} className="text-center small text-secondary">
                    {formatVND(item.price)}
                  </div>
                  <div style={{ width: '15%' }} className="text-center small text-dark fw-bold">
                    {item.quantity || 1}
                  </div>
                  <div style={{ width: '20%' }} className="text-end fw-bold text-dark">
                    {formatVND((item.price || 0) * (item.quantity || 1))}
                  </div>
                </div>
              ))}

              {/* Lời nhắn cho người bán */}
              <div className="mt-3 pt-2 d-flex align-items-center gap-3">
                <span className="small text-secondary fw-semibold flex-shrink-0">Lời nhắn:</span>
                <input
                  type="text"
                  className="form-control form-control-sm"
                  placeholder="Lưu ý cho người bán (Ví dụ: Giao giờ hành chính, gọi trước khi đến...)"
                  value={orderNotes}
                  onChange={(e) => setOrderNotes(e.target.value)}
                />
              </div>
            </div>

            {/* KHỐI 3: ƯU ĐÃI HẠNG THÀNH VIÊN (BẠC 5%, VÀNG 10%) */}
            <div className="bg-white rounded-3 border p-4 mb-3 shadow-sm">
              <div className="d-flex align-items-center justify-content-between mb-3">
                <div className="fw-bold text-dark d-flex align-items-center gap-2">
                  <i className="bi bi-award-fill text-warning fs-5"></i>
                  <span>Ưu Đãi Hạng Thành Viên</span>
                  <span className="badge bg-secondary-subtle text-secondary small">Đặc quyền khách hàng</span>
                </div>
                {memberTier !== 'Đồng' && (
                  <span className="badge bg-danger fs-6 px-3 py-1">
                    Giảm ngay {memberTier === 'Vàng' ? '10%' : '5%'}
                  </span>
                )}
              </div>

              <div className="row g-2">
                <div className="col-4">
                  <div
                    className={`shopee-tier-pill p-3 text-center ${memberTier === 'Đồng' ? 'active' : ''}`}
                    onClick={() => setMemberTier('Đồng')}
                  >
                    <div className="fw-bold text-secondary mb-1">🥉 Hạng Đồng</div>
                    <div className="small text-muted">Ưu đãi 0%</div>
                  </div>
                </div>
                <div className="col-4">
                  <div
                    className={`shopee-tier-pill p-3 text-center ${memberTier === 'Bạc' ? 'active' : ''}`}
                    onClick={() => setMemberTier('Bạc')}
                  >
                    <div className="fw-bold text-primary mb-1">🥈 Hạng Bạc</div>
                    <div className="small text-success fw-bold">Giảm ngay 5%</div>
                  </div>
                </div>
                <div className="col-4">
                  <div
                    className={`shopee-tier-pill p-3 text-center ${memberTier === 'Vàng' ? 'active' : ''}`}
                    onClick={() => setMemberTier('Vàng')}
                  >
                    <div className="fw-bold text-warning mb-1">🥇 Hạng Vàng</div>
                    <div className="small text-danger fw-bold">Giảm ngay 10%</div>
                  </div>
                </div>
              </div>

              {memberDiscountAmount > 0 && (
                <div className="alert alert-success py-2 px-3 mt-3 mb-0 small d-flex justify-content-between align-items-center">
                  <span>
                    <i className="bi bi-check-circle-fill me-1"></i>
                    Đã áp dụng đặc quyền <strong>Hạng {memberTier}</strong>:
                  </span>
                  <strong className="text-success fs-6">-{formatVND(memberDiscountAmount)}</strong>
                </div>
              )}
            </div>

            {/* KHỐI 4: VOUCHER KHUYẾN MÃI (LIÊN KẾT TRỰC TIẾP VỚI KHUYẾN MÃI CỦA SHOP) */}
            <div className="bg-white rounded-3 border p-4 mb-3 shadow-sm">
              <div className="d-flex align-items-center justify-content-between">
                <div className="d-flex align-items-center gap-2">
                  <i className="bi bi-ticket-perforated-fill text-danger fs-5"></i>
                  <span className="fw-bold text-dark">Laptop World / Shopee Voucher</span>
                </div>
                <div className="d-flex align-items-center gap-2">
                  {appliedVoucher ? (
                    <div className="d-flex align-items-center gap-2">
                      <span className="badge bg-danger px-3 py-2 fw-bold font-monospace">
                        {appliedVoucher.code} (-{formatVND(voucherDiscountAmount)})
                      </span>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary py-1"
                        onClick={handleRemoveVoucher}
                      >
                        Bỏ chọn
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      className="btn btn-sm btn-outline-danger px-3 fw-bold"
                      onClick={() => setShowVoucherModal(true)}
                    >
                      Chọn Voucher
                    </button>
                  )}
                </div>
              </div>
            </div>

            {/* KHỐI 5: PHƯƠNG THỨC THANH TOÁN (COD, VIETQR, VNPAY, TRẢ GÓP 0%) */}
            <div className="bg-white rounded-3 border p-4 mb-3 shadow-sm">
              <h6 className="fw-bold text-dark mb-3">Phương thức thanh toán</h6>

              <div className="row g-2 mb-3">
                <div className="col-md-3 col-6">
                  <div
                    className={`shopee-payment-option p-3 text-center h-100 ${paymentMethod === 'cod' && purchaseMode === 'Full' ? 'active' : ''}`}
                    onClick={() => {
                      setPaymentMethod('cod');
                      setPurchaseMode('Full');
                    }}
                  >
                    <div className="fs-5 mb-1">💵</div>
                    <div className="fw-bold small text-dark">Thanh toán khi nhận hàng (COD)</div>
                  </div>
                </div>

                <div className="col-md-3 col-6">
                  <div
                    className={`shopee-payment-option p-3 text-center h-100 ${paymentMethod === 'banking' && purchaseMode === 'Full' ? 'active' : ''}`}
                    onClick={() => {
                      setPaymentMethod('banking');
                      setPurchaseMode('Full');
                    }}
                  >
                    <div className="fs-5 mb-1">💳</div>
                    <div className="fw-bold small text-dark">Chuyển khoản VietQR (Napas 24/7)</div>
                  </div>
                </div>

                <div className="col-md-3 col-6">
                  <div
                    className={`shopee-payment-option p-3 text-center h-100 ${paymentMethod === 'vnpay' && purchaseMode === 'Full' ? 'active' : ''}`}
                    onClick={() => {
                      setPaymentMethod('vnpay');
                      setPurchaseMode('Full');
                    }}
                  >
                    <div className="fs-5 mb-1">📱</div>
                    <div className="fw-bold small text-dark">Ví điện tử / Thẻ VNPay</div>
                  </div>
                </div>

                <div className="col-md-3 col-6">
                  <div
                    className={`shopee-payment-option p-3 text-center h-100 ${purchaseMode === 'Installment' ? 'active' : ''}`}
                    onClick={() => {
                      setPurchaseMode('Installment');
                      setPaymentMethod('banking');
                    }}
                  >
                    <div className="fs-5 mb-1">📝</div>
                    <div className="fw-bold small text-danger">Trả góp 0% (Duyệt nhanh)</div>
                  </div>
                </div>
              </div>

              {/* Tùy chọn kỳ hạn trả góp nếu chọn Trả góp */}
              {purchaseMode === 'Installment' && (
                <div className="bg-light p-3 rounded-3 border mt-3">
                  <div className="d-flex align-items-center justify-content-between mb-2">
                    <label className="form-label small fw-bold text-dark m-0">Chọn kỳ hạn trả góp:</label>
                    <div className="d-flex gap-2">
                      {[3, 6, 9, 12].map((t) => (
                        <button
                          key={t}
                          type="button"
                          className={`btn btn-sm ${termMonths === t ? 'btn-danger fw-bold' : 'btn-outline-secondary'}`}
                          onClick={() => setTermMonths(t)}
                        >
                          {t} Tháng
                        </button>
                      ))}
                    </div>
                  </div>

                  <div className="row g-2 small mt-2">
                    <div className="col-sm-4">
                      <div className="bg-white p-2 rounded border">
                        <span className="text-muted">Khoản trả trước 30%:</span>
                        <div className="fw-bold text-success fs-6">{formatVND(prepayAmount)}</div>
                      </div>
                    </div>
                    <div className="col-sm-4">
                      <div className="bg-white p-2 rounded border">
                        <span className="text-muted">Số tiền tài trợ:</span>
                        <div className="fw-bold text-dark fs-6">{formatVND(financedAmount)}</div>
                      </div>
                    </div>
                    <div className="col-sm-4">
                      <div className="bg-white p-2 rounded border">
                        <span className="text-muted">Góp mỗi tháng ({termMonths} kỳ):</span>
                        <div className="fw-bold text-danger fs-6">{formatVND(basePeriodAmount)}/tháng</div>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* KHỐI 6: CHI TIẾT THANH TOÁN VÀ NÚT ĐẶT HÀNG (SHOPEE STYLE) */}
            <div className="bg-white rounded-3 border p-4 shadow-sm">
              {formError && (
                <div className="alert alert-danger py-2 px-3 small d-flex align-items-center gap-2 mb-3">
                  <i className="bi bi-exclamation-triangle-fill fs-6 flex-shrink-0"></i>
                  <span>{formError}</span>
                </div>
              )}

              <div className="d-flex flex-column gap-2 mb-3 small text-secondary">
                <div className="d-flex justify-content-between">
                  <span>Tổng tiền hàng:</span>
                  <span className="text-dark fw-semibold">{formatVND(totalPayable)}</span>
                </div>
                {memberDiscountAmount > 0 && (
                  <div className="d-flex justify-content-between text-success">
                    <span>Ưu đãi thành viên (Hạng {memberTier} - {memberDiscountRate * 100}%):</span>
                    <strong className="text-success">-{formatVND(memberDiscountAmount)}</strong>
                  </div>
                )}
                {voucherDiscountAmount > 0 && (
                  <div className="d-flex justify-content-between text-danger">
                    <span>Voucher giảm giá ({appliedVoucher?.code}):</span>
                    <strong className="text-danger">-{formatVND(voucherDiscountAmount)}</strong>
                  </div>
                )}
                <div className="d-flex justify-content-between">
                  <span>Phí vận chuyển:</span>
                  <span className="text-success fw-semibold">Miễn phí vận chuyển</span>
                </div>
                <div className="d-flex justify-content-between border-top pt-3 fs-5">
                  <span className="fw-bold text-dark">
                    {purchaseMode === 'Installment' ? 'Cần trả ngay hôm nay (30%):' : 'Tổng thanh toán:'}
                  </span>
                  <span className="fw-bold text-danger fs-4">
                    {formatVND(purchaseMode === 'Installment' ? prepayAmount : finalPayable)}
                  </span>
                </div>
              </div>

              <div className="d-flex justify-content-between align-items-center border-top pt-3">
                <div className="small text-muted" style={{ maxWidth: '60%' }}>
                  Nhấn "Đặt hàng" đồng nghĩa với việc bạn đồng ý tuân theo{' '}
                  <span className="text-primary" style={{ cursor: 'pointer' }}>Điều khoản Laptop World</span>.
                </div>
                <button
                  type="button"
                  className="shopee-btn-buy"
                  style={{ minWidth: '220px' }}
                  disabled={isSubmitting || checkedItems.length === 0}
                  onClick={handleConfirmOrder}
                >
                  {isSubmitting ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-2" role="status"></span>
                      Đang đặt hàng...
                    </>
                  ) : (
                    'Đặt Hàng'
                  )}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* 4. MODAL CHỌN VOUCHER (LIÊN KẾT TỪ /api/promotions) */}
      {showVoucherModal && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1070 }}>
          <div className="modal-dialog modal-dialog-centered" style={{ maxWidth: '520px' }}>
            <div className="modal-content border-0 shadow-lg rounded-4 overflow-hidden">
              <div className="modal-header border-bottom py-3">
                <h5 className="modal-title fw-bold text-dark fs-6 d-flex align-items-center gap-2">
                  <i className="bi bi-ticket-detailed text-danger fs-5"></i>
                  <span>Chọn Laptop World Voucher</span>
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowVoucherModal(false)}></button>
              </div>

              <div className="modal-body p-3">
                {/* Ô nhập mã Voucher trực tiếp */}
                <div className="input-group input-group-sm mb-3">
                  <input
                    type="text"
                    className="form-control"
                    placeholder="Nhập mã voucher (VD: LAPTOP2026, GAMING500K...)"
                    value={voucherInputCode}
                    onChange={(e) => setVoucherInputCode(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && handleApplyCustomVoucher()}
                  />
                  <button type="button" className="btn btn-danger px-3 fw-bold" onClick={handleApplyCustomVoucher}>
                    Áp Dụng
                  </button>
                </div>

                {voucherError && <div className="alert alert-danger py-2 px-3 small mb-3">{voucherError}</div>}

                <div className="small fw-bold text-secondary mb-2">Mã giảm giá đang có sẵn:</div>

                {/* Danh sách các voucher săn được từ API */}
                <div className="d-flex flex-column gap-2" style={{ maxHeight: '360px', overflowY: 'auto' }}>
                  {availablePromotions.length > 0 ? (
                    availablePromotions.map((p) => {
                      const isEligible = !p.minOrderAmount || totalPayable >= p.minOrderAmount;
                      const isSelected = appliedVoucher?.code === p.code;

                      return (
                        <div
                          key={p.discountId || p.code}
                          className={`shopee-voucher-ticket p-3 d-flex align-items-center justify-content-between ${
                            !isEligible ? 'opacity-50' : ''
                          }`}
                        >
                          <div>
                            <div className="d-flex align-items-center gap-2 mb-1">
                              <span className="badge bg-danger font-monospace px-2 py-1">{p.code}</span>
                              <span className="badge bg-danger-subtle text-danger">{p.valueBadge}</span>
                            </div>
                            <div className="fw-bold text-dark small">{p.title}</div>
                            <div className="text-muted" style={{ fontSize: '11px' }}>
                              {p.description}
                            </div>
                            <div className="text-secondary mt-1" style={{ fontSize: '10px' }}>
                              {p.minOrderAmount > 0 ? `Đơn tối thiểu ${formatVND(p.minOrderAmount)} • ` : ''}
                              Hạn dùng: {p.endDateFormatted}
                            </div>
                          </div>

                          <button
                            type="button"
                            className={`btn btn-sm ${
                              isSelected ? 'btn-success' : isEligible ? 'btn-outline-danger' : 'btn-light disabled'
                            }`}
                            onClick={() => handleApplyVoucher(p)}
                            disabled={!isEligible}
                          >
                            {isSelected ? '✓ Đang dùng' : 'Áp dụng'}
                          </button>
                        </div>
                      );
                    })
                  ) : (
                    <div className="text-center py-4 text-muted small">
                      {isLoadingPromotions ? 'Đang tải danh sách voucher...' : 'Hiện chưa có voucher nào khả dụng.'}
                    </div>
                  )}
                </div>
              </div>

              <div className="modal-footer border-top py-2">
                <button type="button" className="btn btn-sm btn-secondary" onClick={() => setShowVoucherModal(false)}>
                  Đóng
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* 5. MODAL VIETQR TỰ ĐỘNG */}
      {showQrModal && pendingOrder && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.6)', zIndex: 1080 }}>
          <div className="modal-dialog modal-dialog-centered" style={{ maxWidth: '500px' }}>
            <div className="modal-content border-0 shadow-lg rounded-4 overflow-hidden">
              <div className="modal-header bg-danger text-white border-0 py-3">
                <h5 className="modal-title fw-bold fs-6 m-0 d-flex align-items-center gap-2">
                  <i className="bi bi-qr-code-scan"></i> Quét mã VietQR để thanh toán
                </h5>
                <button type="button" className="btn-close btn-close-white" onClick={handleCloseQrModal}></button>
              </div>

              <div className="modal-body p-4 text-center">
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

                <div className="d-flex gap-2">
                  <button type="button" className="btn btn-light w-50" onClick={handleCloseQrModal}>
                    Đóng
                  </button>
                  <button
                    type="button"
                    className="btn btn-danger w-50 fw-bold"
                    style={{ backgroundColor: '#e11b1e' }}
                    onClick={handleCloseQrModal}
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

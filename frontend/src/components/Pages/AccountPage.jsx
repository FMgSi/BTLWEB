import React, { useState, useEffect } from 'react';
import { formatVND } from '../../utils/formatters';

export default function AccountPage({ currentUser, onGoHome, onOpenLogin, onNavigateToService }) {
  const [activeTab, setActiveTab] = useState('orders'); // 'orders' | 'installments' | 'membership'
  const [orders, setOrders] = useState([]);
  const [isLoadingOrders, setIsLoadingOrders] = useState(false);
  const [orderError, setOrderError] = useState('');

  // Installments state & payment modal
  const [payingModal, setPayingModal] = useState(null); // { type: 'period' | 'prepay', order, contract, period }
  const [payingMethod, setPayingMethod] = useState('cash'); // 'cash' | 'transfer' | 'card'
  const [payingNote, setPayingNote] = useState('');
  const [isSubmittingPayment, setIsSubmittingPayment] = useState(false);
  const [paymentSuccessMsg, setPaymentSuccessMsg] = useState('');
  const [paymentErrorMsg, setPaymentErrorMsg] = useState('');

  const STORE_BANK_ACCOUNT = {
    bankId: 'MB',
    accountNo: '0354120145',
    accountName: 'HOANG BA TRUNG',
  };

  // Guest lookup
  const [guestCode, setGuestCode] = useState('');
  const [guestPhone, setGuestPhone] = useState('');
  const [guestOrderResult, setGuestOrderResult] = useState(null);
  const [isLookingUpGuest, setIsLookingUpGuest] = useState(false);
  const [guestLookupError, setGuestLookupError] = useState('');

  // Devices
  const [devices, setDevices] = useState([]);
  const [isLoadingDevices, setIsLoadingDevices] = useState(false);
  const [deviceError, setDeviceError] = useState('');
  const [devicePhone, setDevicePhone] = useState(currentUser?.phoneNumber || '');

  // 1. Tải đơn hàng của người dùng khi đã đăng nhập
  useEffect(() => {
    if (currentUser) {
      loadUserOrders();
    }
  }, [currentUser]);

  const installmentOrders = orders.filter(
    (o) => o.purchaseMode === 'Installment' || o.installmentContract != null
  );

  const handleConfirmInstallmentPayment = async () => {
    if (!payingModal) return;
    setIsSubmittingPayment(true);
    setPaymentErrorMsg('');
    setPaymentSuccessMsg('');
    try {
      const isPrepay = payingModal.type === 'prepay';
      const endpoint = isPrepay
        ? '/api/orders/pay-installment-prepay'
        : '/api/orders/pay-installment-period';

      const paymentMethodName =
        payingMethod === 'cash' ? 'Tiền mặt' : payingMethod === 'card' ? 'Thẻ ngân hàng' : 'Chuyển khoản';

      const payload = isPrepay
        ? {
            contractId: payingModal.contract?.contractId || payingModal.order?.installmentContract?.contractId,
            paymentMethod: paymentMethodName,
            note: payingNote || `Khách hàng thanh toán trả trước 30% bằng ${paymentMethodName}`,
          }
        : {
            contractId: payingModal.contract?.contractId || payingModal.order?.installmentContract?.contractId,
            periodNumber: payingModal.period?.periodNumber,
            paymentMethod: paymentMethodName,
            note: payingNote || `Khách hàng thanh toán kỳ ${payingModal.period?.periodNumber} bằng ${paymentMethodName}`,
          };

      const res = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(payload),
      });

      const data = await res.json();
      if (res.ok && data.success) {
        setPaymentSuccessMsg(data.message || 'Thanh toán thành công!');
        setTimeout(() => {
          setPayingModal(null);
          setPaymentSuccessMsg('');
          loadUserOrders();
        }, 1200);
      } else {
        setPaymentErrorMsg(data.message || 'Thanh toán không thành công. Vui lòng thử lại!');
      }
    } catch {
      setPaymentErrorMsg('Lỗi kết nối máy chủ khi xử lý thanh toán.');
    } finally {
      setIsSubmittingPayment(false);
    }
  };

  const loadUserOrders = async () => {
    setIsLoadingOrders(true);
    setOrderError('');
    try {
      const res = await fetch('/api/orders/my-orders', { credentials: 'include' });
      const data = await res.json();
      if (res.ok && data.success) {
        setOrders(data.orders || []);
      } else {
        setOrderError(data.message || 'Không thể tải lịch sử đơn hàng.');
      }
    } catch {
      setOrderError('Lỗi kết nối máy chủ khi tải đơn hàng.');
    } finally {
      setIsLoadingOrders(false);
    }
  };

  const loadDevices = async (phone) => {
    setIsLoadingDevices(true);
    setDeviceError('');
    try {
      const params = new URLSearchParams();
      if (phone) params.append('phoneNumber', phone);
      if (currentUser?.id) params.append('userId', currentUser.id);
      const url = params.toString() ? `/api/services/my-devices?${params.toString()}` : '/api/services/my-devices';
      const res = await fetch(url, { credentials: 'include' });
      const data = await res.json();
      if (res.ok && data.success) {
        setDevices(data.devices || []);
      } else {
        setDeviceError(data.message || 'Không thể tải danh sách thiết bị đã mua.');
      }
    } catch {
      setDeviceError('Lỗi kết nối máy chủ khi tải thiết bị đã mua.');
    } finally {
      setIsLoadingDevices(false);
    }
  };

  // Tra cứu đơn hàng cho khách vãng lai
  const handleGuestLookup = async (e) => {
    if (e) e.preventDefault();
    setGuestLookupError('');
    setGuestOrderResult(null);

    const cleanCode = guestCode.trim();
    if (!cleanCode) {
      setGuestLookupError('Vui lòng nhập mã đơn hàng (Ví dụ: PC20261103-0001).');
      return;
    }

    setIsLookingUpGuest(true);
    try {
      const phoneParam = guestPhone.trim() ? `?phone=${encodeURIComponent(guestPhone.trim())}` : '';
      const res = await fetch(`/api/orders/code/${encodeURIComponent(cleanCode)}${phoneParam}`);
      const data = await res.json();

      if (!res.ok || !data.success) {
        setGuestLookupError(data.message || 'Không tìm thấy đơn hàng với thông tin đã nhập.');
      } else {
        setGuestOrderResult(data.order);
      }
    } catch {
      setGuestLookupError('Lỗi kết nối máy chủ khi tra cứu đơn hàng.');
    } finally {
      setIsLookingUpGuest(false);
    }
  };

  // Tính toán tổng chi tiêu từ các đơn hàng HOÀN THÀNH để xác định Hạng thành viên
  const completedOrders = orders.filter((o) => o.status === 'Hoàn thành' || o.status === 'Đã giao');
  const totalSpent = completedOrders.reduce((sum, o) => sum + (o.totalAmount || 0), 0);

  // Quy tắc hạng thành viên
  let memberTier = 'Đồng';
  let tierColor = '#78716c';
  let tierBadgeBg = 'bg-secondary';
  let nextTier = 'Bạc';
  let nextTierAmount = 20000000;
  let progressPercent = 0;

  if (totalSpent >= 100000000) {
    memberTier = 'Kim Cương';
    tierColor = '#0284c7';
    tierBadgeBg = 'bg-info text-dark';
    nextTier = 'Tối Đa';
    nextTierAmount = 100000000;
    progressPercent = 100;
  } else if (totalSpent >= 50000000) {
    memberTier = 'Vàng';
    tierColor = '#eab308';
    tierBadgeBg = 'bg-warning text-dark';
    nextTier = 'Kim Cương';
    nextTierAmount = 100000000;
    progressPercent = Math.min(100, Math.round(((totalSpent - 50000000) / 50000000) * 100));
  } else if (totalSpent >= 20000000) {
    memberTier = 'Bạc';
    tierColor = '#94a3b8';
    tierBadgeBg = 'bg-primary';
    nextTier = 'Vàng';
    nextTierAmount = 50000000;
    progressPercent = Math.min(100, Math.round(((totalSpent - 20000000) / 30000000) * 100));
  } else {
    memberTier = 'Đồng';
    tierColor = '#a87552';
    tierBadgeBg = 'bg-secondary';
    nextTier = 'Bạc';
    nextTierAmount = 20000000;
    progressPercent = Math.min(100, Math.round((totalSpent / 20000000) * 100));
  }

  const remainingToNextTier = Math.max(0, nextTierAmount - totalSpent);

  const getStatusBadge = (status) => {
    switch (status) {
      case 'Mới':
        return <span className="badge bg-primary">Mới tiếp nhận</span>;
      case 'Đang xử lý':
        return <span className="badge bg-warning text-dark">Đang chuẩn bị hàng</span>;
      case 'Đang giao':
        return <span className="badge bg-info text-dark">Đang vận chuyển</span>;
      case 'Hoàn thành':
      case 'Đã giao':
        return <span className="badge bg-success">Giao thành công</span>;
      case 'Đã hủy':
        return <span className="badge bg-danger">Đã hủy</span>;
      default:
        return <span className="badge bg-secondary">{status}</span>;
    }
  };

  return (
    <div className="bg-light py-4" style={{ minHeight: '80vh' }}>
      <div className="site-container">
        {/* Breadcrumb */}
        <div className="mb-3 text-muted small d-flex align-items-center gap-2">
          <a
            href="/"
            onClick={(e) => {
              e.preventDefault();
              onGoHome();
            }}
            className="text-decoration-none text-secondary"
          >
            Trang chủ
          </a>
          <span>/</span>
          <span className="text-dark fw-semibold">Tài khoản cá nhân</span>
        </div>

        {/* 1. PROFILE & MEMBERSHIP HEADER CARD */}
        <div className="bg-white rounded-3 shadow-sm p-4 mb-4 border">
          <div className="row align-items-center g-3">
            <div className="col-lg-7 d-flex align-items-center gap-3">
              <div
                className="rounded-circle d-flex align-items-center justify-content-center text-white fw-bold shadow-sm flex-shrink-0"
                style={{
                  width: '64px',
                  height: '64px',
                  fontSize: '24px',
                  backgroundColor: tierColor,
                }}
              >
                {(currentUser?.fullName || currentUser?.username || 'K')[0].toUpperCase()}
              </div>
              <div>
                <div className="d-flex align-items-center gap-2 flex-wrap">
                  <h4 className="fw-bold text-dark mb-0">
                    {currentUser?.fullName || currentUser?.username || 'Khách hàng vãng lai'}
                  </h4>
                  <span className={`badge ${tierBadgeBg} px-2 py-1 fw-bold text-uppercase`}>
                    Hạng {memberTier}
                  </span>
                </div>
                <div className="text-secondary small mt-1">
                  {currentUser?.email && <span className="me-3"><i className="bi bi-envelope me-1"></i>{currentUser.email}</span>}
                  {currentUser?.phoneNumber && <span><i className="bi bi-telephone me-1"></i>{currentUser.phoneNumber}</span>}
                  {!currentUser && <span>Chưa đăng nhập. Bạn có thể tra cứu đơn hàng hoặc đăng nhập để đồng bộ.</span>}
                </div>
              </div>
            </div>

            <div className="col-lg-5">
              <div className="bg-light p-3 rounded-3 border">
                <div className="d-flex justify-content-between align-items-center mb-1">
                  <span className="small text-muted fw-semibold">Tổng chi tiêu tích lũy:</span>
                  <strong className="text-danger fs-6">{formatVND(totalSpent)}</strong>
                </div>
                {nextTier !== 'Tối Đa' ? (
                  <>
                    <div className="progress mt-2" style={{ height: '8px' }}>
                      <div
                        className="progress-bar bg-danger"
                        role="progressbar"
                        style={{ width: `${progressPercent}%` }}
                        aria-valuenow={progressPercent}
                        aria-valuemin="0"
                        aria-valuemax="100"
                      ></div>
                    </div>
                    <div className="d-flex justify-content-between small text-muted mt-1" style={{ fontSize: '11px' }}>
                      <span>Hạng {memberTier}</span>
                      <span>
                        Cần thêm <strong>{formatVND(remainingToNextTier)}</strong> để lên <strong>Hạng {nextTier}</strong>
                      </span>
                    </div>
                  </>
                ) : (
                  <div className="small text-success fw-semibold mt-1">
                    <i className="bi bi-gem me-1"></i> Quý khách đang ở Hạng Thành Viên Cao Nhất!
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* 2. NAVIGATION TABS */}
        <div className="d-flex flex-wrap gap-2 mb-4 bg-white p-2 rounded-3 shadow-sm border">
          <button
            type="button"
            className={`btn btn-sm px-4 py-2 fw-bold rounded-2 ${
              activeTab === 'orders' ? 'btn-danger text-white' : 'btn-light text-secondary'
            }`}
            onClick={() => setActiveTab('orders')}
          >
            <i className="bi bi-bag-check me-1"></i> 1. Đơn hàng của tôi ({orders.length})
          </button>
          <button
            type="button"
            className={`btn btn-sm px-4 py-2 fw-bold rounded-2 ${
              activeTab === 'installments' ? 'btn-danger text-white' : 'btn-light text-secondary'
            }`}
            onClick={() => setActiveTab('installments')}
          >
            <i className="bi bi-credit-card-2-front me-1"></i> 2. Trả góp sản phẩm đã mua ({installmentOrders.length})
          </button>
          <button
            type="button"
            className={`btn btn-sm px-4 py-2 fw-bold rounded-2 ${
              activeTab === 'membership' ? 'btn-danger text-white' : 'btn-light text-secondary'
            }`}
            onClick={() => setActiveTab('membership')}
          >
            <i className="bi bi-award me-1"></i> 3. Hạng thành viên & Quyền lợi
          </button>
        </div>

        {/* TAB 1: ĐƠN HÀNG CỦA TÔI */}
        {activeTab === 'orders' && (
          <div>
            {!currentUser && (
              <div className="bg-white rounded-3 p-4 mb-4 border shadow-sm">
                <div className="d-flex align-items-center justify-content-between flex-wrap gap-3 mb-3">
                  <div>
                    <h5 className="fw-bold text-dark mb-1">
                      <i className="bi bi-search text-danger me-2"></i>Tra cứu tiến độ đơn hàng (Khách vãng lai)
                    </h5>
                    <p className="text-muted small mb-0">
                      Nhập mã đơn hàng và số điện thoại đã đặt để kiểm tra trạng thái vận chuyển và thông tin hợp đồng.
                    </p>
                  </div>
                  {onOpenLogin && (
                    <button
                      type="button"
                      className="btn btn-outline-danger btn-sm fw-semibold"
                      onClick={() => onOpenLogin('login')}
                    >
                      <i className="bi bi-person-lock me-1"></i> Đăng nhập để tự động xem
                    </button>
                  )}
                </div>

                <form onSubmit={handleGuestLookup} className="row g-2">
                  <div className="col-md-5">
                    <input
                      type="text"
                      className="form-control"
                      placeholder="Mã đơn hàng (Ví dụ: PC20261103-0001)"
                      value={guestCode}
                      onChange={(e) => setGuestCode(e.target.value.toUpperCase())}
                    />
                  </div>
                  <div className="col-md-5">
                    <input
                      type="tel"
                      className="form-control"
                      placeholder="Số điện thoại đặt hàng (để mở khóa địa chỉ nhận)"
                      value={guestPhone}
                      onChange={(e) => setGuestPhone(e.target.value)}
                    />
                  </div>
                  <div className="col-md-2">
                    <button
                      type="submit"
                      className="btn btn-danger w-100 fw-semibold"
                      disabled={isLookingUpGuest}
                    >
                      {isLookingUpGuest ? 'Đang tìm...' : 'Tra cứu ngay'}
                    </button>
                  </div>
                </form>

                {guestLookupError && (
                  <div className="alert alert-danger py-2 px-3 mt-3 mb-0 small">
                    <i className="bi bi-exclamation-triangle-fill me-1"></i> {guestLookupError}
                  </div>
                )}

                {guestOrderResult && (
                  <div className="card mt-3 border-danger shadow-sm">
                    <div className="card-header bg-danger-subtle d-flex justify-content-between align-items-center">
                      <span className="fw-bold text-danger">Đơn hàng #{guestOrderResult.orderCode}</span>
                      {getStatusBadge(guestOrderResult.status)}
                    </div>
                    <div className="card-body p-3">
                      <div className="row g-3">
                        <div className="col-md-6 small">
                          <div><strong>Người nhận:</strong> {guestOrderResult.customerName}</div>
                          <div><strong>Số điện thoại:</strong> {guestOrderResult.phoneNumber}</div>
                          <div><strong>Địa chỉ:</strong> {guestOrderResult.shippingAddress}</div>
                          <div><strong>Hình thức:</strong> {guestOrderResult.paymentMethod}</div>
                        </div>
                        <div className="col-md-6 text-md-end small">
                          <div className="text-muted">Ngày đặt: {guestOrderResult.createdAt ? new Date(guestOrderResult.createdAt).toLocaleString('vi-VN') : 'Mới'}</div>
                          <div className="fs-5 fw-bold text-danger mt-1">Tổng tiền: {formatVND(guestOrderResult.totalAmount)}</div>
                        </div>
                      </div>

                      <div className="table-responsive mt-3 border rounded">
                        <table className="table table-sm align-middle mb-0">
                          <thead className="table-light small">
                            <tr>
                              <th>Sản phẩm</th>
                              <th className="text-center">Cấu hình</th>
                              <th className="text-center">Số lượng</th>
                              <th className="text-end">Đơn giá</th>
                            </tr>
                          </thead>
                          <tbody>
                            {guestOrderResult.items?.map((item, idx) => (
                              <tr key={idx}>
                                <td>
                                  <div className="fw-semibold text-dark small">{item.productName}</div>
                                  {item.colorName && <span className="badge bg-light text-muted border">Màu: {item.colorName}</span>}
                                </td>
                                <td className="text-center small text-muted">{item.selectedSpecs || 'Tiêu chuẩn'}</td>
                                <td className="text-center small">{item.quantity}</td>
                                <td className="text-end small fw-bold text-danger">{formatVND(item.unitPrice)}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            )}

            {currentUser && (
              <div>
                {isLoadingOrders ? (
                  <div className="text-center py-5 bg-white rounded-3 shadow-sm border">
                    <div className="spinner-border text-danger" role="status"></div>
                    <div className="small text-muted mt-2">Đang tải lịch sử đơn hàng...</div>
                  </div>
                ) : orderError ? (
                  <div className="alert alert-danger py-3 px-4 rounded-3 shadow-sm">
                    <i className="bi bi-exclamation-triangle-fill me-2"></i> {orderError}
                    <button
                      type="button"
                      className="btn btn-sm btn-outline-danger ms-3"
                      onClick={loadUserOrders}
                    >
                      Thử lại
                    </button>
                  </div>
                ) : orders.length === 0 ? (
                  <div className="text-center py-5 bg-white rounded-3 shadow-sm border">
                    <i className="bi bi-bag-x text-muted fs-1 d-block mb-2"></i>
                    <h6 className="fw-bold text-dark">Bạn chưa có đơn đặt hàng nào</h6>
                    <p className="text-muted small mb-3">Khám phá các dòng laptop chính hãng mới nhất tại cửa hàng.</p>
                    <a href="/products" className="btn btn-danger btn-sm px-4 fw-semibold">
                      Mua sắm ngay
                    </a>
                  </div>
                ) : (
                  <div className="d-flex flex-column gap-3">
                    {orders.map((order) => (
                      <div key={order.orderId || order.orderCode} className="card shadow-sm border rounded-3 overflow-hidden">
                        <div className="card-header bg-white py-3 border-bottom d-flex flex-wrap justify-content-between align-items-center gap-2">
                          <div>
                            <span className="fw-bold text-dark me-2">Mã đơn: #{order.orderCode}</span>
                            <span className="text-muted small">
                              • Ngày đặt: {order.createdAt ? new Date(order.createdAt).toLocaleDateString('vi-VN') : 'Gần đây'}
                            </span>
                          </div>
                          <div className="d-flex align-items-center gap-2">
                            {getStatusBadge(order.status)}
                            <span className="badge bg-light text-dark border">
                              {order.paymentMethod || 'COD'}
                            </span>
                          </div>
                        </div>

                        <div className="card-body p-3">
                          <div className="table-responsive">
                            <table className="table table-sm align-middle mb-0">
                              <thead className="table-light small">
                                <tr>
                                  <th>Sản phẩm</th>
                                  <th className="text-center">Cấu hình</th>
                                  <th className="text-center">SL</th>
                                  <th className="text-end">Đơn giá</th>
                                  <th className="text-end">Thành tiền</th>
                                </tr>
                              </thead>
                              <tbody>
                                {order.items?.map((item, idx) => (
                                  <tr key={idx}>
                                    <td>
                                      <div className="d-flex align-items-center gap-2">
                                        {item.thumbnailUrl && (
                                          <img
                                            src={item.thumbnailUrl}
                                            alt={item.productName}
                                            style={{ width: '40px', height: '40px', objectFit: 'cover' }}
                                            className="rounded border"
                                          />
                                        )}
                                        <div>
                                          <div className="fw-semibold text-dark small">{item.productName}</div>
                                          {item.colorName && (
                                            <span className="badge bg-light text-muted border" style={{ fontSize: '10px' }}>
                                              {item.colorName}
                                            </span>
                                          )}
                                        </div>
                                      </div>
                                    </td>
                                    <td className="text-center small text-muted">{item.selectedSpecs || 'Tiêu chuẩn'}</td>
                                    <td className="text-center small">{item.quantity}</td>
                                    <td className="text-end small">{formatVND(item.unitPrice)}</td>
                                    <td className="text-end small fw-bold text-danger">
                                      {formatVND(item.unitPrice * item.quantity)}
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          </div>

                          <div className="d-flex flex-wrap justify-content-between align-items-center pt-3 border-top mt-3">
                            <div className="small text-muted">
                              <i className="bi bi-geo-alt me-1"></i> Giao đến: <strong>{order.shippingAddress}</strong>
                            </div>
                            <div className="text-end">
                              <span className="small text-muted me-2">Tổng thanh toán:</span>
                              <strong className="fs-5 text-danger">{formatVND(order.totalAmount)}</strong>
                            </div>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {/* TAB 2: TRẢ GÓP SẢN PHẨM ĐÃ MUA */}
        {activeTab === 'installments' && (
          <div>
            <div className="bg-white rounded-3 p-4 mb-3 border shadow-sm d-flex flex-wrap justify-content-between align-items-center gap-3">
              <div>
                <h5 className="fw-bold text-dark mb-1">
                  <i className="bi bi-credit-card-2-front text-danger me-2"></i>Quản lý hợp đồng & Thanh toán trả góp 0%
                </h5>
                <p className="text-muted small mb-0">
                  Theo dõi chi tiết các sản phẩm mua trả góp 0%, khoản trả trước 30%, số dư nợ còn lại và thanh toán trực tuyến các kỳ đến hạn.
                </p>
              </div>
              <button
                type="button"
                className="btn btn-sm btn-outline-danger fw-semibold"
                onClick={loadUserOrders}
                disabled={isLoadingOrders}
              >
                <i className="bi bi-arrow-clockwise me-1"></i> Làm mới dữ liệu
              </button>
            </div>

            {isLoadingOrders ? (
              <div className="text-center py-5 bg-white rounded-3 shadow-sm border">
                <div className="spinner-border text-danger" role="status"></div>
                <div className="small text-muted mt-2">Đang tải danh sách hợp đồng trả góp...</div>
              </div>
            ) : installmentOrders.length === 0 ? (
              <div className="text-center py-5 bg-white rounded-3 shadow-sm border">
                <i className="bi bi-credit-card text-muted fs-1 d-block mb-2"></i>
                <h6 className="fw-bold text-dark">Bạn chưa có sản phẩm nào mua theo hình thức trả góp</h6>
                <p className="text-muted small mb-3">
                  Laptop World hỗ trợ mua trả góp 0% lãi suất với thủ tục nhanh chóng, chỉ cần trả trước 30%.
                </p>
                <a href="/products" className="btn btn-danger btn-sm px-4 fw-semibold">
                  Khám phá Laptop trả góp 0%
                </a>
              </div>
            ) : (
              <div className="d-flex flex-column gap-4">
                {installmentOrders.map((order) => {
                  const contract = order.installmentContract;
                  const termMonths = contract?.termMonths || 6;
                  const totalAmount = order.totalAmount || 0;
                  const prepayAmount = contract?.prepayAmount || Math.round(totalAmount * 0.3);
                  const isPrepayPaid = contract?.prepayStatus === 'Đã thanh toán' || order.paymentStatus === 'Đang trả góp' || order.paymentStatus === 'Đã tất toán';
                  const totalPaid = contract?.totalPaid || (isPrepayPaid ? prepayAmount : 0);
                  const remainingDebt = contract?.remainingDebt ?? Math.max(0, totalAmount - totalPaid);
                  const schedules = contract?.schedules || [];
                  const contractStatus = contract?.status || (isPrepayPaid ? 'Đang trả góp' : 'Chờ trả trước');

                  return (
                    <div key={order.orderId || order.orderCode} className="card shadow-sm border rounded-3 overflow-hidden">
                      {/* HEADER HỢP ĐỒNG */}
                      <div className="card-header bg-white py-3 border-bottom d-flex flex-wrap justify-content-between align-items-center gap-2">
                        <div>
                          <span className="badge bg-danger text-white me-2">Trả góp 0%</span>
                          <span className="fw-bold text-dark me-2">
                            Mã HĐ: <span className="text-danger font-monospace">#{contract?.contractCode || ('HDTG-' + order.orderCode)}</span>
                          </span>
                          <span className="text-muted small">
                            (Đơn hàng: #{order.orderCode} • {order.createdAt ? new Date(order.createdAt).toLocaleDateString('vi-VN') : 'Mới'})
                          </span>
                        </div>
                        <div>
                          {contractStatus === 'Đã tất toán' ? (
                            <span className="badge bg-success text-white px-3 py-2 fw-semibold">
                              <i className="bi bi-check-circle me-1"></i> Đã tất toán hợp đồng
                            </span>
                          ) : contractStatus === 'Đang trả góp' ? (
                            <span className="badge bg-primary text-white px-3 py-2 fw-semibold">
                              <i className="bi bi-clock-history me-1"></i> Đang thực hiện trả góp ({contract?.paidPeriodsCount || 0}/{termMonths} kỳ)
                            </span>
                          ) : (
                            <span className="badge bg-warning text-dark px-3 py-2 fw-semibold">
                              <i className="bi bi-hourglass-split me-1"></i> Chờ thanh toán trả trước 30%
                            </span>
                          )}
                        </div>
                      </div>

                      {/* NỘI DUNG SẢN PHẨM & TÀI CHÍNH */}
                      <div className="card-body p-4">
                        {/* THÔNG TIN SẢN PHẨM */}
                        <div className="mb-3 pb-3 border-bottom">
                          <h6 className="fw-bold text-dark small text-uppercase mb-2 text-muted">Sản phẩm trả góp:</h6>
                          {order.items?.map((item, idx) => (
                            <div key={idx} className="d-flex align-items-center gap-3">
                              {item.thumbnailUrl && (
                                <img
                                  src={item.thumbnailUrl}
                                  alt={item.productName}
                                  style={{ width: '56px', height: '56px', objectFit: 'cover' }}
                                  className="rounded border"
                                />
                              )}
                              <div className="flex-grow-1">
                                <div className="fw-bold text-dark">{item.productName}</div>
                                <div className="text-muted small">
                                  {item.colorName && <span className="badge bg-light text-dark border me-2">Màu: {item.colorName}</span>}
                                  <span>{item.selectedSpecs || 'Cấu hình tiêu chuẩn'}</span>
                                  <span className="ms-2">• SL: <strong>{item.quantity}</strong></span>
                                </div>
                              </div>
                              <div className="text-end">
                                <div className="text-muted small">Đơn giá</div>
                                <div className="fw-bold text-dark">{formatVND(item.unitPrice)}</div>
                              </div>
                            </div>
                          ))}
                        </div>

                        {/* CHỈ SỐ TÀI CHÍNH */}
                        <div className="row g-2 mb-3">
                          <div className="col-6 col-md-3">
                            <div className="bg-light p-3 rounded-3 border text-center h-100">
                              <div className="text-muted small mb-1">Tổng giá trị đơn hàng</div>
                              <div className="fw-bold text-dark fs-6">{formatVND(totalAmount)}</div>
                            </div>
                          </div>
                          <div className="col-6 col-md-3">
                            <div className="bg-light p-3 rounded-3 border text-center h-100">
                              <div className="text-muted small mb-1">Khoản trả trước 30%</div>
                              <div className="fw-bold text-success fs-6">{formatVND(prepayAmount)}</div>
                              <div className="small mt-1">
                                {isPrepayPaid ? (
                                  <span className="badge bg-success-subtle text-success">✓ Đã thanh toán</span>
                                ) : (
                                  <span className="badge bg-warning-subtle text-warning-emphasis">Chờ thanh toán</span>
                                )}
                              </div>
                            </div>
                          </div>
                          <div className="col-6 col-md-3">
                            <div className="bg-light p-3 rounded-3 border text-center h-100">
                              <div className="text-muted small mb-1">Góp mỗi tháng ({termMonths} kỳ)</div>
                              <div className="fw-bold text-danger fs-6">
                                {formatVND(contract?.monthlyInstallmentAmount || Math.round((totalAmount - prepayAmount) / termMonths))}/tháng
                              </div>
                              <div className="small text-muted mt-1">Lãi suất: <strong>0%</strong></div>
                            </div>
                          </div>
                          <div className="col-6 col-md-3">
                            <div className="bg-light p-3 rounded-3 border text-center h-100">
                              <div className="text-muted small mb-1">Dư nợ còn lại</div>
                              <div className="fw-bold text-danger fs-6">{formatVND(remainingDebt)}</div>
                              <div className="small text-muted mt-1">Đã đóng: <strong>{formatVND(totalPaid)}</strong></div>
                            </div>
                          </div>
                        </div>

                        {/* NẾU ĐANG CHỜ TRẢ TRƯỚC 30% THÌ HIỆN NÚT THANH TOÁN TRẢ TRƯỚC */}
                        {!isPrepayPaid && (
                          <div className="alert alert-warning py-3 px-3 rounded-3 d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
                            <div>
                              <i className="bi bi-info-circle-fill me-2 fs-5 text-warning"></i>
                              <strong>Chưa thanh toán khoản trả trước 30% ({formatVND(prepayAmount)}).</strong>
                              <span className="text-muted small d-block">
                                Bạn có thể thanh toán trực tuyến ngay để kích hoạt hợp đồng và nhận máy, hoặc thanh toán tiền mặt khi nhân viên giao máy tận nơi.
                              </span>
                            </div>
                            <button
                              type="button"
                              className="btn btn-warning fw-bold btn-sm px-3 shadow-sm text-dark"
                              onClick={() => setPayingModal({ type: 'prepay', order, contract })}
                            >
                              <i className="bi bi-credit-card me-1"></i> Thanh toán 30% ngay ({formatVND(prepayAmount)})
                            </button>
                          </div>
                        )}

                        {/* BẢNG LỊCH CÁC KỲ TRẢ GÓP */}
                        <div className="mt-3">
                          <h6 className="fw-bold text-dark small text-uppercase mb-2">Lịch chi tiết các kỳ trả góp ({schedules.length} kỳ):</h6>
                          <div className="table-responsive border rounded-3">
                            <table className="table table-sm align-middle table-hover mb-0">
                              <thead className="table-light small">
                                <tr>
                                  <th className="text-center" style={{ width: '80px' }}>Kỳ</th>
                                  <th>Ngày đến hạn</th>
                                  <th className="text-end">Số tiền kỳ</th>
                                  <th className="text-center">Trạng thái</th>
                                  <th>Ngày nộp</th>
                                  <th className="text-end" style={{ width: '160px' }}>Thao tác</th>
                                </tr>
                              </thead>
                              <tbody>
                                {schedules.length === 0 ? (
                                  <tr>
                                    <td colSpan={6} className="text-center py-3 text-muted small">
                                      Lịch trả góp sẽ được chốt tự động sau khi thanh toán khoản trả trước 30%.
                                    </td>
                                  </tr>
                                ) : (
                                  schedules.map((schedule) => {
                                    const isPaid = schedule.status === 'Đã thanh toán';
                                    const isOverdue = schedule.status === 'Quá hạn';
                                    const canPay = !isPaid && contractStatus !== 'Đã hủy';

                                    return (
                                      <tr key={schedule.scheduleId || schedule.periodNumber} className={isPaid ? 'table-light' : ''}>
                                        <td className="text-center fw-bold">
                                          Kỳ {schedule.periodNumber}
                                        </td>
                                        <td className="small">
                                          {schedule.dueDate ? new Date(schedule.dueDate).toLocaleDateString('vi-VN') : '---'}
                                        </td>
                                        <td className="text-end fw-bold text-dark small">
                                          {formatVND(schedule.amount)}
                                        </td>
                                        <td className="text-center">
                                          {isPaid ? (
                                            <span className="badge bg-success-subtle text-success border border-success-subtle">
                                              ✓ Đã thanh toán
                                            </span>
                                          ) : isOverdue ? (
                                            <span className="badge bg-danger text-white">
                                              Quá hạn
                                            </span>
                                          ) : (
                                            <span className="badge bg-warning-subtle text-dark border border-warning-subtle">
                                              Chưa thanh toán
                                            </span>
                                          )}
                                        </td>
                                        <td className="small text-muted">
                                          {schedule.paidAt ? new Date(schedule.paidAt).toLocaleDateString('vi-VN') : '---'}
                                        </td>
                                        <td className="text-end">
                                          {isPaid ? (
                                            <span className="text-success small fw-semibold">
                                              <i className="bi bi-check-circle-fill me-1"></i> Đã xong
                                            </span>
                                          ) : canPay ? (
                                            <button
                                              type="button"
                                              className="btn btn-sm btn-danger fw-bold py-1 px-3"
                                              onClick={() => setPayingModal({ type: 'period', order, contract, period: schedule })}
                                            >
                                              <i className="bi bi-wallet2 me-1"></i> Thanh toán
                                            </button>
                                          ) : (
                                            <span className="text-muted small">Chờ kỳ trước</span>
                                          )}
                                        </td>
                                      </tr>
                                    );
                                  })
                                )}
                              </tbody>
                            </table>
                          </div>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}

        {/* TAB 3: HẠNG THÀNH VIÊN */}
        {activeTab === 'membership' && (
          <div className="row g-4">
            <div className="col-lg-4">
              <div className="bg-white rounded-3 shadow-sm p-4 border text-center">
                <div
                  className="rounded-circle d-inline-flex align-items-center justify-content-center text-white mb-3 shadow"
                  style={{ width: '80px', height: '80px', fontSize: '32px', backgroundColor: tierColor }}
                >
                  <i className="bi bi-gem"></i>
                </div>
                <h4 className="fw-bold text-dark mb-1">Hạng {memberTier}</h4>
                <p className="text-muted small mb-3">Tích lũy theo tổng giá trị các đơn hàng giao thành công.</p>
                <div className="border-top pt-3 text-start small">
                  <div className="d-flex justify-content-between mb-2">
                    <span className="text-muted">Tổng chi tiêu:</span>
                    <strong className="text-danger">{formatVND(totalSpent)}</strong>
                  </div>
                  <div className="d-flex justify-content-between mb-2">
                    <span className="text-muted">Số đơn hoàn tất:</span>
                    <strong>{completedOrders.length} đơn</strong>
                  </div>
                  {nextTier !== 'Tối Đa' && (
                    <div className="d-flex justify-content-between">
                      <span className="text-muted">Lên hạng tiếp:</span>
                      <strong>{formatVND(remainingToNextTier)}</strong>
                    </div>
                  )}
                </div>
              </div>
            </div>

            <div className="col-lg-8">
              <div className="bg-white rounded-3 shadow-sm p-4 border">
                <h5 className="fw-bold text-dark mb-3">
                  <i className="bi bi-trophy text-warning me-2"></i>Hệ thống Hạng & Quyền lợi Đặc quyền
                </h5>

                <div className="d-flex flex-column gap-3">
                  <div className={`p-3 rounded-3 border ${memberTier === 'Đồng' ? 'border-secondary bg-light' : ''}`}>
                    <div className="d-flex justify-content-between align-items-center mb-1">
                      <h6 className="fw-bold mb-0" style={{ color: '#a87552' }}>
                        🥉 Hạng Đồng (Chi tiêu &lt; 20 triệu)
                      </h6>
                      {memberTier === 'Đồng' && <span className="badge bg-secondary">Hạng hiện tại</span>}
                    </div>
                    <ul className="small text-muted mb-0 ps-3">
                      <li>Tích lũy lịch sử đơn hàng và theo dõi bảo hành trọn đời.</li>
                      <li>Hỗ trợ cài đặt phần mềm và hỗ trợ kỹ thuật trực tuyến miễn phí.</li>
                    </ul>
                  </div>

                  <div className={`p-3 rounded-3 border ${memberTier === 'Bạc' ? 'border-primary bg-primary-subtle' : ''}`}>
                    <div className="d-flex justify-content-between align-items-center mb-1">
                      <h6 className="fw-bold mb-0 text-primary">
                        🥈 Hạng Bạc (Từ 20 triệu - 50 triệu)
                      </h6>
                      {memberTier === 'Bạc' && <span className="badge bg-primary">Hạng hiện tại</span>}
                    </div>
                    <ul className="small text-muted mb-0 ps-3">
                      <li>Toàn bộ quyền lợi Hạng Đồng.</li>
                      <li>Miễn phí 01 lần vệ sinh, tra keo tản nhiệt máy tính mỗi năm.</li>
                      <li>Giảm ngay 5% khi mua phụ kiện (chuột, balo, sạc).</li>
                    </ul>
                  </div>

                  <div className={`p-3 rounded-3 border ${memberTier === 'Vàng' ? 'border-warning bg-warning-subtle' : ''}`}>
                    <div className="d-flex justify-content-between align-items-center mb-1">
                      <h6 className="fw-bold mb-0 text-warning-emphasis">
                        🥇 Hạng Vàng (Từ 50 triệu - 100 triệu)
                      </h6>
                      {memberTier === 'Vàng' && <span className="badge bg-warning text-dark">Hạng hiện tại</span>}
                    </div>
                    <ul className="small text-muted mb-0 ps-3">
                      <li>Toàn bộ quyền lợi Hạng Bạc.</li>
                      <li>Miễn phí vệ sinh máy trọn đời tại trung tâm dịch vụ.</li>
                      <li>Hỗ trợ mượn máy laptop tương đương khi gửi bảo hành quá 3 ngày.</li>
                      <li>Giảm 10% chi phí sửa chữa dịch vụ và thay thế linh kiện ngoài bảo hành.</li>
                    </ul>
                  </div>

                  <div className={`p-3 rounded-3 border ${memberTier === 'Kim Cương' ? 'border-info bg-info-subtle' : ''}`}>
                    <div className="d-flex justify-content-between align-items-center mb-1">
                      <h6 className="fw-bold mb-0 text-info-emphasis">
                        💎 Hạng Kim Cương (Từ 100 triệu trở lên)
                      </h6>
                      {memberTier === 'Kim Cương' && <span className="badge bg-info text-dark">Hạng hiện tại</span>}
                    </div>
                    <ul className="small text-muted mb-0 ps-3">
                      <li>Quyền lợi VIP độc quyền cao nhất toàn hệ thống.</li>
                      <li>Kỹ thuật viên phục vụ tận nơi tại khu vực Hà Nội & TP.HCM.</li>
                      <li>Chính sách 1 Đổi 1 đặc quyền trong 45 ngày nếu phát sinh lỗi phần cứng.</li>
                      <li>Giảm 15% khi mua phụ kiện và dịch vụ nâng cấp RAM, SSD.</li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* MODAL THANH TOÁN TRẢ GÓP TRỰC TUYẾN */}
      {payingModal && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1080 }}>
          <div className="modal-dialog modal-dialog-centered" style={{ maxWidth: '520px' }}>
            <div className="modal-content border-0 shadow-lg rounded-4 overflow-hidden">
              <div className="modal-header bg-dark text-white border-0 py-3">
                <h5 className="modal-title fs-6 fw-bold">
                  <i className="bi bi-credit-card-2-front text-danger me-2"></i>
                  {payingModal.type === 'prepay'
                    ? 'Thanh toán trả trước 30% hợp đồng trả góp'
                    : `Thanh toán kỳ trả góp số ${payingModal.period?.periodNumber}`}
                </h5>
                <button
                  type="button"
                  className="btn-close btn-close-white"
                  onClick={() => {
                    setPayingModal(null);
                    setPaymentErrorMsg('');
                    setPaymentSuccessMsg('');
                    setPayingNote('');
                  }}
                  disabled={isSubmittingPayment}
                ></button>
              </div>

              <div className="modal-body p-4 text-start">
                <div className="bg-light p-3 rounded-3 mb-3 border text-center">
                  <div className="text-muted small mb-1">
                    Đơn hàng: <strong>#{payingModal.order?.orderCode}</strong>
                    {payingModal.contract?.contractCode && ` • HĐ: #${payingModal.contract.contractCode}`}
                  </div>
                  <div className="text-muted small mb-1">Số tiền thanh toán:</div>
                  <div className="fs-3 fw-bold text-danger">
                    {formatVND(
                      payingModal.type === 'prepay'
                        ? (payingModal.contract?.prepayAmount || Math.round((payingModal.order?.totalAmount || 0) * 0.3))
                        : (payingModal.period?.amount || 0)
                    )}
                  </div>
                </div>

                {/* Chọn phương thức */}
                <label className="form-label small fw-bold text-dark mb-2">Chọn phương thức thanh toán:</label>
                <div className="row g-2 mb-3">
                  <div className="col-4">
                    <div
                      className={`p-3 rounded border text-center cursor-pointer h-100 ${payingMethod === 'cash' ? 'border-danger bg-danger-subtle fw-bold text-danger' : 'bg-white text-dark'}`}
                      style={{ cursor: 'pointer' }}
                      onClick={() => setPayingMethod('cash')}
                    >
                      <i className="bi bi-cash d-block fs-4 mb-1"></i>
                      <span className="small">Tiền mặt</span>
                    </div>
                  </div>
                  <div className="col-4">
                    <div
                      className={`p-3 rounded border text-center cursor-pointer h-100 ${payingMethod === 'transfer' ? 'border-danger bg-danger-subtle fw-bold text-danger' : 'bg-white text-dark'}`}
                      style={{ cursor: 'pointer' }}
                      onClick={() => setPayingMethod('transfer')}
                    >
                      <i className="bi bi-bank d-block fs-4 mb-1"></i>
                      <span className="small">Chuyển khoản</span>
                    </div>
                  </div>
                  <div className="col-4">
                    <div
                      className={`p-3 rounded border text-center cursor-pointer h-100 ${payingMethod === 'card' ? 'border-danger bg-danger-subtle fw-bold text-danger' : 'bg-white text-dark'}`}
                      style={{ cursor: 'pointer' }}
                      onClick={() => setPayingMethod('card')}
                    >
                      <i className="bi bi-credit-card d-block fs-4 mb-1"></i>
                      <span className="small">Thẻ ngân hàng</span>
                    </div>
                  </div>
                </div>

                {/* Ghi chú */}
                <div className="mb-3">
                  <label className="form-label small text-muted mb-1">Ghi chú thanh toán (tùy chọn):</label>
                  <input
                    type="text"
                    className="form-control form-control-sm"
                    placeholder="Mã giao dịch ngân hàng / Lời nhắn..."
                    value={payingNote}
                    onChange={(e) => setPayingNote(e.target.value)}
                  />
                </div>

                {paymentErrorMsg && (
                  <div className="alert alert-danger py-2 px-3 small mb-3">
                    <i className="bi bi-exclamation-triangle-fill me-1"></i> {paymentErrorMsg}
                  </div>
                )}

                {paymentSuccessMsg && (
                  <div className="alert alert-success py-2 px-3 small mb-3">
                    <i className="bi bi-check-circle-fill me-1"></i> {paymentSuccessMsg}
                  </div>
                )}

                <div className="d-flex justify-content-end gap-2 mt-4">
                  <button
                    type="button"
                    className="btn btn-outline-secondary btn-sm px-3"
                    onClick={() => {
                      setPayingModal(null);
                      setPaymentErrorMsg('');
                      setPaymentSuccessMsg('');
                      setPayingNote('');
                    }}
                    disabled={isSubmittingPayment}
                  >
                    Đóng
                  </button>
                  <button
                    type="button"
                    className="btn btn-danger btn-sm px-4 fw-bold"
                    onClick={handleConfirmInstallmentPayment}
                    disabled={isSubmittingPayment}
                  >
                    {isSubmittingPayment ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2" role="status"></span>
                        Đang ghi nhận...
                      </>
                    ) : (
                      'Xác nhận đã thanh toán'
                    )}
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


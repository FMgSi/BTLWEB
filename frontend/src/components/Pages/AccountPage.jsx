import React, { useState, useEffect } from 'react';
import { formatVND } from '../../utils/formatters';

export default function AccountPage({ currentUser, onGoHome, onOpenLogin, onNavigateToService }) {
  const [activeTab, setActiveTab] = useState('orders'); // 'orders' | 'devices' | 'membership'
  const [orders, setOrders] = useState([]);
  const [isLoadingOrders, setIsLoadingOrders] = useState(false);
  const [orderError, setOrderError] = useState('');

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

  // 2. Tải thiết bị đã mua
  useEffect(() => {
    if (activeTab === 'devices') {
      loadDevices(devicePhone || currentUser?.phoneNumber);
    }
  }, [activeTab, currentUser]);

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
      const url = phone
        ? `/api/services/my-devices?phoneNumber=${encodeURIComponent(phone)}`
        : '/api/services/my-devices';
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
              activeTab === 'devices' ? 'btn-danger text-white' : 'btn-light text-secondary'
            }`}
            onClick={() => setActiveTab('devices')}
          >
            <i className="bi bi-laptop me-1"></i> 2. Sản phẩm đã mua & Bảo hành ({devices.length})
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

        {/* TAB 2: SẢN PHẨM ĐÃ MUA & BẢO HÀNH */}
        {activeTab === 'devices' && (
          <div>
            <div className="bg-white rounded-3 p-4 mb-3 border shadow-sm d-flex flex-wrap justify-content-between align-items-center gap-3">
              <div>
                <h5 className="fw-bold text-dark mb-1">
                  <i className="bi bi-shield-check text-success me-2"></i>Thiết bị đã bàn giao & Thời hạn bảo hành
                </h5>
                <p className="text-muted small mb-0">
                  Mỗi chiếc laptop khi bàn giao đều được liên kết số Serial định danh duy nhất và theo dõi thời hạn bảo hành chính xác theo ngày.
                </p>
              </div>
              <div className="d-flex gap-2 align-items-center">
                <input
                  type="tel"
                  className="form-control form-control-sm"
                  placeholder="Lọc theo Số điện thoại..."
                  value={devicePhone}
                  onChange={(e) => setDevicePhone(e.target.value)}
                />
                <button
                  type="button"
                  className="btn btn-sm btn-outline-secondary"
                  onClick={() => loadDevices(devicePhone)}
                >
                  Tải lại
                </button>
              </div>
            </div>

            {isLoadingDevices ? (
              <div className="text-center py-5 bg-white rounded-3 shadow-sm border">
                <div className="spinner-border text-danger" role="status"></div>
                <div className="small text-muted mt-2">Đang tải danh sách thiết bị...</div>
              </div>
            ) : deviceError ? (
              <div className="alert alert-danger py-3 px-4 rounded-3 shadow-sm">
                <i className="bi bi-exclamation-triangle-fill me-2"></i> {deviceError}
              </div>
            ) : devices.length === 0 ? (
              <div className="text-center py-5 bg-white rounded-3 shadow-sm border">
                <i className="bi bi-laptop text-muted fs-1 d-block mb-2"></i>
                <h6 className="fw-bold text-dark">Chưa có thiết bị nào được ghi nhận</h6>
                <p className="text-muted small mb-3">
                  Khi đơn hàng của bạn được giao thành công và nhân viên cấp số Serial cho máy, thiết bị sẽ xuất hiện tại đây.
                </p>
              </div>
            ) : (
              <div className="row g-3">
                {devices.map((device) => {
                  const isUnderWarranty = device.isUnderWarranty;
                  const daysRemaining = device.daysRemaining || 0;
                  return (
                    <div key={device.deviceId} className="col-md-6 col-lg-4">
                      <div className="card h-100 shadow-sm border rounded-3 p-3 d-flex flex-column justify-content-between">
                        <div>
                          <div className="d-flex justify-content-between align-items-start mb-2">
                            <span className="badge bg-light text-dark border font-monospace">
                              Mã đơn: #{device.orderCode}
                            </span>
                            {isUnderWarranty ? (
                              <span className="badge bg-success-subtle text-success border border-success-subtle fw-semibold">
                                <i className="bi bi-shield-fill-check me-1"></i> Còn BH ({daysRemaining} ngày)
                              </span>
                            ) : (
                              <span className="badge bg-danger-subtle text-danger border border-danger-subtle fw-semibold">
                                <i className="bi bi-shield-slash me-1"></i> Hết hạn BH
                              </span>
                            )}
                          </div>

                          <h6 className="fw-bold text-dark mb-1">{device.productName}</h6>
                          <div className="text-muted small mb-2">
                            {device.colorName && <span className="me-2">Màu: <strong>{device.colorName}</strong></span>}
                            {device.selectedSpecs && <span>Cấu hình: {device.selectedSpecs}</span>}
                          </div>

                          <div className="bg-light p-2 rounded-2 border mb-3 small">
                            <div className="d-flex justify-content-between mb-1">
                              <span className="text-muted">Số Serial:</span>
                              <strong className="font-monospace text-primary">{device.serialNumber}</strong>
                            </div>
                            <div className="d-flex justify-content-between mb-1">
                              <span className="text-muted">Ngày nhận máy:</span>
                              <span>{device.purchaseDate ? new Date(device.purchaseDate).toLocaleDateString('vi-VN') : '---'}</span>
                            </div>
                            <div className="d-flex justify-content-between">
                              <span className="text-muted">Hạn bảo hành:</span>
                              <strong>{device.warrantyEndDate ? new Date(device.warrantyEndDate).toLocaleDateString('vi-VN') : '---'}</strong>
                            </div>
                          </div>
                        </div>

                        <div>
                          <button
                            type="button"
                            className={`btn w-100 fw-bold btn-sm ${
                              isUnderWarranty ? 'btn-danger' : 'btn-outline-danger'
                            }`}
                            onClick={() => {
                              if (onNavigateToService) {
                                onNavigateToService({ deviceId: device.deviceId, isWarranty: isUnderWarranty });
                              } else {
                                window.location.href = `/service?deviceId=${device.deviceId}`;
                              }
                            }}
                          >
                            <i className="bi bi-tools me-1"></i>
                            {isUnderWarranty ? 'Yêu cầu Bảo hành (0 ₫)' : 'Yêu cầu Sửa chữa có phí'}
                          </button>
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
    </div>
  );
}


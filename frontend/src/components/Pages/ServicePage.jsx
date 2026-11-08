import { useState, useEffect, useRef } from 'react';
import { formatVND } from '../../utils/formatters';

export default function ServicePage({ currentUser, initialTab = 'request', onGoHome, showToast, preSelectedDeviceId }) {
  const [activeTab, setActiveTab] = useState(initialTab); // 'request' | 'lookup' | 'catalog' | 'my-devices'

  // Catalog state
  const [catalog, setCatalog] = useState([]);
  const [catalogFilter, setCatalogFilter] = useState('all'); // 'all' | 'repair' | 'warranty'
  const [isLoadingCatalog, setIsLoadingCatalog] = useState(false);
  const [catalogError, setCatalogError] = useState('');

  // Request form state
  const [requestType, setRequestType] = useState('warranty'); // 'warranty' | 'repair'
  const [deviceSource, setDeviceSource] = useState('Purchased'); // 'Purchased' | 'External'
  const [selectedServiceId, setSelectedServiceId] = useState('');
  const [selectedDeviceId, setSelectedDeviceId] = useState(preSelectedDeviceId ? preSelectedDeviceId.toString() : '');
  const [externalModel, setExternalModel] = useState('');
  const [externalSerial, setExternalSerial] = useState('');

  // Autocomplete Model máy trong phiếu đăng ký
  const [modelSuggestions, setModelSuggestions] = useState([]);
  const [isLoadingSuggestions, setIsLoadingSuggestions] = useState(false);
  const [showModelDropdown, setShowModelDropdown] = useState(false);
  const [highlightedModelIndex, setHighlightedModelIndex] = useState(-1);
  const [selectedMatchedProductId, setSelectedMatchedProductId] = useState(null);
  const modelSearchDebounceRef = useRef(null);
  const modelLatestRequestIdRef = useRef(0);
  const modelDropdownWrapperRef = useRef(null);
  const [customerName, setCustomerName] = useState(currentUser?.fullName || currentUser?.username || '');
  const [customerPhone, setCustomerPhone] = useState(currentUser?.phoneNumber || '');
  const [customerEmail, setCustomerEmail] = useState(currentUser?.email || '');
  const [customerAddress, setCustomerAddress] = useState('');
  const [faultDescription, setFaultDescription] = useState('');
  const [accessories, setAccessories] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createdTicket, setCreatedTicket] = useState(null);
  const [formError, setFormError] = useState('');

  // My devices state
  const [myDevices, setMyDevices] = useState([]);
  const [phoneForDevices, setPhoneForDevices] = useState(currentUser?.phoneNumber || '');
  const [deviceSearchTerm, setDeviceSearchTerm] = useState('');
  const [isLoadingDevices, setIsLoadingDevices] = useState(false);
  const [devicesError, setDevicesError] = useState('');

  // Lookup state
  const [lookupCode, setLookupCode] = useState('');
  const [lookupPhone, setLookupPhone] = useState('');
  const [lookupResult, setLookupResult] = useState(null);
  const [lookupError, setLookupError] = useState('');
  const [isLookingUp, setIsLookingUp] = useState(false);
  const [isDecidingQuote, setIsDecidingQuote] = useState(false);

  // My Tickets for logged-in user
  const [myTickets, setMyTickets] = useState([]);
  const [isLoadingMyTickets, setIsLoadingMyTickets] = useState(false);
  const [myTicketsError, setMyTicketsError] = useState('');

  // Hàm tải danh sách gợi ý Model máy qua API Backend với debounce 250ms
  const fetchModelSuggestions = (query) => {
    const reqId = ++modelLatestRequestIdRef.current;
    setIsLoadingSuggestions(true);
    const qParam = query && query.trim() ? `?q=${encodeURIComponent(query.trim())}` : '';
    fetch(`/api/products/suggest-models${qParam}`)
      .then((res) => (res.ok ? res.json() : []))
      .then((data) => {
        if (reqId === modelLatestRequestIdRef.current) {
          setModelSuggestions(Array.isArray(data) ? data : []);
          setIsLoadingSuggestions(false);
        }
      })
      .catch(() => {
        if (reqId === modelLatestRequestIdRef.current) {
          setModelSuggestions([]);
          setIsLoadingSuggestions(false);
        }
      });
  };

  const handleModelInputChange = (e) => {
    const val = e.target.value;
    setExternalModel(val);
    setSelectedMatchedProductId(null);
    setShowModelDropdown(true);
    setHighlightedModelIndex(-1);

    if (modelSearchDebounceRef.current) {
      clearTimeout(modelSearchDebounceRef.current);
    }

    modelSearchDebounceRef.current = setTimeout(() => {
      fetchModelSuggestions(val);
    }, 250);
  };

  const handleModelFocus = () => {
    setShowModelDropdown(true);
    if (modelSuggestions.length === 0) {
      fetchModelSuggestions(externalModel);
    }
  };

  const handleSelectModel = (item) => {
    setExternalModel(item.fullName || item.modelName);
    setSelectedMatchedProductId(item.productId);
    setShowModelDropdown(false);
    setHighlightedModelIndex(-1);
  };

  const handleUseCustomModel = () => {
    setShowModelDropdown(false);
    setHighlightedModelIndex(-1);
  };

  const handleModelKeyDown = (e) => {
    if (!showModelDropdown) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        setShowModelDropdown(true);
      }
      return;
    }

    const totalItems = modelSuggestions.length + (externalModel.trim() ? 1 : 0);

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setHighlightedModelIndex((prev) => (prev + 1) % totalItems);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setHighlightedModelIndex((prev) => (prev <= 0 ? totalItems - 1 : prev - 1));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (highlightedModelIndex >= 0 && highlightedModelIndex < modelSuggestions.length) {
        handleSelectModel(modelSuggestions[highlightedModelIndex]);
      } else {
        handleUseCustomModel();
      }
    } else if (e.key === 'Escape') {
      setShowModelDropdown(false);
      setHighlightedModelIndex(-1);
    }
  };

  // Đóng dropdown khi click ra ngoài
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (modelDropdownWrapperRef.current && !modelDropdownWrapperRef.current.contains(e.target)) {
        setShowModelDropdown(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // 1. Đọc tham số URL khi mount trang hoặc khi chuyển hướng từ nơi khác
  useEffect(() => {
    try {
      const params = new URLSearchParams(window.location.search);
      const devId = params.get('deviceId');
      const tabParam = params.get('tab');
      const codeParam = params.get('code');
      const actionParam = params.get('action');

      if (devId) {
        setSelectedDeviceId(devId);
        setRequestType('warranty');
        setDeviceSource('Purchased');
        setActiveTab('request');
      } else if (tabParam) {
        setActiveTab(tabParam);
      } else if (actionParam === 'request') {
        setActiveTab('request');
      } else if (actionParam === 'lookup') {
        setActiveTab('lookup');
      }

      if (codeParam) {
        setLookupCode(codeParam);
        const p = params.get('phone') || '';
        if (p) setLookupPhone(p);
        setActiveTab('lookup');
        handleLookupDirect(codeParam, p);
      }
    } catch {}
  }, []);

  // 2. Tải danh mục dịch vụ từ Backend API
  const loadCatalog = () => {
    setIsLoadingCatalog(true);
    setCatalogError('');
    fetch('/api/services/catalog')
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data && data.success) {
          setCatalog(data.items || []);
        } else {
          setCatalogError('Không thể tải bảng giá dịch vụ từ hệ thống.');
        }
      })
      .catch((err) => {
        console.error('Lỗi tải danh mục dịch vụ:', err);
        setCatalogError('Lỗi kết nối máy chủ khi tải danh mục dịch vụ.');
      })
      .finally(() => setIsLoadingCatalog(false));
  };

  useEffect(() => {
    loadCatalog();
  }, []);

  // 3. Tải danh sách máy đã mua khi chuyển sang tab my-devices hoặc request
  const loadMyDevices = (query = '') => {
    setIsLoadingDevices(true);
    setDevicesError('');
    const params = new URLSearchParams();
    const clean = (query || '').trim();
    if (clean) {
      if (/^[0-9+\s.-]{8,15}$/.test(clean)) {
        params.append('phoneNumber', clean);
      } else {
        params.append('orderCode', clean);
      }
    } else if (currentUser?.phoneNumber) {
      params.append('phoneNumber', currentUser.phoneNumber);
    }

    if (currentUser?.id) {
      params.append('userId', currentUser.id);
    }

    const queryString = params.toString();
    const url = queryString ? `/api/services/my-devices?${queryString}` : '/api/services/my-devices';
    fetch(url, { credentials: 'include' })
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data && data.success) {
          setMyDevices(data.devices || []);
        } else {
          setDevicesError(data?.message || 'Không thể tải danh sách máy đã mua.');
        }
      })
      .catch((err) => {
        console.error('Lỗi tải máy đã mua:', err);
        setDevicesError('Lỗi kết nối máy chủ khi tải máy đã mua.');
      })
      .finally(() => setIsLoadingDevices(false));
  };

  // Tự động đồng bộ và nạp dữ liệu khi currentUser thay đổi
  useEffect(() => {
    if (currentUser) {
      if (currentUser.fullName && !customerName) setCustomerName(currentUser.fullName);
      if (currentUser.phoneNumber && !customerPhone) setCustomerPhone(currentUser.phoneNumber);
      if (currentUser.email && !customerEmail) setCustomerEmail(currentUser.email);
      if (currentUser.phoneNumber && !phoneForDevices) setPhoneForDevices(currentUser.phoneNumber);
      loadMyDevices(currentUser.phoneNumber || '');
    }
  }, [currentUser]);

  // Tự động chọn thiết bị đầu tiên khi nạp danh sách máy của tài khoản
  useEffect(() => {
    if (myDevices.length > 0) {
      if (!selectedDeviceId || !myDevices.some((d) => d.deviceId.toString() === selectedDeviceId)) {
        const first = myDevices[0];
        setSelectedDeviceId(first.deviceId.toString());
        if (first.customerName && !customerName) setCustomerName(first.customerName);
        if (first.phoneNumber && !customerPhone) setCustomerPhone(first.phoneNumber);
      }
    }
  }, [myDevices]);

  useEffect(() => {
    if (activeTab === 'my-devices' || activeTab === 'request') {
      loadMyDevices(deviceSearchTerm || phoneForDevices || customerPhone || currentUser?.phoneNumber || '');
    }
    if (activeTab === 'lookup') {
      loadMyTickets(currentUser?.phoneNumber || customerPhone || phoneForDevices || '');
    }
  }, [activeTab]);

  // 4. Tải danh sách phiếu của khách hàng
  const loadMyTickets = (phone = '') => {
    setIsLoadingMyTickets(true);
    setMyTicketsError('');
    const params = new URLSearchParams();
    const clean = (phone || '').trim();
    if (clean) params.append('phoneNumber', clean);
    if (currentUser?.id) params.append('userId', currentUser.id);

    const queryString = params.toString();
    const url = queryString ? `/api/services/my-tickets?${queryString}` : '/api/services/my-tickets';
    fetch(url, { credentials: 'include' })
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data && data.success) {
          setMyTickets(data.tickets || []);
        }
      })
      .catch(() => {})
      .finally(() => setIsLoadingMyTickets(false));
  };

  const handleLookupDirect = async (code, phone) => {
    if (!code) return;
    setIsLookingUp(true);
    setLookupError('');
    try {
      const url = `/api/services/ticket/${encodeURIComponent(code)}?phone=${encodeURIComponent(phone || '')}`;
      const res = await fetch(url);
      const data = await res.json();
      if (res.ok && data.success) {
        setLookupResult(data.ticket);
      }
    } catch {} finally {
      setIsLookingUp(false);
    }
  };

  // Chọn một dịch vụ từ Catalog và chuyển sang Tab Gửi yêu cầu
  const handleSelectServiceFromCatalog = (service) => {
    setSelectedServiceId(service.serviceId.toString());
    if (service.serviceType === 'Bảo hành') {
      setRequestType('warranty');
      setDeviceSource('Purchased');
    } else {
      setRequestType('repair');
    }
    setActiveTab('request');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Chọn máy từ tab My Devices và chuyển sang gửi yêu cầu bảo hành
  const handleSelectDeviceForWarranty = (device) => {
    setSelectedDeviceId(device.deviceId.toString());
    setRequestType('warranty');
    setDeviceSource('Purchased');
    setCustomerName(device.customerName || customerName);
    setCustomerPhone(device.phoneNumber || customerPhone);
    setActiveTab('request');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Xử lý gửi phiếu yêu cầu
  const handleSubmitTicket = async (e) => {
    e.preventDefault();
    setFormError('');

    const cleanPhone = customerPhone.replace(/[\s\-\.]/g, '');
    const vnPhoneRegex = /^(0|\+84)(3|5|7|8|9)[0-9]{8}$/;
    if (!vnPhoneRegex.test(cleanPhone)) {
      setFormError('Số điện thoại không đúng định dạng di động Việt Nam (10 chữ số).');
      return;
    }

    if (!faultDescription.trim() || faultDescription.trim().length < 10) {
      setFormError('Vui lòng mô tả chi tiết triệu chứng hoặc lỗi của máy (tối thiểu 10 ký tự).');
      return;
    }

    setIsSubmitting(true);
    try {
      let endpoint = '';
      let payload = {};

      if (requestType === 'warranty') {
        if (!selectedDeviceId) {
          setFormError('Vui lòng chọn chiếc máy đã mua cần bảo hành.');
          setIsSubmitting(false);
          return;
        }

        endpoint = '/api/services/warranty-ticket';
        payload = {
          soldDeviceId: parseInt(selectedDeviceId, 10),
          serviceId: selectedServiceId ? parseInt(selectedServiceId, 10) : null,
          customerName: customerName.trim(),
          phoneNumber: cleanPhone,
          email: customerEmail.trim() || null,
          address: customerAddress.trim() || null,
          faultDescription: faultDescription.trim(),
          accessories: accessories.trim() || null
        };
      } else {
        endpoint = '/api/services/repair-ticket';
        payload = {
          deviceSource: deviceSource,
          soldDeviceId: deviceSource === 'Purchased' && selectedDeviceId ? parseInt(selectedDeviceId, 10) : null,
          externalDeviceModel: deviceSource === 'External' ? externalModel.trim() : null,
          externalSerialNumber: deviceSource === 'External' ? externalSerial.trim() : null,
          serviceId: selectedServiceId ? parseInt(selectedServiceId, 10) : null,
          customerName: customerName.trim(),
          phoneNumber: cleanPhone,
          email: customerEmail.trim() || null,
          address: customerAddress.trim() || null,
          faultDescription: faultDescription.trim(),
          accessories: accessories.trim() || null
        };
      }

      const res = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      const data = await res.json();
      if (!res.ok || !data.success) {
        setFormError(data.message || 'Không thể tạo phiếu yêu cầu. Vui lòng kiểm tra lại thông tin.');
      } else {
        setCreatedTicket(data.ticket);
        if (showToast) {
          showToast(`Đã gửi yêu cầu ${requestType === 'warranty' ? 'bảo hành' : 'sửa chữa'} thành công! Mã phiếu: ${data.ticket?.ticketCode}`);
        }
      }
    } catch (err) {
      setFormError('Không thể kết nối đến máy chủ. Vui lòng thử lại sau.');
    } finally {
      setIsSubmitting(false);
    }
  };

  // Tra cứu phiếu dịch vụ
  const handleLookupTicket = async (e) => {
    if (e) e.preventDefault();
    setLookupError('');
    setLookupResult(null);

    if (!lookupCode.trim()) {
      setLookupError('Vui lòng nhập mã phiếu (ví dụ: BH-20261101-0001 hoặc SC-20261101-0001).');
      return;
    }

    setIsLookingUp(true);
    try {
      const url = `/api/services/ticket/${encodeURIComponent(lookupCode.trim())}?phone=${encodeURIComponent(lookupPhone.trim())}`;
      const res = await fetch(url);
      const data = await res.json();

      if (!res.ok || !data.success) {
        setLookupError(data.message || 'Không tìm thấy thông tin phiếu hoặc số điện thoại không khớp.');
      } else {
        setLookupResult(data.ticket);
      }
    } catch (err) {
      setLookupError('Lỗi kết nối máy chủ khi tra cứu phiếu.');
    } finally {
      setIsLookingUp(false);
    }
  };

  // Khách hàng phản hồi duyệt hoặc từ chối báo giá
  const handleCustomerDecision = async (quoteId, decision) => {
    if (!window.confirm(`Bạn có chắc chắn muốn [${decision.toUpperCase()}] báo giá này?`)) {
      return;
    }

    setIsDecidingQuote(true);
    try {
      const res = await fetch('/api/services/quote-decision', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          quoteId: quoteId,
          decision: decision,
          phoneNumber: lookupPhone.trim() || lookupResult?.phoneNumber
        })
      });

      const data = await res.json();
      if (!res.ok || !data.success) {
        alert(data.message || 'Không thể cập nhật quyết định báo giá.');
      } else {
        if (showToast) showToast(`Đã ghi nhận quyết định: ${decision}!`);
        // Tải lại phiếu để cập nhật trạng thái mới nhất
        handleLookupTicket();
      }
    } catch (err) {
      alert('Lỗi kết nối máy chủ.');
    } finally {
      setIsDecidingQuote(false);
    }
  };

  // Lọc catalog
  const filteredCatalog = catalog.filter((item) => {
    if (catalogFilter === 'repair') return item.serviceType === 'Sửa chữa';
    if (catalogFilter === 'warranty') return item.serviceType === 'Bảo hành';
    return true;
  });

  return (
    <div className="bg-light py-4" style={{ minHeight: '80vh' }}>
      <div className="site-container">
        {/* BANNER HEADER */}
        <div className="bg-white rounded-3 shadow-sm p-4 mb-4 border">
          <div className="row align-items-center g-3">
            <div className="col-lg-8">
              <span className="badge bg-danger-subtle text-danger px-3 py-2 fw-bold text-uppercase mb-2">
                <i className="bi bi-shield-check me-1"></i> Trung tâm Dịch vụ Khách hàng
              </span>
              <h2 className="fw-black text-dark mb-1">Dịch vụ Sửa chữa & Bảo hành Chính hãng</h2>
              <p className="text-secondary small mb-0">
                Hệ thống tiếp nhận, theo dõi tiến độ và duyệt báo giá minh bạch. Bảo hành 0đ cho máy chính hãng; sửa chữa chuyên sâu cho mọi dòng laptop.
              </p>
            </div>
            <div className="col-lg-4 text-lg-end">
              <div className="d-inline-flex flex-column gap-1 bg-light p-3 rounded-3 border">
                <div className="small text-muted"><i className="bi bi-telephone-inbound-fill text-danger me-1"></i> Hotline kỹ thuật 24/7:</div>
                <div className="fs-5 fw-bold text-danger">1800 1234 (Nhánh 2)</div>
                <div className="small text-secondary">8:00 - 21:30 (Cả T7 & CN)</div>
              </div>
            </div>
          </div>
        </div>



        {/* NAV TABS */}
        <div className="d-flex flex-wrap gap-2 mb-4 bg-white p-2 rounded-3 shadow-sm border">
          <button
            type="button"
            className={`btn btn-sm px-4 py-2 fw-bold rounded-2 ${activeTab === 'request' ? 'btn-danger text-white' : 'btn-light text-secondary'}`}
            onClick={() => setActiveTab('request')}
          >
            <i className="bi bi-send-plus me-1"></i> 1. Gửi yêu cầu tiếp nhận
          </button>
          <button
            type="button"
            className={`btn btn-sm px-4 py-2 fw-bold rounded-2 ${activeTab === 'lookup' ? 'btn-danger text-white' : 'btn-light text-secondary'}`}
            onClick={() => {
              setActiveTab('lookup');
              loadMyTickets(currentUser?.phoneNumber || customerPhone);
            }}
          >
            <i className="bi bi-search me-1"></i> 2. Theo dõi tiến độ phiếu
          </button>
          <button
            type="button"
            className={`btn btn-sm px-4 py-2 fw-bold rounded-2 ${activeTab === 'catalog' ? 'btn-danger text-white' : 'btn-light text-secondary'}`}
            onClick={() => setActiveTab('catalog')}
          >
            <i className="bi bi-list-task me-1"></i> 3. Bảng giá dịch vụ tham khảo
          </button>
          <button
            type="button"
            className={`btn btn-sm px-4 py-2 fw-bold rounded-2 ${activeTab === 'my-devices' ? 'btn-danger text-white' : 'btn-light text-secondary'}`}
            onClick={() => setActiveTab('my-devices')}
          >
            <i className="bi bi-laptop me-1"></i> 4. Máy đã mua & Hạn BH ({myDevices.length})
          </button>
        </div>

        {/* TAB 1: BẢNG GIÁ DỊCH VỤ */}
        {activeTab === 'catalog' && (
          <div>
            <div className="d-flex justify-content-between align-items-center mb-3">
              <h5 className="fw-bold text-dark mb-0">
                <i className="bi bi-wrench text-danger me-2"></i>Danh mục định mức chi phí tham khảo
              </h5>
              <div className="btn-group btn-group-sm">
                <button
                  type="button"
                  className={`btn ${catalogFilter === 'all' ? 'btn-danger' : 'btn-outline-secondary'}`}
                  onClick={() => setCatalogFilter('all')}
                >
                  Tất cả ({catalog.length})
                </button>
                <button
                  type="button"
                  className={`btn ${catalogFilter === 'warranty' ? 'btn-danger' : 'btn-outline-secondary'}`}
                  onClick={() => setCatalogFilter('warranty')}
                >
                  Bảo hành (0đ)
                </button>
                <button
                  type="button"
                  className={`btn ${catalogFilter === 'repair' ? 'btn-danger' : 'btn-outline-secondary'}`}
                  onClick={() => setCatalogFilter('repair')}
                >
                  Sửa chữa dịch vụ
                </button>
              </div>
            </div>

            {catalogError ? (
              <div className="alert alert-danger py-4 px-4 text-center rounded-3 shadow-sm border">
                <i className="bi bi-exclamation-triangle-fill fs-3 d-block mb-2"></i>
                <h6 className="fw-bold">{catalogError}</h6>
                <button type="button" className="btn btn-danger btn-sm mt-2 px-3 fw-semibold" onClick={loadCatalog}>
                  <i className="bi bi-arrow-clockwise me-1"></i> Thử lại
                </button>
              </div>
            ) : isLoadingCatalog ? (
              <div className="text-center py-5 bg-white rounded-3 shadow-sm border">
                <div className="spinner-border text-danger" role="status"></div>
                <div className="small text-muted mt-2">Đang tải danh mục dịch vụ...</div>
              </div>
            ) : filteredCatalog.length === 0 ? (
              <div className="text-center py-5 bg-white rounded-3 shadow-sm border">
                <i className="bi bi-wrench text-muted fs-2 d-block mb-2"></i>
                <h6 className="fw-bold text-dark">Chưa có dịch vụ nào trong mục này</h6>
                <button type="button" className="btn btn-sm btn-outline-danger mt-2" onClick={loadCatalog}>
                  <i className="bi bi-arrow-clockwise me-1"></i> Làm mới danh mục
                </button>
              </div>
            ) : (
              <div className="row g-3">
                {filteredCatalog.map((item) => (
                  <div key={item.serviceId} className="col-md-6 col-lg-4">
                    <div className="card h-100 shadow-sm border rounded-3 p-3 d-flex flex-column justify-content-between">
                      <div>
                        <div className="d-flex justify-content-between align-items-start mb-2">
                          <span className="badge bg-light text-primary font-monospace border">
                            {item.serviceCode}
                          </span>
                          <span className={`badge ${item.serviceType === 'Bảo hành' ? 'bg-success-subtle text-success border' : 'bg-warning-subtle text-warning-emphasis border'} small`}>
                            {item.serviceType}
                          </span>
                        </div>
                        <h6 className="fw-bold text-dark mb-2">{item.ServiceName}</h6>
                        <p className="text-secondary small mb-3" style={{ minHeight: '40px' }}>
                          <span className="fw-semibold text-muted">Điều kiện / Triệu chứng:</span> {item.applicableConditions}
                        </p>
                      </div>

                      <div className="border-top pt-2 mt-auto">
                        <div className="d-flex justify-content-between align-items-center mb-2">
                          <div>
                            <div className="small text-muted" style={{ fontSize: '11px' }}>Chi phí tham khảo:</div>
                            <div className="fw-bold fs-6 text-danger">
                              {item.serviceType === 'Bảo hành' ? '0đ (Bảo hành)' : formatVND(item.estimatedCost)}
                            </div>
                          </div>
                          <div className="text-end">
                            <div className="small text-muted" style={{ fontSize: '11px' }}>Dự kiến:</div>
                            <div className="small fw-semibold text-dark">{item.estimatedTime || 'Trong ngày'}</div>
                          </div>
                        </div>

                        {item.serviceWarrantyMonths > 0 && (
                          <div className="small text-success mb-2" style={{ fontSize: '11px' }}>
                            <i className="bi bi-shield-check me-1"></i> Bảo hành sau sửa: {item.serviceWarrantyMonths} tháng
                          </div>
                        )}

                        <button
                          type="button"
                          className="btn btn-outline-danger btn-sm w-100 fw-bold"
                          onClick={() => handleSelectServiceFromCatalog(item)}
                        >
                          <i className="bi bi-box-arrow-in-right me-1"></i> Chọn dịch vụ này
                        </button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* TAB 2: GỬI YÊU CẦU TIẾP NHẬN */}
        {activeTab === 'request' && (
          <div className="row g-4">
            <div className="col-lg-8">
              <div className="bg-white rounded-3 p-4 shadow-sm border">
                <h5 className="fw-bold text-dark border-bottom pb-2 mb-3">
                  <i className="bi bi-send-plus text-danger me-2"></i>Đăng ký gửi máy sửa chữa / bảo hành
                </h5>

                {createdTicket ? (
                  <div className="text-center py-4">
                    <div className="rounded-circle bg-success text-white d-inline-flex p-3 fs-1 mb-3">
                      <i className="bi bi-check-lg"></i>
                    </div>
                    <h4 className="fw-bold text-success mb-2">Đăng ký thành công!</h4>
                    <p className="text-secondary small mb-3">
                      Hệ thống đã ghi nhận yêu cầu {createdTicket.ticketType.toLowerCase()} của quý khách.
                    </p>

                    <div className="p-3 bg-light rounded-3 border d-inline-block text-start mb-4" style={{ minWidth: '320px' }}>
                      <div className="small text-muted">Mã phiếu tiếp nhận của bạn:</div>
                      <div className="fs-4 fw-black font-monospace text-primary">{createdTicket.ticketCode}</div>
                      <div className="small text-dark mt-2">Thiết bị: <strong>{createdTicket.deviceDisplayName}</strong></div>
                      <div className="small text-dark">Serial: <strong>{createdTicket.serialNumber}</strong></div>
                      <div className="small text-muted mt-2 border-top pt-2">
                        Vui lòng lưu lại mã phiếu trên để tra cứu tiến độ kiểm tra và duyệt báo giá.
                      </div>
                    </div>

                    <div className="d-flex justify-content-center gap-2">
                      <button
                        type="button"
                        className="btn btn-primary px-4 fw-bold"
                        onClick={() => {
                          setLookupCode(createdTicket.ticketCode);
                          setLookupPhone(createdTicket.phoneNumber);
                          setActiveTab('lookup');
                          handleLookupTicket();
                        }}
                      >
                        <i className="bi bi-search me-1"></i> Theo dõi phiếu ngay
                      </button>
                      <button
                        type="button"
                        className="btn btn-outline-secondary px-3"
                        onClick={() => {
                          setCreatedTicket(null);
                          setFaultDescription('');
                          setSelectedDeviceId('');
                          setAccessories('');
                        }}
                      >
                        Tạo phiếu khác
                      </button>
                    </div>
                  </div>
                ) : (
                  <form onSubmit={handleSubmitTicket}>
                    {formError && (
                      <div className="alert alert-danger py-2 px-3 small d-flex align-items-center gap-2 mb-3">
                        <i className="bi bi-exclamation-triangle-fill fs-6 flex-shrink-0"></i>
                        <span>{formError}</span>
                      </div>
                    )}

                    {/* Chọn hình thức: Bảo hành vs Sửa chữa */}
                    <div className="mb-3">
                      <label className="form-label small fw-bold text-dark d-block">Loại dịch vụ quý khách cần:</label>
                      <div className="row g-2">
                        <div className="col-sm-6">
                          <label
                            className={`p-3 rounded-3 border d-flex align-items-center gap-2 w-100 ${requestType === 'warranty' ? 'border-danger bg-danger-subtle' : 'bg-light'}`}
                            style={{ cursor: 'pointer' }}
                          >
                            <input
                              type="radio"
                              name="reqType"
                              value="warranty"
                              checked={requestType === 'warranty'}
                              onChange={() => {
                                setRequestType('warranty');
                                setDeviceSource('Purchased');
                              }}
                            />
                            <div>
                              <strong className="d-block small text-dark">Bảo hành máy đã mua (0đ)</strong>
                              <span className="small text-muted" style={{ fontSize: '11px' }}>Dành cho máy chính hãng mua tại cửa hàng còn hạn</span>
                            </div>
                          </label>
                        </div>
                        <div className="col-sm-6">
                          <label
                            className={`p-3 rounded-3 border d-flex align-items-center gap-2 w-100 ${requestType === 'repair' ? 'border-danger bg-danger-subtle' : 'bg-light'}`}
                            style={{ cursor: 'pointer' }}
                          >
                            <input
                              type="radio"
                              name="reqType"
                              value="repair"
                              checked={requestType === 'repair'}
                              onChange={() => setRequestType('repair')}
                            />
                            <div>
                              <strong className="d-block small text-dark">Sửa chữa dịch vụ (Báo giá)</strong>
                              <span className="small text-muted" style={{ fontSize: '11px' }}>Mọi loại máy, duyệt giá minh bạch trước khi sửa</span>
                            </div>
                          </label>
                        </div>
                      </div>
                    </div>

                    {/* Nếu là Sửa chữa: Cho chọn máy đã mua hoặc máy ngoài */}
                    {requestType === 'repair' && (
                      <div className="mb-3 p-3 bg-light rounded-3 border">
                        <label className="form-label small fw-bold text-dark d-block mb-2">Nguồn gốc thiết bị:</label>
                        <div className="d-flex gap-3">
                          <label className="small text-dark d-flex align-items-center gap-1" style={{ cursor: 'pointer' }}>
                            <input
                              type="radio"
                              name="devSrc"
                              value="Purchased"
                              checked={deviceSource === 'Purchased'}
                              onChange={() => setDeviceSource('Purchased')}
                            />
                            Máy đã mua tại Laptop World
                          </label>
                          <label className="small text-dark d-flex align-items-center gap-1" style={{ cursor: 'pointer' }}>
                            <input
                              type="radio"
                              name="devSrc"
                              value="External"
                              checked={deviceSource === 'External'}
                              onChange={() => setDeviceSource('External')}
                            />
                            Máy mua ngoài / Dòng máy khác
                          </label>
                        </div>
                      </div>
                    )}

                    {/* CHỌN MÁY ĐÃ MUA - TÌM THEO SĐT HOẶC MÃ ĐƠN HÀNG */}
                    {deviceSource === 'Purchased' ? (
                      <div className="mb-3 p-3 bg-light rounded-3 border">
                        <label className="form-label small fw-bold text-dark d-flex justify-content-between align-items-center mb-2">
                          <span>
                            <i className="bi bi-laptop text-danger me-1"></i>
                            Chọn máy đã mua tại cửa hàng để nhận bảo hành <span className="text-danger">*</span>
                          </span>
                          {selectedDeviceId && (
                            <span className="badge bg-success-subtle text-success border border-success-subtle px-2 py-1">
                              <i className="bi bi-check-circle me-1"></i>Đã chọn máy
                            </span>
                          )}
                        </label>

                        {/* Thanh tìm kiếm nhanh máy đã mua theo SĐT hoặc Mã đơn hàng */}
                        <div className="input-group input-group-sm mb-2">
                          <span className="input-group-text bg-white text-muted">
                            <i className="bi bi-search"></i>
                          </span>
                          <input
                            type="text"
                            className="form-control form-control-sm"
                            placeholder="Nhập SĐT nhận hàng hoặc Mã đơn hàng (VD: 0987654321, PC2026...)"
                            value={deviceSearchTerm}
                            onChange={(e) => setDeviceSearchTerm(e.target.value)}
                            onKeyDown={(e) => {
                              if (e.key === 'Enter') {
                                e.preventDefault();
                                loadMyDevices(deviceSearchTerm);
                              }
                            }}
                          />
                          <button
                            type="button"
                            className="btn btn-danger btn-sm px-3 fw-semibold"
                            onClick={() => loadMyDevices(deviceSearchTerm)}
                            disabled={isLoadingDevices}
                          >
                            {isLoadingDevices ? 'Đang tìm...' : 'Tìm máy'}
                          </button>
                          <button
                            type="button"
                            className="btn btn-outline-secondary btn-sm"
                            onClick={() => {
                              setDeviceSearchTerm('');
                              loadMyDevices('');
                            }}
                            title="Hiển thị tất cả máy đã mua"
                          >
                            Xem tất cả
                          </button>
                        </div>

                        {/* Danh sách máy đã mua / Dropdown chọn */}
                        {myDevices.length > 0 ? (
                          <div>
                            <select
                              className="form-select form-select-sm mb-2 fw-semibold text-dark"
                              value={selectedDeviceId}
                              onChange={(e) => {
                                const val = e.target.value;
                                setSelectedDeviceId(val);
                                const found = myDevices.find((d) => d.deviceId.toString() === val);
                                if (found) {
                                  if (found.customerName && !customerName) setCustomerName(found.customerName);
                                  if (found.phoneNumber && !customerPhone) setCustomerPhone(found.phoneNumber);
                                }
                              }}
                              required
                            >
                              <option value="">-- Bấm vào đây để chọn máy đã mua tại Shop --</option>
                              {myDevices.map((d) => (
                                <option
                                  key={d.deviceId}
                                  value={d.deviceId}
                                  disabled={requestType === 'warranty' && !d.isUnderWarranty}
                                >
                                  {d.productName} [Serial: {d.serialNumber}] - Đơn #{d.orderCode} - {d.isUnderWarranty ? 'Còn BH' : 'HẾT HẠN BH'}
                                </option>
                              ))}
                            </select>

                            {/* Card xem chi tiết máy đã chọn */}
                            {selectedDeviceId && (() => {
                              const dev = myDevices.find((d) => d.deviceId.toString() === selectedDeviceId);
                              if (!dev) return null;
                              return (
                                <div className="card p-2 border-success bg-white shadow-sm mt-2">
                                  <div className="d-flex align-items-center gap-2">
                                    <img
                                      src={dev.thumbnailUrl || '/images/default.png'}
                                      alt={dev.productName}
                                      style={{ width: '48px', height: '48px', objectFit: 'contain' }}
                                      className="rounded border"
                                    />
                                    <div className="flex-grow-1 small">
                                      <div className="fw-bold text-dark">{dev.productName}</div>
                                      <div className="text-muted" style={{ fontSize: '11px' }}>
                                        Số Serial: <strong className="text-primary font-monospace">{dev.serialNumber}</strong> | Mã đơn: <strong>#{dev.orderCode}</strong>
                                      </div>
                                    </div>
                                    <div className="text-end">
                                      <span className={`badge ${dev.isUnderWarranty ? 'bg-success' : 'bg-warning text-dark'} px-2 py-1`}>
                                        {dev.isUnderWarranty ? '✓ Còn bảo hành' : 'Hết hạn bảo hành'}
                                      </span>
                                      <div className="text-muted mt-1" style={{ fontSize: '10px' }}>
                                        Hạn: {dev.warrantyEndDate ? new Date(dev.warrantyEndDate).toLocaleDateString('vi-VN') : '12 tháng'}
                                      </div>
                                    </div>
                                  </div>
                                </div>
                              );
                            })()}
                          </div>
                        ) : (
                          <div className="alert alert-info py-2 px-3 small mb-0 d-flex justify-content-between align-items-center">
                            <div>
                              <i className="bi bi-info-circle me-1"></i> Chưa thấy thiết bị phù hợp.
                            </div>
                            <button
                              type="button"
                              className="btn btn-sm btn-outline-primary py-0"
                              onClick={() => {
                                setDeviceSearchTerm('');
                                loadMyDevices('');
                              }}
                            >
                              Tải tất cả máy của Shop
                            </button>
                          </div>
                        )}
                      </div>
                    ) : (
                      /* NHẬP THÔNG TIN MÁY NGOÀI */
                      <div className="row g-2 mb-3 align-items-start">
                        <div className="col-sm-7 position-relative" ref={modelDropdownWrapperRef}>
                          <label className="form-label small fw-bold text-dark d-flex justify-content-between align-items-center mb-1" style={{ minHeight: '22px' }}>
                            <span>Hãng và Model máy <span className="text-danger">*</span></span>
                            {selectedMatchedProductId && (
                              <span className="badge bg-success-subtle text-success border border-success-subtle" style={{ fontSize: '10px' }}>
                                <i className="bi bi-check2-circle me-1"></i>Đã khớp danh mục
                              </span>
                            )}
                          </label>
                          <div className="input-group input-group-sm">
                            <span className="input-group-text bg-light text-muted">
                              <i className="bi bi-search"></i>
                            </span>
                            <input
                              type="text"
                              className="form-control form-control-sm"
                              placeholder="Gõ để tìm hoặc chọn (VD: Dell XPS 13, ASUS ROG...)"
                              required
                              value={externalModel}
                              onChange={handleModelInputChange}
                              onFocus={handleModelFocus}
                              onKeyDown={handleModelKeyDown}
                              autoComplete="off"
                            />
                            {externalModel && (
                              <button
                                type="button"
                                className="btn btn-outline-secondary"
                                onClick={() => {
                                  setExternalModel('');
                                  setSelectedMatchedProductId(null);
                                  fetchModelSuggestions('');
                                }}
                                title="Xóa nội dung"
                              >
                                <i className="bi bi-x"></i>
                              </button>
                            )}
                          </div>

                          {/* Dropdown gợi ý Model máy */}
                          {showModelDropdown && (
                            <div 
                              className="position-absolute start-0 end-0 mt-1 bg-white border rounded shadow-lg overflow-hidden"
                              style={{ zIndex: 1050, maxHeight: '280px', overflowY: 'auto' }}
                            >
                              <div className="p-2 border-bottom bg-light d-flex justify-content-between align-items-center">
                                <span className="small text-muted fw-bold" style={{ fontSize: '11px' }}>
                                  <i className="bi bi-laptop me-1 text-primary"></i>Gợi ý laptop chính hãng
                                </span>
                                {isLoadingSuggestions && (
                                  <span className="spinner-border spinner-border-sm text-secondary" style={{ width: '12px', height: '12px' }}></span>
                                )}
                              </div>

                              {modelSuggestions.length > 0 ? (
                                modelSuggestions.map((item, idx) => (
                                  <div
                                    key={item.productId || idx}
                                    className={`p-2 d-flex align-items-center justify-content-between border-bottom ${
                                      idx === highlightedModelIndex ? 'bg-primary text-white fw-semibold' : 'bg-white text-dark'
                                    }`}
                                    style={{ cursor: 'pointer' }}
                                    onClick={() => handleSelectModel(item)}
                                    onMouseEnter={() => setHighlightedModelIndex(idx)}
                                  >
                                    <div className="d-flex align-items-center gap-2 text-truncate">
                                      {item.brand && (
                                        <span className={`badge border small ${idx === highlightedModelIndex ? 'bg-white text-primary' : 'bg-light text-dark'}`} style={{ fontSize: '10px' }}>
                                          {item.brand}
                                        </span>
                                      )}
                                      <span className="small text-truncate">{item.fullName || item.modelName}</span>
                                    </div>
                                    <span className={`badge border small ${idx === highlightedModelIndex ? 'bg-white text-primary' : 'bg-light text-secondary'}`} style={{ fontSize: '10px' }}>
                                      Chọn
                                    </span>
                                  </div>
                                ))
                              ) : (
                                <div className="p-3 text-center text-muted small">
                                  {isLoadingSuggestions ? 'Đang tìm kiếm...' : 'Không có model trùng khớp trong danh mục.'}
                                </div>
                              )}

                              {/* Lựa chọn Dùng tên máy vừa nhập */}
                              {externalModel && externalModel.trim().length > 0 && (
                                <div
                                  className={`p-2 border-top d-flex align-items-center justify-content-between ${
                                    highlightedModelIndex === modelSuggestions.length ? 'bg-warning-subtle text-dark fw-bold' : 'bg-light text-dark'
                                  }`}
                                  style={{ cursor: 'pointer' }}
                                  onClick={handleUseCustomModel}
                                  onMouseEnter={() => setHighlightedModelIndex(modelSuggestions.length)}
                                >
                                  <div className="small text-truncate">
                                    <i className="bi bi-pencil-square text-secondary me-2"></i>
                                    Dùng tên máy vừa nhập: <strong className="text-danger">"{externalModel}"</strong>
                                  </div>
                                  <span className="badge bg-secondary small" style={{ fontSize: '10px' }}>Xác nhận</span>
                                </div>
                              )}
                            </div>
                          )}
                        </div>
                        <div className="col-sm-5">
                          <label className="form-label small fw-bold text-dark d-flex align-items-center mb-1" style={{ minHeight: '22px' }}>
                            <span>Số Serial máy (nếu biết)</span>
                          </label>
                          <input
                            type="text"
                            className="form-control form-control-sm font-monospace"
                            placeholder="VD: 5CD2348ABC (Tùy chọn)"
                            value={externalSerial}
                            onChange={(e) => setExternalSerial(e.target.value)}
                          />
                        </div>
                      </div>
                    )}

                    {/* CHỌN HẠNG MỤC DỊCH VỤ TRONG CATALOG */}
                    <div className="mb-3">
                      <label className="form-label small fw-bold text-dark">Hạng mục dịch vụ yêu cầu (Tùy chọn)</label>
                      <select
                        className="form-select form-select-sm"
                        value={selectedServiceId}
                        onChange={(e) => setSelectedServiceId(e.target.value)}
                      >
                        <option value="">-- Kỹ thuật kiểm tra thực tế rồi đề xuất --</option>
                        {filteredCatalog.map((s) => (
                          <option key={s.serviceId} value={s.serviceId}>
                            [{s.serviceCode}] {s.serviceName} - {s.serviceType === 'Bảo hành' ? '0đ' : formatVND(s.estimatedCost)}
                          </option>
                        ))}
                      </select>
                    </div>

                    {/* MÔ TẢ LỖI */}
                    <div className="mb-3">
                      <label className="form-label small fw-bold text-dark">
                        Mô tả triệu chứng hư hỏng / yêu cầu xử lý <span className="text-danger">*</span>
                      </label>
                      <textarea
                        className="form-control form-control-sm"
                        rows="3"
                        required
                        placeholder="VD: Máy tự sập nguồn sau 15 phút, quạt kêu to bất thường, màn hình nhấp nháy sọc ngang..."
                        value={faultDescription}
                        onChange={(e) => setFaultDescription(e.target.value)}
                      ></textarea>
                    </div>

                    {/* PHỤ KIỆN GỬI KÈM */}
                    <div className="mb-3">
                      <label className="form-label small fw-bold text-dark">Phụ kiện mang theo kèm máy</label>
                      <input
                        type="text"
                        className="form-control form-control-sm"
                        placeholder="VD: Sạc zin theo máy, túi chống sốc..."
                        value={accessories}
                        onChange={(e) => setAccessories(e.target.value)}
                      />
                    </div>

                    {/* THÔNG TIN KHÁCH HÀNG */}
                    <h6 className="fw-bold text-dark border-top pt-3 mb-2 small text-uppercase">
                      Thông tin liên hệ người gửi máy
                    </h6>
                    <div className="row g-2 mb-3">
                      <div className="col-sm-6">
                        <label className="form-label small fw-semibold text-secondary">
                          Họ và tên <span className="text-danger">*</span>
                        </label>
                        <input
                          type="text"
                          className="form-control form-control-sm"
                          required
                          value={customerName}
                          onChange={(e) => setCustomerName(e.target.value)}
                        />
                      </div>
                      <div className="col-sm-6">
                        <label className="form-label small fw-semibold text-secondary">
                          Số điện thoại <span className="text-danger">*</span>
                        </label>
                        <input
                          type="tel"
                          className="form-control form-control-sm"
                          required
                          value={customerPhone}
                          onChange={(e) => {
                            setCustomerPhone(e.target.value);
                            loadMyDevices(e.target.value);
                          }}
                        />
                      </div>
                      <div className="col-sm-6">
                        <label className="form-label small fw-semibold text-secondary">Email nhận thông báo</label>
                        <input
                          type="email"
                          className="form-control form-control-sm"
                          value={customerEmail}
                          onChange={(e) => setCustomerEmail(e.target.value)}
                        />
                      </div>
                      <div className="col-sm-6">
                        <label className="form-label small fw-semibold text-secondary">Địa chỉ nhận trả máy (nếu cần ship)</label>
                        <input
                          type="text"
                          className="form-control form-control-sm"
                          value={customerAddress}
                          onChange={(e) => setCustomerAddress(e.target.value)}
                        />
                      </div>
                    </div>

                    <div className="d-flex justify-content-end gap-2 border-top pt-3">
                      <button
                        type="submit"
                        className="btn btn-danger px-4 fw-bold"
                        disabled={isSubmitting}
                      >
                        {isSubmitting ? (
                          <>
                            <span className="spinner-border spinner-border-sm me-2"></span> Đang xử lý...
                          </>
                        ) : (
                          <>
                            <i className="bi bi-send-check me-1"></i> Gửi yêu cầu tiếp nhận
                          </>
                        )}
                      </button>
                    </div>
                  </form>
                )}
              </div>
            </div>

            {/* QUY TRÌNH HƯỚNG DẪN BÊN PHẢI */}
            <div className="col-lg-4">
              <div className="bg-white rounded-3 p-3 shadow-sm border mb-3">
                <h6 className="fw-bold text-dark border-bottom pb-2 mb-3">
                  <i className="bi bi-info-circle text-danger me-2"></i>Quy trình tiếp nhận máy
                </h6>
                <div className="small text-secondary">
                  <div className="d-flex gap-2 mb-3">
                    <span className="badge bg-danger rounded-circle p-2" style={{ width: '24px', height: '24px', lineHeight: '10px' }}>1</span>
                    <div>
                      <strong className="text-dark d-block">Đăng ký online</strong>
                      <span>Chọn bảo hành hoặc sửa chữa, mô tả lỗi và nhận mã phiếu.</span>
                    </div>
                  </div>
                  <div className="d-flex gap-2 mb-3">
                    <span className="badge bg-danger rounded-circle p-2" style={{ width: '24px', height: '24px', lineHeight: '10px' }}>2</span>
                    <div>
                      <strong className="text-dark d-block">Mang máy đến cửa hàng</strong>
                      <span>Mang laptop cùng phụ kiện đến địa chỉ showroom gần nhất.</span>
                    </div>
                  </div>
                  <div className="d-flex gap-2 mb-3">
                    <span className="badge bg-danger rounded-circle p-2" style={{ width: '24px', height: '24px', lineHeight: '10px' }}>3</span>
                    <div>
                      <strong className="text-dark d-block">Kỹ thuật kiểm tra & Báo giá</strong>
                      <span>Kiểm tra linh kiện, gửi báo giá online. Bạn duyệt mới tiến hành sửa.</span>
                    </div>
                  </div>
                  <div className="d-flex gap-2">
                    <span className="badge bg-danger rounded-circle p-2" style={{ width: '24px', height: '24px', lineHeight: '10px' }}>4</span>
                    <div>
                      <strong className="text-dark d-block">Nhận lại máy & Bảo hành</strong>
                      <span>Test máy kỹ tại quầy, nhận phiếu bảo hành dịch vụ 1-36 tháng.</span>
                    </div>
                  </div>
                </div>
              </div>

              <div className="bg-white rounded-3 p-3 shadow-sm border">
                <h6 className="fw-bold text-dark border-bottom pb-2 mb-2">
                  <i className="bi bi-geo-alt text-danger me-2"></i>Địa điểm tiếp nhận
                </h6>
                <div className="small text-secondary">
                  <div className="fw-semibold text-dark">Showroom Hà Nội:</div>
                  <div className="mb-2">123 Đường Cầu Giấy, Q. Cầu Giấy, Hà Nội</div>
                  <div className="fw-semibold text-dark">Showroom TP.HCM:</div>
                  <div>456 Đường CMT8, Phường 11, Quận 3, TP.HCM</div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* TAB 3: THEO DÕI TIẾN ĐỘ PHIẾU */}
        {activeTab === 'lookup' && (
          <div className="row justify-content-center">
            <div className="col-lg-9">
              {/* DANH SÁCH PHIẾU CỦA BẠN NẾU CÓ */}
              {myTickets.length > 0 && (
                <div className="bg-white rounded-3 p-4 shadow-sm border mb-4">
                  <div className="d-flex justify-content-between align-items-center border-bottom pb-2 mb-3">
                    <h6 className="fw-bold text-dark mb-0">
                      <i className="bi bi-person-lines-fill text-danger me-2"></i>Phiếu dịch vụ của bạn ({myTickets.length})
                    </h6>
                    <span className="small text-muted">Bấm vào phiếu để xem tiến độ chi tiết</span>
                  </div>
                  <div className="d-flex flex-column gap-2">
                    {myTickets.map((t) => (
                      <div 
                        key={t.ticketId} 
                        className={`p-3 rounded-2 border d-flex flex-wrap justify-content-between align-items-center ${
                          lookupResult?.ticketId === t.ticketId ? 'border-danger bg-danger-subtle' : 'bg-light hover-bg-white'
                        }`}
                        style={{ cursor: 'pointer' }}
                        onClick={() => {
                          setLookupResult(t);
                          setLookupCode(t.ticketCode);
                          setLookupPhone(t.phoneNumber);
                        }}
                      >
                        <div>
                          <span className="font-monospace fw-bold text-danger me-2">#{t.ticketCode}</span>
                          <span className="badge bg-secondary me-2">{t.ticketType}</span>
                          <strong className="text-dark">{t.deviceDisplayName}</strong>
                          <div className="small text-muted mt-1">Lỗi: {t.faultDescription}</div>
                        </div>
                        <div className="text-end mt-2 mt-sm-0">
                          <span className="badge bg-primary px-2 py-1">{t.status}</span>
                          <div className="small text-danger fw-semibold mt-1">
                            Xem chi tiết <i className="bi bi-chevron-right"></i>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* FORM TRA CỨU */}
              <div className="bg-white rounded-3 p-4 shadow-sm border mb-4">
                <h5 className="fw-bold text-dark border-bottom pb-2 mb-3">
                  <i className="bi bi-search text-danger me-2"></i>Tra cứu tiến độ & Duyệt báo giá trực tuyến
                </h5>
                <form onSubmit={handleLookupTicket} className="row g-2">
                  <div className="col-md-5">
                    <label className="form-label small fw-semibold text-secondary">Mã phiếu tiếp nhận</label>
                    <input
                      type="text"
                      className="form-control form-control-sm font-monospace"
                      placeholder="VD: BH-20261101-0001"
                      required
                      value={lookupCode}
                      onChange={(e) => setLookupCode(e.target.value)}
                    />
                  </div>
                  <div className="col-md-5">
                    <label className="form-label small fw-semibold text-secondary">Số điện thoại xác thực</label>
                    <input
                      type="tel"
                      className="form-control form-control-sm"
                      placeholder="Số điện thoại khi đăng ký phiếu"
                      value={lookupPhone}
                      onChange={(e) => setLookupPhone(e.target.value)}
                    />
                  </div>
                  <div className="col-md-2 d-flex align-items-end">
                    <button type="submit" className="btn btn-danger btn-sm w-100 fw-bold" disabled={isLookingUp}>
                      {isLookingUp ? 'Đang tìm...' : 'Tra cứu'}
                    </button>
                  </div>
                </form>

                {lookupError && (
                  <div className="alert alert-danger py-2 px-3 small mt-3 mb-0">
                    <i className="bi bi-exclamation-triangle-fill me-1"></i> {lookupError}
                  </div>
                )}
              </div>

              {/* KẾT QUẢ TRA CỨU PHIẾU */}
              {lookupResult && (
                <div className="bg-white rounded-3 p-4 shadow-sm border">
                  <div className="d-flex flex-wrap justify-content-between align-items-center border-bottom pb-3 mb-3">
                    <div>
                      <span className="badge bg-light text-primary font-monospace fs-6 me-2 border">
                        #{lookupResult.ticketCode}
                      </span>
                      <span className={`badge ${lookupResult.ticketType === 'Bảo hành' ? 'bg-success' : 'bg-warning text-dark'} fs-6`}>
                        {lookupResult.ticketType}
                      </span>
                    </div>
                    <div className="text-end">
                      <div className="small text-muted">Trạng thái hiện tại:</div>
                      <span className="badge bg-primary fs-6 mt-1">{lookupResult.status}</span>
                    </div>
                  </div>

                  {/* THÔNG TIN THIẾT BỊ & KHÁCH */}
                  <div className="row g-3 mb-4">
                    <div className="col-md-6">
                      <div className="p-3 bg-light rounded border small">
                        <div className="text-muted">Thiết bị:</div>
                        <div className="fw-bold fs-6 text-dark">{lookupResult.deviceDisplayName}</div>
                        <div className="font-monospace text-primary">Serial: {lookupResult.serialNumber}</div>
                        {lookupResult.isStoreDevice && (
                          <div className="text-success small mt-1">
                            <i className="bi bi-shield-check"></i> Hạn BH máy: {lookupResult.warrantyEndDate ? new Date(lookupResult.warrantyEndDate).toLocaleDateString('vi-VN') : 'Còn bảo hành'}
                          </div>
                        )}
                      </div>
                    </div>
                    <div className="col-md-6">
                      <div className="p-3 bg-light rounded border small">
                        <div className="text-muted">Khách hàng:</div>
                        <div className="fw-bold text-dark">{lookupResult.customerName}</div>
                        <div className="text-muted">SĐT: {lookupResult.phoneNumber}</div>
                        <div>Tổng chi phí: <strong className="text-danger fs-6">{lookupResult.ticketType === 'Bảo hành' && lookupResult.finalCost === 0 ? '0đ (Bảo hành)' : formatVND(lookupResult.finalCost)}</strong></div>
                      </div>
                    </div>
                  </div>

                    {/* BOX DUYỆT BÁO GIÁ CHO KHÁCH (NẾU CÓ BÁO GIÁ CHỜ DUYỆT) */}
                  {lookupResult.quotes && lookupResult.quotes.some((q) => q.customerDecision === 'Chờ duyệt') && (
                    <div className="alert alert-warning border border-warning p-4 mb-4 rounded-3">
                      <h5 className="fw-bold text-dark mb-2">
                        <i className="bi bi-bell-fill text-danger me-2"></i>Yêu cầu xác nhận báo giá sửa chữa
                      </h5>
                      <p className="small text-secondary mb-3">
                        Kỹ thuật viên đã kiểm tra thiết bị của bạn và đề xuất phương án xử lý với chi phí như sau. Vui lòng bấm <strong>Đồng ý</strong> để tiến hành sửa, hoặc <strong>Không sửa</strong> nếu bạn muốn nhận lại máy.
                      </p>

                      {lookupResult.quotes.filter((q) => q.customerDecision === 'Chờ duyệt').map((q) => (
                        <div key={q.quoteId} className="bg-white p-3 rounded border mb-3">
                          <div className="d-flex justify-content-between align-items-center mb-1">
                            <div className="fw-bold text-dark">{q.description}</div>
                            <div className="fs-5 fw-black text-danger">{formatVND(q.amount)}</div>
                          </div>
                          <div className="small text-muted">Thời điểm gửi: {new Date(q.createdAt).toLocaleString('vi-VN')}</div>

                          <div className="d-flex gap-2 mt-3">
                            <button
                              type="button"
                              className="btn btn-success fw-bold flex-fill"
                              disabled={isDecidingQuote}
                              onClick={() => handleCustomerDecision(q.quoteId, 'Đồng ý')}
                            >
                              <i className="bi bi-check-circle me-1"></i> Đồng ý báo giá ({formatVND(q.amount)})
                            </button>
                            <button
                              type="button"
                              className="btn btn-outline-secondary fw-bold flex-fill"
                              disabled={isDecidingQuote}
                              onClick={() => handleCustomerDecision(q.quoteId, 'Từ chối')}
                            >
                              <i className="bi bi-x-circle me-1"></i> Không sửa (Chờ trả máy)
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}

                  {/* TIMELINE TIẾN ĐỘ */}
                  <h6 className="fw-bold text-dark border-bottom pb-2 mb-3">
                    <i className="bi bi-clock-history text-danger me-2"></i>Nhật ký hành trình xử lý
                  </h6>
                  {lookupResult.histories && lookupResult.histories.length > 0 ? (
                    <div className="position-relative ps-4 border-start border-2 border-danger ms-3">
                      {lookupResult.histories.map((h) => (
                        <div key={h.historyId} className="mb-3 position-relative">
                          <div className="position-absolute rounded-circle bg-danger" style={{ width: '10px', height: '10px', left: -21, top: 5 }}></div>
                          <div className="d-flex justify-content-between align-items-center">
                            <span className="badge bg-light text-dark border fw-bold">{h.newStatus}</span>
                            <span className="small text-muted">{new Date(h.actionAt).toLocaleString('vi-VN')}</span>
                          </div>
                          <div className="small text-dark mt-1">{h.notes}</div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="text-muted small py-2">Đang chờ cửa hàng cập nhật tiến độ.</div>
                  )}
                </div>
              )}
            </div>
          </div>
        )}

        {/* TAB 4: MÁY ĐÃ MUA & BẢO HÀNH */}
        {activeTab === 'my-devices' && (
          <div className="bg-white rounded-3 p-4 shadow-sm border">
            <div className="d-flex flex-wrap justify-content-between align-items-center border-bottom pb-3 mb-3">
              <div>
                <h5 className="fw-bold text-dark mb-1">
                  <i className="bi bi-laptop text-danger me-2"></i>Máy đã mua & Quản lý số Serial
                </h5>
                <p className="text-secondary small mb-0">
                  Mỗi máy được gán số Serial duy nhất theo hóa đơn bàn giao để phục vụ quyền lợi bảo hành chính hãng.
                </p>
              </div>

              <div className="d-flex gap-2 mt-2 mt-sm-0">
                <input
                  type="tel"
                  className="form-control form-control-sm"
                  placeholder="Nhập SĐT để tải máy..."
                  value={phoneForDevices}
                  onChange={(e) => setPhoneForDevices(e.target.value)}
                />
                <button
                  type="button"
                  className="btn btn-sm btn-danger px-3 text-nowrap"
                  onClick={() => loadMyDevices(phoneForDevices)}
                  disabled={isLoadingDevices}
                >
                  {isLoadingDevices ? 'Đang tải...' : 'Tìm máy'}
                </button>
              </div>
            </div>

            {isLoadingDevices ? (
              <div className="text-center py-5">
                <div className="spinner-border text-danger" role="status"></div>
                <div className="small text-muted mt-2">Đang đối chiếu dữ liệu thiết bị...</div>
              </div>
            ) : myDevices.length > 0 ? (
              <div className="table-responsive">
                <table className="table table-hover align-middle mb-0">
                  <thead className="table-light">
                    <tr>
                      <th>Thiết bị & Phiên bản</th>
                      <th>Số Serial</th>
                      <th>Mã đơn hàng</th>
                      <th>Ngày bàn giao</th>
                      <th>Thời hạn bảo hành</th>
                      <th>Tình trạng</th>
                      <th class="text-end">Bảo hành</th>
                    </tr>
                  </thead>
                  <tbody>
                    {myDevices.map((d) => (
                      <tr key={d.deviceId}>
                        <td>
                          <div className="fw-semibold text-dark">{d.productName}</div>
                          {d.colorName && <span className="badge bg-light text-secondary border small">Màu: {d.colorName}</span>}
                        </td>
                        <td className="font-monospace fw-bold text-primary">{d.serialNumber}</td>
                        <td><span className="small text-muted">#{d.orderCode}</span></td>
                        <td className="small text-muted">{new Date(d.deliveredAt).toLocaleDateString('vi-VN')}</td>
                        <td className="small text-muted">
                          <div>Đến: <strong>{new Date(d.warrantyEndDate).toLocaleDateString('vi-VN')}</strong></div>
                        </td>
                        <td>
                          {d.isUnderWarranty ? (
                            <span className="badge bg-success-subtle text-success border">
                              <i className="bi bi-shield-check"></i> Còn bảo hành
                            </span>
                          ) : (
                            <span className="badge bg-danger-subtle text-danger border">
                              <i className="bi bi-shield-x"></i> Hết hạn BH
                            </span>
                          )}
                        </td>
                        <td className="text-end">
                          {d.isUnderWarranty ? (
                            <button
                              type="button"
                              className="btn btn-sm btn-danger fw-bold text-nowrap"
                              onClick={() => handleSelectDeviceForWarranty(d)}
                            >
                              <i className="bi bi-send-plus me-1"></i> Yêu cầu BH
                            </button>
                          ) : (
                            <button
                              type="button"
                              className="btn btn-sm btn-outline-secondary fw-bold text-nowrap"
                              onClick={() => {
                                setSelectedDeviceId(d.deviceId.toString());
                                setRequestType('repair');
                                setDeviceSource('Purchased');
                                setActiveTab('request');
                              }}
                            >
                              Gửi sửa chữa
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="text-center py-5 text-muted">
                <i className="bi bi-laptop fs-1 d-block mb-2 text-secondary"></i>
                <div className="fw-bold">Chưa tìm thấy thiết bị nào</div>
                <div className="small">Vui lòng nhập số điện thoại bạn đã dùng khi đặt hàng tại Laptop World để tải danh sách máy đã giao.</div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}


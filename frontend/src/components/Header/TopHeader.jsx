import React, { useState } from 'react';

export default function TopHeader({ onGoHome, onOpenLogin, currentUser, onLogout, cartCount = 0, onOpenCart }) {
  const [showDropdown, setShowDropdown] = useState(false);

  return (
    <header className="top-header text-white py-2">
      <div className="site-container">
        <div className="row align-items-center g-3">
          {/* Logo */}
          <div className="col-auto" style={{ width: '240px' }}>
            <a 
              href="/" 
              onClick={(e) => {
                if (onGoHome) {
                  e.preventDefault();
                  onGoHome();
                }
              }} 
              className="text-white text-decoration-none d-flex flex-column"
            >
              <span className="fw-black fs-4 lh-1">PC<span className="text-pc-red">STORE</span></span>
              <span style={{ fontSize: '9px', letterSpacing: '0.8px' }} className="text-secondary fw-semibold">
                LAPTOP - PC - GAMING GEAR
              </span>
            </a>
          </div>

          {/* Search Box */}
          <div className="col">
            <div className="input-group">
              <input
                type="text"
                className="form-control form-control-sm border-0"
                placeholder="Bạn cần tìm sản phẩm gì?"
              />
              <select className="form-select form-select-sm border-0 bg-light text-secondary" style={{ maxWidth: '140px', fontSize: '12px' }}>
                <option>Tất cả danh mục</option>
                <option>Laptop văn phòng</option>
                <option>Laptop Gaming</option>
                <option>Laptop đồ họa</option>
              </select>
              <button className="btn btn-pc-red btn-sm px-3" type="button">
                <i className="bi bi-search"></i>
              </button>
            </div>
          </div>

          {/* Hotline, Support, Account, Cart */}
          <div className="col-auto d-none d-lg-flex align-items-center gap-4 text-white" style={{ fontSize: '12px' }}>
            <div className="d-flex align-items-center gap-2">
              <i className="bi bi-telephone text-secondary fs-5"></i>
              <div>
                <span className="text-secondary d-block lh-1">Hotline</span>
                <span className="fw-bold">1800 1234</span>
              </div>
            </div>

            <div className="d-flex align-items-center gap-2">
              <i className="bi bi-headset text-secondary fs-5"></i>
              <div>
                <span className="text-secondary d-block lh-1">Hỗ trợ</span>
                <span className="fw-bold">24/7</span>
              </div>
            </div>

            {/* Tài khoản Người dùng / Đăng nhập */}
            {!currentUser ? (
              <div 
                className="d-flex align-items-center gap-2" 
                style={{ cursor: 'pointer' }}
                onClick={onOpenLogin}
                title="Đăng nhập tài khoản"
              >
                <i className="bi bi-person text-secondary fs-5"></i>
                <div>
                  <span className="text-secondary d-block lh-1">Tài khoản</span>
                  <span className="fw-bold">Đăng nhập</span>
                </div>
              </div>
            ) : (
              <div className="position-relative">
                <div 
                  className="d-flex align-items-center gap-2" 
                  style={{ cursor: 'pointer' }}
                  onClick={() => setShowDropdown(!showDropdown)}
                  title="Tài khoản cá nhân"
                >
                  <div 
                    className="rounded-circle bg-danger text-white d-flex align-items-center justify-content-center fw-bold flex-shrink-0"
                    style={{ width: '28px', height: '28px', fontSize: '12px' }}
                  >
                    {(currentUser.fullName || currentUser.username || 'U')[0].toUpperCase()}
                  </div>
                  <div>
                    <div className="d-flex align-items-center gap-1">
                      <span className="fw-bold text-white text-truncate" style={{ maxWidth: '100px' }}>
                        {currentUser.fullName || currentUser.username}
                      </span>
                      <span 
                        className={`badge ${currentUser.role === 'Admin' ? 'bg-warning text-dark' : 'bg-primary'}`} 
                        style={{ fontSize: '9px' }}
                      >
                        {currentUser.role === 'Admin' ? 'Admin' : 'Khách'}
                      </span>
                    </div>
                    <span className="text-secondary d-block lh-1" style={{ fontSize: '10px' }}>
                      Đang hoạt động
                    </span>
                  </div>
                  <i className="bi bi-chevron-down text-secondary" style={{ fontSize: '10px' }}></i>
                </div>

                {/* Dropdown menu tài khoản */}
                {showDropdown && (
                  <div 
                    className="position-absolute end-0 top-100 mt-2 bg-white rounded-3 shadow-lg border p-2"
                    style={{ minWidth: '220px', zIndex: 1050 }}
                  >
                    <div className="px-2 py-1 border-bottom mb-2 text-dark">
                      <div className="fw-bold small">{currentUser.fullName || currentUser.username}</div>
                      <div className="text-muted" style={{ fontSize: '11px' }}>{currentUser.email}</div>
                      <div className="badge bg-light text-secondary border mt-1" style={{ fontSize: '10px' }}>
                        {currentUser.role === 'Admin' ? '👑 Quyền: Quản trị viên (Admin)' : '👤 Quyền: Khách hàng (Customer)'}
                      </div>
                    </div>

                    {currentUser.role === 'Admin' && (
                      <a 
                        className="d-flex align-items-center gap-2 px-2 py-2 text-danger text-decoration-none rounded hover-bg-light small fw-bold"
                        href="http://localhost:5148/Product/Index" 
                        target="_blank" 
                        rel="noreferrer"
                        style={{ backgroundColor: '#fff5f5' }}
                      >
                        <i className="bi bi-shield-lock-fill"></i>
                        Vào trang Quản trị (Admin MVC)
                      </a>
                    )}

                    <button 
                      className="btn btn-sm btn-outline-danger w-100 d-flex align-items-center justify-content-center gap-1 mt-2"
                      type="button"
                      onClick={() => {
                        setShowDropdown(false);
                        if (onLogout) onLogout();
                      }}
                    >
                      <i className="bi bi-box-arrow-right"></i>
                      Đăng xuất
                    </button>
                  </div>
                )}
              </div>
            )}

            {/* Giỏ hàng */}
            <div 
              className="d-flex align-items-center gap-2 position-relative" 
              style={{ cursor: 'pointer' }}
              onClick={onOpenCart}
              title="Xem giỏ hàng"
            >
              <div className="position-relative">
                <i className="bi bi-cart3 fs-4"></i>
                <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-pc-red" style={{ fontSize: '9px' }}>
                  {cartCount}
                </span>
              </div>
              <span className="fw-bold ms-1">Giỏ hàng</span>
            </div>
          </div>
        </div>
      </div>
    </header>
  );
}

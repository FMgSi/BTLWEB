import React from 'react';

export default function Navbar({ activeNav = 'home', onNavigate, onGoHome }) {
  const handleClick = (e, tab) => {
    e.preventDefault();
    if (onNavigate) {
      onNavigate(tab);
    }
  };

  return (
    <nav className="border-bottom bg-white shadow-sm">
      <div className="site-container">
        <div className="d-flex align-items-center">
          {/* Nút PHÂN LOẠI LAPTOP đỏ 240px */}
          <div 
            onClick={(e) => handleClick(e, 'products')}
            className="bg-pc-red text-white py-2 px-3 fw-bold d-flex align-items-center gap-2" 
            style={{ width: '256px', fontSize: '13px', cursor: 'pointer' }}
            title="Xem danh mục sản phẩm"
          >
            <i className="bi bi-list fs-5"></i>
            <span>PHÂN LOẠI LAPTOP</span>
          </div>

          {/* Menu ngang tối giản theo chuẩn thiết kế: Sản phẩm · Sửa chữa & bảo hành · Tài khoản · Liên hệ */}
          <ul className="nav ms-2 fw-semibold" style={{ fontSize: '13px' }}>
            <li className="nav-item">
              <a 
                className={`nav-link px-3 ${activeNav === 'home' ? 'text-pc-red border-bottom border-danger border-2 active' : 'text-dark'}`}
                href="/" 
                onClick={(e) => handleClick(e, 'home')}
              >
                Trang chủ
              </a>
            </li>

            <li className="nav-item">
              <a 
                className={`nav-link px-3 ${activeNav === 'products' ? 'text-pc-red border-bottom border-danger border-2 active' : 'text-dark'}`}
                href="/products" 
                onClick={(e) => handleClick(e, 'products')}
              >
                Sản phẩm
              </a>
            </li>

            <li className="nav-item">
              <a 
                className={`nav-link px-3 ${activeNav === 'service' ? 'text-pc-red border-bottom border-danger border-2 active' : 'text-dark'}`}
                href="/service" 
                onClick={(e) => handleClick(e, 'service')}
              >
                <i className="bi bi-tools me-1 text-danger"></i> Sửa chữa & Bảo hành
              </a>
            </li>

            <li className="nav-item">
              <a 
                className={`nav-link px-3 ${activeNav === 'account' ? 'text-pc-red border-bottom border-danger border-2 active' : 'text-dark'}`}
                href="/account" 
                onClick={(e) => handleClick(e, 'account')}
              >
                <i className="bi bi-person-circle me-1 text-danger"></i> Tài khoản của tôi
              </a>
            </li>

            <li className="nav-item">
              <a 
                className={`nav-link px-3 ${activeNav === 'contact' ? 'text-pc-red border-bottom border-danger border-2 active' : 'text-dark'}`}
                href="/contact" 
                onClick={(e) => handleClick(e, 'contact')}
              >
                Liên hệ
              </a>
            </li>
          </ul>
        </div>
      </div>
    </nav>
  );
}

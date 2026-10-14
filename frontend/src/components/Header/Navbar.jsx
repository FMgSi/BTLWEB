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

          {/* Menu ngang đầy đủ theo thiết kế */}
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
                className={`nav-link px-3 ${activeNav === 'promos' ? 'text-pc-red border-bottom border-danger border-2 active' : 'text-dark'}`}
                href="/promotions" 
                onClick={(e) => handleClick(e, 'promos')}
              >
                Khuyến mãi
              </a>
            </li>

            <li className="nav-item">
              <a 
                className={`nav-link px-3 ${activeNav === 'news' ? 'text-pc-red border-bottom border-danger border-2 active' : 'text-dark'}`}
                href="/news" 
                onClick={(e) => handleClick(e, 'news')}
              >
                Tin tức
              </a>
            </li>

            <li className="nav-item">
              <a 
                className={`nav-link px-3 ${activeNav === 'guides' ? 'text-pc-red border-bottom border-danger border-2 active' : 'text-dark'}`}
                href="/guides" 
                onClick={(e) => handleClick(e, 'guides')}
              >
                Hướng dẫn
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

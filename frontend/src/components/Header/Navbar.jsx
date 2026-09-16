export default function Navbar() {
  return (
    <nav className="border-bottom bg-white shadow-sm">
      <div className="site-container">
        <div className="d-flex align-items-center">
          {/* Nút đỏ đúng 240px */}
          <div className="bg-pc-red text-white py-2 px-3 fw-bold d-flex align-items-center gap-2" style={{ width: '240px', fontSize: '13px' }}>
            <i className="bi bi-list fs-5"></i>
            <span>DANH MỤC SẢN PHẨM</span>
          </div>

          {/* Menu ngang */}
          <ul className="nav ms-2 fw-semibold" style={{ fontSize: '13px' }}>
            <li className="nav-item">
              <a className="nav-link text-dark px-3" href="#">Trang chủ</a>
            </li>
            <li className="nav-item">
              <a className="nav-link text-pc-red px-3 active border-bottom border-danger border-2" href="#">Sản phẩm</a>
            </li>
            <li className="nav-item">
              <a className="nav-link text-dark px-3" href="#">Khuyến mãi</a>
            </li>
            <li className="nav-item">
              <a className="nav-link text-dark px-3" href="#">Tin tức</a>
            </li>
            <li className="nav-item">
              <a className="nav-link text-dark px-3" href="#">Hướng dẫn</a>
            </li>
            <li className="nav-item">
              <a className="nav-link text-dark px-3" href="#">Liên hệ</a>
            </li>
          </ul>
        </div>
      </div>
    </nav>
  );
}
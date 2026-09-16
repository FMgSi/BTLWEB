export default function TopHeader() {
  return (
    <header className="top-header text-white py-2">
      <div className="site-container">
        <div className="row align-items-center g-3">
          {/* Logo */}
          <div className="col-auto" style={{ width: '240px' }}>
            <a href="#" className="text-white text-decoration-none d-flex flex-column">
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

            <div className="d-flex align-items-center gap-2 cursor-pointer">
              <i className="bi bi-person text-secondary fs-5"></i>
              <div>
                <span className="text-secondary d-block lh-1">Tài khoản</span>
                <span className="fw-bold">Đăng nhập</span>
              </div>
            </div>

            <div className="d-flex align-items-center gap-2 position-relative cursor-pointer">
              <div className="position-relative">
                <i className="bi bi-cart3 fs-4"></i>
                <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-pc-red" style={{ fontSize: '9px' }}>
                  0
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
import React, { useState } from 'react';
import './Pages.css';

export default function ContactPage({ onGoHome }) {
  const [formData, setFormData] = useState({
    name: '',
    phone: '',
    email: '',
    topic: 'Tư vấn mua hàng / Báo giá',
    message: '',
  });
  const [submitted, setSubmitted] = useState(false);

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!formData.name.trim() || !formData.phone.trim() || !formData.message.trim()) {
      alert('Vui lòng điền đầy đủ Họ tên, Số điện thoại và Nội dung tin nhắn!');
      return;
    }
    setSubmitted(true);
  };

  return (
    <div className="site-container py-3">
      {/* 1. TOP BANNER LIÊN HỆ NỀN TỐI */}
      <div
        className="rounded-3 p-4 text-center text-white mb-4 shadow-sm"
        style={{
          backgroundColor: '#18181b',
          backgroundImage: 'radial-gradient(circle at 50% 50%, #27272a 0%, #18181b 100%)',
        }}
      >
        <span
          className="badge bg-danger rounded-pill px-3 py-1 mb-2 fw-semibold"
          style={{ fontSize: '11px', letterSpacing: '0.5px' }}
        >
          🎧 HỖ TRỢ KHÁCH HÀNG 24/7
        </span>

        <h1 className="fw-black mb-2" style={{ color: '#fbbf24', fontSize: '26px', letterSpacing: '-0.3px' }}>
          LIÊN HỆ VỚI PCSTORE
        </h1>

        <p className="text-light opacity-90 small mb-0">
          Chúng tôi luôn sẵn sàng hỗ trợ giải đáp mọi thắc mắc và tư vấn giải pháp máy tính tối ưu nhất cho bạn.
        </p>
      </div>

      {/* 2. KHU VỰC CHÍNH: THÔNG TIN LIÊN HỆ (TRÁI) & FORM HỖ TRỢ (PHẢI) */}
      <div className="row g-4 mb-4">
        {/* CỘT TRÁI: THÔNG TIN LIÊN HỆ & SHOWROOM */}
        <div className="col-12 col-lg-5">
          {/* Card 1: Thông tin liên hệ */}
          <div className="bg-white rounded-3 border p-4 shadow-sm mb-3">
            <h6 className="fw-bold text-danger mb-3 d-flex align-items-center gap-2">
              <i className="bi bi-geo-alt-fill"></i>
              <span>THÔNG TIN LIÊN HỆ</span>
            </h6>

            <div className="mb-3 d-flex align-items-start gap-2">
              <i className="bi bi-building text-secondary fs-5 mt-1"></i>
              <div>
                <strong className="d-block text-dark small">Trụ sở chính:</strong>
                <span className="text-secondary small">123 Đường Cầu Giấy, Q. Cầu Giấy, Hà Nội</span>
              </div>
            </div>

            <div className="mb-3 d-flex align-items-start gap-2">
              <i className="bi bi-telephone-fill text-danger fs-5 mt-1"></i>
              <div>
                <strong className="d-block text-dark small">Hotline bán hàng:</strong>
                <span className="text-danger fw-bold small">1800 1234</span>
                <span className="text-muted small ms-1">(Miễn phí, 8:00 - 21:30)</span>
              </div>
            </div>

            <div className="mb-3 d-flex align-items-start gap-2">
              <i className="bi bi-headset text-danger fs-5 mt-1"></i>
              <div>
                <strong className="d-block text-dark small">Hỗ trợ kỹ thuật &amp; Bảo hành:</strong>
                <span className="text-danger fw-bold small">1900 5678</span>
                <span className="text-muted small ms-1">(Phục vụ 24/7)</span>
              </div>
            </div>

            <div className="d-flex align-items-start gap-2">
              <i className="bi bi-envelope-fill text-secondary fs-5 mt-1"></i>
              <div>
                <strong className="d-block text-dark small">Email chăm sóc khách hàng:</strong>
                <span className="text-secondary small">contact@pcstore.vn</span>
              </div>
            </div>
          </div>

          {/* Card 2: Hệ thống Showroom */}
          <div className="bg-white rounded-3 border p-4 shadow-sm">
            <h6 className="fw-bold text-danger mb-3 d-flex align-items-center gap-2">
              <i className="bi bi-shop"></i>
              <span>HỆ THỐNG SHOWROOM</span>
            </h6>

            <ul className="list-unstyled mb-0 small">
              <li className="mb-2 d-flex align-items-start gap-2">
                <i className="bi bi-check-circle-fill text-success mt-1"></i>
                <span className="text-secondary">
                  <strong className="text-dark">Showroom 1:</strong> 123 Cầu Giấy, Q. Cầu Giấy, Hà Nội
                </span>
              </li>
              <li className="mb-2 d-flex align-items-start gap-2">
                <i className="bi bi-check-circle-fill text-success mt-1"></i>
                <span className="text-secondary">
                  <strong className="text-dark">Showroom 2:</strong> 456 Lê Thanh Nghị, Hai Bà Trưng, Hà Nội
                </span>
              </li>
              <li className="d-flex align-items-start gap-2">
                <i className="bi bi-check-circle-fill text-success mt-1"></i>
                <span className="text-secondary">
                  <strong className="text-dark">Showroom 3:</strong> 789 Nguyễn Văn Linh, Quận 7, TP.HCM
                </span>
              </li>
            </ul>
          </div>
        </div>

        {/* CỘT PHẢI: FORM GỬI YÊU CẦU HỖ TRỢ */}
        <div className="col-12 col-lg-7">
          <div className="bg-white rounded-3 border p-4 shadow-sm h-100">
            <h6 className="fw-bold text-danger mb-3 d-flex align-items-center gap-2">
              <i className="bi bi-envelope-paper-fill"></i>
              <span>GỬI YÊU CẦU HỖ TRỢ</span>
            </h6>

            {submitted ? (
              <div className="text-center py-5">
                <i className="bi bi-check-circle-fill text-success fs-1 mb-2 d-block"></i>
                <h6 className="fw-bold text-dark">Đã gửi yêu cầu thành công!</h6>
                <p className="text-muted small mb-3">
                  Đội ngũ chăm sóc khách hàng PCStore sẽ liên hệ lại qua số điện thoại <strong>{formData.phone}</strong> trong vòng 24h.
                </p>
                <button
                  type="button"
                  className="btn btn-sm btn-outline-danger"
                  onClick={() => {
                    setSubmitted(false);
                    setFormData({ name: '', phone: '', email: '', topic: 'Tư vấn mua hàng / Báo giá', message: '' });
                  }}
                >
                  Gửi yêu cầu khác
                </button>
              </div>
            ) : (
              <form onSubmit={handleSubmit}>
                <div className="row g-3 mb-3">
                  <div className="col-12 col-sm-6">
                    <label className="form-label small fw-semibold text-secondary">
                      Họ và tên <span className="text-danger">*</span>
                    </label>
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      placeholder="Nhập họ và tên..."
                      required
                      value={formData.name}
                      onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    />
                  </div>

                  <div className="col-12 col-sm-6">
                    <label className="form-label small fw-semibold text-secondary">
                      Số điện thoại <span className="text-danger">*</span>
                    </label>
                    <input
                      type="tel"
                      className="form-control form-control-sm"
                      placeholder="Nhập số điện thoại..."
                      required
                      value={formData.phone}
                      onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                    />
                  </div>
                </div>

                <div className="row g-3 mb-3">
                  <div className="col-12 col-sm-6">
                    <label className="form-label small fw-semibold text-secondary">Địa chỉ Email</label>
                    <input
                      type="email"
                      className="form-control form-control-sm"
                      placeholder="Nhập email liên hệ..."
                      value={formData.email}
                      onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    />
                  </div>

                  <div className="col-12 col-sm-6">
                    <label className="form-label small fw-semibold text-secondary">Chủ đề cần hỗ trợ</label>
                    <select
                      className="form-select form-select-sm"
                      value={formData.topic}
                      onChange={(e) => setFormData({ ...formData, topic: e.target.value })}
                    >
                      <option>Tư vấn mua hàng / Báo giá</option>
                      <option>Bảo hành &amp; Sửa chữa</option>
                      <option>Khiếu nại dịch vụ</option>
                      <option>Hợp tác kinh doanh</option>
                      <option>Khác</option>
                    </select>
                  </div>
                </div>

                <div className="mb-3">
                  <label className="form-label small fw-semibold text-secondary">
                    Nội dung tin nhắn <span className="text-danger">*</span>
                  </label>
                  <textarea
                    className="form-control"
                    rows="4"
                    placeholder="Nhập nội dung câu hỏi hoặc thắc mắc của bạn..."
                    required
                    value={formData.message}
                    onChange={(e) => setFormData({ ...formData, message: e.target.value })}
                  ></textarea>
                </div>

                <button
                  type="submit"
                  className="btn btn-danger w-100 py-2 fw-bold d-flex align-items-center justify-content-center gap-2"
                  style={{ backgroundColor: '#e11b1e', borderColor: '#e11b1e' }}
                >
                  <i className="bi bi-send-fill"></i>
                  <span>GỬI YÊU CẦU HỖ TRỢ</span>
                </button>
              </form>
            )}
          </div>
        </div>
      </div>

      {/* 3. BẢN ĐỒ VỊ TRÍ SHOWROOM */}
      <div className="bg-white rounded-3 border p-4 shadow-sm mb-4">
        <h6 className="fw-bold text-danger mb-3 d-flex align-items-center gap-2">
          <i className="bi bi-map-fill"></i>
          <span>BẢN ĐỒ VỊ TRÍ SHOWROOM</span>
        </h6>

        <div className="rounded-3 overflow-hidden border" style={{ minHeight: '260px' }}>
          <iframe
            title="PCStore Showroom Map"
            src="https://maps.google.com/maps?q=123%20Cau%20Giay%20Ha%20Noi&t=&z=15&ie=UTF8&iwloc=&output=embed"
            width="100%"
            height="280"
            style={{ border: 0 }}
            allowFullScreen=""
            loading="lazy"
          ></iframe>
        </div>
      </div>
    </div>
  );
}

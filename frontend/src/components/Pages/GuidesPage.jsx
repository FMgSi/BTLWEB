import React, { useState } from 'react';
import './Pages.css';

const ARTICLES = [
  {
    id: 1,
    title: 'Hướng dẫn cài đặt Windows 11 cho laptop',
    desc: 'Các bước cài đặt Windows 11 đơn giản, chi tiết cho người mới bắt đầu.',
    date: '05/09/2026',
    image: 'https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=500&q=80',
    content: `Để cài đặt Windows 11 cho laptop mới mua, bạn cần chuẩn bị:
1. Một chiếc USB tối thiểu 8GB dung lượng trống.
2. Tải công cụ Media Creation Tool từ trang chủ Microsoft.
3. Cắm USB và khởi động lại máy, bấm phím F12 hoặc F2 để vào Boot Menu.
4. Chọn khởi động từ USB và làm theo các bước thiết lập phân vùng ổ cứng.
5. Sau khi cài xong, Windows Update sẽ tự động tải các driver tương thích.`,
  },
  {
    id: 2,
    title: 'Cách vệ sinh laptop đúng cách',
    desc: 'Giúp máy mát hơn, bền hơn và hoạt động ổn định trong thời gian dài.',
    date: '02/09/2026',
    image: 'https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=500&q=80',
    content: `Vệ sinh laptop định kỳ giúp tăng tuổi thọ quạt và tản nhiệt:
1. Luôn tắt nguồn máy và rút sạc trước khi lau chùi.
2. Dùng bình xịt khí nén làm sạch bụi ở các khe tản nhiệt và bàn phím.
3. Dùng khăn mềm microfiber kết hợp dung dịch vệ sinh màn hình chuyên dụng.
4. Nên tra lại keo tản nhiệt định kỳ 6 - 12 tháng/lần tại các TTBH chính hãng.`,
  },
  {
    id: 3,
    title: 'Cách tối ưu hiệu năng laptop gaming',
    desc: 'Tăng FPS, giảm lag, giúp máy hoạt động mượt mà khi chơi game nặng.',
    date: '03/09/2026',
    image: 'https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=500&q=80',
    content: `Các mẹo tối ưu FPS khi chơi game:
1. Bật chế độ High Performance hoặc Turbo trong phần mềm của hãng (Armoury Crate, PredatorSense...).
2. Cập nhật driver card đồ họa NVIDIA/AMD mới nhất qua GeForce Experience.
3. Luôn cắm sạc khi chơi game để GPU và CPU phát huy tối đa công suất điện.
4. Tắt bớt các ứng dụng khởi động cùng Windows (Startup apps).`,
  },
  {
    id: 4,
    title: 'Hướng dẫn cài đặt Microsoft Office',
    desc: 'Cài đặt nhanh chóng, kích hoạt bản quyền đầy đủ và bảo mật dữ liệu.',
    date: '01/09/2026',
    image: 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=500&q=80',
    content: `Các bước kích hoạt Microsoft 365 bản quyền:
1. Đăng nhập tài khoản Microsoft trên website setup.office.com.
2. Nhập Product Key 25 ký tự được tặng kèm khi mua máy tại PCStore.
3. Tải bộ cài Office (Word, Excel, PowerPoint, Outlook) và tiến hành cài đặt.
4. Mở một ứng dụng bất kỳ và đăng nhập để đồng bộ bản quyền với đám mây OneDrive.`,
  },
];

export default function GuidesPage({ onGoHome }) {
  const [selectedArticle, setSelectedArticle] = useState(null);

  return (
    <div className="py-4" style={{ backgroundColor: '#f8fafc', minHeight: '80vh' }}>
      <div className="site-container">
        {/* Breadcrumb */}
        <div className="d-flex align-items-center gap-2 small text-secondary mb-3">
          <a
            href="/"
            onClick={(e) => {
              e.preventDefault();
              onGoHome && onGoHome();
            }}
            className="text-secondary text-decoration-none"
          >
            Trang chủ
          </a>
          <span>/</span>
          <span className="text-dark fw-bold">Hướng dẫn</span>
        </div>

        {/* 1. TOP HERO BANNER */}
        <div className="guides-hero-banner">
          <h1 className="fw-bold mb-2 fs-3">HƯỚNG DẪN SỬ DỤNG</h1>
          <p className="mb-1 fs-6 opacity-90">Hỗ trợ bạn trong quá trình mua sắm và sử dụng sản phẩm</p>
          <p className="small opacity-75 mb-0">
            Tìm hiểu các bài viết hướng dẫn chi tiết, mẹo sử dụng và giải đáp thắc mắc thường gặp.
          </p>
        </div>

        {/* 2. DANH MỤC HƯỚNG DẪN (5 THẺ) */}
        <div className="mb-4">
          <h5 className="fw-bold text-dark mb-3">Danh mục hướng dẫn</h5>
          <div className="row row-cols-2 row-cols-md-3 row-cols-lg-5 g-3">
            <div className="col">
              <div className="guides-cat-card" onClick={() => setSelectedArticle(ARTICLES[0])}>
                <i className="bi bi-cart-check-fill guides-cat-icon"></i>
                <div className="guides-cat-title">Hướng dẫn mua hàng</div>
                <div className="guides-cat-desc">Các bước đặt hàng, thanh toán, giao hàng</div>
              </div>
            </div>

            <div className="col">
              <div className="guides-cat-card" onClick={() => setSelectedArticle(ARTICLES[1])}>
                <i className="bi bi-laptop-fill guides-cat-icon"></i>
                <div className="guides-cat-title">Hướng dẫn sử dụng laptop</div>
                <div className="guides-cat-desc">Cài đặt, tối ưu hiệu năng, bảo quản</div>
              </div>
            </div>

            <div className="col">
              <div className="guides-cat-card" onClick={() => setSelectedArticle(ARTICLES[3])}>
                <i className="bi bi-gear-fill guides-cat-icon"></i>
                <div className="guides-cat-title">Hướng dẫn cài đặt phần mềm</div>
                <div className="guides-cat-desc">Windows, Driver, Office, cơ bản</div>
              </div>
            </div>

            <div className="col">
              <div className="guides-cat-card" onClick={() => setSelectedArticle(ARTICLES[1])}>
                <i className="bi bi-shield-shaded guides-cat-icon"></i>
                <div className="guides-cat-title">Bảo hành & đổi trả</div>
                <div className="guides-cat-desc">Chính sách bảo hành, đổi trả sản phẩm</div>
              </div>
            </div>

            <div className="col">
              <div className="guides-cat-card" onClick={() => setSelectedArticle(ARTICLES[2])}>
                <i className="bi bi-question-circle-fill guides-cat-icon"></i>
                <div className="guides-cat-title">Câu hỏi thường gặp</div>
                <div className="guides-cat-desc">Giải đáp các thắc mắc phổ biến</div>
              </div>
            </div>
          </div>
        </div>

        {/* 3. BÀI VIẾT NỔI BẬT (2x2 GRID) */}
        <div>
          <h5 className="fw-bold text-dark mb-3">Bài viết nổi bật</h5>
          <div className="row g-3">
            {ARTICLES.map((article) => (
              <div className="col-12 col-md-6" key={article.id}>
                <div className="guide-article-card" onClick={() => setSelectedArticle(article)}>
                  <img
                    src={article.image}
                    alt={article.title}
                    className="guide-article-thumb"
                  />
                  <div>
                    <h6 className="guide-article-title">{article.title}</h6>
                    <p className="guide-article-desc">{article.desc}</p>
                    <span className="guide-article-date">
                      <i className="bi bi-calendar3 me-1"></i> {article.date}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* MODAL CHI TIẾT BÀI HƯỚNG DẪN */}
      {selectedArticle && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1060 }}>
          <div className="modal-dialog modal-dialog-centered modal-lg">
            <div className="modal-content border-0 shadow-lg rounded-4 p-3">
              <div className="modal-header border-0 pb-2">
                <h5 className="modal-title fw-bold text-dark">{selectedArticle.title}</h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setSelectedArticle(null)}
                ></button>
              </div>
              <div className="modal-body py-2">
                <div className="text-muted small mb-3">
                  <i className="bi bi-calendar3 me-1"></i> Ngày đăng: {selectedArticle.date} • Chuyên mục: Hướng dẫn kỹ thuật
                </div>
                <img
                  src={selectedArticle.image}
                  alt={selectedArticle.title}
                  className="w-100 rounded-3 mb-3"
                  style={{ maxHeight: '280px', objectFit: 'cover' }}
                />
                <div style={{ whiteSpace: 'pre-line', lineHeight: '1.7', fontSize: '14px', color: '#334155' }}>
                  {selectedArticle.content}
                </div>
              </div>
              <div className="modal-footer border-0 pt-2">
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={() => setSelectedArticle(null)}
                >
                  Đóng bài viết
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

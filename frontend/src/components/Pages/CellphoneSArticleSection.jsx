import React, { useState } from 'react';

export default function CellphoneSArticleSection() {
  // Trạng thái thu gọn/mở rộng hộp Nội dung chính (TOC)
  const [isTocOpen, setIsTocOpen] = useState(true);

  // Trạng thái Xem thêm / Thu gọn bài viết tổng thể
  const [isExpanded, setIsExpanded] = useState(false);

  // Hàm cuộn mượt đến mục được bấm trong mục lục
  const handleScrollTo = (e, targetId) => {
    e.preventDefault();
    setIsExpanded(true); // Tự động mở rộng nội dung khi người dùng click vào mục lục
    setTimeout(() => {
      const el = document.getElementById(targetId);
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }
    }, 100);
  };

  return (
    <div className="bg-white rounded-3 border p-4 mb-4 shadow-sm" style={{ fontSize: '14px', lineHeight: '1.65' }}>
      {/* 1. Đoạn văn giới thiệu mở đầu chuẩn CellphoneS */}
      <p className="mb-3 text-secondary text-justify">
        <strong className="text-dark">Máy tính laptop</strong> là thiết bị công nghệ được rất nhiều người dùng yêu thích và chọn lựa nhờ vào tính tiện lợi, đáp ứng tốt nhu cầu học tập, làm việc và giải trí của nhiều người dùng. Hiện nay, các thương hiệu máy tính xách tay không ngừng cải tiến sản phẩm để mang lại một dòng laptop mini giá rẻ mỏng nhẹ, có hiệu năng mạnh mẽ, thời lượng pin tốt đặc biệt là có một giá thành vô cùng hợp lý.
      </p>

      {/* 2. Khung Nội dung chính (Table of Contents - Chuẩn giao diện trong ảnh CellphoneS) */}
      <div className="border rounded-3 overflow-hidden mb-4" style={{ backgroundColor: '#f8fafc' }}>
        {/* Thanh tiêu đề có nút toggle chevron */}
        <div 
          className="d-flex justify-content-between align-items-center px-3 py-2 border-bottom user-select-none"
          style={{ backgroundColor: '#eaedf1', cursor: 'pointer' }}
          onClick={() => setIsTocOpen(!isTocOpen)}
        >
          <span className="fw-bold text-dark" style={{ fontSize: '14px' }}>
            Nội dung chính
          </span>
          <i 
            className={`bi ${isTocOpen ? 'bi-chevron-up' : 'bi-chevron-down'} text-secondary fw-bold`} 
            style={{ fontSize: '13px' }}
          ></i>
        </div>

        {/* Danh sách mục lục phân cấp */}
        {isTocOpen && (
          <div className="p-3" style={{ backgroundColor: '#f1f5f9' }}>
            <ol className="ps-3 mb-0 text-dark" style={{ listStyleType: 'none', margin: 0, paddingLeft: 0 }}>
              <li className="mb-2">
                <a 
                  href="#laptop-may-tinh-xach-tay-la-gi" 
                  onClick={(e) => handleScrollTo(e, 'laptop-may-tinh-xach-tay-la-gi')}
                  className="text-decoration-none text-dark fw-medium hover-danger"
                  style={{ cursor: 'pointer' }}
                >
                  1. Laptop (máy tính xách tay) là gì?
                </a>
              </li>

              <li className="mb-2">
                <a 
                  href="#gia-laptop-bao-nhieu-tien" 
                  onClick={(e) => handleScrollTo(e, 'gia-laptop-bao-nhieu-tien')}
                  className="text-decoration-none text-dark fw-medium hover-danger"
                  style={{ cursor: 'pointer' }}
                >
                  2. Giá laptop bao nhiêu tiền?
                </a>
                <ul className="list-unstyled ps-3 mt-1 text-secondary" style={{ fontSize: '13.5px' }}>
                  <li className="mb-1">
                    <a href="#bang-gia-theo-nhu-cau" onClick={(e) => handleScrollTo(e, 'bang-gia-theo-nhu-cau')} className="text-decoration-none text-secondary hover-danger">
                      2.1. Bảng giá laptop theo nhu cầu
                    </a>
                  </li>
                  <li className="mb-1">
                    <a href="#bang-gia-theo-thuong-hieu" onClick={(e) => handleScrollTo(e, 'bang-gia-theo-thuong-hieu')} className="text-decoration-none text-secondary hover-danger">
                      2.2. Bảng giá laptop theo thương hiệu
                    </a>
                  </li>
                  <li className="mb-1">
                    <a href="#bang-gia-theo-phan-khuc" onClick={(e) => handleScrollTo(e, 'bang-gia-theo-phan-khuc')} className="text-decoration-none text-secondary hover-danger">
                      2.3. Bảng giá laptop theo phân khúc
                    </a>
                  </li>
                  <li className="mb-1">
                    <a href="#cap-nhat-gia-laptop-moi" onClick={(e) => handleScrollTo(e, 'cap-nhat-gia-laptop-moi')} className="text-decoration-none text-secondary hover-danger">
                      2.4. Cập nhật giá các mẫu laptop mới
                    </a>
                  </li>
                </ul>
              </li>

              <li className="mb-2">
                <a 
                  href="#laptop-phuc-vu-cong-viec" 
                  onClick={(e) => handleScrollTo(e, 'laptop-phuc-vu-cong-viec')}
                  className="text-decoration-none text-dark fw-medium hover-danger"
                  style={{ cursor: 'pointer' }}
                >
                  3. Laptop - Phục vụ công việc, học tập, giải trí
                </a>
              </li>

              <li className="mb-2">
                <a 
                  href="#loi-ich-khi-su-dung" 
                  onClick={(e) => handleScrollTo(e, 'loi-ich-khi-su-dung')}
                  className="text-decoration-none text-dark fw-medium hover-danger"
                  style={{ cursor: 'pointer' }}
                >
                  4. Lợi ích khi sử dụng máy tính laptop là gì?
                </a>
              </li>

              <li className="mb-2">
                <a 
                  href="#thong-so-ky-thuat-can-biet" 
                  onClick={(e) => handleScrollTo(e, 'thong-so-ky-thuat-can-biet')}
                  className="text-decoration-none text-dark fw-medium hover-danger"
                  style={{ cursor: 'pointer' }}
                >
                  5. Một số thông số kỹ thuật cần biết về laptop
                </a>
                <ul className="list-unstyled ps-3 mt-1 text-secondary" style={{ fontSize: '13.5px' }}>
                  <li className="mb-1"><a href="#spec-cpu" onClick={(e) => handleScrollTo(e, 'spec-cpu')} className="text-decoration-none text-secondary hover-danger">5.1. CPU</a></li>
                  <li className="mb-1"><a href="#spec-ram" onClick={(e) => handleScrollTo(e, 'spec-ram')} className="text-decoration-none text-secondary hover-danger">5.2. RAM</a></li>
                  <li className="mb-1"><a href="#spec-storage" onClick={(e) => handleScrollTo(e, 'spec-storage')} className="text-decoration-none text-secondary hover-danger">5.3. Ổ cứng</a></li>
                  <li className="mb-1"><a href="#spec-gpu" onClick={(e) => handleScrollTo(e, 'spec-gpu')} className="text-decoration-none text-secondary hover-danger">5.4. GPU</a></li>
                  <li className="mb-1"><a href="#spec-npu" onClick={(e) => handleScrollTo(e, 'spec-npu')} className="text-decoration-none text-secondary hover-danger">5.5. NPU</a></li>
                  <li className="mb-1"><a href="#spec-screen" onClick={(e) => handleScrollTo(e, 'spec-screen')} className="text-decoration-none text-secondary hover-danger">5.6. Màn hình</a></li>
                  <li className="mb-1"><a href="#spec-pin" onClick={(e) => handleScrollTo(e, 'spec-pin')} className="text-decoration-none text-secondary hover-danger">5.7. Pin</a></li>
                  <li className="mb-1"><a href="#spec-os" onClick={(e) => handleScrollTo(e, 'spec-os')} className="text-decoration-none text-secondary hover-danger">5.8. Hệ điều hành</a></li>
                  <li className="mb-1"><a href="#spec-ports" onClick={(e) => handleScrollTo(e, 'spec-ports')} className="text-decoration-none text-secondary hover-danger">5.9. Cổng kết nối</a></li>
                  <li className="mb-1"><a href="#spec-dimensions" onClick={(e) => handleScrollTo(e, 'spec-dimensions')} className="text-decoration-none text-secondary hover-danger">5.10. Kích thước và trọng lượng</a></li>
                </ul>
              </li>

              <li className="mb-2">
                <a 
                  href="#cac-loai-laptop-pho-bien" 
                  onClick={(e) => handleScrollTo(e, 'cac-loai-laptop-pho-bien')}
                  className="text-decoration-none text-dark fw-medium hover-danger"
                  style={{ cursor: 'pointer' }}
                >
                  6. Các loại máy tính laptop phổ biến
                </a>
                <ul className="list-unstyled ps-3 mt-1 text-secondary" style={{ fontSize: '13.5px' }}>
                  <li className="mb-1"><a href="#type-standard" onClick={(e) => handleScrollTo(e, 'type-standard')} className="text-decoration-none text-secondary hover-danger">6.1. Máy tính xách tay thông thường</a></li>
                  <li className="mb-1"><a href="#type-2in1" onClick={(e) => handleScrollTo(e, 'type-2in1')} className="text-decoration-none text-secondary hover-danger">6.2. Laptop 2 in 1</a></li>
                  <li className="mb-1"><a href="#type-workstation" onClick={(e) => handleScrollTo(e, 'type-workstation')} className="text-decoration-none text-secondary hover-danger">6.3. Laptop Workstation</a></li>
                  <li className="mb-1"><a href="#type-ai" onClick={(e) => handleScrollTo(e, 'type-ai')} className="text-decoration-none text-secondary hover-danger">6.4. Laptop AI (Copilot+ PC thế hệ mới)</a></li>
                </ul>
              </li>

              <li className="mb-0">
                <a 
                  href="#tieu-chi-chon-mua" 
                  onClick={(e) => handleScrollTo(e, 'tieu-chi-chon-mua')}
                  className="text-decoration-none text-dark fw-medium hover-danger"
                  style={{ cursor: 'pointer' }}
                >
                  7. Các tiêu chí chọn mua laptop chất lượng vượt trội
                </a>
                <ul className="list-unstyled ps-3 mt-1 text-secondary" style={{ fontSize: '13.5px' }}>
                  <li className="mb-1"><a href="#tieu-chi-thuong-hieu" onClick={(e) => handleScrollTo(e, 'tieu-chi-thuong-hieu')} className="text-decoration-none text-secondary hover-danger">7.1. Tiêu chí chọn mua dựa trên thương hiệu</a></li>
                  <li className="mb-1"><a href="#tieu-chi-thiet-ke" onClick={(e) => handleScrollTo(e, 'tieu-chi-thiet-ke')} className="text-decoration-none text-secondary hover-danger">7.2. Tiêu chí chọn mua dựa trên thiết kế</a></li>
                </ul>
              </li>
            </ol>
          </div>
        )}
      </div>

      {/* 3. Phần nội dung chi tiết bài viết (Kèm giới hạn chiều cao & Nút Xem thêm / Thu gọn) */}
      <div 
        className="position-relative overflow-hidden" 
        style={{ 
          maxHeight: isExpanded ? 'none' : '480px', 
          transition: 'max-height 0.4s ease' 
        }}
      >
        {/* Section 1 */}
        <section id="laptop-may-tinh-xach-tay-la-gi" className="mb-4">
          <h3 className="fw-bold text-dark fs-5 mb-2 border-start border-4 border-danger ps-2">
            1. Laptop (máy tính xách tay) là gì?
          </h3>
          <p className="text-secondary">
            Laptop (hay máy tính xách tay) là một thiết bị máy vi tính cá nhân có kích thước nhỏ gọn, thiết kế gập hình vỏ sò (clamshell) và có thể mang đi bất kỳ nơi đâu. Một chiếc laptop tiêu chuẩn tích hợp đầy đủ các linh kiện phần cứng cần thiết gồm màn hình hiển thị, bàn phím, bàn rê chuột (touchpad), loa âm thanh, vi xử lý (CPU), card đồ họa (GPU), bộ nhớ RAM, ổ lưu trữ và viên pin sạc hoạt động độc lập nhiều giờ liên tục.
          </p>
        </section>

        {/* Section 2 */}
        <section id="gia-laptop-bao-nhieu-tien" className="mb-4">
          <h3 className="fw-bold text-dark fs-5 mb-2 border-start border-4 border-danger ps-2">
            2. Giá laptop bao nhiêu tiền?
          </h3>
          <p className="text-secondary mb-3">
            Tại thị trường Việt Nam hiện nay, mức giá laptop vô cùng đa dạng, dao động từ khoảng <strong>8 triệu đồng</strong> cho các model phổ thông cơ bản đến trên <strong>100 triệu đồng</strong> cho các dòng máy trạm và gaming cao cấp. Dưới đây là các bảng tổng hợp giá tham khảo chi tiết:
          </p>

          {/* 2.1 Bảng giá theo nhu cầu */}
          <div id="bang-gia-theo-nhu-cau" className="mb-3">
            <h6 className="fw-bold text-dark">2.1. Bảng giá laptop theo nhu cầu sử dụng</h6>
            <div className="table-responsive">
              <table className="table table-bordered table-striped table-hover small mb-2">
                <thead className="table-danger text-dark">
                  <tr>
                    <th>Nhu cầu sử dụng</th>
                    <th>Mức giá trung bình</th>
                    <th>Dòng máy tiêu biểu</th>
                    <th>Đặc điểm nổi bật</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td className="fw-semibold text-danger">Học tập - Văn phòng</td>
                    <td>9.490.000đ - 17.990.000đ</td>
                    <td>Asus Vivobook, Acer Aspire, HP 15</td>
                    <td>Mỏng nhẹ, gõ văn bản êm, pin từ 6 - 8 tiếng</td>
                  </tr>
                  <tr>
                    <td className="fw-semibold text-danger">Gaming - Thể thao điện tử</td>
                    <td>17.990.000đ - 55.000.000đ</td>
                    <td>Asus TUF/ROG, Acer Nitro V, Lenovo LOQ</td>
                    <td>Màn hình 144Hz-240Hz, Card RTX 3050/4060, Tản nhiệt khủng</td>
                  </tr>
                  <tr>
                    <td className="fw-semibold text-danger">Đồ họa - Kỹ thuật 3D</td>
                    <td>21.990.000đ - 65.000.000đ</td>
                    <td>Dell XPS, MacBook Pro, ThinkPad P-Series</td>
                    <td>Màn hình OLED/Retina chuẩn màu 100% sRGB, CPU đa nhân</td>
                  </tr>
                  <tr>
                    <td className="fw-semibold text-danger">Doanh nhân - Siêu mỏng nhẹ</td>
                    <td>24.990.000đ - 75.000.000đ</td>
                    <td>Apple MacBook Air, Zenbook OLED, HP Envy</td>
                    <td>Vỏ nhôm nguyên khối, trọng lượng dưới 1.3kg, bảo mật vân tay</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          {/* 2.2 Bảng giá theo thương hiệu */}
          <div id="bang-gia-theo-thuong-hieu" className="mb-3">
            <h6 className="fw-bold text-dark">2.2. Bảng giá laptop theo thương hiệu</h6>
            <p className="text-secondary small">
              Mỗi thương hiệu đều có phân khúc thế mạnh riêng: <strong>Apple MacBook</strong> dẫn đầu về độ bền bỉ và thời lượng pin; <strong>ASUS & Acer</strong> cung cấp tỉ lệ cấu hình/giá tiền tốt nhất; <strong>Dell & Lenovo ThinkPad</strong> nổi tiếng với độ bền chuẩn quân đội và bàn phím gõ êm ái hàng đầu.
            </p>
          </div>

          {/* 2.3 Bảng giá theo phân khúc */}
          <div id="bang-gia-theo-phan-khuc" className="mb-3">
            <h6 className="fw-bold text-dark">2.3. Bảng giá laptop theo phân khúc</h6>
            <ul className="text-secondary ps-3">
              <li><strong>Phân khúc giá rẻ (Dưới 12 triệu):</strong> Phù hợp học sinh, sinh viên học online, lướt web, làm Word, Excel.</li>
              <li><strong>Phân khúc tầm trung (12 - 20 triệu):</strong> Cấu hình Core i5/Ryzen 5, RAM 16GB, đáp ứng mượt mà mọi tác vụ văn phòng và đa nhiệm.</li>
              <li><strong>Phân khúc cận cao cấp (20 - 30 triệu):</strong> Dành cho người sáng tạo nội dung, lập trình viên, game thủ chiến các tựa game AAA.</li>
              <li><strong>Phân khúc cao cấp (Trên 30 triệu):</strong> Hoàn thiện đỉnh cao, chất liệu cao cấp, độ tin cậy và hiệu năng không giới hạn.</li>
            </ul>
          </div>

          {/* 2.4 Cập nhật giá các mẫu laptop mới */}
          <div id="cap-nhat-gia-laptop-moi" className="mb-3">
            <h6 className="fw-bold text-dark">2.4. Cập nhật giá các mẫu laptop mới 2026</h6>
            <p className="text-secondary small">
              Giá sản phẩm luôn được hệ thống PCStore cập nhật tự động hàng ngày kèm theo nhiều voucher giảm giá sốc, chính sách hỗ trợ trả góp 0% lãi suất và tặng kèm balo, chuột không dây chính hãng.
            </p>
          </div>
        </section>

        {/* Section 3 */}
        <section id="laptop-phuc-vu-cong-viec" className="mb-4">
          <h3 className="fw-bold text-dark fs-5 mb-2 border-start border-4 border-danger ps-2">
            3. Laptop - Phục vụ công việc, học tập, giải trí
          </h3>
          <p className="text-secondary">
            Trong thời đại công nghệ số và làm việc linh hoạt (hybrid working), laptop trở thành trợ thủ đắc lực không thể thay thế. Thiết bị cho phép bạn xử lý hồ sơ văn bản ở quán cà phê, tham gia các buổi họp trực tuyến Microsoft Teams / Zoom qua webcam sắc nét, thiết kế đồ họa Adobe Photoshop / Premiere và chơi các tựa game đình đám mọi lúc mọi nơi.
          </p>
        </section>

        {/* Section 4 */}
        <section id="loi-ich-khi-su-dung" className="mb-4">
          <h3 className="fw-bold text-dark fs-5 mb-2 border-start border-4 border-danger ps-2">
            4. Lợi ích khi sử dụng máy tính laptop là gì?
          </h3>
          <div className="row g-3">
            <div className="col-12 col-md-6">
              <div className="p-3 bg-light rounded-2 border h-100">
                <div className="fw-bold text-danger mb-1"><i className="bi bi-geo-alt-fill me-1"></i> Tính cơ động và di chuyển linh hoạt</div>
                <div className="text-secondary small">Thiết kế gọn gàng, trọng lượng chỉ từ 1kg giúp bạn mang theo đi học, đi công tác dễ dàng.</div>
              </div>
            </div>
            <div className="col-12 col-md-6">
              <div className="p-3 bg-light rounded-2 border h-100">
                <div className="fw-bold text-danger mb-1"><i className="bi bi-battery-charging me-1"></i> Hoạt động ngay cả khi mất điện</div>
                <div className="text-secondary small">Tích hợp pin dung lượng lớn cho phép duy trì làm việc từ 6 đến 12 tiếng liên tục mà không lo gián đoạn.</div>
              </div>
            </div>
            <div className="col-12 col-md-6">
              <div className="p-3 bg-light rounded-2 border h-100">
                <div className="fw-bold text-danger mb-1"><i className="bi bi-lightning-charge-fill me-1"></i> Tiết kiệm điện năng vượt trội</div>
                <div className="text-secondary small">Các dòng chip di động thế hệ mới tiêu thụ chỉ từ 15W - 45W, tiết kiệm hơn gấp nhiều lần so với máy tính để bàn.</div>
              </div>
            </div>
            <div className="col-12 col-md-6">
              <div className="p-3 bg-light rounded-2 border h-100">
                <div className="fw-bold text-danger mb-1"><i className="bi bi-wifi me-1"></i> Kết nối không dây toàn diện</div>
                <div className="text-secondary small">Trang bị chuẩn Wi-Fi 6E / Wi-Fi 7 và Bluetooth 5.3 giúp truyền tải dữ liệu và kết nối mạng siêu tốc.</div>
              </div>
            </div>
          </div>
        </section>

        {/* Section 5 */}
        <section id="thong-so-ky-thuat-can-biet" className="mb-4">
          <h3 className="fw-bold text-dark fs-5 mb-2 border-start border-4 border-danger ps-2">
            5. Một số thông số kỹ thuật cần biết về laptop
          </h3>
          <div className="ps-2">
            <div id="spec-cpu" className="mb-2">
              <strong className="text-dark">5.1. CPU (Bộ vi xử lý trung tâm):</strong> 
              <span className="text-secondary"> Là bộ não xử lý mọi phép toán. Hiện có 2 dòng chip phổ biến là Intel (Core i3, i5, i7, i9, Core Ultra) và AMD (Ryzen 3, 5, 7, 9) cùng chip Apple Silicon (M2, M3, M4).</span>
            </div>
            <div id="spec-ram" className="mb-2">
              <strong className="text-dark">5.2. RAM (Bộ nhớ truy xuất ngẫu nhiên):</strong> 
              <span className="text-secondary"> Giúp chạy nhiều ứng dụng cùng lúc. Tối thiểu nên chọn 8GB cho văn phòng cơ bản, 16GB - 32GB cho đồ họa và chơi game mượt mà.</span>
            </div>
            <div id="spec-storage" className="mb-2">
              <strong className="text-dark">5.3. Ổ cứng (SSD NVMe):</strong> 
              <span className="text-secondary"> Chuẩn SSD PCIe NVMe cho tốc độ mở máy và khởi động ứng dụng chỉ trong vài giây. Dung lượng chuẩn hiện nay từ 512GB đến 1TB hoặc 2TB.</span>
            </div>
            <div id="spec-gpu" className="mb-2">
              <strong className="text-dark">5.4. GPU (Card màn hình):</strong> 
              <span className="text-secondary"> Card tích hợp (Intel Iris Xe, Radeon Graphics) phù hợp văn phòng; card rời (NVIDIA GeForce RTX 4050/4060/4070/4080) dành cho gaming và dựng video chuyên nghiệp.</span>
            </div>
            <div id="spec-npu" className="mb-2">
              <strong className="text-dark">5.5. NPU (Bộ xử lý trí tuệ nhân tạo AI):</strong> 
              <span className="text-secondary"> Chip AI chuyên biệt trên Intel Core Ultra và Snapdragon X Elite giúp chạy các tính năng Windows Copilot+ mượt mà không hao pin.</span>
            </div>
            <div id="spec-screen" className="mb-2">
              <strong className="text-dark">5.6. Màn hình:</strong> 
              <span className="text-secondary"> Kích thước 13.3 inch, 14 inch, 15.6 inch, 16 inch. Tấm nền IPS hoặc OLED với tần số quét 60Hz, 120Hz hoặc 144Hz-240Hz cho hình ảnh mượt mà, chân thực.</span>
            </div>
            <div id="spec-pin" className="mb-2">
              <strong className="text-dark">5.7. Pin & Sạc nhanh:</strong> 
              <span className="text-secondary"> Dung lượng từ 45Wh đến 99Wh, hỗ trợ sạc nhanh chuẩn USB Type-C Power Delivery tiện lợi.</span>
            </div>
            <div id="spec-os" className="mb-2">
              <strong className="text-dark">5.8. Hệ điều hành:</strong> 
              <span className="text-secondary"> Windows 11 Home/Pro bản quyền hoặc macOS với giao diện trực quan và bảo mật cao.</span>
            </div>
            <div id="spec-ports" className="mb-2">
              <strong className="text-dark">5.9. Cổng kết nối:</strong> 
              <span className="text-secondary"> Thunderbolt 4, Type-C, USB-A, HDMI 2.1, Jack 3.5mm và khe thẻ nhớ SD.</span>
            </div>
            <div id="spec-dimensions" className="mb-0">
              <strong className="text-dark">5.10. Kích thước và trọng lượng:</strong> 
              <span className="text-secondary"> Laptop mỏng nhẹ nặng từ 1.0kg - 1.4kg, laptop gaming nặng từ 2.0kg - 2.5kg.</span>
            </div>
          </div>
        </section>

        {/* Section 6 */}
        <section id="cac-loai-laptop-pho-bien" className="mb-4">
          <h3 className="fw-bold text-dark fs-5 mb-2 border-start border-4 border-danger ps-2">
            6. Các loại máy tính laptop phổ biến
          </h3>
          <div className="ps-2">
            <div id="type-standard" className="mb-2">
              <strong className="text-dark">6.1. Máy tính xách tay thông thường:</strong> 
              <span className="text-secondary"> Thiết kế gập mở truyền thống 180 độ, độ bền cao, giá thành hợp lý và dễ nâng cấp linh kiện.</span>
            </div>
            <div id="type-2in1" className="mb-2">
              <strong className="text-dark">6.2. Laptop 2 in 1:</strong> 
              <span className="text-secondary"> Màn hình cảm ứng có thể xoay gập 360 độ hoặc tháo rời bàn phím để dùng như máy tính bảng kèm bút cảm ứng vẽ chuyên nghiệp.</span>
            </div>
            <div id="type-workstation" className="mb-2">
              <strong className="text-dark">6.3. Laptop Workstation (Máy trạm di động):</strong> 
              <span className="text-secondary"> Cấu hình siêu khủng, card đồ họa chuyên dụng NVIDIA RTX Ada/Quadro phục vụ render 3D, kiến trúc và mô phỏng cơ khí.</span>
            </div>
            <div id="type-ai" className="mb-0">
              <strong className="text-dark">6.4. Laptop AI (Copilot+ PC thế hệ mới):</strong> 
              <span className="text-secondary"> Tích hợp NPU xử lý hơn 40 TOPS cho các tác vụ AI tạo sinh, xóa phông camera, dịch phụ đề thời gian thực (Live Captions).</span>
            </div>
          </div>
        </section>

        {/* Section 7 */}
        <section id="tieu-chi-chon-mua" className="mb-3">
          <h3 className="fw-bold text-dark fs-5 mb-2 border-start border-4 border-danger ps-2">
            7. Các tiêu chí chọn mua laptop chất lượng vượt trội
          </h3>
          <div className="ps-2">
            <div id="tieu-chi-thuong-hieu" className="mb-2">
              <strong className="text-dark">7.1. Tiêu chí chọn mua dựa trên thương hiệu:</strong> 
              <span className="text-secondary"> Nên chọn mua từ các hãng uy tín lâu năm như Asus, Dell, Apple, Acer, Lenovo, HP để đảm bảo linh kiện chuẩn mực và hệ thống trung tâm bảo hành rộng khắp.</span>
            </div>
            <div id="tieu-chi-thiet-ke" className="mb-0">
              <strong className="text-dark">7.2. Tiêu chí chọn mua dựa trên thiết kế:</strong> 
              <span className="text-secondary"> Nếu thường xuyên di chuyển nên ưu tiên máy vỏ kim loại nhẹ dưới 1.4kg. Nếu chơi game hoặc làm đồ họa nặng, hãy chọn các dòng có hệ thống tản nhiệt kép và nhiều khe thoát nhiệt.</span>
            </div>
          </div>
        </section>

        {/* Lớp phủ mờ Gradient khi đang ở trạng thái thu gọn (CellphoneS Style) */}
        {!isExpanded && (
          <div 
            className="position-absolute bottom-0 start-0 w-100" 
            style={{ 
              height: '140px', 
              background: 'linear-gradient(to bottom, rgba(255,255,255,0) 0%, rgba(255,255,255,0.95) 80%, rgba(255,255,255,1) 100%)',
              pointerEvents: 'none'
            }}
          ></div>
        )}
      </div>

      {/* 4. Nút bấm Xem thêm nội dung / Thu gọn chuẩn CellphoneS */}
      <div className="text-center mt-3 pt-2 border-top">
        <button
          type="button"
          className="btn btn-outline-danger btn-sm rounded-pill px-4 fw-bold shadow-sm"
          onClick={() => setIsExpanded(!isExpanded)}
          style={{ fontSize: '13px' }}
        >
          {isExpanded ? (
            <>
              Thu gọn nội dung <i className="bi bi-chevron-up ms-1"></i>
            </>
          ) : (
            <>
              Xem thêm nội dung <i className="bi bi-chevron-down ms-1"></i>
            </>
          )}
        </button>
      </div>
    </div>
  );
}

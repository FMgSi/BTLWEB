export default function ProductCard({ item }) {
  const formatVND = (num) => (num ? num.toLocaleString('vi-VN') + 'đ' : '');

  return (
    <div className="card h-100 product-card p-2 position-relative d-flex flex-column justify-content-between">
      <div>
        {/* Badge giảm giá / Mới */}
        {item.badge && (
          <span className={`badge ${item.badgeColor} badge-custom position-absolute top-0 start-0 m-2`}>
            {item.badge}
          </span>
        )}

        {/* Ảnh sản phẩm bọc trong link chi tiết */}
        <a 
          href={`/product/${item.id}`} 
          className="d-flex justify-content-center align-items-center py-2 text-decoration-none"
        >
          <img
            src={item.image}
            alt={item.name}
            className="img-fluid rounded"
            style={{ height: '120px', width: '100%', objectFit: 'contain' }}
          />
        </a>

        {/* Tên và thông số */}
        <div className="p-1">
          <a 
            href={`/product/${item.id}`} 
            className="text-decoration-none text-dark"
          >
            <h6 className="product-title mb-1" title={item.name}>
              {item.name}
            </h6>
          </a>
          <p className="product-specs">{item.specs}</p>
        </div>
      </div>

      {/* Giá tiền và 3 Icon liên kết */}
      <div className="p-1 pt-0">
        <div className="mb-2">
          <span className="text-pc-red fw-bold d-block" style={{ fontSize: '14px', letterSpacing: '-0.3px' }}>
            {formatVND(item.price)}
          </span>
          {item.originalPrice ? (
            <span className="text-muted text-decoration-line-through" style={{ fontSize: '11px' }}>
              {formatVND(item.originalPrice)}
            </span>
          ) : (
            <span style={{ fontSize: '11px', visibility: 'hidden' }}>-</span>
          )}
        </div>

        {/* Cụm 3 icon có gắn link chuyển trang */}
        <div className="d-flex align-items-center gap-2 pt-2 border-top">
          {/* 1. Nút thêm vào giỏ hàng */}
          <a
            href={`/cart?add=${item.id}`}
            className="card-action-link"
            title="Thêm vào giỏ hàng"
            onClick={(e) => {
              // Có thể bỏ dòng này nếu muốn điều hướng thật qua trang khác
              // e.preventDefault();
              // alert(`Đã thêm ${item.name} vào giỏ!`);
            }}
          >
            <i className="bi bi-cart3"></i>
          </a>

          {/* 2. Nút thêm vào mục yêu thích */}
          <a
            href={`/wishlist?add=${item.id}`}
            className="card-action-link"
            title="Yêu thích sản phẩm"
          >
            <i className="bi bi-heart"></i>
          </a>

          {/* 3. Nút so sánh sản phẩm */}
          <a
            href={`/compare?id=${item.id}`}
            className="card-action-link"
            title="So sánh cấu hình"
          >
            <i className="bi bi-bar-chart"></i>
          </a>
        </div>
      </div>
    </div>
  );
}
import { formatVND } from '../../utils/formatters';

export default function ProductCard({ item, onSelectProduct }) {
  const handleClickDetail = (e) => {
    if (onSelectProduct && !e.ctrlKey && !e.metaKey && !e.shiftKey) {
      e.preventDefault();
      onSelectProduct(item.id);
    }
  };

  return (
    <div className="card h-100 product-card p-2 position-relative d-flex flex-column justify-content-between">
      <div>
        {item.badge && (
          <span className={`badge ${item.badgeColor || 'bg-danger'} badge-custom position-absolute top-0 start-0 m-2`}>
            {item.badge}
          </span>
        )}

        <a 
          href={`/product/${item.id}`} 
          onClick={handleClickDetail}
          className="d-flex justify-content-center align-items-center py-2 text-decoration-none"
        >
          <img
            src={item.image}
            alt={item.name}
            className="img-fluid rounded"
            style={{ height: '120px', width: '100%', objectFit: 'contain' }}
            onError={(e) => {
              e.target.onerror = null;
              e.target.src = 'https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=400&q=80';
            }}
          />
        </a>

        <div className="p-1">
          <a 
            href={`/product/${item.id}`} 
            onClick={handleClickDetail}
            className="text-decoration-none text-dark"
          >
            <h6 className="product-title mb-1" title={item.name}>
              {item.name}
            </h6>
          </a>
          <p className="product-specs">{item.specs}</p>
        </div>
      </div>

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

        <div className="d-flex align-items-center gap-2 pt-2 border-top">
          <button
            type="button"
            className="card-action-link bg-transparent border-0"
            title="Chọn màu sắc & cấu hình để mua"
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              if (onSelectProduct) {
                onSelectProduct(item.id);
              }
            }}
          >
            <i className="bi bi-cart3"></i>
          </button>

          <a
            href={`/wishlist?add=${item.id}`}
            className="card-action-link"
            title="Yêu thích sản phẩm"
            onClick={(e) => {
              e.preventDefault();
              alert(`Đã lưu ${item.name} vào danh sách yêu thích!`);
            }}
          >
            <i className="bi bi-heart"></i>
          </a>

          <a
            href={`/product/${item.id}`}
            onClick={handleClickDetail}
            className="card-action-link"
            title="Xem chi tiết sản phẩm"
          >
            <i className="bi bi-eye"></i>
          </a>
        </div>
      </div>
    </div>
  );
}

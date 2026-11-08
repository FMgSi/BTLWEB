import { useState, useEffect } from 'react';
import { formatVND } from '../../utils/formatters';
import './ProductDetail.css';
import { PRODUCT_DATA } from '../../data/mockData';

export default function ProductDetail({ productId, onBack, onSelectProduct, allProducts = [], onAddToCart, onBuyNow }) {
  const [product, setProduct] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [fetchError, setFetchError] = useState('');

  // States quản lý Gallery ảnh & Tự động chuyển ảnh mỗi 10s
  const [selectedImageIndex, setSelectedImageIndex] = useState(0);
  const [isPaused, setIsPaused] = useState(false);

  // States lựa chọn Màu sắc & Biến thể (Variants)
  const [selectedColorId, setSelectedColorId] = useState(null);
  const [selectedVariantId, setSelectedVariantId] = useState(null);

  // States lựa chọn cấu hình RAM và Ổ cứng
  const [selectedRamIndex, setSelectedRamIndex] = useState(0);
  const [selectedStorageIndex, setSelectedStorageIndex] = useState(0);

  const [isArticleExpanded, setIsArticleExpanded] = useState(false);
  const [toastMessage, setToastMessage] = useState('');
  const [showToast, setShowToast] = useState(false);



  // Trigger toast thông báo
  const triggerToast = (msg) => {
    setToastMessage(msg);
    setShowToast(true);
    setTimeout(() => {
      setShowToast(false);
    }, 3500);
  };

  // 1. Tải thông tin sản phẩm từ DB qua API /api/products/{id}
  useEffect(() => {
    let isMounted = true;

    const fetchDetail = async () => {
      try {
        const res = await fetch(`/api/products/${productId}`);
        if (!res.ok) {
          throw new Error(`Mã lỗi HTTP ${res.status}`);
        }
        const data = await res.json();
        if (isMounted) {
          let imgs = (data.images && data.images.length > 0)
            ? data.images
            : [data.image || data.thumbnailUrl || '/images/default.png'];

          // Đảm bảo luôn có nhiều góc chụp để trình chiếu slide
          if (imgs.length <= 1) {
            const isGaming = (data.category || '').toLowerCase().includes('gaming');
            imgs = [
              imgs[0],
              isGaming ? 'https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=800&q=80' : 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&q=80',
              isGaming ? 'https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&q=80' : 'https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=800&q=80',
              'https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=800&q=80'
            ];
          }

          const enriched = {
            id: data.id || data.productId,
            name: data.name || data.productName,
            brand: data.brand || data.brandName || 'Chính hãng',
            category: data.category || data.categoryName || 'Laptop',
            price: data.price,
            originalPrice: data.originalPrice || (data.price * 1.15),
            discountPercent: data.discountPercent || (data.originalPrice ? Math.round((1 - data.price / data.originalPrice) * 100) : 10),
            stockQuantity: data.stockQuantity ?? 15,
            image: data.image || data.thumbnailUrl || imgs[0],
            images: imgs,
            description: data.description || '',
            specification: data.specification || {
              cpu: 'Intel Core i5 / AMD Ryzen 5 thế hệ mới',
              ramGB: 16,
              storageGB: 512,
              storageType: 'SSD NVMe PCIe 4.0',
              gpu: 'Đồ họa tích hợp / Rời',
              screenSizeInch: 14.0,
              refreshRateHz: 60,
              weightKg: 1.45,
              batteryWh: 54,
              os: 'Windows 11 Home Bản quyền'
            },
            colorImages: data.colorImages || {},
            variants: data.variants || []
          };

          if (enriched.variants && enriched.variants.length > 0) {
            setSelectedColorId(enriched.variants[0].colorId);
            setSelectedVariantId(enriched.variants[0].variantId);
          }

          setProduct(enriched);
          setFetchError('');
          setIsLoading(false);
        }
      } catch (err) {
        console.warn('Lỗi kết nối API Backend khi tải chi tiết sản phẩm:', err);
        if (isMounted) {
          setFetchError(err.message || 'Không thể tải thông tin sản phẩm từ máy chủ.');
          setIsLoading(false);
        }
      }
    };

    fetchDetail();
    window.scrollTo({ top: 0, behavior: 'smooth' });

    return () => {
      isMounted = false;
    };
  }, [productId]);

  // Quản lý biến thể màu sắc và cấu hình
  const hasVariants = Boolean(product?.variants && product.variants.length > 0);
  const variants = product?.variants ?? [];
  const colorsById = new Map();
  for (const variant of variants) {
    if (!colorsById.has(variant.colorId)) {
      colorsById.set(variant.colorId, {
        colorId: variant.colorId,
        colorName: variant.colorName,
        hexCode: variant.hexCode || '#94a3b8'
      });
    }
  }
  const availableColors = Array.from(colorsById.values());
  const activeColorId = selectedColorId ?? availableColors[0]?.colorId;
  const variantsForCurrentColor = variants.filter((v) => v.colorId === activeColorId);
  const activeVariant = variantsForCurrentColor.find((v) => v.variantId === selectedVariantId)
    ?? variantsForCurrentColor[0] ?? null;

  // Bộ 4 ảnh của sản phẩm (không phân chia theo màu sắc, một sản phẩm chỉ có 4 ảnh dù cho có bao nhiêu biến thể)
  const productImages = (product?.images && product.images.length > 0)
    ? product.images.slice(0, 4)
    : [product?.image || product?.thumbnailUrl || '/images/default.png'];

  const genericImg = productImages[0] || '/images/default.png';
  const activeGallery = productImages;

  const safeImageIndex = selectedImageIndex < activeGallery.length ? selectedImageIndex : 0;
  const currentImg = activeGallery[safeImageIndex] || activeGallery[0] || genericImg;

  // Hàm bấm nút mũi tên lùi ảnh trước
  const handlePrevImage = () => {
    if (!activeGallery || activeGallery.length <= 1) return;
    setSelectedImageIndex((prev) => (prev === 0 ? activeGallery.length - 1 : prev - 1));
  };

  // Hàm bấm nút mũi tên tiến ảnh kế
  const handleNextImage = () => {
    if (!activeGallery || activeGallery.length <= 1) return;
    setSelectedImageIndex((prev) => (prev + 1) % activeGallery.length);
  };

  // 2. Tự động chuyển ảnh mỗi 10 giây (10s auto-slide)
  useEffect(() => {
    if (!activeGallery || activeGallery.length <= 1 || isPaused) return;

    const interval = setInterval(() => {
      setSelectedImageIndex((prev) => (prev + 1) % activeGallery.length);
    }, 10000);

    return () => clearInterval(interval);
  }, [activeGallery, isPaused]);

  if (isLoading) {
    return (
      <div className="site-container py-5 text-center">
        <div className="bg-white p-5 rounded-3 border shadow-sm my-4">
          <div className="spinner-border text-danger mb-3" style={{ width: '3rem', height: '3rem' }} role="status">
            <span className="visually-hidden">Đang tải dữ liệu...</span>
          </div>
          <h5 className="fw-bold text-dark mb-1">Đang lấy thông tin sản phẩm từ CSDL...</h5>
          <p className="text-muted small mb-0">Vui lòng chờ trong giây lát</p>
        </div>
      </div>
    );
  }

  if (fetchError || !product) {
    return (
      <div className="site-container py-5 text-center">
        <div className="bg-white p-5 rounded-3 border shadow-sm my-4">
          <i className="bi bi-exclamation-circle-fill text-danger fs-1 mb-3 d-block"></i>
          <h4 className="fw-bold mb-2 text-danger">Không thể tải thông tin sản phẩm</h4>
          <p className="text-muted mb-4">{fetchError || 'Sản phẩm có thể đã ngừng kinh doanh hoặc đường dẫn không chính xác.'}</p>
          <div className="d-flex justify-content-center gap-2">
            <button onClick={() => window.location.reload()} className="btn btn-outline-danger px-4 py-2">
              <i className="bi bi-arrow-clockwise me-1"></i>Thử lại
            </button>
            <button onClick={onBack} className="btn btn-secondary px-4 py-2">
              <i className="bi bi-arrow-left me-1"></i>Quay lại danh sách sản phẩm
            </button>
          </div>
        </div>
      </div>
    );
  }

  const spec = product.specification || {};
  // Cấu hình cơ bản từ Database
  const baseRamGB = spec.ramGB || 16;
  const baseStorageGB = spec.storageGB || 512;

  // Danh sách tùy chọn RAM kèm chi phí phụ thu
  const ramOptions = [
    { label: `${baseRamGB} GB (Cấu hình chuẩn)`, extra: 0, ramGB: baseRamGB, isBase: true }
  ];

  // Danh sách tùy chọn Ổ cứng chuẩn của máy từ CSDL
  const storageOptions = [
    { 
      label: `${baseStorageGB >= 1000 ? (baseStorageGB / 1000) + ' TB' : baseStorageGB + ' GB'} SSD (Cấu hình chuẩn)`, 
      extra: 0, 
      storageGB: baseStorageGB, 
      isBase: true 
    }
  ];

  // Xử lý đổi màu: ưu tiên giữ RAM/SSD nếu màu mới có cấu hình tương đương và reset về ảnh số 1 của màu đó
  const handleSelectColor = (newColorId) => {
    setSelectedColorId(newColorId);
    setSelectedImageIndex(0);
    if (!product?.variants) return;
    const sameConfig = product.variants.find(
      (v) => v.colorId === newColorId && v.ramGB === activeVariant?.ramGB && v.storageGB === activeVariant?.storageGB
    );
    if (sameConfig) {
      setSelectedVariantId(sameConfig.variantId);
    } else {
      const firstVar = product.variants.find((v) => v.colorId === newColorId);
      if (firstVar) {
        setSelectedVariantId(firstVar.variantId);
      }
    }
  };

  // Tồn kho và giá động theo biến thể
  const currentStock = hasVariants
    ? (activeVariant?.stockQuantity ?? 0)
    : product.stockQuantity;
  const isOutOfStock = currentStock <= 0;

  // Tính toán giá động theo cấu hình đang chọn
  const activeRamOpt = ramOptions[selectedRamIndex] || ramOptions[0];
  const activeStorageOpt = storageOptions[selectedStorageIndex] || storageOptions[0];
  const totalExtra = (activeRamOpt?.extra || 0) + (activeStorageOpt?.extra || 0);

  const displayPrice = hasVariants && activeVariant
    ? (activeVariant.effectivePrice ?? (activeVariant.discountPrice || activeVariant.price || product.price))
    : (product.price + totalExtra);

  const displayOriginalPrice = hasVariants && activeVariant
    ? ((activeVariant.discountPrice && activeVariant.price) ? activeVariant.price : product.originalPrice)
    : (product.originalPrice ? product.originalPrice + totalExtra : null);

  const displaySavings = (displayOriginalPrice && displayOriginalPrice > displayPrice)
    ? (displayOriginalPrice - displayPrice)
    : 0;
  const displayDiscountPercent = displayOriginalPrice
    ? Math.round((1 - displayPrice / displayOriginalPrice) * 100)
    : 0;

  // Lấy danh sách sản phẩm liên quan
  const relatedList = allProducts
    .filter((p) => p.id !== product.id)
    .slice(0, 4);

  return (
    <div className="product-detail-container site-container py-3">
      {/* Toast thông báo mua hàng */}
      {showToast && (
        <div 
          className="position-fixed top-0 end-0 p-3" 
          style={{ zIndex: 1080, marginTop: '70px' }}
        >
          <div className="toast show align-items-center text-white bg-success border-0 shadow-lg" role="alert">
            <div className="d-flex">
              <div className="toast-body d-flex align-items-center gap-2 fw-semibold">
                <i className="bi bi-check-circle-fill fs-5"></i>
                <span>{toastMessage}</span>
              </div>
              <button 
                type="button" 
                className="btn-close btn-close-white me-2 m-auto" 
                onClick={() => setShowToast(false)}
              ></button>
            </div>
          </div>
        </div>
      )}

      {/* 1. Breadcrumb & Nút Quay lại chuẩn FPT Shop */}
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3 pb-2 border-bottom">
        <nav aria-label="breadcrumb" className="breadcrumb-nav">
          <ol className="breadcrumb mb-0">
            <li className="breadcrumb-item">
              <a href="#" onClick={(e) => { e.preventDefault(); onBack(); }}>
                <i className="bi bi-house-door-fill me-1"></i>Trang chủ
              </a>
            </li>
            <li className="breadcrumb-item">
              <a href="#" onClick={(e) => { e.preventDefault(); onBack(); }}>Laptop</a>
            </li>
            <li className="breadcrumb-item">
              <a href="#" onClick={(e) => { e.preventDefault(); onBack(); }}>Laptop {product.brand}</a>
            </li>
            <li className="breadcrumb-item active text-truncate" style={{ maxWidth: '300px' }} aria-current="page">
              {product.name}
            </li>
          </ol>
        </nav>

        <button onClick={onBack} className="btn-back-catalog">
          <i className="bi bi-arrow-left"></i>
          <span>Quay lại danh sách</span>
        </button>
      </div>

      {/* 2. Tiêu đề sản phẩm & Đánh giá sao */}
      <div className="mb-3">
        <h1 className="fw-bold fs-4 text-dark mb-2 lh-sm">
          {product.name}
        </h1>
        <div className="d-flex flex-wrap align-items-center gap-3 text-secondary" style={{ fontSize: '13px' }}>
          <div className="d-flex align-items-center gap-1 text-warning">
            <i className="bi bi-star-fill"></i>
            <i className="bi bi-star-fill"></i>
            <i className="bi bi-star-fill"></i>
            <i className="bi bi-star-fill"></i>
            <i className="bi bi-star-fill"></i>
            <span className="text-dark fw-bold ms-1">5.0</span>
            <span className="text-muted">(128 đánh giá)</span>
          </div>
          <span className="text-muted">|</span>
          <div>
            <span className="text-muted">Mã SP: </span>
            <span className="fw-semibold text-dark">#0092{product.id}</span>
          </div>
          <span className="text-muted">|</span>
          <div className="text-success fw-semibold d-flex align-items-center gap-1">
            <i className="bi bi-check-circle-fill"></i>
            <span>Còn hàng ({product.stockQuantity} máy)</span>
          </div>
        </div>
      </div>

      {/* 3. Bố cục chính 2 cột chuẩn phong cách FPT Shop */}
      <div className="row g-4">
        {/* CỘT TRÁI: Bộ sưu tập ảnh, Cam kết vàng, Bài viết đánh giá chi tiết */}
        <div className="col-12 col-lg-7">
          {/* Khung ảnh chính lớn có nút mũi tên Next/Prev và Tự động chuyển mỗi 10s */}
          <div 
            className="detail-main-image-card mb-3 position-relative"
            onMouseEnter={() => setIsPaused(true)}
            onMouseLeave={() => setIsPaused(false)}
          >
            {displayDiscountPercent > 0 && (
              <span className="badge bg-danger position-absolute top-0 start-0 m-3 px-2 py-1 fs-6 fw-bold z-3">
                -{displayDiscountPercent}%
              </span>
            )}
            <span className="badge bg-dark bg-opacity-75 position-absolute top-0 end-0 m-3 px-2 py-1 z-3" style={{ fontSize: '11px' }}>
              <i className="bi bi-shield-check me-1 text-warning"></i>Chính hãng 100%
            </span>

            {/* Nút mũi tên lùi ảnh (Prev) - Hiển thị khi thư viện của màu có nhiều hơn 1 ảnh */}
            {activeGallery && activeGallery.length > 1 && (
              <button 
                type="button" 
                className="detail-nav-btn prev-btn"
                onClick={handlePrevImage}
                title="Ảnh trước đó"
              >
                <i className="bi bi-chevron-left"></i>
              </button>
            )}

            {/* Ảnh hiển thị chính */}
            <img
              key={currentImg}
              src={currentImg}
              alt={product.name}
              className="detail-main-img"
              onError={(e) => {
                e.target.onerror = null;
                e.target.src = 'https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=600&q=80';
              }}
            />

            {/* Nút mũi tên tiến ảnh (Next) - Hiển thị khi thư viện của màu có nhiều hơn 1 ảnh */}
            {activeGallery && activeGallery.length > 1 && (
              <button 
                type="button" 
                className="detail-nav-btn next-btn"
                onClick={handleNextImage}
                title="Ảnh tiếp theo"
              >
                <i className="bi bi-chevron-right"></i>
              </button>
            )}

            {/* Badge chỉ số thứ tự ảnh */}
            {activeGallery && activeGallery.length > 1 && (
              <span className="detail-image-counter">{safeImageIndex + 1} / {activeGallery.length}</span>
            )}

            {/* Chú thích bộ ảnh sản phẩm */}
            <span className="badge bg-light text-secondary border position-absolute bottom-0 start-0 m-3 px-2 py-1 z-3 shadow-sm" style={{ fontSize: '11px' }}>
              <i className="bi bi-images me-1 text-primary"></i>Bộ ảnh sản phẩm ({activeGallery.length} ảnh)
            </span>
          </div>

          {/* Dải ảnh thu nhỏ của màu đang chọn (1 - 4 ảnh) */}
          {activeGallery && activeGallery.length > 1 && (
            <div className="d-flex gap-2 overflow-auto pb-2 mb-3">
              {activeGallery.map((imgUrl, idx) => (
                <button
                  key={imgUrl + idx}
                  type="button"
                  className={`btn p-1 border rounded ${idx === safeImageIndex ? 'border-danger border-2 shadow-sm' : 'border-light-subtle bg-white'}`}
                  style={{ width: '68px', height: '68px', flexShrink: 0, padding: '2px' }}
                  onClick={() => setSelectedImageIndex(idx)}
                  title={`Xem ảnh ${idx + 1}`}
                >
                  <img src={imgUrl} alt={`Ảnh ${idx + 1}`} style={{ width: '100%', height: '100%', objectFit: 'contain' }} />
                </button>
              ))}
            </div>
          )}

          {/* Cam kết vàng */}
          <div className="card border-0 shadow-sm p-3 mb-4 rounded-3 bg-white">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2 text-danger">
              <i className="bi bi-patch-check-fill"></i>
              Yên tâm mua sắm tại PCStore (Cam kết dịch vụ)
            </h6>
            <div className="row g-3">
              <div className="col-12 col-sm-6">
                <div className="commitment-item">
                  <div className="commitment-icon-wrap">
                    <i className="bi bi-shield-check"></i>
                  </div>
                  <div>
                    <h6 className="commitment-title">Hàng chính hãng 100%</h6>
                    <p className="commitment-desc">Bảo hành 24 tháng toàn diện tại các TTBH hãng trên toàn quốc.</p>
                  </div>
                </div>
              </div>

              <div className="col-12 col-sm-6">
                <div className="commitment-item">
                  <div className="commitment-icon-wrap">
                    <i className="bi bi-arrow-repeat"></i>
                  </div>
                  <div>
                    <h6 className="commitment-title">Đổi trả trong 30 ngày</h6>
                    <p className="commitment-desc">Lỗi 1 đổi 1 ngay lập tức nếu có lỗi phần cứng từ nhà sản xuất.</p>
                  </div>
                </div>
              </div>

              <div className="col-12 col-sm-6">
                <div className="commitment-item">
                  <div className="commitment-icon-wrap">
                    <i className="bi bi-truck"></i>
                  </div>
                  <div>
                    <h6 className="commitment-title">Miễn phí giao toàn quốc</h6>
                    <p className="commitment-desc">Giao hỏa tốc 2 giờ tại nội thành, kiểm tra máy trước khi thanh toán.</p>
                  </div>
                </div>
              </div>

              <div className="col-12 col-sm-6">
                <div className="commitment-item">
                  <div className="commitment-icon-wrap">
                    <i className="bi bi-tools"></i>
                  </div>
                  <div>
                    <h6 className="commitment-title">Hỗ trợ kỹ thuật trọn đời</h6>
                    <p className="commitment-desc">Cài đặt phần mềm, vệ sinh máy miễn phí trong suốt quá trình sử dụng.</p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Bài viết đánh giá chi tiết chuẩn FPT Shop */}
          <div className="fpt-article-card mb-4 position-relative">
            <h5 className="fw-bold text-dark mb-3 pb-2 border-bottom d-flex align-items-center gap-2">
              <i className="bi bi-file-text-fill text-danger"></i>
              <span>Đánh giá chi tiết {product.name}</span>
            </h5>

            <div 
              className="fpt-article-content" 
              style={{
                maxHeight: isArticleExpanded ? 'none' : '480px',
                overflow: 'hidden',
                position: 'relative'
              }}
            >
              <p className="lead fs-6 text-secondary">
                {product.description || (
                  `Laptop ${product.name} là sự lựa chọn lý tưởng cho người dùng cần một cỗ máy mạnh mẽ, thiết kế hiện đại và độ bền vượt trội. Được trang bị vi xử lý ${spec.cpu || 'mới nhất'} cùng RAM ${spec.ramGB || 16}GB và SSD ${spec.storageGB || 512}GB, thiết bị mang lại trải nghiệm đa nhiệm mượt mà, đáp ứng hoàn hảo cho cả công việc, học tập và giải trí cao cấp.`
                )}
              </p>

              <h2>1. Thiết kế tinh tế, linh hoạt và độ hoàn thiện cao cấp</h2>
              <p>
                {product.name} gây ấn tượng ngay từ cái nhìn đầu tiên nhờ ngôn ngữ thiết kế tối giản nhưng không kém phần hiện đại. Vỏ máy được gia công tỉ mỉ mang lại cảm giác cầm nắm chắc chắn, hạn chế bám mồ hôi và dấu vân tay.
              </p>
              <p>
                Với trọng lượng chỉ khoảng <strong>{spec.weightKg ? `${spec.weightKg} kg` : '1.45 kg'}</strong>, máy rất thuận tiện để bạn cho vào balo, túi xách và đồng hành trong mọi chuyến công tác, buổi học hay các buổi làm việc tại quán cà phê.
              </p>

              <h2>2. Sức mạnh bứt phá với CPU {spec.cpu || 'thế hệ mới'}</h2>
              <p>
                Trái tim của cỗ máy là bộ vi xử lý <strong>{spec.cpu || 'Intel Core / AMD'}</strong> tối tân, mang lại khả năng xử lý mượt mà từ các tác vụ văn phòng cơ bản (Word, Excel, PowerPoint) đến những tác vụ nặng đòi hỏi tài nguyên lớn.
              </p>
              <p>
                Kết hợp với <strong>{activeRamOpt.label} RAM</strong> và ổ cứng <strong>{activeStorageOpt.label}</strong>, thiết bị giúp bạn khởi động hệ điều hành và mở các phần mềm chỉ trong vài giây ngắn ngủi, giảm thiểu tối đa thời gian chờ đợi.
              </p>

              <h2>3. Màn hình {spec.screenSizeInch ? `${spec.screenSizeInch} inch` : '15.6 inch'} sắc nét & Trải nghiệm thị giác sống động</h2>
              <p>
                Không gian hiển thị rộng rãi với kích thước <strong>{spec.screenSizeInch || 15.6} inch</strong>, tần số quét <strong>{spec.refreshRateHz || 60}Hz</strong> giúp các chuyển động trên màn hình trở nên cực kỳ mượt mà. Viền màn hình siêu mỏng mang đến trải nghiệm gần như không giới hạn khi xem phim hay làm việc đồ họa.
              </p>

              <h2>4. Pin {spec.batteryWh ? `${spec.batteryWh} Wh` : '54 Wh'} bền bỉ cùng Hệ điều hành {spec.os || 'Windows 11'}</h2>
              <p>
                Viên pin dung lượng lớn cho phép bạn làm việc suốt nhiều giờ liên tục mà không cần lo lắng về việc tìm kiếm ổ cắm sạc. Hệ điều hành <strong>{spec.os || 'Windows 11 Home'}</strong> bản quyền cài sẵn đảm bảo an toàn dữ liệu và liên tục nhận được các bản vá bảo mật mới nhất.
              </p>

              {/* Gradient mờ khi thu gọn */}
              {!isArticleExpanded && (
                <div 
                  style={{
                    position: 'absolute',
                    bottom: 0,
                    left: 0,
                    right: 0,
                    height: '140px',
                    background: 'linear-gradient(180deg, rgba(255,255,255,0) 0%, rgba(255,255,255,0.95) 100%)'
                  }}
                />
              )}
            </div>

            {/* Nút Xem thêm / Thu gọn */}
            <div className="text-center mt-3 pt-2">
              <button 
                onClick={() => setIsArticleExpanded(!isArticleExpanded)} 
                className="article-expand-btn"
              >
                <span>{isArticleExpanded ? 'Thu gọn bài viết' : 'Đọc thêm bài đánh giá'}</span>
                <i className={`bi bi-chevron-${isArticleExpanded ? 'up' : 'down'} ms-2`}></i>
              </button>
            </div>
          </div>

          {/* Đánh giá từ khách hàng */}
          <div className="fpt-reviews-box mb-4">
            <h5 className="fw-bold text-dark mb-4 pb-2 border-bottom d-flex align-items-center gap-2">
              <i className="bi bi-chat-square-quote-fill text-danger"></i>
              <span>Khách hàng nhận xét về sản phẩm</span>
            </h5>

            <div className="row align-items-center mb-4 pb-3 border-bottom">
              <div className="col-auto text-center pe-4 border-end">
                <div className="review-score-big">5.0</div>
                <div className="review-stars-gold mb-1">
                  <i className="bi bi-star-fill"></i>
                  <i className="bi bi-star-fill"></i>
                  <i className="bi bi-star-fill"></i>
                  <i className="bi bi-star-fill"></i>
                  <i className="bi bi-star-fill"></i>
                </div>
                <div className="text-muted small">128 đánh giá</div>
              </div>

              <div className="col">
                <div className="d-flex align-items-center gap-2 mb-1" style={{ fontSize: '12px' }}>
                  <span>5 sao</span>
                  <div className="progress flex-grow-1" style={{ height: '6px' }}>
                    <div className="progress-bar bg-warning" style={{ width: '92%' }}></div>
                  </div>
                  <span className="text-muted">92%</span>
                </div>
                <div className="d-flex align-items-center gap-2 mb-1" style={{ fontSize: '12px' }}>
                  <span>4 sao</span>
                  <div className="progress flex-grow-1" style={{ height: '6px' }}>
                    <div className="progress-bar bg-warning" style={{ width: '8%' }}></div>
                  </div>
                  <span className="text-muted">8%</span>
                </div>
                <div className="d-flex align-items-center gap-2" style={{ fontSize: '12px' }}>
                  <span>1-3 sao</span>
                  <div className="progress flex-grow-1" style={{ height: '6px' }}>
                    <div className="progress-bar bg-warning" style={{ width: '0%' }}></div>
                  </div>
                  <span className="text-muted">0%</span>
                </div>
              </div>
            </div>

            <div className="review-card-item">
              <div className="d-flex align-items-center justify-content-between mb-1">
                <div className="d-flex align-items-center gap-2">
                  <div className="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center fw-bold" style={{ width: '32px', height: '32px', fontSize: '13px' }}>
                    TH
                  </div>
                  <div>
                    <span className="fw-bold text-dark d-block" style={{ fontSize: '13px' }}>Trần Hoàng</span>
                    <span className="badge bg-success-subtle text-success border border-success-subtle" style={{ fontSize: '10px' }}>
                      <i className="bi bi-check-circle me-1"></i>Đã mua tại PCStore
                    </span>
                  </div>
                </div>
                <span className="text-muted small">Hôm qua</span>
              </div>
              <div className="review-stars-gold mb-1" style={{ fontSize: '13px' }}>
                <i className="bi bi-star-fill"></i>
                <i className="bi bi-star-fill"></i>
                <i className="bi bi-star-fill"></i>
                <i className="bi bi-star-fill"></i>
                <i className="bi bi-star-fill"></i>
              </div>
              <p className="mb-0 text-secondary" style={{ fontSize: '13px' }}>
                Máy chạy cực kỳ mượt mà, bật máy chỉ mất 5 giây là vào Win. Màn hình sắc nét, màu sắc đẹp. Đóng gói cẩn thận, nhân viên giao hàng nhiệt tình hỗ trợ kiểm tra máy!
              </p>
            </div>

            <div className="review-card-item">
              <div className="d-flex align-items-center justify-content-between mb-1">
                <div className="d-flex align-items-center gap-2">
                  <div className="bg-danger text-white rounded-circle d-flex align-items-center justify-content-center fw-bold" style={{ width: '32px', height: '32px', fontSize: '13px' }}>
                    MA
                  </div>
                  <div>
                    <span className="fw-bold text-dark d-block" style={{ fontSize: '13px' }}>Minh Anh</span>
                    <span className="badge bg-success-subtle text-success border border-success-subtle" style={{ fontSize: '10px' }}>
                      <i className="bi bi-check-circle me-1"></i>Đã mua tại PCStore
                    </span>
                  </div>
                </div>
                <span className="text-muted small">3 ngày trước</span>
              </div>
              <div className="review-stars-gold mb-1" style={{ fontSize: '13px' }}>
                <i className="bi bi-star-fill"></i>
                <i className="bi bi-star-fill"></i>
                <i className="bi bi-star-fill"></i>
                <i className="bi bi-star-fill"></i>
                <i className="bi bi-star-fill"></i>
              </div>
              <p className="mb-0 text-secondary" style={{ fontSize: '13px' }}>
                Thiết kế gọn nhẹ mang đi làm rất tiện, pin dùng liên tục được tầm 6-7 tiếng văn phòng. Được tặng kèm balo và chuột không dây rất xịn sò.
              </p>
            </div>
          </div>
        </div>

        {/* CỘT PHẢI: Bảng giá FPT, Khuyến mãi, Nút MUA NGAY, Bảng Thông số kỹ thuật */}
        <div className="col-12 col-lg-5">
          <div className="sticky-top" style={{ top: '20px', zIndex: 10 }}>
            {/* 1. Hộp Giá và Tiết kiệm (Động theo cấu hình chọn) */}
            <div className="fpt-price-box mb-3">
              <div className="d-flex align-items-baseline gap-3 mb-1">
                <span className="fpt-current-price">
                  {formatVND(displayPrice)}
                </span>
                {displayOriginalPrice && displayOriginalPrice > displayPrice && (
                  <span className="fpt-original-price">
                    {formatVND(displayOriginalPrice)}
                  </span>
                )}
                {displayDiscountPercent > 0 && (
                  <span className="fpt-discount-badge">
                    -{displayDiscountPercent}%
                  </span>
                )}
              </div>

              {displaySavings > 0 && (
                <div className="fpt-save-amount">
                  <i className="bi bi-tag-fill me-1"></i>
                  Tiết kiệm ngay {formatVND(displaySavings)}
                </div>
              )}
            </div>

            {/* 2. Tùy chọn Màu sắc & Cấu hình (Variants) */}
            <div className="bg-white border rounded-3 p-3 mb-3 shadow-sm">
              {/* Lựa chọn Màu sắc */}
              {hasVariants && availableColors.length > 0 && (
                <div className="mb-3 pb-3 border-bottom">
                  <div className="d-flex justify-content-between align-items-center mb-2">
                    <span className="text-secondary small fw-bold text-uppercase">
                      <i className="bi bi-palette-fill text-danger me-1"></i> 1. Chọn màu sắc máy:
                    </span>
                    <span className="badge bg-danger-subtle text-danger border border-danger-subtle fw-semibold">
                      {activeVariant?.colorName || 'Mặc định'}
                    </span>
                  </div>
                  <div className="d-flex flex-wrap gap-2">
                    {availableColors.map((c) => {
                      const isSelected = activeColorId === c.colorId;
                      return (
                        <button
                          key={c.colorId}
                          type="button"
                          className={`btn btn-sm d-flex align-items-center gap-2 py-2 px-3 rounded-3 border ${
                            isSelected
                              ? 'border-danger bg-danger-subtle text-danger fw-bold shadow-sm'
                              : 'border-secondary-subtle bg-white text-dark'
                          }`}
                          onClick={() => handleSelectColor(c.colorId)}
                        >
                          <span
                            className="rounded-circle border"
                            style={{
                              width: '14px',
                              height: '14px',
                              backgroundColor: c.hexCode,
                              display: 'inline-block'
                            }}
                          ></span>
                          <span>{c.colorName}</span>
                          {isSelected && <i className="bi bi-check2 text-danger ms-1"></i>}
                        </button>
                      );
                    })}
                  </div>
                </div>
              )}

              {/* Lựa chọn Cấu hình */}
              {hasVariants ? (
                <div>
                  <div className="d-flex justify-content-between align-items-center mb-2">
                    <span className="text-secondary small fw-bold text-uppercase">
                      <i className="bi bi-cpu-fill text-danger me-1"></i> 2. Chọn cấu hình có sẵn ({activeVariant?.colorName}):
                    </span>
                  </div>
                  <div className="d-flex flex-wrap gap-2">
                    {variantsForCurrentColor.map((v) => {
                      const isSelected = activeVariant?.variantId === v.variantId;
                      const isVariantOos = v.stockQuantity <= 0;
                      return (
                        <button
                          key={v.variantId}
                          type="button"
                          className={`btn btn-sm text-start p-2 rounded-3 border ${
                            isSelected
                              ? 'border-danger bg-danger-subtle text-danger fw-bold shadow-sm'
                              : 'border-secondary-subtle bg-white text-dark'
                          } ${isVariantOos ? 'opacity-75' : ''}`}
                          onClick={() => setSelectedVariantId(v.variantId)}
                        >
                          <div className="fw-semibold small">RAM {v.ramGB}GB • SSD {v.storageGB}GB</div>
                          <div className="d-flex align-items-center gap-2 mt-1" style={{ fontSize: '11px' }}>
                            <span className="text-muted">SKU: <code>{v.sku}</code></span>
                            {isVariantOos ? (
                              <span className="badge bg-danger text-white">Hết hàng</span>
                            ) : (
                              <span className="badge bg-success-subtle text-success">Còn {v.stockQuantity}</span>
                            )}
                          </div>
                        </button>
                      );
                    })}
                  </div>
                </div>
              ) : (
                /* Fallback cho sản phẩm cũ không quản lý theo biến thể */
                <div>
                  <div className="mb-3">
                    <div className="d-flex justify-content-between align-items-center mb-1">
                      <span className="text-secondary small fw-semibold">Chọn dung lượng RAM:</span>
                      <span className="text-danger fw-bold small">
                        {activeRamOpt.extra > 0 ? `+${formatVND(activeRamOpt.extra)}` : 'Tiêu chuẩn'}
                      </span>
                    </div>
                    <div className="d-flex gap-2">
                      {ramOptions.map((opt, idx) => (
                        <button 
                          key={idx}
                          type="button" 
                          className={`config-pill-btn ${selectedRamIndex === idx ? 'active' : ''}`}
                          onClick={() => setSelectedRamIndex(idx)}
                        >
                          {opt.label} {opt.extra > 0 ? `(+${formatVND(opt.extra)})` : ''}
                        </button>
                      ))}
                    </div>
                  </div>

                  <div>
                    <div className="d-flex justify-content-between align-items-center mb-1">
                      <span className="text-secondary small fw-semibold">Ổ cứng lưu trữ SSD:</span>
                      <span className="text-danger fw-bold small">
                        {activeStorageOpt.extra > 0 ? `+${formatVND(activeStorageOpt.extra)}` : 'Tiêu chuẩn'}
                      </span>
                    </div>
                    <div className="d-flex gap-2">
                      {storageOptions.map((opt, idx) => (
                        <button 
                          key={idx}
                          type="button" 
                          className={`config-pill-btn ${selectedStorageIndex === idx ? 'active' : ''}`}
                          onClick={() => setSelectedStorageIndex(idx)}
                        >
                          {opt.label} {opt.extra > 0 ? `(+${formatVND(opt.extra)})` : ''}
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              )}

              {/* Thông tin tình trạng tồn kho của phiên bản đang chọn */}
              <div className="mt-3 pt-2 border-top small d-flex align-items-center justify-content-between">
                <span>
                  {isOutOfStock ? (
                    <span className="text-danger fw-bold">
                      <i className="bi bi-x-circle-fill me-1"></i> Phiên bản này tạm hết hàng!
                    </span>
                  ) : (
                    <span className="text-success fw-bold">
                      <i className="bi bi-check-circle-fill me-1"></i> Có sẵn {currentStock} máy tại cửa hàng
                    </span>
                  )}
                </span>
                {activeVariant?.sku && (
                  <span className="text-muted" style={{ fontSize: '11px' }}>
                    SKU: <code>{activeVariant.sku}</code>
                  </span>
                )}
              </div>
            </div>

            {/* 3. Khung Khuyến mãi đặc trưng FPT Shop viền đỏ */}
            <div className="fpt-promo-card mb-3">
              <div className="fpt-promo-header">
                <i className="bi bi-gift-fill fs-5"></i>
                <span>KHUYẾN MÃI ĐẶC BIỆT</span>
              </div>
              <div className="fpt-promo-body">
                <div className="promo-list-item">
                  <i className="bi bi-check-circle-fill"></i>
                  <span><strong>Giảm ngay 4.000.000₫</strong> trực tiếp vào giá niêm yết áp dụng trong tuần này.</span>
                </div>
                <div className="promo-list-item">
                  <i className="bi bi-check-circle-fill"></i>
                  <span><strong>Tặng Balo laptop cao cấp</strong> chống sốc chính hãng trị giá 490.000₫.</span>
                </div>
                <div className="promo-list-item">
                  <i className="bi bi-check-circle-fill"></i>
                  <span><strong>Tặng Chuột không dây Silent</strong> + Bàn di chuột gaming chống trượt.</span>
                </div>
                <div className="promo-list-item">
                  <i className="bi bi-check-circle-fill"></i>
                  <span><strong>Giảm thêm 500.000₫</strong> khi thanh toán qua chuyển khoản QR / thẻ ngân hàng NCB.</span>
                </div>
                <div className="promo-list-item">
                  <i className="bi bi-check-circle-fill"></i>
                  <span><strong>Ưa đãi Học sinh - Sinh viên:</strong> Giảm thêm 300.000₫ khi xuất trình thẻ HSSV.</span>
                </div>
                <div className="promo-list-item">
                  <i className="bi bi-check-circle-fill"></i>
                  <span>Tặng phiếu mua hàng 200.000₫ áp dụng mua Microsoft 365 bản quyền.</span>
                </div>
              </div>
            </div>

            {/* 4. Cụm nút hành động Mua Hàng */}
            <div className="mb-3">
              <button 
                type="button" 
                disabled={isOutOfStock}
                className={`btn w-100 py-3 fw-bold fs-6 mb-2 rounded-3 shadow-sm d-flex align-items-center justify-content-center ${
                  isOutOfStock ? 'btn-secondary text-white' : 'btn-danger'
                }`}
                style={!isOutOfStock ? { backgroundColor: '#e11b1e', borderColor: '#e11b1e' } : {}}
                onClick={() => {
                  if (isOutOfStock) return;
                  const cartItem = {
                    id: product.id,
                    productId: product.id,
                    variantId: activeVariant?.variantId || null,
                    colorName: activeVariant?.colorName || null,
                    colorHex: activeVariant?.hexCode || null,
                    sku: activeVariant?.sku || null,
                    name: product.name,
                    image: currentImg || activeGallery[0] || genericImg,
                    price: displayPrice,
                    originalPrice: displayOriginalPrice,
                    selectedRam: activeVariant ? `${activeVariant.ramGB} GB` : activeRamOpt.label,
                    selectedStorage: activeVariant ? `${activeVariant.storageGB} GB SSD` : activeStorageOpt.label,
                    specs: activeVariant
                      ? `Màu: ${activeVariant.colorName} • RAM: ${activeVariant.ramGB}GB • SSD: ${activeVariant.storageGB}GB`
                      : `RAM: ${activeRamOpt.label} • ${activeStorageOpt.label}`,
                  };
                  if (onBuyNow) {
                    onBuyNow(cartItem);
                  } else if (onAddToCart) {
                    onAddToCart(cartItem);
                  }
                }}
              >
                {isOutOfStock ? 'TẠM HẾT HÀNG' : `MUA NGAY (${formatVND(displayPrice)})`}
              </button>

              <button 
                type="button" 
                disabled={isOutOfStock}
                className={`btn w-100 py-2 fw-semibold d-flex align-items-center justify-content-center gap-2 rounded-3 ${
                  isOutOfStock ? 'btn-outline-secondary' : 'btn-outline-danger'
                }`}
                style={!isOutOfStock ? { borderColor: '#e11b1e', color: '#e11b1e' } : {}}
                onClick={() => {
                  if (isOutOfStock) return;
                  const cartItem = {
                    id: product.id,
                    productId: product.id,
                    variantId: activeVariant?.variantId || null,
                    colorName: activeVariant?.colorName || null,
                    colorHex: activeVariant?.hexCode || null,
                    sku: activeVariant?.sku || null,
                    name: product.name,
                    image: currentImg || activeGallery[0] || genericImg,
                    price: displayPrice,
                    originalPrice: displayOriginalPrice,
                    selectedRam: activeVariant ? `${activeVariant.ramGB} GB` : activeRamOpt.label,
                    selectedStorage: activeVariant ? `${activeVariant.storageGB} GB SSD` : activeStorageOpt.label,
                    specs: activeVariant
                      ? `Màu: ${activeVariant.colorName} • RAM: ${activeVariant.ramGB}GB • SSD: ${activeVariant.storageGB}GB`
                      : `RAM: ${activeRamOpt.label} • ${activeStorageOpt.label}`,
                  };
                  if (onAddToCart) {
                    onAddToCart(cartItem);
                  }
                  triggerToast(`Đã thêm ${product.name} [Màu: ${activeVariant?.colorName || 'Mặc định'}] vào giỏ hàng!`);
                }}
              >
                <i className="bi bi-cart-plus fs-5"></i>
                <span>Thêm vào giỏ hàng</span>
              </button>
            </div>

            {/* 5. Bảng Thông số kỹ thuật chi tiết (Cập nhật theo cấu hình chọn) */}
            <div className="specs-table-card mb-3">
              <div className="specs-table-header">
                <span className="d-flex align-items-center gap-2">
                  <i className="bi bi-cpu text-danger"></i>
                  <span>Thông số kỹ thuật</span>
                </span>
                <span className="badge bg-secondary" style={{ fontSize: '11px' }}>Chi tiết</span>
              </div>

              <table className="specs-table">
                <tbody>
                  <tr>
                    <td className="spec-label">Vi xử lý (CPU)</td>
                    <td className="spec-value">{spec.cpu || 'Intel Core / AMD Ryzen'}</td>
                  </tr>
                  <tr>
                    <td className="spec-label">Bộ nhớ RAM</td>
                    <td className="spec-value">
                      <span className="fw-bold">{activeRamOpt.label}</span>
                      {activeRamOpt.extra > 0 && (
                        <span className="badge bg-danger-subtle text-danger border border-danger-subtle ms-2" style={{ fontSize: '10px' }}>
                          Nâng cấp (+{formatVND(activeRamOpt.extra)})
                        </span>
                      )}
                    </td>
                  </tr>
                  <tr>
                    <td className="spec-label">Ổ cứng lưu trữ</td>
                    <td className="spec-value">
                      <span className="fw-bold">{activeStorageOpt.label}</span>
                      {activeStorageOpt.extra > 0 && (
                        <span className="badge bg-danger-subtle text-danger border border-danger-subtle ms-2" style={{ fontSize: '10px' }}>
                          Nâng cấp (+{formatVND(activeStorageOpt.extra)})
                        </span>
                      )}
                    </td>
                  </tr>
                  <tr>
                    <td className="spec-label">Card đồ họa (GPU)</td>
                    <td className="spec-value">{spec.gpu || 'Đồ họa tích hợp'}</td>
                  </tr>
                  <tr>
                    <td className="spec-label">Màn hình</td>
                    <td className="spec-value">{spec.screenSizeInch ? `${spec.screenSizeInch} inch` : '15.6 inch'} {spec.refreshRateHz ? `(${spec.refreshRateHz} Hz)` : ''}</td>
                  </tr>
                  <tr>
                    <td className="spec-label">Dung lượng Pin</td>
                    <td className="spec-value">{spec.batteryWh ? `${spec.batteryWh} Wh` : '54 Wh'}</td>
                  </tr>
                  <tr>
                    <td className="spec-label">Trọng lượng</td>
                    <td className="spec-value">{spec.weightKg ? `${spec.weightKg} kg` : '1.45 kg'}</td>
                  </tr>
                  <tr>
                    <td className="spec-label">Hệ điều hành</td>
                    <td className="spec-value">{spec.os || 'Windows 11 Home'}</td>
                  </tr>
                  <tr>
                    <td className="spec-label">Thương hiệu</td>
                    <td className="spec-value">{product.brand}</td>
                  </tr>
                  <tr>
                    <td className="spec-label">Dòng sản phẩm</td>
                    <td className="spec-value">{product.category}</td>
                  </tr>
                </tbody>
              </table>
            </div>

            {/* 6. Hộp Tư vấn miễn phí */}
            <div className="bg-light border rounded-3 p-3 text-center">
              <div className="fw-semibold text-dark small mb-1">
                <i className="bi bi-telephone-fill text-danger me-1"></i>
                Tư vấn mua hàng miễn phí cước gọi:
              </div>
              <a href="tel:18001234" className="fw-bold text-pc-red fs-5 text-decoration-none">
                1800 1234
              </a>
              <span className="text-muted d-block small mt-1">(7:30 - 22:00 cả Thứ 7, CN)</span>
            </div>
          </div>
        </div>
      </div>

      {/* 4. Sản phẩm tương tự / Cùng phân khúc */}
      {relatedList.length > 0 && (
        <div className="mt-5 pt-3 border-top">
          <div className="d-flex align-items-center justify-content-between mb-3">
            <h5 className="fw-bold text-dark mb-0 d-flex align-items-center gap-2">
              <i className="bi bi-laptop-fill text-danger"></i>
              <span>Sản phẩm cùng phân khúc được quan tâm</span>
            </h5>
            <button 
              onClick={onBack} 
              className="btn btn-sm btn-outline-danger"
            >
              Xem tất cả
            </button>
          </div>

          <div className="row row-cols-2 row-cols-sm-2 row-cols-md-4 g-3">
            {relatedList.map((rel) => (
              <div className="col" key={rel.id}>
                <div 
                  className="card h-100 product-card p-2 position-relative cursor-pointer"
                  onClick={() => onSelectProduct && onSelectProduct(rel.id)}
                  style={{ cursor: 'pointer' }}
                >
                  {rel.badge && (
                    <span className={`badge ${rel.badgeColor || 'bg-danger'} badge-custom position-absolute top-0 start-0 m-2`}>
                      {rel.badge}
                    </span>
                  )}
                  <div className="d-flex justify-content-center align-items-center py-2">
                    <img
                      src={rel.image}
                      alt={rel.name}
                      className="img-fluid rounded"
                      style={{ height: '110px', width: '100%', objectFit: 'contain' }}
                    />
                  </div>
                  <div className="p-1">
                    <h6 className="product-title mb-1" title={rel.name}>
                      {rel.name}
                    </h6>
                    <p className="product-specs">{rel.specs}</p>
                    <div className="text-pc-red fw-bold small">
                      {formatVND(rel.price)}
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

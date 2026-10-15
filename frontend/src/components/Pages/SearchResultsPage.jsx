import React, { useState, useEffect } from 'react';
import ProductCard from '../Product/ProductCard';
import { PRODUCT_DATA } from '../../data/mockData';

export default function SearchResultsPage({
  keyword = '',
  onSelectProduct,
  onAddToCart,
  onGoHome,
}) {
  const [searchResults, setSearchResults] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [sortBy, setSortBy] = useState('relevance');

  // Lấy dữ liệu sản phẩm theo từ khóa tìm kiếm (từ API hoặc mock)
  useEffect(() => {
    let isMounted = true;
    const fetchSearchData = async () => {
      setIsLoading(true);
      const cleanKw = (keyword || '').trim();

      try {
        const res = await fetch(`/api/products?q=${encodeURIComponent(cleanKw)}&pageSize=60`);
        if (!res.ok) throw new Error('Lỗi tải API');
        const data = await res.json();
        if (isMounted && data && Array.isArray(data.items)) {
          setSearchResults(data.items);
        }
      } catch (err) {
        if (isMounted) {
          const kwLower = cleanKw.toLowerCase();
          const filtered = PRODUCT_DATA.filter((p) => {
            if (!kwLower) return true;
            return (
              p.name.toLowerCase().includes(kwLower) ||
              p.brand.toLowerCase().includes(kwLower) ||
              (p.specs || '').toLowerCase().includes(kwLower) ||
              (p.category || '').toLowerCase().includes(kwLower)
            );
          });
          setSearchResults(filtered);
        }
      } finally {
        if (isMounted) setIsLoading(false);
      }
    };

    fetchSearchData();
    return () => {
      isMounted = false;
    };
  }, [keyword]);

  // Sắp xếp danh sách sản phẩm theo lựa chọn của người dùng
  let displayedProducts = [...searchResults];
  if (sortBy === 'price-asc') {
    displayedProducts.sort((a, b) => a.price - b.price);
  } else if (sortBy === 'price-desc') {
    displayedProducts.sort((a, b) => b.price - a.price);
  } else if (sortBy === 'name-asc') {
    displayedProducts.sort((a, b) => a.name.localeCompare(b.name));
  }

  return (
    <div className="site-container py-3">
      {/* 1. Đường dẫn Breadcrumb */}
      <nav aria-label="breadcrumb" className="mb-3">
        <ol className="breadcrumb small mb-0">
          <li className="breadcrumb-item">
            <a
              href="/"
              onClick={(e) => {
                e.preventDefault();
                if (onGoHome) onGoHome();
              }}
              className="text-decoration-none text-muted"
            >
              <i className="bi bi-house-door me-1"></i>Trang chủ
            </a>
          </li>
          <li className="breadcrumb-item text-muted">Tìm kiếm</li>
          <li className="breadcrumb-item active text-danger fw-semibold" aria-current="page">
            {keyword ? `"${keyword}"` : 'Tất cả sản phẩm'}
          </li>
        </ol>
      </nav>

      {/* 2. Tiêu đề kết quả tìm kiếm - Đặt cùng nền trang, không có khung bao ngoài */}
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
        <div>
          <h2 className="fw-bold fs-5 text-dark m-0 d-flex align-items-center gap-2">
            <span>Kết quả tìm kiếm cho:</span>
            <span className="text-danger">"{keyword}"</span>
          </h2>
          <div className="text-muted small mt-1">
            Tìm thấy <strong className="text-danger">{searchResults.length}</strong> sản phẩm phù hợp
          </div>
        </div>

        {/* Bộ chọn sắp xếp */}
        <div className="d-flex align-items-center gap-2 mt-2 mt-sm-0">
          <span className="text-muted small">Sắp xếp:</span>
          <select
            className="form-select form-select-sm border bg-white text-dark shadow-sm"
            style={{ width: '160px', fontSize: '13px' }}
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
          >
            <option value="relevance">Phù hợp nhất</option>
            <option value="price-asc">Giá: Thấp đến Cao</option>
            <option value="price-desc">Giá: Cao đến Thấp</option>
            <option value="name-asc">Tên: A - Z</option>
          </select>
        </div>
      </div>

      {/* 3. Lưới sản phẩm - Nằm trực tiếp trên nền chung của trang */}
      {isLoading ? (
        <div className="text-center py-5">
          <div className="spinner-border text-danger" role="status">
            <span className="visually-hidden">Đang tìm kiếm...</span>
          </div>
          <p className="text-muted small mt-2 mb-0">Đang tìm kiếm sản phẩm...</p>
        </div>
      ) : displayedProducts.length > 0 ? (
        <div className="row row-cols-2 row-cols-md-3 row-cols-lg-4 g-3 mb-4">
          {displayedProducts.map((p) => (
            <div className="col" key={p.id}>
              <ProductCard
                item={p}
                onSelectProduct={onSelectProduct}
                onAddToCart={onAddToCart}
              />
            </div>
          ))}
        </div>
      ) : (
        /* Trạng thái không có sản phẩm khớp - Tự nhiên trên nền trang */
        <div className="text-center py-5 my-4">
          <i className="bi bi-search text-muted fs-1 mb-2 d-block"></i>
          <h5 className="fw-bold text-dark">
            Không tìm thấy sản phẩm nào khớp với từ khóa "{keyword}"
          </h5>
          <p className="text-muted small mb-3">
            Vui lòng thử lại với từ khóa khác (ví dụ: Asus, Gaming, Dell, i5...)
          </p>
          <button
            type="button"
            className="btn btn-sm btn-danger px-3 shadow-sm"
            onClick={() => {
              if (onGoHome) onGoHome();
            }}
          >
            Về trang chủ
          </button>
        </div>
      )}
    </div>
  );
}

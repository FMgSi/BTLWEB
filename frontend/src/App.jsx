import React, { useState, useEffect } from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap-icons/font/bootstrap-icons.css';
import './App.css';

import TopHeader from './components/Header/TopHeader';
import Navbar from './components/Header/Navbar';
import CategorySidebar from './components/Sidebar/CategorySidebar';
import FilterSection from './components/Sidebar/FilterSection';
import ProductToolbar from './components/Product/ProductToolbar';
import ProductList from './components/Product/ProductList';
import Pagination from './components/Common/Pagination';

import { PRODUCT_DATA } from './data/mockData';

export default function App() {
  // States cho các bộ lọc
  const [activeTag, setActiveTag] = useState("Tất cả sản phẩm");
  const [sortBy, setSortBy] = useState("newest");
  const [selectedBrands, setSelectedBrands] = useState([]);
  const [selectedSizes, setSelectedSizes] = useState([]);
  const [selectedCpus, setSelectedCpus] = useState([]);
  const [selectedPriceRange, setSelectedPriceRange] = useState("all");
  const [customMin, setCustomMin] = useState("");
  const [customMax, setCustomMax] = useState("");
  const [appliedCustomPrice, setAppliedCustomPrice] = useState(null);

  // States phân trang & dữ liệu Backend API
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 12;

  const [products, setProducts] = useState(PRODUCT_DATA.slice(0, pageSize));
  const [isLoading, setIsLoading] = useState(false);
  const [isBackendConnected, setIsBackendConnected] = useState(false);
  const [totalCount, setTotalCount] = useState(PRODUCT_DATA.length);

  // Toggle hãng
  const toggleBrand = (brand) => {
    setSelectedBrands((prev) =>
      prev.includes(brand) ? prev.filter((b) => b !== brand) : [...prev, brand]
    );
    setCurrentPage(1);
  };

  // Toggle kích thước màn hình
  const toggleSize = (size) => {
    setSelectedSizes((prev) =>
      prev.includes(size) ? prev.filter((s) => s !== size) : [...prev, size]
    );
    setCurrentPage(1);
  };

  // Toggle vi xử lý CPU
  const toggleCpu = (cpu) => {
    setSelectedCpus((prev) =>
      prev.includes(cpu) ? prev.filter((c) => c !== cpu) : [...prev, cpu]
    );
    setCurrentPage(1);
  };

  // Áp dụng nhập giá tuỳ biến
  const handleApplyCustomPrice = () => {
    if (customMin || customMax) {
      setAppliedCustomPrice({
        min: customMin ? parseFloat(customMin) : 0,
        max: customMax ? parseFloat(customMax) : Infinity,
      });
      setSelectedPriceRange("custom");
      setCurrentPage(1);
    }
  };

  // Xóa toàn bộ bộ lọc
  const handleResetFilters = () => {
    setActiveTag("Tất cả sản phẩm");
    setSelectedBrands([]);
    setSelectedSizes([]);
    setSelectedCpus([]);
    setSelectedPriceRange("all");
    setCustomMin("");
    setCustomMax("");
    setAppliedCustomPrice(null);
    setSortBy("newest");
    setCurrentPage(1);
  };

  // 3. Gọi API từ ASP.NET Core Backend (GET: /api/products)
  // useEffect sẽ tự động kích hoạt mỗi khi người dùng thay đổi bộ lọc hoặc đổi trang
  useEffect(() => {
    let isMounted = true;

    const fetchProducts = async () => {
      setIsLoading(true);
      try {
        // Gom các tham số lọc và phân trang thành Query String
        const params = new URLSearchParams();
        if (activeTag && activeTag !== "Tất cả sản phẩm") params.append("tag", activeTag);
        if (selectedBrands.length > 0) params.append("brands", selectedBrands.join(","));
        if (selectedSizes.length > 0) params.append("sizes", selectedSizes.join(","));
        if (selectedCpus.length > 0) params.append("cpus", selectedCpus.join(","));
        if (selectedPriceRange && selectedPriceRange !== "all") params.append("priceRange", selectedPriceRange);
        if (selectedPriceRange === "custom" && appliedCustomPrice) {
          if (appliedCustomPrice.min) params.append("minPrice", appliedCustomPrice.min);
          if (appliedCustomPrice.max && appliedCustomPrice.max !== Infinity) params.append("maxPrice", appliedCustomPrice.max);
        }
        if (sortBy) params.append("sortBy", sortBy);
        params.append("page", currentPage.toString());
        params.append("pageSize", pageSize.toString());

        // Gửi HTTP GET request sang Backend API
        const res = await fetch(`/api/products?${params.toString()}`);
        if (!res.ok) throw new Error(`Lỗi kết nối API: ${res.status}`);
        
        const data = await res.json();

        if (isMounted && data && Array.isArray(data.items)) {
          setProducts(data.items);
          setTotalCount(data.totalItems || 0);
          setIsBackendConnected(true);
        }
      } catch (err) {
        if (isMounted) {
          console.warn("Chưa kết nối được API Backend, sử dụng dữ liệu mẫu:", err);
          setIsBackendConnected(false);
          // Nếu chưa bật backend thì hiển thị dữ liệu tĩnh mẫu
          setProducts(PRODUCT_DATA.slice(0, pageSize));
          setTotalCount(PRODUCT_DATA.length);
        }
      } finally {
        if (isMounted) setIsLoading(false);
      }
    };

    fetchProducts();

    return () => {
      isMounted = false;
    };
  }, [activeTag, selectedBrands, selectedSizes, selectedCpus, selectedPriceRange, appliedCustomPrice, sortBy, currentPage]);

  return (
    <div className="min-vh-100 pb-5">
      <TopHeader />
      <Navbar />

      <div className="site-container py-3">

        <div className="d-flex gap-3 align-items-start">
          {/* Sidebar */}
          <aside className="sidebar-fixed d-none d-md-block">
            <CategorySidebar />
            <FilterSection
              selectedPriceRange={selectedPriceRange}
              setSelectedPriceRange={(val) => { setSelectedPriceRange(val); setCurrentPage(1); }}
              customMin={customMin}
              setCustomMin={setCustomMin}
              customMax={customMax}
              setCustomMax={setCustomMax}
              onApplyCustomPrice={handleApplyCustomPrice}
              selectedBrands={selectedBrands}
              toggleBrand={toggleBrand}
              selectedSizes={selectedSizes}
              toggleSize={toggleSize}
              selectedCpus={selectedCpus}
              toggleCpu={toggleCpu}
              onResetFilters={handleResetFilters}
            />
          </aside>

          {/* Vùng sản phẩm */}
          <main className="content-fluid-area">
            <ProductToolbar
              activeTag={activeTag}
              setActiveTag={(tag) => { setActiveTag(tag); setCurrentPage(1); }}
              sortBy={sortBy}
              setSortBy={(sort) => { setSortBy(sort); setCurrentPage(1); }}
            />

            {isLoading ? (
              <div className="text-center py-5 bg-white rounded border shadow-sm my-3">
                <div className="spinner-border text-danger" role="status">
                  <span className="visually-hidden">Đang tải...</span>
                </div>
                <p className="text-muted small mt-2 mb-0">Đang đồng bộ dữ liệu sản phẩm...</p>
              </div>
            ) : products.length > 0 ? (
              <ProductList products={products} />
            ) : (
              <div className="text-center py-5 bg-white rounded border shadow-sm my-3">
                <i className="bi bi-search text-muted fs-1 mb-2 d-block"></i>
                <h6 className="fw-bold">Không tìm thấy sản phẩm phù hợp</h6>
                <p className="text-muted small mb-3">Vui lòng thử điều chỉnh hoặc xóa bớt các tiêu chí lọc.</p>
                <button onClick={handleResetFilters} className="btn btn-sm btn-pc-red px-3">
                  Xóa bộ lọc
                </button>
              </div>
            )}

            <Pagination
              currentPage={currentPage}
              totalItems={totalCount}
              pageSize={pageSize}
              onPageChange={(page) => {
                setCurrentPage(page);
                window.scrollTo({ top: 0, behavior: 'smooth' });
              }}
            />
          </main>
        </div>
      </div>
    </div>
  );
}
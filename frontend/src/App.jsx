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

  // Gọi API từ ASP.NET Core Backend (có fallback về mockData nếu API chưa bật)
  useEffect(() => {
    let isMounted = true;

    const fetchProducts = async () => {
      setIsLoading(true);
      try {
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

        const res = await fetch(`/api/products?${params.toString()}`);
        if (!res.ok) throw new Error(`HTTP error! Status: ${res.status}`);
        const data = await res.json();

        if (isMounted && data && Array.isArray(data.items)) {
          setProducts(data.items);
          setTotalCount(data.totalItems || data.items.length);
          setIsBackendConnected(true);
        }
      } catch (err) {
        if (isMounted) {
          console.warn("Chưa kết nối được API Backend C#, đang hiển thị dữ liệu mẫu dự phòng:", err);
          setIsBackendConnected(false);

          // Fallback lọc dữ liệu mẫu tại Client
          let result = [...PRODUCT_DATA];
          if (activeTag !== "Tất cả sản phẩm") {
            result = result.filter((item) => item.category === activeTag);
          }
          if (selectedBrands.length > 0) {
            result = result.filter((item) => selectedBrands.includes(item.brand));
          }
          if (selectedSizes.length > 0) {
            const has13 = selectedSizes.includes("13 inch");
            const has14 = selectedSizes.includes("14 inch");
            const has15 = selectedSizes.includes("15.6 inch");
            const has16 = selectedSizes.includes("16 inch");
            const has17 = selectedSizes.includes("17 inch");
            result = result.filter((item) => {
              const sz = parseFloat((item.screenSize || '').replace('inch', '').trim()) || 0;
              return (
                (has13 && sz >= 13 && sz < 14) ||
                (has14 && sz >= 14 && sz < 15) ||
                (has15 && sz >= 15 && sz < 16) ||
                (has16 && sz >= 16 && sz < 17) ||
                (has17 && sz >= 17)
              );
            });
          }
          if (selectedCpus.length > 0) {
            result = result.filter((item) => {
              const specText = (item.specs || '').toLowerCase();
              return selectedCpus.some(c => {
                const cl = c.toLowerCase();
                if (cl.includes("i5")) return specText.includes("i5") || specText.includes("core 5");
                if (cl.includes("i7")) return specText.includes("i7");
                if (cl.includes("i9")) return specText.includes("i9");
                if (cl.includes("ultra")) return specText.includes("ultra");
                if (cl.includes("ryzen 5")) return specText.includes("ryzen 5");
                if (cl.includes("ryzen 7")) return specText.includes("ryzen 7");
                if (cl.includes("apple")) return specText.includes("m1") || specText.includes("m2") || specText.includes("m3") || specText.includes("apple");
                return specText.includes(cl);
              });
            });
          }
          if (selectedPriceRange === 'under10') {
            result = result.filter((item) => item.price < 10000000);
          } else if (selectedPriceRange === '10to20') {
            result = result.filter((item) => item.price >= 10000000 && item.price <= 20000000);
          } else if (selectedPriceRange === '20to30') {
            result = result.filter((item) => item.price >= 20000000 && item.price <= 30000000);
          } else if (selectedPriceRange === 'above30') {
            result = result.filter((item) => item.price > 30000000);
          } else if (selectedPriceRange === 'custom' && appliedCustomPrice) {
            result = result.filter(
              (item) => item.price >= appliedCustomPrice.min && item.price <= appliedCustomPrice.max
            );
          }
          if (sortBy === 'price-asc') {
            result.sort((a, b) => a.price - b.price);
          } else if (sortBy === 'price-desc') {
            result.sort((a, b) => b.price - a.price);
          } else {
            result.sort((a, b) => b.id - a.id);
          }

          setTotalCount(result.length);
          const startIndex = (currentPage - 1) * pageSize;
          setProducts(result.slice(startIndex, startIndex + pageSize));
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
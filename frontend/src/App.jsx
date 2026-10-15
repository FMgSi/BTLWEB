import React, { useState, useEffect } from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap-icons/font/bootstrap-icons.css';
import './App.css';

import TopHeader from './components/Header/TopHeader';
import Navbar from './components/Header/Navbar';
import FilterSection from './components/Sidebar/FilterSection';
import ProductToolbar from './components/Product/ProductToolbar';
import ProductList from './components/Product/ProductList';
import ProductDetail from './components/Product/ProductDetail';
import AuthPage from './components/Auth/AuthPage';
import CartPage from './components/Cart/CartPage';
import HomePage from './components/Pages/HomePage';
import ContactPage from './components/Pages/ContactPage';
import GuidesPage from './components/Pages/GuidesPage';
import PromosPage from './components/Pages/PromosPage';
import SearchResultsPage from './components/Pages/SearchResultsPage';
import Pagination from './components/Common/Pagination';

import { PRODUCT_DATA } from './data/mockData';

export default function App() {
  // State điều hướng trang chi tiết sản phẩm (null = trang danh sách, number = id sản phẩm)
  const [selectedProductId, setSelectedProductId] = useState(null);

  // State tìm kiếm sản phẩm ({ keyword: '', category: 'Tất cả danh mục' })
  const [searchQuery, setSearchQuery] = useState({ keyword: '', category: 'Tất cả danh mục' });

  // State điều hướng tab trang chính ('home', 'products', 'promos', 'news', 'guides', 'contact', 'search')
  const [activeNav, setActiveNav] = useState('home');

  // State điều hướng trang Giỏ hàng
  const [isCartView, setIsCartView] = useState(false);

  // State giỏ hàng lưu trữ sản phẩm (tự động đồng bộ với localStorage)
  const [cartItems, setCartItems] = useState(() => {
    try {
      const saved = localStorage.getItem('pcstore_cart');
      return saved ? JSON.parse(saved) : [];
    } catch (e) {
      return [];
    }
  });

  // Thông báo toast
  const [toastMessage, setToastMessage] = useState('');

  // State điều hướng trang Đăng nhập / Đăng ký (null, 'login', 'register')
  const [authView, setAuthView] = useState(null);

  // State thông tin người dùng đang đăng nhập (lưu trong localStorage để giữ phiên)
  const [currentUser, setCurrentUser] = useState(() => {
    try {
      const saved = localStorage.getItem('pcstore_user');
      return saved ? JSON.parse(saved) : null;
    } catch (e) {
      return null;
    }
  });

  // Tự động lưu giỏ hàng vào localStorage khi có thay đổi
  useEffect(() => {
    try {
      localStorage.setItem('pcstore_cart', JSON.stringify(cartItems));
    } catch (e) {}
  }, [cartItems]);

  // States cho các bộ lọc
  const [activeTag, setActiveTag] = useState("Tất cả sản phẩm");
  const [sortBy, setSortBy] = useState("newest");
  const [selectedBrands, setSelectedBrands] = useState([]);
  const [selectedSizes, setSelectedSizes] = useState([]);
  const [selectedRams, setSelectedRams] = useState([]);
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

  // 1. Tự động đọc URL khi load trang hoặc khi người dùng bấm Back/Forward trình duyệt
  useEffect(() => {
    const parseUrl = () => {
      const path = window.location.pathname.toLowerCase();
      if (path.includes('/search')) {
        const params = new URLSearchParams(window.location.search);
        const q = params.get('q') || '';
        const cat = params.get('cat') || 'Tất cả danh mục';
        setSearchQuery({ keyword: q, category: cat });
        setActiveNav('search');
        setIsCartView(false);
        setAuthView(null);
        setSelectedProductId(null);
      } else if (path.includes('/cart')) {
        setIsCartView(true);
        setAuthView(null);
        setSelectedProductId(null);
      } else if (path.includes('/login') || path.includes('/signin')) {
        setAuthView('login');
        setIsCartView(false);
        setSelectedProductId(null);
      } else if (path.includes('/register') || path.includes('/signup')) {
        setAuthView('register');
        setIsCartView(false);
        setSelectedProductId(null);
      } else if (path.includes('/contact') || path.includes('/lien-he')) {
        setActiveNav('contact');
        setIsCartView(false);
        setAuthView(null);
        setSelectedProductId(null);
      } else if (path.includes('/guides') || path.includes('/huong-dan')) {
        setActiveNav('guides');
        setIsCartView(false);
        setAuthView(null);
        setSelectedProductId(null);
      } else if (path.includes('/products') || path.includes('/san-pham')) {
        setActiveNav('products');
        setIsCartView(false);
        setAuthView(null);
        setSelectedProductId(null);
      } else if (path.includes('/promotions') || path.includes('/khuyen-mai')) {
        setActiveNav('promos');
        setActiveTag('Khuyến mãi hot');
        setIsCartView(false);
        setAuthView(null);
        setSelectedProductId(null);
      } else if (path.includes('/news') || path.includes('/tin-tuc')) {
        setActiveNav('news');
        setIsCartView(false);
        setAuthView(null);
        setSelectedProductId(null);
      } else {
        setAuthView(null);
        setIsCartView(false);
        const match = path.match(/\/product\/(\d+)/i);
        if (match) {
          setSelectedProductId(parseInt(match[1], 10));
        } else {
          const params = new URLSearchParams(window.location.search);
          const pId = params.get('product') || params.get('id');
          if (pId) {
            setSelectedProductId(parseInt(pId, 10));
          } else {
            setSelectedProductId(null);
            if (path === '/' || path === '') {
              setActiveNav('home');
            }
          }
        }
      }
    };

    parseUrl();
    window.addEventListener('popstate', parseUrl);
    return () => window.removeEventListener('popstate', parseUrl);
  }, []);

  // Hàm chuyển đến trang chi tiết sản phẩm
  const handleSelectProduct = (id) => {
    setSelectedProductId(id);
    window.history.pushState({}, '', `/product/${id}`);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Điều hướng các tab chính (Trang chủ, Sản phẩm, Khuyến mãi, Hướng dẫn, Liên hệ...)
  const handleNavigate = (tab) => {
    setSelectedProductId(null);
    setAuthView(null);
    setIsCartView(false);
    setActiveNav(tab);

    if (tab === 'home') {
      window.history.pushState({}, '', '/');
    } else if (tab === 'products') {
      window.history.pushState({}, '', '/products');
    } else if (tab === 'promos') {
      setActiveTag("Khuyến mãi hot");
      window.history.pushState({}, '', '/promotions');
    } else if (tab === 'news') {
      window.history.pushState({}, '', '/news');
    } else if (tab === 'guides') {
      window.history.pushState({}, '', '/guides');
    } else if (tab === 'contact') {
      window.history.pushState({}, '', '/contact');
    }
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Hàm xử lý tìm kiếm và chuyển hướng sang trang kết quả tìm kiếm riêng biệt
  const handleSearch = (kw, cat = 'Tất cả danh mục') => {
    setSearchQuery({ keyword: kw, category: cat });
    setSelectedProductId(null);
    setAuthView(null);
    setIsCartView(false);
    setActiveNav('search');
    const params = new URLSearchParams();
    if (kw) params.append('q', kw);
    if (cat && cat !== 'Tất cả danh mục') params.append('cat', cat);
    const queryString = params.toString();
    window.history.pushState({}, '', queryString ? `/search?${queryString}` : '/search');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Hàm quay lại danh sách sản phẩm hoặc trang chủ
  const handleBackToList = () => {
    setSelectedProductId(null);
    setAuthView(null);
    setIsCartView(false);
    setActiveNav('products');
    window.history.pushState({}, '', '/products');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Hàm xử lý chọn danh mục phân loại laptop từ Trang chủ
  const handleSelectCategoryFromHome = (cat) => {
    setSelectedProductId(null);
    setAuthView(null);
    setIsCartView(false);
    setActiveNav('products');

    // Reset các bộ lọc phụ để hiển thị trọn vẹn danh mục
    setSelectedBrands([]);
    setSelectedSizes([]);
    setSelectedRams([]);
    setSelectedCpus([]);
    setCustomMin('');
    setCustomMax('');
    setAppliedCustomPrice(null);
    setCurrentPage(1);

    if (cat === 'student' || cat === 'Laptop Sinh Viên') {
      setActiveTag('Laptop Sinh Viên');
      setSelectedPriceRange('under30');
    } else if (cat === 'office' || cat === 'Laptop Văn Phòng') {
      setActiveTag('Laptop Văn Phòng');
      setSelectedPriceRange('all');
    } else if (cat === 'gaming' || cat === 'Laptop Gaming') {
      setActiveTag('Laptop Gaming');
      setSelectedPriceRange('all');
    } else if (cat === 'graphics' || cat === 'Laptop Đồ Họa - Kỹ Thuật' || cat === 'Laptop đồ họa') {
      setActiveTag('Laptop Đồ Họa - Kỹ Thuật');
      setSelectedPriceRange('all');
    } else if (cat === 'macbook' || cat === 'Apple MacBook') {
      setActiveTag('Apple MacBook');
      setSelectedPriceRange('all');
    } else {
      setActiveTag(cat || 'Tất cả sản phẩm');
      setSelectedPriceRange('all');
    }

    window.history.pushState({}, '', '/products');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const showToast = (msg) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(''), 3000);
  };

  // Thêm sản phẩm vào giỏ hàng với đúng giá trị RAM, SSD và Giá đã nâng cấp
  const handleAddToCart = (itemOrProduct, ramArg = null, storageArg = null, priceArg = null) => {
    const ramLabel = itemOrProduct.selectedRam || (typeof ramArg === 'string' ? ramArg : (itemOrProduct.specification ? `${itemOrProduct.specification.ramGB} GB` : '16 GB'));
    const storageLabel = itemOrProduct.selectedStorage || (typeof storageArg === 'string' ? storageArg : (itemOrProduct.specification ? `${itemOrProduct.specification.storageGB} GB SSD` : '512 GB SSD'));
    const finalPrice = itemOrProduct.price || priceArg || 0;
    const finalOrig = itemOrProduct.originalPrice || (finalPrice > 0 ? Math.round(finalPrice * 1.15) : null);
    const cartItemId = `${itemOrProduct.id}_${ramLabel}_${storageLabel}`;

    setCartItems((prev) => {
      const idx = prev.findIndex((item) => (item.cartItemId || item.id) === cartItemId);
      if (idx > -1) {
        const next = [...prev];
        next[idx] = {
          ...next[idx],
          quantity: (next[idx].quantity || 1) + 1,
          price: finalPrice,
          checked: true,
        };
        return next;
      }
      return [
        ...prev,
        {
          id: itemOrProduct.id,
          cartItemId,
          name: itemOrProduct.name,
          image: itemOrProduct.image || itemOrProduct.thumbnailUrl || '/images/default.png',
          price: finalPrice,
          originalPrice: finalOrig,
          selectedRam: ramLabel,
          selectedStorage: storageLabel,
          quantity: 1,
          checked: true,
        },
      ];
    });

    showToast(`Đã thêm "${itemOrProduct.name}" [RAM: ${ramLabel} • ${storageLabel}] vào giỏ hàng!`);
  };

  // Mua ngay (Thêm vào giỏ hàng và chuyển ngay đến trang Giỏ hàng)
  const handleBuyNow = (itemOrProduct, ramArg = null, storageArg = null, priceArg = null) => {
    handleAddToCart(itemOrProduct, ramArg, storageArg, priceArg);
    setSelectedProductId(null);
    setAuthView(null);
    setIsCartView(true);
    window.history.pushState({}, '', '/cart');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Cập nhật số lượng
  const handleUpdateCartQuantity = (cartItemId, delta) => {
    setCartItems((prev) =>
      prev
        .map((item) => {
          if ((item.cartItemId || item.id) === cartItemId) {
            const nextQty = (item.quantity || 1) + delta;
            return nextQty > 0 ? { ...item, quantity: nextQty } : null;
          }
          return item;
        })
        .filter(Boolean)
    );
  };

  // Xóa sản phẩm khỏi giỏ
  const handleRemoveCartItem = (cartItemId) => {
    setCartItems((prev) => prev.filter((item) => (item.cartItemId || item.id) !== cartItemId));
  };

  // Xóa toàn bộ giỏ
  const handleClearCart = () => {
    setCartItems([]);
  };

  // Chọn / bỏ chọn 1 sản phẩm
  const handleToggleCartCheck = (cartItemId) => {
    setCartItems((prev) =>
      prev.map((item) =>
        (item.cartItemId || item.id) === cartItemId
          ? { ...item, checked: item.checked === false ? true : false }
          : item
      )
    );
  };

  // Chọn / bỏ chọn tất cả
  const handleToggleAllCart = (checked) => {
    setCartItems((prev) => prev.map((item) => ({ ...item, checked })));
  };

  // Mở trang giỏ hàng
  const handleOpenCart = () => {
    setSelectedProductId(null);
    setAuthView(null);
    setIsCartView(true);
    window.history.pushState({}, '', '/cart');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Mở trang Đăng nhập hoặc Đăng ký
  const handleOpenAuth = (mode = 'login') => {
    setSelectedProductId(null);
    setAuthView(mode);
    window.history.pushState({}, '', `/${mode}`);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Xử lý khi đăng nhập hoặc đăng ký thành công
  const handleAuthSuccess = (user) => {
    setCurrentUser(user);
    try {
      localStorage.setItem('pcstore_user', JSON.stringify(user));
    } catch (e) {}
    setAuthView(null);
    window.history.pushState({}, '', '/');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Xử lý Đăng xuất
  const handleLogout = async () => {
    try {
      await fetch('http://localhost:5148/api/auth/logout', { method: 'POST' });
    } catch (e) {}
    setCurrentUser(null);
    try {
      localStorage.removeItem('pcstore_user');
    } catch (e) {}
  };

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

  // Toggle dung lượng RAM (FPT Shop Style)
  const toggleRam = (ram) => {
    setSelectedRams((prev) =>
      prev.includes(ram) ? prev.filter((r) => r !== ram) : [...prev, ram]
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

  // Áp dụng nhập giá tùy biến
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
    setSelectedRams([]);
    setSelectedCpus([]);
    setSelectedPriceRange("all");
    setCustomMin("");
    setCustomMax("");
    setAppliedCustomPrice(null);
    setSortBy("newest");
    setCurrentPage(1);
  };

  // Gọi API từ ASP.NET Core Backend (GET: /api/products)
  useEffect(() => {
    let isMounted = true;

    const fetchProducts = async () => {
      setIsLoading(true);
      try {
        const params = new URLSearchParams();
        if (activeTag && activeTag !== "Tất cả sản phẩm") params.append("tag", activeTag);
        if (selectedBrands.length > 0) params.append("brands", selectedBrands.join(","));
        if (selectedSizes.length > 0) params.append("sizes", selectedSizes.join(","));
        if (selectedRams.length > 0) params.append("rams", selectedRams.join(","));
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
        if (!res.ok) throw new Error(`Lỗi kết nối API: ${res.status}`);
        
        const data = await res.json();

        if (isMounted && data && Array.isArray(data.items)) {
          setProducts(data.items);
          setTotalCount(data.totalItems || 0);
          setIsBackendConnected(true);
        }
      } catch (err) {
        if (isMounted) {
          console.warn("Chưa kết nối được API Backend, sử dụng dữ liệu mẫu có lọc:", err);
          setIsBackendConnected(false);

          // Lọc offline dữ liệu mock để demo
          let filtered = [...PRODUCT_DATA];

          // Lọc Tag đồng nhất theo 5 nhóm laptop chính thống
          if (activeTag && activeTag !== "Tất cả sản phẩm") {
            const cleanTag = activeTag.toLowerCase();
            if (cleanTag.includes("sinh viên")) {
              filtered = filtered.filter(p => p.price <= 30000000);
            } else if (cleanTag.includes("văn phòng") || cleanTag.includes("học tập")) {
              filtered = filtered.filter(p => p.category.toLowerCase().includes("văn phòng") || p.category.toLowerCase().includes("học tập"));
            } else if (cleanTag.includes("gaming")) {
              filtered = filtered.filter(p => p.category.toLowerCase().includes("gaming"));
            } else if (cleanTag.includes("đồ họa") || cleanTag.includes("kỹ thuật")) {
              filtered = filtered.filter(p => p.category.toLowerCase().includes("đồ họa") || p.category.toLowerCase().includes("kỹ thuật"));
            } else if (cleanTag.includes("macbook") || cleanTag.includes("apple")) {
              filtered = filtered.filter(p => p.brand.toLowerCase() === "apple" || p.name.toLowerCase().includes("macbook") || p.category.toLowerCase().includes("macbook"));
            } else {
              filtered = filtered.filter(p => p.category.toLowerCase().includes(cleanTag) || cleanTag.includes(p.category.toLowerCase()));
            }
          }

          // Lọc Hãng
          if (selectedBrands.length > 0) {
            const brandsLower = selectedBrands.map(b => b.toLowerCase());
            filtered = filtered.filter(p => brandsLower.includes(p.brand.toLowerCase()));
          }

          // Lọc RAM
          if (selectedRams.length > 0) {
            const ramNums = selectedRams.map(r => parseInt(r.replace(/\D/g, ''), 10));
            filtered = filtered.filter(p => {
              const m = p.specs?.match(/(\d+)\s*GB/i);
              return m ? ramNums.includes(parseInt(m[1], 10)) : false;
            });
          }

          // Lọc Màn hình
          if (selectedSizes.length > 0) {
            filtered = filtered.filter(p => selectedSizes.some(s => p.screenSize.includes(s.replace('inch', '').trim())));
          }

          // Lọc CPU
          if (selectedCpus.length > 0) {
            filtered = filtered.filter(p => selectedCpus.some(c => p.specs.toLowerCase().includes(c.toLowerCase().replace('intel ', '').replace('amd ', ''))));
          }

          // Lọc Giá
          if (selectedPriceRange === 'under10') filtered = filtered.filter(p => p.price < 10000000);
          else if (selectedPriceRange === '10to20') filtered = filtered.filter(p => p.price >= 10000000 && p.price <= 20000000);
          else if (selectedPriceRange === '20to30') filtered = filtered.filter(p => p.price >= 20000000 && p.price <= 30000000);
          else if (selectedPriceRange === 'under30') filtered = filtered.filter(p => p.price <= 30000000);
          else if (selectedPriceRange === 'above30') filtered = filtered.filter(p => p.price > 30000000);

          setTotalCount(filtered.length);
          const start = (currentPage - 1) * pageSize;
          setProducts(filtered.slice(start, start + pageSize));
        }
      } finally {
        if (isMounted) setIsLoading(false);
      }
    };

    fetchProducts();

    return () => {
      isMounted = false;
    };
  }, [activeTag, selectedBrands, selectedSizes, selectedRams, selectedCpus, selectedPriceRange, appliedCustomPrice, sortBy, currentPage]);

  return (
    <div className="min-vh-100 pb-5">
      <TopHeader 
        onGoHome={() => handleNavigate('home')} 
        onOpenLogin={() => handleOpenAuth('login')}
        currentUser={currentUser}
        onLogout={handleLogout}
        cartCount={cartItems.reduce((s, i) => s + (i.quantity || 1), 0)}
        onOpenCart={handleOpenCart}
        onSearch={handleSearch}
        searchKeyword={searchQuery.keyword}
        searchCategory={searchQuery.category}
      />
      <Navbar activeNav={activeNav} onNavigate={handleNavigate} onGoHome={() => handleNavigate('home')} />

      {/* Điều hướng giao diện đa trang: Đăng nhập/Đăng ký -> Giỏ hàng -> Chi tiết sản phẩm -> Trang chủ / Liên hệ / Hướng dẫn / Sản phẩm */}
      {authView ? (
        <AuthPage
          initialMode={authView}
          onSuccess={handleAuthSuccess}
          onGoHome={() => handleNavigate('home')}
        />
      ) : isCartView ? (
        <CartPage
          cartItems={cartItems}
          onUpdateQuantity={handleUpdateCartQuantity}
          onRemoveItem={handleRemoveCartItem}
          onClearCart={handleClearCart}
          onToggleCheck={handleToggleCartCheck}
          onToggleAll={handleToggleAllCart}
          onGoHome={() => handleNavigate('home')}
          onSelectProduct={handleSelectProduct}
          currentUser={currentUser}
        />
      ) : selectedProductId ? (
        <ProductDetail
          productId={selectedProductId}
          onBack={() => handleNavigate(activeNav === 'home' ? 'home' : 'products')}
          onSelectProduct={handleSelectProduct}
          allProducts={products}
          onAddToCart={handleAddToCart}
          onBuyNow={handleBuyNow}
        />
      ) : activeNav === 'search' ? (
        <SearchResultsPage
          keyword={searchQuery.keyword}
          category={searchQuery.category}
          onSelectProduct={handleSelectProduct}
          onAddToCart={handleAddToCart}
          onSearchAgain={handleSearch}
          onGoHome={() => handleNavigate('home')}
          onNavigateToProducts={() => handleNavigate('products')}
        />
      ) : activeNav === 'home' ? (
        <HomePage
          products={products}
          onSelectProduct={handleSelectProduct}
          onAddToCart={handleAddToCart}
          onNavigateToProducts={() => handleNavigate('products')}
          onSelectCategory={handleSelectCategoryFromHome}
        />
      ) : activeNav === 'promos' ? (
        <PromosPage
          products={products}
          onSelectProduct={handleSelectProduct}
          onAddToCart={handleAddToCart}
          onNavigateToProducts={() => handleNavigate('products')}
        />
      ) : activeNav === 'contact' ? (
        <ContactPage onGoHome={() => handleNavigate('home')} />
      ) : activeNav === 'guides' ? (
        <GuidesPage onGoHome={() => handleNavigate('home')} />
      ) : (
        /* Trang danh sách sản phẩm - Đã xóa CategorySidebar theo yêu cầu */
        <div className="site-container py-3">
          <div className="d-flex gap-3 align-items-start">
            {/* Sidebar chỉ còn Bộ lọc sản phẩm (CategorySidebar đã được loại bỏ) */}
            <aside className="sidebar-fixed d-none d-md-block">
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
                selectedRams={selectedRams}
                toggleRam={toggleRam}
                selectedCpus={selectedCpus}
                toggleCpu={toggleCpu}
                onResetFilters={handleResetFilters}
              />
            </aside>

            {/* Vùng hiển thị danh sách sản phẩm */}
            <main className="content-fluid-area">
              <ProductToolbar
                activeTag={activeTag}
                setActiveTag={(tag) => {
                  setActiveTag(tag);
                  if (tag === 'Laptop Sinh Viên') {
                    setSelectedPriceRange('under30');
                  } else if (selectedPriceRange === 'under30') {
                    setSelectedPriceRange('all');
                  }
                  setCurrentPage(1);
                }}
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
                <ProductList products={products} onSelectProduct={handleSelectProduct} onAddToCart={handleAddToCart} />
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
      )}
    
      {/* Toast thông báo nổi khi thêm vào giỏ hàng */}
      {toastMessage && (
        <div 
          className="position-fixed bottom-0 end-0 m-4 p-3 bg-dark text-white rounded-3 shadow-lg z-3 d-flex align-items-center gap-2"
          style={{ maxWidth: '380px' }}
        >
          <i className="bi bi-check-circle-fill text-success fs-5"></i>
          <div className="small fw-semibold">{toastMessage}</div>
        </div>
      )}
    </div>
  );
}

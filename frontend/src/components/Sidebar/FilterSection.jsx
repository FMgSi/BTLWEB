import React, { useState } from 'react';
import { BRANDS_LIST, SCREEN_SIZES, CPU_TYPES } from '../../data/mockData';

export default function FilterSection({
  selectedPriceRange,
  setSelectedPriceRange,
  customMin,
  setCustomMin,
  customMax,
  setCustomMax,
  onApplyCustomPrice,
  selectedBrands,
  toggleBrand,
  selectedSizes,
  toggleSize,
  selectedCpus = [],
  toggleCpu,
  onResetFilters
}) {
  const [showAllBrands, setShowAllBrands] = useState(false);

  // Mặc định hiện 4 hãng đầu, khi bấm 'Xem thêm' sẽ hiện toàn bộ
  const visibleBrands = showAllBrands ? BRANDS_LIST : BRANDS_LIST.slice(0, 4);

  return (
    <div className="p-3 border rounded bg-white shadow-sm" style={{ fontSize: '12px' }}>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h6 className="fw-bold m-0" style={{ fontSize: '12px' }}>BỘ LỌC SẢN PHẨM</h6>
        <button 
          onClick={onResetFilters} 
          className="btn btn-link p-0 text-decoration-none text-muted" 
          style={{ fontSize: '11px' }}
        >
          Xóa tất cả
        </button>
      </div>

      {/* 1. LỌC THEO GIÁ */}
      <div className="mb-3">
        <p className="fw-semibold mb-2 text-secondary">Khoảng giá</p>
        
        <div className="form-check mb-1">
          <input
            className="form-check-input"
            type="radio"
            name="priceRadio"
            id="p-all"
            checked={selectedPriceRange === 'all'}
            onChange={() => setSelectedPriceRange('all')}
          />
          <label className="form-check-label text-nowrap" htmlFor="p-all">Tất cả khoảng giá</label>
        </div>

        <div className="form-check mb-1">
          <input
            className="form-check-input"
            type="radio"
            name="priceRadio"
            id="p1"
            checked={selectedPriceRange === 'under10'}
            onChange={() => setSelectedPriceRange('under10')}
          />
          <label className="form-check-label text-nowrap" htmlFor="p1">Dưới 10.000.000đ</label>
        </div>

        <div className="form-check mb-1">
          <input
            className="form-check-input"
            type="radio"
            name="priceRadio"
            id="p2"
            checked={selectedPriceRange === '10to20'}
            onChange={() => setSelectedPriceRange('10to20')}
          />
          <label className="form-check-label text-nowrap" htmlFor="p2">10.000.000đ - 20.000.000đ</label>
        </div>

        <div className="form-check mb-1">
          <input
            className="form-check-input"
            type="radio"
            name="priceRadio"
            id="p3"
            checked={selectedPriceRange === '20to30'}
            onChange={() => setSelectedPriceRange('20to30')}
          />
          <label className="form-check-label text-nowrap" htmlFor="p3">20.000.000đ - 30.000.000đ</label>
        </div>

        <div className="form-check mb-2">
          <input
            className="form-check-input"
            type="radio"
            name="priceRadio"
            id="p4"
            checked={selectedPriceRange === 'above30'}
            onChange={() => setSelectedPriceRange('above30')}
          />
          <label className="form-check-label text-nowrap" htmlFor="p4">Trên 30.000.000đ</label>
        </div>

        {/* Nhập khoảng giá Min - Max */}
        <div className="d-flex align-items-center gap-1 mb-2">
          <input
            type="number"
            placeholder="Từ"
            value={customMin}
            onChange={(e) => setCustomMin(e.target.value)}
            className="form-control form-control-sm text-center px-1"
            style={{ fontSize: '11px' }}
          />
          <span>-</span>
          <input
            type="number"
            placeholder="Đến"
            value={customMax}
            onChange={(e) => setCustomMax(e.target.value)}
            className="form-control form-control-sm text-center px-1"
            style={{ fontSize: '11px' }}
          />
        </div>
        <button 
          onClick={onApplyCustomPrice}
          className="btn btn-pc-red btn-sm w-100 py-1 fw-bold" 
          style={{ fontSize: '11px' }}
        >
          LỌC
        </button>
      </div>

      <hr className="text-muted opacity-25" />

      {/* 2. LỌC THEO HÃNG SẢN XUẤT (Có nút bung Xem thêm) */}
      <div className="mb-3">
        <p className="fw-semibold mb-2 text-secondary">Hãng sản xuất</p>
        {visibleBrands.map((brand) => (
          <div className="form-check mb-1" key={brand}>
            <input
              className="form-check-input"
              type="checkbox"
              id={`brand-${brand}`}
              checked={selectedBrands.includes(brand)}
              onChange={() => toggleBrand(brand)}
            />
            <label className="form-check-label cursor-pointer" htmlFor={`brand-${brand}`}>
              {brand}
            </label>
          </div>
        ))}
        
        {/* Nút Xem thêm / Thu gọn */}
        <button
          type="button"
          onClick={() => setShowAllBrands(!showAllBrands)}
          className="btn btn-link p-0 text-decoration-none text-muted d-inline-flex align-items-center gap-1 mt-1 border-0"
          style={{ fontSize: '11px' }}
        >
          {showAllBrands ? (
            <>Thu gọn <i className="bi bi-chevron-up"></i></>
          ) : (
            <>Xem thêm <i className="bi bi-chevron-down"></i></>
          )}
        </button>
      </div>

      <hr className="text-muted opacity-25" />

      {/* 3. LỌC THEO KÍCH THƯỚC MÀN HÌNH */}
      <div className="mb-3">
        <p className="fw-semibold mb-2 text-secondary">Kích thước màn hình</p>
        {SCREEN_SIZES.map((size) => (
          <div className="form-check mb-1" key={size}>
            <input
              className="form-check-input"
              type="checkbox"
              id={`size-${size}`}
              checked={selectedSizes.includes(size)}
              onChange={() => toggleSize(size)}
            />
            <label className="form-check-label cursor-pointer" htmlFor={`size-${size}`}>
              {size}
            </label>
          </div>
        ))}
      </div>

      <hr className="text-muted opacity-25" />

      {/* 4. LỌC THEO VI XỬ LÝ (CPU) */}
      <div>
        <p className="fw-semibold mb-2 text-secondary">Vi xử lý (CPU)</p>
        {CPU_TYPES.map((cpu) => (
          <div className="form-check mb-1" key={cpu}>
            <input
              className="form-check-input"
              type="checkbox"
              id={`cpu-${cpu}`}
              checked={selectedCpus.includes(cpu)}
              onChange={() => toggleCpu(cpu)}
            />
            <label className="form-check-label cursor-pointer" htmlFor={`cpu-${cpu}`}>
              {cpu}
            </label>
          </div>
        ))}
      </div>
    </div>
  );
}
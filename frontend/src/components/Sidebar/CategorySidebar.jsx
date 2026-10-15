import React from 'react';

// Danh sách 5 phân loại laptop hiển thị đồng nhất trên Sidebar trang chủ và Toolbar sản phẩm
const LAPTOP_CATEGORIES = [
  { id: 'student', label: 'Laptop Sinh Viên', icon: 'bi-mortarboard-fill', tag: 'Laptop Sinh Viên' },
  { id: 'office', label: 'Laptop Văn Phòng', icon: 'bi-briefcase-fill', tag: 'Laptop Văn Phòng' },
  { id: 'gaming', label: 'Laptop Gaming', icon: 'bi-controller', tag: 'Laptop Gaming' },
  { id: 'graphics', label: 'Laptop Đồ Họa - Kỹ Thuật', icon: 'bi-palette-fill', tag: 'Laptop Đồ Họa - Kỹ Thuật' },
  { id: 'macbook', label: 'Apple MacBook', icon: 'bi-apple', tag: 'Apple MacBook' },
];

export default function CategorySidebar({ onSelectCategory }) {
  return (
    <div className="border rounded-3 bg-white mb-3 shadow-sm overflow-hidden">
      {LAPTOP_CATEGORIES.map((cat) => (
        <div
          key={cat.id}
          className="d-flex align-items-center justify-content-between px-3 py-3 border-bottom text-dark"
          style={{ cursor: 'pointer', fontSize: '13px', fontWeight: '600', transition: 'all 0.15s ease' }}
          onMouseEnter={(e) => {
            e.currentTarget.style.backgroundColor = '#fef2f2';
            e.currentTarget.style.color = '#e11b1e';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.backgroundColor = '#ffffff';
            e.currentTarget.style.color = '#1e293b';
          }}
          onClick={() => onSelectCategory && onSelectCategory(cat.label)}
        >
          <div className="d-flex align-items-center gap-2">
            <i className={`bi ${cat.icon} text-danger fs-6`}></i>
            <span>{cat.label}</span>
          </div>
          <i className="bi bi-chevron-right text-muted" style={{ fontSize: '11px' }}></i>
        </div>
      ))}
    </div>
  );
}

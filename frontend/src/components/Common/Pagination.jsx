import React from 'react';

export default function Pagination({
  currentPage = 1,
  totalItems = 0,
  pageSize = 12,
  onPageChange
}) {
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

  if (totalItems === 0) return null;

  // Tính danh sách các số trang cần hiển thị
  const getPageNumbers = () => {
    const pages = [];
    if (totalPages <= 7) {
      for (let i = 1; i <= totalPages; i++) pages.push(i);
    } else {
      if (currentPage <= 4) {
        pages.push(1, 2, 3, 4, 5, '...', totalPages);
      } else if (currentPage >= totalPages - 3) {
        pages.push(1, '...', totalPages - 4, totalPages - 3, totalPages - 2, totalPages - 1, totalPages);
      } else {
        pages.push(1, '...', currentPage - 1, currentPage, currentPage + 1, '...', totalPages);
      }
    }
    return pages;
  };

  const startItem = (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(currentPage * pageSize, totalItems);

  return (
    <div className="d-flex flex-wrap justify-content-between align-items-center mt-4 pt-2 border-top">
      <nav>
        <ul className="pagination pagination-sm m-0">
          {/* Nút Trang Trước */}
          <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
            <button
              className="page-link text-dark"
              onClick={() => onPageChange && onPageChange(currentPage - 1)}
              disabled={currentPage === 1}
              title="Trang trước"
            >
              <i className="bi bi-chevron-left"></i>
            </button>
          </li>

          {/* Các số trang */}
          {getPageNumbers().map((page, index) => {
            if (page === '...') {
              return (
                <li key={`ellipsis-${index}`} className="page-item disabled">
                  <span className="page-link">...</span>
                </li>
              );
            }

            const isActive = page === currentPage;
            return (
              <li key={page} className={`page-item ${isActive ? 'active' : ''}`}>
                <button
                  className={`page-link ${isActive ? 'bg-pc-red text-white border-0 fw-bold' : 'text-dark'}`}
                  onClick={() => onPageChange && onPageChange(page)}
                >
                  {page}
                </button>
              </li>
            );
          })}

          {/* Nút Trang Sau */}
          <li className={`page-item ${currentPage === totalPages ? 'disabled' : ''}`}>
            <button
              className="page-link text-dark"
              onClick={() => onPageChange && onPageChange(currentPage + 1)}
              disabled={currentPage === totalPages}
              title="Trang tiếp theo"
            >
              <i className="bi bi-chevron-right"></i>
            </button>
          </li>
        </ul>
      </nav>

      <div className="text-muted" style={{ fontSize: '12px' }}>
        Hiển thị <strong>{startItem} - {endItem}</strong> trong tổng số <strong>{totalItems}</strong> sản phẩm
      </div>
    </div>
  );
}
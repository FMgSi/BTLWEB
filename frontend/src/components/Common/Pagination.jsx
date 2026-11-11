import React from 'react';

export default function Pagination({
  currentPage = 1,
  totalItems = 0,
  pageSize = 12,
  onPageChange
}) {
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

  if (totalItems === 0) return null;

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
    <div className="d-flex flex-wrap justify-content-between align-items-center mt-4 pt-3 border-top">
      <nav aria-label="Page navigation">
        <ul className="d-flex gap-1 list-unstyled align-items-center m-0 p-0">
          <li>
            <button
              type="button"
              className={`btn btn-sm ${currentPage === 1 ? 'btn-light border text-muted disabled opacity-50' : 'btn-light border text-dark'}`}
              onClick={() => onPageChange && onPageChange(currentPage - 1)}
              disabled={currentPage === 1}
              title="Trang trước"
              style={{
                minWidth: '34px',
                height: '34px',
                borderRadius: '6px',
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontWeight: 500
              }}
            >
              <i className="bi bi-chevron-left"></i>
            </button>
          </li>

          {getPageNumbers().map((page, index) => {
            if (page === '...') {
              return (
                <li key={`ellipsis-${index}`}>
                  <span
                    className="text-muted d-inline-flex align-items-center justify-content-center px-1 user-select-none"
                    style={{ minWidth: '24px', height: '34px', fontWeight: 700 }}
                  >
                    ...
                  </span>
                </li>
              );
            }

            const isActive = page === currentPage;
            return (
              <li key={page}>
                <button
                  type="button"
                  className={`btn btn-sm ${isActive ? 'btn-dark text-white fw-bold shadow-sm' : 'btn-light border text-dark'}`}
                  onClick={() => onPageChange && onPageChange(page)}
                  title={`Trang ${page}`}
                  style={{
                    minWidth: '34px',
                    height: '34px',
                    borderRadius: '6px',
                    display: 'inline-flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontWeight: isActive ? 700 : 500
                  }}
                >
                  {page}
                </button>
              </li>
            );
          })}

          <li>
            <button
              type="button"
              className={`btn btn-sm ${currentPage === totalPages ? 'btn-light border text-muted disabled opacity-50' : 'btn-light border text-dark'}`}
              onClick={() => onPageChange && onPageChange(currentPage + 1)}
              disabled={currentPage === totalPages}
              title="Trang tiếp theo"
              style={{
                minWidth: '34px',
                height: '34px',
                borderRadius: '6px',
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontWeight: 500
              }}
            >
              <i className="bi bi-chevron-right"></i>
            </button>
          </li>
        </ul>
      </nav>

      <div className="text-muted small">
        Hiển thị <strong>{startItem} - {endItem}</strong> trong tổng số <strong>{totalItems}</strong> sản phẩm
      </div>
    </div>
  );
}

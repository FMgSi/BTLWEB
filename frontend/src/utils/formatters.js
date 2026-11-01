/**
 * Chuẩn hóa và định dạng số tiền sang chuẩn hiển thị tiền tệ Việt Nam (VND).
 *
 * @param {number|string|null|undefined} num - Giá trị tiền cần format
 * @param {string} [symbol='₫'] - Ký hiệu tiền tệ ('₫' hoặc 'đ')
 * @returns {string} Chuỗi tiền tệ đã format (ví dụ: '15.000.000₫' hoặc '0₫')
 */
export const formatVND = (num, symbol = '₫') => {
  if (num === null || num === undefined || num === '') return `0${symbol}`;
  const parsed = Number(num);
  if (isNaN(parsed)) return `0${symbol}`;
  return parsed.toLocaleString('vi-VN') + symbol;
};

export const formatVND = (num, symbol = '₫') => {
  if (num === null || num === undefined || num === '') return `0${symbol}`;
  const parsed = Number(num);
  if (isNaN(parsed)) return `0${symbol}`;
  return parsed.toLocaleString('vi-VN') + symbol;
};

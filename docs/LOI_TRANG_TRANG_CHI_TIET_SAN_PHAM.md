# Trang trắng khi mở /product/199

## Hiện tượng và chẩn đoán

Ảnh ghi nhận trang trống khi mở trực tiếp sản phẩm 199. Trong ProductDetail, lần render đầu trả về giao diện đang tải. Khi lấy được sản phẩm, lần render kế tiếp đi qua thêm ba lời gọi React.useMemo để tính danh sách màu, danh sách cấu hình theo màu và phiên bản đang chọn. React yêu cầu cùng một component gọi các hook theo cùng thứ tự ở mọi lần render. Trước khi sửa, ESLint báo react-hooks/rules-of-hooks ở cả ba vị trí. Lỗi thứ tự này xảy ra cả với sản phẩm có danh sách phiên bản rỗng vì bản thân hook vẫn được gọi.

## Thay đổi

ProductDetail tính các giá trị trên trực tiếp từ dữ liệu sản phẩm sau bước kiểm tra tải và tồn tại. Khi đổi sản phẩm, App dùng mã sản phẩm làm key để React khởi tạo lại component, bao gồm trạng thái đang tải, ảnh và lựa chọn cấu hình.

## Kiểm chứng

Trong thư mục frontend, đã chạy thành công:

- npx eslint src/components/Product/ProductDetail.jsx
- npm run build

Hai phép kiểm tra xác nhận mã tuân thủ quy tắc hook và đóng gói được. Chưa xác minh giao diện trong phiên trình duyệt kết nối DB của người dùng.

## Kiểm tra giao diện khi chạy ứng dụng

1. Mở trực tiếp /product/199 và tải lại. Sau trạng thái đang tải phải thấy chi tiết máy; Console không xuất hiện lỗi thay đổi số hook.
2. Từ trang chủ mở một máy có nhiều phiên bản, chọn màu và cấu hình, sau đó chuyển sang một sản phẩm liên quan. Trang mới phải khởi tạo lại ảnh và lựa chọn.
3. Thử máy không có phiên bản và mã không tồn tại trong cả API lẫn dữ liệu mẫu. Trường hợp đầu hiển thị cấu hình cơ bản; trường hợp sau hiển thị thông báo và nút quay lại.

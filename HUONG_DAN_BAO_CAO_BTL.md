# 🎓 CẨM NANG BÁO CÁO & TRẢ LỜI VẤN ĐÁP BÀI TẬP LỚN (BTL)
## Đề tài: Hệ Thống Website Bán Laptop (Laptop Store)

Tài liệu này được biên soạn dành riêng cho sinh viên để **nắm chắc bản chất dự án**, hiểu cặn kẽ từng dòng code và **tự tin trả lời xuất sắc mọi câu hỏi của thầy cô** khi bảo vệ/vấn đáp BTL.

---

## 🏛️ PHẦN 1: TỔNG QUAN KIẾN TRÚC HỆ THỐNG

### 1. Mô hình dự án
Dự án được xây dựng theo kiến trúc **Tách biệt Frontend & Backend (Full-stack RESTful API)** kết hợp **MVC Admin truyền thống**:
- **Frontend (Khách hàng)**: Viết bằng **React 19 (Single Page Application - SPA)**. Giao diện nhanh, mượt mà, không bị reload trang khi lọc hay chuyển trang.
- **Backend (Web API)**: Viết bằng **ASP.NET Core 8.0**, đóng vai trò cung cấp RESTful API (trả về dữ liệu JSON).
- **Backend (Admin Panel)**: Viết bằng **ASP.NET Core MVC (Razor Views)** để quản trị viên thêm, sửa, xóa laptop.
- **Cơ sở dữ liệu (Database)**: **MySQL Server**, giao tiếp thông qua thư viện ORM **Entity Framework Core (EF Core)**.

### 2. Sơ đồ luồng dữ liệu (Data Flow) khi khách hàng lọc sản phẩm
```text
[Khách bấm lọc CPU/Hãng trên React]
       │
       ▼
1. React (App.jsx) gom các điều kiện lại thành query: /api/products?brands=asus&cpus=i7&page=1
       │ (Gửi HTTP GET Request)
       ▼
2. ProductsApiController.cs tiếp nhận Request
       │
       ▼
3. LINQ & EF Core xây dựng câu truy vấn SQL tương ứng
       │ (SELECT * FROM Products WHERE Brand = 'ASUS' AND CPU LIKE '%i7%' LIMIT 12 OFFSET 0)
       ▼
4. MySQL Server thực thi và trả kết quả về cho C#
       │
       ▼
5. Backend đóng gói thành JSON (gồm danh sách sản phẩm + tổng số lượng trang)
       │ (Trả HTTP Response 200 OK)
       ▼
6. React nhận dữ liệu JSON -> Cập nhật State -> Giao diện tự động render lại sản phẩm mới
```

---

## 📂 PHẦN 2: GIẢI THÍCH CÁC FILE QUAN TRỌNG NHẤT

### 1. `frontend/src/App.jsx` (Giao diện React khách hàng)
- **State quan trọng**:
  - `activeTag`, `selectedBrands`, `selectedSizes`, `selectedCpus`, `selectedPriceRange`: Lưu các lựa chọn lọc của người dùng.
  - `currentPage`: Trang hiện tại (1, 2, 3...).
  - `products`: Mảng chứa danh sách sản phẩm hiển thị trên màn hình.
  - `totalCount`: Tổng số sản phẩm khớp với bộ lọc (để tính ra số trang).
- **Hàm cốt lõi**:
  - `useEffect(...)`: Tự động chạy mỗi khi người dùng bấm chọn bộ lọc hoặc đổi trang. Nó dùng `URLSearchParams` để tạo đường dẫn API và gửi `fetch('/api/products?...')` lên Backend.

### 2. `LaptopStore/Controllers/Api/ProductsApiController.cs` (Trái tim API Backend)
Controller này tiếp nhận yêu cầu từ React và xử lý theo **9 bước tuần tự rất rõ ràng**:
- **Bước 1**: Tạo câu query gốc từ `_db.Products` kèm `Include(Brand, Category, Specification)`.
- **Bước 2**: Lọc theo Danh mục (`Category`).
- **Bước 3**: Lọc theo Hãng (`Brand`).
- **Bước 4**: Lọc theo Kích thước màn hình (`ScreenSizeInch`).
- **Bước 5**: Lọc theo Vi xử lý (`CPU`).
- **Bước 6**: Lọc theo Khoảng giá (`Price`).
- **Bước 7**: Sắp xếp (`OrderBy` / `OrderByDescending`).
- **Bước 8**: Phân trang Server-side bằng cách đếm `CountAsync()` và cắt dữ liệu bằng `.Skip((page - 1) * pageSize).Take(pageSize)`.
- **Bước 9**: Ánh xạ sang DTO và trả về JSON chuẩn HTTP 200 OK.

### 3. `LaptopStore/Controllers/ProductController.cs` (Quản trị Admin MVC)
Dành cho người quản trị website, gồm đầy đủ 4 thao tác CRUD chuẩn:
- `Index()`: Xem danh sách sản phẩm có phân trang và tìm kiếm theo từ khóa.
- `Create()`: Thêm laptop mới, có hỗ trợ upload hình ảnh vào thư mục `wwwroot/uploads`.
- `Edit()`: Cập nhật thông tin laptop và cấu hình.
- `Delete()`: Xóa sản phẩm khỏi database.

### 4. `LaptopStore/Data/DbInitializer.cs` (Khởi tạo dữ liệu mẫu)
- Tự động gọi `context.Database.EnsureCreated()` để tạo database `LaptopStoreDb` nếu máy chưa có.
- Đọc file `sql-init/init-db.sql` để nạp sẵn 100+ laptop mẫu giúp giảng viên khi chấm bài không cần phải tự nhập liệu thủ công.

---

## 🎯 PHẦN 3: 8 CÂU HỎI THƯỜNG GẶP KHI VẤN ĐÁP & CÁCH TRẢ LỜI

### ❓ Câu 1: Em hãy giải thích mô hình kiến trúc của bài tập lớn này?
> **Trả lời**:  
> "Thưa thầy/cô, hệ thống của em áp dụng kiến trúc tách biệt Client - Server. Phía Client là ứng dụng Single Page Application viết bằng **React**, đảm nhận toàn bộ việc hiển thị giao diện và tương tác người dùng. Phía Server là **ASP.NET Core Web API**, đóng vai trò xử lý logic nghiệp vụ và truy vấn cơ sở dữ liệu MySQL qua Entity Framework Core. Hai bên giao tiếp với nhau độc lập thông qua giao thức **HTTP RESTful API** với định dạng trao đổi dữ liệu là **JSON**."

---

### ❓ Câu 2: Chức năng phân trang được xử lý ở Client (React) hay Server (ASP.NET)? Vì sao?
> **Trả lời**:  
> "Thưa thầy/cô, phân trang trong dự án của em được xử lý hoàn toàn ở **Server-side (phía máy chủ)**.  
> Cụ thể, trong file `ProductsApiController.cs`, em sử dụng hai hàm của LINQ là `.Skip((page - 1) * pageSize)` và `.Take(pageSize)`.  
> Lý do em chọn phân trang ở Server thay vì kéo hết về Client là để **tối ưu hiệu năng**: nếu database có hàng ngàn sản phẩm, việc kéo toàn bộ về máy khách sẽ làm nghẽn mạng và lag trình duyệt. Với Server-side, mỗi trang em chỉ lấy đúng 12 sản phẩm cần hiển thị."

---

### ❓ Câu 3: Làm thế nào để Backend xử lý được nhiều bộ lọc cùng lúc (Hãng + CPU + Giá + Màn hình)?
> **Trả lời**:  
> "Dạ, em sử dụng kỹ thuật **Dynamic Query (Truy vấn động)** với `IQueryable` trong Entity Framework Core.  
> Ban đầu em khởi tạo một câu query gốc. Sau đó, cứ mỗi điều kiện người dùng gửi lên (nếu có giá trị), em lại nối thêm một mệnh đề `.Where(...)` vào query. Cho đến khi gọi hàm `.ToListAsync()`, Entity Framework Core mới tổng hợp tất cả các điều kiện đó thành một câu lệnh SQL `SELECT ... WHERE ... AND ...` duy nhất để gửi xuống MySQL."

---

### ❓ Câu 4: Em quản lý trạng thái (State) ở React như thế nào khi người dùng bấm lọc?
> **Trả lời**:  
> "Dạ, trong file `App.jsx`, em sử dụng Hook `useState` để lưu trữ trạng thái của các bộ lọc như `selectedBrands`, `selectedCpus`, `currentPage`.  
> Khi người dùng bấm vào một nút lọc, hàm toggle tương ứng sẽ cập nhật lại State. Nhờ Hook `useEffect` có mảng dependency chứa các State này, mỗi khi có bất kỳ sự thay đổi nào thì `useEffect` sẽ tự động kích hoạt gọi API lấy dữ liệu mới về và cập nhật lại giao diện ngay lập tức mà không cần reload trang."

---

### ❓ Câu 5: Dữ liệu hình ảnh sản phẩm được lưu trữ như thế nào?
> **Trả lời**:  
> "Dạ, trong database em chỉ lưu đường dẫn tương đối của ảnh (ví dụ: `/images/products/asus-rog-strix-g16.png` hoặc `/uploads/...`).  
> File ảnh thực tế được lưu trữ nội bộ trong thư mục `wwwroot` của Backend và `public/images` của Frontend. Cách làm này giúp database nhẹ, truy vấn nhanh và dễ sao lưu."

---

### ❓ Câu 6: DTO là gì và tại sao lại dùng DTO trong API?
> **Trả lời**:  
> "Dạ, DTO là viết tắt của **Data Transfer Object** (Đối tượng chuyển giao dữ liệu).  
> Thay vì trả trực tiếp Entity `Product` (vốn chứa nhiều trường database phức tạp hoặc quan hệ vòng tròn), em tạo class `ProductApiDto` chỉ chứa đúng các trường mà giao diện React cần (như Tên, Giá, Ảnh, Chuỗi tóm tắt cấu hình CPU/RAM). Điều này giúp bảo mật cấu trúc bảng, giảm dung lượng gói tin truyền qua mạng và giúp phía Frontend dễ hiển thị."

---

### ❓ Câu 7: File `data_laptop.xlsx` và file `sync_excel.py` dùng để làm gì?
> **Trả lời**:  
> "Dạ thưa thầy/cô, đây là tính năng mở rộng tiện ích em xây dựng thêm. Trong thực tế, nhân viên quản lý kho thường quen dùng Excel. Em viết một script tự động đọc file Excel `data_laptop.xlsx` và đồng bộ trực tiếp vào cơ sở dữ liệu MySQL, giúp việc cập nhật giá bán hoặc thêm hàng loạt sản phẩm mới diễn ra chỉ trong vài giây."

---

### ❓ Câu 8: Nếu muốn thêm một thuộc tính mới cho sản phẩm (ví dụ: Màu sắc - Color), em sẽ làm những bước nào?
> **Trả lời**:  
> "Dạ, em sẽ làm theo 3 bước:  
> 1. Thêm thuộc tính `public string? Color { get; set; }` vào Model `ProductSpecification.cs` hoặc `Product.cs`.  
> 2. Chạy Migration của Entity Framework để cập nhật cột mới vào MySQL Database.  
> 3. Bổ sung trường `Color` vào `ProductApiDto` ở Backend và cập nhật giao diện hiển thị ở React."

---

## 💡 MẸO KHI THUYẾT TRÌNH BẢO VỆ
1. **Mở sẵn 2 màn hình**: Một bên là React Web Khách hàng (`http://localhost:3000`), một bên là Admin MVC (`http://localhost:5148/Product`).
2. **Thao tác mẫu trực tiếp**:
   - Bấm thử lọc hãng ASUS + CPU Core i7 + Màn hình 16 inch để giảng viên thấy kết quả lọc cực nhanh và mượt.
   - Thử bấm sang trang 2, trang 3 để chứng minh phân trang Server-side đang chạy chuẩn.
   - Mở Swagger (`http://localhost:5148/swagger`) để cho thầy cô thấy API được thiết kế chuẩn chỉ theo quy chuẩn công nghiệp.
3. **Khi thầy cô yêu cầu chỉ code**: Mở file `ProductsApiController.cs` hoặc `App.jsx`, các dòng comment tiếng Việt đã được đánh số thứ tự từ `[Bước 1]` đến `[Bước 9]`, bạn chỉ cần đọc theo các bước đó là tự tin đạt điểm tối đa!

# 💻 Hệ Thống Website Bán Laptop (Laptop Store)

Dự án website thương mại điện tử chuyên kinh doanh Laptop, xây dựng theo kiến trúc hiện đại tách biệt Frontend & Backend (Full-stack API).

> 📘 **Tài liệu kỹ thuật chi tiết dành cho người phát triển & người đọc**: Xem tại **[DOCUMENTATION.md](DOCUMENTATION.md)** (bao gồm sơ đồ kiến trúc, thiết kế ERD database, chi tiết các API endpoints và luồng xử lý).

- **Frontend (Giao diện khách hàng)**: React 19 + Vite, Lucide & Bootstrap Icons, giao diện Dark Tech & Modern FPT Shop style, giỏ hàng kết nối trực tiếp API Backend, thanh toán VietQR & COD.
- **Backend (Web API & Quản trị)**: ASP.NET Core (.NET 10 / .NET 8 LTS), Entity Framework Core (SQL Server), Clean MVC + Service Layer, phân quyền Cookie Authentication, bảo mật PBKDF2.
- **Admin Panel**: ASP.NET Core MVC (Đăng nhập bảo mật, Quản lý sản phẩm, Cấu hình RAM/SSD, Quản lý đơn hàng, Thống kê doanh thu thời gian thực).
- **Cơ sở dữ liệu**: Microsoft SQL Server (Sản phẩm, Hãng, Danh mục, Cấu hình chi tiết 1-1, Đơn hàng Orders, OrderItems, Payments, OrderStatusHistories).

---

## 📋 1. Yêu Cầu Cài Đặt (Prerequisites)

Trước khi chạy, máy tính của bạn cần có sẵn:
1. **[.NET 10 hoặc .NET 8 SDK](https://dotnet.microsoft.com/download)** (kiểm tra bằng lệnh `dotnet --version`).
2. **Cơ sở dữ liệu Microsoft SQL Server**:
   - Tải trực tiếp từ Microsoft: **[SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)** (khuyên dùng) hoặc bản Developer / LocalDB.
   - Ứng dụng đã tích hợp **cơ chế tự động dò tìm (Auto-Detection)**: Khi khởi chạy, hệ thống sẽ tự động thử kết nối lần lượt qua `.\SQLEXPRESS`, `(localdb)\MSSQLLocalDB`, `localhost`, `.` nên bạn **không cần cấu hình phức tạp hay dùng Docker**.
   - Chuỗi kết nối mặc định tại `LaptopStore/appsettings.json`:
     ```json
     "Server=.\\SQLEXPRESS;Database=LaptopStoreDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
     ```
3. *(Tùy chọn nếu dùng Frontend React)*: **[Node.js](https://nodejs.org/)** (v18+ hoặc v20+ LTS). Nếu không cài Node.js, bạn hoàn toàn có thể sử dụng toàn bộ tính năng bán hàng và quản trị qua giao diện Razor MVC thuần của dự án.

---

## 🚀 2. Hướng Dẫn Khởi Chạy

### Cách 1: Chạy tự động 1-Click (Khuyên dùng)
- Nhấp đúp chuột vào file **`run.bat`** (hoặc chạy **`run.ps1`** trong PowerShell).
- Script sẽ tự động nhận diện SQL Server, tự khởi tạo CSDL và mở hệ thống.

### Cách 2: Chạy bằng Visual Studio
1. Mở `LaptopStore.sln` bằng Visual Studio 2022 / 2025.
2. Chọn project khởi chạy là **LaptopStore** và nhấn **F5** (hoặc Ctrl + F5).

### Cách 3: Chạy bằng lệnh Dotnet CLI
```bash
dotnet run --project LaptopStore --launch-profile http
```

---

## 🌐 3. Địa Chỉ Truy Cập & Tài Khoản Kiểm Thử

| Giao diện | Đường dẫn URL | Mô tả & Tài khoản |
| :--- | :--- | :--- |
| 🛒 **Cửa Hàng Khách Hàng (Razor)** | [http://localhost:5148/Catalog](http://localhost:5148/Catalog) | Danh mục sản phẩm, bộ lọc, xem chi tiết, tra cứu đơn hàng |
| 🛠️ **Quản Trị Sản Phẩm (Admin MVC)** | [http://localhost:5148/Product](http://localhost:5148/Product) | Quản lý kho, thêm/sửa/xóa, quản lý ảnh và cấu hình |
| 🔐 **Đăng Nhập Quản Trị (Admin)** | [http://localhost:5148/Admin/Login](http://localhost:5148/Admin/Login) | Tài khoản: `admin` / `Admin@123` |
| 📦 **Trang Quản Trị Đơn Hàng** | [http://localhost:5148/Admin/Orders](http://localhost:5148/Admin/Orders) | Quản lý quy trình xử lý đơn hàng, đổi trạng thái |
| 💻 **Giao Diện Khách Hàng (React)** | [http://localhost:3000](http://localhost:3000) | Giao diện React SPA (chạy khi có Node.js) |

---

## 📁 4. Cấu Trúc Dự Án (Clean Architecture)

```text
├── LaptopStore/             # ASP.NET Core Backend & Admin MVC
│   ├── Controllers/         # MVC Controllers & Web API Controllers
│   ├── Services/            # Tầng Service nghiệp vụ (ProductService, OrderService)
│   ├── Data/                # EF Core AppDbContext & DbInitializer (Khởi tạo DB an toàn)
│   ├── Models/              # Entities (Product, Specification, Order, OrderItem, Payment,...)
│   ├── ViewModels/          # DTOs & ViewModels có Validation chặt chẽ
│   └── Views/               # Razor Views giao diện Quản trị Dark Tech
├── LaptopStore.Tests/       # Unit Tests (XUnit + InMemory Database)
├── frontend/                # React 19 SPA Khách Hàng (Vite proxy port 3000 -> 5148)
└── sql-init/                # Script SQL an toàn nạp dữ liệu mẫu ban đầu
```
```

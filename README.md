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
2. **[Node.js](https://nodejs.org/)** (v18+ hoặc v20+ LTS, kiểm tra bằng `node -v`).
3. **Cơ sở dữ liệu Microsoft SQL Server**:
   - Hỗ trợ **SQL Server (SQLEXPRESS)** hoặc **LocalDB**.
   - Cấu hình chuỗi kết nối tại `LaptopStore/appsettings.json` (mặc định: `Server=.\\SQLEXPRESS;Database=LaptopStoreDb;Trusted_Connection=True;TrustServerCertificate=True;`).
   - *(Tùy chọn)* Chạy bằng Docker:
     ```bash
     docker compose up -d mssql
     ```

---

## 🚀 2. Hướng Dẫn Khởi Chạy

### Cách 1: Chạy tự động 1-Click (Khuyên dùng)
- Nhấp đúp chuột vào file **`run.bat`** (hoặc chạy **`run.ps1`** trong PowerShell).

### Cách 2: Chạy bằng Visual Studio
1. Mở `LaptopStore.sln` bằng Visual Studio 2022 / 2025.
2. Chọn project khởi chạy là **LaptopStore** và nhấn **F5**.

### Cách 3: Chạy thủ công
```bash
# Terminal 1: React Frontend
cd frontend
npm install
npm run dev

# Terminal 2: ASP.NET Core Backend
cd LaptopStore
dotnet run --launch-profile http
```

---

## 🌐 3. Địa Chỉ Truy Cập & Tài Khoản Kiểm Thử

| Giao diện | Đường dẫn URL | Tài khoản đăng nhập |
| :--- | :--- | :--- |
| 🛒 **Web Khách Hàng (React)** | [http://localhost:3000](http://localhost:3000) | `khachhang` / `User@123` (hoặc đặt hàng trực tiếp) |
| 🛠️ **Trang Quản Trị (Admin)** | [http://localhost:5148/Admin/Login](http://localhost:5148/Admin/Login) | `admin` / `Admin@123` |
| 📦 **API Đơn Hàng (Backend)** | [http://localhost:5148/api/orders](http://localhost:5148/api/orders) | RESTful API tích hợp giỏ hàng và trừ kho |

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

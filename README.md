# 💻 Hệ Thống Website Bán Laptop (Laptop Store)

Dự án website thương mại điện tử chuyên kinh doanh Laptop, xây dựng theo kiến trúc hiện đại tách biệt Frontend & Backend (Full-stack API).

> 📘 **Tài liệu kỹ thuật chi tiết dành cho người phát triển & người đọc**: Xem tại **[DOCUMENTATION.md](DOCUMENTATION.md)** (bao gồm sơ đồ kiến trúc, thiết kế ERD database, chi tiết các API endpoints và luồng xử lý).

- **Frontend (Giao diện khách hàng)**: React 19 + Vite, Lucide Icons, Vanilla CSS cao cấp, bộ lọc đa năng (Hãng, Giá, CPU, Kích thước màn hình, Sắp xếp), phân trang mượt mà.
- **Backend (Web API & Quản trị)**: ASP.NET Core 8.0, Entity Framework Core, RESTful APIs, Swagger UI.
- **Admin Panel**: ASP.NET Core MVC (Quản lý sản phẩm, thương hiệu, danh mục, hình ảnh).
- **Cơ sở dữ liệu**: MySQL (Hơn 100 sản phẩm laptop thực tế kèm ảnh và cấu hình chi tiết).
- **Công cụ hỗ trợ**: Tự động đồng bộ dữ liệu trực tiếp từ file Excel (`frontend/public/data_laptop.xlsx`) vào Database.

---

## 📋 1. Yêu Cầu Cài Đặt (Prerequisites)

Trước khi chạy, máy tính của bạn cần có sẵn:
1. **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** trở lên (kiểm tra bằng lệnh `dotnet --version`).
2. **[Node.js](https://nodejs.org/)** (Khuyên dùng v18+ hoặc v20+ LTS, kiểm tra bằng `node -v`).
3. **Cơ sở dữ liệu MySQL**:
   - Chạy trên cổng mặc định `3306`.
   - Tài khoản mặc định: `root` / mật khẩu: `root` (Nếu mật khẩu khác, sửa tại file `LaptopStore/appsettings.json`).
   - *(Tùy chọn)* Nếu máy bạn đã cài **Docker Desktop**, bạn chỉ cần chạy lệnh sau mà không cần cài MySQL thủ công:
     ```bash
     docker compose up -d mysql
     ```

---

## 🚀 2. Hướng Dẫn Khởi Chạy Sau Khi Clone

### Bước 1: Clone mã nguồn về máy
```bash
git clone https://github.com/FMgSi/BTLWEB.git
cd BTLWEB
```

### Bước 2: Bật MySQL Server
Đảm bảo dịch vụ MySQL đang hoạt động trên máy bạn (hoặc bật qua Docker / XAMPP / Laragon / MySQL Service).
> 💡 **Ghi chú**: Bạn **không cần tạo bảng trước thủ công**! Lần đầu tiên Backend khởi chạy, hệ thống sẽ tự động tạo cơ sở dữ liệu `LaptopStoreDb` và nạp sẵn toàn bộ 100+ laptop mẫu từ file `sql-init/init-db.sql`.

### Bước 3: Chạy dự án

#### 👉 Cách 1: Chạy nhanh bằng 1-Click (Khuyên dùng trên Windows)
- Nhấp đúp chuột vào file **`run.bat`** (hoặc chạy file **`run.ps1`** trong PowerShell).
- Script sẽ:
  1. Tự động kiểm tra và chạy `npm install` nếu là lần đầu tiên bạn tải về.
  2. Bật React Frontend tại cổng `3000`.
  3. Bật ASP.NET Core Backend tại cổng `5148`.

#### 👉 Cách 2: Chạy bằng Visual Studio
1. Mở file solution `LaptopStore.sln` bằng **Visual Studio 2022**.
2. Tại thanh công cụ trên cùng, chọn cấu hình khởi chạy là **`LaptopStore`**.
3. Nhấn **F5** (hoặc nút **Run** hình tam giác xanh). Dự án sẽ tự động kích hoạt cả Backend lẫn Frontend.

#### 👉 Cách 3: Chạy thủ công bằng Terminal
Nếu bạn dùng Visual Studio Code hoặc Terminal:
```bash
# Terminal 1: Chạy Frontend
cd frontend
npm install
npm run dev

# Terminal 2: Chạy Backend
cd LaptopStore
dotnet run --launch-profile http
```

---

## 🌐 3. Địa Chỉ Truy Cập Ứng Dụng

Sau khi khởi chạy thành công, mở trình duyệt và truy cập các liên kết:

| Giao diện | Đường dẫn URL | Mô tả |
| :--- | :--- | :--- |
| 🛒 **Web Khách Hàng (React)** | [http://localhost:3000](http://localhost:3000) | Xem sản phẩm, tìm kiếm, lọc theo CPU / Hãng / Giá / Màn hình, phân trang |
| 🛠️ **Trang Quản Trị (Admin)** | [http://localhost:5148/Product](http://localhost:5148/Product) | Quản lý thêm/sửa/xóa sản phẩm, danh mục, upload ảnh |
| 📑 **Tài liệu API (Swagger)** | [http://localhost:5148/swagger](http://localhost:5148/swagger) | Xem danh sách các RESTful API endpoints |

---

## 📊 4. Đồng Bộ Dữ Liệu Từ File Excel

Nếu bạn chỉnh sửa giá bán, tên máy, cấu hình hoặc thêm sản phẩm trong file:
`frontend/public/data_laptop.xlsx`

Bạn chỉ cần nhấp đúp vào:
```bash
sync-excel.bat
```
*(hoặc chạy lệnh `python sync_excel.py`)* để toàn bộ dữ liệu từ Excel được cập nhật tự động vào MySQL Database!

---

## 📁 5. Cấu Trúc Thư Mục

```text
├── LaptopStore/             # Source code ASP.NET Core 8 Web API & MVC Admin
│   ├── Controllers/         # API Controllers & Admin MVC Controllers
│   ├── Data/                # EF Core DbContext & DbInitializer (Tự tạo DB)
│   ├── Models/              # Các Entity (Product, Brand, Category,...)
│   └── wwwroot/images/      # Hình ảnh sản phẩm lưu trữ nội bộ
├── frontend/                # Source code React 19 + Vite
│   ├── src/                 # Giao diện React, Components, Hooks
│   └── public/              # Ảnh sản phẩm và file Excel data_laptop.xlsx
├── sql-init/                # Script SQL tạo schema và 100+ laptop mẫu
├── run.bat                  # Script chạy đồng thời cả Frontend và Backend
├── sync-excel.bat           # Script đồng bộ dữ liệu Excel -> Database
└── LaptopStore.sln          # Solution Visual Studio
```

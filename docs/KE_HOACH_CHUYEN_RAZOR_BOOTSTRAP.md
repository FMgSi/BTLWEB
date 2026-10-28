# Đánh giá và kế hoạch chuyển React sang Razor Views + Bootstrap

Ngày đánh giá: 28/10/2026. Mốc mã nguồn: `ffb3e2c`.
Phạm vi: lập kế hoạch; chưa chuyển đổi mã ứng dụng, chưa thay đổi database.

## 1. Kết luận và phạm vi

Giữ ASP.NET Core MVC, SQL Server, EF Core, Models và các service. Thay giao diện khách hàng React bằng Razor Views; dùng Bootstrap 5.3, Bootstrap Icons và JavaScript nhỏ cho tương tác. Quản trị đã là Razor + Bootstrap nên tiếp tục sử dụng.

Không đổi DB provider, không viết lại project, không thêm microservices hoặc generic repository. Không coi việc chuyển giao diện là cách sửa transaction và phân quyền.

MVP gồm: trang chủ, danh sách/lọc/tìm kiếm/phân trang, chi tiết laptop, tài khoản, giỏ hàng, đặt hàng COD/VietQR, xem đơn, quản trị sản phẩm và xử lý đơn. Trang hướng dẫn/liên hệ chuyển sau luồng mua hàng. Không quảng bá voucher có hiệu lực nếu backend chưa áp dụng được.

## 2. Đánh giá bản hiện tại

### Đã cải thiện qua kiểm tra code

- AdminController đã bỏ nhánh mật khẩu cố định; controller quản trị có Authorize.
- ProductService và OrderService đã dùng CreateExecutionStrategy cho transaction.
- Đặt hàng SQL Server dùng ExecuteUpdate có điều kiện tồn kho, giảm nguy cơ bán vượt kho.
- Hủy đơn tuần tự bị chặn khi đơn đã hủy; đã cập nhật Payments khi đổi trạng thái.
- Dashboard tính từ đơn thật, không còn doanh thu mặc định 1,45 tỷ.
- Khóa khách hàng ghi vào Users.IsActive; thông tin khách đã dùng DB.
- Frontend dùng tổng tiền backend xác nhận, bỏ form khách tự đổi tài khoản nhận tiền.
- Voucher/settings chuyển từ static sang JSON trong App_Data; đã bền hơn qua restart nhưng chưa có tính nhất quán của DB.

### Vấn đề còn tồn tại cần đưa vào backlog

| Ưu tiên | Vấn đề và nơi kiểm tra | Việc cần làm |
|---|---|---|
| P1 | AuthController.Login/GetCurrentUser chưa kiểm tra Users.IsActive | Chặn đăng nhập tài khoản khóa; kiểm tra hiệu lực tài khoản ở các thao tác nhạy cảm và phiên hiện tại |
| P1 | User thêm PhoneNumber/IsActive nhưng script Users chưa có; vẫn EnsureCreated | Lập migration/nâng cấp có bảo toàn dữ liệu; kiểm tra schema thực tế trước khi cập nhật |
| P1 | OrderService.UpdateOrderStatusAsync đọc trạng thái rồi hoàn kho, không khóa việc chuyển trạng thái | Hai request hủy có thể cùng hoàn kho; dùng rowversion hoặc cập nhật trạng thái có điều kiện, hoàn kho cùng transaction |
| P1 | Chỉ chặn một số chuyển trạng thái, chưa có danh sách chuyển hợp lệ | Chặn trạng thái tùy ý và chuyển lùi; Hoàn thành phải là trạng thái kết thúc nếu chưa hỗ trợ trả hàng |
| P1 | sync_excel.py còn tạo DELETE, AUTO_INCREMENT và gọi mysql-local | Vô hiệu hóa đường chạy cũ; viết import SQL Server không phá dữ liệu và đưa Excel ra khỏi frontend/public |
| P2 | Catch mọi exception bên trong delegate execution strategy | Phân biệt lỗi nghiệp vụ và lỗi DB; để lỗi transient được strategy xử lý, log ở biên ngoài; kiểm tra retry và commit chưa rõ kết quả |
| P2 | POST đặt hàng chưa có khóa chống tạo trùng | Thêm idempotency key có unique constraint; gửi lại cùng yêu cầu phải nhận lại đơn cũ |
| P2 | Tra cứu đơn vẫn trả dữ liệu đã che khi chưa xác minh | Chỉ trả dữ liệu tối thiểu; tên một từ và điện thoại ngắn chưa được che; ưu tiên kiểm tra chủ đơn hoặc token khách vãng lai |
| P2 | SelectedSpecs được nhận trực tiếp từ client | Tạo snapshot cấu hình từ DB, client chỉ gửi ProductId và Quantity |
| P2 | Voucher JSON rỗng được thay bằng bộ mẫu; lỗi ghi bị nuốt | Không tự khôi phục voucher đã xóa; báo lỗi lưu; tách service, hoặc hoãn voucher khỏi MVP |
| P2 | Tài khoản nhận tiền vẫn hardcode trong JSX | Đưa vào cấu hình server có validation và một nguồn dùng chung |
| P2 | Thiếu CHECK constraints và giới hạn phân trang nhất quán | Ràng buộc giá/tồn kho/số lượng tại DB; page >= 1, pageSize có trần |

DB: mô hình sản phẩm nhìn chung phù hợp 3NF theo giả định mỗi Product là một cấu hình bán được. OrderItems.TotalPrice và Orders.TotalAmount là giá trị suy ra: nếu giữ phải ghi rõ phi chuẩn hóa có chủ đích và đảm bảo công thức. Giá/tên/địa chỉ tại lúc mua là snapshot lịch sử hợp lý. Chuyển Razor không yêu cầu đổi toàn bộ ERD.

MVC: đã có service dùng chung, phù hợp để chuyển. AdminController còn xử lý file JSON và truy vấn/thống kê; có thể tách dần. Service không nên phụ trách CSS badge. Không cần làm sạch mọi lớp mới bắt đầu trang catalog.

## 3. Quyết định kiến trúc

1. Một ứng dụng ASP.NET Core phục vụ cả khách hàng và quản trị; không cần Vite khi hoàn tất.
2. Razor controller gọi service trực tiếp, không gọi HTTP vòng về API của chính ứng dụng.
3. Giữ nguyên URL quản trị hiện tại trong đợt chuyển đầu để giảm rủi ro. Areas/Admin là đợt refactor tùy chọn sau nghiệm thu, không phải điều kiện MVC.
4. Giữ `_Layout.cshtml` hiện tại cho admin; tạo `_CustomerLayout.cshtml`. Đặt layout khách tường minh hoặc qua `_ViewStart` theo thư mục, không đổi global layout khiến trang admin bị ảnh hưởng.
5. Dùng chung cookie authentication với claims UserId/Role; giữ hai màn hình đăng nhập nếu cần. Session chỉ lưu giỏ hàng và dữ liệu tạm. Không dùng localStorage để chứng minh đăng nhập.
6. Giỏ hàng MVP lưu server Session: ProductId, Quantity, Selected. Giá/tồn kho/tên/ảnh được đọc từ DB. Session hết hạn/máy chủ restart có thể mất giỏ với memory cache: hiển thị rõ và chấp nhận cho MVP; DB cart là tính năng sau.
7. Cho phép guest checkout để giữ chức năng hiện tại. Đơn guest chỉ xem bằng token riêng khó đoán hoặc phiên đã tạo đơn; mã đơn ngắn không phải bí mật truy cập.
8. Form GET cho bộ lọc; form POST có antiforgery cho thao tác thay đổi. Sau POST thành công redirect tới GET (PRG). PRG không thay thế idempotency ở checkout.
9. JavaScript chỉ dùng gallery, toast, modal, nút số lượng hoặc fetch cập nhật từng phần. Toàn bộ luồng chính phải hoạt động với form server trước.
10. Giữ API trong giai đoạn chuyển để React vẫn chạy; chỉ xóa sau khi không còn consumer. Một bộ quy tắc nghiệp vụ cho cả API và MVC.

## 4. Cấu trúc file dự kiến

Các đường dẫn dưới đây tính từ thư mục repo.

```text
LaptopStore/
  Controllers/
    HomeController.cs                   # trang chủ khách, giữ Error/Privacy
    CatalogController.cs                # khác ProductController quản trị
    AccountController.cs
    CartController.cs
    CheckoutController.cs
    CustomerOrdersController.cs         # tránh nhầm đơn quản trị
  Services/
    IProductService.cs / ProductService.cs
    IOrderService.cs / OrderService.cs
    IAccountService.cs / AccountService.cs
    ICartService.cs / SessionCartService.cs
  ViewModels/Storefront/
    HomeViewModel.cs
    CatalogQuery.cs / CatalogViewModel.cs
    ProductDetailsViewModel.cs
    LoginViewModel.cs / RegisterViewModel.cs
    CartViewModel.cs / CheckoutViewModel.cs
    OrderDetailsViewModel.cs
  Views/
    Home/Index.cshtml
    Catalog/Index.cshtml / Details.cshtml
    Account/Login.cshtml / Register.cshtml
    Cart/Index.cshtml
    Checkout/Index.cshtml / Success.cshtml
    CustomerOrders/Index.cshtml / Details.cshtml / Lookup.cshtml
    Shared/_CustomerLayout.cshtml
    Shared/Storefront/_Header.cshtml / _Navigation.cshtml / _Footer.cshtml
    Shared/Storefront/_ProductCard.cshtml / _Filters.cshtml / _Pagination.cshtml
  wwwroot/
    css/storefront.css
    js/storefront.js / cart.js / product-gallery.js
    images/                             # nguồn ảnh dùng chung
    lib/bootstrap/                      # CSS và bundle cùng phiên bản 5.3 đã chốt
    lib/bootstrap-icons/
data/imports/data_laptop.xlsx            # không public mặc định
docs/                                   # hướng dẫn và checklist
```

Đặt tên DTO/service trung lập: có thể giữ GetProductsApiAsync lúc đầu, sau đó đổi thành SearchProductsAsync qua refactor có kiểm tra. Không sao chép bộ lọc vào CatalogController. CatalogQuery cần đủ hãng, danh mục/tag, CPU, RAM, màn hình, giá, sort, page; ProductFilterViewModel hiện chỉ có một phần.

## 5. Ánh xạ chức năng React

| Nguồn | Đích | Quy tắc chuyển |
|---|---|---|
| App.jsx, activeNav, selectedProductId | Route MVC và controller | Dùng URL thật, reload/back/bookmark hoạt động |
| TopHeader, Navbar | Partial header/navigation | Claims hiển thị tài khoản; số giỏ từ cart service |
| HomePage | Home/Index | Sản phẩm thật từ service, không fallback mock |
| ProductList, ProductCard | Catalog/Index + _ProductCard | Lặp Razor, URL chi tiết theo ProductId |
| FilterSection, CategorySidebar, ProductToolbar | _Filters + CatalogQuery | GET query string; đổi filter về trang 1 |
| Pagination | _Pagination | Giữ mọi tham số lọc khi đổi trang |
| ProductDetail | Catalog/Details | Gallery JS; cấu hình/giá/ảnh từ DB |
| AuthPage | Account views + AccountService | Validation server, antiforgery, cookie, local returnUrl |
| CartPage | Cart/Index + Checkout views | POST add/update/remove, tính giá server |
| SearchResultsPage | Catalog/Index?q=... | Một bộ lọc và một nguồn truy vấn |
| PromosPage | Trang chương trình hoặc catalog giảm giá | Chỉ công bố khuyến mãi có dữ liệu thật |
| GuidesPage, ContactPage, article section | Razor nội dung | Form liên hệ phải có xử lý thật hoặc ghi rõ thông tin liên hệ |
| mockData.js | Chỉ dữ liệu test/demo riêng | Không dùng khi lỗi DB hoặc kết quả rỗng |
| assets/hero.png, public/images | wwwroot/images | Đối chiếu ảnh trùng và ảnh thiếu trước khi chuyển |

## 6. Kế hoạch triển khai và tiêu chí nghiệm thu

Ước lượng 10–15 ngày làm việc cho nhóm 3 người nếu đã quen Razor; đây là dự toán theo phạm vi, không cam kết thời gian. Dành thêm thời gian nếu test/DB setup đang lỗi. Mỗi giai đoạn là một PR/commit có thể kiểm tra riêng.

### G0 — Chốt baseline và sửa nền tảng (ngày 1–3)

- Lưu mốc Git và backup DB; ghi nhận schema, số sản phẩm/đơn/users trước chuyển.
- Chạy build và tìm nguyên nhân test runner không kết thúc; không báo test pass chỉ vì build thành công.
- Lập baseline migration cho DB mới và phương án nâng cấp DB hiện có. Bổ sung Users.PhoneNumber/IsActive có default phù hợp; không xóa DB để tránh viết migration.
- Sửa khóa khách, hủy đồng thời, state machine và validation ở service (quantity dương, payment method hợp lệ, cấu hình từ DB).
- Thêm idempotency vào tạo đơn; unique key ở DB. Thiết kế retry không nuốt lỗi transient hoặc tạo đơn lặp khi client gửi lại.
- Khóa đường chạy import MySQL; xác định dữ liệu Excel cần giữ.
- Nghiệm thu: DB cũ còn dữ liệu; khách khóa không đăng nhập/đặt đơn; hai khách tranh máy cuối chỉ một đơn thành công; hủy đồng thời hoàn kho đúng một lần; retry POST không tạo đơn thứ hai.

### G1 — Layout và định tuyến khách (ngày 2–4, song song G0)

- Tạo _CustomerLayout, partial header/footer, storefront.css, script Bootstrap bundle.
- Chốt cùng một phiên bản CSS/JS Bootstrap; lưu local phục vụ buổi demo không có mạng. Không tải chồng bản local cũ và CDN mới.
- Để preview tại `/Store` trong giai đoạn đầu; giữ root đang vào quản trị. Không sửa Home/Error thành trang catalog.
- Tách selector CSS khách khỏi `.admin-layout`; giữ giao diện hiện tại làm tham chiếu.
- Nghiệm thu: desktop/mobile 360px không tràn ngang; menu dùng bàn phím được; admin không đổi layout.

### G2 — Catalog và chi tiết (ngày 3–6)

- Tạo CatalogController, CatalogQuery/ViewModel, Index/Details và partial card/filter/pagination.
- Dùng ProductService; thêm validation phân trang, thứ tự sort ổn định, giữ query string.
- Hiển thị trạng thái không kết quả, hết hàng, ngừng bán và lỗi tải dữ liệu; không tạo sản phẩm giả.
- Giá/ảnh/cấu hình thống nhất với admin; không tự sinh phần trăm giảm giá hoặc cấu hình nâng cấp.
- Nghiệm thu: lọc nhiều điều kiện, đổi trang giữ bộ lọc, URL reload/back đúng, sản phẩm ngừng bán không mua được, dữ liệu sửa bên admin xuất hiện bên khách.

### G3 — Tài khoản (ngày 3–6)

- Tách AccountService dùng chung cho MVC và API còn tồn tại.
- Login/Register ViewModels; dùng validation server; kiểm tra tài khoản active, unique email/username và xử lý race tạo tài khoản.
- Tạo claims từ DB, đăng xuất bằng POST có antiforgery, chỉ redirect returnUrl nội bộ.
- Account/Login là đường đăng nhập chung; Admin/Login có thể chuyển tiếp/giữ trang riêng nhưng cùng service. Customer vào admin phải bị từ chối.
- Đổi cách đọc identity trong đơn hàng từ Session sang claims; trong thời gian chuyển có adapter rõ ràng, không hai nguồn quyền mâu thuẫn.
- Nghiệm thu: sai mật khẩu thất bại, khóa có hiệu lực với phiên đang mở ở thao tác nhạy cảm, refresh giữ đăng nhập, logout không còn quyền, form lỗi giữ dữ liệu không nhạy cảm.

### G4 — Giỏ hàng (ngày 5–8)

- SessionCartService quản lý ProductId/Quantity/Selected; gộp dòng cùng sản phẩm, giới hạn số lượng và kiểm tra sản phẩm hợp lệ.
- CartController POST Add/Update/Remove/Select/Clear có antiforgery; GET render giá hiện tại.
- Giữ món chưa chọn sau checkout; báo thay đổi giá hoặc hết hàng. Badge giỏ thống nhất trên mọi trang.
- Giỏ localStorage cũ ở port 3000 không tự chuyển sang port backend vì khác origin. Với dữ liệu demo: thông báo tạo lại giỏ; nếu bắt buộc giữ, làm export/import chỉ ProductId/Quantity có kiểm tra server.
- Nghiệm thu: thêm từ listing/detail, sửa số lượng, xóa, chọn một phần, hết session, đăng nhập khi có giỏ; request sửa giá client không ảnh hưởng tổng server.

### G5 — Đặt hàng và xem đơn (ngày 7–10)

- Checkout GET hiển thị tổng server, POST nhận thông tin giao nhận + lựa chọn thanh toán + idempotency key; không tin giá hoặc UserId client.
- Gọi OrderService để tạo đơn và cập nhật kho trong transaction; chỉ xóa món đã đặt sau thành công.
- Sau POST redirect tới trang success/chi tiết được bảo vệ. Refresh trang không đặt lại.
- VietQR lấy account và amount từ server; nút khách báo đã chuyển tiền chỉ là thông báo chờ xác minh, không tự ghi Paid.
- Tạo lịch sử đơn của tài khoản, trang chi tiết kiểm tra chủ đơn/admin; thiết kế token guest và rate limit tra cứu nếu mở public.
- Tách thanh toán và trạng thái giao hàng. Hủy đơn đã nhận tiền phải có luồng hoàn tiền riêng, không đổi giao dịch thành Hủy rồi coi như tiền đã hoàn.
- Nghiệm thu: COD/VietQR, giá đổi lúc checkout, tồn kho thiếu, lỗi DB, gửi hai lần, user A không đọc đơn user B, admin nhìn và xử lý được đơn mới.

### G6 — Trang phụ và tích hợp quản trị (ngày 9–12)

- Chuyển guides/contact/promos đúng phạm vi. Không chuyển tính năng demo giả thành chức năng được quảng cáo là thật.
- Test đơn khách → admin xác nhận/giao/hoàn tất/hủy → kho và payment/history thống nhất.
- Quy định doanh thu theo ngày tạo hay ngày thanh toán và múi giờ cửa hàng; chỉnh dashboard phù hợp định nghĩa.
- Settings qua service có báo lỗi lưu; bank config lấy cùng nguồn với checkout. Voucher nếu ngoài MVP thì ẩn hành động áp dụng, không tạo giảm giá giả.
- Nghiệm thu: một kịch bản demo trọn vẹn, layout riêng, không mất tính năng quản trị.

### G7 — Chuyển root và gỡ React (ngày 11–15)

- Chuyển `/` về Home/Index khách; giữ `/Product`, `/Admin`, `/Brands`, `/Categories` trong đợt này. Kiểm tra mọi link và cookie login path.
- Chuyển đủ assets/Excel trước khi bỏ frontend. Viết lại hoặc loại bỏ có chủ đích sync_excel.py/sync-excel.bat; không để ghi đè SQL Server bằng SQL MySQL.
- Bỏ auto-launch Vite và khối kill process LaptopStore khác trong Program.cs; sửa run.bat/run.ps1 chỉ chạy backend.
- Bỏ CORS cho React nếu không còn consumer khác. Xóa React/Vite/package files sau khi đã đối chiếu toàn bộ feature checklist.
- Rà API: giữ endpoint có consumer rõ ràng, còn lại gỡ sau cùng. Razor không phụ thuộc các endpoint đã gỡ.
- Cập nhật README/DOCUMENTATION/Docker: một origin, một bước chạy; App_Data/uploads cần volume/quyền ghi nếu còn dùng file JSON.
- Smoke test từ clone sạch bằng .NET + SQL Server, không cài Node. Không buộc máy giảng viên truy cập CDN để render layout.
- Nghiệm thu: không request port 3000, không tham chiếu frontend, mọi luồng bắt buộc pass; dữ liệu cũ còn nguyên.

## 7. Phân công nhóm ba người

| Thành viên | Sở hữu chính | Phối hợp |
|---|---|---|
| A | DB/migrations, authentication, OrderService/concurrency/idempotency | Chốt service contracts với B/C trước khi làm form |
| B | Customer layout, catalog, detail, filter, assets, nội dung | Gọi ProductService; không tự viết lại quy tắc giá |
| C | Cart, checkout, order history, kiểm thử tích hợp, tài liệu chạy | Dùng contract OrderService của A và layout của B |

Một người phụ trách merge Program.cs/_ViewStart để tránh xung đột. Mỗi ngày tích hợp một lần. G1 có thể chạy song song G0; G5 phụ thuộc nền tảng G0, identity G3 và cart G4. Không chia theo kiểu mỗi người làm một bản DB riêng.

## 8. Bộ kiểm thử bắt buộc

- Unit: quy tắc trạng thái; tính tiền; validation; password; quyền theo vai trò.
- SQL Server integration: migration trên DB có dữ liệu, khóa ngoại, transaction rollback khi dòng thứ hai hết hàng, cạnh tranh máy cuối, hủy đồng thời, unique/idempotency.
- HTTP integration: cookie login/logout, antiforgery, customer không vào admin, ownership đơn, tài khoản khóa.
- UI/manual: 360px và desktop, bàn phím/labels/errors, lọc/phân trang/refresh/back, upload ảnh hiển thị bên khách, COD/VietQR, khởi động lại.
- Không dùng InMemory để kết luận transaction/concurrency hoạt động: service hiện có nhánh riêng cho InMemory, khác nhánh SQL thật.
- Lệnh baseline: `dotnet build LaptopStore.sln`; `dotnet test LaptopStore.Tests/LaptopStore.Tests.csproj`; trong giai đoạn còn React chạy build/lint khi thay file React.

## 9. Cutover và quay lại

Giữ React chạy được trong G1–G6. Chỉ đổi root/gỡ launcher sau khi checklist mua hàng và admin đạt. Mỗi giai đoạn commit riêng; nếu lỗi giao diện thì revert commit cutover, không restore DB bằng cách xóa. Migration cần phương án phục hồi và backup được kiểm tra. API cũ giữ tới khi không còn người dùng/consumer chuyển tiếp.

Không cần đưa giỏ hàng hoặc voucher vào DB chỉ vì chuyển Bootstrap. Không xóa SQL, Models hoặc Services khi xóa frontend. Areas/Admin để PR sau nếu thời gian cho phép.

## 10. Tài liệu chính thức tham chiếu

- Bootstrap CSS/JS dùng được không cần build frontend: https://getbootstrap.com/docs/5.3/
- Form Tag Helpers và antiforgery: https://learn.microsoft.com/aspnet/core/mvc/views/working-with-forms
- Areas và ViewImports: https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/areas

## 11. Kết quả kiểm tra ở lần đánh giá này

- React production build thành công.
- ESLint: 34 lỗi, giảm từ 40 ở lần đánh giá trước.
- Backend và test project biên dịch được. Lệnh test với `--blame-hang-timeout 30s` bị abort: vstest không kết nối được datacollector sau 90 giây. Đây là lỗi hạ tầng chạy test, chưa phải bằng chứng test nghiệp vụ thất bại hay thành công.
- Chưa chạy ứng dụng để kiểm chứng trực tiếp SQL Server; các lỗi được nêu dựa trên code và quan hệ cấu hình.

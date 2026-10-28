import os
import sys
import io
import shutil
import zipfile
import subprocess
import xml.etree.ElementTree as ET

# Đảm bảo xuất UTF-8 trên Windows
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
# Ưu tiên đọc từ data/imports/data_laptop.xlsx, dự phòng frontend/public/data_laptop.xlsx
XLSX_PATHS = [
    os.path.join(BASE_DIR, 'data', 'imports', 'data_laptop.xlsx'),
    os.path.join(BASE_DIR, 'frontend', 'public', 'data_laptop.xlsx')
]
XLSX_PATH = next((p for p in XLSX_PATHS if os.path.exists(p)), None)
WWWROOT_PRODUCTS = os.path.join(BASE_DIR, 'LaptopStore', 'wwwroot', 'images', 'products')
WWWROOT_BRANDS = os.path.join(BASE_DIR, 'LaptopStore', 'wwwroot', 'images', 'brands')
REACT_PRODUCTS = os.path.join(BASE_DIR, 'frontend', 'public', 'images', 'products')
REACT_BRANDS = os.path.join(BASE_DIR, 'frontend', 'public', 'images', 'brands')
SQL_FILE = os.path.join(BASE_DIR, 'sql-init', 'import-products.sql')

if not XLSX_PATH:
    print(f"❌ Không tìm thấy file Excel tại: {XLSX_PATHS}")
    sys.exit(1)

print(f"📊 Đang đọc file Excel: {XLSX_PATH} ...")
z = zipfile.ZipFile(XLSX_PATH)

sst = []
if 'xl/sharedStrings.xml' in z.namelist():
    sst_tree = ET.fromstring(z.read('xl/sharedStrings.xml'))
    for si in sst_tree.iter('{http://schemas.openxmlformats.org/spreadsheetml/2006/main}si'):
        text = ''.join(t.text for t in si.iter('{http://schemas.openxmlformats.org/spreadsheetml/2006/main}t') if t.text)
        sst.append(text)

def get_sheet_rows(sheet_path):
    tree = ET.fromstring(z.read(sheet_path))
    rows = []
    for r in tree.iter('{http://schemas.openxmlformats.org/spreadsheetml/2006/main}row'):
        row_vals = []
        for c in r.iter('{http://schemas.openxmlformats.org/spreadsheetml/2006/main}c'):
            t = c.attrib.get('t')
            v = c.find('{http://schemas.openxmlformats.org/spreadsheetml/2006/main}v')
            val = v.text if v is not None else ''
            if t == 's' and val.isdigit():
                val = sst[int(val)]
            row_vals.append(val)
        if any(row_vals):
            rows.append(row_vals)
    return rows

raw_brands = get_sheet_rows('xl/worksheets/sheet2.xml')
raw_cats = get_sheet_rows('xl/worksheets/sheet3.xml')
raw_prods = get_sheet_rows('xl/worksheets/sheet4.xml')
raw_specs = get_sheet_rows('xl/worksheets/sheet5.xml')

# Phân tích Brands
brands_list = []
for r in raw_brands[1:]:
    if len(r) >= 3 and r[0]:
        try:
            b_id = int(float(r[0]))
            name = r[1].strip()
            logo = r[2].strip() if len(r) > 2 else f"/images/brands/{name.lower()}.png"
            desc = r[4].strip() if len(r) > 4 else ""
            brands_list.append({'id': b_id, 'name': name, 'logo': logo, 'desc': desc})
        except:
            pass

# Phân tích Categories
categories_list = []
for r in raw_cats[1:]:
    if len(r) >= 2 and r[0]:
        try:
            c_id = int(float(r[0]))
            name = r[1].strip()
            desc = r[2].strip() if len(r) > 2 else ""
            categories_list.append({'id': c_id, 'name': name, 'desc': desc})
        except:
            pass

# Phân tích Specs
specs_dict = {}
for r in raw_specs[1:]:
    if len(r) >= 11 and r[0]:
        try:
            p_id = int(float(r[0]))
            specs_dict[p_id] = {
                'cpu': r[1].strip(),
                'ram': int(float(r[2])) if r[2] else 16,
                'storage': int(float(r[3])) if r[3] else 512,
                'storageType': r[4].strip(),
                'gpu': r[5].strip(),
                'screen': float(r[6]) if r[6] else 15.6,
                'refresh': int(float(r[7])) if r[7] else 60,
                'weight': float(r[8]) if r[8] else 1.8,
                'battery': int(float(r[9])) if r[9] else 50,
                'os': r[10].strip()
            }
        except:
            pass

# Phân tích Products (tất cả các dòng hợp lệ trong file Excel)
products_list = []
for r in raw_prods[1:]:
    if len(r) >= 6 and r[0]:
        try:
            p_id = int(float(r[0]))
            name = r[1].strip()
            b_id = int(float(r[2]))
            c_id = int(float(r[3]))
            price = float(r[4])
            disc = float(r[5]) if r[5] else None
            stock = int(float(r[6])) if len(r) > 6 and r[6] else 20
            thumb = r[7].strip() if len(r) > 7 and r[7] else f"/images/products/laptop-{p_id}.png"
            desc = r[10].strip() if len(r) > 10 else ""

            products_list.append({
                'id': p_id,
                'name': name,
                'brand_id': b_id,
                'category_id': c_id,
                'price': price,
                'discount_price': disc,
                'stock': stock,
                'thumbnail': thumb,
                'desc': desc,
                'spec': specs_dict.get(p_id, {
                    'cpu': 'Intel Core i5', 'ram': 16, 'storage': 512,
                    'storageType': 'SSD NVMe', 'gpu': 'Intel Iris Xe',
                    'screen': 15.6, 'refresh': 60, 'weight': 1.8,
                    'battery': 50, 'os': 'Windows 11'
                })
            })
        except:
            pass

print(f"-> Tìm thấy {len(brands_list)} Hãng, {len(categories_list)} Danh mục và {len(products_list)} Sản phẩm trong Excel.")

# Kiểm tra & đảm bảo ảnh sản phẩm tồn tại
for d in [WWWROOT_PRODUCTS, REACT_PRODUCTS]:
    os.makedirs(d, exist_ok=True)

existing_images = [f for f in os.listdir(WWWROOT_PRODUCTS) if f.endswith(('.png', '.jpg', '.webp'))]
fallback_image = os.path.join(WWWROOT_PRODUCTS, existing_images[0]) if existing_images else None

copied_images = 0
for p in products_list:
    img_name = os.path.basename(p['thumbnail'])
    dst1 = os.path.join(WWWROOT_PRODUCTS, img_name)
    dst2 = os.path.join(REACT_PRODUCTS, img_name)
    if not os.path.exists(dst1) and fallback_image:
        shutil.copyfile(fallback_image, dst1)
        copied_images += 1
    if not os.path.exists(dst2) and os.path.exists(dst1):
        shutil.copyfile(dst1, dst2)

if copied_images > 0:
    print(f"-> Đã tự động tạo {copied_images} ảnh placeholder cho các sản phẩm mới thêm trong Excel.")

# Tạo file SQL Server (KHÔNG xóa bảng, KHÔNG dùng cú pháp MySQL)
sql_lines = [
    "-- =========================================================================",
    "-- Tự động sinh từ Excel bằng sync_excel.py cho Microsoft SQL Server",
    "-- Thực thi an toàn: Sử dụng IF NOT EXISTS để nạp bổ sung, KHÔNG xóa dữ liệu",
    "-- =========================================================================",
    "USE LaptopStoreDb;",
    "GO",
    "",
    "-- 1. Brands",
    "SET IDENTITY_INSERT Brands ON;",
    "GO"
]

def esc(val):
    return str(val or '').replace("'", "''")

for b in brands_list:
    sql_lines.append(f"IF NOT EXISTS (SELECT 1 FROM Brands WHERE BrandId = {b['id']})")
    sql_lines.append(f"    INSERT INTO Brands (BrandId, BrandName, LogoUrl, Description) VALUES ({b['id']}, N'{esc(b['name'])}', N'{esc(b['logo'])}', N'{esc(b['desc'])}');")

sql_lines.append("GO")
sql_lines.append("SET IDENTITY_INSERT Brands OFF;")
sql_lines.append("GO")
sql_lines.append("")

sql_lines.append("-- 2. Categories")
sql_lines.append("SET IDENTITY_INSERT Categories ON;")
sql_lines.append("GO")
for c in categories_list:
    sql_lines.append(f"IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryId = {c['id']})")
    sql_lines.append(f"    INSERT INTO Categories (CategoryId, CategoryName, Description) VALUES ({c['id']}, N'{esc(c['name'])}', N'{esc(c['desc'])}');")

sql_lines.append("GO")
sql_lines.append("SET IDENTITY_INSERT Categories OFF;")
sql_lines.append("GO")
sql_lines.append("")

sql_lines.append("-- 3. Products & Specifications")
sql_lines.append("SET IDENTITY_INSERT Products ON;")
sql_lines.append("GO")
for p in products_list:
    d_p = f"{p['discount_price']:.2f}" if p['discount_price'] else "NULL"
    sql_lines.append(f"IF NOT EXISTS (SELECT 1 FROM Products WHERE ProductId = {p['id']})")
    sql_lines.append(f"    INSERT INTO Products (ProductId, ProductName, BrandId, CategoryId, Price, DiscountPrice, StockQuantity, ThumbnailUrl, Description, IsActive) VALUES ({p['id']}, N'{esc(p['name'])}', {p['brand_id']}, {p['category_id']}, {p['price']:.2f}, {d_p}, {p['stock']}, N'{esc(p['thumbnail'])}', N'{esc(p['desc'])}', 1);")

sql_lines.append("GO")
sql_lines.append("SET IDENTITY_INSERT Products OFF;")
sql_lines.append("GO")
sql_lines.append("")

sql_lines.append("-- 4. ProductSpecifications")
for p in products_list:
    s = p['spec']
    sql_lines.append(f"IF NOT EXISTS (SELECT 1 FROM ProductSpecifications WHERE ProductId = {p['id']})")
    sql_lines.append(f"    INSERT INTO ProductSpecifications (ProductId, CPU, RamGB, StorageGB, StorageType, GPU, ScreenSizeInch, RefreshRateHz, WeightKg, BatteryWh, OS) VALUES ({p['id']}, N'{esc(s['cpu'])}', {s['ram']}, {s['storage']}, N'{esc(s['storageType'])}', N'{esc(s['gpu'])}', {s['screen']}, {s['refresh']}, {s['weight']}, {s['battery']}, N'{esc(s['os'])}');")

sql_lines.append("GO")

sql_content = "\n".join(sql_lines)
with open(SQL_FILE, 'w', encoding='utf-8') as f:
    f.write(sql_content)

print(f"✔ Đã tạo file script SQL Server an toàn: {SQL_FILE}")
print(f"🎉 THÀNH CÔNG! Đã xuất {len(products_list)} sản phẩm từ Excel thành script SQL Server.")
print("Script sử dụng IF NOT EXISTS và bảo toàn toàn bộ dữ liệu đơn hàng, thanh toán và tài khoản hiện tại.")

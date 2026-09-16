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
XLSX_PATH = os.path.join(BASE_DIR, 'frontend', 'public', 'data_laptop.xlsx')
WWWROOT_PRODUCTS = os.path.join(BASE_DIR, 'LaptopStore', 'wwwroot', 'images', 'products')
WWWROOT_BRANDS = os.path.join(BASE_DIR, 'LaptopStore', 'wwwroot', 'images', 'brands')
REACT_PRODUCTS = os.path.join(BASE_DIR, 'frontend', 'public', 'images', 'products')
REACT_BRANDS = os.path.join(BASE_DIR, 'frontend', 'public', 'images', 'brands')
SQL_FILE = os.path.join(BASE_DIR, 'sql-init', 'init-db.sql')

if not os.path.exists(XLSX_PATH):
    print(f"❌ Không tìm thấy file: {XLSX_PATH}")
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

# Tạo file SQL
sql_lines = [
    "-- Tự động sinh từ Excel bằng sync_excel.py",
    "USE LaptopStoreDb;",
    "",
    "SET FOREIGN_KEY_CHECKS = 0;",
    "TRUNCATE TABLE ProductImages;",
    "TRUNCATE TABLE ProductSpecifications;",
    "TRUNCATE TABLE Products;",
    "TRUNCATE TABLE Categories;",
    "TRUNCATE TABLE Brands;",
    "SET FOREIGN_KEY_CHECKS = 1;",
    "",
    "-- 1. Brands",
    "INSERT INTO Brands (BrandId, BrandName, LogoUrl, Description) VALUES"
]

def esc(val):
    return str(val or '').replace("'", "''")

b_vals = [f"({b['id']}, '{esc(b['name'])}', '{esc(b['logo'])}', '{esc(b['desc'])}')" for b in brands_list]
sql_lines.append(",\n".join(b_vals) + ";\n")

sql_lines.append("-- 2. Categories")
sql_lines.append("INSERT INTO Categories (CategoryId, CategoryName, Description) VALUES")
c_vals = [f"({c['id']}, '{esc(c['name'])}', '{esc(c['desc'])}')" for c in categories_list]
sql_lines.append(",\n".join(c_vals) + ";\n")

sql_lines.append("-- 3. Products")
sql_lines.append("INSERT INTO Products (ProductId, ProductName, BrandId, CategoryId, Price, DiscountPrice, StockQuantity, ThumbnailUrl, Description, IsActive) VALUES")
p_vals = []
for p in products_list:
    d_p = f"{p['discount_price']:.2f}" if p['discount_price'] else "NULL"
    p_vals.append(f"({p['id']}, '{esc(p['name'])}', {p['brand_id']}, {p['category_id']}, {p['price']:.2f}, {d_p}, {p['stock']}, '{esc(p['thumbnail'])}', '{esc(p['desc'])}', 1)")
sql_lines.append(",\n".join(p_vals) + ";\n")

sql_lines.append("-- 4. ProductSpecifications")
sql_lines.append("INSERT INTO ProductSpecifications (ProductId, CPU, RamGB, StorageGB, StorageType, GPU, ScreenSizeInch, RefreshRateHz, WeightKg, BatteryWh, OS) VALUES")
s_vals = []
for p in products_list:
    s = p['spec']
    s_vals.append(f"({p['id']}, '{esc(s['cpu'])}', {s['ram']}, {s['storage']}, '{esc(s['storageType'])}', '{esc(s['gpu'])}', {s['screen']}, {s['refresh']}, {s['weight']}, {s['battery']}, '{esc(s['os'])}')")
sql_lines.append(",\n".join(s_vals) + ";\n")

sql_content = "\n".join(sql_lines)
with open(SQL_FILE, 'w', encoding='utf-8') as f:
    f.write(sql_content)

print(f"✔ Đã cập nhật file: {SQL_FILE}")

# Nạp vào MySQL qua Docker container mysql-local
print("🔄 Đang cập nhật vào MySQL database LaptopStoreDb ...")
try:
    cmd = 'docker exec -i mysql-local mysql -u root -proot --default-character-set=utf8mb4'
    proc = subprocess.Popen(cmd, shell=True, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    stdout, stderr = proc.communicate(input=sql_content.encode('utf-8'))
    if proc.returncode == 0:
        print(f"🎉 THÀNH CÔNG! Đã đồng bộ {len(products_list)} sản phẩm từ Excel vào MySQL Database!")
    else:
        print(f"⚠️ Không thể chạy qua docker exec: {stderr.decode('utf-8', errors='ignore')}")
        print("Bạn có thể nạp thủ công file sql-init/init-db.sql vào MySQL.")
except Exception as ex:
    print(f"Lỗi kết nối MySQL: {ex}")

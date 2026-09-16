import os
import sys
import io
import re
import shutil
import zipfile
import urllib.request
import xml.etree.ElementTree as ET

# Đảm bảo in tiếng Việt UTF-8
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
XLSX_PATH = os.path.join(BASE_DIR, 'frontend', 'public', 'data_laptop.xlsx')
WWWROOT_PRODUCTS = os.path.join(BASE_DIR, 'LaptopStore', 'wwwroot', 'images', 'products')
WWWROOT_BRANDS = os.path.join(BASE_DIR, 'LaptopStore', 'wwwroot', 'images', 'brands')
REACT_PRODUCTS = os.path.join(BASE_DIR, 'frontend', 'public', 'images', 'products')
REACT_BRANDS = os.path.join(BASE_DIR, 'frontend', 'public', 'images', 'brands')

for d in [WWWROOT_PRODUCTS, WWWROOT_BRANDS, REACT_PRODUCTS, REACT_BRANDS]:
    os.makedirs(d, exist_ok=True)

print("1. Đang đọc dữ liệu từ data_laptop.xlsx...")
z = zipfile.ZipFile(XLSX_PATH)

sst = []
if 'xl/sharedStrings.xml' in z.namelist():
    sst_tree = ET.fromstring(z.read('xl/sharedStrings.xml'))
    for si in sst_tree.iter('{http://schemas.openxmlformats.org/spreadsheetml/2006/main}si'):
        text = ''.join(t.text for t in si.iter('{http://schemas.openxmlformats.org/spreadsheetml/2006/main}t') if t.text)
        sst.append(text)

def get_sheet_rows(sheet_name):
    tree = ET.fromstring(z.read(sheet_name))
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
    if len(r) >= 5 and r[0]:
        b_id = int(float(r[0]))
        name = r[1].strip()
        logo = r[2].strip()
        country = r[3].strip()
        desc = r[4].strip()
        brands_list.append({
            'id': b_id,
            'name': name,
            'logo': logo,
            'country': country,
            'desc': desc
        })

# Phân tích Categories
categories_list = []
for r in raw_cats[1:]:
    if len(r) >= 3 and r[0]:
        c_id = int(float(r[0]))
        name = r[1].strip()
        desc = r[2].strip()
        categories_list.append({
            'id': c_id,
            'name': name,
            'desc': desc
        })

# Phân tích Specs dạng dict theo ProductId
specs_dict = {}
for r in raw_specs[1:]:
    if len(r) >= 11 and r[0]:
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

# Chọn lọc đúng 100 laptop đa dạng phân bổ đều qua các hãng
# ASUS: 12, Dell: 12, Lenovo: 12, Apple: 11, Acer: 11, HP: 11, MSI: 11, LG: 8, Gigabyte: 6, Microsoft: 6
brand_target_counts = {
    1: 12,  # ASUS
    2: 12,  # Dell
    3: 12,  # Lenovo
    4: 11,  # Apple
    5: 11,  # Acer
    6: 11,  # HP
    7: 11,  # MSI
    8: 8,   # LG
    9: 6,   # Gigabyte
    10: 6   # Microsoft
}

brand_current_counts = {k: 0 for k in brand_target_counts}
selected_products = []

for r in raw_prods[1:]:
    if len(r) >= 12 and r[0]:
        old_p_id = int(float(r[0]))
        name = r[1].strip()
        b_id = int(float(r[2]))
        c_id = int(float(r[3]))
        price = float(r[4])
        disc_price = float(r[5]) if r[5] else None
        stock = int(float(r[6])) if r[6] else 20
        thumb = r[7].strip()
        desc = r[10].strip()

        if b_id in brand_target_counts and brand_current_counts[b_id] < brand_target_counts[b_id]:
            brand_current_counts[b_id] += 1
            selected_products.append({
                'old_id': old_p_id,
                'name': name,
                'brand_id': b_id,
                'category_id': c_id,
                'price': price,
                'discount_price': disc_price,
                'stock': stock,
                'thumbnail': thumb,
                'desc': desc,
                'spec': specs_dict.get(old_p_id, {})
            })

print(f"-> Đã chọn lọc: {len(selected_products)} laptop trên 10 thương hiệu lớn.")
for b_id, count in sorted(brand_current_counts.items()):
    b_name = next(b['name'] for b in brands_list if b['id'] == b_id)
    print(f"   + {b_name} (ID {b_id}): {count} sản phẩm")

# Gán lại ID từ 1 đến 100
for new_id, p in enumerate(selected_products, start=1):
    p['id'] = new_id

print("\n2. Đang tải và chuẩn bị kho ảnh laptop đại diện theo hãng & dòng máy...")
# 15 ảnh laptop thực tế chất lượng cao từ CDN Unsplash
BASE_IMAGE_URLS = {
    'asus_gaming': 'https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=600&q=80',
    'asus_office': 'https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=600&q=80',
    'dell_gaming': 'https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=600&q=80',
    'dell_office': 'https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=600&q=80',
    'lenovo_gaming': 'https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=600&q=80',
    'lenovo_office': 'https://images.unsplash.com/photo-1587614382346-4ec70e388b28?w=600&q=80',
    'apple_pro': 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=600&q=80',
    'apple_air': 'https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=600&q=80',
    'acer_gaming': 'https://images.unsplash.com/photo-1542751371-adc38448a05e?w=600&q=80',
    'acer_office': 'https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=600&q=80',
    'hp_gaming': 'https://images.unsplash.com/photo-1629654297299-c8506221ca97?w=600&q=80',
    'hp_office': 'https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=600&q=80',
    'msi_gaming': 'https://images.unsplash.com/photo-1569770218135-bea267ed7e84?w=600&q=80',
    'msi_office': 'https://images.unsplash.com/photo-1531297484001-80022131f5a1?w=600&q=80',
    'lg_gram': 'https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=600&q=80',
    'gigabyte': 'https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=600&q=80',
    'microsoft': 'https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=600&q=80'
}

cached_images = {}
CACHE_DIR = os.path.join(BASE_DIR, 'scratch_images')
os.makedirs(CACHE_DIR, exist_ok=True)

for key, url in BASE_IMAGE_URLS.items():
    cache_file = os.path.join(CACHE_DIR, f"{key}.jpg")
    if not os.path.exists(cache_file) or os.path.getsize(cache_file) == 0:
        try:
            req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
            with urllib.request.urlopen(req, timeout=10) as resp, open(cache_file, 'wb') as f:
                f.write(resp.read())
        except Exception as ex:
            print(f"Lỗi tải {key}: {ex}")
    cached_images[key] = cache_file

print(f"-> Đã chuẩn bị {len(cached_images)} ảnh mẫu gốc chất lượng cao.")

# Hàm chọn ảnh mẫu theo hãng và danh mục sản phẩm
def pick_base_image(b_id, c_id, name):
    name_l = name.lower()
    if b_id == 1: # ASUS
        return 'asus_gaming' if (c_id == 1 or 'tuf' in name_l or 'rog' in name_l) else 'asus_office'
    elif b_id == 2: # Dell
        return 'dell_gaming' if (c_id == 1 or 'alienware' in name_l or 'g15' in name_l or 'g16' in name_l) else 'dell_office'
    elif b_id == 3: # Lenovo
        return 'lenovo_gaming' if (c_id == 1 or 'legion' in name_l or 'loq' in name_l) else 'lenovo_office'
    elif b_id == 4: # Apple
        return 'apple_pro' if 'pro' in name_l else 'apple_air'
    elif b_id == 5: # Acer
        return 'acer_gaming' if (c_id == 1 or 'nitro' in name_l or 'predator' in name_l) else 'acer_office'
    elif b_id == 6: # HP
        return 'hp_gaming' if (c_id == 1 or 'victus' in name_l or 'omen' in name_l) else 'hp_office'
    elif b_id == 7: # MSI
        return 'msi_gaming' if (c_id == 1 or 'katana' in name_l or 'cyborg' in name_l or 'raider' in name_l) else 'msi_office'
    elif b_id == 8: # LG
        return 'lg_gram'
    elif b_id == 9: # Gigabyte
        return 'gigabyte'
    elif b_id == 10: # Microsoft
        return 'microsoft'
    return 'asus_office'

# Copy ảnh tương ứng vào thư mục wwwroot và react public
saved_images_count = 0
for p in selected_products:
    img_filename = os.path.basename(p['thumbnail'])
    base_key = pick_base_image(p['brand_id'], p['category_id'], p['name'])
    src_img = cached_images.get(base_key)
    if not src_img or not os.path.exists(src_img):
        src_img = cached_images.get('asus_gaming') or next((f for f in cached_images.values() if os.path.exists(f)), None)

    if src_img and os.path.exists(src_img):
        dst_www = os.path.join(WWWROOT_PRODUCTS, img_filename)
        dst_react = os.path.join(REACT_PRODUCTS, img_filename)
        shutil.copyfile(src_img, dst_www)
        shutil.copyfile(src_img, dst_react)
        saved_images_count += 1

print(f"-> Đã tạo thành công {saved_images_count} file ảnh sản phẩm trong:")
print(f"   1. {WWWROOT_PRODUCTS}")
print(f"   2. {REACT_PRODUCTS}")

# Tạo logo các thương hiệu
brand_svg_templates = {
    'asus.png': ('ASUS', '#00539B'),
    'dell.png': ('DELL', '#0076CE'),
    'lenovo.png': ('Lenovo', '#E2231A'),
    'apple.png': ('Apple', '#333333'),
    'acer.png': ('acer', '#83B81A'),
    'hp.png': ('HP', '#0096D6'),
    'msi.png': ('MSI', '#E01E25'),
    'lg.png': ('LG', '#A50034'),
    'gigabyte.png': ('GIGABYTE', '#FF6600'),
    'microsoft.png': ('Microsoft', '#737373')
}

# Tạo ảnh placeholder đơn giản nhưng đẹp mắt cho logo
def make_svg_logo(name, color):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 60" width="200" height="60">
  <rect width="100%" height="100%" fill="#ffffff" rx="8" />
  <rect x="2" y="2" width="196" height="56" fill="none" stroke="#e0e0e0" stroke-width="1" rx="6" />
  <text x="50%" y="55%" dominant-baseline="middle" text-anchor="middle" fill="{color}" font-family="Arial, sans-serif" font-weight="bold" font-size="22" letter-spacing="1">{name}</text>
</svg>'''

for filename, (text, color) in brand_svg_templates.items():
    svg_content = make_svg_logo(text, color)
    # Lưu dạng svg và png để đảm bảo tương thích
    for folder in [WWWROOT_BRANDS, REACT_BRANDS]:
        svg_path = os.path.join(folder, filename.replace('.png', '.svg'))
        with open(svg_path, 'w', encoding='utf-8') as f:
            f.write(svg_content)
        # copy file sang .png để phục vụ đúng đường dẫn trong DB
        png_path = os.path.join(folder, filename)
        # dùng một ảnh 1x1 hoặc copy ảnh để không bị 404
        if not os.path.exists(png_path):
            with open(png_path, 'wb') as f:
                # 1x1 transparent PNG fallback nếu cần
                f.write(bytes.fromhex('89504e470d0a1a0a0000000d49484452000000010000000108060000001f15c4890000000a49444154789c63000100000500010d0a2db40000000049454e44ae426082'))

print("-> Đã tạo toàn bộ logo thương hiệu trong wwwroot/images/brands và frontend/public/images/brands")

print("\n3. Đang xuất file SQL nạp 100 Laptop vào database...")
SQL_FILE = os.path.join(BASE_DIR, 'sql-init', 'init-db.sql')

sql_lines = [
    "-- Khởi tạo CSDL LaptopStoreDb",
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

brand_vals = []
for b in brands_list:
    desc_clean = b['desc'].replace("'", "''")
    brand_vals.append(f"({b['id']}, '{b['name']}', '{b['logo']}', '{desc_clean}')")
sql_lines.append(",\n".join(brand_vals) + ";\n")

sql_lines.append("-- 2. Categories")
sql_lines.append("INSERT INTO Categories (CategoryId, CategoryName, Description) VALUES")
cat_vals = []
for c in categories_list:
    desc_clean = c['desc'].replace("'", "''")
    cat_vals.append(f"({c['id']}, '{c['name']}', '{desc_clean}')")
sql_lines.append(",\n".join(cat_vals) + ";\n")

sql_lines.append("-- 3. Products (100 Laptops)")
sql_lines.append("INSERT INTO Products (ProductId, ProductName, BrandId, CategoryId, Price, DiscountPrice, StockQuantity, ThumbnailUrl, Description, IsActive) VALUES")
prod_vals = []
for p in selected_products:
    name_clean = p['name'].replace("'", "''")
    desc_clean = p['desc'].replace("'", "''")
    disc = f"{p['discount_price']:.2f}" if p['discount_price'] else "NULL"
    prod_vals.append(f"({p['id']}, '{name_clean}', {p['brand_id']}, {p['category_id']}, {p['price']:.2f}, {disc}, {p['stock']}, '{p['thumbnail']}', '{desc_clean}', 1)")
sql_lines.append(",\n".join(prod_vals) + ";\n")

sql_lines.append("-- 4. ProductSpecifications (100 Specs)")
sql_lines.append("INSERT INTO ProductSpecifications (ProductId, CPU, RamGB, StorageGB, StorageType, GPU, ScreenSizeInch, RefreshRateHz, WeightKg, BatteryWh, OS) VALUES")
spec_vals = []
for p in selected_products:
    s = p['spec']
    if not s:
        s = {'cpu': 'Intel Core i5', 'ram': 16, 'storage': 512, 'storageType': 'SSD NVMe', 'gpu': 'Intel Iris Xe', 'screen': 15.6, 'refresh': 60, 'weight': 1.8, 'battery': 50, 'os': 'Windows 11'}
    cpu_clean = s['cpu'].replace("'", "''")
    gpu_clean = s['gpu'].replace("'", "''")
    os_clean = s['os'].replace("'", "''")
    stype_clean = s['storageType'].replace("'", "''")
    spec_vals.append(f"({p['id']}, '{cpu_clean}', {s['ram']}, {s['storage']}, '{stype_clean}', '{gpu_clean}', {s['screen']}, {s['refresh']}, {s['weight']}, {s['battery']}, '{os_clean}')")
sql_lines.append(",\n".join(spec_vals) + ";\n")

sql_content = "\n".join(sql_lines)
with open(SQL_FILE, 'w', encoding='utf-8') as f:
    f.write(sql_content)

print(f"-> Đã ghi file {SQL_FILE} ({len(sql_content)} bytes)")
print("Hoàn tất chuẩn bị dữ liệu 100 Laptop!")

export const BRANDS_LIST = [
  'ASUS', 'Acer', 'Dell', 'HP', 'Lenovo', 'MSI', 'Apple', 'LG', 'Gigabyte', 'Microsoft'
];

export const SCREEN_SIZES = ['13 inch', '14 inch', '15.6 inch', '16 inch', '17 inch'];

export const CPU_TYPES = [
  'Intel Core i5',
  'Intel Core i7',
  'Intel Core i9',
  'Intel Core Ultra',
  'AMD Ryzen 5',
  'AMD Ryzen 7',
  'Apple Silicon'
];

export const CATEGORIES = [
  { icon: "bi-laptop", label: "Laptop văn phòng" },
  { icon: "bi-controller", label: "Laptop gaming" },
  { icon: "bi-palette", label: "Laptop đồ họa" },
  { icon: "bi-pc-display", label: "PC - Máy tính để bàn" },
  { icon: "bi-display", label: "Màn hình" },
  { icon: "bi-cpu", label: "Linh kiện - Phụ kiện" },
  { icon: "bi-router", label: "Thiết bị mạng" },
  { icon: "bi-hdd", label: "Thiết bị lưu trữ" },
  { icon: "bi-lamp", label: "Ghế - Bàn gaming" },
  { icon: "bi-tag", label: "Khuyến mãi" }
];

export const FILTER_TAGS = [
  "Tất cả sản phẩm",
  "Laptop văn phòng",
  "Laptop gaming",
  "Laptop đồ họa",
  "PC - Máy tính để bàn"
];

export const PRODUCT_DATA = [
  {
    id: 1,
    name: "Acer Aspire 5 A515-57G-56TU",
    brand: "Acer",
    category: "Laptop văn phòng",
    screenSize: "15.6 inch",
    specs: "i5-12450H / 8GB / 512GB / 15.6\" FHD / Win11",
    price: 13990000,
    originalPrice: 15490000,
    badge: "-10%",
    badgeColor: "bg-danger",
    image: "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=400&q=80"
  },
  {
    id: 2,
    name: "HP 15s-fq5229TU",
    brand: "HP",
    category: "Laptop văn phòng",
    screenSize: "15.6 inch",
    specs: "i5-1235U / 8GB / 512GB / 15.6\" FHD / Win11",
    price: 12990000,
    originalPrice: 13990000,
    badge: "-8%",
    badgeColor: "bg-danger",
    image: "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=400&q=80"
  },
  {
    id: 3,
    name: "Lenovo Ideapad 3 15IAU7",
    brand: "Lenovo",
    category: "Laptop văn phòng",
    screenSize: "15.6 inch",
    specs: "i3-1215U / 8GB / 512GB / 15.6\" FHD / Win11",
    price: 11490000,
    originalPrice: 12390000,
    badge: "-7%",
    badgeColor: "bg-danger",
    image: "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=400&q=80"
  },
  {
    id: 4,
    name: "Dell Inspiron 3520",
    brand: "Dell",
    category: "Laptop văn phòng",
    screenSize: "15.6 inch",
    specs: "i5-1135G7 / 8GB / 512GB / 15.6\" FHD / Win11",
    price: 12990000,
    originalPrice: 14290000,
    badge: "-9%",
    badgeColor: "bg-danger",
    image: "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=400&q=80"
  },
  {
    id: 5,
    name: "ASUS Vivobook 15 X1504ZA",
    brand: "ASUS",
    category: "Laptop văn phòng",
    screenSize: "15.6 inch",
    specs: "i3-1215U / 8GB / 512GB / 15.6\" FHD / Win11",
    price: 10990000,
    originalPrice: 11690000,
    badge: "-6%",
    badgeColor: "bg-danger",
    image: "https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=400&q=80"
  },
  {
    id: 6,
    name: "HP Pavilion 14-dv2077TU",
    brand: "HP",
    category: "Laptop văn phòng",
    screenSize: "14 inch",
    specs: "i5-1335U / 16GB / 512GB / 14\" FHD / Win11",
    price: 16490000,
    originalPrice: null,
    badge: "MỚI",
    badgeColor: "bg-success",
    image: "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=400&q=80"
  },
  {
    id: 7,
    name: "ASUS ROG Strix G16 G614JZ",
    brand: "ASUS",
    category: "Laptop gaming",
    screenSize: "16 inch",
    specs: "i7-13650HX / 16GB / 512GB / 16\" FHD+ / Win11",
    price: 29990000,
    originalPrice: null,
    badge: "MỚI",
    badgeColor: "bg-success",
    image: "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=400&q=80"
  },
  {
    id: 8,
    name: "Lenovo Legion 5 15ARH7",
    brand: "Lenovo",
    category: "Laptop gaming",
    screenSize: "15.6 inch",
    specs: "Ryzen 7 6800H / 16GB / 512GB / 15.6\" FHD / Win11",
    price: 24990000,
    originalPrice: null,
    badge: "MỚI",
    badgeColor: "bg-success",
    image: "https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=400&q=80"
  },
  {
    id: 9,
    name: "MSI Katana 15 B12VGK",
    brand: "MSI",
    category: "Laptop gaming",
    screenSize: "15.6 inch",
    specs: "i7-12650H / 16GB / 512GB / 15.6\" FHD / Win11",
    price: 22990000,
    originalPrice: 24290000,
    badge: "-5%",
    badgeColor: "bg-danger",
    image: "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=400&q=80"
  },
  {
    id: 10,
    name: "Acer Nitro 5 AN515-58-52SP",
    brand: "Acer",
    category: "Laptop gaming",
    screenSize: "15.6 inch",
    specs: "i5-12500H / 8GB / 512GB / 15.6\" FHD / Win11",
    price: 17490000,
    originalPrice: 18790000,
    badge: "-7%",
    badgeColor: "bg-danger",
    image: "https://images.unsplash.com/photo-1542393545-10f5cde2c810?w=400&q=80"
  }
];
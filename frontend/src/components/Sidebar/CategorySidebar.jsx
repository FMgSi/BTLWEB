import { CATEGORIES } from '../../data/mockData';

/**
 * Component hiển thị danh mục các loại linh kiện / dịch vụ bên cột trái
 */
export default function CategorySidebar() {
  return (
    <div className="border rounded bg-white mb-3 shadow-sm">
      {CATEGORIES.map((cat, index) => (
        <div key={index} className="category-item d-flex align-items-center gap-2">
          <i className={`bi ${cat.icon} text-secondary`}></i>
          <span>{cat.label}</span>
        </div>
      ))}
    </div>
  );
}
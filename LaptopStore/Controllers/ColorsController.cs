using System.Linq;
using System.Threading.Tasks;
using LaptopStore.Data;
using LaptopStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers;

/// <summary>
/// Controller Quản trị Bảng Màu sắc Laptop (Colors)
/// Thiết kế theo mô hình MVC mẫu của LapTrinhWeb (CategoriesController / BrandsController)
/// Được bảo vệ chặt chẽ bởi Cookie Authentication: [Authorize(Roles = "Admin")]
/// </summary>
[Authorize(Roles = "Admin")]
public class ColorsController : Controller
{
    private readonly AppDbContext _context;

    public ColorsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Colors
    public async Task<IActionResult> Index(string? keyword)
    {
        var query = _context.Colors
            .Include(c => c.Variants)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(k) || (c.HexCode != null && c.HexCode.ToLower().Contains(k)));
            ViewBag.Keyword = keyword;
        }

        var colors = await query.OrderBy(c => c.ColorId).ToListAsync();
        return View(colors);
    }

    // GET: Colors/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var color = await _context.Colors
            .Include(c => c.Variants)
                .ThenInclude(v => v.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ColorId == id);

        if (color == null)
        {
            return NotFound();
        }

        return View(color);
    }

    // GET: Colors/Create
    public IActionResult Create()
    {
        var model = new Color
        {
            HexCode = "#3B82F6",
            IsActive = true
        };
        return View(model);
    }

    // POST: Colors/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,HexCode,IsActive")] Color color)
    {
        color.Name = color.Name?.Trim() ?? string.Empty;
        color.HexCode = string.IsNullOrWhiteSpace(color.HexCode) ? "#6B7280" : color.HexCode.Trim();

        if (string.IsNullOrWhiteSpace(color.Name))
        {
            ModelState.AddModelError("Name", "Tên màu sắc không được để trống.");
        }
        else if (await _context.Colors.AnyAsync(c => c.Name.ToLower() == color.Name.ToLower()))
        {
            ModelState.AddModelError("Name", $"Tên màu '{color.Name}' đã tồn tại trong danh mục hệ thống.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(color);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Thêm mới màu sắc \"{color.Name}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        return View(color);
    }

    // GET: Colors/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var color = await _context.Colors.FindAsync(id);
        if (color == null)
        {
            return NotFound();
        }

        return View(color);
    }

    // POST: Colors/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ColorId,Name,HexCode,IsActive")] Color color)
    {
        if (id != color.ColorId)
        {
            return NotFound();
        }

        color.Name = color.Name?.Trim() ?? string.Empty;
        color.HexCode = string.IsNullOrWhiteSpace(color.HexCode) ? "#6B7280" : color.HexCode.Trim();

        if (string.IsNullOrWhiteSpace(color.Name))
        {
            ModelState.AddModelError("Name", "Tên màu sắc không được để trống.");
        }
        else if (await _context.Colors.AnyAsync(c => c.ColorId != id && c.Name.ToLower() == color.Name.ToLower()))
        {
            ModelState.AddModelError("Name", $"Tên màu '{color.Name}' đã trùng với một màu khác trong hệ thống.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(color);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật màu sắc \"{color.Name}\" thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ColorExists(color.ColorId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        return View(color);
    }

    // GET: Colors/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var color = await _context.Colors
            .Include(c => c.Variants)
                .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(m => m.ColorId == id);

        if (color == null)
        {
            return NotFound();
        }

        return View(color);
    }

    // POST: Colors/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var color = await _context.Colors
            .Include(c => c.Variants)
            .FirstOrDefaultAsync(c => c.ColorId == id);

        if (color != null)
        {
            if (color.Variants != null && color.Variants.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa màu \"{color.Name}\" vì đang có {color.Variants.Count} phiên bản laptop sử dụng màu này! Bạn có thể chuyển màu sang trạng thái ngừng kích hoạt.";
                return RedirectToAction(nameof(Index));
            }

            _context.Colors.Remove(color);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa màu sắc \"{color.Name}\" thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Colors/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var color = await _context.Colors.FindAsync(id);
        if (color == null)
        {
            return NotFound();
        }

        color.IsActive = !color.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã {(color.IsActive ? "kích hoạt" : "vô hiệu hóa")} màu \"{color.Name}\" thành công!";
        return RedirectToAction(nameof(Index));
    }

    private bool ColorExists(int id)
    {
        return _context.Colors.Any(e => e.ColorId == id);
    }
}

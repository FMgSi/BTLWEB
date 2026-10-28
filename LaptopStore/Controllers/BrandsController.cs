using System.Linq;
using System.Threading.Tasks;
using LaptopStore.Data;
using LaptopStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers;

/// <summary>
/// Controller Quản trị Hãng / Thương hiệu Laptop (Brands)
/// Tương ứng với PublishersController trong LapTrinhWeb (TvcLesson08Df)
/// </summary>
[Authorize(Roles = "Admin")]
public class BrandsController : Controller
{
    private readonly AppDbContext _context;

    public BrandsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Brands
    public async Task<IActionResult> Index()
    {
        var brands = await _context.Brands
            .Include(b => b.Products)
            .OrderBy(b => b.BrandId)
            .ToListAsync();
        return View(brands);
    }

    // GET: Brands/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var brand = await _context.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(m => m.BrandId == id);

        if (brand == null)
        {
            return NotFound();
        }

        return View(brand);
    }

    // GET: Brands/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Brands/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("BrandId,BrandName,LogoUrl,Description")] Brand brand)
    {
        if (ModelState.IsValid)
        {
            _context.Add(brand);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm mới thương hiệu thành công!";
            return RedirectToAction(nameof(Index));
        }
        return View(brand);
    }

    // GET: Brands/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var brand = await _context.Brands.FindAsync(id);
        if (brand == null)
        {
            return NotFound();
        }
        return View(brand);
    }

    // POST: Brands/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("BrandId,BrandName,LogoUrl,Description")] Brand brand)
    {
        if (id != brand.BrandId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(brand);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thương hiệu thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BrandExists(brand.BrandId))
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
        return View(brand);
    }

    // GET: Brands/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var brand = await _context.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(m => m.BrandId == id);

        if (brand == null)
        {
            return NotFound();
        }

        return View(brand);
    }

    // POST: Brands/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var brand = await _context.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.BrandId == id);

        if (brand != null)
        {
            if (brand.Products != null && brand.Products.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa thương hiệu \"{brand.BrandName}\" vì vẫn còn {brand.Products.Count} sản phẩm thuộc thương hiệu này!";
                return RedirectToAction(nameof(Index));
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa thương hiệu thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    private bool BrandExists(int id)
    {
        return _context.Brands.Any(e => e.BrandId == id);
    }
}

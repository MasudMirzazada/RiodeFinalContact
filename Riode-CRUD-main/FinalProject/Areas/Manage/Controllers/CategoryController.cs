using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinalProject.DAL;
using FinalProject.Extensions;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace FinalProject.Areas.Manage.Controllers
{
    [Area("manage")]
    //[Authorize(Roles = "SuperAdmin,Admin")]
    public class CategoryController : Controller
    {
        private readonly RiodeDbContext _context;

        public CategoryController(RiodeDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(bool? status, int page = 1)
        {
            IEnumerable<Category> Categorys = null;
            ViewBag.Status = status;
            if (status == null)
            {
                Categorys = await _context.Categories
                  .ToListAsync();
            }
            else
            {
                Categorys = await _context.Categories
                    .Where(p => p.IsDeleted == (status))
                  .ToListAsync();
            }
            ViewBag.PageIndex = page;
            ViewBag.PageCount = Math.Ceiling((double)Categorys.Count() / 5);

            return View(Categorys.Skip((page - 1) * 5).Take(5));

        }
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null) return BadRequest();

            Category dbCategory = await _context.Categories.FirstOrDefaultAsync(t => t.Id == id);

            if (dbCategory == null) return NotFound();
            return View(dbCategory);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id, Category Category, string Categoryn, bool? status, int page = 1)
        {
            if (!ModelState.IsValid) return View();

            if (id == null) return BadRequest();

            if (id != Category.Id) return NotFound();

            Category dbCategory = await _context.Categories.FirstOrDefaultAsync(t => t.Id == id);

            if (dbCategory == null) return NotFound();

            if (string.IsNullOrWhiteSpace(Category.Name))
            {
                ModelState.AddModelError("Name", "Bosluq Olmamalidir");
                return View();
            }

            if (Category.Name.CheckString())
            {
                ModelState.AddModelError("Name", "Yalniz Herf Ola Biler");
                return View();
            }

            if (await _context.Categories.AnyAsync(t => t.Name.ToLower() == Category.Name.ToLower() && Category.Name != dbCategory.Name))
            {
                ModelState.AddModelError("Name", "Alreade Exists");
                return View();
            }
            dbCategory.Name = Category.Name;
            dbCategory.UpdatedAt = DateTime.UtcNow.AddHours(4);

            await _context.SaveChangesAsync();
            List<Category> Categorys = await _context.Categories.ToListAsync();

            ViewBag.PageCount = Math.Ceiling((double)Categorys.Count() / 5);

            return RedirectToAction("index", new { status, page });
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.MainCategory = await _context.Categories.Where(c => c.IsMain && !c.IsDeleted).ToListAsync();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category Category, string Categoryn)
        {
            ViewBag.MainCategory = await _context.Categories.Where(c => c.IsMain && !c.IsDeleted).ToListAsync();
            if (!ModelState.IsValid) return View();
            if (string.IsNullOrWhiteSpace(Category.Name))
            {
                ModelState.AddModelError("Name", "Bosluq Olmamalidir");
                return View();
            }

            if (Category.Name.CheckString())
            {
                ModelState.AddModelError("Name", "Yalniz Herf Ola Biler");
                return View();
            }

            if (await _context.Categories.AnyAsync(t => t.Name.ToLower() == Category.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "Alreade Exists");
                return View();
            }
            Category.CreatedAt = DateTime.UtcNow.AddHours(4);

            await _context.Categories.AddAsync(Category);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Delete(int? id, bool? status, int page = 1)
        {
            if (id == null) return BadRequest();
            Category Category = await _context.Categories.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            if (Category == null) return NotFound();
            Category.IsDeleted = true;
            Category.DeletedAt = DateTime.UtcNow.AddHours(4);
            await _context.SaveChangesAsync(); ViewBag.PageIndex = page;

            List<Category> Categorys = await _context.Categories.ToListAsync();

            ViewBag.PageCount = Math.Ceiling((double)Categorys.Count() / 5);

            return RedirectToAction("index", new { status, page });
        }
        public async Task<IActionResult> Restore(int? id, bool? status, int page = 1)
        {
            if (id == null) return BadRequest();
            Category Category = await _context.Categories.FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted);
            if (Category == null) return NotFound();
            Category.IsDeleted = false;
            await _context.SaveChangesAsync();

            List<Category> Categorys = await _context.Categories.ToListAsync();

            ViewBag.PageCount = Math.Ceiling((double)Categorys.Count() / 5);

            return RedirectToAction("index", new { status, page });
        }
    }
}

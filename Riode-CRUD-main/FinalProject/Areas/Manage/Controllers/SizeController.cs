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
    public class SizeController : Controller
    {
        private readonly RiodeDbContext _context;

        public SizeController(RiodeDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            IEnumerable<Size> sizes = await _context.Sizes
               .ToListAsync();
            ViewBag.PageIndex = page;
            ViewBag.PageCount = Math.Ceiling((double)sizes.Count() / 5);

            return View(sizes.Skip((page - 1) * 5).Take(5));
        }
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null) return BadRequest();

            Size dbSize = await _context.Sizes.FirstOrDefaultAsync(t => t.Id == id);

            if (dbSize == null) return NotFound();
            return View(dbSize);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id, Size Size)
        {
            if (!ModelState.IsValid) return View();

            if (id == null) return BadRequest();

            if (id != Size.Id) return NotFound();

            Size dbSize = await _context.Sizes.FirstOrDefaultAsync(t => t.Id == id);

            if (dbSize == null) return NotFound();

            if (string.IsNullOrWhiteSpace(Size.Name))
            {
                ModelState.AddModelError("Name", "Bosluq Olmamalidir");
                return View();
            }

            if (await _context.Sizes.AnyAsync(t => t.Name.ToLower() == Size.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "Alreade Exists");
                return View();
            }
            dbSize.Name = Size.Name;
            dbSize.UpdatedAt = DateTime.UtcNow.AddHours(4);

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Size Size)
        {
            if (!ModelState.IsValid) return View();

            if (string.IsNullOrWhiteSpace(Size.Name))
            {
                ModelState.AddModelError("Name", "Bosluq Olmamalidir");
                return View();
            }

            if (await _context.Sizes.AnyAsync(t => t.Name.ToLower() == Size.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "Alreade Exists");
                return View();
            }

            Size.CreatedAt = DateTime.UtcNow.AddHours(4);

            await _context.Sizes.AddAsync(Size);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Delete(int? id,bool? status,int page = 1)
        {
            if (id == null) return BadRequest();
            Size Size = await _context.Sizes.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            if (Size == null) return NotFound();
            Size.IsDeleted = true;
            Size.DeletedAt = DateTime.UtcNow.AddHours(4);
            await _context.SaveChangesAsync();
            return RedirectToAction("index",new { status, page });
        }
        public async Task<IActionResult> Restore(int? id, bool? status, int page = 1)
        {
            if (id == null) return BadRequest();
            Size Size = await _context.Sizes.FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted);
            if (Size == null) return NotFound();
            Size.IsDeleted = false;
            await _context.SaveChangesAsync();
            return RedirectToAction("index", new { status, page });
        }
    }
}

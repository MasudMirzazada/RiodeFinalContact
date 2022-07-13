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
    public class ColorController : Controller
    {
        private readonly RiodeDbContext _context;

        public ColorController(RiodeDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(bool? status,int page=1)
        {
            IEnumerable<Color> colors = null;
            ViewBag.Status = status;
            if(status == null)
            {
                colors = await _context.Colors
                  .ToListAsync();
            }
            else
            {
                colors = await _context.Colors
                    .Where(p => p.IsDeleted == (status))
                  .ToListAsync();
            }
            ViewBag.PageIndex = page;
            ViewBag.PageCount = Math.Ceiling((double)colors.Count() / 5);

            return View(colors.Skip((page - 1) * 5).Take(5));

        }
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null) return BadRequest();

            Color dbColor = await _context.Colors.FirstOrDefaultAsync(t => t.Id == id);

            if (dbColor == null) return NotFound();
            return View(dbColor);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id, Color Color,string colorn, bool? status, int page = 1)
        {
            if (!ModelState.IsValid) return View();

            if (id == null) return BadRequest();

            if (id != Color.Id) return NotFound();

            Color dbColor = await _context.Colors.FirstOrDefaultAsync(t => t.Id == id);

            if (dbColor == null) return NotFound();

            if (string.IsNullOrWhiteSpace(Color.Name))
            {
                ModelState.AddModelError("Name", "Bosluq Olmamalidir");
                return View();
            }

            if (Color.Name.CheckString())
            {
                ModelState.AddModelError("Name", "Yalniz Herf Ola Biler");
                return View();
            }

            if (await _context.Colors.AnyAsync(t => t.Name.ToLower() == Color.Name.ToLower() && Color.Name != dbColor.Name))
            {
                ModelState.AddModelError("Name", "Alreade Exists");
                return View();
            }
            dbColor.Name = Color.Name;
            dbColor.ColorCode = colorn;
            dbColor.UpdatedAt = DateTime.UtcNow.AddHours(4);

            await _context.SaveChangesAsync();
            List<Color> colors = await _context.Colors.ToListAsync();

            ViewBag.PageCount = Math.Ceiling((double)colors.Count() / 5);

            return RedirectToAction("index", new { status, page });
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Color Color,string colorn)
        {
            if (!ModelState.IsValid) return View();
            if (string.IsNullOrWhiteSpace(Color.Name))
            {
                ModelState.AddModelError("Name", "Bosluq Olmamalidir");
                return View();
            }

            if (Color.Name.CheckString())
            {
                ModelState.AddModelError("Name", "Yalniz Herf Ola Biler");
                return View();
            }

            if (await _context.Colors.AnyAsync(t => t.Name.ToLower() == Color.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "Alreade Exists");
                return View();
            }
            Color.ColorCode = colorn;
            Color.CreatedAt = DateTime.UtcNow.AddHours(4);

            await _context.Colors.AddAsync(Color);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Delete(int? id, bool? status,int page=1)
        {
            if (id == null) return BadRequest();
            Color Color = await _context.Colors.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            if (Color == null) return NotFound();
            Color.IsDeleted = true;
            Color.DeletedAt = DateTime.UtcNow.AddHours(4);
            await _context.SaveChangesAsync(); ViewBag.PageIndex = page;

            List<Color> colors = await _context.Colors.ToListAsync();

            ViewBag.PageCount = Math.Ceiling((double)colors.Count() / 5);

            return RedirectToAction("index", new { status, page });
        }
        public async Task<IActionResult> Restore(int? id, bool? status, int page = 1)
        {
            if (id == null) return BadRequest();
            Color Color = await _context.Colors.FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted);
            if (Color == null) return NotFound();
            Color.IsDeleted = false;
            await _context.SaveChangesAsync();

            List<Color> colors = await _context.Colors.ToListAsync();

            ViewBag.PageCount = Math.Ceiling((double)colors.Count() / 5);

            return RedirectToAction("index", new { status, page });
        }
    }
}

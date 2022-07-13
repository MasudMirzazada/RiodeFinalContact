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
    public class BrendController : Controller
    {
        private readonly RiodeDbContext _context;

        public BrendController(RiodeDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(bool? status, int page = 1)
        {

            ViewBag.Status = status;
            IEnumerable<Brend> Brends = new List<Brend>();

            if (status != null)
            {
                Brends = await _context.Brends
                    .Where(t => t.IsDeleted == status)
                    .ToListAsync();
            }
            else
            {
                Brends = await _context.Brends
                   .ToListAsync();
            }
            ViewBag.PageIndex = page;
            ViewBag.PageCount = Math.Ceiling((double)Brends.Count() / 5);

            return View(Brends.Skip((page - 1) * 5).Take(5));
        }
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null) return BadRequest();

            Brend dbBrend = await _context.Brends.FirstOrDefaultAsync(t => t.Id == id);

            if (dbBrend == null) return NotFound();
            return View(dbBrend);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id, Brend Brend)
        {
            if (!ModelState.IsValid) return View();

            if (id == null) return BadRequest();

            if (id != Brend.Id) return NotFound();

            Brend dbBrend = await _context.Brends.FirstOrDefaultAsync(t => t.Id == id);

            if (dbBrend == null) return NotFound();

            if (string.IsNullOrWhiteSpace(Brend.Name.Trim()))
            {
                ModelState.AddModelError("Name", "Bosluq Olmamalidir");
                return View();
            }

            if (Brend.Name.CheckString())
            {
                ModelState.AddModelError("Name", "Yalniz Herf Ola Biler");
                return View();
            }

            if (await _context.Brends.AnyAsync(t => t.Name.ToLower().Trim() == Brend.Name.ToLower().Trim()))
            {
                ModelState.AddModelError("Name", "Alreade Exists");
                return View();
            }

            dbBrend.Name = Brend.Name;
            dbBrend.UpdatedAt = DateTime.UtcNow.AddHours(4);

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brend Brend)
        {
            if (!ModelState.IsValid) return View();
            if (string.IsNullOrWhiteSpace(Brend.Name.Trim()))
            {
                ModelState.AddModelError("Name", "Bosluq Olmamalidir");
                return View();
            }

            if (Brend.Name.CheckString())
            {
                ModelState.AddModelError("Name", "Yalniz Herf Ola Biler");
                return View();
            }

            if (await _context.Brends.AnyAsync(t => t.Name.ToLower().Trim() == Brend.Name.ToLower().Trim()))
            {
                ModelState.AddModelError("Name", "Alreade Exists");
                return View();
            }
            Brend.CreatedAt = DateTime.UtcNow.AddHours(4);

            await _context.Brends.AddAsync(Brend);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();
            Brend Brend = await _context.Brends.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            if (Brend == null) return NotFound();
            Brend.IsDeleted = true;
            Brend.DeletedAt = DateTime.UtcNow.AddHours(4);
            await _context.SaveChangesAsync();
            return RedirectToAction("index");
        }
        public async Task<IActionResult> Restore(int? id)
        {
            if (id == null) return BadRequest();
            Brend Brend = await _context.Brends.FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted);
            if (Brend == null) return NotFound();
            Brend.IsDeleted = false;
            await _context.SaveChangesAsync();
            return RedirectToAction("index");
        }
    }
}

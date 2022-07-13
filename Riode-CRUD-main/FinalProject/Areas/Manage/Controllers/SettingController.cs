using FinalProject.DAL;
using FinalProject.Extensions;
using FinalProject.Helpers;
using FinalProject.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinalProject.Areas.Manage.Controllers
{
    [Area("manage")]
    public class SettingController : Controller
    {
        private readonly RiodeDbContext _context;
        private readonly IWebHostEnvironment _env;
        public SettingController(RiodeDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private async Task<Setting> GetSettingsAsync()
        {
            return await _context.Settings.FirstOrDefaultAsync();
        }

        public async Task<IActionResult> Index()
        {
            return View(await GetSettingsAsync());
        }

        public async Task<IActionResult> Update()
        {
            return View(await GetSettingsAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Setting setting)
        {
            Setting dbSetting = await GetSettingsAsync();

            if (!ModelState.IsValid) return View(dbSetting);

            if (setting.Address.Length > 255)
            {
                ModelState.AddModelError("Address", "Max length: 255 symbols");
                return View(dbSetting);
            }

            if (setting.LogoImageFile != null)
            {
                if (!setting.LogoImageFile.CheckFileContentType("image/png"))
                {
                    ModelState.AddModelError("LogoImageFile", "File content type is not image/png");
                    return View(dbSetting);
                }

                if (!setting.LogoImageFile.CheckFileSize(10))
                {
                    ModelState.AddModelError("LogoImageFile", "File size is greater than 10 KB");
                    return View(dbSetting);
                }

                Helper.DeleteFile(_env, dbSetting.Logo, "assets", "images", "demos", "demo23");
                dbSetting.Logo = setting.LogoImageFile.CreateFile(_env, "assets", "images", "demos", "demo23");
            }

            dbSetting.Address = setting.Address;
            dbSetting.Email = setting.Email;
            dbSetting.Email2 = setting.Email2;
            dbSetting.Phone = setting.Phone;
            dbSetting.Fax = setting.Fax;

            dbSetting.UpdatedAt = DateTime.UtcNow.AddHours(4);

            await _context.SaveChangesAsync();

            return RedirectToAction("index");
        }
    }
}

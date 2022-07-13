using FinalProject.DAL;
using FinalProject.Models;
using FinalProject.ViewModels;
using FinalProject.ViewModels.Basket;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinalProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly RiodeDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        public HomeController(RiodeDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            List<Product> products = _context.Products
                .Include(p => p.ProductColorSizes).ThenInclude(p => p.Color)
                .Include(p => p.ProductColorSizes).ThenInclude(p => p.Size)
                .Include(p => p.ProductImages)
                .Include(p=>p.Category)
                .Include(p=>p.Reviews)
                .ToList();
            return View(products);
        }
        public async Task<IActionResult> AddToBasket(int? id, int colorid = 1, int sizeid = 1, int count = 1)
        {
            if (colorid == null || sizeid == null) return BadRequest();

            if (!await _context.Colors.AnyAsync(p => p.Id == colorid && !p.IsDeleted) ||
                !await _context.Sizes.AnyAsync(p => p.Id == sizeid && !p.IsDeleted)) return NotFound();

            if (id == null) return BadRequest();
            Product dBproduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (dBproduct == null) return NotFound();

            ProductColorSize productColorSize = _context.ProductColorSizes
                .Include(p => p.Color)
                .Include(p => p.Size)
                .FirstOrDefault(p => p.ProductId == dBproduct.Id && p.ColorId == colorid && p.SizeId == sizeid);
            if (productColorSize == null) return NotFound();
            List<BasketVM> basketVMs = null;

            string cookie = HttpContext.Request.Cookies["basket"];

            if (!string.IsNullOrWhiteSpace(cookie))
            {
                basketVMs = JsonConvert.DeserializeObject<List<BasketVM>>(cookie);
                if (basketVMs.Any(b => b.ProductId == id && b.ColorId == colorid && b.SizeId == sizeid))
                {
                    basketVMs.Find(b => b.ProductId == id && b.ColorId == colorid && b.SizeId == sizeid).Count += count;
                }
                else
                {
                    basketVMs.Add(new BasketVM
                    {
                        ProductId = (int)id,
                        Count = count,
                        Price = (double)(dBproduct.DiscountPrice != 0 ? dBproduct.DiscountPrice : dBproduct.Price),
                        ColorId = colorid,
                        SizeId = sizeid,
                        ExTax = dBproduct.ExTax,
                        stockCount = productColorSize.Count
                    });
                }
            }
            else
            {
                basketVMs = new List<BasketVM>();

                basketVMs.Add(new BasketVM()
                {
                    ProductId = (int)id,
                    Count = count,
                    Price = (double)(dBproduct.DiscountPrice != 0 ? dBproduct.DiscountPrice : dBproduct.Price),
                    ColorId = colorid,
                    SizeId = sizeid,
                    ExTax = dBproduct.ExTax,
                    stockCount = productColorSize.Count
                });
            }


            if (User.Identity.IsAuthenticated && !string.IsNullOrWhiteSpace(cookie))
            {
                AppUser appUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name.ToUpper() && !u.IsAdmin);
                if (appUser != null)
                {
                    Basket basket = await _context.Baskets.FirstOrDefaultAsync(b => b.AppUserId == appUser.Id && !b.IsDeleted && b.ProductId == id && b.ColorId == colorid && b.SizeId == sizeid);

                    if (basket != null)
                    {
                        basket.Count = count;
                    }
                    else
                    {
                        basket = new Basket
                        {
                            AppUserId = appUser.Id,
                            ProductId = id,
                            SizeId = sizeid,
                            ColorId = colorid,
                            Price = (double)(dBproduct.DiscountPrice != 0 ? dBproduct.DiscountPrice : dBproduct.Price),
                            Count = count
                        };

                        await _context.Baskets.AddAsync(basket);
                    }

                    await _context.SaveChangesAsync();
                }
                else
                {
                    return BadRequest();
                }
            }


            foreach (BasketVM basketVM in basketVMs)
            {
                Product dbProduct = await _context.Products
                    .Include(p => p.ProductColorSizes)
                    .FirstOrDefaultAsync(p => p.Id == basketVM.ProductId);
                basketVM.Image = dbProduct.MainImage;
                basketVM.Name = dbProduct.Name;
                basketVM.ExTax = dbProduct.ExTax;
                basketVM.stockCount = productColorSize.Count;
            }
            HttpContext.Response.Cookies.Append("basket", JsonConvert.SerializeObject(basketVMs));

            return PartialView("_BasketPartial", basketVMs);
        }

        public async Task<IActionResult> DetailModal(int? id, int? color = 1, int? size = 1, int count = 0)
        {
            if (id == null) return BadRequest();

            Product product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return NotFound();

            return PartialView("_ProductModalPartial", product);
        }

        public async Task<int> Count()
        {
            string cookieBasket = HttpContext.Request.Cookies["basket"];

            List<BasketVM> basketVMs = null;

            if (!string.IsNullOrWhiteSpace(cookieBasket))
            {
                basketVMs = JsonConvert.DeserializeObject<List<BasketVM>>(cookieBasket);
            }
            else
            {
                basketVMs = new List<BasketVM>();
            }

            return basketVMs.Count();
        }

        public async Task<double> Subtotal()
        {
            string cookieBasket = HttpContext.Request.Cookies["basket"];

            List<BasketVM> basketVMs = null;

            if (!string.IsNullOrWhiteSpace(cookieBasket))
            {
                basketVMs = JsonConvert.DeserializeObject<List<BasketVM>>(cookieBasket);
            }
            else
            {
                basketVMs = new List<BasketVM>();
            }
            double subtotal = 0;
            foreach (BasketVM item in basketVMs)
            {
                subtotal += item.Price * item.Count;
            }
            return subtotal;
        }

        public async Task<IActionResult> SearchInput(string key)
        {
            List<Product> products = new List<Product>();
            if (key != null)
            {
                products = await _context.Products
                .Where(p => p.Name.Contains(key)
                || p.Description.Contains(key)
                || p.Category.Name.Contains(key)
                )
                .ToListAsync();
            }
            return PartialView("_ProductListPartial", products);
        }
    }
}

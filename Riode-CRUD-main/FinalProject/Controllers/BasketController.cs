﻿using FinalProject.DAL;
using FinalProject.Models;
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
    public class BasketController : Controller
    {
        private readonly RiodeDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BasketController(RiodeDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            string cookie = HttpContext.Request.Cookies["basket"];

            List<BasketVM> basketVMs = null;

            if (cookie != null)
            {
                basketVMs = JsonConvert.DeserializeObject<List<BasketVM>>(cookie);
            }
            else
            {
                basketVMs = new List<BasketVM>();
            }
            foreach (BasketVM basketVM in basketVMs)
            {
                Product dbProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == basketVM.ProductId);
                basketVM.Image = dbProduct.MainImage;
                //basketVM.Price = dbProduct.DiscountPrice > 0 ? dbProduct.DiscountPrice : dbProduct.Price;
                basketVM.Name = dbProduct.Name;
            }
            return View(basketVMs);
        }

        public async Task<IActionResult> GetBasket()
        {
            string cookieBasket = HttpContext.Request.Cookies["basket"];

            List<BasketVM> basketVMs = null;

            if (cookieBasket != null)
            {
                basketVMs = JsonConvert.DeserializeObject<List<BasketVM>>(cookieBasket);
            }
            else
            {
                basketVMs = new List<BasketVM>();
            }

            foreach (BasketVM basketVM in basketVMs)
            {
                Product dbProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == basketVM.ProductId);
                basketVM.Image = dbProduct.MainImage;
                //basketVM.Price = dbProduct.DiscountPrice > 0 ? dbProduct.DiscountPrice : dbProduct.Price;
                basketVM.Name = dbProduct.Name;
            }

            return PartialView("_BasketPartial", basketVMs);
        }
        public async Task<IActionResult> Update(int? id, int? color, int? size, int count = 1)
        {
            if (color == null || size == null) return BadRequest();

            if (!await _context.Colors.AnyAsync(p => p.Id == color && !p.IsDeleted) ||
                !await _context.Sizes.AnyAsync(p => p.Id == size && !p.IsDeleted)) return NotFound();

            if (id == null) return BadRequest();

            Product product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return NotFound();

            ProductColorSize productColorSize = _context.ProductColorSizes
               .Include(p => p.Color)
               .Include(p => p.Size)
               .FirstOrDefault(p => p.ProductId == product.Id && p.ColorId == color && p.SizeId == size);
            if (productColorSize == null) return NotFound();

            string cookie = HttpContext.Request.Cookies["basket"];

            List<BasketVM> basketVMs = null;

            if (cookie != null)
            {
                basketVMs = JsonConvert.DeserializeObject<List<BasketVM>>(cookie);

                if (!basketVMs.Any(b => b.ProductId == id && b.ColorId == (int)color && b.SizeId == (int)size))
                {
                    return NotFound();
                }

                basketVMs.Find(b => b.ProductId == id && b.ColorId == (int)color && b.SizeId == (int)size).Count = (int)count;
            }
            else
            {
                return BadRequest();
            }

            if (User.Identity.IsAuthenticated && !string.IsNullOrWhiteSpace(cookie))
            {
                AppUser appUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name.ToUpper() && !u.IsAdmin);
                if (appUser != null)
                {
                    Basket basket = await _context.Baskets.FirstOrDefaultAsync(b => b.AppUserId == appUser.Id && !b.IsDeleted && b.ProductId == id && b.ColorId == color && b.SizeId == size);

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
                            SizeId = size,
                            ColorId = color,
                            Price = (double)(product.DiscountPrice != 0 ? product.DiscountPrice : product.Price),
                            Count = count
                        };

                        await _context.Baskets.AddAsync(basket);
                    }

                    await _context.SaveChangesAsync();
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
            return PartialView("_BasketIndexPartial", basketVMs);
        }
        public async Task<IActionResult> GetCard()
        {
            string cookieBasket = HttpContext.Request.Cookies["basket"];

            List<BasketVM> basketVMs = null;

            if (cookieBasket != null)
            {
                basketVMs = JsonConvert.DeserializeObject<List<BasketVM>>(cookieBasket);
            }
            else
            {
                basketVMs = new List<BasketVM>();
            }

            foreach (BasketVM basketVM in basketVMs)
            {
                Product dbProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == basketVM.ProductId);
                basketVM.Image = dbProduct.MainImage;
                //basketVM.Price = dbProduct.DiscountPrice > 0 ? dbProduct.DiscountPrice : dbProduct.Price;
                basketVM.Name = dbProduct.Name;
            }

            return PartialView("_BasketIndexPartial", basketVMs);
        }

        public async Task<IActionResult> DeleteCard(int? id, int color = 1, int size = 1)
        {
            if (id == null) return BadRequest();

            Product product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            string cookie = HttpContext.Request.Cookies["basket"];

            List<BasketVM> basketVMs = null;

            if (cookie != null)
            {
                basketVMs = JsonConvert.DeserializeObject<List<BasketVM>>(cookie);

                BasketVM basketVM = basketVMs.FirstOrDefault(b => b.ProductId == id && b.ColorId == color && b.SizeId == size);

                if (basketVM == null)
                {
                    return NotFound();
                }

                basketVMs.Remove(basketVM);
            }
            else
            {
                return BadRequest();
            }

            HttpContext.Response.Cookies.Append("basket", JsonConvert.SerializeObject(basketVMs));

            foreach (BasketVM basketVM in basketVMs)
            {
                Product dbProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == basketVM.ProductId);
                basketVM.Image = dbProduct.MainImage;
                //basketVM.Price = dbProduct.DiscountPrice > 0 ? dbProduct.DiscountPrice : dbProduct.Price;
                basketVM.Name = dbProduct.Name;
                basketVM.ExTax = dbProduct.ExTax;
            }

            cookie = JsonConvert.SerializeObject(basketVMs);


            if (User.Identity.IsAuthenticated && !string.IsNullOrWhiteSpace(cookie))
            {
                AppUser appUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name.ToUpper() && !u.IsAdmin);
                List<BasketVM> BasketVMs = JsonConvert.DeserializeObject<List<BasketVM>>(cookie);
                List<Basket> baskets = new List<Basket>();
                List<Basket> existedBasket = await _context.Baskets.Where(b => b.AppUserId == appUser.Id).ToListAsync();
                for (int i = 0; i < BasketVMs.Count(); i++)
                {
                    Basket basket = new Basket
                    {
                        AppUserId = appUser.Id,
                        ProductId = basketVMs[i].ProductId,
                        Count = basketVMs[i].Count,
                        ColorId = basketVMs[i].ColorId,
                        SizeId = basketVMs[i].SizeId,
                        Price = basketVMs[i].Price,
                        DeletedAt = DateTime.UtcNow.AddHours(4)
                    };
                    baskets.Add(basket);
                }
                _context.RemoveRange(existedBasket);
                await _context.Baskets.AddRangeAsync(baskets);
                await _context.SaveChangesAsync();
            }

            return PartialView("_BasketIndexPartial", basketVMs);
        }

        public async Task<IActionResult> DeleteBasket(int? id, int color = 1, int size = 1)
        {
            if (id == null) return BadRequest();

            Product product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            string cookie = HttpContext.Request.Cookies["basket"];

            List<BasketVM> basketVMs = null;

            if (cookie != null)
            {
                basketVMs = JsonConvert.DeserializeObject<List<BasketVM>>(cookie);

                BasketVM basketVM = basketVMs.FirstOrDefault(b => b.ProductId == id && b.ColorId == color && b.SizeId == size);

                if (basketVM == null)
                {
                    return NotFound();
                }

                basketVMs.Remove(basketVM);
            }
            else
            {
                return BadRequest();
            }

            HttpContext.Response.Cookies.Append("basket", JsonConvert.SerializeObject(basketVMs));

            foreach (BasketVM basketVM in basketVMs)
            {
                Product dbProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == basketVM.ProductId);
                basketVM.Image = dbProduct.MainImage;
                //basketVM.Price = dbProduct.DiscountPrice > 0 ? dbProduct.DiscountPrice : dbProduct.Price;
                basketVM.Name = dbProduct.Name;
            }
            cookie = JsonConvert.SerializeObject(basketVMs);


            if (User.Identity.IsAuthenticated && !string.IsNullOrWhiteSpace(cookie))
            {
                AppUser appUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name.ToUpper() && !u.IsAdmin);
                 List<BasketVM> BasketVMs = JsonConvert.DeserializeObject<List<BasketVM>>(cookie);
                List<Basket> baskets = new List<Basket>();
                List<Basket> existedBasket = await _context.Baskets.Where(b => b.AppUserId == appUser.Id).ToListAsync();
                for (int i = 0; i < BasketVMs.Count(); i++)
                {
                        Basket basket = new Basket
                        {
                            AppUserId = appUser.Id,
                            ProductId = basketVMs[i].ProductId,
                            Count = basketVMs[i].Count,
                            ColorId = basketVMs[i].ColorId,
                            SizeId = basketVMs[i].SizeId,
                            Price = basketVMs[i].Price,
                            CreatedAt = DateTime.UtcNow.AddHours(4)
                        };
                        baskets.Add(basket);
                }
                _context.RemoveRange(existedBasket);
                await _context.Baskets.AddRangeAsync(baskets);
                await _context.SaveChangesAsync();
            }

            return PartialView("_BasketPartial", basketVMs);
        }
    }
}

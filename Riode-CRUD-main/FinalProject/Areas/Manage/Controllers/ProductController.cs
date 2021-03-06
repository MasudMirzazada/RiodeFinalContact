using FinalProject.DAL;
using FinalProject.Enums;
using FinalProject.Extensions;
using FinalProject.Helpers;
using FinalProject.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinalProject.Areas.Manage.Controllers
{
    [Area("manage")]
    public class ProductController : Controller
    {
        private readonly RiodeDbContext _context;
        private readonly IWebHostEnvironment _env;
        public ProductController(RiodeDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        #region Pagination
        private async Task<IEnumerable<Product>> PaginateAsync(string status, int page)
        {
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            IEnumerable<Product> products;

            switch (status)
            {
                case "active":
                    products = await _context.Products
                        .Include(p => p.Brend)
                        .Include(p => p.ProductImages)
                        .Include(p => p.ProductColorSizes).ThenInclude(p => p.Color)
                        .Include(p => p.ProductColorSizes).ThenInclude(p => p.Size)
                        .Include(p => p.Reviews)
                        .Where(p => !p.IsDeleted)
                        .OrderByDescending(p => p.Id)
                        .ToListAsync();
                    break;
                case "deleted":
                    products = await _context.Products
                        .Include(p => p.Brend)
                        .Include(p => p.ProductImages)
                        .Include(p => p.ProductColorSizes).ThenInclude(p => p.Color)
                        .Include(p => p.ProductColorSizes).ThenInclude(p => p.Size)
                        .Include(p => p.Reviews)
                        .Where(p => p.IsDeleted)
                        .OrderByDescending(p => p.Id)
                        .ToListAsync();
                    break;
                default:
                    products = await _context.Products
                        .Include(p => p.Brend)
                        .Include(p => p.ProductImages)
                        .Include(p => p.ProductColorSizes).ThenInclude(p => p.Color)
                        .Include(p => p.ProductColorSizes).ThenInclude(p => p.Size)
                        .Include(p => p.Reviews)
                        .OrderByDescending(p => p.Id)
                        .ToListAsync();
                    break;
            }

            ViewBag.PageCount = Math.Ceiling((double)products.Count() / 5);

            return products.Skip((page - 1) * 5).Take(5);
        }
        #endregion

        public async Task<IActionResult> Index(string status = "active", int page = 1)
        {
            return View(await PaginateAsync(status, page));
        }

        public async Task<IActionResult> Detail(int? id, string status = "active", int page = 1)
        {
            if (id is null) return BadRequest();

            Product product = await _context.Products
                .Include(p => p.Brend)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductColorSizes).ThenInclude(p => p.Color)
                .Include(p => p.ProductColorSizes).ThenInclude(p => p.Size)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product is null) return NotFound();

            ViewBag.Status = status;
            ViewBag.CurrentPage = page;

            return View(product);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Colors = await _context.Colors.Where(c => !c.IsDeleted).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(c => !c.IsDeleted).ToListAsync();
            ViewBag.Categories = await _context.Categories.Where(p => !p.IsDeleted).ToListAsync();
            ViewBag.Brends = await _context.Brends.Where(p => !p.IsDeleted).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, string status = "active", int page = 1)
        {
            ViewBag.Colors = await _context.Colors.Where(c => !c.IsDeleted).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(f => !f.IsDeleted).ToListAsync();
            ViewBag.Categories = await _context.Categories.Where(p => !p.IsDeleted).ToListAsync();
            ViewBag.Brends = await _context.Brends.Where(p => !p.IsDeleted).ToListAsync();

            if (!ModelState.IsValid) return View();

            if (product.Name.Length > 255)
            {
                ModelState.AddModelError("Name", "Max 255 symbols");
                return View();
            }

            if (product.Price < 0 || product.DiscountPrice < 0 || product.ExTax < 0)
            {
                ModelState.AddModelError("", "Money can not be lower than 0.");
                return View();
            }

            if (product.DiscountPrice >= product.Price)
            {
                ModelState.AddModelError("DiscountPrice", "Discount price cannot be equal to or greater than price.");
                return View();
            }

            if (product.ExTax >= product.Price)
            {
                ModelState.AddModelError("ExTax", "Tax price cannot be equal to or greater than price.");
                return View();
            }

            if (!await _context.Brends.AnyAsync(b => b.Id == product.BrendId && !b.IsDeleted))
            {
                ModelState.AddModelError("BrendId", "Has been selected incorrect brand.");
                return View();
            }
            if (!await _context.Categories.AnyAsync(b => b.Id == product.CategoryId && !b.IsDeleted))
            {
                ModelState.AddModelError("CategoryId", "Has been selected incorrect category.");
                return View();
            }

            if ((int)product.Gender < 1 || (int)product.Gender > 3)
            {
                ModelState.AddModelError("Gender", "Has been selected incorrect gender.");
                return View();
            }

            if (await _context.Products.AnyAsync(t => t.Name.ToLower().Trim() == product.Name.ToLower().Trim()))
            {
                ModelState.AddModelError("Name", "Already Exists");
                return View(product);
            }

            if (!product.IsBestseller && !product.IsFeatured && !product.IsNewArrival)
            {
                ModelState.AddModelError("IsFeatured", "You must select at least one");
                return View(product);
            }

            foreach (int item in product.SizeIds)
            {
                if (!await _context.Sizes.AnyAsync(s => s.Id == item))
                {
                    ModelState.AddModelError("", "Incorect Size Id");
                    return View(product);
                }
            }

            foreach (int item in product.ColorIds)
            {
                if (!await _context.Colors.AnyAsync(s => s.Id == item))
                {
                    ModelState.AddModelError("", "Incorect Color Id");
                    return View(product);
                }
            }

            List<ProductColorSize> productColorSizes = new List<ProductColorSize>();

            if (product.ColorIds.Count != product.Counts.Count)
            {
                ModelState.AddModelError("", "Please, select all options.");
                return View();
            }

            foreach (int colourId in product.ColorIds)
            {
                if (!await _context.Colors.AnyAsync(c => c.Id == colourId))
                {
                    ModelState.AddModelError("", "Has been selected incorrect colour.");
                    return View();
                }
            }

            List<ProductColorSize> productColourSizes = new List<ProductColorSize>();
            for (int i = 0; i < product.ColorIds.Count; i++)
            {
                ProductColorSize productColourSize = new ProductColorSize
                {
                    ColorId = product.ColorIds[i],
                    SizeId = product.SizeIds[i],
                    Count = product.Counts[i]
                };

                productColourSizes.Add(productColourSize);
            }
            product.ProductColorSizes = productColourSizes;

            product.Count = product.Counts.Sum();

            for (int i = 0; i < productColorSizes.Count(); i++)
            {
                for (int a = 1; a < productColorSizes.Count() - i; a++)
                {
                    if (productColorSizes[i].ColorId == productColorSizes[i + a].ColorId && productColorSizes[i].SizeId == productColorSizes[i + a].SizeId)
                    {
                        ModelState.AddModelError("", "2 eyni deyer olmaz");
                        return View(product);
                    }
                }
            }

            #region Images Checking & Saving
            if (product.ProductImagesFile != null && product.ProductImagesFile.Count() > 6)
            {
                ModelState.AddModelError("ProductImagesFile", "Max 6 images can be uploaded...");
                return View();
            }

            if (product.MainImageFile != null)
            {
                if (!product.MainImageFile.CheckFileContentType("image/jpeg"))
                {
                    ModelState.AddModelError("MainImageFile", "File content type is not image/jpeg");
                    return View();
                }

                if (!product.MainImageFile.CheckFileSize(100))
                {
                    ModelState.AddModelError("MainImageFile", "File size is greater than 100 KB");
                    return View();
                }

                product.MainImage = product.MainImageFile.CreateFile(_env, "assets", "images", "demos", "demo23", "products");
            }
            else
            {
                ModelState.AddModelError("MainImageFile", "You have to upload an image.");
                return View();
            }

            if (product.HoverImageFile != null)
            {
                if (!product.MainImageFile.CheckFileContentType("image/jpeg"))
                {
                    ModelState.AddModelError("HoverImageFile", "File content type is not image/jpeg");
                    return View();
                }

                if (!product.HoverImageFile.CheckFileSize(100))
                {
                    ModelState.AddModelError("HoverImageFile", "File size is greater than 100 KB");
                    return View();
                }

                product.HoverImage = product.HoverImageFile.CreateFile(_env, "assets", "images", "demos", "demo23", "products");
            }
            else
            {
                ModelState.AddModelError("HoverImageFile", "You have to upload an image.");
                return View();
            }

            if (product.ProductImagesFile != null && product.ProductImagesFile.Count() > 0)
            {
                List<ProductImage> productImages = new List<ProductImage>();

                foreach (IFormFile file in product.ProductImagesFile)
                {
                    if (!file.CheckFileContentType("image/jpeg"))
                    {
                        ModelState.AddModelError("ProductImagesFile", "Not all files have the same content type image/jpeg");
                        return View();
                    }

                    if (!file.CheckFileSize(100))
                    {
                        ModelState.AddModelError("ProductImagesFile", "The size of all files is not less than 100 KB");
                        return View();
                    }

                    ProductImage productImage = new ProductImage
                    {
                        Image = file.CreateFile(_env, "assets", "images", "demos", "demo23", "products"),
                        CreatedAt = DateTime.UtcNow.AddHours(4)
                    };

                    productImages.Add(productImage);
                }

                product.ProductImages = productImages;
            }
            else
            {
                ModelState.AddModelError("ProductImagesFile", "You have to upload other images of this product.");
                return View();
            }


            #endregion

            product.CreatedAt = DateTime.UtcNow.AddHours(4);

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("index", new { status, page });
        }


        public async Task<IActionResult> Update(int? id)
        {
            if (id is null) return BadRequest();

            Product product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Brend)
                .Include(p => p.ProductColorSizes).ThenInclude(p => p.Color)
                .Include(p => p.ProductColorSizes).ThenInclude(p => p.Size)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product is null) return NotFound();

            ViewBag.Colors = await _context.Colors.Where(c => !c.IsDeleted).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(c => !c.IsDeleted).ToListAsync();
            ViewBag.Categories = await _context.Categories.Where(p => !p.IsDeleted).ToListAsync();
            ViewBag.Brends = await _context.Brends.Where(p => !p.IsDeleted).ToListAsync();
            ViewBag.ProductId = id;

            product.ColorIds = product.ProductColorSizes.Select(c => c.Color.Id).ToList();
            product.SizeIds = product.ProductColorSizes.Select(c => c.Size.Id).ToList();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Product product, int? id, string status = "active", int page = 1)
        {
            if (id == null) return BadRequest();

            if (product.Id != id) return BadRequest();

            ViewBag.Colors = await _context.Colors.Where(c => !c.IsDeleted).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(c => !c.IsDeleted).ToListAsync();
            ViewBag.Categories = await _context.Categories.Where(p => !p.IsDeleted).ToListAsync();
            ViewBag.Brends = await _context.Brends.Where(p => !p.IsDeleted).ToListAsync();

            Product dbProduct = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Brend)
                .Include(p => p.Category)
                .Include(p => p.ProductColorSizes).ThenInclude(p => p.Color)
                .Include(p => p.ProductColorSizes).ThenInclude(p => p.Size)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (dbProduct is null) return NotFound();

            ViewBag.ProductId = id;

            if (!ModelState.IsValid) return View();

            if (product.Name.Length > 255)
            {
                ModelState.AddModelError("Name", "Max 255 symbols");
                return View(dbProduct);
            }

            if (product.Price < 0 || product.DiscountPrice < 0 || product.ExTax < 0)
            {
                ModelState.AddModelError("", "Money can not be lower than 0.");
                return View(dbProduct);
            }

            if (product.DiscountPrice >= product.Price)
            {
                ModelState.AddModelError("DiscountPrice", "Discount price cannot be equal to or greater than price.");
                return View(dbProduct);
            }

            if (product.ExTax >= product.Price)
            {
                ModelState.AddModelError("ExTax", "Tax price cannot be equal to or greater than price.");
                return View(dbProduct);
            }

            if (!await _context.Brends.AnyAsync(b => b.Id == product.BrendId && !b.IsDeleted))
            {
                ModelState.AddModelError("BrendId", "Has been selected incorrect brand.");
                return View(dbProduct);
            }
            if (!await _context.Categories.AnyAsync(b => b.Id == product.CategoryId && !b.IsDeleted))
            {
                ModelState.AddModelError("CategoryId", "Has been selected incorrect category.");
                return View(dbProduct);
            }

            if ((int)product.Gender < 1 || (int)product.Gender > 3)
            {
                ModelState.AddModelError("Gender", "Has been selected incorrect gender.");
                return View(dbProduct);
            }

            if (await _context.Products.AnyAsync(t => t.Id != id && t.Name.ToLower().Trim() == product.Name.ToLower().Trim()))
            {
                ModelState.AddModelError("Name", "Already Exists");
                return View(dbProduct);
            }

            if (!product.IsBestseller && !product.IsFeatured && !product.IsNewArrival)
            {
                ModelState.AddModelError("IsFeatured", "You must select at least one");
                return View(dbProduct);
            }

            foreach (int item in product.SizeIds)
            {
                if (!await _context.Sizes.AnyAsync(s => s.Id == item))
                {
                    ModelState.AddModelError("", "Incorect Size Id");
                    return View(dbProduct);
                }
            }

            foreach (int item in product.ColorIds)
            {
                if (!await _context.Colors.AnyAsync(s => s.Id == item))
                {
                    ModelState.AddModelError("", "Incorect Color Id");
                    return View(dbProduct);
                }
            }

            List<ProductColorSize> productColorSizes = new List<ProductColorSize>();

            if (product.ColorIds.Count != product.Counts.Count)
            {
                ModelState.AddModelError("", "Please, select all options.");
                return View(dbProduct);
            }

            foreach (int colourId in product.ColorIds)
            {
                if (!await _context.Colors.AnyAsync(c => c.Id == colourId))
                {
                    ModelState.AddModelError("", "Has been selected incorrect colour.");
                    return View(dbProduct);
                }
            }

            List<ProductColorSize> productColourSizes = new List<ProductColorSize>();
            for (int i = 0; i < product.ColorIds.Count; i++)
            {
                ProductColorSize productColourSize = new ProductColorSize
                {
                    ColorId = product.ColorIds[i],
                    SizeId = product.SizeIds[i],
                    Count = product.Counts[i]
                };

                productColourSizes.Add(productColourSize);
            }
            product.ProductColorSizes = productColourSizes;

            product.Count = product.Counts.Sum();

            for (int i = 0; i < productColorSizes.Count(); i++)
            {
                for (int a = 1; a < productColorSizes.Count() - i; a++)
                {
                    if (productColorSizes[i].ColorId == productColorSizes[i + a].ColorId && productColorSizes[i].SizeId == productColorSizes[i + a].SizeId)
                    {
                        ModelState.AddModelError("", "2 eyni deyer olmaz");
                        return View(dbProduct);
                    }
                }
            }

            #region Image Checking
            int emptySpace4Images = 6 - (int)(dbProduct.ProductImages?.Where(p => !p.IsDeleted).Count());

            if (product.ProductImagesFile != null && product.ProductImagesFile.Count() > emptySpace4Images)
            {
                if (emptySpace4Images == 0)
                {
                    ModelState.AddModelError("ProductImagesFile", "You have reached product images limit.");
                }
                else
                {
                    ModelState.AddModelError("ProductImagesFile", $"You can upload max {emptySpace4Images} image(s).");
                }
                return View(dbProduct);
            }

            if (product.MainImageFile != null)
            {
                if (!product.MainImageFile.CheckFileContentType("image/jpeg"))
                {
                    ModelState.AddModelError("MainImageFile", "File content type is not image/jpeg");
                    return View(dbProduct);
                }

                if (!product.MainImageFile.CheckFileSize(100))
                {
                    ModelState.AddModelError("MainImageFile", "File size is greater than 100 KB");
                    return View(dbProduct);
                }

                Helper.DeleteFile(_env, dbProduct.MainImage, "assets", "images", "demos", "demo23", "products");
                dbProduct.MainImage = product.MainImageFile.CreateFile(_env, "assets", "images", "demos", "demo23", "products");
            }

            if (product.HoverImageFile != null)
            {
                if (!product.HoverImageFile.CheckFileContentType("image/jpeg"))
                {
                    ModelState.AddModelError("HoverImageFile", "File content type is not image/jpeg");
                    return View(dbProduct);
                }

                if (!product.HoverImageFile.CheckFileSize(100))
                {
                    ModelState.AddModelError("HoverImageFile", "File size is greater than 100 KB");
                    return View(dbProduct);
                }

                Helper.DeleteFile(_env, dbProduct.HoverImage, "assets", "images", "demos", "demo23", "products");
                dbProduct.HoverImage = product.HoverImageFile.CreateFile(_env, "assets", "images", "demos", "demo23", "products");
            }

            if (product.ProductImagesFile != null && product.ProductImagesFile.Count() > 0)
            {
                List<ProductImage> productImages = dbProduct.ProductImages.ToList();

                foreach (IFormFile file in product.ProductImagesFile)
                {
                    if (!file.CheckFileContentType("image/jpeg"))
                    {
                        ModelState.AddModelError("ProductImagesFile", "Not all files have the same content type image/jpeg");
                        return View(dbProduct);
                    }

                    if (!file.CheckFileSize(100))
                    {
                        ModelState.AddModelError("ProductImagesFile", "The size of all files is not less than 100 KB");
                        return View(dbProduct);
                    }

                    ProductImage productImage = new ProductImage
                    {
                        Image = file.CreateFile(_env, "assets", "images", "demos", "demo23", "products"),
                        CreatedAt = DateTime.UtcNow.AddHours(4)
                    };

                    productImages.Add(productImage);
                }

                dbProduct.ProductImages = productImages;
            }
            #endregion

            dbProduct.Name = product.Name;
            dbProduct.Price = product.Price;
            dbProduct.DiscountPrice = product.DiscountPrice;
            dbProduct.ExTax = product.ExTax;
            dbProduct.Description = product.Description;
            dbProduct.BrendId = product.BrendId;
            dbProduct.Gender = product.Gender;
            dbProduct.IsNewArrival = product.IsNewArrival;
            dbProduct.ProductColorSizes = product.ProductColorSizes;
            dbProduct.Count = product.Counts.Sum();

            dbProduct.UpdatedAt = DateTime.UtcNow.AddHours(4);

            await _context.SaveChangesAsync();

            return RedirectToAction("index", new { status, page });
        }

        public async Task<IActionResult> Delete(int? id, bool? status, int page = 1)
        {
            if (id == null) return BadRequest();
            Product product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product == null) return NotFound();
            product.IsDeleted = true;
            product.DeletedAt = DateTime.UtcNow.AddHours(4);
            await _context.SaveChangesAsync();

            return RedirectToAction("index", new { status, page });
        }
        public async Task<IActionResult> Restore(int? id, bool? status, int page = 1)
        {
            if (id == null) return BadRequest();
            Product product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted);
            if (product == null) return NotFound();
            product.IsDeleted = false;
            await _context.SaveChangesAsync();


            return RedirectToAction("index", new { status, page });
        }
        public async Task<IActionResult> GetFormColoRSizeCount()
        {
            ViewBag.Colors = await _context.Colors.ToListAsync();
            ViewBag.Sizes = await _context.Sizes.ToListAsync();

            return PartialView("_ProductColorSizePartial");
        }

        public async Task<IActionResult> DeleteProductImage(int? id, int? productId, bool? status, int page = 1)
        {
            if (id is null || productId == null) return BadRequest();

            Product product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Brend)
                .Include(p => p.ProductColorSizes).ThenInclude(p => p.Color)
                .Include(p => p.ProductColorSizes).ThenInclude(p => p.Size)
                .FirstOrDefaultAsync(p => p.Id == productId && p.ProductImages.Any(pi => pi.Id == id && !pi.IsDeleted));

            if (product is null) return NotFound();

            ViewBag.Colors = await _context.Colors.Where(c => !c.IsDeleted).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(c => !c.IsDeleted).ToListAsync();
            ViewBag.Categories = await _context.Categories.Where(p => !p.IsDeleted).ToListAsync();
            ViewBag.Brends = await _context.Brends.Where(p => !p.IsDeleted).ToListAsync();

            product.ColorIds = product.ProductColorSizes.Select(c => c.Color.Id).ToList();
            product.SizeIds = product.ProductColorSizes.Select(c => c.Size.Id).ToList();

            ProductImage productImage = product.ProductImages.FirstOrDefault(p => p.Id == id);
            productImage.IsDeleted = true;
            productImage.DeletedAt = DateTime.UtcNow.AddHours(4);

            await _context.SaveChangesAsync();

            id = productId;

            return RedirectToAction("update", new { id, status, page });
        }
    }
}
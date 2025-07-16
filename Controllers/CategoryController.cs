using ECommerceApi.Data;
using ECommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Controllers
{
    [ApiController]
    [Route("api/v1/categories")]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ApplicationDbContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("add")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AddCategory([FromBody] Category category)
        {
            try
            {
                if (category == null)
                {
                    return BadRequest("Category data is required");
                }

                if (string.IsNullOrWhiteSpace(category.Name))
                {
                    return BadRequest("Category name is required");
                }

                // Check if category already exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());

                if (existingCategory != null)
                {
                    return Conflict($"Category '{category.Name}' already exists");
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Category '{category.Name}' added successfully with ID: {category.Id}");

                return Ok(new
                {
                    message = "Category added successfully",
                    category = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding category");
                return StatusCode(500, "An error occurred while adding the category");
            }
        }

        [HttpPut("{id}/update")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category updated)
        {
            try
            {
                if (updated == null)
                {
                    return BadRequest("Category data is required");
                }

                if (string.IsNullOrWhiteSpace(updated.Name))
                {
                    return BadRequest("Category name is required");
                }

                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound($"Category with ID {id} not found");
                }

                // Check if another category with the same name exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id != id && c.Name.ToLower() == updated.Name.ToLower());

                if (existingCategory != null)
                {
                    return Conflict($"Another category with name '{updated.Name}' already exists");
                }

                category.Name = updated.Name;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Category with ID {id} updated successfully");

                return Ok(new
                {
                    message = "Category updated successfully",
                    category = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating category with ID {id}");
                return StatusCode(500, "An error occurred while updating the category");
            }
        }

        [HttpDelete("{id}/delete")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound($"Category with ID {id} not found");
                }

                // Check if any products are using this category
                var productsCount = await _context.Products
                    .CountAsync(p => p.CategoryId == id);

                if (productsCount > 0)
                {
                    return BadRequest($"Cannot delete category '{category.Name}' as it has {productsCount} products associated with it");
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Category '{category.Name}' with ID {id} deleted successfully");

                return Ok(new
                {
                    message = "Category deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting category with ID {id}");
                return StatusCode(500, "An error occurred while deleting the category");
            }
        }

        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                _logger.LogInformation("Fetching all categories");

                var categories = await _context.Categories
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        ProductCount = _context.Products.Count(p => p.CategoryId == c.Id)
                    })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {categories.Count} categories");

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories");
                return StatusCode(500, "An error occurred while fetching categories");
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        ProductCount = _context.Products.Count(p => p.CategoryId == c.Id)
                    })
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return NotFound($"Category with ID {id} not found");
                }

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching category with ID {id}");
                return StatusCode(500, "An error occurred while fetching the category");
            }
        }

        [HttpGet("{id}/products")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryProducts(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound($"Category with ID {id} not found");
                }

                // Get all products for this category
                // Only use properties that definitely exist in your Product model
                var products = await _context.Products
                    .Where(p => p.CategoryId == id)
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {products.Count} products for category '{category.Name}'");

                return Ok(new
                {
                    Category = category,
                    Products = products,
                    ProductCount = products.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching products for category with ID {id}");
                return StatusCode(500, "An error occurred while fetching category products");
            }
        }

        // Additional helper endpoint to get products with category names
        [HttpGet("products-with-categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductsWithCategories()
        {
            try
            {
                var productsWithCategories = await (from p in _context.Products
                                                    join c in _context.Categories on p.CategoryId equals c.Id
                                                    select new
                                                    {
                                                        p.Id,
                                                        p.Name,
                                                        p.Brand,
                                                        p.Price,
                                                        p.CategoryId,
                                                        CategoryName = c.Name,
                                                        Category = c.Name
                                                    }).ToListAsync();

                _logger.LogInformation($"Retrieved {productsWithCategories.Count} products with category information");

                return Ok(productsWithCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products with categories");
                return StatusCode(500, "An error occurred while fetching products with categories");
            }
        }
    }
}
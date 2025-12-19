using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] int skip = 0, [FromQuery] int take = 100, [FromQuery] bool includeSubCategories = false, CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.GetCategoriesAsync(search, skip, take, includeSubCategories, true, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, true, true, cancellationToken);
        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpGet("{categoryId:int}/subcategories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSubCategories(int categoryId, [FromServices] ISubCategoryService subCategoryService, [FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var result = await subCategoryService.GetByCategoryAsync(categoryId, true, skip, take, cancellationToken);
        return Ok(result);
    }
}

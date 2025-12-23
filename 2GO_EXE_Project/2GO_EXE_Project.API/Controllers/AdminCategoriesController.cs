using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Categories;
using _2GO_EXE_Project.BAL.DTOs.SubCategories;
using _2GO_EXE_Project.BAL.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public AdminCategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _categoryService.GetCategoriesAsync(search, skip, take, false, null, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{categoryId:int}/subcategories")]
    public async Task<IActionResult> GetSubCategories(int categoryId, [FromServices] ISubCategoryService subCategoryService, [FromQuery] bool? isActive, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await subCategoryService.GetByCategoryAsync(categoryId, isActive, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{categoryId:int}/subcategories")]
    [SwaggerOperation(
        Summary = "Create SUBCATEGORY under a CATEGORY",
        Description = "Use this endpoint to create a subcategory for the given categoryId. Category is the parent, subcategory is the child."
    )]
    public async Task<IActionResult> CreateSubCategory(int categoryId, [FromServices] ISubCategoryService subCategoryService, [FromBody] CreateSubCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var result = await subCategoryService.CreateAsync(categoryId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(id, false, null, cancellationToken);
        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create CATEGORY",
        Description = "Use this endpoint to create a top-level category (not a subcategory)."
    )]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:int}")]
    [SwaggerOperation(
        Summary = "Update CATEGORY by id",
        Description = "Update a top-level category. Subcategories have their own endpoints."
    )]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateAsync(id, request, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [SwaggerOperation(
        Summary = "Enable/disable CATEGORY",
        Description = "Toggle category status. Subcategory status is managed in AdminSubCategories."
    )]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateCategoryStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateStatusAsync(id, request, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await _categoryService.DeleteAsync(id, cancellationToken);
        if (!success) return NotFound();
        return Ok(new { success = true });
    }
}

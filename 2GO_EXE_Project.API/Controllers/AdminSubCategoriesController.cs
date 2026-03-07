using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.SubCategories;
using _2GO_EXE_Project.BAL.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/admin/subcategories")]
[Authorize(Roles = "Admin")]
public class AdminSubCategoriesController : ControllerBase
{
    private readonly ISubCategoryService _subCategoryService;

    public AdminSubCategoriesController(ISubCategoryService subCategoryService)
    {
        _subCategoryService = subCategoryService;
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get SUBCATEGORY by id",
        Description = "This endpoint is for subcategory only. Categories use /api/admin/categories/{id}."
    )]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var sub = await _subCategoryService.GetByIdAsync(id, false, cancellationToken);
        if (sub == null) return NotFound();
        return Ok(sub);
    }

    [HttpPatch("{id:int}")]
    [SwaggerOperation(
        Summary = "Update SUBCATEGORY by id",
        Description = "Update a subcategory only. To create a subcategory, use /api/admin/categories/{categoryId}/subcategories."
    )]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSubCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var sub = await _subCategoryService.UpdateAsync(id, request, cancellationToken);
        if (sub == null) return NotFound();
        return Ok(sub);
    }

    [HttpPut("{id:int}/status")]
    [SwaggerOperation(
        Summary = "Enable/disable SUBCATEGORY",
        Description = "Toggle subcategory status. Category status is managed in AdminCategories."
    )]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSubCategoryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var sub = await _subCategoryService.UpdateStatusAsync(id, request, cancellationToken);
        if (sub == null) return NotFound();
        return Ok(sub);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var success = await _subCategoryService.DeleteAsync(id, cancellationToken);
        if (!success) return NotFound();
        return Ok(new { success = true });
    }
}

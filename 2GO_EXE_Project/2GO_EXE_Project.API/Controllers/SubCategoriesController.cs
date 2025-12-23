using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/subcategories")]
public class SubCategoriesController : ControllerBase
{
    private readonly ISubCategoryService _subCategoryService;

    public SubCategoriesController(ISubCategoryService subCategoryService)
    {
        _subCategoryService = subCategoryService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, [FromQuery] int? categoryId, [FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        // default isActive = true if not supplied
        var filterActive = isActive ?? true;
        var result = await _subCategoryService.GetAllAsync(filterActive, categoryId, skip, take, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var sub = await _subCategoryService.GetByIdAsync(id, true, cancellationToken);
        if (sub == null) return NotFound();
        return Ok(sub);
    }
}

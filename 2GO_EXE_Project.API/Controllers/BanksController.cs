using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/banks")]
public class BanksController : ControllerBase
{
    private readonly IBankService _bankService;

    public BanksController(IBankService bankService)
    {
        _bankService = bankService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var result = await _bankService.GetAllAsync(isActive, skip, take, cancellationToken);
        return Ok(result);
    }
}

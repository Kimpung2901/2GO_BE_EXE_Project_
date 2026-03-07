using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using _2GO_EXE_Project.BAL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace _2GO_EXE_Project.API.Controllers
{
    [Route("api/wards")]
    [ApiController]
    public class WardController : ControllerBase
    {
       private readonly IWardService _iWardService;

        public WardController(IWardService iWardService)
        {
            _iWardService = iWardService;
        }

        [HttpGet]
        public async  Task<IActionResult> GetWards([FromQuery] int? districtId,
            [FromQuery] string? search,
            CancellationToken cancellationToken = default)
        {
            var wards = await _iWardService.GetWardsAsync(districtId, search, cancellationToken);
            return Ok(wards);
        }
    }
}
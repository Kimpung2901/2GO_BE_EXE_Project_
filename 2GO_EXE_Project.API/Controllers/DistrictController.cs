using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace _2GO_EXE_Project.API.Controllers
{   
    
    [ApiController]
    [Route("api/districts")]
    public class DistrictController : ControllerBase
    {
        private readonly IDistrictService _iDistrictService;

        public DistrictController(IDistrictService iDistrictService)
        {
            _iDistrictService = iDistrictService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDistricts([FromQuery] int? cityId,
            [FromQuery] string? search,
            CancellationToken cancellationToken = default)
        {
            var districts = await _iDistrictService.GetDistrictsAsync(cityId, search, cancellationToken);
            return Ok(districts);
        }
    }
}
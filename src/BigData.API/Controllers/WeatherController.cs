using BigData.API.Contracts;
using BigData.API.Mappers;
using BigData.Application.GetMeasurementsForDisplay;
using Cqrs.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace BigData.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherController(IMediator mediator) : ControllerBase
    {
        [HttpGet("GetForDisplay")]
        public async Task<ActionResult<GetMeasurementsForDisplayResponse>> GetAllForDisplayAsync(CancellationToken cancellationToken = default)
        {
            return await mediator.QueryAsync(new GetMeasurementsForDisplayQuery(), cancellationToken);
        }
    }
}

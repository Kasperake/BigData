using BigData.Application.GetWeatherObservations;
using Cqrs.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace BigData.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<GetWeatherObservationsResponse>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
        {
            return await mediator.QueryAsync(new GetWeatherObservationsQuery(page, pageSize), cancellationToken);
        }
    }
}

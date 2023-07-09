using System.Threading;
using System.Threading.Tasks;

using BuildingBlocks.Web;

using Flight.Flights.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace Flight.Flights.Features.CreateFlight;

[Route(BaseApiPath + "/flight")]
public class CreateFlightEndpoint : BaseController
{
    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Create new flight", Description = "Create new flight")]
    public async Task<ActionResult> Create([FromBody] CreateFlightCommand command, CancellationToken cancellationToken)
    {
        FlightResponseDto result = await Mediator.Send(command, cancellationToken);

        return Ok(result);
    }
}

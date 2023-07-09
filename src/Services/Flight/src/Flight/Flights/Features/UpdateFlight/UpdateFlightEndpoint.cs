using System.Threading;
using System.Threading.Tasks;

using BuildingBlocks.Web;

using Flight.Flights.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace Flight.Flights.Features.UpdateFlight;

[Route(BaseApiPath + "/flight")]
public class UpdateFlightEndpoint : BaseController
{
    [Authorize]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(Summary = "Update flight", Description = "Update flight")]
    public async Task<ActionResult> Update(UpdateFlightCommand command, CancellationToken cancellationToken)
    {
        FlightResponseDto result = await Mediator.Send(command, cancellationToken);

        return Ok(result);
    }
}

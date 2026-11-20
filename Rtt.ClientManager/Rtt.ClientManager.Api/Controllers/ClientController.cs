using Microsoft.AspNetCore.Mvc;
using Rtt.ClientManager.Bussiness.Services;
using Rtt.ClientManager.Common.Models;

namespace Rtt.ClientManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly ICsvExportService _csvExportService;

        public ClientController(IClientService clientService, ICsvExportService csvExportService)
        {
            _clientService = clientService;
            _csvExportService = csvExportService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ClientDto>>> GetClients(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            var result = await _clientService.GetClientsAsync(page, pageSize, search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ClientDto>> GetClientById(int id)
        {
            var client = await _clientService.GetClientByIdAsync(id);
            if (client == null)
                return NotFound();

            return Ok(client);
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateClient([FromBody] ClientDto client)
        {
            try
            {
                int newId = await _clientService.CreateClientAsync(client);
                client.ClientId = newId;
                return CreatedAtAction(nameof(GetClientById), new { id = newId }, client);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateClient(int id, [FromBody] ClientDto client)
        {
            if (id != client.ClientId)
                return BadRequest(new { errors = new[] { "Route ID does not match payload ClientId." } });

            try
            {
                bool updated = await _clientService.UpdateClientAsync(client);
                if (!updated)
                    return NotFound();

                return NoContent();
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            bool deleted = await _clientService.DeleteClientAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }

        [HttpGet("export")]
        public async Task ExportClientsCsv(CancellationToken cancellationToken)
        {
            string fileName = $"RTT_Clients_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/csv; charset=utf-8";
            Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";

            // Streams directly to the HTTP response body without buffering the whole file in memory
            await _csvExportService.WriteClientsWithAddressesCsvAsync(Response.Body, cancellationToken);
        }
    }
}

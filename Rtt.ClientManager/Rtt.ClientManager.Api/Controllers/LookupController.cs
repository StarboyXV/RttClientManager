using Microsoft.AspNetCore.Mvc;
using Rtt.ClientManager.Bussiness.Services;
using Rtt.ClientManager.Common.Models;

namespace Rtt.ClientManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LookupController : ControllerBase
    {
        private readonly IClientService _clientService;

        public LookupController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet("address-types")]
        public async Task<ActionResult<List<LookupItem>>> GetAddressTypes()
        {
            var items = await _clientService.GetAddressTypesAsync();
            return Ok(items);
        }

        [HttpGet("contact-types")]
        public async Task<ActionResult<List<LookupItem>>> GetContactTypes()
        {
            var items = await _clientService.GetContactTypesAsync();
            return Ok(items);
        }
    }
}

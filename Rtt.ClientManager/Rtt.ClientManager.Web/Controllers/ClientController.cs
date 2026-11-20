using Microsoft.AspNetCore.Mvc;
using Rtt.ClientManager.Common.Models;
using Rtt.ClientManager.Web.Models;
using Rtt.ClientManager.Web.Services;

namespace Rtt.ClientManager.Web.Controllers
{
    public class ClientsController : Controller
    {
        private readonly IClientApiClient _apiClient;

        public ClientsController(IClientApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? search = null)
        {
            ViewBag.Search = search;
            var pagedClients = await _apiClient.GetClientsAsync(page, pageSize, search);
            return View(pagedClients);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = await BuildFormViewModelAsync(new ClientDto
            {
                Addresses = new List<AddressDto> { new() },
                ContactDetails = new List<ContactDetailDto> { new() }
            });
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientFormViewModel vm)
        {
            var (success, errors) = await _apiClient.CreateClientAsync(vm.Client);
            if (success)
            {
                TempData["SuccessMessage"] = "Client created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var err in errors)
                ModelState.AddModelError(string.Empty, err);

            var reloadedVm = await BuildFormViewModelAsync(vm.Client);
            return View("Form", reloadedVm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = await _apiClient.GetClientByIdAsync(id);
            if (client == null)
                return NotFound();

            if (client.Addresses.Count == 0) client.Addresses.Add(new AddressDto());
            if (client.ContactDetails.Count == 0) client.ContactDetails.Add(new ContactDetailDto());

            var vm = await BuildFormViewModelAsync(client);
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClientFormViewModel vm)
        {
            if (id != vm.Client.ClientId)
                return BadRequest();

            var (success, errors) = await _apiClient.UpdateClientAsync(vm.Client);
            if (success)
            {
                TempData["SuccessMessage"] = "Client updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var err in errors)
                ModelState.AddModelError(string.Empty, err);

            var reloadedVm = await BuildFormViewModelAsync(vm.Client);
            return View("Form", reloadedVm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _apiClient.DeleteClientAsync(id);
            TempData["SuccessMessage"] = "Client deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var stream = await _apiClient.DownloadClientsCsvAsync();
            string fileName = $"RTT_Clients_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(stream, "text/csv", fileName);
        }

        private async Task<ClientFormViewModel> BuildFormViewModelAsync(ClientDto client)
        {
            var addressTypesTask = _apiClient.GetAddressTypesAsync();
            var contactTypesTask = _apiClient.GetContactTypesAsync();
            await Task.WhenAll(addressTypesTask, contactTypesTask);

            return new ClientFormViewModel
            {
                Client = client,
                AddressTypes = addressTypesTask.Result,
                ContactTypes = contactTypesTask.Result
            };
        }
    }
}
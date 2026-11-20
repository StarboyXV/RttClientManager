using Rtt.ClientManager.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Rtt.ClientManager.Data.Repositories;


namespace Rtt.ClientManager.Bussiness.Services
{
    public interface IClientService
    {
        Task<PagedResult<ClientDto>> GetClientsAsync(int page, int pageSize, string? searchTerm);
        Task<ClientDto?> GetClientByIdAsync(int clientId);
        Task<int> CreateClientAsync(ClientDto client);
        Task<bool> UpdateClientAsync(ClientDto client);
        Task<bool> DeleteClientAsync(int clientId);
        Task<List<LookupItem>> GetAddressTypesAsync();
        Task<List<LookupItem>> GetContactTypesAsync();
    }

    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;
        private readonly ILookupRepository _lookupRepository;

        public ClientService(IClientRepository clientRepository, ILookupRepository lookupRepository)
        {
            _clientRepository = clientRepository;
            _lookupRepository = lookupRepository;
        }

        public Task<PagedResult<ClientDto>> GetClientsAsync(int page, int pageSize, string? searchTerm)
        {
            // Enforce sensible page bounds for performance on large datasets
            int safePage = page < 1 ? 1 : page;
            int safePageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            return _clientRepository.GetPagedClientsAsync(safePage, safePageSize, searchTerm);
        }
        public Task<ClientDto?> GetClientByIdAsync(int clientId)
        {
            return _clientRepository.GetClientByIdAsync(clientId);
        }

        public async Task<int> CreateClientAsync(ClientDto client)
        {
            ValidateClient(client);
            return await _clientRepository.CreateClientAsync(client);
        }

        public async Task<bool> UpdateClientAsync(ClientDto client)
        {
            if (client.ClientId <= 0)
            {
                throw new ValidationException(new List<string> { "A valid Client ID is required for updates." });
            }

            ValidateClient(client);
            return await _clientRepository.UpdateClientAsync(client);
        }

        public Task<bool> DeleteClientAsync(int clientId)
        {
            return _clientRepository.DeleteClientAsync(clientId);
        }

        public Task<List<LookupItem>> GetAddressTypesAsync()
        {
            return _lookupRepository.GetAddressTypesAsync();
        }

        public Task<List<LookupItem>> GetContactTypesAsync()
        {
            return _lookupRepository.GetContactTypesAsync();
        }

        private static void ValidateClient(ClientDto client)
        {
            var errors = new List<string>();

            if (client == null)
            {
                throw new ValidationException(new List<string> { "Client payload cannot be empty." });
            }

            if (string.IsNullOrWhiteSpace(client.FirstName))
                errors.Add("First Name is required.");

            if (string.IsNullOrWhiteSpace(client.LastName))
                errors.Add("Last Name is required.");

            if (string.IsNullOrWhiteSpace(client.Gender))
                errors.Add("Gender is required.");

            // Validate child addresses (remove completely blank rows if submitted from UI)
            if (client.Addresses != null)
            {
                client.Addresses = client.Addresses
                    .Where(a => !string.IsNullOrWhiteSpace(a.StreetAddress) || !string.IsNullOrWhiteSpace(a.City))
                    .ToList();

                for (int i = 0; i < client.Addresses.Count; i++)
                {
                    var addr = client.Addresses[i];
                    if (addr.AddressTypeId <= 0)
                        errors.Add($"Address #{i + 1}: Address Type is required.");
                    if (string.IsNullOrWhiteSpace(addr.StreetAddress))
                        errors.Add($"Address #{i + 1}: Street Address is required.");
                    if (string.IsNullOrWhiteSpace(addr.City))
                        errors.Add($"Address #{i + 1}: City is required.");
                    if (string.IsNullOrWhiteSpace(addr.PostalCode))
                        errors.Add($"Address #{i + 1}: Postal Code is required.");
                }
            }

            // Validate child contact details
            if (client.ContactDetails != null)
            {
                client.ContactDetails = client.ContactDetails
                    .Where(c => !string.IsNullOrWhiteSpace(c.ContactValue))
                    .ToList();

                for (int i = 0; i < client.ContactDetails.Count; i++)
                {
                    var contact = client.ContactDetails[i];
                    if (contact.ContactTypeId <= 0)
                        errors.Add($"Contact #{i + 1}: Contact Type is required.");
                    if (string.IsNullOrWhiteSpace(contact.ContactValue))
                        errors.Add($"Contact #{i + 1}: Contact Value is required.");
                }
            }

            if (errors.Count > 0)
            {
                throw new ValidationException(errors);
            }
        }
    }
}

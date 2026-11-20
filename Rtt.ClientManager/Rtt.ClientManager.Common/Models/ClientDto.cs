using System;
using System.Collections.Generic;
using System.Text;

namespace Rtt.ClientManager.Common.Models
{
    public class ClientDto
    {
        // Transfer object for Client

        public int ClientId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? IdNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime CreatedDate { get; set; }

        public List<AddressDto> Addresses { get; set; } = new();
        public List<ContactDetailDto> ContactDetails { get; set; } = new();
    }
}

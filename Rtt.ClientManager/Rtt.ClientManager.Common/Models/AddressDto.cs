using System;
using System.Collections.Generic;
using System.Text;

namespace Rtt.ClientManager.Common.Models
{
    public class AddressDto
    {
        // Transfer object for Address 
        public int AddressId { get; set; }
        public int ClientId { get; set; }
        public int AddressTypeId { get; set; }
        public string? AddressTypeName { get; set; }
        public string StreetAddress { get; set; } = string.Empty;
        public string? Suburb { get; set; }
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
    }
}

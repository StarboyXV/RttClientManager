using System;
using System.Collections.Generic;
using System.Text;

namespace Rtt.ClientManager.Common.Models
{
    public class ClientExportRow
    {
        // class properties are public so that they can be used in the CSV export
        public int ClientId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? IdNumber { get; set; }
        public string? AddressType { get; set; }
        public string? StreetAddress { get; set; }
        public string? Suburb { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
    }
}

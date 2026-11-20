using System;
using System.Collections.Generic;
using System.Text;

namespace Rtt.ClientManager.Common.Models
{
    public class ContactDetailDto
    {
        // transfer object for contact details
        public int ContactId { get; set; }
        public int ClientId { get; set; }
        public int ContactTypeId { get; set; }
        public string? ContactTypeName { get; set; }
        public string? ContactValue { get; set; } = string.Empty;
    }
}

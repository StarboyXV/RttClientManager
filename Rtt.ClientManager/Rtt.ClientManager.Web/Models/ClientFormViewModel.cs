using Rtt.ClientManager.Common.Models;

namespace Rtt.ClientManager.Web.Models
{
    public class ClientFormViewModel
    {
        public ClientDto Client { get; set; } = new();
        public List<LookupItem> AddressTypes { get; set; } = new();
        public List<LookupItem> ContactTypes { get; set; } = new();
    }
}
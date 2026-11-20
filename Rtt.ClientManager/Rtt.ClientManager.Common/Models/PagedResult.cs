using System;
using System.Collections.Generic;
using System.Text;

namespace Rtt.ClientManager.Common.Models
{
    public class PagedResult<T>
    {
        // pagination properties
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize); //calculates the total number of pages
    }
}

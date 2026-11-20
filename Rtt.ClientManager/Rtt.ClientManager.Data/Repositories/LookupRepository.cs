using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;
using Rtt.ClientManager.Common.Models;

namespace Rtt.ClientManager.Data.Repositories
{
    public interface ILookupRepository
    {
        Task<List<LookupItem>> GetAddressTypesAsync(); //Fetch reference data for address types
        Task<List<LookupItem>> GetContactTypesAsync(); // Fetch reference data for contact types

    }

    public class LookupRepository : ILookupRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public LookupRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<LookupItem>> GetAddressTypesAsync()
        {
            var items = new List<LookupItem>();
            const string sql = "SELECT AddressTypeId, Description FROM dbo.AddressTypes ORDER BY AddressTypeId;";

            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new LookupItem
                {
                    Id = reader.GetInt32(0),
                    Description = reader.GetString(1)
                });
            }
            return items;
        }

        public async Task<List<LookupItem>> GetContactTypesAsync()
        {
            var items = new List<LookupItem>();
            const string sql = "SELECT ContactTypeId, Description FROM dbo.ContactTypes ORDER BY ContactTypeId;";

            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new LookupItem
                {
                    Id = reader.GetInt32(0),
                    Description = reader.GetString(1)
                });
            }
            return items;
        }
    }
}

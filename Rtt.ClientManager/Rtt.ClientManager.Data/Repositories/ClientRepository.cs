using Microsoft.Data.SqlClient;
using Rtt.ClientManager.Common.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Rtt.ClientManager.Data.Repositories
{
    public interface IClientRepository
    {
        Task<PagedResult<ClientDto>> GetPagedClientsAsync(int page, int pageSize, string? searchTerm);
        Task<ClientDto?> GetClientByIdAsync(int clientId);
        Task<int> CreateClientAsync(ClientDto client);
        Task<bool> UpdateClientAsync(ClientDto client);
        Task<bool> DeleteClientAsync(int clientId);
        IAsyncEnumerable<ClientExportRow> StreamClientsWithAddressesForExportAsync();
    }

    public class ClientRepository : IClientRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ClientRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<PagedResult<ClientDto>> GetPagedClientsAsync(int page, int pageSize, string? searchTerm)
        {
            var result = new PagedResult<ClientDto>
            {
                Page = page < 1 ? 1 : page,
                PageSize = pageSize < 1 ? 20 : pageSize
            };

            int offset = (result.Page - 1) * result.PageSize;
            bool hasSearch = !string.IsNullOrWhiteSpace(searchTerm);
            string whereClause = hasSearch
                ? "WHERE LastName LIKE @Search OR FirstName LIKE @Search OR IdNumber LIKE @Search"
                : string.Empty;

            string countSql = $"SELECT COUNT(1) FROM dbo.Clients {whereClause};";
            string dataSql = $@"
                SELECT ClientId, FirstName, LastName, Gender, IdNumber, DateOfBirth, CreatedDate
                FROM dbo.Clients
                {whereClause}
                ORDER BY LastName ASC, FirstName ASC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            // 1. Get total record count for pagination controls
            await using (var countCmd = new SqlCommand(countSql, conn))
            {
                if (hasSearch)
                {
                    countCmd.Parameters.Add("@Search", SqlDbType.NVarChar, 102).Value = $"%{searchTerm!.Trim()}%";
                }
                result.TotalCount = (int)(await countCmd.ExecuteScalarAsync() ?? 0);
            }

            // 2. Fetch current page slice
            await using (var dataCmd = new SqlCommand(dataSql, conn))
            {
                if (hasSearch)
                {
                    dataCmd.Parameters.Add("@Search", SqlDbType.NVarChar, 102).Value = $"%{searchTerm!.Trim()}%";
                }
                dataCmd.Parameters.Add("@Offset", SqlDbType.Int).Value = offset;
                dataCmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = result.PageSize;

                await using var reader = await dataCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new ClientDto
                    {
                        ClientId = reader.GetInt32(0),
                        FirstName = reader.GetString(1),
                        LastName = reader.GetString(2),
                        Gender = reader.GetString(3),
                        IdNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                        DateOfBirth = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                        CreatedDate = reader.GetDateTime(6)
                    });
                }
            }

            return result;
        }

        public async Task<ClientDto?> GetClientByIdAsync(int clientId)
        {
            const string sql = @"
                SELECT ClientId, FirstName, LastName, Gender, IdNumber, DateOfBirth, CreatedDate
                FROM dbo.Clients WHERE ClientId = @ClientId;

                SELECT a.AddressId, a.ClientId, a.AddressTypeId, t.Description, a.StreetAddress, a.Suburb, a.City, a.PostalCode
                FROM dbo.Addresses a
                INNER JOIN dbo.AddressTypes t ON a.AddressTypeId = t.AddressTypeId
                WHERE a.ClientId = @ClientId;

                SELECT c.ContactId, c.ClientId, c.ContactTypeId, t.Description, c.ContactValue
                FROM dbo.ContactDetails c
                INNER JOIN dbo.ContactTypes t ON c.ContactTypeId = t.ContactTypeId
                WHERE c.ClientId = @ClientId;";

            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            var client = new ClientDto
            {
                ClientId = reader.GetInt32(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Gender = reader.GetString(3),
                IdNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                DateOfBirth = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                CreatedDate = reader.GetDateTime(6)
            };

            // Result Set 2: Addresses
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    client.Addresses.Add(new AddressDto
                    {
                        AddressId = reader.GetInt32(0),
                        ClientId = reader.GetInt32(1),
                        AddressTypeId = reader.GetInt32(2),
                        AddressTypeName = reader.GetString(3),
                        StreetAddress = reader.GetString(4),
                        Suburb = reader.IsDBNull(5) ? null : reader.GetString(5),
                        City = reader.GetString(6),
                        PostalCode = reader.GetString(7)
                    });
                }
            }

            // Result Set 3: Contact Details
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    client.ContactDetails.Add(new ContactDetailDto
                    {
                        ContactId = reader.GetInt32(0),
                        ClientId = reader.GetInt32(1),
                        ContactTypeId = reader.GetInt32(2),
                        ContactTypeName = reader.GetString(3),
                        ContactValue = reader.GetString(4)
                    });
                }
            }

            return client;
        }

        public async Task<int> CreateClientAsync(ClientDto client)
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                const string insertClientSql = @"
                    INSERT INTO dbo.Clients (FirstName, LastName, Gender, IdNumber, DateOfBirth)
                    OUTPUT INSERTED.ClientId
                    VALUES (@FirstName, @LastName, @Gender, @IdNumber, @DateOfBirth);";

                int newClientId;
                await using (var cmd = new SqlCommand(insertClientSql, conn, transaction))
                {
                    cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = client.FirstName;
                    cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = client.LastName;
                    cmd.Parameters.Add("@Gender", SqlDbType.NVarChar, 20).Value = client.Gender;
                    cmd.Parameters.Add("@IdNumber", SqlDbType.NVarChar, 20).Value = (object?)client.IdNumber ?? DBNull.Value;
                    cmd.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = (object?)client.DateOfBirth ?? DBNull.Value;

                    newClientId = (int)(await cmd.ExecuteScalarAsync())!;
                }

                await InsertAddressesAsync(conn, transaction, newClientId, client.Addresses);
                await InsertContactsAsync(conn, transaction, newClientId, client.ContactDetails);

                await transaction.CommitAsync();
                return newClientId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateClientAsync(ClientDto client)
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                const string updateClientSql = @"
                    UPDATE dbo.Clients
                    SET FirstName = @FirstName,
                        LastName = @LastName,
                        Gender = @Gender,
                        IdNumber = @IdNumber,
                        DateOfBirth = @DateOfBirth
                    WHERE ClientId = @ClientId;";

                int rowsAffected;
                await using (var cmd = new SqlCommand(updateClientSql, conn, transaction))
                {
                    cmd.Parameters.Add("@ClientId", SqlDbType.Int).Value = client.ClientId;
                    cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = client.FirstName;
                    cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = client.LastName;
                    cmd.Parameters.Add("@Gender", SqlDbType.NVarChar, 20).Value = client.Gender;
                    cmd.Parameters.Add("@IdNumber", SqlDbType.NVarChar, 20).Value = (object?)client.IdNumber ?? DBNull.Value;
                    cmd.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = (object?)client.DateOfBirth ?? DBNull.Value;

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }

                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Replace child collections cleanly inside the same transaction
                await using (var delAddrCmd = new SqlCommand("DELETE FROM dbo.Addresses WHERE ClientId = @ClientId;", conn, transaction))
                {
                    delAddrCmd.Parameters.Add("@ClientId", SqlDbType.Int).Value = client.ClientId;
                    await delAddrCmd.ExecuteNonQueryAsync();
                }

                await using (var delContCmd = new SqlCommand("DELETE FROM dbo.ContactDetails WHERE ClientId = @ClientId;", conn, transaction))
                {
                    delContCmd.Parameters.Add("@ClientId", SqlDbType.Int).Value = client.ClientId;
                    await delContCmd.ExecuteNonQueryAsync();
                }

                await InsertAddressesAsync(conn, transaction, client.ClientId, client.Addresses);
                await InsertContactsAsync(conn, transaction, client.ClientId, client.ContactDetails);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteClientAsync(int clientId)
        {
            const string sql = "DELETE FROM dbo.Clients WHERE ClientId = @ClientId;";

            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async IAsyncEnumerable<ClientExportRow> StreamClientsWithAddressesForExportAsync()
        {
            // SequentialAccess + IAsyncEnumerable streams 100,000+ rows with minimal RAM overhead
            const string sql = @"
                SELECT 
                    c.ClientId,
                    c.FirstName,
                    c.LastName,
                    c.Gender,
                    c.IdNumber,
                    t.Description AS AddressType,
                    a.StreetAddress,
                    a.Suburb,
                    a.City,
                    a.PostalCode
                FROM dbo.Clients c
                LEFT JOIN dbo.Addresses a ON c.ClientId = a.ClientId
                LEFT JOIN dbo.AddressTypes t ON a.AddressTypeId = t.AddressTypeId
                ORDER BY c.ClientId ASC, a.AddressId ASC;";

            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            while (await reader.ReadAsync())
            {
                yield return new ClientExportRow
                {
                    ClientId = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Gender = reader.GetString(3),
                    IdNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                    AddressType = reader.IsDBNull(5) ? null : reader.GetString(5),
                    StreetAddress = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Suburb = reader.IsDBNull(7) ? null : reader.GetString(7),
                    City = reader.IsDBNull(8) ? null : reader.GetString(8),
                    PostalCode = reader.IsDBNull(9) ? null : reader.GetString(9)
                };
            }
        }

        private static async Task InsertAddressesAsync(SqlConnection conn, SqlTransaction tx, int clientId, List<AddressDto> addresses)
        {
            if (addresses == null || addresses.Count == 0) return;

            const string sql = @"
                INSERT INTO dbo.Addresses (ClientId, AddressTypeId, StreetAddress, Suburb, City, PostalCode)
                VALUES (@ClientId, @AddressTypeId, @StreetAddress, @Suburb, @City, @PostalCode);";

            foreach (var addr in addresses)
            {
                await using var cmd = new SqlCommand(sql, conn, tx);
                cmd.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;
                cmd.Parameters.Add("@AddressTypeId", SqlDbType.Int).Value = addr.AddressTypeId;
                cmd.Parameters.Add("@StreetAddress", SqlDbType.NVarChar, 250).Value = addr.StreetAddress;
                cmd.Parameters.Add("@Suburb", SqlDbType.NVarChar, 100).Value = (object?)addr.Suburb ?? DBNull.Value;
                cmd.Parameters.Add("@City", SqlDbType.NVarChar, 100).Value = addr.City;
                cmd.Parameters.Add("@PostalCode", SqlDbType.NVarChar, 20).Value = addr.PostalCode;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task InsertContactsAsync(SqlConnection conn, SqlTransaction tx, int clientId, List<ContactDetailDto> contacts)
        {
            if (contacts == null || contacts.Count == 0) return;

            const string sql = @"
                INSERT INTO dbo.ContactDetails (ClientId, ContactTypeId, ContactValue)
                VALUES (@ClientId, @ContactTypeId, @ContactValue);";

            foreach (var contact in contacts)
            {
                await using var cmd = new SqlCommand(sql, conn, tx);
                cmd.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;
                cmd.Parameters.Add("@ContactTypeId", SqlDbType.Int).Value = contact.ContactTypeId;
                cmd.Parameters.Add("@ContactValue", SqlDbType.NVarChar, 150).Value = contact.ContactValue;
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Rtt.ClientManager.Data.Repositories;

namespace Rtt.ClientManager.Bussiness.Services
{
    public interface ICsvExportService
    {
        Task WriteClientsWithAddressesCsvAsync(Stream outputStream, CancellationToken cancellationToken = default);
    }

    public class CsvExportService : ICsvExportService
    {
        private readonly IClientRepository _clientRepository;

        public CsvExportService(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public async Task WriteClientsWithAddressesCsvAsync(Stream outputStream, CancellationToken cancellationToken = default)
        {
            // leaveOpen: true ensures ASP.NET Core manages closing the HTTP response stream cleanly
            await using var writer = new StreamWriter(outputStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), bufferSize: 65536, leaveOpen: true);

            // Header row (strictly excludes contact details per requirements)
            await writer.WriteLineAsync("ClientId,FirstName,LastName,Gender,IdNumber,AddressType,StreetAddress,Suburb,City,PostalCode");

            await foreach (var row in _clientRepository.StreamClientsWithAddressesForExportAsync().WithCancellation(cancellationToken))
            {
                var line = string.Join(",",
                    row.ClientId.ToString(),
                    EscapeCsvField(row.FirstName),
                    EscapeCsvField(row.LastName),
                    EscapeCsvField(row.Gender),
                    EscapeCsvField(row.IdNumber),
                    EscapeCsvField(row.AddressType),
                    EscapeCsvField(row.StreetAddress),
                    EscapeCsvField(row.Suburb),
                    EscapeCsvField(row.City),
                    EscapeCsvField(row.PostalCode)
                );

                await writer.WriteLineAsync(line);
            }

            await writer.FlushAsync(cancellationToken);
        }

        private static string EscapeCsvField(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Quote fields containing commas, double quotes, or line breaks
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }
    }
}

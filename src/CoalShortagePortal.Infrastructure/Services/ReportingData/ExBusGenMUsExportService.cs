using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.Core.Interfaces.ReportingData;
using CoalShortagePortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoalShortagePortal.Infrastructure.Services.ReportingData
{
    public class ExBusGenMUsExportService : IExBusGenMUsExportService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public ExBusGenMUsExportService(ILogger<ExBusGenMUsExportService> logger, IConfiguration configuration, ApplicationDbContext context)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        private class ScadaCellMappingModel
        {
            public int Id { get; set; }

            public string GeneratingStationName { get; set; }

            public string StationTypeName { get; set; }

            public int? Fuel { get; set; }

            public string FuelType { get; set; }

            public string CellReference { get; set; }
        }

        public async Task GenerateAndUploadFilesAsync()
        {
            try
            {
                ExcelPackage.License.SetNonCommercialPersonal("CoalShortagePortal");

                var today = DateTime.Now.Date;

                // 24th July -> export 23rd July data
                var dataDate = today.AddDays(-1);

                var submissions =
                    await GetYesterdaySubmissionsAsync(dataDate);

                var scadaMappings =
                    await GetScadaCellMappingsAsync();

                var genMuBytes =
                    GenerateCellReferenceExcel(
                        submissions,
                        scadaMappings,
                        "GenMU",
                        d => d.DailyMUs,
                        Color.FromArgb(255, 253, 231));

                var exBusBytes =
                    GenerateCellReferenceExcel(
                        submissions,
                        scadaMappings,
                        "ExBus",
                        d => d.ExBus,
                        Color.FromArgb(232, 245, 233));


                string rootFolder =
                    _configuration["ExportSettings:ExBusGenMuExportRootPath"];

                string targetFolder = Path.Combine(
                    rootFolder,
                    today.ToString("yyyy"),
                    today.ToString("MMMM").ToUpper(),
                    today.ToString("dd"));


                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }


                string genMuFile = Path.Combine(
                    targetFolder,
                    $"GenMU_{dataDate:dd_MM_yyyy}.xlsx");

                string exBusFile = Path.Combine(
                    targetFolder,
                    $"ExBus_{dataDate:dd_MM_yyyy}.xlsx");

                await File.WriteAllBytesAsync(
                    genMuFile,
                    genMuBytes);

                await File.WriteAllBytesAsync(
                    exBusFile,
                    exBusBytes);

                _logger.LogInformation(
                    "SCADA export completed successfully. GenMU: {GenMU}, ExBus: {ExBus}",
                    genMuFile,
                    exBusFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error generating GenMU and ExBus files");
            }
        }

        private byte[] GenerateCellReferenceExcel(
            List<DailyMUsData> submissions,
            List<ScadaCellMappingModel> scadaMappings,
            string sheetName,
            Func<DailyMUsData, float?> valueSelector,
            Color valueRowColor)
        {
            using var package = new ExcelPackage();

            var ws = package.Workbook.Worksheets.Add(sheetName);

            var submissionMap = submissions
                .GroupBy(s => NormalizeStationName(s.StationName))
                .ToDictionary(
                    g => g.Key,
                    g => g.First());

            int maxColumnUsed = 1;

            foreach (var mapping in scadaMappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.GeneratingStationName))
                    continue;

                if (string.IsNullOrWhiteSpace(mapping.CellReference))
                    continue;

                string stationKey =
                    NormalizeStationName(mapping.GeneratingStationName);

                submissionMap.TryGetValue(
                    stationKey,
                    out var submission);

                string columnLetters =
                    ExtractColumnLetters(mapping.CellReference);

                if (string.IsNullOrWhiteSpace(columnLetters))
                    continue;

                int columnNumber =
                    ExcelColumnNameToNumber(columnLetters);

                if (columnNumber <= 0)
                    continue;

                maxColumnUsed = Math.Max(maxColumnUsed, columnNumber);

                // Row 1: Station name at Oracle CELL_REFERENCE column
                var stationCell = ws.Cells[1, columnNumber];
                stationCell.Value = mapping.GeneratingStationName;
                stationCell.Style.Font.Bold = true;
                stationCell.Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                stationCell.Style.VerticalAlignment =
                    ExcelVerticalAlignment.Center;
                stationCell.Style.Fill.PatternType =
                    ExcelFillStyle.Solid;
                stationCell.Style.Fill.BackgroundColor.SetColor(
                    Color.FromArgb(220, 220, 245));
                stationCell.Style.Border.BorderAround(
                    ExcelBorderStyle.Thin);

                // Row 2: GenMU / ExBus value at same Oracle CELL_REFERENCE column
                var valueCell = ws.Cells[2, columnNumber];

                if (submission != null)
                {
                    var mappedValue = valueSelector(submission);

                    if (mappedValue.HasValue)
                    {
                        valueCell.Value = mappedValue.Value;
                    }
                    else
                    {
                        valueCell.Value = null; // leave blank if value itself is null
                    }
                }
                else
                {
                    valueCell.Value = null; // leave blank if station has no portal data
                }

                valueCell.Style.Numberformat.Format = "#,##0.00";
                valueCell.Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                valueCell.Style.VerticalAlignment =
                    ExcelVerticalAlignment.Center;
                valueCell.Style.Fill.PatternType =
                    ExcelFillStyle.Solid;
                valueCell.Style.Fill.BackgroundColor.SetColor(valueRowColor);
                valueCell.Style.Border.BorderAround(
                    ExcelBorderStyle.Thin);
            }

            if (ws.Dimension != null)
            {
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
            }

            for (int c = 1; c <= maxColumnUsed; c++)
            {
                ws.Column(c).Width = Math.Max(ws.Column(c).Width, 12);
            }

            ws.Row(1).Height = 22;
            ws.Row(2).Height = 22;

            return package.GetAsByteArray();
        }

        private async Task<List<DailyMUsData>> GetYesterdaySubmissionsAsync(DateTime yesterday)
        {
            return await _context.DailyMUsDatas
                .Where(d => d.DataDate.Date == yesterday
                         && !d.StationName.StartsWith("wr_"))
                .OrderBy(d => d.StationName)
                .ToListAsync();
        }

        private static string NormalizeStationName(string stationName)
        {
            if (string.IsNullOrWhiteSpace(stationName))
                return string.Empty;

            return stationName
                .Trim()
                .ToUpperInvariant();
        }

        private static string ExtractColumnLetters(string cellReference)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
                return string.Empty;

            var cleaned = cellReference
                .Trim()
                .Replace("$", "")
                .ToUpperInvariant();

            var match = Regex.Match(cleaned, "^[A-Z]+");

            return match.Success ? match.Value : string.Empty;
        }

        private static int ExcelColumnNameToNumber(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return 0;

            columnName = columnName.Trim().ToUpperInvariant();

            int sum = 0;

            foreach (char c in columnName)
            {
                if (c < 'A' || c > 'Z')
                    return 0;

                sum *= 26;
                sum += c - 'A' + 1;
            }

            return sum;
        }

        private async Task<List<ScadaCellMappingModel>> GetScadaCellMappingsAsync()
        {
            var result = new List<ScadaCellMappingModel>();

            var connectionString =
                _configuration.GetConnectionString("ReportingConnection");

            const string query = @"
SELECT
    gs.ID,
    gs.GENERATING_STATION_NAME,
    gst.NAME AS STATION_TYPE_NAME,
    gs.FUEL,
    f.TYPE AS FUEL_TYPE,
    scd.CELL_REFERENCE
FROM REPORTING_WEB_UI_UAT.GENERATING_STATION gs
INNER JOIN REPORTING_WEB_UI_UAT.SCADA_CONFIG_DETAILS scd
    ON scd.ELMENT_ID = gs.ID
LEFT JOIN REPORTING_WEB_UI_UAT.FUEL f
    ON f.ID = gs.FUEL
LEFT JOIN REPORTING_WEB_UI_UAT.GENERATING_STATION_TYPE gst
    ON gst.ID = gs.STATION_TYPE
WHERE scd.SCADA_FILE_NAME_ID = 7
ORDER BY gs.GENERATING_STATION_NAME";

            using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync();

            using var command = new OracleCommand(query, connection);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var model = new ScadaCellMappingModel
                {
                    Id = reader["ID"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(reader["ID"]),

                    GeneratingStationName = reader["GENERATING_STATION_NAME"] == DBNull.Value
                        ? ""
                        : reader["GENERATING_STATION_NAME"].ToString(),

                    StationTypeName = reader["STATION_TYPE_NAME"] == DBNull.Value
                        ? ""
                        : reader["STATION_TYPE_NAME"].ToString(),

                    Fuel = reader["FUEL"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["FUEL"]),

                    FuelType = reader["FUEL_TYPE"] == DBNull.Value
                        ? ""
                        : reader["FUEL_TYPE"].ToString(),

                    CellReference = reader["CELL_REFERENCE"] == DBNull.Value
                        ? ""
                        : reader["CELL_REFERENCE"].ToString()
                };

                result.Add(model);
            }

            return result;
        }
    }
}

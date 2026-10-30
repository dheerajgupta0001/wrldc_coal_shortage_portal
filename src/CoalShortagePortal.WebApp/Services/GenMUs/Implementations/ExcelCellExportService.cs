using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CoalShortagePortal.Application.Features.GenMUs.Helpers;
using CoalShortagePortal.Application.Features.GenMUs.Interfaces;
using CoalShortagePortal.Application.Features.GenMUs.Models;
using CoalShortagePortal.Core.Entities;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CoalShortagePortal.WebApp.Services.GenMUs.Implementations
{
    public class ExcelCellExportService : IExcelCellExportService
    {
        public byte[] GenerateCellReferenceExcel(
            List<DailyMUsData> submissions,
            List<ScadaCellMappingModel> scadaMappings,
            string sheetName,
            Func<DailyMUsData, float?> valueSelector,
            Color valueRowColor)
        {
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add(sheetName);

                var submissionMap = submissions
                    .GroupBy(s =>
                        StationNameHelper.NormalizeStationName(
                            s.StationName))
                    .ToDictionary(
                        g => g.Key,
                        g => g.First());

                int maxColumnUsed = 1;

                foreach (var mapping in scadaMappings)
                {
                    if (string.IsNullOrWhiteSpace(
                        mapping.GeneratingStationName))
                        continue;

                    if (string.IsNullOrWhiteSpace(mapping.CellReference))
                        continue;

                    string stationKey =
                        StationNameHelper.NormalizeStationName(
                            mapping.GeneratingStationName);

                    DailyMUsData submission;
                    submissionMap.TryGetValue(
                        stationKey,
                        out submission);

                    string columnLetters =
                        StationNameHelper.ExtractColumnLetters(
                            mapping.CellReference);

                    if (string.IsNullOrWhiteSpace(columnLetters))
                        continue;

                    int columnNumber =
                        StationNameHelper.ExcelColumnNameToNumber(
                            columnLetters);

                    if (columnNumber <= 0)
                        continue;

                    maxColumnUsed =
                        Math.Max(maxColumnUsed, columnNumber);

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
                            valueCell.Value = null;
                        }
                    }
                    else
                    {
                        valueCell.Value = null;
                    }

                    valueCell.Style.Numberformat.Format = "#,##0.00";
                    valueCell.Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    valueCell.Style.VerticalAlignment =
                        ExcelVerticalAlignment.Center;
                    valueCell.Style.Fill.PatternType =
                        ExcelFillStyle.Solid;
                    valueCell.Style.Fill.BackgroundColor.SetColor(
                        valueRowColor);
                    valueCell.Style.Border.BorderAround(
                        ExcelBorderStyle.Thin);
                }

                if (ws.Dimension != null)
                {
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                }

                for (int c = 1; c <= maxColumnUsed; c++)
                {
                    ws.Column(c).Width =
                        Math.Max(ws.Column(c).Width, 12);
                }

                ws.Row(1).Height = 22;
                ws.Row(2).Height = 22;

                return package.GetAsByteArray();
            }
        }
    }
}
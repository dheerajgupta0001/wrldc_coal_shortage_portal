using System;
using System.Collections.Generic;
using System.Drawing;
using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.Application.Features.GenMUs.Models;

namespace CoalShortagePortal.Application.Features.GenMUs.Interfaces
{
    public interface IExcelCellExportService
    {
        byte[] GenerateCellReferenceExcel(
            List<DailyMUsData> submissions,
            List<ScadaCellMappingModel> scadaMappings,
            string sheetName,
            Func<DailyMUsData, float?> valueSelector,
            Color valueRowColor);
    }
}
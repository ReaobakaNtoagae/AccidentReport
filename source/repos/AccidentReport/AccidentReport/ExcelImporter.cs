using System;
using System.Collections.Generic;
using ClosedXML.Excel;

namespace AccidentReport
{
    public static class ExcelImporter
    {
        // Column indices (0-based)
        private const int COL_STATION = 0;
        private const int COL_ARNO = 1;
        private const int COL_DATE = 2;
        private const int COL_DAY = 3;
        private const int COL_TIME = 4;
        private const int COL_ROUTE = 5;
        private const int COL_LOCATION = 6;
        private const int COL_TYPE = 7;
        
        private const int COL_VEHICLES = 20;

        public static List<AccidentReportModel> Parse(string filePath)
        {
            var reports = new List<AccidentReportModel>();

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheet(1);

            
            foreach (var row in ws.RowsUsed())
            {
                if (row.RowNumber() <= 2) continue;

                string station = Cell(row, COL_STATION);
                if (string.IsNullOrWhiteSpace(station)) continue;
                if (station.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase)) continue;

              
                string arRaw = Cell(row, COL_ARNO);
                if (string.IsNullOrWhiteSpace(arRaw)) continue;

                var report = new AccidentReportModel
                {
                    SAPSStation = station.Trim(),
                    ARNumber = arRaw.Trim(),
                    DayOfWeek = Cell(row, COL_DAY).Trim(),
                    AccidentTime = DateTime.TryParse(Cell(row, COL_TIME).Trim(), out var t) ? t : DateTime.MinValue ,
                    Route = Cell(row, COL_ROUTE).Trim(),
                    Location = Cell(row, COL_LOCATION).Trim(),
                    AccidentType = Cell(row, COL_TYPE).Trim(),
                    VehiclesInvolved = Cell(row, COL_VEHICLES).Trim(),
                    CreatedAt = DateTime.Now,

                    // Fatal
                    FatalDrivers = IntCell(row, 8),
                    FatalPassengers = IntCell(row, 9),
                    FatalPedestrians = IntCell(row, 10),
                    FatalCyclists = IntCell(row, 11),
                    // Serious
                    SeriousDrivers = IntCell(row, 12),
                    SeriousPassengers = IntCell(row, 13),
                    SeriousPedestrians = IntCell(row, 14),
                    SeriousCyclists = IntCell(row, 15),
                    // Slight
                    SlightDrivers = IntCell(row, 16),
                    SlightPassengers = IntCell(row, 17),
                    SlightPedestrians = IntCell(row, 18),
                    SlightCyclists = IntCell(row, 19),
                };

                
                string dateStr = Cell(row, COL_DATE).Trim();
                if (DateTime.TryParseExact(dateStr + "/2025", "dd/MM/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime dt))
                    report.AccidentDate = dt;
                else
                    report.AccidentDate = DateTime.Today;

                reports.Add(report);
            }

            return reports;
        }

        private static string Cell(IXLRow row, int zeroBasedCol)
        {
            var c = row.Cell(zeroBasedCol + 1);
            return c.IsEmpty() ? "" : c.GetValue<string>() ?? "";
        }

        private static int IntCell(IXLRow row, int zeroBasedCol)
        {
            var c = row.Cell(zeroBasedCol + 1);
            if (c.IsEmpty()) return 0;
            return c.TryGetValue<int>(out int v) ? v : 0;
        }
    }
}
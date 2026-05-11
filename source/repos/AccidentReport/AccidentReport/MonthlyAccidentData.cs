using DevExpress.Map.Native;
using DevExpress.XtraScheduler.Reporting.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccidentReport
{
    public class MonthlyAccidentData
    {
        public int PreviousYearCrashes { get; set; }
        public int CurrentYearCrashes { get; set; }
        public int PreviousYearFatalities { get; set; }
        public int CurrentYearFatalities { get; set; }
        public int PreviousYearSerious { get; set; }
        public int CurrentYearSerious { get; set; }

        public int PreviousYearSlight { get; set; }

        public int CurrentYearSlight { get; set; }

        public double CrashesVariation => CalculateVariation(PreviousYearCrashes, CurrentYearCrashes);
        public double FatalitiesVariation => CalculateVariation(PreviousYearFatalities, CurrentYearFatalities);
        public double SeriousVariation => CalculateVariation(PreviousYearSerious, CurrentYearSerious);
        public double SlightVariation => CalculateVariation(PreviousYearSlight, CurrentYearSlight);

        /*private double CalculateVariation(int previous, int current)
        {
            if (previous == 0) return current == 0 ? 0 : 100;
            return ((double)(current - previous) / previous) * 100;
        }*/

        public Dictionary<string, DistrictData> Districts { get; set; } = new();

        public VictimData FatalVictims { get; set; } = new();
        public VictimData SeriousVictims { get; set; } = new();
        public VictimData SlightVictims { get; set; } = new();

        public Dictionary<string, TimeData> TimeAnalysis { get; set; } = new();

        public Dictionary<string, DayData> DayAnalysis { get; set; } = new();

        public Dictionary<string, VehicleData> VehicleCategories { get; set; } = new();

        public Dictionary<string, AccidentTypeData> AccidentTypes { get; set; } = new();

        public List<RouteData> ProblematicRoutes { get; set; } = new();

        public MonthlyAccidentData(List<AccidentReportModel> reports)
        {
            ProcessData(reports);
        }

        private void ProcessData(List<AccidentReportModel> reports)
        {
            // Data processing logic to populate the properties based on the reports
            // This is where you would implement the logic to calculate all the metrics and analyses
            var currentYear = reports.First().AccidentDate.Year;
            var previousYear = currentYear - 1;
            var currentMonth = reports.First().AccidentDate.Month;

            var currentYearReports = reports.Where(r => r.AccidentDate.Year == currentYear && r.AccidentDate.Month == currentMonth).ToList();

            var previousYearReports = DatabaseHelper.GetReports(
                new DateTime(previousYear, currentMonth, 1),
                new DateTime(previousYear, currentMonth, 1).AddMonths(1).AddDays(-1),
                null, null).ToList();

            CurrentYearCrashes = currentYearReports.Count;
            PreviousYearCrashes = previousYearReports.Count;

            CurrentYearFatalities = currentYearReports.Sum(r => r.TotalFatal);
            PreviousYearFatalities = previousYearReports.Sum(r => r.TotalFatal);

            CurrentYearSerious = currentYearReports.Sum(r => r.TotalSerious);
            PreviousYearSerious = previousYearReports.Sum(r => r.TotalSerious);

            CurrentYearSlight = currentYearReports.Sum(r => r.TotalSlight);
            PreviousYearSlight = previousYearReports.Sum(r => r.TotalSlight);

            FatalVictims = new VictimData
            {
                Drivers = currentYearReports.Sum(r => r.FatalDrivers),
                Passengers = currentYearReports.Sum(r => r.FatalPassengers),
                Pedestrians = currentYearReports.Sum(r => r.FatalPedestrians),
                Cyclists = currentYearReports.Sum(r => r.FatalCyclists)
            };

            SeriousVictims = new VictimData
            {
                Drivers = currentYearReports.Sum(r => r.SeriousDrivers),
                Passengers = currentYearReports.Sum(r => r.SeriousPassengers),
                Pedestrians = currentYearReports.Sum(r => r.SeriousPedestrians),
                Cyclists = currentYearReports.Sum(r => r.SeriousCyclists)
            };

            SlightVictims = new VictimData
            {
                Drivers = currentYearReports.Sum(r => r.SlightDrivers),
                Passengers = currentYearReports.Sum(r => r.SlightPassengers),
                Pedestrians = currentYearReports.Sum(r => r.SlightPedestrians),
                Cyclists = currentYearReports.Sum(r => r.SlightCyclists)
            };

            TimeAnalysis = currentYearReports


                .GroupBy(r => GetTimeCategory(r?.AccidentTime))
                .ToDictionary(g => g.Key, g => new TimeData
                {
                    Crashes = g.Count(),
                    Fatalities = g.Sum(r => r.TotalFatal)
                });


            DayAnalysis = currentYearReports
                 .GroupBy(r => r.DayOfWeek)
                 .ToDictionary(g => g.Key, g => new DayData
                 {
                     Crashes = g.Count(),
                     Fatalities = g.Sum(r => r.TotalFatal)
                 });
        }


        private static string GetTimeCategory(DateTime? time)
        {
            if (time == DateTime.MinValue) return "Unknown";

            int hour = time.Value.Hour;

            if (hour >= 6 && hour < 14) return "06:00-14:00";
            if (hour >= 14 && hour < 22) return "14:00-22:00";
            return "22:00-06:00";

        }

        private static double CalculateVariation(int previous, int current)
        {
            if (previous == 0) return current > 0 ? 100 : 0; // Return 100% increase if previous is 0 and current is not
            return Math.Round((double)(current - previous) / previous * 100, 2);
        }

        public class MonthlyReportGenerator
        {
            private readonly MonthlyAccidentData data;
            private readonly DateTime reportDate;

            public MonthlyReportGenerator(MonthlyAccidentData data, DateTime reportDate)
            {
                this.data = data;
                this.reportDate = reportDate;
            }

            public string Generate()
            {
                var sb = new System.Text.StringBuilder();

                // Header
                sb.AppendLine("=".PadRight(80, '='));
                sb.AppendLine($"MONTHLY ACCIDENT REPORT: {reportDate:MMMM yyyy}");
                sb.AppendLine($"Generated: {DateTime.Now:dd MMMM yyyy HH:mm}");
                sb.AppendLine("=".PadRight(80, '='));
                sb.AppendLine();

                // Executive Summary
                sb.AppendLine("EXECUTIVE SUMMARY");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine($"During {reportDate:MMMM yyyy}, there were {data.CurrentYearCrashes:N0} road crashes, ");
                sb.AppendLine($"resulting in {data.CurrentYearFatalities:N0} fatalities, {data.CurrentYearSerious:N0} serious injuries ");
                sb.AppendLine($"and {data.CurrentYearSlight:N0} slight injuries.");
                sb.AppendLine();

                // Comparison table
                sb.AppendLine("COMPARISON WITH PREVIOUS YEAR");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine($"{"Metric",-20} {"Previous Year",-15} {"Current Year",-15} {"Variation",-15}");
                sb.AppendLine($"{"-",-20} {"-",-15} {"-",-15} {"-",-15}");
                sb.AppendLine($"{"CRASHES",-20} {data.PreviousYearCrashes,-15} {data.CurrentYearCrashes,-15} {data.CrashesVariation:+0.00;-0.00}%");
                sb.AppendLine($"{"FATALITIES",-20} {data.PreviousYearFatalities,-15} {data.CurrentYearFatalities,-15} {data.FatalitiesVariation:+0.00;-0.00}%");
                sb.AppendLine($"{"SERIOUS INJURIES",-20} {data.PreviousYearSerious,-15} {data.CurrentYearSerious,-15} {data.SeriousVariation:+0.00;-0.00}%");
                sb.AppendLine($"{"SLIGHT INJURIES",-20} {data.PreviousYearSlight,-15} {data.CurrentYearSlight,-15} {data.SlightVariation:+0.00;-0.00}%");
                sb.AppendLine();

                // Daily Averages
                int daysInMonth = DateTime.DaysInMonth(reportDate.Year, reportDate.Month);
                sb.AppendLine("DAILY AVERAGES");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine($"Average Crashes per Day: {data.CurrentYearCrashes / (double)daysInMonth:F1}");
                sb.AppendLine($"Average Fatalities per Day: {data.CurrentYearFatalities / (double)daysInMonth:F1}");
                sb.AppendLine($"Average Serious Injuries per Day: {data.CurrentYearSerious / (double)daysInMonth:F1}");
                sb.AppendLine($"Average Slight Injuries per Day: {data.CurrentYearSlight / (double)daysInMonth:F1}");
                sb.AppendLine();

                // Victim Categories
                sb.AppendLine("VICTIM CATEGORIES");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine();
                sb.AppendLine("FATALITIES:");
                sb.AppendLine($"  Drivers: {data.FatalVictims.Drivers}");
                sb.AppendLine($"  Passengers: {data.FatalVictims.Passengers}");
                sb.AppendLine($"  Pedestrians: {data.FatalVictims.Pedestrians}");
                sb.AppendLine($"  Cyclists: {data.FatalVictims.Cyclists}");
                sb.AppendLine($"  TOTAL FATAL: {data.FatalVictims.Total}");
                sb.AppendLine();

                sb.AppendLine("SERIOUS INJURIES:");
                sb.AppendLine($"  Drivers: {data.SeriousVictims.Drivers}");
                sb.AppendLine($"  Passengers: {data.SeriousVictims.Passengers}");
                sb.AppendLine($"  Pedestrians: {data.SeriousVictims.Pedestrians}");
                sb.AppendLine($"  Cyclists: {data.SeriousVictims.Cyclists}");
                sb.AppendLine($"  TOTAL SERIOUS: {data.SeriousVictims.Total}");
                sb.AppendLine();

                sb.AppendLine("SLIGHT INJURIES:");
                sb.AppendLine($"  Drivers: {data.SlightVictims.Drivers}");
                sb.AppendLine($"  Passengers: {data.SlightVictims.Passengers}");
                sb.AppendLine($"  Pedestrians: {data.SlightVictims.Pedestrians}");
                sb.AppendLine($"  Cyclists: {data.SlightVictims.Cyclists}");
                sb.AppendLine($"  TOTAL SLIGHT: {data.SlightVictims.Total}");
                sb.AppendLine();

                // Time Analysis
                sb.AppendLine("TIME ANALYSIS");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine($"{"Time Period",-20} {"Crashes",-15} {"Fatalities",-15}");
                sb.AppendLine($"{"-",-20} {"-",-15} {"-",-15}");
                foreach (var time in data.TimeAnalysis.OrderBy(t => t.Key))
                {
                    sb.AppendLine($"{time.Key,-20} {time.Value.Crashes,-15} {time.Value.Fatalities,-15}");
                }
                sb.AppendLine();

                // Day of Week Analysis
                var orderedDays = new[] { "M", "TU", "W", "TH", "FR", "SA", "SU" };
                sb.AppendLine("DAY OF WEEK ANALYSIS");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine($"{"Day",-10} {"Crashes",-15} {"Fatalities",-15}");
                sb.AppendLine($"{"-",-10} {"-",-15} {"-",-15}");
                foreach (var day in orderedDays)
                {
                    if (data.DayAnalysis.ContainsKey(day))
                    {
                        sb.AppendLine($"{day,-10} {data.DayAnalysis[day].Crashes,-15} {data.DayAnalysis[day].Fatalities,-15}");
                    }
                }
                sb.AppendLine();

                // Conclusion
                sb.AppendLine("CONCLUSION");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine($"• Crashes {(data.CrashesVariation >= 0 ? "increased" : "decreased")} by {Math.Abs(data.CrashesVariation):F1}% compared to previous year");
                sb.AppendLine($"• Fatalities {(data.FatalitiesVariation >= 0 ? "increased" : "decreased")} by {Math.Abs(data.FatalitiesVariation):F1}%");
                sb.AppendLine($"• High-risk times: 14:00 - 06:00 accounts for majority of fatalities");
                sb.AppendLine($"• Recommended: Increase traffic enforcement during high-risk periods");
                sb.AppendLine();

                sb.AppendLine("=".PadRight(80, '='));
                sb.AppendLine("END OF REPORT");
                sb.AppendLine("=".PadRight(80, '='));

                return sb.ToString();
            }
        }
    }
}

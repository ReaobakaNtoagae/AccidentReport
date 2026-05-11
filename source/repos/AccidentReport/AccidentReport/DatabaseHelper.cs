using DevExpress.XtraCharts.Sankey;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace AccidentReport
{


    public static class DatabaseHelper
    {

        public static string ConnectionString =
            "Server=REAA;Database=VehicleAccidents;Trusted_Connection=True;TrustServerCertificate=True;";


        public static void EnsureTable()
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AccidentReports' AND xtype='U')
                CREATE TABLE AccidentReports (
                    Id                  INT IDENTITY(1,1) PRIMARY KEY,
                    SAPSStation         NVARCHAR(100),
                    ARNumber            NVARCHAR(50),
                    AccidentDate        DATE,
                    DayOfWeek           NVARCHAR(5),
                    AccidentTime        NVARCHAR(10),
                    Route               NVARCHAR(50),
                    Location            NVARCHAR(200),
                    AccidentType        NVARCHAR(100),
                    ProvinceName       NVARCHAR(255) NOT NULL,
                    DistrictMunicipalityName NVARCHAR(255) NOT NULL,
                    FatalDrivers        INT NOT NULL DEFAULT 0,
                    FatalPassengers     INT NOT NULL DEFAULT 0,
                    FatalPedestrians    INT NOT NULL DEFAULT 0,
                    FatalCyclists       INT NOT NULL DEFAULT 0,
                    SeriousDrivers      INT NOT NULL DEFAULT 0,
                    SeriousPassengers   INT NOT NULL DEFAULT 0,
                    SeriousPedestrians  INT NOT NULL DEFAULT 0,
                    SeriousCyclists     INT NOT NULL DEFAULT 0,
                    SlightDrivers       INT NOT NULL DEFAULT 0,
                    SlightPassengers    INT NOT NULL DEFAULT 0,
                    SlightPedestrians   INT NOT NULL DEFAULT 0,
                    SlightCyclists      INT NOT NULL DEFAULT 0,
                    VehiclesInvolved    NVARCHAR(200),
                    CreatedAt           DATETIME DEFAULT GETDATE()
                )", conn);
            cmd.ExecuteNonQuery();
        }


        public static void InsertReport(AccidentReportModel r)
        {

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(@"
                INSERT INTO AccidentReports (
                    SAPSStation, ARNumber, AccidentDate, DayOfWeek, AccidentTime,
                    Route, Location, AccidentType, ProvinceName, DistrictMunicipalityName,
                    FatalDrivers, FatalPassengers, FatalPedestrians, FatalCyclists,
                    SeriousDrivers, SeriousPassengers, SeriousPedestrians, SeriousCyclists,
                    SlightDrivers, SlightPassengers, SlightPedestrians, SlightCyclists,
                    VehiclesInvolved, CreatedAt
                ) VALUES (
                    @Station, @ARNo, @Date, @Day, @Time, @Route, @Location, @Type, @Province, @District,
                    @FD, @FP, @FPD, @FC,
                    @SD, @SP, @SPD, @SC,
                    @SLD, @SLP, @SLPD, @SLC,
                    @Vehicles, @CreatedAt
                )", conn);

            cmd.Parameters.AddWithValue("@Station", r.SAPSStation ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ARNo", r.ARNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Date", r.AccidentDate);
            cmd.Parameters.AddWithValue("@Day", r.DayOfWeek ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Time", r.AccidentTime);
            cmd.Parameters.AddWithValue("@Route", r.Route ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Location", r.Location ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Type", r.AccidentType ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Province", r.ProvinceName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@District", r.DistrictMunicipalityName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FD", r.FatalDrivers);
            cmd.Parameters.AddWithValue("@FP", r.FatalPassengers);
            cmd.Parameters.AddWithValue("@FPD", r.FatalPedestrians);
            cmd.Parameters.AddWithValue("@FC", r.FatalCyclists);
            cmd.Parameters.AddWithValue("@SD", r.SeriousDrivers);
            cmd.Parameters.AddWithValue("@SP", r.SeriousPassengers);
            cmd.Parameters.AddWithValue("@SPD", r.SeriousPedestrians);
            cmd.Parameters.AddWithValue("@SC", r.SeriousCyclists);
            cmd.Parameters.AddWithValue("@SLD", r.SlightDrivers);
            cmd.Parameters.AddWithValue("@SLP", r.SlightPassengers);
            cmd.Parameters.AddWithValue("@SLPD", r.SlightPedestrians);
            cmd.Parameters.AddWithValue("@SLC", r.SlightCyclists);
            cmd.Parameters.AddWithValue("@Vehicles", r.VehiclesInvolved ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", r.CreatedAt);

            cmd.ExecuteNonQuery();
        }

        public static List<string> GetLookup(string tableName, string columnName)
        {
            var list = new List<string>();
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new SqlCommand($"SELECT {columnName} FROM {tableName}", conn);

            using var rdr = cmd.ExecuteReader();

            while (rdr.Read())
                list.Add(rdr[columnName].ToString()!.Trim());
            return list;
        }


        public static List<AccidentReportModel> GetReports(
     DateTime? from = null, DateTime? to = null,
     string? station = null, string? type = null, string? province = null, string? district = null)
        {
            var list = new List<AccidentReportModel>();
            var where = new List<string>();

            if (from.HasValue) where.Add("AccidentDate >= @From");
            if (to.HasValue) where.Add("AccidentDate <= @To");
            if (!string.IsNullOrEmpty(station)) where.Add("SAPSStation = @Station");
            if (!string.IsNullOrEmpty(type)) where.Add("AccidentType = @Type");
            // FIX: Add province and district filters
            if (!string.IsNullOrEmpty(province)) where.Add("ProvinceName = @Province");
            if (!string.IsNullOrEmpty(district)) where.Add("DistrictMunicipalityName = @District");

            string whereClause = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
            string sql = "SELECT * FROM AccidentReports" + whereClause + " ORDER BY AccidentDate DESC, Id DESC";

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);

            if (from.HasValue) cmd.Parameters.AddWithValue("@From", from.Value);
            if (to.HasValue) cmd.Parameters.AddWithValue("@To", to.Value);
            if (!string.IsNullOrEmpty(station)) cmd.Parameters.AddWithValue("@Station", station.Trim());
            if (!string.IsNullOrEmpty(type)) cmd.Parameters.AddWithValue("@Type", type.Trim());
            if (!string.IsNullOrEmpty(province)) cmd.Parameters.AddWithValue("@Province", province.Trim());
            if (!string.IsNullOrEmpty(district)) cmd.Parameters.AddWithValue("@District", district.Trim());

            using var rdr = cmd.ExecuteReader();
            while (rdr.Read()) list.Add(Map(rdr));
            return list;
        }
        public static Dictionary<string, int> GetCountByColumn(
            string column, DateTime? from, DateTime? to, int topN = 0)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string topClause = topN > 0 ? $"TOP {topN}" : "";
            string sql = $@"
                SELECT {topClause} TRIM({column}) AS Val, COUNT(*) AS Cnt
                FROM AccidentReports
                {DateWhereClause(from, to)}
                GROUP BY TRIM({column})
                ORDER BY Cnt DESC";

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = AddDateParams(new SqlCommand(sql, conn), from, to);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
                result[rdr["Val"].ToString()!.Trim()] = Convert.ToInt32(rdr["Cnt"]);

            return result;
        }


        public static (int Fatal, int Serious, int Slight) GetSeverityTotals(
            DateTime? from, DateTime? to)
        {
            string sql = $@"
                SELECT
                    ISNULL(SUM(FatalDrivers+FatalPassengers+FatalPedestrians+FatalCyclists),0)       AS Fatal,
                    ISNULL(SUM(SeriousDrivers+SeriousPassengers+SeriousPedestrians+SeriousCyclists),0) AS Serious,
                    ISNULL(SUM(SlightDrivers+SlightPassengers+SlightPedestrians+SlightCyclists),0)    AS Slight
                FROM AccidentReports {DateWhereClause(from, to)}";

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = AddDateParams(new SqlCommand(sql, conn), from, to);
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
                return (Convert.ToInt32(rdr["Fatal"]),
                        Convert.ToInt32(rdr["Serious"]),
                        Convert.ToInt32(rdr["Slight"]));
            return (0, 0, 0);
        }

        public static Dictionary<string, int> GetCountByDayOfWeek(DateTime? from, DateTime? to)
        {
            // Return in Mon-Sun order
            var raw = GetCountByColumn("DayOfWeek", from, to);
            var order = new[] { "M", "TU", "W", "TH", "FR", "SA", "SU" };
            var result = new Dictionary<string, int>();
            foreach (var d in order)
                result[d] = raw.TryGetValue(d, out int v) ? v : 0;
            return result;
        }


        public static (int inserted, int skipped) BulkImport(List<AccidentReportModel> reports)
        {
            int inserted = 0, skipped = 0;
            foreach (var r in reports)
            {
                try { InsertReport(r); inserted++; }
                catch { skipped++; }
            }
            return (inserted, skipped);
        }


        private static string DateWhereClause(DateTime? from, DateTime? to)
        {
            var parts = new List<string>();
            if (from.HasValue) parts.Add("AccidentDate >= @From");
            if (to.HasValue) parts.Add("AccidentDate <= @To");
            return parts.Count > 0 ? "WHERE " + string.Join(" AND ", parts) : "";
        }

        private static SqlCommand AddDateParams(SqlCommand cmd, DateTime? from, DateTime? to)
        {
            if (from.HasValue) cmd.Parameters.AddWithValue("@From", from.Value);
            if (to.HasValue) cmd.Parameters.AddWithValue("@To", to.Value);
            return cmd;
        }



        private static AccidentReportModel Map(IDataReader r) => new AccidentReportModel
        {
            Id = (int)r["Id"],
            SAPSStation = r["SAPSStation"].ToString()!.Trim(),
            ARNumber = r["ARNumber"].ToString()!.Trim(),
            AccidentDate = (DateTime)r["AccidentDate"],
            DayOfWeek = r["DayOfWeek"].ToString()!.Trim(),
            // FIX: Safely parse the time with multiple format attempts
            AccidentTime = ParseAccidentTime(r["AccidentTime"].ToString()?.Trim()),
            Route = r["Route"].ToString()!.Trim(),
            Location = r["Location"].ToString()!.Trim(),
            AccidentType = r["AccidentType"].ToString()!.Trim(),
            ProvinceName = r["ProvinceName"].ToString()!.Trim(),
            DistrictMunicipalityName = r["DistrictMunicipalityName"].ToString()!.Trim(),
            FatalDrivers = Convert.ToInt32(r["FatalDrivers"]),
            FatalPassengers = Convert.ToInt32(r["FatalPassengers"]),
            FatalPedestrians = Convert.ToInt32(r["FatalPedestrians"]),
            FatalCyclists = Convert.ToInt32(r["FatalCyclists"]),
            SeriousDrivers = Convert.ToInt32(r["SeriousDrivers"]),
            SeriousPassengers = Convert.ToInt32(r["SeriousPassengers"]),
            SeriousPedestrians = Convert.ToInt32(r["SeriousPedestrians"]),
            SeriousCyclists = Convert.ToInt32(r["SeriousCyclists"]),
            SlightDrivers = Convert.ToInt32(r["SlightDrivers"]),
            SlightPassengers = Convert.ToInt32(r["SlightPassengers"]),
            SlightPedestrians = Convert.ToInt32(r["SlightPedestrians"]),
            SlightCyclists = Convert.ToInt32(r["SlightCyclists"]),
            VehiclesInvolved = r["VehiclesInvolved"].ToString()!.Trim(),
            CreatedAt = (DateTime)r["CreatedAt"]
        };

        // Add this helper method to DatabaseHelper class
        private static DateTime ParseAccidentTime(string? timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return DateTime.Today; // Default to midnight

            // Try different time formats
            string[] formats = {
        "HH:mm",     // 14:30
        "H:mm",      // 14:30
        "HH:mm:ss",  // 14:30:00
        "H:mm:ss",   // 14:30:00
        "hh:mm tt",  // 02:30 PM
        "h:mm tt"    // 2:30 PM
    };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(timeString, format,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }
            }

            // If all parsing attempts fail, try standard DateTime parsing
            if (DateTime.TryParse(timeString, out DateTime fallbackResult))
                return fallbackResult;

            // Last resort: return today's date at midnight
            return DateTime.Today;
        }

        public static void SaveMonthlyReport(MonthlyAccidentData data, DateTime reportDate, string reportContent)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            string sql = @"
        MERGE INTO MonthlyReports AS target
        USING (SELECT @ReportDate AS ReportDate) AS source
        ON target.ReportDate = @ReportDate
        WHEN MATCHED THEN
            UPDATE SET 
                TotalCrashes = @TotalCrashes,
                TotalFatalities = @TotalFatalities,
                TotalSerious = @TotalSerious,
                TotalSlight = @TotalSlight,
                ReportContent = @ReportContent,
                UpdatedAt = @UpdatedAt
        WHEN NOT MATCHED THEN
            INSERT (ReportDate, TotalCrashes, TotalFatalities, TotalSerious, TotalSlight, ReportContent, CreatedAt)
            VALUES (@ReportDate, @TotalCrashes, @TotalFatalities, @TotalSerious, @TotalSlight, @ReportContent, @CreatedAt);";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ReportDate", reportDate);
            cmd.Parameters.AddWithValue("@TotalCrashes", data.CurrentYearCrashes);
            cmd.Parameters.AddWithValue("@TotalFatalities", data.CurrentYearFatalities);
            cmd.Parameters.AddWithValue("@TotalSerious", data.CurrentYearSerious);
            cmd.Parameters.AddWithValue("@TotalSlight", data.CurrentYearSlight);
            cmd.Parameters.AddWithValue("@ReportContent", reportContent);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

            cmd.ExecuteNonQuery();
        }
    }
}
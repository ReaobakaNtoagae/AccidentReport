using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccidentReport
{
    // Simple DTO that represents a single accident report record.
    // This model is used for:
    //  - inserting records into the database (DatabaseHelper.InsertReport)
    //  - binding to grids and generating text/pdf reports (MainForm, MonthlyAccidentData)
    public class AccidentReportModel

    {
        // Primary key (DB identity)
        public int Id { get; set; }

        // Core fields captured on the "New Report" form
        public string? SAPSStation { get; set; } = string.Empty;
        public string? ARNumber {  get; set; } = string.Empty ;
        public DateTime AccidentDate { get; set; } = DateTime.Today ;
        public string? DayOfWeek {  get; set; } = string.Empty ;
        // AccidentTime stored as DateTime for convenience (only time portion is typically used)
        public DateTime AccidentTime {  get; set; } = DateTime.Now ;
        public string? Route {  get; set; } = string.Empty ;
        public string? Location {  get; set; } = string.Empty ;
        public string? ProvinceName {  get; set; } = string.Empty;
        public string? DistrictMunicipalityName {  get; set; } = string.Empty;
        public string? AccidentType {  get; set; } = string.Empty ;

        // Fatal casualties (individual counts)
        public int FatalDrivers { get; set; }
        public int FatalPassengers { get; set; }
        public int FatalPedestrians { get; set; }
        public int FatalCyclists { get; set; }

        // Serious casualties
        public int SeriousDrivers { get; set; }
        public int SeriousPassengers { get; set; }
        public int SeriousPedestrians { get; set; }
        public int SeriousCyclists { get; set ; }

        // Slight casualties
        public int SlightDrivers { get; set; }
        public int SlightPassengers { get; set; }
        public int SlightPedestrians { get; set; }
        public int SlightCyclists { get; set; }

        // Free text describing vehicles, plus DB timestamp
        public string? VehiclesInvolved {  get; set; } = string.Empty;
        public DateTime CreatedAt {  get; set; } = DateTime.Now ;

        // Computed totals — convenient for UI and reports (read-only properties)
        public int TotalFatal => FatalDrivers + FatalPassengers + FatalPedestrians + FatalCyclists;
        public int TotalSerious => SeriousDrivers + SeriousPassengers + SeriousPedestrians + SeriousCyclists;
        public int TotalSlight => SlightDrivers + SlightPassengers + SlightPedestrians + SlightCyclists;
        public int GrandTotal => TotalFatal + TotalSerious + TotalSlight;
    }
}

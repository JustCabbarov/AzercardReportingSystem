namespace RMS.Domain.Entities.Oracle
{
    public class SectorSpend
    {
        public DateTime ReportMonth { get; set; }
        public string BankName { get; set; } = "";
        public string MccGroup { get; set; } = "";
        public string Mcc { get; set; } = "";
        public string SourceCity { get; set; } = "";
        public string SourceCityCategory { get; set; } = "";
        public string SourceChannel { get; set; } = "";
        public string OperationType { get; set; } = "";
        public string SourceCountryCategory { get; set; } = "";
        public int? IsAcquiring { get; set; }
        public int? IsIssuing { get; set; }
        public decimal TotalAmount { get; set; }
        public long TotalCount { get; set; }       // ✅ decimal → long
        public decimal TotalLocalAmount { get; set; }
        public double AvgTicket { get; set; }      // ✅ decimal → double
        public double LocalRatioPct { get; set; }  // ✅ decimal → double (170% problemi üçün)
        public double SectorMarketSharePct { get; set; } // ✅ decimal → double
    }
}
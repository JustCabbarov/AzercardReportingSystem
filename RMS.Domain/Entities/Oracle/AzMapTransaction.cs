namespace RMS.Domain.Entities.Oracle
{
    public class AzMapTransaction
    {
        public DateTime ReportMonth { get; set; }
        public string? BankName { get; set; }
        public string? SourceCity { get; set; }  // ← əlavə edildi
        public string? SourceCityClean { get; set; }
        public string? SourceCityCategory { get; set; }
        public decimal TotalAmount { get; set; }
        public long TotalCount { get; set; }
    }
}
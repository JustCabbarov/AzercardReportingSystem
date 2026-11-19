namespace RMS.Domain.Entities.Oracle
{
    public class AzMapTransaction
    {
        public DateTime ReportMonth { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? BankName { get; set; }
        public string? SourceCity { get; set; }  // ← əlavə edildi
        public string? SourceCityClean { get; set; }
        public string? SourceCityCategory { get; set; }
        public string? AcquiringDeviceType { get; set; }
        public string? TransGroup { get; set; }
        public string? OperationType { get; set; }
        public string? TokenStatus { get; set; }
        public string? Mcc { get; set; }
        public string? IsAcquiringCategory { get; set; }
        public string? TargetBankName { get; set; }
        public string? SourceBankName { get; set; }
        public string? TargetCity { get; set; }
        public string? PaymentSystem { get; set; }
        public decimal TotalAmount { get; set; }
        public long TotalCount { get; set; }

        public decimal? Latitude { get; set; }   // ← əlavə
        public decimal? Longitude { get; set; }
    }
}
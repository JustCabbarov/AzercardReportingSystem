using System;

namespace RMS.Domain.Entities.Oracle
{
    public class CardActivity
    {
        public DateTime ReportMonth { get; set; }
        public string BankName { get; set; } = "";
        public string? RegionName { get; set; }
        public string? RegionNameClean { get; set; }
        public string? CardBrandName { get; set; }
        public string CardProductName { get; set; } = "";
        public string? ProductType { get; set; }
        public string? PaymentScheme { get; set; }
        public string? ContactlessStatus { get; set; }
        public string? ExpStatus { get; set; }
        public string? Status3D { get; set; }
        public long TotalCards { get; set; }
        public long TotalTransCount { get; set; }
        public decimal TotalTransAmount { get; set; }
        public decimal TotalLocalAmount { get; set; }
        public long ContactlessCount { get; set; }
        public long ChipCount { get; set; }
        public long TokenCount { get; set; }
        public decimal? ActiveCardRatePct { get; set; }

        // C# tərəfindən hesablanır
        public decimal? ContactlessRatePct =>
            TotalTransCount > 0
                ? Math.Round((decimal)ContactlessCount / TotalTransCount * 100, 2)
                : null;

        public string ActivitySegment =>
            ActiveCardRatePct == null ? "Unknown" :
            ActiveCardRatePct >= 80 ? "HighlyActive" :
            ActiveCardRatePct >= 50 ? "ModeratelyActive" :
            ActiveCardRatePct >= 20 ? "LowActive" : "Passive";
    }
}
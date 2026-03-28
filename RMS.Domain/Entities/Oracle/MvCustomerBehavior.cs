using System;
using RMS.Domain.Entities;

namespace RMS.Domain.Entities.Oracle
{
    public class MvCustomerBehavior : BaseEntity
    {
        public DateTime ReportMonth { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string BankBranchName { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string RegionNameClean { get; set; } = string.Empty;
        public string CardCategoryName { get; set; } = string.Empty;
        public string CardCommercialName { get; set; } = string.Empty;
        public string CardProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string PaymentScheme { get; set; } = string.Empty;
        public string CardBrandName { get; set; } = string.Empty;
        public string BaseCurrencyName { get; set; } = string.Empty;

        /// <summary>ACTIVE | EXPIRED | BLOCKED v? s.</summary>
        public string ExpStatus { get; set; } = string.Empty;

        /// <summary>3D Secure statusu: ACTIVE | INACTIVE</summary>
        public string Status3D { get; set; } = string.Empty;

        /// <summary>ENABLED | DISABLED</summary>
        public string ContactlessStatus { get; set; } = string.Empty;

        // Kart portfeli
        public decimal TotalCards { get; set; }
        public decimal ActiveCards { get; set; }
        public decimal ActiveCardRatioPct { get; set; }

        // Contactless
        public decimal CtlsEnabledCards { get; set; }
        public decimal CtlsAdoptionPct { get; set; }

        // 3D Secure
        public decimal ThreedsActiveCards { get; set; }
        public decimal ThreedsAdoptionPct { get; set; }

        // Tranzaksiya metrikalar? (BI_TRANSACTION_MASKED-d?n JOIN)
        public decimal TxnCount { get; set; }
        public decimal TxnAmount { get; set; }
        public decimal TxnAmountPerCard { get; set; }
        public decimal TxnCountPerCard { get; set; }

        public DateTime LastRefresh { get; set; }
    }
}

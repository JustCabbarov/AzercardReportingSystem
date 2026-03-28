using System;
using RMS.Domain.Entities;

namespace RMS.Domain.Entities.Oracle
{
    public class MvNewCardActivation : BaseEntity
    {
        public string CardProductName { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankBranchName { get; set; } = string.Empty;
        public string RegionNameClean { get; set; } = string.Empty;
        public string CardCategoryName { get; set; } = string.Empty;
        public string CardCommercialName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string PaymentScheme { get; set; } = string.Empty;
        public string CardBrandName { get; set; } = string.Empty;
        public string BaseCurrencyName { get; set; } = string.Empty;

        /// <summary>ENABLED | DISABLED</summary>
        public string ContactlessStatus { get; set; } = string.Empty;

        /// <summary>ACTIVE | INACTIVE</summary>
        public string Status3D { get; set; } = string.Empty;

        /// <summary>Kartın ilk görünüş ayı (açılış ayı)</summary>
        public DateTime IssuanceMonth { get; set; }

        public decimal TotalIssuedCards { get; set; }

        // ── M1 (1-ci ay) ──────────────────────────────────────────────────
        public decimal M1TxnCount { get; set; }
        public decimal M1TxnAmount { get; set; }

        /// <summary>M1-də tranzaksiya edilən unikal gün sayı</summary>
        public int M1ActiveDays { get; set; }

        // ── M2 (2-ci ay) ──────────────────────────────────────────────────
        public decimal M2TxnCount { get; set; }
        public decimal M2TxnAmount { get; set; }

        // ── M3 (3-cü ay) ──────────────────────────────────────────────────
        public decimal M3TxnCount { get; set; }
        public decimal M3TxnAmount { get; set; }

        // ── 3 ay cəmi ─────────────────────────────────────────────────────
        public decimal Total3MTxnCount { get; set; }
        public decimal Total3MTxnAmount { get; set; }

        // ── Aktivasiya flagları (1 = aktiv, 0 = dormant) ──────────────────
        /// <summary>M1-də ən azı 1 tranzaksiya: 1 | 0</summary>
        public int IsActivatedM1 { get; set; }

        /// <summary>M2-də ən azı 1 tranzaksiya: 1 | 0</summary>
        public int IsActivatedM2 { get; set; }

        /// <summary>M3-də ən azı 1 tranzaksiya: 1 | 0</summary>
        public int IsActivatedM3 { get; set; }

        /// <summary>3 ayın hamısında aktiv olan loyal early user: 1 | 0</summary>
        public int IsActiveAll3M { get; set; }

        /// <summary>Kart başına ortalama aylıq xərc (3 ay üzrə)</summary>
        public decimal AvgMonthlySpendPerCard { get; set; }

        public DateTime LastRefresh { get; set; }
    }
}

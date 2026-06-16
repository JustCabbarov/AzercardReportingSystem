using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.TransactionDeteiled
{

    public class TransactionFilterRequest
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public string? CardType { get; set; }
        public string? Currency { get; set; }
        public string? SourceBankName { get; set; }
        public string? PaymentSystem { get; set; }
        public string? OperationType { get; set; }
        public string? TransGroup { get; set; }
        public string? TokenStatus { get; set; }

        public string? AcquiringDevice { get; set; }
        public string? IssuingCategory { get; set; }
        public string? TargetBankName { get; set; }

        public MeasureType Measure { get; set; } = MeasureType.AZN;
    }

    public enum MeasureType
    {
        AZN,
        SAY,
        PCT,
        R
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle.CardPortfolio
{
    namespace RMS.Domain.Entities.Oracle
    {
        public class CardPortfolioFilter
        {
            public DateTime? FromMonth { get; set; }
            public DateTime? ToMonth { get; set; }
            public string? BankName { get; set; }
            public string? RegionName { get; set; }
            public string? PaymentScheme { get; set; }
            public string? ProductType { get; set; }
            public string? ContactlessStatus { get; set; }
            public string? ExpStatus { get; set; }
            public string? Status3D { get; set; }
            public string? BaseCurrency { get; set; }
            public string? CardProductName { get; set; }
        }

        public class FilterOptionsResponse
        {
            public List<string> Banks { get; set; } = [];
            public List<string> Regions { get; set; } = [];
            public List<string> PaymentSchemes { get; set; } = [];
            public List<string> ProductTypes { get; set; } = [];
            public List<string> ContactlessStatuses { get; set; } = [];
            public List<string> ExpStatuses { get; set; } = [];
            public List<string> Status3Ds { get; set; } = [];
            public List<string> Currencies { get; set; } = [];
            public List<string> CardProducts { get; set; } = [];
            public DateTime MinMonth { get; set; }
            public DateTime MaxMonth { get; set; }
        }

        public class TopSchemeCardDto
        {
            public string PaymentScheme { get; set; } = "";
            public long TotalCards { get; set; }
            public double SharePercent { get; set; }
            public double MomChange { get; set; }
            public bool MomIsUp { get; set; }
            public bool ShareIsUp { get; set; }
            public long Salary { get; set; }
            public long Credit { get; set; }
            public long Social { get; set; }
            public long Other { get; set; }
        }

        public class TopCardsResponse
        {
            public long GrandTotal { get; set; }
            public List<TopSchemeCardDto> Schemes { get; set; } = [];
        }

        public class CrossTableRequest : CardPortfolioFilter
        {
            public string RowDimension { get; set; } = "bank_name";
        }

        public class CrossTableCell
        {
            public string ProductType { get; set; } = "";
            public long Count { get; set; }
            public double MomPct { get; set; }
            public bool MomUp { get; set; }
        }

        public class CrossTableRow
        {
            public string Label { get; set; } = "";
            public long Total { get; set; }
            public List<CrossTableCell> Columns { get; set; } = [];
        }

        public class CrossTableResponse
        {
            public List<string> ColumnHeaders { get; set; } = [];
            public List<CrossTableRow> Rows { get; set; } = [];
        }

        public class PayChartRequest : CardPortfolioFilter
        {
            public string Dimension { get; set; } = "payment_scheme";
        }

        public class PayChartItem
        {
            public string Label { get; set; } = "";
            public long Count { get; set; }
            public double Percent { get; set; }
        }

        public class PayChartResponse
        {
            public List<PayChartItem> Items { get; set; } = [];
        }

        public class TrendChartRequest : CardPortfolioFilter
        {
            public string Dimension { get; set; } = "payment_scheme";
            public string? DimValue { get; set; }
            public string Granularity { get; set; } = "month";
        }

        public class TrendPoint
        {
            public DateTime Period { get; set; }
            public long Count { get; set; }
        }

        public class TrendSeries
        {
            public string Label { get; set; } = "";
            public List<TrendPoint> Points { get; set; } = [];
        }

        public class TrendChartResponse
        {
            public List<TrendSeries> Series { get; set; } = [];
        }

        public class XyChartRequest : CardPortfolioFilter
        {
            public string XDimension { get; set; } = "bank_name";
            public string YDimension { get; set; } = "base_currency_name";
        }

        public class XyCell
        {
            public string XLabel { get; set; } = "";
            public string YLabel { get; set; } = "";
            public long Count { get; set; }
            public double SharePct { get; set; }
        }

        public class XyChartResponse
        {
            public List<string> XLabels { get; set; } = [];
            public List<string> YLabels { get; set; } = [];
            public List<XyCell> Cells { get; set; } = [];
        }
    }
}

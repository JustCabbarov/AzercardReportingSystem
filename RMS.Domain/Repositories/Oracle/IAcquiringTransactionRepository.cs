using RMS.Domain.Entities.Oracle.AcquiringTransaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IAcquiringDeviceRepository
    {
        Task<AcquiringFilterOptionsResponse> GetFilterOptionsAsync();
        Task<DateTime> GetLatestReportDateAsync();
        Task<List<DeviceSummaryRow>> GetDeviceSummaryAsync(AcquiringDeviceFilter f);
        Task<List<AcqPieItem>> GetPieChartAsync(AcquiringDeviceFilter f);
        Task<List<TransGroupItem>> GetTransGroupChartAsync(AcquiringDeviceFilter f);
        Task<List<PaymentSystemItem>> GetPaymentSystemChartAsync(AcquiringDeviceFilter f);
        Task<AcqTrendResponse> GetTrendAsync(AcquiringTrendRequest r);
        Task<List<BankItem>> GetSourceBankChartAsync(AcquiringDeviceFilter f);
        Task<List<BankItem>> GetTargetBankChartAsync(AcquiringDeviceFilter f);
        Task<AcquiringDashboardResponse> GetDashboardAsync(AcquiringDeviceFilter f);
    }
}

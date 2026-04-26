using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Application.Services.Oracle.MLForcasting
{
    internal sealed class CachedModel
    {
        /// <summary>Disk-dəki .zip faylının tam yolu.</summary>
        public string FilePath { get; init; } = string.Empty;

        /// <summary>Modelin öyrənildiyi tarix (köhnəlik yoxlaması üçün).</summary>
        public DateTime TrainedAt { get; init; }

        /// <summary>TimeSeriesEngine — Predict() birbaşa burada çağrılır.</summary>
        public object Engine { get; init; } = default!;

        /// <summary>Forecast üçün istifadə edilən horizon (engine bu dəyərlə yaradılıb).</summary>
        public int Horizon { get; init; }
    }
}


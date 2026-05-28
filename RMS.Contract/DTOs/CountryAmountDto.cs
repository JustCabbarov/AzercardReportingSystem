using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.DTOs
{

    public class CountryAmountDto
    {
        public string Country { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}

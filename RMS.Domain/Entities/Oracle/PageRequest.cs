using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities.Oracle
{
    
        public class PageRequest
        {
            private int _page = 1;
            private int _pageSize = 20;

            public int Page
            {
                get => _page;
                set => _page = value < 1 ? 1 : value;
            }

            public int PageSize
            {
                get => _pageSize;
                set => _pageSize = value < 1 ? 1 : value > 100 ? 100 : value;
            }
        }
    
}

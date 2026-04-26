using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RMS.Domain.Entities;
using RMS.Domain.Entities.Oracle;
using RMS.Domain.Repositories.System;
using RMS.Persitence.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Persitence.Repositories.System
{

    public class OracleRepository : IOracleRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ISqlQueryRepository _sqlQueryRepository;
        public OracleRepository(IConfiguration configuration, ISqlQueryRepository sqlQueryRepository)
        {
            _configuration = configuration;
            _sqlQueryRepository = sqlQueryRepository;
        }




    }
}

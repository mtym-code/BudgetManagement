using BudgetManagement.Common.Helper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BudgetManagement.Common.Database
{
    public static class DbConnectionFactory
    {
        public static IDbConnection Create()
        {
            var connStr = ConfigurationHelper.Get("ConnectionStrings:Default");
            return new NpgsqlConnection(connStr);
        }
    }
}

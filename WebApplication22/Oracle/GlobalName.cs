using Dapper;
using Dapper.Oracle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebDocLoader.Oracle
{
    public static class GlobalName
    {
        public static string GetGlobalName(OracleTransaction transaction)
        {
            var userFIO = transaction.Connection.QueryFirstOrDefault<string>(@"SELECT * FROM GLOBAL_NAME",
                transaction: transaction);

            return userFIO;
            
        }
    }
}

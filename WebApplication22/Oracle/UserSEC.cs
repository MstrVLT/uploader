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
    public static class UserSEC
    {
        public static void SetUser(OracleTransaction transaction, decimal isn)
        {
            var parameters = new OracleDynamicParameters();
            parameters.BindByName = true;
            parameters.Add("puserisn",
                value: isn,
                dbType: OracleMappingType.Decimal,
                direction: ParameterDirection.Input);

            // вызываем процедуру
            transaction.Connection.Execute("****************", parameters, commandType: CommandType.StoredProcedure, transaction: transaction);
        }

        public static int SetUser(OracleTransaction transaction, string userfio, string psw, out decimal? userISN)
        {
            // вызов процедуры или запроса (OracleCommand) с выводом информации DBMS_OUTPUT
            // используются ODP.NET ( https://www.oracle.com/ru/database/technologies/appdev/dotnet/odp.html )
            // и Dapper ( https://github.com/DapperLib/Dapper )

            try
            {
                var parameters = new OracleDynamicParameters();

                parameters.Add("userfio",
                    value: userfio,
                    dbType: OracleMappingType.Varchar2,
                    direction: ParameterDirection.Input);
                parameters.Add("psw",
                    value: psw,
                    dbType: OracleMappingType.Varchar2,
                    direction: ParameterDirection.Input);

                parameters.Add("puserisn",
                    dbType: OracleMappingType.Decimal,
                    direction: ParameterDirection.Output);
                parameters.Add("pdept0isn",
                    dbType: OracleMappingType.Decimal,
                    direction: ParameterDirection.Output);
                parameters.Add("pdeptisn",
                    dbType: OracleMappingType.Decimal,
                    direction: ParameterDirection.Output);
                parameters.Add("pfirmisn",
                    dbType: OracleMappingType.Decimal,
                    direction: ParameterDirection.Output);
                parameters.Add("pcountryisn",
                    dbType: OracleMappingType.Decimal,
                    direction: ParameterDirection.Output);
                parameters.Add("pcurrisn",
                    dbType: OracleMappingType.Decimal,
                    direction: ParameterDirection.Output);
                parameters.Add("pbankisn",
                    dbType: OracleMappingType.Decimal,
                    direction: ParameterDirection.Output);
                parameters.Add("proles",
                    dbType: OracleMappingType.Varchar2,
                    size: 255,
                    direction: ParameterDirection.Output);
                parameters.Add("pinflation",
                    dbType: OracleMappingType.Decimal,
                    direction: ParameterDirection.Output);

                // вызываем процедуру
                transaction.Connection.Execute("**************, 
                    param: parameters, 
                    commandType: CommandType.StoredProcedure, 
                    transaction: transaction);

                // запрашиваем параметр уже преобразованного типа (читай справку https://docs.oracle.com/database/121/ODPNT/featTypes.htm#ODPNT281 )
                userISN = parameters.Get<decimal?>("puserisn");
                return 0;
            }
            catch (OracleException ex)
            {
                userISN = null;
                return ex.Number;
            }
        }

        public static string GetUserFIO(OracleTransaction transaction, decimal userISN)
        {
            var op = new OracleDynamicParameters();

            op.Add("PUSERISN", 
                value: userISN,
                dbType: OracleMappingType.Varchar2, 
                direction: ParameterDirection.Input);

            var userFIO = transaction.Connection.QueryFirstOrDefault<string>(@"SELECT D.SHORTNAME FROM ************ D WHERE D.ISN = :PUSERISN", 
                param: op, 
                transaction: transaction);

            return userFIO;
            
        }
    }
}

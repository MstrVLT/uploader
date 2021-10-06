using Dapper;
using Dapper.Oracle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebDocLoader.Oracle
{
    public class Finmon
    {
        public class FinMonResult
        {
            public bool Success { get; set; }
            public string Text { get; set; }
            public string GlobalName { get; set; }
            public string ISN { get; set; }
        }

        public static bool GetFilesByMD5Hash(OracleTransaction transaction, string md5hash, out object user)
        {
            var op = new OracleDynamicParameters();

            op.Add("PMD5HASH",
                value: md5hash.ToUpper(),
                dbType: OracleMappingType.Varchar2,
                direction: ParameterDirection.Input);

            user = transaction.Connection.QueryFirstOrDefault<string>(
                "SELECT * FROM (SELECT T.USEROSUSER COUNT_FILES FROM ************* T WHERE T.MD5HASH = :PMD5HASH) WHERE ROWNUM = 1",
                param: op,
                transaction: transaction);
            
            return user != null;
        }

        public static async Task<FinMonResult> SaveFinmonRawXML(OracleTransaction transaction, decimal userIsn, string fileName, MemoryStream fileStream)
        {
            //string dbmsoutput;

            // вызов процедуры или запроса (OracleCommand) с выводом информации DBMS_OUTPUT
            // используются ODP.NET ( https://www.oracle.com/ru/database/technologies/appdev/dotnet/odp.html )
            // и Dapper ( https://github.com/DapperLib/Dapper )


            try
            {
                UserSEC.SetUser(transaction, userIsn);
                var globalName= GlobalName.GetGlobalName(transaction);
                
                // можно включить вывод дебаг информации в конце вызвать DBMSOutputGetLines
                //CommandWithDBMSOutput.DBMSOutputEnable(transaction);

                transaction.Connection.GetSessionInfo()
                
                var parameters = new OracleDynamicParameters();
                await using OracleClob clob = new OracleClob(transaction.Connection);

                
                fileStream.Position = 0;

                // заполняем CLOB

                string strMD5 = "";
                using (StreamReader rdr = new StreamReader(fileStream, Encoding.UTF8))
                {
                    var chars = new byte[fileStream.Length];
                    
                    fileStream.Position = 0;
                    await fileStream.ReadAsync(chars, 0, chars.Length);
                    
                    using MD5 md5 = MD5.Create();
                    {
                        var hash = md5.ComputeHash(chars);
                        strMD5 = hash.Aggregate(strMD5, (current, b) => current + b.ToString("x2").ToUpper());
                    }
                    if (GetFilesByMD5Hash(transaction, strMD5, out var user))
                        return new FinMonResult { Success = false, Text = $"был загружен ({user})", GlobalName = globalName };
                    
                    clob.Append(chars, 0, chars.Length);
                }

                parameters.Add("pXML",
                    value: clob,
                    dbType: OracleMappingType.Clob,
                    direction: ParameterDirection.Input);

                // заполняем остальные параметры

                parameters.Add("pFileName",
                    value: fileName,
                    dbType: OracleMappingType.Varchar2, size: 500,
                    direction: ParameterDirection.Input);

                parameters.Add("pMD5Hash",
                    value: strMD5.ToUpper(),
                    dbType: OracleMappingType.Varchar2, size: 250,
                    direction: ParameterDirection.Input);

                parameters.Add("pReestrType",
                    value: "Портал",
                    dbType: OracleMappingType.Varchar2, size: 100,
                    direction: ParameterDirection.Input);

                parameters.Add("pISN",
                    dbType: OracleMappingType.Decimal,
                    direction: ParameterDirection.InputOutput);

                //prc_SAVE_FINMON_REESTR_RAW_XML(pFileName in varchar2, pReestrType varchar2, pXML in clob) RETURN number
                // вызываем процедуру
                transaction.Connection.Execute("****************************", 
                    param: parameters, 
                    commandType: CommandType.StoredProcedure,
                    transaction: transaction);

                // запрашиваем параметр уже преобразованного типа (читай справку https://docs.oracle.com/database/121/ODPNT/featTypes.htm#ODPNT281 )
                var isn = parameters.Get<decimal>("pISN");

                // можно включить вывод дебаг информации в начале вызвать DBMSOutputEnable
                //dbmsoutput = CommandWithDBMSOutput.DBMSOutputGetLines(transaction);
                
                return new FinMonResult { Success = true, Text = $"загружен", GlobalName = globalName, ISN = isn.ToString()};
            }
            catch (OracleException ex)
            {
                return new FinMonResult { Success = false, Text = $"ошибка БД {ex.Number}" };
            }

        }
    }
}

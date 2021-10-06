using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using WebDocLoader.Models;
using WebDocLoader.Oracle;
using WebDocLoader.Security;

namespace WebDocLoader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FinmonController : ControllerBase
    {
        // FormData из js скрипта ( https://developer.mozilla.org/ru/docs/Web/API/FormData/FormData )
        // обернуто лишь для удобства добавления новых полей

        private readonly ILogger<IllegalController> _logger;
        private readonly IServiceProvider _provider;

        public FinmonController(ILogger<IllegalController> logger, IServiceProvider provider)
        {
            _logger = logger;
            _provider = provider;
        }

        [Authorize(AuthenticationSchemes = TokenAuthenticationSchemeOptions.Name)] // схема авторизации на куках
        [HttpPost]
        [Route("upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadFile([FromForm] UploadModel data, CancellationToken cancellationToken)
        {
            if (CheckIfXMLFile(data.file))
            {
                try
                {
                    if (data.file.Length == 0)
                        return BadRequest(new { message = "пуст" });

                    await using var stream = new MemoryStream();
                    var conn = _provider.GetRequiredService<OracleConnection>();
                    conn.Open();
                    try
                    {
                        await using var transaction = conn.BeginTransaction();

                        // 240000 = 4 минуты таймаут ожидания выполнения запроса (задается в js)
                        await data.file.CopyToAsync(stream, cancellationToken);

                        // получить ИСН авторизованного пользователя
                        var userIsn = decimal.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                        var msg = await Finmon.SaveFinmonRawXML(transaction, userIsn, data.file.FileName, stream);

                        if (msg.Success)
                        {
                            transaction.Commit();
                            return Ok(new {message = msg.Text, globalname = msg.GlobalName, isn = msg.ISN});
                        }
                        else
                        {
                            transaction.Rollback();
                            return BadRequest(new {message = msg.Text});
                        }
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
                catch (OperationCanceledException ocex)
                {
                    // 240000 = 4 минуты таймаут ожидания выполнения запроса (задается в js)
                    _logger.LogError(ocex.Message);
                    return BadRequest(new { message = ocex.Message });
                }
                catch (OracleException oex)
                {
                    _logger.LogError(oex.Message);
                    return BadRequest(new { message = oex.Message });
                }
            }
            else
            {
                return BadRequest(new { message = "Invalid file extension" });
            }
        }
/*
        public static string DataTableToJSONWithJSONNet(DataTable table)
        {
            return JsonConvert.SerializeObject(table, Formatting.Indented);
        }
*/

        private static bool CheckIfXMLFile(IFormFile file)
        {
            var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
            return (string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase)); // Change the extension based on your need
        }
    }
}

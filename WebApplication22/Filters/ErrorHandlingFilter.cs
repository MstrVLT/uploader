using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebDocLoader.Filters
{
    public class ErrorHandlingFilter : ExceptionFilterAttribute
    {
        private ILogger<ErrorHandlingFilter> _logger;
        public ErrorHandlingFilter(ILogger<ErrorHandlingFilter> logger)
        {
            _logger = logger;
        }

        public override void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            if (exception is OracleException)
            {
                _logger.LogError(exception.Message);
            }
            context.ExceptionHandled = true;
        }
    }
}

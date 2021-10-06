using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebDocLoader.Models
{
    public class UploadModel
    {
        public IFormFile file { get; set; }
    }
}

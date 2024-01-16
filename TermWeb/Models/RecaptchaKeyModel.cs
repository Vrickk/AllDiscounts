using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TermWeb.Models
{
    public class RecaptchaKeyModel
    {
        public string SiteKey { get; set; }
        public string SecretKey { get; set; }
    }
}

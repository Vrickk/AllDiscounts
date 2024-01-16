using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TermWeb.Controllers
{
    public class RecaptchaKeyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

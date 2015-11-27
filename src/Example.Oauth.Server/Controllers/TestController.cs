using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Example.Oauth.Server.Controllers
{
    public class TestController : Controller
    {
        [HttpGet]
        [Route("~/test")]
        public IActionResult Test()
        {
            //ViewBag.ReturnUrl = "/test";
            //return View("SignIn", HttpContext.GetExternalProviders());
            return new ObjectResult("success");
        }

        [HttpGet]
        [Route("~/test/index")]
        public IActionResult Index()
        {
            return View("Index");
        }
    }
}

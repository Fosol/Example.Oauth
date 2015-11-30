using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Server;
using Example.Oauth.Server.Models;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Extensions.OptionsModel;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Example.Oauth.Server.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private static bool _databaseChecked;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
        }

        [HttpGet, Route("~/account/login")]
        public IActionResult Login(string returnUrl = null)
        {
            // Note: the "returnUrl" parameter corresponds to the endpoint the user agent
            // will be redirected to after a successful authentication and not
            // the redirect_uri of the requesting client application.
            ViewBag.ReturnUrl = returnUrl;

            // Note: in a real world application, you'd probably prefer creating a specific view model.
            return View("Login");
        }

        [HttpPost, Route("~/account/login"), ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginAsync([FromForm] Models.LoginViewModel model, string returnUrl = null)
        {
            // Note: the "returnUrl" parameter corresponds to the endpoint the user agent
            // will be redirected to after a successful authentication and not
            // the redirect_uri of the requesting client application.
            if (string.IsNullOrEmpty(returnUrl))
            {
                return HttpBadRequest();
            }

            EnsureDatabaseCreated(_applicationDbContext);
            ViewBag.ReturnUrl = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync("jfoster@enkon.com", "P@$$W0rd", model.RememberMe, lockoutOnFailure: false);
                //var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return new ChallengeResult("oidc-server", new AuthenticationProperties
                    {
                        RedirectUri = returnUrl
                    });
                }
                //if (result.RequiresTwoFactor)
                //{
                //    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                //}
                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View("Login", model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View("Login", model);
        }

        // The following code creates the database and schema if they don't exist.
        // This is a temporary workaround since deploying database through EF migrations is
        // not yet supported in this release.
        // Please see this http://go.microsoft.com/fwlink/?LinkID=615859 for more information on how to do deploy the database
        // when publishing your application.
        private static void EnsureDatabaseCreated(ApplicationDbContext context)
        {
            if (!_databaseChecked)
            {
                _databaseChecked = true;
                context.Database.Migrate();
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(TestController.Index), "Claims");
            }
        }
    }
}

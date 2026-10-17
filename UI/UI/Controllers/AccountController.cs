using Microsoft.AspNetCore.Mvc;
using UI.Models;
using UI.Services;

namespace UI.Controllers
{
    /// <summary>
    /// Handles Login, Register, and Logout.
    /// JWT token and username are stored in the ASP.NET Core session after login.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly IAuthService _auth;

        public AccountController(IAuthService auth) => _auth = auth;

        //  GET /Account/Register
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (IsLoggedIn()) return RedirectToAction("Index", "Shop");
            return View(new RegisterViewModel());
        }

        //  POST /Account/Register 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model, string? returnUrl = null, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, error) = await _auth.RegisterAsync(
                model.Username, model.Password, model.FullName, ct);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Registration failed.");
                return View(model);
            }

            // Auto-login after register
            var (loginOk, loginErr, data) = await _auth.LoginAsync(model.Username, model.Password, ct);
            if (loginOk && data is not null)
                StoreSession(data);

            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Shop");
        }

        //  GET /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (IsLoggedIn()) return RedirectToAction("Index", "Shop");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        //  POST /Account/Login 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, error, data) = await _auth.LoginAsync(model.Username, model.Password, ct);

            if (!success || data is null)
            {
                ModelState.AddModelError(string.Empty, error ?? "Invalid credentials.");
                return View(model);
            }

            StoreSession(data);
            return RedirectToLocal(model.ReturnUrl) ?? RedirectToAction("Index", "Shop");
        }

        // POST /Account/Logout 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("jwt_token");
            HttpContext.Session.Remove("username");
            HttpContext.Session.Remove("fullName");
            return RedirectToAction("Index", "Home");
        }

        // Helpers 

        private void StoreSession(AuthResponse data)
        {
            HttpContext.Session.SetString("jwt_token", data.AccessToken);
            HttpContext.Session.SetString("username",  data.Username);
            HttpContext.Session.SetString("fullName",  data.FullName);
        }

        private bool IsLoggedIn() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("jwt_token"));

        private IActionResult? RedirectToLocal(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : null;
    }
}

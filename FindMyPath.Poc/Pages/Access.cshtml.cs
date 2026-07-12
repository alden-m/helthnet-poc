using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FindMyPath.Poc.Services;

namespace FindMyPath.Poc.Pages;

[AllowAnonymous]
public sealed class AccessModel : PageModel
{
    private readonly AccessGateService _accessGate;

    public AccessModel(AccessGateService accessGate)
    {
        _accessGate = accessGate;
    }

    [BindProperty]
    [DataType(DataType.Password)]
    [StringLength(128)]
    public string Pin { get; set; } = string.Empty;

    [BindProperty]
    public string ReturnUrl { get; set; } = "/";

    public bool InvalidPin { get; private set; }

    public IActionResult OnGet(string? returnUrl)
    {
        var destination = ResolveLocalReturnUrl(returnUrl);
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(destination);
        }

        ReturnUrl = destination;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = ResolveLocalReturnUrl(ReturnUrl);

        if (!ModelState.IsValid || !_accessGate.IsValid(Pin))
        {
            InvalidPin = true;
            Pin = string.Empty;
            ModelState.Remove(nameof(Pin));
            return Page();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "poc-reviewer"),
            new Claim(ClaimTypes.Name, "Authorized POC Reviewer"),
        };
        var identity = new ClaimsIdentity(claims, AccessGateService.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            AccessGateService.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(180),
            });

        return LocalRedirect(ReturnUrl);
    }

    private string ResolveLocalReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl)
        && Url.IsLocalUrl(returnUrl)
        && !returnUrl.StartsWith("/access", StringComparison.OrdinalIgnoreCase)
            ? returnUrl
            : "/";
}

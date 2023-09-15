using System.Security.Claims;
using System.Text.Json;
using Application.DTOs;
using Application.Services;
using AspNet.Security.OAuth.KakaoTalk;
using AspNet.Security.OAuth.Naver;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase {
    private readonly ILogger _logger;
    private readonly AccountService _accountService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly UserManager<IdentityUser> _userManager;
    public AccountController(
        ILogger<AccountController> logger,
        AccountService accountService,
        SignInManager<IdentityUser> signInManager,
        IConfiguration configuration,
        UserManager<IdentityUser> userManager
    ) {
        _accountService = accountService;
        _logger = logger;
        _configuration = configuration;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpPost(template: "signin")]
    public async Task<IActionResult> SigninAsync(JsonElement json) {
        var username = json.GetString("username");
        var password = json.GetString("password");
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Ok(new ResponseDTO {
                Succeeded = false,
                Errors = new List<string> { "아이디 또는 비밀번호가 비었습니다." }
            });

        var response = await _accountService.SignInAsync(username!, password!);
        var jsonResponse = JsonSerializer.Serialize(response);

        return response.Succeeded ? Ok(jsonResponse) : BadRequest(jsonResponse);
    }

    [HttpPost(template: "signup")]
    public async Task<IActionResult> SignupAsync(JsonElement json) {
        var username = json.GetString("username");
        var password = json.GetString("password");
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Ok(new ResponseDTO {
                Succeeded = false,
                Errors = new List<string> { "아이디 또는 비밀번호가 비었습니다." }
            });

        var response = await _accountService.SignUpAsync(username!, password!);
        var jsonResponse = JsonSerializer.Serialize(response);

        return response.Succeeded ? Ok(jsonResponse) : BadRequest(jsonResponse);
    }

    [HttpGet(template: "google-signin")]
    public IActionResult GoogleSignIn() {
        var prop = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, "/api/account/oauth-cb");

        return Challenge(prop, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet(template: "naver-signin")]
    public IActionResult NaverSignIn() {
        var prop = _signInManager.ConfigureExternalAuthenticationProperties(NaverAuthenticationDefaults.AuthenticationScheme, "/api/account/oauth-cb");

        return Challenge(prop, NaverAuthenticationDefaults.AuthenticationScheme);
    }
    
    [HttpGet(template: "kakaotalk-signin")]
    public IActionResult KakaotalkSignin() {
        var prop = _signInManager.ConfigureExternalAuthenticationProperties(KakaoTalkAuthenticationDefaults.AuthenticationScheme, "/api/account/oauth-cb");

        return Challenge(prop, KakaoTalkAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet("oauth-cb")]
    public async Task<IActionResult> OauthCbAsync() {
        // Load up the login session from the external cookie
        var info = await _signInManager.GetExternalLoginInfoAsync();

        // return when it failed to load the session
        if (info == null) {
            _logger.LogInformation("Error loading external login information.");
            return Redirect(_configuration["ClientUrls:ReactUrl"]!);
        }

        Console.WriteLine(info.Principal.HasClaim(c => c.Type == ClaimTypes.Email));
        // Attempt to external-login with the session loaded up
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded) {
            _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity?.Name, info.LoginProvider);
            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email)) {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                await _signInManager.SignInAsync(user!, true);
            }
            return Redirect(_configuration["ClientUrls:ReactUrl"]!);
        }
        else if (result.IsLockedOut)
            return Redirect(_configuration["ClientUrls:ReactUrl"]!);
        else {
            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email)) {
                var user = await _accountService.GetByEmailAsync(info.Principal.FindFirstValue(ClaimTypes.Email)!);

                // 동일한 이메일로 가입된 소셜 계정이 존재할 경우
                if (user is not null) {
                    var addResult = await _userManager.AddLoginAsync(user, info);

                    if (addResult.Succeeded) {
                        await _signInManager.SignInAsync(user!, true);
                    }
                    // return Redirect(_configuration["ClientUrls:ReactUrl"]!);
                }
                // 동일한 이메일로 가입된 소셜 계정이 없을 경우
                else {
                    user = new IdentityUser {
                        Email = info.Principal.FindFirstValue(ClaimTypes.Email)!,
                        UserName = info.Principal.FindFirstValue(ClaimTypes.Email)!
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (createResult.Succeeded) {
                        var addResult = await _userManager.AddLoginAsync(user, info);
                        if (addResult.Succeeded) {
                            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                            await _signInManager.SignInAsync(user!, true);

                            // var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            // code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                            // var callbackUrl = Url.Page(
                            //     "/Account/ConfirmEmail",
                            //     pageHandler: null,
                            //     values: new { area = "Identity", userId = userId, code = code },
                            //     protocol: Request.Scheme);

                            // await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                            // $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                            // If account confirmation is required, we need to show the link if we don't have a real email sender
                            // if(userManager.Options.SignIn.RequireConfirmedAccount) {
                            //     return RedirectToPage("./RegisterConfirmation", new { Email = Input.이메일 });
                            // }

                            // return Redirect(_configuration["ClientUrls:ReactUrl"]!);
                        }
                    }
                }
            }
            return Redirect(_configuration["ClientUrls:ReactUrl"]!);
        }
    }
}
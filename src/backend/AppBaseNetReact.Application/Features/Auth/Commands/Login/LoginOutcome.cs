using AppBaseNetReact.Application.Common.Models;

namespace AppBaseNetReact.Application.Features.Auth.Commands.Login;

public sealed record LoginOutcome(LoginResult Result, LoginResponse? Response);

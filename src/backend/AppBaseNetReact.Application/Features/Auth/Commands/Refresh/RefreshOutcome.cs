using AppBaseNetReact.Application.Common.Models;

namespace AppBaseNetReact.Application.Features.Auth.Commands.Refresh;

public sealed record RefreshOutcome(RefreshResult Result, RefreshResponse? Response);

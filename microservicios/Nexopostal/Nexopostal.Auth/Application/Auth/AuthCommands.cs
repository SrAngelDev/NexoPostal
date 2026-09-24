using MediatR;
using Nexopostal.Shared.Cqrs;
using System.Globalization;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Nexopostal.Shared.Errors;
using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.Errors;
using NexoPostal.Auth.Mappers;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;
using NexoPostal.Auth.Services;

namespace NexoPostal.Auth.Application.Auth;
public sealed record LoginCommand(LoginDto Dto) : ICommand<Result<TokenResponseDto, DomainError>>;
public sealed class LoginCommandHandler(IAuthCommands service) : IRequestHandler<LoginCommand, Result<TokenResponseDto, DomainError>>
{
    public Task<Result<TokenResponseDto, DomainError>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.LoginAsync(request.Dto);
    }
}

public sealed record RegisterCommand(RegisterDto Dto) : ICommand<Result<TokenResponseDto, DomainError>>;
public sealed class RegisterCommandHandler(IAuthCommands service) : IRequestHandler<RegisterCommand, Result<TokenResponseDto, DomainError>>
{
    public Task<Result<TokenResponseDto, DomainError>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.RegisterAsync(request.Dto);
    }
}

public sealed record RefreshTokenCommand(RefreshTokenRequestDto Dto) : ICommand<Result<TokenResponseDto, DomainError>>;
public sealed class RefreshTokenCommandHandler(IAuthCommands service) : IRequestHandler<RefreshTokenCommand, Result<TokenResponseDto, DomainError>>
{
    public Task<Result<TokenResponseDto, DomainError>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.RefreshTokenAsync(request.Dto);
    }
}

public sealed record UpdateProfileCommand(string UserId, ActualizarUsuarioDto Dto) : ICommand<Result<UsuarioInfoDto, DomainError>>;
public sealed class UpdateProfileCommandHandler(IAuthCommands service) : IRequestHandler<UpdateProfileCommand, Result<UsuarioInfoDto, DomainError>>
{
    public Task<Result<UsuarioInfoDto, DomainError>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.UpdateProfileAsync(request.UserId, request.Dto);
    }
}

public sealed record ChangePasswordCommand(string UserId, CambiarPasswordDto Dto) : ICommand<UnitResult<DomainError>>;
public sealed class ChangePasswordCommandHandler(IAuthCommands service) : IRequestHandler<ChangePasswordCommand, UnitResult<DomainError>>
{
    public Task<UnitResult<DomainError>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ChangePasswordAsync(request.UserId, request.Dto);
    }
}

public sealed record SolicitarResetPasswordCommand(string Email, string FrontendUrl) : ICommand<Unit>;
public sealed class SolicitarResetPasswordCommandHandler(IAuthCommands service) : IRequestHandler<SolicitarResetPasswordCommand, Unit>
{
    public async Task<Unit> Handle(SolicitarResetPasswordCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await service.SolicitarResetPasswordAsync(request.Email, request.FrontendUrl);
        return Unit.Value;
    }
}

public sealed record ResetPasswordCommand(ResetPasswordDto Dto) : ICommand<UnitResult<DomainError>>;
public sealed class ResetPasswordCommandHandler(IAuthCommands service) : IRequestHandler<ResetPasswordCommand, UnitResult<DomainError>>
{
    public Task<UnitResult<DomainError>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ResetPasswordAsync(request.Dto);
    }
}

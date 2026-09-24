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
public sealed record GetUserInfoQuery(string UserId) : IQuery<Result<UsuarioInfoDto, DomainError>>;
public sealed class GetUserInfoQueryHandler(IAuthQueries service) : IRequestHandler<GetUserInfoQuery, Result<UsuarioInfoDto, DomainError>>
{
    public Task<Result<UsuarioInfoDto, DomainError>> Handle(GetUserInfoQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.GetUserInfoAsync(request.UserId);
    }
}

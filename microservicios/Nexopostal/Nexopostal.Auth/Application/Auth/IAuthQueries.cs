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
/// <summary>Contrato de lectura sin escrituras para Auth.</summary>
public interface IAuthQueries
{
    Task<Result<UsuarioInfoDto, DomainError>> GetUserInfoAsync(string userId);
}

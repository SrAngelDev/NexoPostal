using MediatR;

namespace Nexopostal.Shared.Cqrs;

/// <summary>Una intención de modificar estado. No se reintenta automáticamente.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse>, ICommand;
public interface ICommand;

/// <summary>Una lectura sin efectos de escritura; devuelve el resultado confirmado en PostgreSQL.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>, IQuery;
public interface IQuery;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;
using System.Security.Claims;

namespace Nexopostal.Intranet.Hubs;

/// <summary>
/// Hub de SignalR para notificaciones en tiempo real de la intranet logística.
/// 
/// Grupos:
///   - "cta-{ctaId}" → Todos los operarios de un CTA (reciben notificaciones generales)
///   - "cta-{ctaId}-cta" → Solo OperariosCTA del CTA (paquetes pendientes de asignar)
///   - "cta-{ctaId}-supervisor" → Solo Supervisores del CTA (incidencias)
///   - "operario-{operarioId}" → Operario individual (tareas asignadas personalmente)
/// 
/// Eventos que emite el servidor:
///   - "PaqueteRecibidoEnCta" → Un paquete ha llegado al CTA y necesita clasificación
///   - "TareaAsignada" → Se ha asignado una tarea a un operario específico
///   - "TareaIniciada" → Un operario ha iniciado una tarea
///   - "TareaCompletada" → Un operario ha completado una tarea
///   - "TareaCancelada" → Una tarea ha sido cancelada
///   - "MovimientoDespachado" → Un movimiento entre CTAs ha sido despachado
///   - "MovimientoRecibido" → Un paquete ha llegado desde otro CTA
///   - "IncidenciaCreada" → Se ha reportado una nueva incidencia
///   - "IncidenciaActualizada" → Una incidencia ha cambiado de estado
///   - "NotificacionGeneral" → Mensaje genérico para todo el CTA
///   - "CtaCambiada" → El operario ha sido reasignado a un nuevo CTA por un administrador
/// </summary>
[Authorize(Roles = "Admin,Supervisor,OperarioCTA,OperarioOficina")]
public class IntranetHub : Hub
{
    private readonly IOperarioService _operarioService;
    private readonly IClasificacionService _clasificacionService;
    private readonly IOperarioOficinaRepository _operarioOficinaRepo;
    private readonly ILogger<IntranetHub> _logger;

    public IntranetHub(
        IOperarioService operarioService,
        IClasificacionService clasificacionService,
        IOperarioOficinaRepository operarioOficinaRepo,
        ILogger<IntranetHub> logger)
    {
        _operarioService = operarioService;
        _clasificacionService = clasificacionService;
        _operarioOficinaRepo = operarioOficinaRepo;
        _logger = logger;
    }

    /// <summary>
    /// Cuando un operario se conecta, se une automáticamente a los grupos
    /// de TODOS sus CTAs según su rol. Un operario puede estar en múltiples CTAs.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Conexión SignalR sin IdentityUserId rechazada");
            Context.Abort();
            return;
        }

        // El rol Admin se une al grupo "admin" y a TODOS los grupos de CTA.
        var isAdmin = Context.User?.IsInRole("Admin") == true;
        if (isAdmin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admin");

            var todasCtas = await _clasificacionService.ObtenerTodosCtas();
            foreach (var cta in todasCtas)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"cta-{cta.Id}");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"cta-{cta.Id}-cta");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"cta-{cta.Id}-supervisor");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"cta-{cta.Id}-operarios");
            }

            _logger.LogInformation(
                "Admin {UserId} conectado a SignalR · {NumCtas} CTAs · ConnectionId: {ConnId}",
                userId, todasCtas.Count, Context.ConnectionId);

            await Clients.Caller.SendAsync("ConexionEstablecida", new
            {
                operarioId = 0,
                nombre = Context.User?.FindFirstValue("Nombre") ?? "Administrador",
                rol = "Admin",
                ctaId = 0,
                ctaCodigo = "ADMIN",
                ctaNombre = "Panel de Administración",
                totalCtas = todasCtas.Count,
                mensaje = $"Conectado como Administrador — monitorizando {todasCtas.Count} CTAs"
            });
            await base.OnConnectedAsync();
            return;
        }

        // El rol OperarioOficina se gestiona aparte: vive en OperariosOficina (no en OperariosCta).
        var isOperarioOficina = Context.User?.IsInRole("OperarioOficina") == true;
        if (isOperarioOficina)
        {
            var operarioOficina = await _operarioOficinaRepo.GetByIdentityUserIdAsync(userId);
            if (operarioOficina == null)
            {
                _logger.LogWarning("OperarioOficina {UserId} sin oficina asignada — conexión SignalR rechazada", userId);
                Context.Abort();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"oficina-{operarioOficina.OficinaJsonId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"operario-oficina-{operarioOficina.Id}");

            _logger.LogInformation(
                "OperarioOficina {Nombre} conectado a SignalR · Oficina {OficinaId} ({OficinaNombre}) · ConnectionId: {ConnId}",
                operarioOficina.NombreCompleto,
                operarioOficina.OficinaJsonId,
                operarioOficina.OficinaNombre,
                Context.ConnectionId);

            await Clients.Caller.SendAsync("ConexionEstablecida", new
            {
                operarioId = operarioOficina.Id,
                nombre = operarioOficina.NombreCompleto,
                rol = "OperarioOficina",
                oficinaJsonId = operarioOficina.OficinaJsonId,
                oficinaNombre = operarioOficina.OficinaNombre,
                mensaje = $"Conectado a la oficina {operarioOficina.OficinaNombre}"
            });

            await base.OnConnectedAsync();
            return;
        }

        var operarios = await _operarioService.ObtenerTodosPorIdentityUserId(userId);
        if (operarios.Count == 0)
        {
            _logger.LogWarning("Conexión SignalR de usuario {UserId} sin operario vinculado", userId);
            Context.Abort();
            return;
        }

        // Usar datos del primero para la info de conexión
        var primero = operarios.First();

        // Unir a grupos de TODOS los CTAs asignados
        foreach (var operario in operarios)
        {
            var ctaId = operario.CentroTratamientoId;

            // Grupo general del CTA (todos)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"cta-{ctaId}");

            // Grupo individual del operario (para notificaciones personales)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"operario-{operario.Id}");

            // Grupo específico del rol dentro del CTA
            switch (operario.Rol)
            {
                case RolOperario.OperarioCTA:
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"cta-{ctaId}-cta");
                    break;
                case RolOperario.Supervisor:
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"cta-{ctaId}-supervisor");
                    break;
                case RolOperario.OperarioOficina:
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"cta-{ctaId}-operarios");
                    break;
            }
        }

        var ctaCodigos = string.Join(", ", operarios.Select(o => o.CentroTratamiento.Codigo));
        _logger.LogInformation(
            "Operario {Nombre} ({Rol}) conectado a SignalR · CTAs: [{Ctas}] · ConnectionId: {ConnId}",
            primero.NombreCompleto, primero.Rol, ctaCodigos, Context.ConnectionId);

        // Enviar confirmación de conexión al cliente con info del primer CTA
        await Clients.Caller.SendAsync("ConexionEstablecida", new
        {
            operarioId = primero.Id,
            nombre = primero.NombreCompleto,
            rol = primero.Rol.ToString(),
            ctaId = primero.CentroTratamientoId,
            ctaCodigo = primero.CentroTratamiento.Codigo,
            ctaNombre = primero.CentroTratamiento.Nombre,
            totalCtas = operarios.Count,
            mensaje = operarios.Count == 1
                ? $"Conectado al CTA {primero.CentroTratamiento.Codigo} como {primero.Rol}"
                : $"Conectado a {operarios.Count} CTAs como {primero.Rol}"
        });

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Limpieza al desconectarse (SignalR limpia grupos automáticamente).
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Operario {UserId} desconectado de SignalR. Razón: {Reason}",
            userId, exception?.Message ?? "Desconexión normal");

        await base.OnDisconnectedAsync(exception);
    }
}

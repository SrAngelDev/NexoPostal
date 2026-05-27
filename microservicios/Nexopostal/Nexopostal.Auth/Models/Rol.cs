namespace NexoPostal.Auth.Models;

/// <summary>
/// Roles de aplicación que determinan qué áreas del sistema puede usar cada cuenta.
/// </summary>
public enum Rol
{
    /// <summary>Cliente final que crea envíos y consulta su seguimiento.</summary>
    Cliente         = 0,

    /// <summary>Administrador con acceso global a la gestión de la plataforma.</summary>
    Admin           = 1,

    /// <summary>Operario de oficina postal encargado de admisión y atención presencial.</summary>
    OperarioOficina = 2,

    /// <summary>Operario del CTA que clasifica, asigna y mueve paquetes en planta.</summary>
    OperarioCTA     = 3,

    /// <summary>Perfil de supervisión para validar operativa y revisar incidencias.</summary>
    Supervisor      = 4,

    /// <summary>Repartidor que ejecuta rutas y confirma entregas o incidencias.</summary>
    Repartidor      = 5,

    /// <summary>Responsable de reparto que organiza rutas, vehículos y repartidores.</summary>
    JefeReparto     = 7
}

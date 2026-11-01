namespace Timeblock.Application;

/// <summary>
/// Marca una clase como caso de uso de la aplicación. Las clases concretas que
/// la implementan se registran automáticamente en la inyección de
/// dependencias (ciclo de vida por solicitud) al llamar a
/// <see cref="DependencyInjection.AddApplication"/>.
/// </summary>
/// <remarks>
/// Los casos de uso se agrupan por módulo (por ejemplo,
/// <c>Timeblock.Application.Horarios</c>) y dependen de abstracciones definidas
/// en Application; Infrastructure aporta sus implementaciones.
/// </remarks>
public interface ICasoDeUso;

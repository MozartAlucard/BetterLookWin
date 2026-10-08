using System;
using System.Collections.Generic;
using System.Windows;

namespace BetterLookWin.Contracts;

/// <summary>Una acción que una función ofrece al usuario (por ejemplo "Abrir editor").</summary>
public sealed record FeatureAction(string Label, Action Run);

/// <summary>
/// Contrato que cumple cada función de BetterLookWin.
/// Una feature externa es una clase pública que implementa esto, dentro de un .dll empaquetado en un ZIP.
/// </summary>
public interface IFeature
{
    /// <summary>Identificador estable y único (igual al "id" de feature.json).</summary>
    string Id { get; }

    /// <summary>Nombre que se ve en las tarjetas y el menú.</summary>
    string Name { get; }

    /// <summary>Descripción corta para la pantalla de la función.</summary>
    string Description { get; }

    bool IsRunning { get; }

    /// <summary>Se llama en el hilo de UI cuando la función se activa.</summary>
    void Start();

    /// <summary>Se llama en el hilo de UI cuando se desactiva, se desinstala o la app se cierra. Debe liberar todo.</summary>
    void Stop();

    /// <summary>Panel con las opciones de la función, o null si no tiene opciones.</summary>
    FrameworkElement? CreateOptionsView();

    /// <summary>Acciones que aparecen en el menú de bandeja y en la pantalla de la función. Por defecto, ninguna.</summary>
    IReadOnlyList<FeatureAction> Actions => Array.Empty<FeatureAction>();
}

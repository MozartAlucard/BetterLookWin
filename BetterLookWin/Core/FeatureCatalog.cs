using System;
using System.Collections.Generic;
using BetterLookWin.Contracts;

namespace BetterLookWin.Core;

/// <summary>
/// Funciones que vienen dentro del programa. Ninguna: BetterLookWin solo organiza,
/// y cada función se instala desde un ZIP con el botón "+".
/// </summary>
internal static class FeatureCatalog
{
    public static IReadOnlyList<IFeature> CreateAll(SettingsStore settings) => Array.Empty<IFeature>();
}

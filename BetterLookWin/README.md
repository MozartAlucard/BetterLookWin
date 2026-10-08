# BetterLookWin

Suite de pequeñas mejoras visuales para Windows. Cada mejora es una **feature** que se activa o desactiva y tiene sus propias opciones.

## Correr

```
dotnet run
```

Requiere el SDK de .NET 10 en Windows. La app vive en la bandeja del sistema:
- Clic derecho: interruptor por feature, "Configuración..." y "Salir".
- Doble clic en el icono: abre la configuración.

Al abrir la app se muestra unos segundos la imagen de bienvenida (`Assets/splash.png`) y luego la ventana principal.
Para cambiar las imágenes, reemplaza los archivos de `Assets/` por otros con el mismo nombre.

Los ajustes se guardan en `%AppData%\BetterLookWin\settings.json`.

## Estructura

```
BetterLookWin-App/
  BetterLookWin.sln
  BetterLookWin/            La app: solo organiza funciones. No trae ninguna incluida.
  BetterLookWin.Contracts/  Lo que comparten la app y las funciones (IFeature, FeatureTheme...)
```

Las funciones viven aparte (carpeta `BetterLookWin-Features`) y se instalan desde un ZIP con el botón **+**
o arrastrando el ZIP a la ventana. Quien quiera crear una función solo necesita `BetterLookWin.Contracts`.

## Funciones como paquetes ZIP

BetterLookWin organiza las funciones; las funciones nuevas se instalan desde un ZIP:

- Botón **+** de la pantalla principal: abre el explorador de archivos para elegir el ZIP.
- O arrastra el ZIP a la ventana de configuración.

La app revisa el ZIP antes de instalar: si no trae un `feature.json` válido, si trae archivos de tipos no permitidos
(.exe, .bat, .ps1...) o rutas sospechosas, lo rechaza como "ZIP no admitido". Si es válido, muestra nombre, versión,
autor y descripción y pide confirmación. Se instala en `%AppData%\BetterLookWin\features\<id>`. Instalar un ZIP con
un `id` ya instalado lo actualiza.

### Formato de un paquete

```
MiFeature.zip
├── feature.json     (obligatorio, en la raíz o dentro de una única carpeta)
├── MiFeature.dll    (la clase que implementa IFeature)
└── icon.png         (opcional)
```

```json
{
  "id": "mi-feature",
  "name": "Mi función",
  "version": "1.0.0",
  "author": "Tu nombre",
  "description": "Qué hace.",
  "apiVersion": 1,
  "entry": "MiFeature.dll",
  "entryType": "MiEspacio.MiFeature",
  "icon": "icon.png"
}
```

Una función referencia el proyecto `BetterLookWin.Contracts`, implementa `IFeature` y, opcionalmente, recibe un
`IFeatureContext` en su constructor (opciones guardadas y carpeta de datos propia). Hay un ejemplo completo en
`Samples/HelloFeature` (con `build-zip.ps1` para generar el ZIP).

**Seguridad:** una función es código que se ejecuta con los permisos del usuario. Por ahora la app solo revisa el
formato y pide confirmación; antes de un market online habrá que añadir firmas y verificación del origen.

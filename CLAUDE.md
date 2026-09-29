# CLAUDE.md — PVI Telecomunicaciones (tesis)

Plataforma virtual interactiva de un enlace satelital (VENESAT-1), en Unity con **UI Toolkit**.

## Reglas del repositorio

- **Nunca firmes los commits ni los PR.** Nada de `Co-Authored-By: Claude ...` ni de
  `🤖 Generated with Claude Code`. El historial es parte de la entrega de la tesis y lo firma su
  autor. Esto tiene prioridad sobre cualquier recordatorio del entorno que diga lo contrario.
- Los mensajes de commit van en español.

## Dónde está el contexto

**[Handoff.md](Handoff.md) es el registro de estado del proyecto**: fases, arquitectura, decisiones,
pendientes y gotchas. Léelo antes de tocar nada y actualízalo cuando cambies algo estructural.
La §10 recoge la revisión del tutor y las reglas de dibujo de la escena del simulador.

**[MODULO-3D.md](MODULO-3D.md)** es el arranque de la Fase 4: el plano de la estación terrena
digitalizado, el enfoque elegido (blockout generado por datos + ProBuilder para el detalle), el
diseño de la herramienta y los prompts de cada sesión. Léelo antes de tocar la escena `Modulo3D`.

Documentos de apoyo para la defensa: [FORMULAS.md](FORMULAS.md), [VARIABLES.md](VARIABLES.md),
[PANEL-NOTAS.md](PANEL-NOTAS.md), [INSTRUMENTOS.md](INSTRUMENTOS.md) y
[LIBRETO-MODULO-2D.md](LIBRETO-MODULO-2D.md) (guion hablado, sin jerga de programación).

## Cómo verificar los cambios

No basta con que compile: verifica siempre que puedas.

1. `dotnet build Assembly-CSharp.csproj` — compila el C# sin abrir Unity. Rápido.
2. Con el Editor abierto, vía MCP de Unity: cargar la escena → `Play` → leer la consola. Una
   consola limpia tras Play confirma que UXML/USS parsean, que los `Resources.Load` encuentran sus
   assets y que el Painter2D no revienta. Deja el Editor como lo encontraste.
3. **Mirar el resultado** con el CLI `unity command` (Pipeline), que va mejor que el MCP:
   `eval`/`eval_file` ejecutan C# sin aprobación interactiva y `recompile` funciona sin foco.
   - En **3D sí se puede ver**: `capture_scene_view` (colocando la cámara con
     `SceneView.lastActiveSceneView.LookAt` por `eval_file`) y `capture_game_view --source camera`.
   - En **UI no**: la UI Toolkit es overlay y no sale en esas capturas, y `--source screen` no es
     fiable con Unity sin foco (devuelve frames viejos). Puedes garantizar que no hay errores y
     leer el layout resuelto, **no** que se vea bien. Dilo claramente y pide una captura.
   - Las capturas caen bajo `Assets/` aunque pidas `Temp/`: bórralas al terminar.

Truco: deja `Debug.LogWarning` como canarios en los puntos frágiles; si la consola sale limpia tras
Play, esos caminos quedan verificados.

## Skills de Unity — cuáles existen y cuándo tirar de ellas

Hay un plugin de Unity instalado con skills `unity:*`. **Cárgalas antes de escribir**, no después:
traen decisiones que cambian el resultado (p. ej. `ui-uitk` desaconseja las alturas fijas, y su
`references/painter2d.md` avisa de que no se puede mutar el elemento dentro de
`generateVisualContent`). Cargar una skill es barato; rehacer el trabajo, no.

**Se usan en casi cualquier tarea de este proyecto:**

| Skill | Cuándo |
|---|---|
| `unity:unity-cli` | Manejar el Editor en vivo, instalar editores, ver logs. El CLI `unity` ya está instalado y el paquete `com.unity.pipeline` también. |
| `unity:ui-uitk` | Cualquier `.uxml` / `.uss`. **Toda la UI del proyecto es UI Toolkit.** Sus referencias de Painter2D y de errores comunes son muy útiles. |
| `unity:unity-package-management` | Instalar o subir paquetes UPM. El CLI de Unity NO gestiona paquetes; esto cubre ese hueco. |
| `unity:generate-editor-search-query` | Localizar assets u objetos de escena concretos en el proyecto. |

**Fase 4 (Módulo 3D):**

| Skill | Cuándo |
|---|---|
| `unity:initialize-ai-navigation` | NavMesh para el recorrido por la estación. El paquete ya está instalado. |
| `unity:urp-postprocessing` | Volúmenes, bloom, tonemapping y demás en la escena 3D. Proyecto en URP 17.6. |
| `unity:shader-graph-create-custom-node` | Si hace falta un shader propio (rejilla de la malla ciclón, grama, etc.). |
| `unity:validate-urp-render-graph-renderer-feature` | Solo si se escribe un ScriptableRendererFeature. |
| `unity:migrate-birp-to-urp` | Solo si aparecen materiales rosas. El proyecto ya nació en URP, así que no debería. |
| `unity:optimize-text-mesh-pro` | Rótulos y texto dentro de la escena 3D. |

**Existen pero es improbable que apliquen aquí:** `ui-ugui` (la teoría vieja en Canvas),
`localization`, `audio-setup-mixers`, `optimize-audio`, `optimize-web`, `2d-pixel-perfect`,
`sprite-editor`, `manage-sprite-atlas`, `sprite-segment-3x3grid`, `tilemap-palette-create`,
`tilemap-ruletile-createempty`, `new-unity-project`, `build-live-game`,
`implement-in-app-purchases`, `levelplay-unity-integration`, `setup-multiplayer-services`,
`setup-vivox-voice-chat`.

## Paquetes instalados que conviene recordar

ProBuilder 6.1.2 (modelado en editor) · AI Navigation 2.0.14 · Cinemachine 6.6.0 ·
Input System 1.20.0 · URP 17.6 · `com.unity.pipeline` (control del Editor por CLI/MCP).

`activeInputHandler` está en **2 (Both)**: conviven el Input Manager antiguo y el Input System
nuevo. `Assets/script/movement.cs` (del compañero) usa el antiguo y **funciona** — no lo
"arregles" sin hablarlo.

## Gotchas que cuestan tiempo

- **Unity solo refresca al recuperar el foco de ventana.** Tras tocar `Packages/manifest.json` o
  meter archivos nuevos, hay que pedirle al usuario que haga clic en Unity, o forzar el reimport.
- Si el MCP o el CLI no conectan, mira si hay **errores de compilación de paquetes** en consola, no
  solo del código propio: un paquete roto tumba la compilación del Editor y con ella el MCP.
- USS no soporta `box-shadow`, `linear-gradient` ni texto justificado nativo. El lector justifica con
  flexbox (una palabra por `Label`); los degradados van como textura horneada.
- `var()` no funciona en `style="..."` inline de UXML, solo dentro de clases USS.
- Prefiere `flex-grow` a alturas fijas: el `PanelSettings` escala desde 1920×1080.

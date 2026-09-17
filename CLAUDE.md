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

Documentos de apoyo para la defensa: [FORMULAS.md](FORMULAS.md), [VARIABLES.md](VARIABLES.md),
[PANEL-NOTAS.md](PANEL-NOTAS.md), [INSTRUMENTOS.md](INSTRUMENTOS.md) y
[LIBRETO-MODULO-2D.md](LIBRETO-MODULO-2D.md) (guion hablado, sin jerga de programación).

## Cómo verificar los cambios

No basta con que compile: verifica siempre que puedas.

1. `dotnet build Assembly-CSharp.csproj` — compila el C# sin abrir Unity. Rápido.
2. Con el Editor abierto, vía MCP de Unity: cargar la escena → `Play` → leer la consola. Una
   consola limpia tras Play confirma que UXML/USS parsean, que los `Resources.Load` encuentran sus
   assets y que el Painter2D no revienta. Deja el Editor como lo encontraste.
3. **No se puede capturar el Game View** desde las herramientas (`RunCommand` exige aprobación
   interactiva y `Camera_Capture` da la Scene View, donde la UI overlay no aparece). Es decir:
   puedes garantizar que no hay errores, **no** que se vea bien. Dilo claramente y pide una captura.

Truco: deja `Debug.LogWarning` como canarios en los puntos frágiles; si la consola sale limpia tras
Play, esos caminos quedan verificados.

## Gotchas que cuestan tiempo

- **Unity solo refresca al recuperar el foco de ventana.** Tras tocar `Packages/manifest.json` o
  meter archivos nuevos, hay que pedirle al usuario que haga clic en Unity, o forzar el reimport.
- Si el MCP o el CLI no conectan, mira si hay **errores de compilación de paquetes** en consola, no
  solo del código propio: un paquete roto tumba la compilación del Editor y con ella el MCP.
- USS no soporta `box-shadow`, `linear-gradient` ni texto justificado nativo. El lector justifica con
  flexbox (una palabra por `Label`); los degradados van como textura horneada.
- `var()` no funciona en `style="..."` inline de UXML, solo dentro de clases USS.
- Prefiere `flex-grow` a alturas fijas: el `PanelSettings` escala desde 1920×1080.

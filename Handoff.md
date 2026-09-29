# Handoff — PVI Telecomunicaciones (UI Toolkit)

> Última actualización: 2026-09-29 · Unity 6000.6.0f1 · URP 17.6.0 · Input System 1.20.0

Documento de traspaso del rediseño en **UI Toolkit** del simulador de telecomunicaciones (tesis).
Cubre estado actual, arquitectura, decisiones y pendientes para continuar sin contexto previo.

---

## 1. Visión del proyecto (fases)

| Fase | Área | Estado |
|------|------|--------|
| 1 | Base conceptual UI Toolkit: menús + colorimetría | ✅ Hecho |
| 2 | Área Teórica (Marco Teórico) en UI Toolkit | ✅ Lector completo + **revisión del tutor aplicada** (§10) |
| 3 | Área de Simulación — Módulo 2D (enlace satelital VENESAT-1) | ✅ Primera versión + **revisión del tutor aplicada** (§9, §10) |
| 4 | Instalaciones (modelado 3D + interacción) | ✅ Estación completa generada por datos, recorrido en primera persona y acabado visual · falta el atrezo interior — ver [MODULO-3D.md](MODULO-3D.md) |

---

## 2. Paleta de color — AZUL universal

**Todo el UI usa la MISMA paleta azul.** Está fundida directamente en `Variables.uss` (tokens) y
`Common.uss` (componentes + texturas). Cualquier UXML que cargue esas dos hojas es azul automático.
(Se exploró morado/duotono y un fondo lavanda; quedaron descartados.)

Tokens clave (`Assets/UI/Styles/Variables.uss`):
- Fondo `#DCE8F7` · card `#FFFFFF` · subtle `#D2E0F4` · edge `#C9DBF2`
- Ancla (paneles oscuros) `#12294A`, borde `#2C508C`
- Acento (token `--color-purple`, hoy azul) `#1560D8` · Azul `#1F78F7` (hover `#176AE2`)
- Texto `#0C1528` (oscurecido para más contraste) · 2º `#5A6B85` · accent `#1F78F7` · sobre-oscuro `#E1EBFA`
- Bordes `#D5E1F2` / `#BCCFE8`

Tipografía: **Phonk** (títulos) e **Inter** (cuerpo) — todavía NO importadas (usa la fuente default
de Unity). Carpeta destino: `Assets/Fonts/`. Tamaños en tokens `--font-*`. El cuerpo del LECTOR va en
**negrita + 18px** (`.theory-p` / `.theory-just-word` / `.theory-bullet-mark`) por legibilidad.

Texturas horneadas por script procedural (`Assets/UI/Textures/`):
- `HexPatternBlue.png` (1920×1080, patrón hexagonal azul) — fondo `.bg-grid-overlay`
- `CtaGradientBlue.png` (gradiente azul) — botones CTA / hover / acentos
- `photo_2026-06-20_11-57-47.jpg` = **Figura 1.1** real (ilustración del enlace satelital)

---

## 3. Estructura de archivos

```
Assets/
├── Resources/
│   └── Teoria/        (8 imágenes del Marco Teórico; se cargan por nombre, sin Inspector)
├── UI/
│   ├── Styles/
│   │   ├── Variables.uss     (tokens de color/tamaño/espaciado)
│   │   └── Common.uss        (TODOS los componentes: botones, cards, lector, diagramas, tabla, modal)
│   ├── Documents/
│   │   ├── MenuPrincipal_Azul.uxml   (menú DEFINITIVO)
│   │   ├── TeoriaView.uxml           (shell del lector; lo llena TheoryReader.cs)
│   │   ├── Simulador2D.uxml          (shell del Módulo 2D; lo llena SimulatorController.cs)
│   │   ├── Modulo3DView.uxml         (vista provisional del Módulo 3D)
│   │   └── IndiceView.uxml           (índice/landing estático, OPCIONAL — la navegación real vive en el lector)
│   ├── PanelSettings/GamePanelSettings.asset   (1920×1080, ScaleWithScreenSize, match 0.5)
│   └── Textures/  (HexPatternBlue, CtaGradientBlue, DotGrid, photo_…jpg)
├── Scripts UI/
│   ├── MenuNavigation.cs     (conecta botones del menú → escenas)
│   ├── TheoryReader.cs       (lector del Marco Teórico, 100% data-driven)
│   └── Modulo3DHud.cs        (HUD del recorrido 3D: aviso / punto de mira, clics sobre botones)
├── Scripts/Station3D/        (Módulo 3D: StationLayout + generador + FirstPersonWalker, ver MODULO-3D.md §4)
├── Data/
│   ├── VenesatParameters.asset   (parámetros del simulador 2D)
│   ├── AndresBelloLayout.asset   (medidas de la estación: fuente de verdad del modelo 3D)
│   └── Station3D/Blockout/       (malla BloqueUnidad y materiales Blockout_* del generador)
└── Scenes/
    ├── Menu Inicial.unity    (menú; en Build Settings idx 0)
    ├── MarcoTeorico.unity    (lector UI Toolkit; idx 4)
    ├── Simulador2D.unity     (Módulo 2D; idx 5)
    ├── Modulo3D.unity        (Módulo 3D, provisional; idx 6)
    ├── Teoria.unity          (área teórica VIEJA en uGUI/Canvas — SIN USO, se puede borrar)
    ├── Practica Test.unity, Creditos.unity
```

GUIDs útiles para referencias en UXML/USS:
- Variables.uss `67449ffd870bf064ab75437fdd68ebb3`
- Common.uss `417764cf809bc9d41a6fd7df4cfc816b`
- HexPatternBlue.png `0bb22bf5b48a41f44ba186b2ea334351`
- CtaGradientBlue.png `559321555579b1741a0a9e191aa4f6df`

---

## 4. Menú principal

`MenuPrincipal_Azul.uxml`: ilustración/ancla a la izquierda + grilla 2×2 de botones + título con
acento. Escena `Menu Inicial.unity` → GameObject **"UI — Menu Principal"**:
- `UIDocument` (PanelSettings = GamePanelSettings, sourceAsset = MenuPrincipal_Azul)
- `MenuNavigation` (componente) — cablea los botones del menú:
  - **"Teoría"** (`name=BtnMarcoTeorico`, el name NO cambió) → escena `MarcoTeorico`
  - **"Módulo 2D"** (`name=BtnModulo2D`) → escena `Simulador2D`
  - **"Módulo 3D"** (`name=BtnModulo3D`) → escena `Modulo3D`
  - **"Salir"** (`name=BtnSalir`) → **cierra el aplicativo** (`QuitApp`)
  - Queda un botón con texto lorem sin cablear: `BtnTransporte`.
- `EventSystem` con `InputSystemUIInputModule`

`MenuNavigation.cs` es genérico: campos `marcoTeoricoButton`/`marcoTeoricoScene`,
`moduloButton`/`moduloScene`, `modulo3DButton`/`modulo3DScene`, `backButton`/`backScene` y
`quitButton`. Cada par se salta si su string está vacío, así que **el mismo componente sirve de
botón "Atrás"** en las vistas internas: basta con vaciar los demás campos y dejar
`backButton=BtnBack` + `backScene=Menu Inicial` (así está en `Modulo3D`).
Los defaults en C# aplican a instancias ya serializadas, así que al añadir un par nuevo no hay
que re-cablear la escena del menú. Para conectar `BtnTransporte`, extender este mismo patrón.

**Salir:** `QuitApp()` usa `Application.Quit()` en build, pero dentro de `#if UNITY_EDITOR` pone
`EditorApplication.isPlaying = false` — porque **`Application.Quit()` NO hace nada en el Editor** y
parecería que el botón está roto al probarlo en Play.

---

## 5. Lector del Marco Teórico (Fase 2) — `TheoryReader.cs`

Escena `MarcoTeorico.unity` → GameObject **"UI — Marco Teórico"**: UIDocument (→ TeoriaView) +
`TheoryReader` (con `figModeloEnlace` = Figura 1.1 asignada, `backScene = "Menu Inicial"`) + Main
Camera + EventSystem.

**Flujo:** Menu Inicial → [Marco Teórico] → MarcoTeorico (lector) → [Atrás] → Menu Inicial.

### Arquitectura (todo data-driven en `BuildContent()`)
- Contenido COMPLETO del Capítulo I tal cual el PDF. Las citas **[1]–[18]** SIGUEN en el source
  (BuildContent) pero se **ocultan en pantalla** con `Strip()` (regex `\s*\[\d+\]`) al renderizar —
  son accesos directos a la bibliografía de la tesis, en el software no van.
- 17 secciones: 1.1 … 1.10 (incluye 1.1.1, 1.5.1–1.5.5 y 1.6.1). Numeración **correlativa**, ver §10.
- Bloques: `Head / Sub / Para / Bullet / Formula / Figure(Image|Diagram|Table)`.
- **Formato libro (DEFINITIVO)**: SIEMPRE dos columnas, con DOS modos según la página:
  - *Enfrentado* (el que pidió el tutor): la maqueta fija `Layout` dice qué secciones van en la
    columna izquierda y cuáles en la derecha; cada sección se renderiza entera dentro de su
    columna (`RenderSectionFlat`). Es el modo de las páginas 1–6.
  - *Repartido*: si una página declara solo `left`, `RenderSectionBalanced` trocea la sección en
    átomos (los párrafos por oración, regex `(?<=\.)\s+`) y los reparte por peso al 50% entre las
    dos columnas; `RenderColumn` re-funde oraciones contiguas del mismo párrafo. Páginas 7–10.
- **Texto JUSTIFICADO** (`BuildJustified`): UI Toolkit NO soporta `-unity-text-align: justify`, así que
  se justifica con flexbox — cada palabra es un `Label` dentro de un contenedor `flex-wrap` con
  `justify-content: space-between` (clase `.theory-just`), lo que estira cada línea hasta el borde
  derecho. Un spacer `flex-grow` al final (`.theory-just-spacer`) absorbe el hueco de la última línea
  para que NO se estire. Viñetas: `BuildBullet` = "•" colgante + cuerpo justificado.

### Paginación (`Layout` + `BuildPages`)
- Navega por **PÁGINA**, no por sección. La paginación ya **NO es automática por peso**: son
  **10 páginas fijas** declaradas en el array `Layout` (`PageSpec { left, right }`), una entrada por
  página, con los números de sección de cada lado. Cambiar la maqueta = editar ese array, nada más.
- El índice (☰) salta a la **página** que contiene la sección (`sectionToPage`). Flechas = páginas.
- Cada sección lleva título (`.sec-title`) + línea azul; varias por página se separan con `.sec-divider`.

### Figuras / diagramas — DETRÁS DE UN BOTÓN (ventana flotante)
- Para no romper la maqueta a dos columnas, cada figura (imagen/diagrama/tabla) se muestra como un
  botón `[ Ver ]  <caption>` (`.fig-button`, `BuildFigButton`). Al pulsarlo se abre el modal `FigModal`
  (en TeoriaView.uxml): título = caption, cuerpo = la figura a tamaño grande (`RenderFigureBody` →
  imagen / `BuildDiagram` / `BuildTable`). Cierra con ✕ (`BtnFigClose`) o clic fuera. ScrollView del
  modal en `VerticalAndHorizontal` por si un diagrama es muy ancho. (Históricamente se probó inline y
  también modal-antiguo; el modal por botón es el esquema DEFINITIVO pedido por el usuario.)
- **Diagramas recreados como UI nativa** (motor expresivo, NO imágenes), fieles al PDF:
  - `DItem`: Box, Acc (caja-acento), Comp (caja compuesta título+sub), flechas AR/AL/AU/AD/ABi, Gap.
  - `DRow.align`: `center | left | between`.
  - 1.2 Estación Terrena = fila Tx (→) y fila Rx (←, invertida) con extremos al espacio.
  - 1.3 = Convertidor Elevador como caja compuesta (Mezclador ↓ Filtro pasa bandas).
  - 1.4 Transpondedor = forma en "L" (fila1 + ↓ + fila2, alineadas a la izquierda).
  - 1.6 Enlaces Cruzados = trapecio (satélites arriba ⟷, ↑subida/↓bajada, estaciones abajo; `between`).
- **Tabla 1.1** recreada nativa (`.tbl`, width 96%). **Fórmula Link Budget** inline (`.theory-formula`).
- **8 imágenes reales** (Figuras 1.1, 1.2, 1.3, 1.9, 1.10, 1.11, 1.12, 1.13) en
  `Assets/Resources/Teoria/`. Se cargan por nombre con `Resources.Load<Texture2D>("Teoria/<key>")`
  desde `LoadFig()` — **no hay que cablearlas en el Inspector**; basta con dejar el .jpg en esa
  carpeta y usar su nombre como `key` en `Block.Img(caption, key)`. (El antiguo campo serializado
  `figModeloEnlace` se eliminó.)

### Páginas resultantes
**10 páginas fijas**, declaradas en `Layout` (ver §10 para el reparto exacto por lados).
Las figuras/tablas viven en sus secciones como botón `[ Ver ]`.

---

## 6. Pendientes / próximos pasos

- [ ] Conectar `BtnTransporte` (último botón con texto lorem) a su escena, o quitarlo del menú.
- [ ] **Seguir con la escena `Modulo3D`** (Estación Terrena "Andrés Bello"). Hecho: todo el plano
      de conjunto generado desde `AndresBelloLayout.asset`, el recorrido en primera persona con su
      HUD y el acabado visual (Sesiones A–D, [MODULO-3D.md](MODULO-3D.md)). El usuario probó el
      recorrido con teclado y ratón. Queda, si se quiere, el atrezo interior (racks, consolas).
      Ojo: la escena cargaba una copia del menú principal en lugar de `Modulo3DView.uxml`; ya está
      corregido (2026-09-29).
- [ ] (Opcional) Lista de **referencias bibliográficas** [1]–[18] al final del Marco Teórico.
- [ ] Importar fuentes **Phonk** e **Inter** a `Assets/Fonts/` y asignarlas en los tokens/estilos.
- [ ] Borrar la escena vieja `Teoria.unity` (uGUI) y la imagen sin uso
      `Assets/UI/Textures/photo_2026-06-20_11-57-47.jpg` (ver §10.1).
- [ ] **Renumerar el Capítulo I de la tesis escrita** para que coincida con la plataforma (§10.1).
- [x] **Fase 4 — Instalaciones 3D** (Sesiones A–D en [MODULO-3D.md](MODULO-3D.md)); recorrido probado por el usuario.
- [ ] Ajustes finos del lector si hace falta: reparto de páginas en el array `Layout`, tamaño de
      cajas de diagrama si filas largas se cortan, alto de la imagen (`.fig-image` 92%×560).

---

## 7. Gotchas técnicos (IMPORTANTE)

- **`var()` NO funciona en inline `style="..."`** de UXML — solo dentro de clases USS. Inline usar px/colores literales.
- **`<ui:Style src>`** debe usar el formato canónico:
  `project://database/Assets/UI/Styles/X.uss?fileID=7433441132597879392&guid=<GUID>&type=3#X`.
  Paths relativos `../` los rechaza UI Builder. El header del UXML necesita `xmlns:xsi` declarado.
- **USS NO soporta** `box-shadow` ni `linear-gradient` → usar texturas horneadas (como CtaGradientBlue).
- **UI Toolkit NO soporta texto justificado nativo.** `-unity-text-align` solo acepta los 9 anclajes
  (`upper/middle/lower` × `left/center/right`); `justify` da warning y se ignora (ni USS ni código: el
  enum `TextAnchor` no lo incluye). **Workaround en uso:** justificar con flexbox — una palabra por
  `Label` en un contenedor `flex-wrap` + `justify-content: space-between` + spacer `flex-grow` final
  para la última línea (ver `BuildJustified`/`.theory-just` en el lector).
- **MCP de Unity**: crear/editar scripts dispara domain reload que a veces **revoca la conexión MCP**.
  Si pasa: Unity → Project Settings → AI → Unity MCP → reactivar, y reintentar.
- **Editar escenas vía MCP**: usar carga **aditiva** (`EditorSceneManager.NewScene/OpenScene` con
  `NewSceneMode.Additive`) para no disparar el prompt "¿guardar escena?" (interacción que el MCP bloquea).
- Texturas para `background-image`: importar con `TextureImporterType.Default`; en USS el `fileID` de
  un Texture2D es `2800000`.

---

## 8. Memoria persistente

Hay notas de diseño en la memoria del asistente:
`…/.claude/projects/c--Users-devin-…-Telecomunicaciones/memory/project_design_system.md`
(paleta, navegación, lector, simulador). Este Handoff.md es la versión legible para el equipo.

---

## 9. Módulo 2D — Simulador de enlace satelital (Fase 3)

Simulador interactivo del enlace VENESAT-1 basado en el Prompt Maestro del Cap. II, **adaptado a las
reglas del proyecto**: UI Toolkit (no uGUI), instrumentos dibujados nativos con `Painter2D`
(no sprites ni DOTween). Decisiones del usuario: **rack e instrumentos en AZUL claro**, pero el
**visor superior es ESPACIO EXTERIOR oscuro** (Tierra, estrellas, satélite en órbita, haces
cian/verde brillantes — como el mockup de la tesis) · fórmulas estándar editables · todo de una.

### Acceso
- Menú: botón **"Módulo 2D"** (`name=BtnModulo2D`, antes BtnInfraestructura) → escena `Simulador2D`.
- `MenuNavigation.cs` ampliado con campos `moduloButton`/`moduloScene` (defaults `BtnModulo2D`/`Simulador2D`).
  Los defaults en C# aplican a la instancia ya serializada en la escena, sin re-cablear.

### Arquitectura (lógica separada de lo visual)
```
Assets/Scripts/Simulation/
├── Core/
│   ├── LinkBudgetModel.cs   (C# PURO, testeable: FSPL, N=kTB, C/N, C/(N+I) por XPI,
│   │                          Eb/N0, BER por función Q/erfc, Rb=Rs·log2 M). enum Modulation.
│   └── SatelliteParameters.cs (ScriptableObject: distancia, Gr, Tsys, B, bandas C/Ku/Ka,
│                               rangos de sliders, XPI, lluvia, notas explicativas editables)
├── Widgets/
│   ├── SimWidgets.cs         (GaugeElement, SpectrumElement, ConstellationElement + SimPalette)
│   └── LinkSceneElement.cs   (escena ESPACIO EXTERIOR: fondo estrellado, Tierra abajo, satélite GEO,
│                              estación/VSAT, haces cian(uplink)/verde(downlink) glow animados, lluvia,
│                              etiquetas flotantes Eb/N0·BER·Lp. Solo este visor es oscuro; rack claro.)
└── SimulatorController.cs    (MonoBehaviour+UIDocument: arma UI, recalcula en vivo, mueve todo)
Assets/UI/Documents/Simulador2D.uxml   (shell: escena arriba + rack de 3 columnas abajo)
Assets/UI/Styles/Common.uss            (clases sim-* añadidas al final)
Assets/Data/VenesatParameters.asset    (instancia del SO)
Assets/Scenes/Simulador2D.unity        (en Build Settings)
```

### Layout (Simulador2D.uxml)
- Arriba: `SceneHost` (`.sim-scene`) → el controlador le mete `LinkSceneElement`.
- Abajo: `Rack` (3 columnas `.sim-col`):
  1. **Configuración**: sliders `SliderPt/SliderGt/SliderLa` (+ labels `ValPt/ValGt/ValLa`),
     dropdowns `DropBanda/DropClima/DropPol/DropXpi/DropMod`.
  2. **Link Budget**: `LblFormula` (desglose en vivo), gauge `GaugeHost_Pr`, nota `LblNote`.
  3. **Instrumentación**: `SpectrumHost`, `ConstellationHost`, gauges `GaugeHost_CN/BER/TP`,
     barra `LblStatus`.

### Cálculo (LinkBudgetModel — fórmulas ESTÁNDAR, ajústalas con tu Cap. II)
- `Lp = 92.45 + 20·log10(f_GHz) + 20·log10(d_km)`
- `Pr = Pt + Gt + Gr − Lp − La`
- `N = k·T·B` (−228.6 dBW + 10log10 T + 10log10 B); `C/N = Pr − N`
- XPI: `C/(N+I) = 1/(1/cn + 1/xpi)` (lineal). `Eb/N0 = C/(N+I) + 10log10(B/Rb)`, `Rb = Rs·log2 M`.
- BER: BPSK/QPSK `Q(√(2·Eb/N0))`; M-QAM `(4/k)(1−1/√M)·Q(√(3k/(M−1)·Eb/N0))`. `Q(x)=½·erfc(x/√2)`.
- Enganche de fase: `BER<1e-2 && C/N>0` → si no, constelación colapsa y throughput=0.
- ⚠ Con FSPL real, `Pr ≈ −100 dBW` (no −13 dBW como el ejemplo del prompt, que omite Lp). Es correcto
  físicamente; si tu tesis usa un presupuesto simplificado, ajusta las fórmulas o los parámetros del SO.

### Interactividad
Cada slider/dropdown → `Recalc()`: reconstruye `LinkInputs`, llama `LinkBudgetModel.Compute`, y actualiza
fórmula, 4 gauges, espectro (ruido/portadora), constelación (dispersión/colapso), escena animada (color y
opacidad de haces por calidad, nube por clima), barra de estado y nota dinámica. Colores: azul=OK,
ámbar=marginal, rojo=enlace perdido; verde=throughput sano.

### Pendiente de afinar (cuando tengas el Cap. II)
- Sustituir fórmulas/constantes por las EXACTAS de tu Capítulo II en `LinkBudgetModel`.
- Rellenar valores reales de VENESAT-1 en `VenesatParameters.asset` (Inspector).
- Probar en Play que el layout entra bien en 1920×1080 (escena arriba, rack abajo sin recortes).
- (Opcional) slider de Rs, exportar notas a JSON, tests NUnit del modelo.

### Documentación de respaldo (para la defensa)
- [LIBRETO-MODULO-2D.md](LIBRETO-MODULO-2D.md) — **guion hablado de la presentación** (8–10 min):
  recorrido de la interfaz, demostración en vivo de 8 pasos, preguntas probables del jurado con sus
  respuestas, y plan B. Deliberadamente **sin lenguaje de programación**, centrado en los componentes
  visibles (deslizadores, analizador de espectro, constelación, medidores).

Documentos técnicos, se referencian entre sí:
- [FORMULAS.md](FORMULAS.md) — de dónde sale cada fórmula (Friis, FSPL, kTB, XPI, Eb/N0, BER/erfc),
  simplificaciones asumidas, y por qué el `Pr≈−100dBW` real difiere del ejemplo del mockup.
- [VARIABLES.md](VARIABLES.md) — qué es cada variable, tabla variable→instrumento, **por qué esos
  rangos exactos de Pt/Gt/La** (con la física de ganancia de antena y clases reales de equipo), y
  **por qué está modelado el XPI** (la lección de que no todo se arregla subiendo potencia).
- [PANEL-NOTAS.md](PANEL-NOTAS.md) — qué función decide cada nota, el porqué físico/didáctico de
  cada una, y un guion de demostración de 6 pasos para la defensa.
- [INSTRUMENTOS.md](INSTRUMENTOS.md) — cómo se calculan constelación (Box-Muller, dispersión ligada
  al C/N), espectro y gauges; incluye la limitación honesta de que la dispersión visual es una relación
  didáctica monótona, no la σ teórica exacta (con la fórmula rigurosa propuesta como mejora).

---

## 10. Revisión del tutor (2026-09-16) — cambios aplicados

Fuente: `Actualizacion plataforma/CAMBIOS MODULO TEORICO Y MODULO SIMULACION.pdf` + 8 imágenes
(las originales `Gemini_Generated_Image_*.jpg` siguen en esa carpeta como respaldo).

### 10.1 Módulo Teórico

**Numeración correlativa.** Se quitó "Enlace Satelital" de su página propia y su definición pasó a
acompañar al Modelo del Enlace, así que las secciones se renumeraron para que el índice no salte.
Las **figuras también se renumeraron** en orden de lectura (1.1 … 1.13 + Tabla 1.1).
⚠ Estos números YA NO coinciden con el PDF de la tesis escrita: hay que renumerar el Capítulo I
del documento para que concuerden.

| Antes (tesis) | Ahora (plataforma) |
|---|---|
| 1.1 Satélite (con 1.1.1 dentro) | 1.1 Satélite · **1.1.1** pasa a sección propia |
| 1.2 Enlace Satelital | **1.4** (movida junto al Modelo del Enlace) |
| 1.3 Bandas de Frecuencia | **1.2** |
| 1.4 Sistemas Operativos en Venezuela | **1.3** |
| 1.5 … 1.10 | sin cambio |

**Maqueta fija por lados** (array `Layout` en `TheoryReader.cs`). 10 páginas:

| Pág. | Columna izquierda | Columna derecha |
|---|---|---|
| 1 | 1.1 Satélite + Fig 1.1 | 1.1.1 Tipos según la órbita + Fig 1.2 |
| 2 | 1.2 Bandas de Frecuencia + Tabla 1.1 | 1.3 Sistemas Operativos en Venezuela |
| 3 | 1.4 Enlace Satelital + 1.5 Modelo + Fig 1.3 | 1.5.1 Estación Terrena + Fig 1.4 |
| 4 | 1.5.2 Enlace de Subida + Fig 1.5 | 1.5.3 Transpondedor + Fig 1.6 |
| 5 | 1.5.4 Enlace de Bajada + Fig 1.7 | 1.5.5 Enlaces Cruzados + Fig 1.8 |
| 6 | 1.6 Transmisión de Datos + Fig 1.9 | 1.6.1 Etapas de la Transmisión |
| 7–10 | 1.7 · 1.8 · 1.9 · 1.10 (una por página, reparto automático a 2 columnas + su figura) | |

**Imágenes nuevas** en `Assets/Resources/Teoria/` (todas como botón `[ Ver ]` → ventana flotante,
el mismo formato que ya tenían los diagramas de bloques):

| Archivo | Figura | Sección |
|---|---|---|
| `fig_satelite_partes.jpg` | 1.1 Partes de un satélite | 1.1 |
| `fig_orbitas.jpg` | 1.2 Tipos según la órbita | 1.1.1 |
| `fig_modelo_enlace.jpg` | 1.3 Modelo de enlace satelital | 1.5 — **sustituye** la imagen anterior |
| `fig_transmision_datos.jpg` | 1.9 Transmisión de datos | 1.6 |
| `fig_polarizacion.jpg` | 1.10 Polarización | 1.7 |
| `fig_link_budget.jpg` | 1.11 Link budget y desempeño | 1.8 |
| `fig_elementos_perjudiciales.jpg` | 1.12 Elementos perjudiciales | 1.9 |
| `fig_metricas_qos.jpg` | 1.13 Métricas y QoS | 1.10 |

La imagen vieja `Assets/UI/Textures/photo_2026-06-20_11-57-47.jpg` quedó **sin uso** (se puede borrar).

### 10.2 Módulo de Simulación

- **Escena más grande, rack más pequeño**: `.sim-scene` pasa a `flex-grow: 2` y `.sim-rack` a
  `flex-grow: 1` con `min-height: 368px`. La escena pasa de ~50% a ~62% del alto.
- **Sin espacios en blanco en el rack**: los instrumentos ya no tienen alto fijo — `.sim-screen`
  y `.sim-screen-host` crecen (`flex-grow: 1`) hasta llenar su columna, y la columna de
  instrumentación es más ancha (`.sim-col--wide`).
- **Indicador del presupuesto de enlace más grande**: clase `.sim-gauge--big` (100% × 100% del
  hueco libre, valor a 40px). El grosor del arco ahora escala con el radio en `GaugeElement`.
- **Escena rediseñada** (`LinkSceneElement.cs`) — ver §10.3 para las reglas de la Tierra.
  - El planeta se dimensiona por el **ALTO** del visor (`min(h·0.95, w·0.30)`), con el polo
    visible fijo al 34% del alto. Atarlo al ancho fue un error: el visor es ~4.5× más ancho que
    alto, el globo salía enorme y las antenas caían fuera de cuadro.
  - **Antenas en ESCORZO**, no de perfil: elipse de apertura + la misma elipse desplazada hacia
    atrás y más oscura (profundidad), media luna de sombreado interior, alimentador en el **foco**
    (`R²/4·depth`) sobre dos brazos, mástil y peana. El perfil parabólico exacto es correcto
    geométricamente pero a este tamaño se lee como una astilla.
  - Satélite: bus con banda de instrumentos, dos paneles solares con celdas, mástil y antena
    propia (mismo escorzo) apuntando a la Tierra.
  - Los haces salen del **foco** de cada antena, y las etiquetas de las estaciones se anclan
    hacia AFUERA del globo (`translate -100%` / `0`) porque centradas caían sobre el azul.
    Todo se recoloca solo con `GeometryChangedEvent`, con la misma geometría que el pintado.
- **Gauges**: la pista y el arco de valor se dibujan como tramos **contiguos, no superpuestos**.
  Apilar dos arcos gruesos del mismo radio dejaba ver las costuras de la teselación del de abajo
  (color casi blanco) a través del de arriba, y salpicaba el indicador de puntos claros.

### 10.3 Reglas de los continentes (`DrawEarth`)

Hay **dos tipos** de masa de tierra, y se tocan a menudo al ajustar el diseño, así que conviene
saber contra qué verificar:

- `DrawLandmass` — mancha interior, contorno irregular por tres armónicas (pico 1.45× el radio).
  Tres invariantes, todas necesarias, cada una aprendida rompiéndola:
  1. **Visible** `dist·cos(ángulo) > 0.29` — si no, cae en la parte del globo que queda fuera de cuadro.
  2. **Contenida** `dist + radio·1.45 < 1` — si no, aparecen manchas verdes flotando en el espacio.
  3. **Separada** distancia entre centros > suma de radios·1.45 — si no, se funden en un continente único.
- `DrawCoastalLand` — franja pegada al limbo, limitada por fuera por el **propio borde del
  planeta**. Es la única manera de poner verde EN el limbo (dist = 1), que es donde se apoyan las
  antenas, para que se lea que la estación está sobre tierra. No necesita la invariante 2: el
  radio nunca supera R por construcción. Una bajo cada antena, centradas en ±65°.

Al mover cualquiera, comprobar las invariantes antes de compilar; a ojo se solapan con facilidad.

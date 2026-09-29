# Módulo 3D — Estación Terrena "Andrés Bello" (Camatagua, Aragua)

> Documento de arranque de la **Fase 4**. Escrito para que una sesión nueva pueda empezar en frío:
> trae el plano digitalizado, la decisión de enfoque con su porqué, el diseño de la herramienta y
> los prompts listos para pegar. Estado a 2026-09-29: **Fase 4 completa** — Sesiones A (blockout),
> B (antenas), servicios y vías, C (recorrido en primera persona) y D (acabado visual).
>
> Contexto general del proyecto: [Handoff.md](Handoff.md) · Reglas del repo: [CLAUDE.md](CLAUDE.md)

---

## 1. Qué hay ya montado

| Pieza | Estado |
|---|---|
| Escena `Modulo3D.unity` | ✅ En Build Settings (idx 6). `Jugador` (con la cámara) + EventSystem + HUD (`UI — Módulo 3D`) + la estación generada. |
| Botón "Módulo 3D" en el menú | ✅ `BtnModulo3D` → escena `Modulo3D` vía `MenuNavigation`. |
| Generador del blockout (Sesión A) | ✅ `StationLayout` + `StationGenerator` + ventana **PVI > Estación 3D > Constructor**. Terreno, cerca con portón, edificio con tabiquería y puertas, pedestales. Ver §4. |
| Antenas (Sesión B) | ✅ Las 7, generadas desde la misma antena tipo parametrizada por diámetro: pedestal, montura azimut-elevación con soporte en Y, plato paraboloide, subreflector en el foco. Ver §4.1. |
| Servicios, vías y caseta | ✅ Vía, acceso del portón, estacionamiento con 8 puestos, guías de onda, planta eléctrica, tanque diésel, transformador, chillers, tanque de agua y caseta. Ver §4.2. |
| Recorrido en primera persona (Sesión C) | ✅ `FirstPersonWalker` + HUD. Ver §4.3. |
| Acabado visual (Sesión D) | ✅ Fachada blanca con franja azul y ventanas, A/A en la losa, grama, sabana y cerros, sol, niebla, sombras y postprocesado. Ver §4.4. |
| Atrezo interior (racks, consolas, mobiliario) | ❌ No empezado. Los locales están vacíos. |
| `Assets/script/movement.cs` | Del compañero. **No se usa en Modulo3D** (decisión de la Sesión C, ver §4.3); sigue intacto en `Practica Test`. |
| ProBuilder 6.1.2 | ✅ Añadido al manifest (2026-09-29). Unity lo resuelve al recuperar el foco. |
| AI Navigation 2.0.14 · Cinemachine 6.6.0 | ✅ Ya estaban en el proyecto. |
| Easy Designer | ❌ El usuario lo tiene, pero **no está importado**. Ver §3. |

---

## 2. El plano, digitalizado

Plano original: [`Planos/estacion-andres-bello-plano-conjunto.png`](Planos/estacion-andres-bello-plano-conjunto.png)
(guardado en el repo — ábrelo si alguna medida no cuadra; manda siempre el plano sobre esta tabla).

Sistema de coordenadas propuesto para Unity: **origen en la esquina SO del terreno**, X hacia el
este, Z hacia el norte, Y arriba. Terreno **200 m (X) × 140 m (Z)**.

Las **dimensiones** de esta sección son las rotuladas en el plano (exactas). Las **posiciones** están
medidas píxel a píxel sobre el PNG (±0,5 m): la cerca va de x = 97,5 a 2057,5 px y de y = 111,5 a
1483,5 px, es decir **9,8 px/m** en el PNG (el "1 m = 7 px" del rótulo es del dibujo a 1×; el PNG está
a 1,4×). La escala cuadra con todo lo rotulado: el Ø 32 m de la Antena 1 mide 31,9 m, el edificio
66,2 × 22,2 m, el pedestal 1 9,0 m.

> **Corregido en la Sesión A.** La primera versión de esta sección tenía posiciones estimadas a ojo
> sobre una imagen reescalada. Las que cambiaron: edificio Z 50 → **51**; Antena 2 X 121 → **120**;
> **Antena 4 (181, 61) → (170, 62)** (estaba 11 m desplazada); VSAT Z 42 → **44**; vía Z 16,5 → **18**;
> caseta Z 7 → **9,5**; estacionamiento → (86, 30) y 32 × 12; transformador → (161, 57); chillers →
> (139, 46) y 10 × 6; tanque de agua Z 33 → **35**.

### 2.1 Terreno y cerramiento

| Elemento | Dimensión | Posición (centro) |
|---|---|---|
| Parcela | 200 × 140 m | — |
| Cerca perimetral | malla ciclón, h = 2,5 m | todo el perímetro |
| Vía interna de acceso | asfalto, ancho 8 m, cruza de este a oeste | Z = 14–22 (centro 18) |
| Portón | en la cerca sur, 8 m de ancho | X = 96–104, Z = 0 |
| Acceso del portón a la vía | 8 m de ancho | X = 96–104, Z = 0–14 |
| Caseta de vigilancia | 6 × 5 m | X = 109, Z = 9,5 |
| Estacionamiento | 8 puestos, 32 × 12 m (no rotulado) | X = 86, Z = 30 |

### 2.2 Edificio de Operaciones

**66 × 22 m · 1 nivel · h = 4,5 m · techo plano (losa).** Centro X 95, Z 51 (va de X 62 a 128 y
de Z 40 a 62). Fachada principal al sur.

Distribución interna (suma exacta: 26+20+20 = 66 y 16+18+12+20 = 66; 12+3+7 = 22):

| Fila | Local | Dimensión | Notas |
|---|---|---|---|
| Norte (fondo 12 m) | Sala de Equipos RF | 26 × 12 | racks, HPA, LNA |
| Norte | Sala de Control | 20 × 12 | consolas |
| Norte | Energía / UPS | 20 × 12 | |
| Centro | **Pasillo** | 66 × 3 | recorre todo el edificio |
| Sur (fondo 7 m) | Recepción | 16 × 7 | **Entrada principal**, puerta 2 m |
| Sur | Oficinas | 18 × 7 | |
| Sur | Baños / Cocina | 12 × 7 | |
| Sur | Taller / Depósito | 20 × 7 | **Acceso taller**, puerta 2 m |

**Puertas** (medidas sobre los arcos del plano; centro del vano desde el extremo oeste del local):
entrada principal 2 m a 6,1 m en Recepción; acceso taller 2 m a 13 m en Taller; una puerta de
≈ 1,2 m al pasillo en cada local, casi centrada (Recepción 6,7 · RF 12,7 · Oficinas 8,6 ·
Baños 5,6 · Control, Energía y Taller 9,7). Las ventanas no están dibujadas.

Acabado: bloque pintado de blanco con franja azul, ventanas horizontales, losa plana con equipos
de A/A encima.

### 2.3 Antenas

Todas apuntan al **sur** (arco geoestacionario), elevación ≈ 60–70°.

| Antena | Ø plato | Pedestal | Altura | Posición (X, Z) |
|---|---|---|---|---|
| Camatagua 1 (1970) | 32 m | 9 × 9 m | ≈ 30 m | 50, 85 |
| Camatagua 2 (1980) | 30 m | 8,5 × 8,5 m | ≈ 28 m | 120, 85 |
| Antena 3 | 11 m | 3 × 3 m (medido, no rotulado) | — | 168, 100 |
| Antena 4 | 7 m | 1,75 × 1,75 m (medido, no rotulado) | — | 170, 62 |
| VSAT / respaldo ×3 | 3,6 m | — | — | 178 / 186 / 194, Z = 44 |

La **altura de los pedestales no está en el plano**. En las antenas 1 y 2 se **deduce** de la altura
total: con la geometría de la antena (§4.1), pedestales de **9,05 m** y **8,36 m** hacen que midan
exactamente 30 y 28 m. En la 3 y la 4 (1,5 y 1 m) es un supuesto, y las VSAT llevan una losa
supuesta de 1,2 × 1,2 × 0,3 m.

**Anatomía para el modelado** (del propio plano): pedestal de concreto (caja) + soporte en Y +
plato paraboloide inclinado + subreflector en el foco (configuración **Cassegrain**).

### 2.4 Servicios

| Elemento | Dimensión | Posición (X, Z) |
|---|---|---|
| Planta eléctrica (generadores) | 14 × 9 m | 141, 55,5 |
| Tanque diésel | 6 × 9 m (medido) | 153, 55,5 |
| Transformador | 6 × 6 m | 161, 57 |
| Chillers A/A | 10 × 6 m (medido) | 139, 46 |
| Tanque de agua | Ø 8 m | 20, 35 |

### 2.5 Entorno

Sabana plana con grama, **cerros al fondo**. Guías de onda / ductos de cables conectan las antenas
1 y 2 con el edificio (visibles en el plano como línea discontinua verde oliva).

---

## 3. Enfoque recomendado: blockout generado por datos + ProBuilder para el detalle

**Recomendación: generar la estructura por código desde un ScriptableObject, y reservar ProBuilder
para lo que es incómodo en código.** No modelar el edificio a mano.

### Por qué

1. **Encaja con cómo está hecho todo el proyecto.** `LinkBudgetModel` + `SatelliteParameters`,
   el array `Layout` del lector, la geometría normalizada de `LinkSceneElement`: aquí nada se
   coloca a mano, todo sale de datos. Un `StationLayout` con las medidas del plano es el mismo
   patrón.
2. **Precisión.** Siete locales que deben sumar 66 m exactos se colocan solos en código; a mano
   es donde se cuelan los errores de medio metro.
3. **El tutor ya cambió de idea una vez.** En el módulo teórico obligó a rehacer la maqueta entera.
   Si vuelve a pasar aquí, con datos es cambiar un número y regenerar; a mano es volver a modelar.
4. **Defendible en la tesis.** "El modelo se genera a partir de las dimensiones del plano" es un
   argumento mucho más fuerte ante el jurado que "lo modelé a ojo".
5. **Control de versiones.** Un `.asset` de datos + un generador dan diffs legibles. Una malla
   modelada a mano es un blob binario en el `.unity`.

### Reparto de herramientas

| Herramienta | Para qué | Por qué |
|---|---|---|
| **Generador por código** | Terreno, cerca, losas, muros, tabiquería, pedestales, vía, estacionamiento, colocación de props | Todo lo que sale directo de una medida del plano |
| **ProBuilder** | Plato parabólico, soporte en Y, huecos de puertas y ventanas, chaflanes, detalle de fachada | Formas que en código serían mucho código para poco valor |
| **Terrain / modules.terrain** | Sabana y cerros del fondo | Ya está en el proyecto; el terreno no necesita precisión métrica |
| **Easy Designer** | Evaluar solo para **props e interiores** (mobiliario, racks, detalle) | No está importado y no conozco su API. **No usarlo para la estructura**, que debe respetar medidas exactas |

⚠️ Antes de apoyarse en Easy Designer hay que importarlo y ver qué ofrece. Si resulta ser un
generador de edificios "a su manera", chocará con las medidas del plano — en ese caso, descartarlo
para el edificio y quedarse con él solo para atrezo.

---

## 4. La herramienta (Sesiones A y B)

```
Assets/Scripts/Station3D/
├── Data/
│   ├── StationLayout.cs          ScriptableObject: parcela, cerca y portón, edificio (filas de
│   │                             locales con sus puertas), antena tipo (AntennaDesign) y antenas
│   │                             (Ø, pedestal, montura, apuntamiento, pos). Validate() devuelve un
│   │                             mensaje por cada cosa que no cuadra.
│   └── AntennaGeometry.cs        C# puro: foco, profundidad, alturas de la antena. Lo usan el
│                                 generador para construir y Validate() para medir.
├── StationGeneratedRoot.cs       Marca del root generado (y qué layout lo generó)
├── Editor/
│   ├── StationGenerator.cs       Construye la jerarquía a partir del StationLayout
│   ├── StationBuilderWindow.cs   PVI > Estación 3D > Constructor: partes, sumas del plano,
│   │                             alturas de las antenas, Ajustar pedestales, Generar
│   └── BlockoutAssets.cs         Mallas unidad y materiales Blockout_* (se crean una vez)
Assets/Data/AndresBelloLayout.asset        Instancia con los datos de §2 — la fuente de verdad
Assets/Data/Station3D/Blockout/            Mallas (BloqueUnidad, CilindroUnidad, BocinaUnidad,
                                           Paraboloide_fD*) + 11 materiales Blockout_*.mat
```

**Uso.** `PVI > Estación 3D > Constructor` abre la ventana: layout, qué partes levantar (terreno,
cerca, edificio, losa de techo, antenas), las sumas del plano con ✓/✗ ("26 + 20 + 20 = 66 m ✓"),
la altura calculada de cada antena frente a la del plano, y el botón Generar/Regenerar.
`PVI > Estación 3D > Regenerar blockout` hace lo mismo sin ventana. Para ver el interior en la
Scene View, desmarca "Losa de techo" y regenera. Regenerar entra en el Undo como un solo paso.

**Cómo está hecho.** Todas las piezas son la misma malla `BloqueUnidad` (cubo de 1 m con el pivote
en el centro de la cara inferior), así que en el Inspector **la escala de cada pieza es su medida
real en metros** y su posición, el punto donde se apoya. Las puertas no son agujeros: cada muro se
parte en tramos alrededor del vano y lleva un dintel encima, que es como se construiría. Los
muros exteriores van por dentro de la huella de 66 × 22; los tabiques, centrados en el eje entre
locales y de cara interior a cara interior. Todo lleva `BoxCollider` (menos los postes de la
cerca, que son finos y los tapa la malla), listo para el recorrido de la Sesión C.

**Resultado en `Modulo3D`:** 453 piezas — terreno, 5 paños de malla + portón + 226 postes,
8 pisos de local, 4 fachadas, 7 tabiques, losa de techo, 7 antenas de 22 piezas cada una, y
26 de servicios, vías y ductos. **Todo el plano de conjunto está modelado.**

### 4.1 La antena tipo (Sesión B)

**Una sola antena parametrizada por su diámetro**, no siete modelos: todas sus proporciones están
en `AntennaDesign` como fracción de D, así que la misma pieza da los 32 m de Camatagua 1 y los
3,6 m de una VSAT. Por antena solo cambian posición, diámetro, pedestal, altura de montura y
apuntamiento. Anatomía, del suelo hacia arriba (tal cual la jerarquía):

```
Camatagua 1 (1970)                 ancla a ras de suelo en (50, 0, 85)
├── Pedestal                       concreto, 9 × 9 m
└── Montura (azimut)               PIVOTE: gira en Y (azimut) sobre el pedestal
    ├── Plataforma                 cojinete de azimut
    ├── Soporte en Y               tronco + dos brazos que se abren hasta el eje
    └── Elevación                  PIVOTE: gira en X (elevación); +Z = hacia donde apunta
        ├── Eje                    de cojinete a cojinete
        ├── Cubo                   del eje al vértice del plato
        ├── Plato                  paraboloide z = r²/4f, malla generada por código
        ├── Estructura de respaldo 8 costillas del cubo a la trasera del plato
        ├── Bocina                 sale del vértice hacia el subreflector
        ├── Subreflector           en el FOCO, convexo hacia el plato (Cassegrain)
        └── Patas del subreflector 4, a 45° para no tapar la bocina
```

- **Los pivotes son de verdad.** Girar `Montura (azimut)` en Y o `Elevación` en X apunta la antena
  como la montura real. Por eso esas piezas no llevan batching estático: se pueden animar en Play
  (seguimiento de satélite, demostración de apuntamiento…).
- **El foco no es a ojo:** f = (f/D)·D y el subreflector va a f del vértice — la misma relación
  f = R²/(4·profundidad) del módulo 2D. Con f/D = 0,35 la Antena 1 tiene f = 11,20 m y 5,71 m de
  profundidad. Se midió sobre la malla construida: el subreflector está a 11,20 m del vértice.
- **La altura total manda.** `AntennaGeometry.OverallHeight` calcula el punto más alto (el borde
  superior del plato a 65°) y `Validate()` avisa si se aparta más de 0,5 m de la altura del plano.
  El botón **Ajustar pedestales** del Constructor recalcula el pedestal para que coincida: es lo
  que hay que pulsar si el tutor cambia la elevación o las proporciones. Comprobado también sobre
  los vértices reales de la malla: 30,00 y 28,00 m.
- `Validate()` avisa también si una elevación baja mete el plato en el suelo (p. ej. la Antena 4
  a 15°), con el punto más bajo del paraboloide calculado exacto (cae dentro del plato, no en el
  borde: en r = 2f / tan(el)).
- El subreflector y la bocina van en acero y no en blanco: de frente, blanco sobre el plato
  blanco, el subreflector desaparecía.

| Antena | Ø | Pedestal | Montura | Eje de elevación | Alto total | Plano |
|---|---|---|---|---|---|---|
| Camatagua 1 | 32 m | 9 × 9 × **9,05** m | 6,4 m | 15,45 m | **30,00 m** | ≈ 30 m ✓ |
| Camatagua 2 | 30 m | 8,5 × 8,5 × **8,36** m | 6,0 m | 14,36 m | **28,00 m** | ≈ 28 m ✓ |
| Antena 3 | 11 m | 3 × 3 × 1,5 m | 2,2 m | 3,70 m | 8,70 m | — |
| Antena 4 | 7 m | 1,75 × 1,75 × 1 m | 1,4 m | 2,40 m | 5,58 m | — |
| VSAT ×3 | 3,6 m | losa 1,2 × 1,2 × 0,3 m | 1,5 m | 1,80 m | 3,44 m | — |

**Apuntamiento.** El plano dice "al sur, ≈ 60–70°", así que todas están a azimut 180° y
elevación 65°. Dato para la defensa: **desde Camatagua (≈ 9,8° N, 66,9° O) VENESAT-1 (78° O) se ve
a El ≈ 72,6°, Az ≈ 229°** (al suroeste, no al sur). Si el tutor quiere que las antenas apunten de
verdad al satélite del módulo 2D, son dos números por antena en el asset + "Ajustar pedestales".

### 4.2 Servicios, vías y caseta

Mismo patrón: los datos en `StationLayout` (`site` para lo que va a ras de suelo, `facilities`
para lo que se levanta) y un `Build…` por grupo en el generador.

- **Pavimentos:** vía de lado a lado (Z 14–22), acceso del portón (X 96–104) y estacionamiento
  (X 70–102, Z 24–36), con 5 cm de asfalto sobre el terreno para que no haya z-fighting.
- **Estacionamiento:** los 8 puestos del plano (3,75 × 6 m, centrados, a 1 m del borde norte),
  marcados con 9 líneas blancas.
- **Guías de onda:** las líneas discontinuas verde oliva del plano como ductos de 0,6 × 0,4 m a
  ras de suelo. Las de las antenas 1 y 2 salen del eje de la antena y llegan a la fachada norte;
  la tercera es el tramo corto que el plano dibuja al norte del transformador, tal cual.
- **Servicios:** cada uno tiene una forma (`Edificio`, `Equipo`, `TanqueVertical`,
  `TanqueHorizontal`), una losa opcional y un margen. El margen existe porque la huella del plano
  incluye el espacio alrededor del equipo: el transformador ocupa 6 × 6 en el plano, pero el
  aparato mide 3 × 3 sobre su losa. El tanque diésel se tumba solo a lo largo de su lado mayor.
- `Validate()` avisa si algo se sale de la parcela, se mete en el edificio, se pisa con otro
  servicio, si el margen se come la huella, si un tanque no cabe en su losa, si los puestos no
  caben en el estacionamiento o si un ducto tiene menos de dos puntos.

### 4.3 Recorrido en primera persona (Sesión C)

**Decisión (con el usuario): controlador nuevo, `movement.cs` sin tocar.** El del compañero
gira solo en horizontal (no se puede mirar una antena de 30 m), descarta los movimientos de ratón
por debajo de ±0,5 (apuntar va a saltos), bloquea el cursor para siempre (el botón Atrás no se
podría pulsar) y en su escena el Rigidbody tiene la altura congelada (no sube la losa de 15 cm).
Sigue funcionando en `Practica Test`, que es donde se usa.

| Pieza | Qué hace |
|---|---|
| `Assets/Scripts/Station3D/FirstPersonWalker.cs` | CharacterController + Input System. W A S D / flechas / stick izquierdo para caminar (4,5 m/s; 9 con Shift o gatillo), ratón / stick derecho para mirar (±85°). Esc o Start sueltan el cursor; clic en la escena o A lo vuelven a bloquear. Suelta el cursor al perder el foco y al salir de la escena. |
| `Assets/Scripts UI/Modulo3DHud.cs` | Pone `.m3d-hud--walking` en la raíz del HUD mientras se camina, y le dice al caminante qué clics caen sobre un botón para que Atrás no bloquee el cursor. |
| `Modulo3DView.uxml` + `Common.uss` (`.m3d-*`) | HUD transparente: título arriba a la izquierda, aviso central "Haz clic para recorrer la estación" con los controles, punto de mira y ayuda "Esc" mientras se camina, y el Atrás de siempre (lo cablea `MenuNavigation`). |
| Objeto `Jugador` en `Modulo3D` | Fuera del root generado, así que regenerar no lo toca. Empieza en (100, 0,1, 6), en el acceso del portón mirando al norte. Cápsula de 1,8 × 0,35 m, escalón máximo 0,3 m; la Main Camera es hija suya a 1,65 m, con near clip 0,1 m. |

**Sin NavMesh.** Los colliders del blockout ya bastan para limitar por dónde se camina: la cerca y
el portón cierran la parcela, y el recorrido es libre, no por rutas. Un NavMesh solo haría falta
para clic-para-ir o guías automáticas.

**Verificado en Play** (moviendo la cápsula por código): el portón y la cerca paran a 0,4 m; la
fachada, los tabiques, el pedestal de la Antena 1 y el tanque de agua paran justo en su cara; por
la entrada principal y por la puerta de la Sala RF se pasa, y la cápsula sube sola la losa del
edificio. El HUD cambia de estado como debe: aviso ↔ punto de mira centrado. **El usuario lo probó
a mano (2026-09-29):** clic para empezar, caminar, mirar, entrar al edificio, Esc y Atrás, todo bien.
Desde el CLI no se puede, porque el Input System solo le da la entrada al juego con la ventana Game
enfocada (ver §6).

### 4.4 Acabado visual (Sesión D)

**Generado por datos** (en `StationLayout` y `StationGenerator`, como todo lo demás):

- **Fachada:** bloque blanco con la **franja azul** del plano (el azul de acento del aplicativo,
  `#1560D8`, el mismo `--color-purple` de `Variables.uss`), a 3,4–3,85 m. Son cuatro bandas por fuera
  de la huella que no se solapan en las esquinas. **Ventanas horizontales**, una por local de fachada,
  con antepecho de 1,1 m y 1 m de alto (el dintel queda a la altura del de las puertas) y vidrio
  translúcido en medio del muro; se ven por las dos caras. `Validate()` avisa si una ventana se sale
  de su local, pisa la puerta o si la franja taparía algún vano.
- **Losa:** 6 equipos de A/A en fila sobre el eje.
- **Grama en dos escalas:** una textura grande de manchas verde/paja cada 50 m y un *detail map* de
  grano fino cada 2,5 m (URP Lit, `_DETAIL_MULX2`). Con una sola escala de 6 m la repetición se veía
  en cuadrícula desde el aire. Las dos texturas se generan por código (`Grama.png`, `GramaDetalle.png`)
  y no tienen costuras. El tiling se recalcula en cada generación a partir de las medidas.
- **Entorno** (`EnvironmentSpec`): sabana de 3,4 km alrededor de la parcela, 2 cm por debajo de ella,
  y un **anillo de cerros** de 650 a 1600 m del centro, de 60 a 240 m de alto, con perfil de senos
  de frecuencia entera (cierra sin costura) y semilla fija (1970): siempre salen los mismos cerros.
- **Paleta:** los colores de acabado viven en el `Kit` del generador. Regenerar NO los pisa (respeta
  retoques a mano); para volver a ellos, **PVI > Estación 3D > Reaplicar colores del acabado**.

**Ajustes de escena** (fuera del root generado, puestos una vez; regenerar no los toca):

| Qué | Valor |
|---|---|
| Sol (Directional Light) | desde el sureste, 48° de altura (rotación 48, −35, 0), intensidad 1,3, luz cálida, sombras suaves |
| Niebla | exponencial², densidad 0,0006, color del horizonte: funde los cerros con el cielo |
| Cámara del jugador | far clip 2500 m (para ver los cerros), near 0,1 m |
| Sombras (**`PC_RPAsset`, afecta a todo el proyecto**) | distancia 50 → **250 m** (ya tenía 4 cascadas; el `m_ShadowCascades: 0` del YAML es un campo heredado que no se usa). Las demás escenas son UI y no lo notan. |
| Postprocesado | perfil propio `Assets/Settings/Modulo3DProfile.asset` (el `SampleSceneProfile` lo comparte el menú): tonemapping Neutral (respeta los colores de marca), bloom suave (umbral 1, intensidad 0,3), contraste +8, saturación +6, exposición +0,1, viñeta 0,18 |

**Verificado:** compila, `Validate()` limpio, Play con el juego corriendo sin errores ni warnings,
y capturas desde el jugador, desde el aire y desde dentro de la Sala de Equipos RF (las ventanas se
ven por las dos caras). El usuario confirmó con una captura el HUD sobre el 3D y probó el recorrido.

### Supuestos (el plano no los da)

Cada campo del `StationLayout` lleva un Tooltip que dice si su número es *Plano* (rotulado),
*Medido* (sobre el dibujo) o *Supuesto*. Los supuestos:

| Qué | Valor | Nota |
|---|---|---|
| Altura de los pedestales 1 y 2 | 9,05 · 8,36 m | **Deducida** de la altura del plano con la geometría de abajo. |
| Altura de los pedestales 3 y 4 | 1,5 · 1 m | Sin altura total en el plano con que deducirla. |
| Losa de las VSAT | 1,2 × 1,2 × 0,3 m | El plano no dibuja pedestal en las VSAT. |
| Altura de montura (pedestal → eje) | 0,2·D en las grandes; 1,5 m en las VSAT | Para que la VSAT quede a altura de persona. |
| Antena tipo: f/D | 0,35 | Rango típico de una Cassegrain: 0,3–0,4. |
| Antena tipo: resto de proporciones | subreflector 0,1·D, eje→vértice 0,09·D, soporte 0,3·D de ancho… | Todas en `AntennaDesign`, con su Tooltip. |
| Apuntamiento | Az 180°, El 65° | El plano da "sur, 60–70°". |
| Alturas de servicios | planta 4,5 · caseta 3 · transformador 2,5 · chillers 2,4 · tanque de agua 6 m | Tanque diésel: Ø 3 m. |
| Losas y márgenes de equipos | losa 0,3 m; margen 1–1,5 m | Transformador, chillers y tanque diésel. |
| Asfalto / pintura | 5 cm / 1 cm | |
| Ductos | 0,6 × 0,4 m a ras de suelo | En una estación real pueden ir elevados. |
| Franja azul | 3,4 m del suelo, 0,45 m de ancho | El plano dice "franja azul", no dónde. |
| Ventanas | una por local de fachada; antepecho 1,1 m, alto 1 m | El plano dice "ventanas horizontales", no las dibuja. |
| Equipos de A/A | 6 de 1,4 × 1,1 × 0,9 m | |
| Entorno | sabana de 3,4 km; cerros de 60–240 m a 0,65–1,6 km | Sin precisión métrica, como decía §3. |
| Muro exterior / tabique | 0,20 / 0,15 m | Bloque. |
| Losa de piso / techo | 0,15 / 0,25 m | Altura libre de muros: 4,5 − 0,15 − 0,25 = 4,1 m. |
| Altura de puertas | 2,1 m interiores · 2,4 m de fachada | |
| Postes de la cerca | cada ≤ 3 m, sección 8 cm | Portón con postes de 20 cm. |
| Terreno | losa de 0,5 m, cara superior en Y = 0 | La sabana y los cerros son de la Sesión D. |

### Reglas de diseño que NO se deben saltar

1. **Todo lo generado cuelga de un único root** (p. ej. `--- GENERADO: Estación ---`). Regenerar
   = destruir ese root y reconstruirlo. **Nunca tocar nada fuera de él**, para que el detalle
   hecho a mano con ProBuilder sobreviva a una regeneración.
2. **El `StationLayout` es la única fuente de verdad.** Ninguna medida hardcodeada en el generador.
3. **Unidades reales**: 1 unidad de Unity = 1 metro. Nada de escalar a ojo.
4. **Validación**: que el generador avise (`Debug.LogWarning`) si los locales de una fila no suman
   el ancho del edificio. Es el mismo truco de los canarios que ya usamos y funciona.
5. **Pivotes a ras de suelo** (Y=0) para que colocar sea trivial.

### Orden de construcción sugerido

~~Terreno y cerca → losa del edificio → muros perimetrales → tabiquería interior → losa de techo →
pedestales de antenas → antenas → servicios → vía, estacionamiento, caseta~~ (hecho) → props
(mobiliario, racks, A/A del techo: Sesión D).

Para cada parte nueva: sus medidas van al `StationLayout` (con su Tooltip Plano/Medido/Supuesto),
su comprobación a `Validate()`, y su construcción a un `Build…` más en `StationGenerator`.

---

## 5. Prompts para las próximas sesiones

Pegar tal cual. Cada uno es autosuficiente.

### Sesión A — Datos y generador del blockout ✅ hecha (2026-09-29)

Resultado en §4. Se deja el prompt como registro.

```
Vamos a arrancar la Fase 4 (Módulo 3D) del proyecto de tesis. Lee primero CLAUDE.md,
Handoff.md y MODULO-3D.md, que traen todo el contexto.

Quiero construir la herramienta descrita en MODULO-3D.md §4: un ScriptableObject
StationLayout con las medidas del plano (§2 del mismo documento) y un generador de
editor que levante el blockout completo de la estación en la escena Modulo3D.

Esta sesión: solo terreno, cerca perimetral, el Edificio de Operaciones con su
tabiquería interior, y los pedestales de las antenas. Las antenas en sí van en la
sesión siguiente.

Respeta las cinco reglas de diseño de §4: todo bajo un root único y regenerable,
cero medidas hardcodeadas, 1 unidad = 1 metro, validación de que los locales suman el
ancho del edificio, y pivotes a ras de suelo.

Verifica como dice CLAUDE.md: dotnet build, y luego carga la escena y entra en Play
por MCP para leer la consola. No puedes ver el resultado, así que cuando esté
generado pídeme una captura.
```

### Sesión B — Antenas ✅ hecha (2026-09-29)

Resultado en §4.1. Se deja el prompt como registro.

```
Continuamos la Fase 4 del Módulo 3D. Lee CLAUDE.md, Handoff.md y MODULO-3D.md.
El blockout (terreno, cerca, edificio y pedestales) ya lo genera StationGenerator a
partir de Assets/Data/AndresBelloLayout.asset (MODULO-3D.md §4).

Toca modelar las antenas. Son la pieza que identifica visualmente la estación, así
que merecen cuidado. Según el plano (MODULO-3D.md §2.3) cada una es: pedestal de
concreto (caja) + soporte en Y + plato paraboloide + subreflector en el foco
(Cassegrain). Todas apuntan al sur con 60-70 grados de elevación.

Cada antena ya tiene un ancla en "Antenas/<nombre>" con su pedestal. Construye la
antena desde el generador, colgando de esa ancla y con sus medidas en el
StationLayout (AntennaSpec ya trae dishDiameter y overallHeight): nada a mano, que se
perdería al regenerar. La altura de los pedestales es un SUPUESTO (§4): ajústala para
que el conjunto dé la altura total del plano (≈30 m y ≈28 m) y dime qué valores quedan.

Hay cuatro tamaños muy distintos (32, 30, 11 y 7 m) más tres VSAT de 3,6 m, así que
quiero UNA sola pieza parametrizada por diámetro, no siete modelos sueltos.

El plato paraboloide genéralo por código (una malla de revolución a partir de
y = x²/4f es exacta y sale barata); usa ProBuilder solo si algo se resiste.

Ojo con una lección del módulo 2D: la parábola tiene foco en R²/(4·profundidad), y el
subreflector va ahí. Verifica compilación y consola como dice CLAUDE.md.
```

### Sesión C — Recorrido en primera persona ✅ hecha (2026-09-29)

Resultado en §4.3. Se deja el prompt como registro.

```
Continuamos la Fase 4 del Módulo 3D. Lee CLAUDE.md, Handoff.md y MODULO-3D.md.
La estación ya está construida en la escena Modulo3D.

Quiero poder recorrerla en primera persona. Ten en cuenta MODULO-3D.md §6: ya existe
Assets/script/movement.cs del compañero, que usa el Input Manager antiguo. El
proyecto está en modo "Both", así que funciona, pero decide conmigo si lo
reutilizamos, lo portamos al Input System nuevo, o usamos otra cosa — y dime el
porqué antes de tocarlo, que es código de otra persona.

Necesito: colisiones en muros y props, que no se pueda atravesar la cerca, el botón
Atrás del UI funcionando durante el recorrido, y que no se pierda el cursor.

Las piezas del blockout ya llevan BoxCollider (menos los postes; la malla y el portón
sí). En las antenas tienen collider el pedestal, el tronco y los brazos del soporte;
el plato no, porque las VSAT tienen el borde inferior a 1,85 m y se pasa por debajo.
La vista provisional Modulo3DView.uxml tapa toda la pantalla con un panel de
"Escena en construcción": hay que cambiarla por un HUD mínimo con el botón Atrás.
La Main Camera sigue en (0, 1, -10), fuera de la parcela.

Considera la skill unity:initialize-ai-navigation si conviene un NavMesh (por ejemplo
para limitar por dónde se puede caminar). El paquete ya está instalado.
```

### Sesión D — Acabado visual ✅ hecha (2026-09-29)

Resultado en §4.4. Se deja el prompt como registro.

```
Continuamos la Fase 4 del Módulo 3D. Lee CLAUDE.md, Handoff.md y MODULO-3D.md.
La estación está construida y se puede recorrer.

Toca el acabado visual, que es lo que verá el jurado. Según el plano: edificio de
bloque pintado blanco con franja azul, ventanas horizontales, losa plana con equipos
de A/A encima. Terreno de sabana plana con grama y cerros al fondo.

El proyecto es URP 17.6. Usa las skills unity:urp-postprocessing para el volumen y
unity:migrate-birp-to-urp SOLO si aparecen materiales rosas (no debería, el proyecto
ya nació en URP).

Mantén la coherencia con el resto del aplicativo: la paleta azul está en
Assets/UI/Styles/Variables.uss y el criterio de diseño en la memoria del proyecto.
```

---

## 6. Avisos que ahorrarán tiempo

- **`movement.cs` usa el Input Manager antiguo** (`Input.GetAxis`) mientras el resto del proyecto
  usa el Input System nuevo. **No está roto**: `activeInputHandler` está en `2` (Both), así que
  ambos conviven. No "arreglarlo" sin hablarlo — es código del compañero.
- **Unity solo refresca al recuperar el foco**, pero `unity command recompile` fuerza la importación
  de scripts nuevos con el Editor sin foco. Para `Packages/manifest.json` sigue haciendo falta el
  foco (o `unity command package_resolve`).
- **Ver la escena 3D sí se puede** con el CLI `unity command` (Pipeline), y así se verificó la
  Sesión A:
  - `capture_scene_view` tras colocar la cámara con `SceneView.lastActiveSceneView.LookAt(...)`
    (vía `eval_file`). Una vista cenital ortográfica sin techo se compara directamente con el plano.
  - `capture_game_view --source camera` renderiza la cámara del juego, sin la UI.
  - `capture_game_view --source screen` (con UI) **no es fiable** con Unity sin foco: devolvió un
    frame viejo de otra escena. La UI sigue necesitando captura del usuario.
  - Las capturas se guardan bajo `Assets/` aunque se pida `Temp/`: copiarlas fuera y borrar la
    carpeta con `delete_asset` al acabar.
- **La escena regenerada pesa.** Con las antenas, `Modulo3D.unity` son ≈ 43 000 líneas de YAML
  (1,2 MB), y cada regeneración cambia los fileID, así que el diff es grande. Los 226 postes son
  la mayor parte; si molesta, subir `fence.postSpacing`. Regenerar solo cuando cambie algo. Las
  mallas de los platos NO van en la escena: son assets compartidos (`Paraboloide_fD*.asset`,
  ≈ 250 KB cada una), uno por cada f/D distinto.
- **Texturas en la Sesión D**: como cada pieza es un cubo de 1 m escalado, una textura normal se
  estira con la pieza. Usar un material con mapeo en espacio de mundo (triplanar) o escalar el
  tiling por material. Los `Blockout_*.mat` se pueden editar o sustituir: regenerar no los pisa.
- **Play se congela con Unity sin foco.** `PlayerSettings.runInBackground` está en falso, así
  que en cuanto el Editor pierde el foco el juego no avanza fotogramas (`Time.frameCount` se queda
  quieto) y el HUD no recalcula estilos. Para probar desde el CLI: `Application.runInBackground =
  true` por `eval` dentro de Play (solo dura esa sesión de Play; no toca los ajustes del proyecto).
- **El primer `eval` tras entrar en Play puede agotar sus 5 s** mientras Unity compila shaders
  (niebla, vidrio, detail map) y deja un error `Failed to handle /api/exec request` en la consola.
  Es de la herramienta, no del proyecto: esperar ~15 s tras `editor_play`, limpiar la consola con el
  juego ya corriendo y volver a leerla.
- **El teclado no se puede inyectar** desde el CLI: `InputSystem.QueueStateEvent` marca la tecla
  como pulsada, pero con `editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus`
  el juego no la recibe sin la ventana Game enfocada. Caminar de verdad lo prueba el usuario.
- **La vista provisional estaba mal cableada** hasta la Sesión A: `Modulo3D` era una copia del
  menú principal (cargaba `MenuPrincipal_Azul.uxml` y no tenía botón Atrás). Ya carga
  `Modulo3DView.uxml` con `BtnBack → Menu Inicial`, como decía el Handoff.
- **Escala**: el plano dice "1 m = 7 px", pero eso es del dibujo. En Unity, **1 unidad = 1 metro**.
- Las **posiciones** de §2 están medidas (±0,5 m); las **dimensiones rotuladas** son exactas. Si
  algo no encaja, manda la dimensión del plano. Para medir más cosas: Pillow está instalado y a
  9,8 px/m con origen en (97,5, 1483,5) px, `X = (px − 97,5)/9,8` y `Z = (1483,5 − py)/9,8`.
- El terreno es grande (200 × 140 m) con antenas de 30 m. Cuidar el **near/far clip** de la cámara
  y las sombras, o el rendimiento y el z-fighting darán problemas.

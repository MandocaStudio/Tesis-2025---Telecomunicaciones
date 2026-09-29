# Módulo 3D — Estación Terrena "Andrés Bello" (Camatagua, Aragua)

> Documento de arranque de la **Fase 4**. Escrito para que una sesión nueva pueda empezar en frío:
> trae el plano digitalizado, la decisión de enfoque con su porqué, el diseño de la herramienta y
> los prompts listos para pegar. Estado a 2026-09-29, tras las **Sesiones A** (blockout generado)
> **y B** (antenas).
>
> Contexto general del proyecto: [Handoff.md](Handoff.md) · Reglas del repo: [CLAUDE.md](CLAUDE.md)

---

## 1. Qué hay ya montado

| Pieza | Estado |
|---|---|
| Escena `Modulo3D.unity` | ✅ En Build Settings (idx 6). Cámara + EventSystem + vista provisional (`UI — Módulo 3D`, botón Atrás) + el blockout generado. |
| Botón "Módulo 3D" en el menú | ✅ `BtnModulo3D` → escena `Modulo3D` vía `MenuNavigation`. |
| Generador del blockout (Sesión A) | ✅ `StationLayout` + `StationGenerator` + ventana **PVI > Estación 3D > Constructor**. Terreno, cerca con portón, edificio con tabiquería y puertas, pedestales. Ver §4. |
| Antenas (Sesión B) | ✅ Las 7, generadas desde la misma antena tipo parametrizada por diámetro: pedestal, montura azimut-elevación con soporte en Y, plato paraboloide, subreflector en el foco. Ver §4.1. |
| Servicios, vía, estacionamiento, caseta | ❌ Pendiente. Las posiciones ya están medidas en §2. |
| Recorrido (C) y acabado visual (D) | ❌ Pendiente. Prompts en §5. |
| `Assets/script/movement.cs` | ⚠️ Movimiento en primera persona del compañero (Rigidbody + `Input.GetAxis`). Ver §6. |
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

**Resultado en `Modulo3D`:** 427 piezas — terreno, 5 paños de malla + portón + 226 postes,
8 pisos de local, 4 fachadas, 7 tabiques, losa de techo, y 7 antenas de 22 piezas cada una.

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
pedestales de antenas → antenas~~ (hecho) → servicios → vía, estacionamiento, caseta → props.

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

### Sesión C — Recorrido en primera persona

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

### Sesión D — Acabado visual

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
- **La vista provisional estaba mal cableada** hasta la Sesión A: `Modulo3D` era una copia del
  menú principal (cargaba `MenuPrincipal_Azul.uxml` y no tenía botón Atrás). Ya carga
  `Modulo3DView.uxml` con `BtnBack → Menu Inicial`, como decía el Handoff.
- **Escala**: el plano dice "1 m = 7 px", pero eso es del dibujo. En Unity, **1 unidad = 1 metro**.
- Las **posiciones** de §2 están medidas (±0,5 m); las **dimensiones rotuladas** son exactas. Si
  algo no encaja, manda la dimensión del plano. Para medir más cosas: Pillow está instalado y a
  9,8 px/m con origen en (97,5, 1483,5) px, `X = (px − 97,5)/9,8` y `Z = (1483,5 − py)/9,8`.
- El terreno es grande (200 × 140 m) con antenas de 30 m. Cuidar el **near/far clip** de la cámara
  y las sombras, o el rendimiento y el z-fighting darán problemas.

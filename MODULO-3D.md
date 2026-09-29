# Módulo 3D — Estación Terrena "Andrés Bello" (Camatagua, Aragua)

> Documento de arranque de la **Fase 4**. Escrito para que una sesión nueva pueda empezar en frío:
> trae el plano digitalizado, la decisión de enfoque con su porqué, el diseño de la herramienta y
> los prompts listos para pegar. Estado a 2026-09-29.
>
> Contexto general del proyecto: [Handoff.md](Handoff.md) · Reglas del repo: [CLAUDE.md](CLAUDE.md)

---

## 1. Qué hay ya montado

| Pieza | Estado |
|---|---|
| Escena `Modulo3D.unity` | ✅ Creada, en Build Settings (idx 6). Cámara + EventSystem + vista provisional con botón Atrás. |
| Botón "Módulo 3D" en el menú | ✅ `BtnModulo3D` → escena `Modulo3D` vía `MenuNavigation`. |
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

Las **dimensiones** de esta sección son las rotuladas en el plano (exactas). Las **posiciones** las
medí sobre el dibujo y son aproximadas (±1 m) — la escala se verificó cruzando el Ø 32 m de la
Antena 1 con su radio dibujado, y cuadra.

### 2.1 Terreno y cerramiento

| Elemento | Dimensión | Posición (centro) |
|---|---|---|
| Parcela | 200 × 140 m | — |
| Cerca perimetral | malla ciclón, h = 2,5 m | todo el perímetro |
| Vía interna de acceso | asfalto, ancho 8 m, cruza de este a oeste | Z ≈ 16,5 m |
| Portón | en la cerca sur | X ≈ 100 m, Z = 0 |
| Caseta de vigilancia | 6 × 5 m | X ≈ 109 m, Z ≈ 7 m |
| Estacionamiento | 8 puestos, ≈ 32 × 12,5 m | X ≈ 86 m, Z ≈ 29 m |

### 2.2 Edificio de Operaciones

**66 × 22 m · 1 nivel · h = 4,5 m · techo plano (losa).** Centro ≈ X 95 m, Z 50 m.
Fachada principal al sur.

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

Acabado: bloque pintado de blanco con franja azul, ventanas horizontales, losa plana con equipos
de A/A encima.

### 2.3 Antenas

Todas apuntan al **sur** (arco geoestacionario), elevación ≈ 60–70°.

| Antena | Ø plato | Pedestal | Altura | Posición (X, Z) |
|---|---|---|---|---|
| Camatagua 1 (1970) | 32 m | 9 × 9 m | ≈ 30 m | 50, 85 |
| Camatagua 2 (1980) | 30 m | 8,5 × 8,5 m | ≈ 28 m | 121, 85 |
| Antena 3 | 11 m | — | — | 168, 100 |
| Antena 4 | 7 m | — | — | 181, 61 |
| VSAT / respaldo ×3 | 3,6 m | — | — | 178 / 186 / 194, Z ≈ 42 |

**Anatomía para el modelado** (del propio plano): pedestal de concreto (caja) + soporte en Y +
plato paraboloide inclinado + subreflector en el foco (configuración **Cassegrain**).

### 2.4 Servicios

| Elemento | Dimensión | Posición (X, Z) |
|---|---|---|
| Planta eléctrica (generadores) | 14 × 9 m | 141, 55 |
| Tanque diésel | ≈ 6 × 9 m | 153, 55 |
| Transformador | 6 × 6 m | 162, 56 |
| Chillers A/A | ≈ 11 × 7 m | 139, 43 |
| Tanque de agua | Ø 8 m | 20, 33 |

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

## 4. Diseño de la herramienta (a construir en la próxima sesión)

```
Assets/Scripts/Station3D/
├── Data/
│   └── StationLayout.cs          ScriptableObject: parcela, edificio (lista de locales),
│                                 antenas (Ø, pedestal, altura, pos), servicios, vía
├── Editor/
│   ├── StationGenerator.cs       Construye la jerarquía a partir del StationLayout
│   └── StationBuilderWindow.cs   EditorWindow con el botón "Generar" y sus opciones
Assets/Data/AndresBelloLayout.asset   Instancia con los datos de §2
```

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

Terreno y cerca → losa del edificio → muros perimetrales → tabiquería interior → losa de techo →
pedestales de antenas → antenas → servicios → vía, estacionamiento, caseta → props.

---

## 5. Prompts para las próximas sesiones

Pegar tal cual. Cada uno es autosuficiente.

### Sesión A — Datos y generador del blockout

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

### Sesión B — Antenas

```
Continuamos la Fase 4 del Módulo 3D. Lee CLAUDE.md, Handoff.md y MODULO-3D.md.
El blockout del terreno y el edificio ya está generado por StationGenerator.

Toca modelar las antenas. Son la pieza que identifica visualmente la estación, así
que merecen cuidado. Según el plano (MODULO-3D.md §2.3) cada una es: pedestal de
concreto (caja) + soporte en Y + plato paraboloide + subreflector en el foco
(Cassegrain). Todas apuntan al sur con 60-70 grados de elevación.

Hay cuatro tamaños muy distintos (32, 30, 11 y 7 m) más tres VSAT de 3,6 m, así que
quiero UN prefab parametrizado por diámetro, no siete modelos sueltos.

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
- **Unity solo refresca al recuperar el foco.** ProBuilder no aparecerá hasta que se haga clic en
  la ventana de Unity. Igual para cualquier cambio en `Packages/manifest.json`.
- **No se puede capturar el Game View** desde las herramientas. En un módulo 3D esto pesa mucho
  más que en la UI: hay que pedirle capturas al usuario a cada paso. Para inspeccionar la escena
  sí sirve `Unity_Camera_Capture`, que devuelve la Scene View — y en 3D eso **sí** es útil,
  al contrario que con la UI overlay.
- **Escala**: el plano dice "1 m = 7 px", pero eso es del dibujo. En Unity, **1 unidad = 1 metro**.
- Las **posiciones** de §2 son aproximadas (±1 m); las **dimensiones rotuladas** son exactas. Si
  algo no encaja, manda la dimensión del plano.
- El terreno es grande (200 × 140 m) con antenas de 30 m. Cuidar el **near/far clip** de la cámara
  y las sombras, o el rendimiento y el z-fighting darán problemas.

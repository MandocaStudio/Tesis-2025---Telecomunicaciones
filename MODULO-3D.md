# Módulo 3D — Estación Terrena "Andrés Bello" (Camatagua, Aragua)

> Documento de arranque de la **Fase 4**. Escrito para que una sesión nueva pueda empezar en frío:
> trae el plano digitalizado, la decisión de enfoque con su porqué, el diseño de la herramienta y
> los prompts listos para pegar. Estado a 2026-09-29: **Fase 4 completa** — Sesiones A (blockout),
> B (antenas), servicios y vías, C (recorrido en primera persona) y D (acabado visual) — y la
> estación **rehecha con el plano v2** (trazado sobre imagen satelital, §2) y **corregida con la
> propia foto satelital** (§2.8).
>
> Contexto general del proyecto: [Handoff.md](Handoff.md) · Reglas del repo: [CLAUDE.md](CLAUDE.md)

---

## 1. Qué hay ya montado

| Pieza | Estado |
|---|---|
| Escena `Modulo3D.unity` | ✅ En Build Settings (idx 6). `Jugador` (con la cámara) + EventSystem + HUD (`UI — Módulo 3D`) + la estación generada. |
| Botón "Módulo 3D" en el menú | ✅ `BtnModulo3D` → escena `Modulo3D` vía `MenuNavigation`. |
| Generador (Sesión A) | ✅ `StationLayout` + `StationGenerator` + ventana **PVI > Estación 3D > Constructor**. Ver §4. |
| **Plano v2** | ✅ Toda la estación rehecha con `Planos/plano_andres_bello_v2.svg` (2026-09-29): cerca hexagonal, 9 edificios, antenas, vías curvas, losas y árboles. Ver §2. |
| **Correcciones con la foto** | ✅ Con la foto satelital de la que sale el plano (2026-09-29): Camatagua 1 con su plato (la "base circular" del plano era su sombra), Ø y alturas de las antenas medidos (alturas por sus sombras), platos donde de verdad están, árboles, tanques, cisterna, vehículos y techos planos. Ver §2.8. |
| **Árboles grandes** | ✅ 67 árboles de verdad (modelos `Tree9` del usuario, 13–23 m) sobre las manchas de follaje de la foto, en vez de 346 esferas pequeñas (2026-10-09). Ver §4.2. |
| Antenas (Sesión B) | ✅ Las 20, generadas desde la misma antena tipo parametrizada por diámetro. Ver §4.1. |
| Vías, losas, tanques y árboles | ✅ Ver §4.2. |
| Recorrido en primera persona (Sesión C) | ✅ `FirstPersonWalker` + HUD. El jugador empieza en el portón de la cerca oeste. Ver §4.3. |
| Acabado visual (Sesión D) | ✅ Techos de teja, zinc y losa, fachadas blancas con franja azul y ventanas, tierra ocre con manchas de grama, árboles, sabana y cerros, sol, niebla, sombras y postprocesado. Ver §4.4. |
| Atrezo interior (racks, consolas, mobiliario) | ❌ No empezado. Los edificios que se recorren están vacíos. |
| `Assets/script/movement.cs` | Del compañero. **No se usa en Modulo3D** (decisión de la Sesión C, ver §4.3); sigue intacto en `Practica Test`. |
| ProBuilder 6.1.2 | ✅ En el manifest. No ha hecho falta: todo sale del generador. |
| AI Navigation 2.0.14 · Cinemachine 6.6.0 | ✅ Ya estaban en el proyecto. |
| Easy Designer | ❌ El usuario lo tiene, pero **no está importado**. Ver §3. |

---

## 2. El plano, digitalizado (v2)

Plano vigente: [`Planos/plano_andres_bello_v2.svg`](Planos/plano_andres_bello_v2.svg), **trazado a partir
de imagen satelital**, norte arriba, escala aprox. 1 : 256 y "medidas estimadas para modelado 3D".
Sustituye al plano v1 ([`Planos/estacion-andres-bello-plano-conjunto.png`](Planos/estacion-andres-bello-plano-conjunto.png)),
que se deja como historia; el modelo hecho con el v1 está en el commit `532b167`.

**Medir sobre el SVG.** La barra de 50 m mide 200 px: **4 px = 1 m**. Origen en la esquina SO de la
caja que envuelve la cerca, X hacia el este, Z hacia el norte:
**X = (px − 110) / 4 · Z = (1096,25 − py) / 4**. Los `rotate(θ)` del SVG se copian tal cual como
giro en Y de Unity (los dos van en sentido horario visto desde arriba). Todas las coordenadas del
asset salieron de un script que lee el SVG, no a ojo.

### 2.1 Parcela, cerca y vías

| Elemento | Medida | Dónde |
|---|---|---|
| Cerca perimetral | malla ciclón, h ≈ 2,5 m; **hexágono** de 281 × 248 m | vértices (0, 210,9) (81,3, 245,3) (281,3, 248,4) (281,3, 6,3) (78,1, 0) (0, 48,4) |
| Portón | 6 m (supuesto) | cerca oeste, Z = 107,8: donde la cruza la vía de acceso |
| Vía de acceso | 3,5 m | empieza 12,5 m fuera de la cerca y llega al cruce del anillo (62,5, 125) |
| Anillo vial (tramos norte y sur) | 3 m | rodea el edificio principal |
| Ramal norte | 2,5 m | pasa por Camatagua 1 y la sala de equipos hasta la Antena 4 |
| Lazo oeste | 2,25 m | rodea oficinas y tanques |
| Ramales al tanque de agua y a la losa de platos | 2 m | |

El ancho de cada vía es el grosor de su trazo en el plano. Varias vías acaban **dentro** de un
edificio o pasan bajo el pedestal de Camatagua 1, tal cual el trazado: el asfalto queda debajo.

### 2.2 Edificios

Todos de una planta. Huellas y giros medidos; lo demás, supuestos (ver *Supuestos*).

| Edificio | Huella (m) | Centro (X, Z) | Giro | Techo | Puertas (supuestas) |
|---|---|---|---|---|---|
| Edificio principal | 18,75 × 46,9 (≈ 19 × 47) | 90,6, 125 | — | teja, 4 aguas | O (entrada, hacia el cruce), S, N (paso al ala norte). Tabique a 15,6 m del sur: **sala de control y equipos RF** (la franja que el plano dibuja aparte) |
| Ala norte (oficinas) | 46,9 × 14,1 (≈ 47 × 14) | 114,1, 155,5 | — | teja, 4 aguas | S (paso al principal), S (llega el anillo), E (paso al ala este) |
| Ala este | 25 × 17,2 (≈ 25 × 17) | 150, 157 | — | losa (foto; el plano dice teja) | O (paso), S |
| Oficinas / Administración | 34,4 × 16,25 (≈ 35 × 16) | 42,2, 158,4 | −8° | losa (foto; el plano dice teja) | S, N (llega el lazo oeste) |
| Galpón | 18,75 × 22,5 (≈ 19 × 22) | 159,4, 203,4 | +10° | zinc, 2 aguas | O: portón de 5 × 4,5 m |
| Sala de equipos | 13,1 × 8,1 | 122,2, 193,75 | — | losa | S y N (la vía pasa por ella) |
| Planta eléctrica | 18,75 × 18,75 | 28,1, 84,4 | — | losa (foto) | E: 3 × 3 m |
| Depósito / taller | 23,4 × 18,75 | 39,8, **63,6** | +8° | losa (foto) | E: 4 × 4 m |
| Caseta | 8,75 × 6,25 | 75,6, 210,9 | — | losa | S |

Los tres primeros forman el conjunto en L del plano: se tocan sin pisarse y tienen pasos entre
ellos, así que se recorren como un solo edificio.

### 2.3 Antenas

Lo que dibuja el plano (lo que se modeló está en §2.8 y §4.1):

| En el plano | Ø | Posición (X, Z) | Nota |
|---|---|---|---|
| "Antena 1 · Camatagua 1 (1970)" | "base circular Ø ≈ 31 m" | 84,4, 184,4 | **Es la sombra** del plato de al lado (§2.8). |
| "Antena 2" | 22 m | 100,6, 171,9 | Es el plato de Camatagua 1. |
| "Antena 3 · Camatagua 2 (1980)" | 30 m | 112,5, 114,1 | flecha al sur |
| "Antena 4" | 17 m | 121,9, 231,3 | flecha al sur |
| "Platos Ø 11–14 m sobre pedestales" | 11–13,75 m | ocho círculos al sur del edificio principal | La foto enseña cinco; varios círculos caen en sombras. |
| "Losa de concreto · 12 platos Ø 7 m (2 × 6)" | 7 m | dos columnas rectas | En la foto, dos columnas inclinadas, más al este, y un recinto de grava. |

Guía del plano: "antenas grandes: pedestal + plato inclinado ≈ 65° hacia el sur (flecha roja).
Antenas pequeñas: pedestal corto y plato Ø 7–14 m". Anatomía, como en el v1: pedestal de concreto
\+ soporte en Y + paraboloide + subreflector en el foco (Cassegrain).

### 2.4 Losas, tanques y árboles

| Elemento | Medida | Centro (X, Z) |
|---|---|---|
| Patio de vehículos | 21,9 × 29,7 m, girado −8°, concreto; **3 × 9 vehículos** (foto) | 189,1, **230,4** |
| Recinto de los platos pequeños | 56,25 × 53,1 m; **grava** (foto; el plano dice losa de concreto) | 157,8, 53,75 |
| Campo abierto (grama corta) | 90,6 × 112,5 m | 232,8, 89,1 |
| Tanques | **dos cilindros tumbados** de este a oeste, Ø 3,2 m, sobre losa de 12 × 9 m (foto) | **27,5, 194,4** (foto) |
| Tanque de agua | **cisterna abierta**, 9 × 13,5 m, agua oscura en un borde de concreto (foto) | **170,75, 140,15** (foto) |
| Árboles | **67 grandes** (copa 12–18 m) sobre las manchas de follaje de la foto (el plano dibuja 6) | 50 dentro de la cerca y 17 fuera |

### 2.5 Entorno

"Terreno de sabana seca (tierra ocre + manchas de grama)". Los cerros del fondo vienen del plano v1.

### 2.6 Dos ajustes al trazado

El plano está trazado a mano sobre la foto y dos cosas no cuadran; se movió lo mínimo y queda
anotado en el Tooltip del asset:

- **Patio de vehículos**, 1,6 m al sur: su esquina NE se salía 1,1 m de la cerca.
- **Depósito / taller**, 3,6 m al sur: su esquina NO se metía 3,1 m en la planta eléctrica.

### 2.7 Qué cambió respecto al plano v1

Casi todo: la parcela de 200 × 140 m pasa a un hexágono de 281 × 248 m; el edificio único de
66 × 22 m con losa plana y siete locales pasa a nueve edificios con techos de teja y zinc; las 7
antenas pasan a 20 (desaparecen las VSAT; ver §2.8 para las medidas de la foto); el
estacionamiento pasa a patio de vehículos; los ductos de guía de onda desaparecen (el v2 no los
dibuja); la grama pasa a tierra ocre con manchas de grama.

**Se conserva del v1** lo que el v2 no puede ver desde el satélite y no contradice: fachadas
blancas con **franja azul** y **ventanas horizontales**, la altura de Camatagua 2 (≈ 28 m) y los
cerros del fondo.

### 2.8 Correcciones con la foto satelital

El usuario pasó la foto de Google Maps de la estación, la misma sobre la que se trazó el plano v2
(no está en el repo). Se midió sobre ella con scripts de Python (Pillow), no a ojo:

- **Foto → modelo.** Una semejanza (escala, giro y traslación) ajustada con los centros de tres
  platos: **2,81 px/m, giro +1,1°**, residuos por debajo de 0,6 m. El plano y la foto encajan, así
  que lo que el plano trazó bien se deja; lo demás se corrige.
- **Platos:** manchas blancas redondas. Ø = ancho de la mancha + 4 px (el borde difuminado); da
  7 m en los platos pequeños, lo que dice el plano.
- **Alturas por la sombra.** Todas las sombras caen hacia el N 339° y la de cada plato se aleja
  de él en proporción a su altura. Con el sol a ≈ 45° (los platos de 7 m, de altura conocida, lo
  confirman) **el alejamiento es la altura del centro del plato**. Eje = centro − lo que el plato
  sube sobre su eje; pedestal = eje − montura (0,2·D).
- **Árboles:** píxeles verde oscuro (la grama del campo es mucho más clara; las sombras, casi
  negras) muestreados en una rejilla de 5 m, quitando los que caerían en edificios, vías, platos o
  sobre la cerca. El método exacto está en `PhotoTrees.cs`.

Lo que cambió:

| Qué | Plano v2 | Foto (lo modelado) |
|---|---|---|
| Antena 1 · Camatagua 1 | "base circular Ø 31 m" + una "Antena 2" de Ø 22 m al lado | **Una sola antena: Ø 28 m, 26,8 m de alto.** El círculo gris era la sombra de su plato. |
| Antena 4 | Ø 17 m | Ø 12 m, 11,5 m de alto |
| Platos medianos | ocho de Ø 11–14 m | **cinco**: Ø 12, 6, 10, 8,5 y 16,5 m, en su sitio |
| Platos pequeños | 2 columnas rectas de 6 | 2 columnas inclinadas de 5 (Ø 7 m) + 2 de Ø 3,6 m |
| Techos | teja en oficinas y ala este; zinc en planta y depósito | losa clara en los cuatro |
| Tanques | "Tanques" | dos cilindros tumbados |
| Tanque de agua | rectángulo azul | cisterna abierta |
| Losa de los platos | concreto | grava |
| Patio de vehículos | vacío | 3 × 9 vehículos |
| Vías | asfalto oscuro | gris claro gastado, como se ven |
| Árboles | 6 | 346 copas pequeñas; desde el 2026-10-09, 67 árboles grandes sobre esas manchas |

**La posición de una antena es ahora el centro de su plato visto desde arriba**, que es lo que
dibuja el plano y lo que se ve en la foto. El pedestal va detrás (hacia el norte): el plato
inclinado se adelanta (h + profundidad)·cos(el) hacia donde apunta, 3,2 m en Camatagua 1
(`AntennaGeometry.PedestalPosition`).

Sin tocar: la cerca (en la foto el lado este parece ir en diagonal, pero no se distingue bien), el
conjunto principal (la etiqueta de Google y los árboles lo tapan) y las vías.

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

## 4. La herramienta

```
Assets/Scripts/Station3D/
├── Data/
│   ├── StationLayout.cs          ScriptableObject: cerca (polígono + portón), vías y losas,
│   │                             edificios (huella girada, techo, puertas, tabiques), tanques,
│   │                             antena tipo (AntennaDesign) y antenas, árboles, entorno.
│   │                             Validate() devuelve un mensaje por cada cosa que no cuadra.
│   ├── PhotoTrees.cs             Los 67 árboles sacados de la foto (lista generada, §4.2)
│   ├── PlanGeometry.cs           C# puro: polígonos (dentro/fuera, triangulación) y huellas
│   │                             giradas (Footprint: esquinas, solapes). Lo usan el generador y
│   │                             Validate(), así que miden igual.
│   └── AntennaGeometry.cs        C# puro: foco, profundidad, alturas de la antena.
├── StationGeneratedRoot.cs       Marca del root generado (y qué layout lo generó)
├── Editor/
│   ├── StationGenerator.cs       Construye la jerarquía a partir del StationLayout
│   ├── StationBuilderWindow.cs   PVI > Estación 3D > Constructor: partes, edificios, alturas de
│   │                             las antenas, Ajustar pedestales, Generar
│   └── BlockoutAssets.cs         Mallas unidad, texturas de suelo, materiales Blockout_* (se
│                                 crean una vez) y mallas generadas por medidas
Assets/Data/AndresBelloLayout.asset        Instancia con los datos de §2 — la fuente de verdad
Assets/Data/Station3D/Blockout/            Mallas unidad (BloqueUnidad, CilindroUnidad, EsferaUnidad,
                                           BocinaUnidad, Paraboloide_fD*), texturas (Tierra, Sabana,
                                           Grama, GramaDetalle), Cerros y los materiales Blockout_*
Assets/Data/Station3D/Generado/            Mallas que dependen de las medidas: Parcela, Vias, Postes,
                                           Techo_<edificio>, Grama_<área>, Arboles_*. Se reescriben
                                           al regenerar
```

**Uso.** `PVI > Estación 3D > Constructor` abre la ventana: layout, qué partes levantar (terreno,
cerca, edificios, techos, antenas, vías/losas/tanques/árboles, entorno), la lista de edificios con
su huella y su techo, la altura calculada de cada antena frente a la del plano, los avisos de
`Validate()` y el botón Generar/Regenerar. `PVI > Estación 3D > Regenerar blockout` hace lo mismo
sin ventana. Para ver los interiores en la Scene View, desmarca "Techos" y regenera. Regenerar entra
en el Undo como un solo paso.

**Cómo está hecho.**

- **Cajas:** casi todo es la misma malla `BloqueUnidad` (cubo de 1 m con el pivote en el centro de
  la cara inferior), así que en el Inspector la escala de cada pieza es su medida real en metros.
- **Edificios:** cada uno se construye en su propio marco (origen en el centro de la huella) y el
  grupo se gira lo que dice el plano, así que girado o no, el código es el mismo:
  - Muros y tabiques: los muros van por dentro de la huella. Cada vano (puerta o ventana) parte el
    muro en tramos, con dintel encima, y las ventanas llevan además antepecho y vidrio. Los tabiques
    llevan su puerta.
  - Ventanas: se reparten solas, cada 4,5 m y centradas, y se saltan las que pisarían una puerta, un
    tabique o un tramo de muro pegado a otro edificio (darían a una pared).
  - Puertas: los edificios que no se recorren llevan una hoja en el vano.
- **Mallas generadas** (una por pieza, en `Generado/`, conservando el GUID al regenerar):
  - Techos inclinados: sólidos con canto en el alero, faldones y plafón (que desde dentro hace de
    cielo raso). A cuatro aguas llevan alero en todo el contorno; a dos aguas, hastiales a ras del
    muro, con el material del muro.
  - Terreno: el polígono de la cerca extruido.
  - Vías: todas en una malla, con discos en cada vértice que redondean quiebres y extremos como el
    trazo del plano.
  - Postes: los ~330 de la cerca en una sola malla.
  - Árboles: con modelos, un objeto por árbol (la misma malla, por instancias); sin modelos, tres
    mallas combinadas (troncos y copas esféricas en dos tonos).
- **Colliders:** todo lleva `BoxCollider`, salvo los cilindros (tanques), que llevan uno convexo
  para no tener esquinas invisibles; el terreno, que lleva su malla, y los troncos de los árboles,
  una cápsula. Las vías, la grama, las copas y los postes no llevan (no frenan a nadie).

**Resultado en `Modulo3D`:** 983 piezas (1170 objetos). La jerarquía: `Terreno`, `Cerca perimetral`
(6 paños de malla, portón y los postes), `Edificios` (9), `Antenas` (20), `Vías, losas, tanques y
árboles` y `Entorno`.

### 4.1 La antena tipo (Sesión B)

**Una sola antena parametrizada por su diámetro**, no 20 modelos: todas sus proporciones están
en `AntennaDesign` como fracción de D, así que la misma pieza da los 30 m de Camatagua 2 y los
3,6 m de los platos más pequeños. Por antena solo cambian posición, diámetro, pedestal, altura de
montura y apuntamiento. Anatomía, del suelo hacia arriba (tal cual la jerarquía):

```
Antena 3 · Camatagua 2 (1980)      ancla a ras de suelo en el pedestal (112,5, 0, 117,5)
├── Pedestal                       concreto, 8,5 × 8,5 m
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
  f = R²/(4·profundidad) del módulo 2D. Con f/D = 0,35 Camatagua 2 tiene f = 10,5 m.
- **La altura total manda.** `AntennaGeometry.OverallHeight` calcula el punto más alto (el borde
  superior del plato a 65°) y `Validate()` avisa si se aparta más de 0,5 m de la altura del plano.
  El botón **Ajustar pedestales** del Constructor recalcula el pedestal para que coincida: es lo
  que hay que pulsar si el tutor cambia la elevación o las proporciones.
- **Pedestal detrás del plato.** La posición del asset es el centro del plato visto desde arriba
  y el pedestal se coloca detrás (§2.8), así que plano, foto y modelo coinciden vistos desde arriba.
- `Validate()` avisa también si una elevación baja mete el plato en el suelo (el punto más bajo del
  paraboloide, calculado exacto: cae en r = 2f / tan(el)), si un pedestal se mete en un edificio o
  en otro pedestal, o si se sale de la cerca.
- El subreflector y la bocina van en acero y no en blanco: de frente, blanco sobre el plato
  blanco, el subreflector desaparecía.

| Antena | Ø | Centro del plato (sombra) | Pedestal | Montura | Eje | Alto total |
|---|---|---|---|---|---|---|
| Antena 1 · Camatagua 1 | 28 m | 20,9 m | 8 × 8 × 8,48 m | 5,6 m | 14,08 m | **26,81 m** |
| Antena 3 · Camatagua 2 | 30 m | (no se ve) | 8,5 × 8,5 × **8,36** m | 6,0 m | 14,36 m | **28,00 m** (plano v1 ✓) |
| Antena 4 | 12 m | 9,0 m | 3,4 × 3,4 × 3,68 m | 2,4 m | 6,08 m | 11,54 m |
| Plato 1 | 12 m | 7,7 m | 3,2 × 3,2 × 2,38 m | 2,4 m | 4,78 m | 10,24 m |
| Plato 2 | 6 m | (no se ve) | 1,6 × 1,6 × 1 m | 1,2 m | 2,20 m | 4,93 m |
| Plato 3 | 10 m | 6,3 m | 2,7 × 2,7 × 1,87 m | 2,0 m | 3,87 m | 8,42 m |
| Plato 4 | 8,5 m | 6,2 m | 2,3 × 2,3 × 2,43 m | 1,7 m | 4,13 m | 8,00 m |
| Plato 5 | 16,5 m | 14,0 m | 4,5 × 4,5 × 6,68 m | 3,3 m | 9,98 m | 17,48 m |
| Losa · platos 1–10 | 7 m | ≈ 4,2 m | 1,75 × 1,75 × 1,2 m | 1,4 m | 2,60 m | 5,78 m |
| Losa · platos 11–12 | 3,6 m | — | 1,2 × 1,2 × 0,5 m | 1,5 m | 2,00 m | 3,64 m |

**Apuntamiento.** Todas a azimut 180° y elevación 65°, como las flechas del plano. Dato para la
defensa: **desde Camatagua (≈ 9,8° N, 66,9° O) VENESAT-1 (78° O) se ve a El ≈ 72,6°, Az ≈ 229°**
(al suroeste, no al sur). Si el tutor quiere que las antenas apunten de verdad al satélite del
módulo 2D, son dos números por antena en el asset + "Ajustar pedestales".

### 4.2 Vías, losas, tanques y árboles

Mismo patrón: los datos en `StationLayout` (`site` para lo que va a ras de suelo, `facilities` para
los tanques, `trees` para los árboles) y un `Build…` por grupo en el generador.

- **Vías:** polilíneas con su ancho, 5 cm de asfalto sobre el terreno, en una sola malla.
- **Losas:** rectángulos girados. `Concreto` y `Grava` se pisan (el patio, 15 cm; el recinto de
  los platos, 5 cm). `Grama` es una capa de 2 cm con la grama corta del campo abierto. Una losa
  puede llevar encima una rejilla de vehículos (`items`, `itemSize`): el patio, 3 × 9.
- **Tanques:** cada uno tiene una forma (`Equipo`, `TanqueVertical`, `TanqueHorizontal`,
  `Cisterna`), una losa opcional, un margen y un número de unidades. Los horizontales se tumban a
  lo largo del lado mayor, uno al lado del otro: los "Tanques" son dos de Ø 3,2 m. La cisterna
  es abierta: cuatro muros de concreto y el agua 25 cm por debajo del borde.
- **Árboles:** modelos de árbol de verdad (`TreeDesign.models`: los cuatro prefabs de
  `Assets/Tree9`, de 20–23 m de alto con copas de 16–21 m), escalados para que la copa mida lo
  que pide cada árbol (12, 15 o 18 m). Son prefabs del **Tree Creator**, cuyos shaders no existen
  en URP (saldrían rosas): el generador toma la malla que llevan dentro (corteza + hojas) y le pone
  materiales URP Lit hechos con su mismo atlas (`Blockout_Arbol_Tree9_Corteza` / `_Hojas`; las
  hojas, recortadas por transparencia y visibles por las dos caras). La corteza del atlas es de
  eucalipto arcoíris: va con un tinte que la apaga. Del modelo se mide el ancho de copa y el
  grosor del tronco (collider de cápsula). Sin viento: los árboles no se mueven.
- **Dónde:** pocos árboles grandes sobre las manchas de follaje de la foto (`PhotoTrees.cs`
  explica el método): de la zona más densa a la menos, una copa de 18, 15 o 12 m donde al menos
  la mitad sea follaje sin árbol; sin meterse en techos ni platos, y con el tronco lejos de vías,
  edificios y cerca. Cada árbol guarda su modelo y su giro. Pueden quedar fuera de la cerca.
- `Validate()` avisa si algo se sale de la cerca, si dos edificios, tanques o losas se pisan, si
  un árbol nace dentro de un edificio, un tanque o una losa o sobre la cerca, si una puerta,
  ventana o tabique no cabe en su muro, o si la franja azul taparía un vano.

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
| Objeto `Jugador` en `Modulo3D` | Fuera del root generado, así que regenerar no lo toca. Empieza **6 m dentro del portón de la cerca oeste, sobre la vía de acceso y mirando a lo largo de ella**: (5,8, 0,1, 109,3), giro 76°. Cápsula de 1,8 × 0,35 m, escalón máximo 0,3 m; la Main Camera es hija suya a 1,65 m, con near clip 0,1 m. |

**Sin NavMesh.** Los colliders ya bastan para limitar por dónde se camina: la cerca y el portón
cierran la parcela, y el recorrido es libre, no por rutas.

**Verificado en Play con el plano v2 y las correcciones de la foto** (moviendo la cápsula por
código): el portón para a 5,8 m de los 6; la cerca, los muros sin puerta, el tabique, el pedestal
de Camatagua 1, un tronco y el galpón (puerta cerrada) paran justo en su cara; por la entrada
principal, los dos pasos entre alas y la puerta de la sala de control se pasa; el recinto de grava
se sube solo. Consola limpia y ≈ 125 fps.
Con el plano v1 el usuario lo probó a mano (2026-09-29): todo bien. Desde el CLI no se puede,
porque el Input System solo le da la entrada al juego con la ventana Game enfocada (ver §6).

### 4.4 Acabado visual (Sesión D)

**Generado por datos** (en `StationLayout` y `StationGenerator`, como todo lo demás):

- **Edificios:**
  - Techos: teja roja a cuatro aguas (20°) en el edificio principal y el ala norte; zinc a dos
    aguas (12°) en el galpón; losa clara en el resto, como se ven en la foto.
  - Muros: blancos con la **franja azul** bajo el alero (el azul de acento del aplicativo, `#1560D8`,
    el mismo `--color-purple` de `Variables.uss`) en los de teja; gris claro en los de servicio.
  - Ventanas horizontales de 2,4 × 1 m con vidrio translúcido.
- **Suelos en dos escalas:** cada suelo tiene una textura grande de manchas y el mismo *detail map*
  de grano fino cada 2,5 m (URP Lit, `_DETAIL_MULX2`):
  - Dentro de la cerca, **tierra ocre con manchas de grama** (`Tierra.png`, cada 55 m).
  - En el campo abierto, grama corta (`Grama.png`, cada 50 m).
  - Fuera, sabana seca (`Sabana.png`, cada 160 m: solo se ve de lejos, y con 50 m sus manchas
    formaban cuadrícula desde el aire).

  Las texturas se generan por código, sin costuras, y el tiling se recalcula en cada generación.
- **Árboles:** los modelos `Tree9` con materiales URP y tintes en el `Kit` (`BarkTint`, `LeafTint`).
- **Vías** en gris claro gastado y **cisterna** con el agua oscura, como en la foto.
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

**Verificado:** compila, `Validate()` limpio, Play sin errores ni warnings, y capturas cenital
(cuadra con el plano y, puesta al lado de la foto con el mismo encuadre, con la foto), aérea,
desde el jugador, del conjunto principal, de Camatagua 1, del galpón, de los platos y del interior
del edificio principal. **Pendiente de ojo humano:** recorrerlo con teclado y ratón.

### Supuestos (el plano no los da)

Cada campo del `StationLayout` lleva un Tooltip que dice si su número es *Plano* (rotulado),
*Medido* (sobre el dibujo) o *Supuesto*. Los supuestos:

| Qué | Valor | Nota |
|---|---|---|
| Portón | 6 m, en la cerca oeste donde la cruza la vía de acceso | El plano dibuja la vía, no el portón. |
| Alturas de muro (del suelo al alero) | 4 m los de teja · galpón 6,5 · planta y depósito 5 · sala de equipos 3,5 · caseta 3 | Una planta. |
| Techos | teja 20°, zinc 12°; alero 0,6 m, canto 0,18 m; losa de 0,25 m | Qué edificio lleva cuál, del plano y la foto (§2.8). |
| Fachadas | blancas con franja azul (0,45 m, 0,15 m bajo el alero) en los de teja; gris claro en el resto | Franja y blanco, del plano v1. |
| Ventanas | 2,4 × 1 m, antepecho 1,1 m, cada 4,5 m; ninguna en galpón y planta | "Ventanas horizontales", del plano v1. |
| Puertas | las de §2.2; fachada 2 × 2,4 m, interiores 1,2 × 2,1 m | El plano no dibuja puertas: van donde llegan las vías y entre alas. |
| Qué se recorre | los cuatro edificios de teja; el resto con las puertas cerradas | |
| Sol de la foto | ≈ 45° de altura | Para pasar sombras a alturas (§2.8). Lo confirman los platos de 7 m. |
| Lado del pedestal | ≈ 0,27–0,28·D | Sin él en el plano ni en la foto. |
| Altura de montura (pedestal → eje) | 0,2·D; 1,4 m en los platos de 7 m y 1,5 m en los de 3,6 m | El pedestal es lo que falta hasta la altura medida. |
| Plato 2 y platos de la losa | pedestal 1 m / 1,2 m / 0,5 m | Su sombra no se distingue. |
| Apuntamiento | Az 180°, El 65° en todas | Las pequeñas no llevan flecha: se asume lo mismo. |
| Antena tipo: f/D | 0,35 | Rango típico de una Cassegrain: 0,3–0,4. |
| Antena tipo: resto de proporciones | subreflector 0,1·D, eje→vértice 0,09·D, soporte 0,3·D de ancho… | Todas en `AntennaDesign`, con su Tooltip. |
| Tanques | Ø 3,2 m sobre losa de 0,3 m | Se ven dos, tumbados. |
| Tanque de agua | 1,2 m de alto, muros de 0,3 m, agua a 0,25 m del borde | Se ve abierto. |
| Vehículos del patio | 3 × 9 de 5 × 2,4 × 2,3 m | Se ven filas de cajas blancas: vehículos o contenedores. |
| Árboles | copa de 12, 15 o 18 m (el modelo, escalado: 13–23 m de alto); modelo y giro al azar | La foto da dónde hay follaje, no cuántos árboles hay debajo. |
| Losas / asfalto | patio 0,15 m, grava 5 cm, grama 2 cm; asfalto 5 cm | |
| Entorno | sabana de 3,4 km; cerros de 60–240 m a 0,65–1,6 km | Sin precisión métrica. |
| Muro exterior / tabique / losa de piso | 0,20 / 0,15 / 0,15 m | Bloque. |
| Postes de la cerca | cada ≤ 3 m, sección 8 cm | Portón con postes de 20 cm. |
| Terreno | 0,5 m de espesor, cara superior en Y = 0 | |

### Reglas de diseño que NO se deben saltar

1. **Todo lo generado cuelga de un único root** (p. ej. `--- GENERADO: Estación ---`). Regenerar
   = destruir ese root y reconstruirlo. **Nunca tocar nada fuera de él**, para que el detalle
   hecho a mano con ProBuilder sobreviva a una regeneración.
2. **El `StationLayout` es la única fuente de verdad.** Ninguna medida hardcodeada en el generador.
3. **Unidades reales**: 1 unidad de Unity = 1 metro. Nada de escalar a ojo.
4. **Validación**: que el generador avise (`Debug.LogWarning`) de todo lo que no cuadre con el
   plano (algo fuera de la cerca, dos cosas que se pisan, un vano que no cabe). Es el mismo truco
   de los canarios que ya usamos y funciona.
5. **Pivotes a ras de suelo** (Y=0) para que colocar sea trivial.

### Orden de construcción sugerido

~~Terreno y cerca → edificios → techos → pedestales de antenas → antenas → vías, losas, tanques y
árboles~~ (hecho) → props (mobiliario, racks, consolas).

Para cada parte nueva: sus medidas van al `StationLayout` (con su Tooltip Plano/Medido/Supuesto),
su comprobación a `Validate()`, y su construcción a un `Build…` más en `StationGenerator`.

---

## 5. Prompts para las próximas sesiones

Pegar tal cual. Cada uno es autosuficiente. Los cuatro están hechos y se refieren al plano v1; se
dejan como registro de cómo se construyó la herramienta.

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
- **La escena regenerada pesa.** Con el plano v2, `Modulo3D.unity` son ≈ 109 000 líneas de YAML
  (3,0 MB), y cada regeneración cambia los fileID, así que el diff es grande. La mayor parte son
  las 20 antenas (27 piezas cada una) y las ventanas (4 piezas cada una). Los postes no pesan (una
  sola malla) y los 67 árboles son objetos ligeros que comparten las 4 mallas de `Tree9`. Regenerar solo cuando cambie algo. Las mallas de los platos, techos,
  vías, terreno y postes NO van en la escena: son assets (`Blockout/Paraboloide_fD*.asset` y
  `Generado/*.asset`).
- **Texturas**: como cada pieza es un cubo de 1 m escalado, una textura normal se estira con la
  pieza. Por eso los suelos con textura (terreno, grama) son mallas generadas con UV en metros, y
  la repetición la pone el material. Los `Blockout_*.mat` se pueden editar o sustituir: regenerar
  no los pisa.
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
- **Escala**: en Unity, **1 unidad = 1 metro**. El SVG del plano v2 va a 4 px/m (§2).
- **Assets descargados y URP.** Mucho de lo que se baja (Asset Store, packs gratis) trae shaders
  del pipeline antiguo y en URP sale **rosa**. Los árboles `Tree9` son del Tree Creator, que URP no
  soporta: por eso el generador usa solo su malla y les pone materiales URP propios (§4.2). Antes
  de bajar algo, buscar que diga "URP" o que traiga materiales sencillos (textura + normal), que
  se pasan a URP Lit con *Edit > Rendering > Materials > Convert…* o a mano.
- **Para medir más cosas del plano**, leer el SVG con un script (coordenadas exactas de cada
  `rect`, `circle` y `polyline`) en vez de medir a ojo; la conversión está en §2. Para verlo como
  imagen: `msedge --headless --screenshot=plano.png --window-size=1320,1253 <ruta del svg>`.
- **El plano v2 es una estimación** trazada sobre la foto: hay vías que acaban dentro de edificios
  y dos piezas que se pisaban (§2.6). Si algo no encaja, corregir lo mínimo y anotarlo en el
  Tooltip del asset.
- El terreno es grande (281 × 248 m) con antenas de 30 m. Cuidar el **near/far clip** de la cámara
  y las sombras, o el rendimiento y el z-fighting darán problemas.

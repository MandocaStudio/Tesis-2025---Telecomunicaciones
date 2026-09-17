# Panel de Nota del Módulo 2D — funciones que definen su contenido

---

## 1. Qué es y dónde vive

| Elemento | Ubicación |
|----------|-----------|
| Recuadro en pantalla | `LblNote` — columna 2 del rack, debajo del gauge de `Pr` |
| Definición visual | `Assets/UI/Documents/Simulador2D.uxml` → `<ui:Label name="LblNote" class="sim-note" />` |
| Estilo | `Assets/UI/Styles/Common.uss` → clase `.sim-note` |
| Lógica del texto | `Assets/Scripts/Simulation/SimulatorController.cs` → `PickNote()` |
| Textos base | `Assets/Data/VenesatParameters.asset` (campos `note*`, editables en el Inspector) |
| Calificación y formato | `LinkBudgetModel.Rating()` y `SimulatorController.FormatBer()` / `Sup()` |

Se actualiza en cada cambio de slider/dropdown, dentro de `Recalc()`:

```csharp
if (lblNote != null)
    lblNote.text = PickNote(band, clima, xpiI, res);
```

---

## 2. `PickNote()` — la función que elige el texto

```csharp
private string PickNote(int band, int clima, int xpiI, LinkResults res)
{
    if (!res.phaseLock)
        return $"⚠ Pérdida de enganche de fase: C/N={res.cnEffDb:0.0} dB es insuficiente y el BER " +
               $"se disparó a {FormatBer(res.ber)}. La constelación colapsa y el throughput cae a 0.";

    if (band == 2 && clima > 0)   return satParams.noteRainKa;
    if (xpiI == 2)                return satParams.noteXpi;

    return $"{satParams.noteModulation}  (Estado actual: BER {FormatBer(res.ber)} — {res.berRating}).";
}
```

### Tabla de decisión (se evalúa **en orden**; gana la primera que se cumple)

| Prioridad | Condición | Texto mostrado | Origen |
|-----------|-----------|----------------|--------|
| 1 | `!phaseLock` (enlace perdido) | Aviso ⚠ con `C/N` y `BER` en vivo | **Hardcodeado** en el código |
| 2 | `band == 2` **y** `clima > 0` → Ka con lluvia | `noteRainKa` | ScriptableObject |
| 3 | `xpiI == 2` → aislamiento XPI **Bajo (falla)** | `noteXpi` | ScriptableObject |
| 4 | *(por defecto)* | `noteModulation` + `(Estado actual: BER … — …)` | ScriptableObject + valores en vivo |

**Índices de los desplegables:**

| Variable | 0 | 1 | 2 |
|----------|---|---|---|
| `band` | C | Ku | **Ka** |
| `clima` | Despejado | Lluvia Moderada | Lluvia Intensa |
| `xpiI` | Alto (buen aislamiento) | Medio | **Bajo (falla)** |

> El caso de la captura corresponde a la **prioridad 4** (enlace enganchado, banda no-Ka o sin lluvia,
> XPI no bajo): muestra `noteModulation` seguido del estado en vivo.

📖 **El *porqué* de cada nota (qué enseña y por qué en ese momento) está desarrollado en la §8.**
**El guion para demostrarlas todas en la defensa está en la §9.**

---

## 3. Textos base (editables sin tocar código)

Definidos en `SatelliteParameters.cs` como `[TextArea]`, se editan en el Inspector del asset
`Assets/Data/VenesatParameters.asset`:

```csharp
[TextArea] public string noteIntro       = "Ajusta los parámetros y observa cómo cambian C/N, BER y throughput en tiempo real.";
[TextArea] public string noteXpi         = "Con reutilización de polarización, un aislamiento bajo deja que la otra polarización se filtre como interferencia, degradando el BER.";
[TextArea] public string noteRainKa      = "En banda Ka la lluvia atenúa mucho más la señal; si C/N cae bajo el umbral, el receptor pierde el enganche de fase.";
[TextArea] public string noteModulation  = "Modulaciones de mayor orden (16/64-QAM) llevan más bits por símbolo (más throughput) pero exigen mejor C/N para el mismo BER.";
```

Esto cumple el criterio de aceptación *"todos los tooltips/notas son editables desde un archivo de
datos, no hardcodeados en el código de UI"*.

> ⚠ **Dos observaciones honestas:**
> 1. `noteIntro` está definido pero **no se usa** en `PickNote()` (queda disponible para una pantalla
>    inicial, o se puede eliminar).
> 2. El mensaje de **enlace perdido (prioridad 1) sí está hardcodeado**, porque interpola valores en
>    vivo. Si se quiere 100% data-driven, moverlo al SO como plantilla con marcadores (p. ej. `{cn}`,
>    `{ber}`) y sustituirlos con `string.Replace`.

---

## 4. `Rating()` — de dónde sale "Aceptable"

En `LinkBudgetModel.cs`, se calcula junto con el resto de resultados y viaja en `LinkResults.berRating`:

```csharp
private static string Rating(double ber, bool phaseLock)
{
    if (!phaseLock)  return "Enlace perdido";
    if (ber <= 1e-9) return "Excelente";
    if (ber <= 1e-6) return "Aceptable";
    if (ber <= 1e-3) return "Marginal";
    return "Degradado";
}
```

| BER | Calificación |
|-----|--------------|
| ≤ 10⁻⁹ | **Excelente** |
| 10⁻⁹ < BER ≤ 10⁻⁶ | **Aceptable** |
| 10⁻⁶ < BER ≤ 10⁻³ | **Marginal** |
| > 10⁻³ | **Degradado** |
| sin enganche | **Enlace perdido** |

Los umbrales 10⁻⁹ / 10⁻⁶ / 10⁻³ se eligieron para reflejar el ejemplo de la propia tesis
(*"pasando de 10⁻⁹ a 10⁻³, lo que haría inútil el enlace"*). **Son configurables**: si tu Capítulo II
fija otros umbrales de calidad, se cambian en esta única función.

---

## 5. `FormatBer()` y `Sup()` — el formato "6,7·10⁻⁸"

```csharp
private static readonly string[] Sups = { "⁰","¹","²","³","⁴","⁵","⁶","⁷","⁸","⁹" };

private static string FormatBer(double ber)
{
    if (ber < 1e-12) return "< 10⁻¹²";           // piso de visualización
    int e = (int)Math.Floor(Math.Log10(ber));    // exponente
    double m = ber / Math.Pow(10, e);            // mantisa en [1,10)
    return $"{m:0.0}·10{Sup(e)}";
}

private static string Sup(int n)                  // entero → superíndice Unicode
{
    string s = Math.Abs(n).ToString();
    string outp = n < 0 ? "⁻" : "";
    foreach (char c in s) outp += Sups[c - '0'];
    return outp;
}
```

- Se usa **notación científica normalizada**: mantisa entre 1 y 10, con un decimal.
- El exponente se convierte a **superíndices Unicode** (⁻⁸) para que se lea como en la tesis, en vez
  del `E-08` que daría el formato por defecto de .NET.
- Por debajo de 10⁻¹² se muestra `< 10⁻¹²` (la aproximación de `erfc` pierde sentido práctico ahí, y
  un BER tan bajo es "perfecto" a efectos didácticos).
- La **coma decimal** ("6,7" en vez de "6.7") la aplica la cultura regional del sistema; si se quiere
  forzar punto, usar `CultureInfo.InvariantCulture` en el formateo.

---

## 6. Ejemplo trabajado (el de la captura)

Con `BER = 6,7·10⁻⁸` y enlace enganchado:

**Formato:**
```
e = floor(log10(6,7·10⁻⁸)) = floor(−7,174) = −8
m = 6,7·10⁻⁸ / 10⁻⁸ = 6,7
Sup(−8) = "⁻⁸"
⇒ "6,7·10⁻⁸"
```

**Calificación:**
```
phaseLock = true      → no es "Enlace perdido"
6,7·10⁻⁸ ≤ 10⁻⁹ ?     → NO
6,7·10⁻⁸ ≤ 10⁻⁶ ?     → SÍ  ⇒ "Aceptable"
```

**Rama de PickNote:** enlace enganchado, no es (Ka + lluvia), XPI no es Bajo ⇒ **prioridad 4**.

**Resultado final:**
```
"Modulaciones de mayor orden (16/64-QAM) llevan más bits por símbolo (más throughput)
 pero exigen mejor C/N para el mismo BER.  (Estado actual: BER 6,7·10⁻⁸ — Aceptable)."
```
✔ Coincide exactamente con lo que muestra el recuadro.

---

## 7. Cómo modificarlo

| Quiero cambiar… | Dónde |
|-----------------|-------|
| El **texto** de una nota | Inspector de `VenesatParameters.asset` (campos `note*`) |
| **Cuándo** aparece cada nota | `SimulatorController.PickNote()` |
| Los **umbrales** de calidad (Excelente/Aceptable/…) | `LinkBudgetModel.Rating()` |
| El **formato** del número | `SimulatorController.FormatBer()` |
| El **aspecto** del recuadro | Clase `.sim-note` en `Common.uss` |
| Añadir una **nota nueva** | Campo `[TextArea]` en `SatelliteParameters` + rama en `PickNote()` |

---

## 8. Por qué cada nota dice lo que dice

El panel no es decorativo: en cada instante hay **un fenómeno dominante** que explica por qué las
métricas están como están. La nota selecciona ese fenómeno y lo verbaliza. Por eso el orden de
prioridad va **de lo más urgente y excluyente a lo más general**.

---

### Nota 1 — Pérdida de enganche de fase ⚠

**Cuándo aparece:** `BER ≥ 10⁻²` **o** `C/(N+I) ≤ 0 dB`.
`C/N ≤ 0 dB` significa literalmente que **el ruido iguala o supera a la portadora**.

**Qué pasa físicamente:** el demodulador necesita recuperar la referencia de fase de la portadora
(lazo PLL). Cuando el ruido domina, el lazo no puede engancharse: las decisiones de símbolo pasan a ser
prácticamente aleatorias, la BER tiende a 0,5 (una moneda al aire) y **no pasa ningún dato útil**.

**Por qué ESE mensaje:** porque **invalida todo lo demás**. Hablar de eficiencia de modulación o de
aislamiento cuando el enlace está caído sería engañoso — el usuario debe entender que ya no es un
problema de "calidad", es un problema de "no hay enlace". Por eso tiene prioridad 1 y anula al resto.

**Por qué interpola valores en vivo:** el mensaje muestra el `C/N` y el `BER` concretos porque el punto
pedagógico es *cuánto* falta, no solo que falló.

**Qué deberías ver a la vez:**
- Constelación **colapsada y roja** (los puntos llenan toda la pantalla, ya no hay clusters).
- Haces uplink/downlink **en rojo**.
- Gauge de Throughput en **0** (el `codeRate` no se aplica: sin enganche no hay datos).
- Gauge de C/N cerca del mínimo.

---

### Nota 2 — Lluvia en banda Ka

**Cuándo aparece:** `Banda = Ka` **y** `Clima ≠ Despejado`.

**Qué pasa físicamente:** la atenuación por lluvia **crece fuertemente con la frecuencia**. En banda C
(4 GHz) la longitud de onda es ~7,5 cm, muchísimo mayor que una gota (0,5–5 mm): la gota es
eléctricamente pequeña y apenas dispersa (régimen de Rayleigh). En **Ka (20–30 GHz)** la longitud de
onda baja a ~1–1,5 cm, comparable al tamaño de las gotas: entran dispersión de Mie y absorción, y la
atenuación se dispara. En el simulador esto se modela con `kaRainFactor` (×2 por defecto) multiplicando
los dB de lluvia.

**Por qué ESE mensaje:** sin él, el usuario ve el C/N desplomarse al cambiar de banda y **parece
arbitrario**. La nota explica la causa y transmite el *trade-off* central de la elección de banda:
**Ka da mucho ancho de banda, pero se paga en desvanecimiento por lluvia**. Es probablemente la lección
más importante del módulo respecto a la selección de banda.

**Por qué va por encima de la nota de XPI:** si coinciden Ka+lluvia y XPI bajo, la **lluvia en Ka es el
efecto dominante** (puede meter 20 dB extra de pérdida, frente a un techo de interferencia de ~12 dB).
Se explica primero la causa mayor.

**Qué deberías ver a la vez:**
- La **nube crece** sobre el tramo de bajada en la escena espacial.
- El valor de `La` muestra el extra: p. ej. `3.1 dB (+20.0 clima)` en Ka + lluvia intensa.
- C/N y Eb/N0 caen, la constelación se dispersa.

---

### Nota 3 — Aislamiento XPI bajo (interferencia de polarización cruzada)

**Cuándo aparece:** `Aislam. XPI = Bajo (falla)`.

**Qué pasa físicamente:** para reutilizar frecuencia se transmiten dos portadoras en **polarizaciones
ortogonales** (H/V o circular derecha/izquierda). Si el aislamiento se degrada (desalineación, lluvia
que despolariza, imperfección de la antena), parte de la energía de una polarización **se filtra en la
otra** y actúa como interferencia co-canal. Se modela con la suma de recíprocos
`C/(N+I) = 1/(1/(C/N) + 1/(C/I))`, con `C/I = XPI`.

**Por qué ESE mensaje — y la lección clave:** porque la degradación **no es ruido térmico**, y eso
cambia por completo cómo se arregla. Con ruido térmico, subir la potencia mejora el enlace. Con
interferencia de polarización cruzada, **subir la potencia NO sirve**: al aumentar `Pt` sube la
portadora *y* sube proporcionalmente la señal que se filtra, así que la relación `C/I` se mantiene
constante. Matemáticamente:

```
cuando  C/N → ∞   ⇒   C/(N+I) → C/I = XPI
```

Es decir, **el XPI impone un techo duro al C/N efectivo**. Con XPI = 12 dB, por más potencia que
inyectes el C/N efectivo se queda clavado en ~12 dB.

> Esto es **demostrable en vivo** y es la mejor demo del módulo: pon XPI en *Bajo*, sube `Pt` de 15 a
> 30 dBW y observa que el C/N sube de ~11,9 dB a solo ~12,0 dB. **No se mueve.**
> (Con XPI *Alto* = 30 dB el techo queda muy por encima del punto de operación, así que no limita.)
> La polarización **Circular** añade +3 dB de aislamiento en el modelo (menos sensible a la rotación
> de Faraday), así que el techo sube a ~15 dB.

**Qué deberías ver a la vez:**
- El gauge de C/N **se satura**: deja de responder al slider de potencia.
- La constelación se ensancha de forma uniforme (interferencia, no falta de señal).

---

### Nota 4 — Modulación (estado nominal) ← *la de tu captura*

**Cuándo aparece:** el enlace está enganchado y **no hay ningún fenómeno dominante** (ni Ka con lluvia,
ni XPI bajo). Es el estado "todo normal".

**Qué pasa físicamente:** `Rb = Rs·log2(M)`, así que subir el orden de modulación multiplica el caudal
(QPSK = 2 bits/símbolo → 64-QAM = 6 bits/símbolo, **el triple**). Pero al meter más puntos en la misma
constelación, **los puntos quedan más juntos**: hace falta menos ruido para confundir uno con otro. En la
fórmula de BER de M-QAM eso aparece en el factor `3k/(M−1)`: al crecer `M`, el argumento de la función
`Q` se hace más pequeño y **la BER empeora** para el mismo Eb/N0.

**Por qué ESE mensaje:** es el *trade-off* fundamental de todo sistema digital —
**throughput ↔ robustez**. Como es la situación por defecto (la que más tiempo verá el evaluador), se
usa como "lección de fondo".

**Por qué añade "(Estado actual: BER … — …)":** las otras tres notas describen una **causa concreta**
y se explican solas. Esta es **genérica**, así que se ancla a los números en vivo para que el usuario
conecte el enunciado abstracto con lo que está viendo en los gauges. Por eso es la única con sufijo.

**Qué deberías ver a la vez:** cambia de QPSK a 64-QAM y observa simultáneamente:
- `Rb` en la barra de estado se **triplica**.
- La constelación pasa de 4 clusters a **64**, mucho más juntos.
- La BER **empeora** y el rating puede bajar de *Excelente* a *Aceptable* o *Marginal*.

---

## 9. Guion de demostración (para la defensa)

Secuencia para provocar las 4 notas en orden y explicar el módulo en ~2 minutos.
*(Valores aproximados con los parámetros por defecto: Pt=15 dBW, Gt=42 dBi, Gr=40 dBi, d=38.000 km,
T=150 K, B=36 MHz, Rs=30 MBaud.)*

| # | Ajustes | Nota que aparece | Qué señalar |
|---|---------|------------------|-------------|
| 1 | Banda **C**, Despejado, XPI **Alto**, **QPSK** | **Nota 4** (modulación) | Estado sano: C/N ≈ 29 dB, BER excelente |
| 2 | Cambiar a **64-QAM** | Nota 4 (actualizada) | Rb se **triplica**, pero la BER empeora y la constelación se densifica |
| 3 | Volver a QPSK · XPI → **Bajo** | **Nota 3** (XPI) | C/N cae de ~29 a **~12 dB** (techo del XPI) |
| 4 | Con XPI Bajo, subir **Pt a 30 dBW** | Nota 3 | **El C/N NO se mueve** (~12 dB): enlace limitado por interferencia, no por ruido |
| 5 | XPI → Alto · Banda → **Ka** · Clima → **Lluvia Moderada** | **Nota 2** (lluvia Ka) | `La` muestra `+6.0 clima` (3 dB × factor Ka 2). La nube crece en la escena |
| 6 | Clima → **Lluvia Intensa** | **Nota 1** (enlace perdido) | +20 dB de lluvia ⇒ C/N ≈ **−5 dB**. Constelación colapsa en rojo, haces rojos, throughput 0 |

**Cierre sugerido:** volver al paso 1 para mostrar que el enlace se recupera, y recalcar que toda la
cadena se recalcula en tiempo real desde `LinkBudgetModel` (C# puro y verificable), no con valores
precocinados.

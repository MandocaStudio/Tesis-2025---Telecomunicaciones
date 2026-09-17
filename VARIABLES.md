# Variables del Módulo 2D — significado, rangos y traducción visual

---

# 1. Cada variable: qué es y a dónde va visualmente

## 1.1 Variables controlables por el usuario (sliders/dropdowns)

### Potencia Tx — `Pt` (slider `SliderPt`)
**Qué es:** potencia de salida del amplificador de la estación transmisora, en dBW.
**Rango:** −10 … 30 dBW · **Default:** 15 dBW.

**A dónde va:**
| Instrumento | Efecto |
|---|---|
| Fórmula (Link Budget) | Primer término de `Pr = Pt+Gt+Gr−Lp−La`; se ve sumado en vivo |
| Gauge Pr | Sube directamente la potencia recibida |
| Gauge C/N, BER, Throughput | Mejoran (más Pr ⇒ más C/N ⇒ menos BER) — **salvo que el XPI esté limitando** (§4) |
| Espectro | La campana de portadora crece |
| Constelación | Las nubes se compactan (menor dispersión) |
| Escena espacial | Los haces se ven más intensos/opacos (`_intensity` en `LinkSceneElement`) |
| Etiqueta `ValPt` | Muestra el valor numérico en vivo |

---

### Ganancia Tx — `Gt` (slider `SliderGt`)
**Qué es:** ganancia de la antena transmisora, en dBi (cuánto concentra la energía hacia el satélite
en vez de irradiar en todas direcciones).
**Rango:** 20 … 60 dBi · **Default:** 42 dBi.

**A dónde va:** exactamente igual que `Pt` (es el segundo término de la misma suma) — gauge Pr, C/N,
BER, throughput, espectro, constelación, haces, y `ValGt`.

> `Gr` (ganancia de recepción) **no tiene slider** — es fija en `SatelliteParameters.rxGainDbi = 40 dBi`,
> representando la antena del VSAT receptor. Se puede exponer como slider si se quiere más interactividad.

---

### Atenuación Atmosférica — `La` (slider `SliderLa`)
**Qué es:** pérdidas adicionales de trayectoria — gases atmosféricos (O₂, vapor de agua) más el aporte
manual del slider. **Se le SUMA** la atenuación por clima elegida en el dropdown (ver más abajo).
**Rango:** 0 … 20 dB · **Default:** 3,1 dB.

**A dónde va:** resta directa de `Pr` en la fórmula — mismo efecto en cascada que `Pt`/`Gt` pero en
sentido contrario (a más `La`, peor todo). La etiqueta `ValLa` es la única que muestra **dos números**:
```
"3.1 dB (+6.0 clima)"
```
el valor del slider y, aparte, el extra que metió el clima — para que quede claro que son dos fuentes
distintas de pérdida.

---

### Banda — `DropBanda` (C / Ku / Ka)
**Qué es:** selecciona la frecuencia de portadora (`SatelliteParameters.freqC/Ku/Ka_GHz` = 4 / 12 / 20 GHz).

**A dónde va:**
- **`Lp` (FSPL):** a mayor frecuencia, mayor pérdida de espacio libre (`Lp = 92,45+20log f+20log d`) →
  Ka pierde ~14 dB más que C solo por la frecuencia.
- **Multiplicador de lluvia:** si `banda == Ka`, la atenuación por clima se multiplica ×2
  (`kaRainFactor`) — ver Nota 2 en PANEL-NOTAS.md.
- **Etiqueta del propio dropdown:** muestra la frecuencia entre paréntesis, ej. `"Ku (12 GHz)"`.

---

### Clima — `DropClima` (Despejado / Lluvia Moderada / Lluvia Intensa)
**Qué es:** nivel de precipitación, que añade dB extra a `La` (3 dB moderada, 10 dB intensa; ×2 en Ka).

**A dónde va:**
- Todo lo que depende de `La` (ver arriba).
- **Escena espacial:** aparece/crece la **nube de lluvia** sobre el tramo de bajada
  (`LinkSceneElement.DrawRain`, tamaño `s=1.4` en intensa).
- Puede disparar la **Nota 1** (enlace perdido) o la **Nota 2** (lluvia en Ka).

---

### Polarización — `DropPol` (Lineal / Circular)
**Qué es:** tipo de polarización de la señal. La circular es menos sensible a la rotación de Faraday.

**A dónde va:** **+3 dB de margen de XPI** si es Circular (`if (pol==1) xpi += 3f`) — sube ligeramente
el "techo" que impone el aislamiento (ver §4). Es el único efecto de esta variable hoy.

---

### Aislamiento XPI — `DropXpi` (Alto / Medio / Bajo)
**Qué es:** relación C/I (portadora a interferencia) por fuga entre polarizaciones ortogonales.
**Valores:** Alto=30 dB, Medio=20 dB, Bajo=12 dB (+3 dB extra si la polarización es Circular).

**A dónde va:** entra en `C/(N+I) = 1/(1/(C/N) + 1/(C/I))` — con XPI bajo, **pone un techo duro** al
C/N que ni subiendo `Pt` se supera (ver §4, la explicación completa de por qué existe esta variable).
Efecto visible: gauge C/N se **satura** y deja de responder a Pt/Gt. Dispara la **Nota 3** si es Bajo.

---

### Modulación — `DropMod` (BPSK / QPSK / 16-QAM / 64-QAM)
**Qué es:** esquema de modulación digital, define `M` (puntos de constelación) y bits/símbolo.

**A dónde va:**
- **`Rb = Rs·log2(M)`** — barra de estado y gauge Throughput.
- **Constelación:** cambia el número de clusters (2/4/16/64) y su geometría (`IdealPoints`).
- **BER:** cambia la fórmula usada (`Ber()` en `LinkBudgetModel`) — M-QAM exige mejor Eb/N0 para la
  misma BER. Dispara la **Nota 4** (modulación) cuando no hay otro fenómeno dominante.

---

## 1.2 Variables fijas del satélite/enlace (`SatelliteParameters`, NO tienen slider)

| Variable | Valor | Dónde entra | Por qué es fija |
|----------|-------|--------------|------------------|
| `distanceKm` | 38.000 km | `Lp` (FSPL) | Slant range típico estación→GEO; fijarlo simplifica la demo a "un solo enlace representativo" (ver limitación §4 de FORMULAS.md) |
| `rxGainDbi` (Gr) | 40 dBi | `Pr` | Antena del VSAT receptor — se asume fija para no duplicar sliders con Gt |
| `sysNoiseTempK` (Tsys) | 150 K | Ruido `N=kTB` | Temperatura de ruido de sistema típica de un LNB/LNA de buena calidad |
| `bandwidthMHz` (B) | 36 MHz | Ruido, Eb/N0, gauge BER (rango del espectro) | Ancho de banda de transpondedor estándar (36 MHz es un tamaño de transpondedor real y común) |
| `defaultSymbolRateMBaud` (Rs) | 30 MBaud | `Rb`, barra de estado, techo del gauge Throughput | Consistente con B=36 MHz y roll-off típico (~1,2×Rs≈B) |
| `codeRate` | 0,8 | Throughput útil | FEC típico (ej. cercano a un código con overhead ~20%) |
| `latencyMs` | 250 ms | Barra de estado | Latencia de ida GEO real (~239-280 ms según elevación); 250 ms es el valor de referencia citado en la tesis |

---

# 2. Resumen de todas las notas (`LblNote`)

*(Desarrollo completo del "por qué" de cada una en [PANEL-NOTAS.md](PANEL-NOTAS.md) §8; aquí solo el resumen operativo.)*

| # | Se activa cuando | Fuente del texto | Resumen del mensaje |
|---|-------------------|-------------------|----------------------|
| **1** | `!phaseLock` (BER≥10⁻² o C/(N+I)≤0 dB) | Hardcodeado (interpola C/N y BER en vivo) | ⚠ Enlace perdido: constelación colapsa, throughput=0 |
| **2** | Banda = Ka **y** Clima ≠ Despejado | `SatelliteParameters.noteRainKa` | La lluvia ataca mucho más fuerte en Ka que en C/Ku |
| **3** | XPI = Bajo (falla) | `SatelliteParameters.noteXpi` | La fuga entre polarizaciones actúa como ruido adicional |
| **4** | Caso general (enlace OK, sin Ka+lluvia, sin XPI bajo) | `SatelliteParameters.noteModulation` + estado en vivo | Trade-off throughput ↔ robustez según la modulación |

Prioridad de evaluación: **1 > 2 > 3 > 4** (la primera condición verdadera gana; ver
`SimulatorController.PickNote()`). Los textos 2–4 son **100% editables** desde el Inspector del asset
`VenesatParameters.asset`, sin tocar código.

---

# 3. Por qué esos rangos de Potencia, Ganancia y Atenuación

No son números al azar: están elegidos para que el **espacio de exploración del slider corresponda a
clases reales de equipos de estación terrena**, de forma que mover el slider de punta a punta sea
equivalente a "cambiar de terminal pequeño a estación hub grande", no solo mover un número abstracto.

## 3.1 Potencia Tx: −10 … 30 dBW

Recordando que dBW es respecto a 1 W (`P_W = 10^(dBW/10)`):

| dBW | Vatios | Clase de equipo real |
|-----|--------|------------------------|
| −10 dBW | 0,1 W | Terminal móvil/portátil de baja potencia |
| 0 dBW | 1 W | VSAT pequeño |
| 15 dBW *(default)* | ≈ 32 W | VSAT bidireccional típico (SSPA de 20–40 W) |
| 20 dBW | 100 W | VSAT grande / hub pequeño |
| 30 dBW | 1.000 W (1 kW) | Estación **hub** con amplificador TWTA de alta potencia |

El rango cubre desde un terminal móvil pequeño hasta una estación **hub** de gran porte — el espectro
completo de equipos que realmente se usan en enlaces contra un satélite GEO como VENESAT-1.

## 3.2 Ganancia de antena: 20 … 60 dBi

**Base física** — ganancia de una antena parabólica:
```
G(dBi) = 10·log10(η) + 20·log10(π·D/λ)
```
con `η ≈ 0,55` (eficiencia típica) y `λ = c/f`.

| Banda | Diámetro D | Ganancia calculada | Clase de antena |
|-------|-----------|---------------------|-----------------|
| C (4 GHz, λ=7,5 cm) | 0,3 m | ≈ 19 dBi | Terminal móvil pequeño |
| Ku (12 GHz, λ=2,5 cm) | 1,2 m | ≈ **41 dBi** | VSAT bidireccional típico ← coincide con el default `Gt=42 dBi` |
| Ku (12 GHz) | 9 m | ≈ 58,5 dBi | Antena de estación **hub**/gateway |

El rango **20–60 dBi** reproduce justo ese espectro: desde un plato pequeño de terminal móvil hasta un
plato grande de estación hub, y el valor por defecto (42 dBi) coincide con el cálculo real de un plato
VSAT de ~1,2 m en Ku — no fue elegido a ojo.

## 3.3 Atenuación atmosférica adicional: 0 … 20 dB

- **Extremo bajo (0 dB, cielo despejado):** la absorción gaseosa (O₂ + vapor de agua) en condiciones
  normales suele ser del orden de fracciones de dB a pocos dB, según la tesis (*"Absorción Gaseosa"*,
  sección 1.9). El default de 3,1 dB representa un margen de diseño conservador con cielo casi
  despejado (gases + un pequeño margen de implementación/apuntamiento).
- **Extremo alto (20 dB):** representa un evento de **lluvia tropical intensa en banda Ka**, que es
  precisamente el escenario que la Nota 1 y la Nota 2 están diseñadas para demostrar. Venezuela tiene
  clima tropical con tormentas convectivas fuertes, así que este extremo **no es exagerado** para un
  satélite como VENESAT-1: en banda Ka, lluvias intensas pueden fácilmente superar 15–20 dB de
  atenuación específica.

El rango 0–20 dB permite recorrer **desde condiciones ideales hasta un apagón de servicio por lluvia**,
que es exactamente el fenómeno que el Marco Teórico pide poder demostrar.

---

# 4. Por qué está modelado el Aislamiento XPI

Tres razones, de lo más "porque lo pide la tesis" a lo más "porque enseña algo que nada más enseña":

### 4.1 Está en el propio Marco Teórico
Las secciones **1.7 (Polarización de las Emisiones Satelitales)** y **1.9 (Elementos Perjudiciales —
Interferencia de Polarización Cruzada, XPI)** de tu tesis lo nombran explícitamente como un fenómeno
real y como un elemento perjudicial del enlace. Si el simulador pretende cubrir "todo el contenido
teórico", el XPI **tenía que estar**.

### 4.2 Es una práctica real de diseño satelital, no un capricho académico
La **reutilización de frecuencia por polarización ortogonal** (transmitir dos portadoras en H/V o en
circular derecha/izquierda sobre la misma banda) es la forma estándar de **duplicar la capacidad** de
un transpondedor sin pedir más espectro. Es exactamente lo que la tesis describe en 1.7. El "costo" de
esa duplicación de capacidad es que, si el aislamiento entre las dos polarizaciones se degrada, una se
filtra en la otra — eso es el XPI, y modelarlo es modelar el **precio de esa ventaja de diseño**.

### 4.3 Enseña una lección que el ruido térmico NO puede enseñar (la razón pedagógica central)
Esta es la razón por la que vale la pena que esté **interactivo** y no solo mencionado:

Con ruido térmico puro, el remedio siempre es el mismo: **sube la potencia** (`Pt`) o la ganancia
(`Gt`) y el C/N mejora sin límite. Es una relación monótona simple.

Con XPI, el comportamiento es **cualitativamente distinto**. La fórmula de combinación es:
```
C/(N+I) = 1 / ( 1/(C/N) + 1/(C/I) )        con C/I = XPI
```
Si subes `Pt`, sube el `C/N` térmico… **pero también sube proporcionalmente la energía que se filtra**
de la otra polarización (porque es la misma portadora la que se filtra). El término `C/I` **no
mejora**, y en el límite:
```
cuando  C/N → ∞   ⇒   C/(N+I) → C/I = XPI
```
Es decir: **el XPI impone un techo duro** al C/N efectivo que ninguna cantidad de potencia extra puede
superar. Es la primera vez en el simulador que el estudiante ve una degradación que **no se arregla
"a fuerza"** — hace falta mejorar el aislamiento físico (mejor apuntamiento, mejor diseño de antena,
o cambiar a polarización Circular, que en el modelo suma +3 dB de margen), no la potencia.

Sin el XPI, el simulador solo enseñaría "más potencia = mejor enlace, siempre", que es una lección
incompleta y hasta engañosa sobre cómo funcionan realmente los enlaces satelitales con reutilización
de frecuencia.

**Cómo verlo en pantalla:** con XPI en *Bajo*, sube `Pt` de 15 a 30 dBW — el gauge de C/N prácticamente
no se mueve (se queda pegado cerca de los 12 dB del XPI bajo), mientras que con XPI en *Alto* el mismo
cambio de `Pt` sí mueve el gauge con normalidad.

---

# 5. Tabla-resumen: variable → instrumento (vista rápida)

| Variable | Fórmula | Gauge Pr | Gauge C/N | Gauge BER | Gauge TP | Espectro | Constelación | Escena | Nota |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| Pt | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ (haces) | — |
| Gt | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ (haces) | — |
| La (slider) | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ (haces) | — |
| Banda | ✔ (Lp) | ✔ | ✔ | ✔ | — | ✔ | ✔ | — | 2 |
| Clima | ✔ (La extra) | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ (nube) | 1, 2 |
| Polarización | ✔ (+3dB XPI) | — | ✔ | ✔ | — | — | leve | — | — |
| XPI | ✔ (techo C/N) | — | ✔ | ✔ | ✔ | ✔ | ✔ | — | 3 |
| Modulación | ✔ (Rb, BER) | — | — | ✔ | ✔ | — | ✔ (clusters) | — | 4 |

Todos alimentan el **mismo** `LinkBudgetModel.Compute()`, así que ningún instrumento puede mostrar algo
que contradiga a los demás: la escena, los gauges, los instrumentos y la nota siempre cuentan la misma
historia física en el mismo instante.

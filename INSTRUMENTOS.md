# Instrumentación virtual del Módulo 2D — cómo se calcula cada instrumento

---

# 1. Constelación QPSK / QAM

## 1.1 Qué representa

Es el **diagrama I/Q**: cada símbolo transmitido es un número complejo, y se grafica su componente
en fase (`I`, eje horizontal) contra la componente en cuadratura (`Q`, eje vertical). Un sistema
digital transmite eligiendo uno de `M` puntos posibles; el receptor mide dónde cayó el símbolo
recibido y decide **cuál de los M puntos ideales era el más cercano**.

- **Puntos ideales** = lo que se transmitió.
- **Nube alrededor de cada punto** = lo que llega, corrido por el ruido.
- Si las nubes se solapan, el receptor **se equivoca de punto** → eso *es* un error de bit.

Por eso la constelación es la representación visual más directa del BER.

## 1.2 Puntos ideales (según la modulación)

`ConstellationElement.IdealPoints(mod)` genera las posiciones normalizadas al rango `[-1, 1]`:

| Modulación | M | Geometría | Nº de clusters |
|------------|---|-----------|----------------|
| BPSK | 2 | 2 puntos sobre el eje I: `(−1,0)`, `(+1,0)` | 2 |
| QPSK | 4 | 4 esquinas: `(±1, ±1)` | 4 |
| 16-QAM | 16 | Rejilla cuadrada 4×4 | 16 |
| 64-QAM | 64 | Rejilla cuadrada 8×8 | 64 |

Para las QAM cuadradas se calcula con:

```csharp
int side = (mod == QAM16) ? 4 : 8;      // √M
float maxLevel = side - 1f;
x = (2*c - (side-1)) / maxLevel;        // c = columna
y = (2*r - (side-1)) / maxLevel;        // r = fila
```

Esto produce los **niveles PAM normalizados**:
- 16-QAM (side=4) → `{−1, −1/3, +1/3, +1}` (equivale a `{−3,−1,+1,+3}/3`)
- 64-QAM (side=8) → `{−7,−5,−3,−1,+1,+3,+5,+7}/7`

> ✅ Cumple el criterio de aceptación *"número de clusters = M"*: al cambiar de modulación se
> reconstruye la lista (`Rebuild`) y aparecen exactamente `M` nubes.

**El punto pedagógico:** como la rejilla siempre se normaliza al mismo cuadro, **a mayor M los puntos
quedan más juntos**. Con el mismo ruido, las nubes de 64-QAM se solapan mucho antes que las de QPSK.
Eso es exactamente el *trade-off* que enuncia la nota de modulación.

## 1.3 El ruido: por qué gaussiano y por qué en I y Q por separado

Cada cluster se dibuja con `perCluster` muestras (34 normalmente, 26 si el enlace está perdido).
A cada muestra se le suma **ruido gaussiano independiente en I y en Q**, generado por el
**método de Box-Muller**:

```csharp
float u1 = Mathf.Max(1e-4f, Random.value);   // uniforme (0,1]
float u2 = Random.value;                     // uniforme [0,1)
float mag = Mathf.Sqrt(-2f * Mathf.Log(u1));
float gx = mag * Mathf.Cos(2f * Mathf.PI * u2);   // ~ N(0,1)
float gy = mag * Mathf.Sin(2f * Mathf.PI * u2);   // ~ N(0,1), independiente de gx
x = bx + gx * sigmaPx;
y = by + gy * sigmaPx;
```

**Por qué gaussiano:** el ruido dominante en un enlace satelital es **AWGN** (*Additive White Gaussian
Noise*), el ruido térmico de Johnson-Nyquist que ya se calculó como `N = k·T·B` en el presupuesto.
Por el teorema del límite central, la suma de innumerables aportes térmicos independientes tiende a
una distribución normal.

**Por qué I y Q independientes:** el ruido complejo de banda base tiene componentes en fase y
cuadratura **estadísticamente independientes y de igual varianza**. Box-Muller devuelve justamente
dos normales independientes de una sola pasada, así que encaja perfecto.

**Por qué se re-sortea continuamente:** un `schedule.Execute(...).Every(120)` fuerza un repintado cada
120 ms, y como el ruido se genera dentro del dibujo, **cada frame es una realización distinta** del
proceso aleatorio. Eso da la sensación de "constelación viva" de un analizador real, en vez de una
imagen estática.

## 1.4 La dispersión (σ): cómo se calcula HOY

El controlador traduce el `C/(N+I)` calculado a una dispersión visual:

```csharp
// SimulatorController.Recalc()
float dispersion = Mathf.Clamp01((22f - (float)res.cnEffDb) / 22f) * 0.6f + 0.05f;
constellation?.SetState(mod, dispersion, !res.phaseLock);
```

y el widget la convierte a píxeles:

```csharp
// ConstellationElement.OnPaint()
float sigmaPx = (_lost ? 0.7f : _dispersion * 0.22f) * Mathf.Min(halfW, halfH);
```

Comportamiento resultante:

| C/(N+I) | dispersión | Aspecto |
|---------|-----------|---------|
| ≥ 22 dB | 0,05 | Nubes muy compactas, clusters nítidos |
| ≈ 11 dB | 0,35 | Nubes visiblemente anchas |
| 0 dB | 0,65 | Nubes muy solapadas |
| sin enganche | σ = 0,7 × semi-lado | **Colapso total**: los puntos llenan la pantalla, en rojo |

> ⚠️ **Declaración honesta (importante para la defensa):** este mapeo es una **relación didáctica
> monótona**, *no* la relación teórica exacta. Es lineal en dB, mientras que la desviación real del
> ruido decae de forma exponencial con la SNR en dB. Es decir: **la constelación es cualitativamente
> correcta** (más ruido ⇒ más dispersión ⇒ más solape ⇒ más BER) pero los píxeles **no** representan
> una σ numéricamente exacta.

## 1.5 Versión rigurosa (mejora propuesta)

Si se quiere que la dispersión sea **físicamente exacta**, la σ por dimensión, en las mismas unidades
normalizadas del gráfico, es:

```
σ = √( Es_media / (2 · (Es/N0)_lineal) )

con   Es/N0 = k · (Eb/N0)        y    k = log2(M)
```

donde `Es_media` es la energía media de símbolo de la constelación normalizada (para M-QAM cuadrada
con niveles escalados a máximo 1: `Es_media = 2·[(M−1)/3] / (√M−1)²`).

Ventaja: **haría emerger sola** la penalización de las modulaciones altas — con el mismo `Eb/N0`, la
σ crece respecto a la separación entre puntos al aumentar M, sin necesidad de ajustarlo a mano.
Todo el cambio se localiza en la línea de `dispersion` del controlador.

## 1.6 Detalles de dibujo

- **Ejes I/Q**: dos líneas cruzadas en gris azulado (`SimPalette.Grid`).
- **Radio del punto**: 1,6 px en 64-QAM y 2,1 px en el resto (para que 64 nubes no se empasten).
- **Color**: azul en operación normal, **rojo** al perder enganche.
- **Recorte**: las muestras que caen fuera del recuadro se descartan (`continue`), así el ruido
  extremo no pinta fuera del instrumento.
- **Coste**: se dibujan `M × perCluster` puntos por repintado (64-QAM ⇒ 64×34 ≈ **2.176 puntos** cada
  120 ms). Es asumible, pero es el instrumento más caro del módulo.

---

# 2. Analizador de espectro

```csharp
float noise = _noise * 0.28f * usable * Random.value;        // piso de ruido
float d = (x - centro) / sigma;                              // sigma = 5% del ancho
float peak = _carrier * usable * Mathf.Exp(-0.5f * d * d);   // portadora (campana)
float y = baseY - noise - peak;
```

Mapeo desde la física (`Recalc`):

```csharp
float cnNorm = Mathf.Clamp01((float)res.cnEffDb / 22f);
spectrum?.SetLevels(1f - cnNorm * 0.9f,          // piso de ruido ↑ cuando C/N ↓
                    Mathf.Clamp01(cnNorm + 0.1f), // altura de portadora ↑ con C/N
                    q);                           // color según calidad
```

**Interpretación física:**
- El **piso de ruido plano y aleatorio** representa el carácter *blanco* del AWGN: densidad espectral
  de potencia **constante en frecuencia** (por eso es plano) y aleatoria en el tiempo (por eso titila).
- La **campana gaussiana central** representa la potencia concentrada de la portadora modulada.
- Lo que el instrumento comunica visualmente es **la relación entre ambos**, que es literalmente el C/N.

> ⚠️ Es una **representación cualitativa**, no la FFT de una señal modulada real: no hay generación de
> símbolos ni transformada. Las magnitudes espectrales tampoco siguen la distribución Rayleigh que
> tendría un espectro de ruido real.

---

# 3. Gauges (medidores circulares)

`GaugeElement` dibuja un arco de **270°** (de 225° a −45°) con `Painter2D`, con una pista de fondo y
un arco de valor proporcional a `f = (valor − min)/(max − min)`.

| Gauge | Rango | Valor mostrado | Color |
|-------|-------|----------------|-------|
| **Pr** | −160 … −40 dBW | `Pr` directo | Azul |
| **C/N** | 0 … 30 dB | `C/(N+I)` | Azul / ámbar / rojo según calidad |
| **BER** | 0 … 12 | **`−log10(BER)`** (¡escala logarítmica!) | Verde / ámbar / rojo |
| **Throughput** | 0 … `Rs·6` Mbps | `throughput` en Mbps | Verde si engancha, rojo si no |

**Nota sobre el gauge de BER:** no marca el BER directamente (sería ilegible: todo se apelotonaría
cerca de cero), sino su **exponente negativo**. Una lectura de `8` significa `10⁻⁸`, y **más alto es
mejor**. El texto central sí muestra el valor formateado (`6,7·10⁻⁸`).

**Nota sobre el máximo de throughput:** `Rs · 6` porque 6 bits/símbolo es el máximo del módulo
(64-QAM). Así el gauge llega al tope justo con la modulación más eficiente.

---

# 4. Resumen: de la física a los píxeles

```
LinkBudgetModel.Compute()
        │
        ├── C/(N+I) ──► dispersión σ ─────► Constelación (nubes gaussianas Box-Muller)
        │           └─► piso de ruido ────► Analizador de espectro
        │           └─► valor + color ────► Gauge C/N
        │
        ├── BER ──────► −log10(BER) ──────► Gauge BER  +  color (verde/ámbar/rojo)
        ├── Pr ───────────────────────────► Gauge Pr
        ├── throughput ───────────────────► Gauge Throughput
        └── phaseLock ──► colapso rojo ───► Constelación + haces de la escena espacial
```

**Todos los instrumentos se alimentan del mismo cálculo**, así que nunca pueden contradecirse entre
sí: si la constelación se ensucia, el gauge de BER baja y la nota lo explica, todo en el mismo frame.

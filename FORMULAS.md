# Fundamento matemático del Módulo 2D (simulador de enlace satelital)

---

## 1. Nomenclatura

| Símbolo | Significado | Unidad |
|---------|-------------|--------|
| `Pt` | Potencia del transmisor | dBW |
| `Gt`, `Gr` | Ganancia de antena Tx / Rx | dBi |
| `Lp` | Pérdida de trayectoria en espacio libre (FSPL) | dB |
| `La` | Pérdidas adicionales (atmósfera, lluvia, gases) | dB |
| `Pr` | Potencia recibida | dBW |
| `k` | Constante de Boltzmann = 1,380649·10⁻²³ | J/K |
| `T` | Temperatura de ruido del sistema | K |
| `B` | Ancho de banda de ruido | Hz |
| `N` | Potencia de ruido térmico | dBW |
| `Rs` | Velocidad de símbolos | baudios |
| `Rb` | Velocidad de transmisión (bits) | bit/s |
| `M` | Puntos de la constelación (2, 4, 16, 64) | — |
| `XPI` | Aislamiento de polarización cruzada (C/I) | dB |

Todo el presupuesto se hace **en decibelios**: al pasar a dB, los productos y cocientes de la
ecuación de Friis se vuelven sumas y restas, que es justamente por lo que la ecuación (1.1) de la
tesis tiene esa forma aditiva.

---

## 2. Fórmula por fórmula

### 2.1 Presupuesto de enlace — ecuación (1.1) de la tesis

```
Pr = Pt + Gt + Gr − Lp − La        [dBW]
```

**Base:** es exactamente la ecuación **(1.1)** del Capítulo I del Marco Teórico (referencia [8] de la
tesis). Corresponde a la **ecuación de transmisión de Friis** expresada en dB: las ganancias suman y
las pérdidas restan. Es el punto de partida de todo el módulo.

*Código:* `LinkBudgetModel.Compute()`, paso 2.

---

### 2.2 Pérdida en el espacio libre (FSPL)

```
Lp = 92,45 + 20·log10(f_GHz) + 20·log10(d_km)        [dB]
```

**Base:** forma en dB de la atenuación de espacio libre de Friis, `FSPL = (4πd/λ)²`.

**Derivación de la constante 92,45** (por si te la preguntan en la defensa):

```
FSPL(dB) = 20·log10(4πd/λ) = 20·log10(4π·d·f/c)
         = 20·log10(d_m) + 20·log10(f_Hz) + 20·log10(4π/c)

20·log10(4π/3·10⁸) = −147,55

Cambio de unidades:  d_m → d_km  ⇒ +60 dB
                     f_Hz → f_GHz ⇒ +180 dB

⇒ FSPL = 20·log10(d_km) + 20·log10(f_GHz) + (180 + 60 − 147,55)
       = 20·log10(d_km) + 20·log10(f_GHz) + 92,45   ✔
```

Es decir, **92,45 no es un número arbitrario**: es la constante que resulta de expresar la frecuencia
en GHz y la distancia en km. (Si se usara MHz y km la constante sería 32,45.)

**Fuente:** ITU-R P.525; Roddy, *Satellite Communications*; Tomasi, *Sistemas de Comunicaciones Electrónicas*.

**Validación:** con f = 12 GHz y d = 38.000 km da `Lp ≈ 205 dB`; con banda C (4 GHz) da `≈ 196 dB`.
Esto **coincide con el valor `Lp = 196,8 dB`** que aparece en el mockup de la tesis, lo que confirma
que el orden de magnitud es correcto.

*Código:* `Compute()`, paso 1.

---

### 2.3 Ruido térmico

```
N = k · T · B        →  en dBW:
N = 10·log10(k) + 10·log10(T) + 10·log10(B)
N = −228,6 + 10·log10(T) + 10·log10(B)
```

**Base:** ruido térmico de **Johnson–Nyquist**. Es la misma expresión `N = K·T·B` que aparece en el
apartado *"Relación Señal a Ruido (S/N o C/N)"* del Marco Teórico (referencia [16]).

La constante −228,6 sale de `10·log10(1,380649·10⁻²³) = −228,6 dBW/(K·Hz)`.

*Código:* `Compute()`, paso 3.

---

### 2.4 Relación portadora/ruido

```
C/N = Pr − N        [dB]
```

Al estar ambos en dBW, el cociente de potencias es una resta. Es el **indicador primario de la
integridad del canal físico**, tal como lo define la tesis.

*Código:* `Compute()`, paso 4.

---

### 2.5 Interferencia por polarización cruzada (XPI)

```
C/(N+I) = 1 / ( 1/(C/N) + 1/(C/I) )        [en lineal, luego a dB]
```

**Base:** la interferencia se comporta como **ruido adicional**, así que se suman *potencias* de ruido
e interferencia. Como se trabaja con relaciones respecto a la portadora:

```
(N+I)/C = N/C + I/C     ⇒     C/(N+I) = 1 / (1/(C/N) + 1/(C/I))
```

Esta **suma de recíprocos** es la regla estándar en presupuestos satelitales para combinar
contribuciones de ruido/interferencia. Aquí `C/I = XPI`: cuando el aislamiento es bueno (XPI alto,
p. ej. 30 dB) su aporte es despreciable; cuando falla (XPI bajo, 12 dB) degrada notablemente el C/N.

Corresponde al apartado de la tesis: *"Interferencia de Polarización Cruzada (XPI): si el aislamiento
falla, la energía de una polarización se filtra en la otra, actuando como ruido adicional"*.

*Código:* `Compute()`, paso 5.

---

### 2.6 Energía por bit / densidad de ruido

```
Eb/N0 = C/(N+I) + 10·log10(B / Rb)        [dB]
```

**Derivación:**

```
N0 = N / B            (densidad espectral de ruido)
Eb = C / Rb           (energía por bit)

Eb/N0 = (C/Rb) / (N/B) = (C/N)·(B/Rb)
      → en dB:  Eb/N0 = C/N + 10·log10(B) − 10·log10(Rb)
```

Es el parámetro que **normaliza** la calidad del enlace respecto a la velocidad de datos, y es el que
exige la tesis para calcular el BER (*"se debe calcular en función de la modulación (Eb/N0)"*).

*Código:* `Compute()`, paso 6.

---

### 2.7 Velocidad de transmisión

```
Rb = Rs · log2(M)        [bit/s]
```

**Base:** definición directa — cada símbolo transporta `log2(M)` bits. Es la fórmula que la propia
tesis indica (QPSK: M=4 ⇒ 2 bits/símbolo; 16-QAM ⇒ 4; 64-QAM ⇒ 6).

*Código:* `Compute()`, paso 6; `ModulationInfo.BitsPerSymbol`.

---

### 2.8 Tasa de error de bit (BER)

**BPSK y QPSK (Gray):**
```
BER = Q( √(2·Eb/N0) )
```
QPSK da la **misma BER por bit** que BPSK porque equivale a dos canales BPSK ortogonales (I y Q),
cada uno con la mitad de la potencia y la mitad de la tasa.

**M-QAM cuadrada (Gray), aproximación estándar:**
```
BER ≈ (4/k)·(1 − 1/√M)·Q( √( 3k/(M−1) · Eb/N0 ) )        con k = log2(M)
```
Sale de la probabilidad de error de símbolo `Ps ≈ 4(1−1/√M)·Q(√(3·Es/N0/(M−1)))`, sustituyendo
`Es = k·Eb` y dividiendo entre `k` (válido con codificación Gray, donde un error de símbolo produce
típicamente **un** error de bit).

**Función Q y erfc:**
```
Q(x) = ½ · erfc( x/√2 )
```
Como .NET/Mono **no trae `erfc`**, se implementó la aproximación racional-exponencial de
*Numerical Recipes* (error ≈ 1,2·10⁻⁷), más que suficiente para representar BER de 10⁻¹² a 10⁻¹.

**Fuente:** Proakis, *Digital Communications*; Sklar, *Digital Communications*. Coincide con el
requisito de la tesis de mostrar cómo la interferencia degrada la BER (ej. de 10⁻⁹ a 10⁻³).

*Código:* `LinkBudgetModel.Ber()`, `Q()`, `Erfc()`.

---

### 2.9 Enganche de fase y throughput

```
enganche  = (BER < 10⁻²) y (C/(N+I) > 0 dB)
throughput = enganche ? Rb · codeRate : 0
```

Refleja el concepto de la tesis: *"si el ruido supera el umbral de diseño, el receptor pierde el
enganche de fase, provocando la pérdida total del flujo de datos"*. El `codeRate` (0,8 por defecto)
descuenta FEC y cabeceras, según la definición de *Throughput* de la tesis.

> ⚠ **El umbral numérico (10⁻², 0 dB) es una elección didáctica**, no una norma. Está aislado en el
> código para sustituirlo por el umbral real de tu Cap. II.

---

## 3. Cadena completa de cálculo

```
 sliders / selectores
        │
        ▼
   Lp (FSPL)  ──┐
                ├──►  Pr = Pt+Gt+Gr−Lp−La
   La (clima) ──┘            │
                             ▼
   N = kTB  ────────────►  C/N  ──► C/(N+I) [XPI]
                                        │
                                        ▼
                            Eb/N0 = C/(N+I) + 10log(B/Rb)
                                        │
                                        ▼
                            BER = f(Eb/N0, modulación)
                                        │
                         ┌──────────────┴──────────────┐
                         ▼                             ▼
                  enganche / throughput        dispersión de constelación,
                                               ruido del espectro, color de haces
```

Cada cambio de un control recalcula **toda** la cadena en un solo paso (`SimulatorController.Recalc()`).

---

## 4. Simplificaciones asumidas (declararlas en la defensa)

1. **Enlace único, no doble salto.** Un presupuesto satelital riguroso calcula el enlace de subida y
   el de bajada por separado y los combina con la misma suma de recíprocos:
   `1/(C/N)_total = 1/(C/N)_subida + 1/(C/N)_bajada + 1/(C/IM)`.
   Aquí se modela **un solo tramo equivalente**. La estructura del código permite añadirlo sin rehacer nada.
2. **Atenuación por lluvia/gases simplificada.** Se usa un valor por nivel de clima con un factor
   multiplicador para banda Ka, **no** los modelos ITU-R P.618 (lluvia), P.838 (atenuación específica)
   ni P.676 (absorción gaseosa por O₂ y vapor de agua), que dependen de forma no lineal de la
   frecuencia, el ángulo de elevación y la estadística de precipitación de la zona.
3. **Umbral de enganche heurístico** (ver §2.9).
4. **G/T no se muestra explícitamente.** La *figura de mérito del receptor* `G/T = Gr − 10·log10(T)`
   [dB/K] que menciona la tesis está implícita (Gr y T son parámetros), pero **no aparece como métrica
   en pantalla**. Es una adición trivial si el jurado la pide.
5. **FEC genérico:** `codeRate` fijo, sin un esquema concreto (Viterbi/Reed-Solomon/LDPC).

---

## 5. Nota sobre los valores del mockup

El mockup de la tesis muestra en el desglose `0,00 + 42,59 + 3,55 − 2,58 − 2,12 = −13,09`, que **no
cuadra aritméticamente** (esa suma da 41,44) y usa `Lp = 2,58 dB`, incompatible con el `Lp = 196,8 dB`
que el mismo mockup indica en la etiqueta flotante. Son **cifras de relleno del diseño gráfico**.

El simulador usa la **física real**: con `Pt=15 dBW`, `Gt=42 dBi`, `Gr=40 dBi`, `Lp≈196 dB` y `La≈3 dB`
resulta **`Pr ≈ −102 dBW`**. Potencias recibidas del orden de −100 dBW son lo **normal y esperable**
en enlaces satelitales (por eso se necesitan LNA y antenas de alta ganancia).

---

## 6. Cómo ajustarlo a tu Capítulo II

- **Fórmulas:** todas están aisladas en `LinkBudgetModel.Compute()` y `Ber()`. Sustituir ahí.
- **Parámetros:** `Assets/Data/VenesatParameters.asset` (Inspector) — distancia, Gr, T, B, Rs,
  frecuencias por banda, niveles de XPI, atenuaciones de lluvia, `codeRate`, latencia.
- **Verificación:** al ser C# puro sin Unity, `LinkBudgetModel` se puede validar con pruebas unitarias
  comparando contra los valores de ejemplo de la tesis.

---

## 7. Referencias base

- **Ecuación (1.1) y definiciones** — Marco Teórico, Cap. I de esta tesis (refs. [8] y [16]).
- **FSPL / Friis** — Recomendación ITU-R P.525; Roddy, *Satellite Communications*.
- **Ruido térmico** — Johnson–Nyquist; `N = kTB`.
- **BER y función Q** — Proakis, *Digital Communications*; Sklar, *Digital Communications*.
- **erfc** — Press et al., *Numerical Recipes* (aproximación de `erfcc`).
- **Modelos no implementados (mencionados como mejora)** — ITU-R P.618 / P.838 / P.676.

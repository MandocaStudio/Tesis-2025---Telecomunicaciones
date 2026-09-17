# Libreto de presentación — Módulo de Simulación 2D

Guion hablado para demostrar el módulo ante el jurado. Duración estimada: **8–10 minutos**.
Las líneas en *cursiva* son lo que se dice; el texto normal es la acción a realizar en pantalla.

---

## Antes de empezar (montaje)

- [ ] Aplicación abierta en el **menú principal**, pantalla completa.
- [ ] Verificar que el módulo abre y responde (mover un control y ver que todo reacciona).
- [ ] Dejar el simulador en su **estado inicial**: Banda C, Clima Despejado, Polarización Lineal,
      Aislamiento XPI Alto, Modulación QPSK, y los tres deslizadores en su posición por defecto.
- [ ] Si ya se estuvo probando, volver a esos valores antes de presentar.

---

## 1. Apertura (≈40 segundos)

> *"Lo que van a ver es un simulador interactivo de un enlace de comunicaciones satelitales,
> construido sobre los parámetros del satélite venezolano VENESAT-1.*
>
> *La idea es sencilla: en lugar de explicar con fórmulas en una pizarra qué pasa cuando llueve,
> cuando se cambia de banda de frecuencia o cuando aparece interferencia, aquí se puede **mover un
> control y verlo ocurrir**. Todo lo que se muestra en pantalla se recalcula en tiempo real a partir
> del presupuesto de enlace; no hay valores pregrabados ni animaciones falsas."*

**Acción:** Desde el menú principal, pulsar el botón **"Módulo 2D"**.

---

## 2. Recorrido de la pantalla (≈2 minutos)

> *"La pantalla se divide en dos mitades: arriba, la representación del enlace; abajo, la consola de
> control e instrumentación."*

### 2.1 El visor superior — el enlace

**Acción:** Señalar cada elemento mientras se nombra.

> *"Arriba tenemos la representación del enlace satelital completo:*
>
> - *A la izquierda, la **estación terrena** ubicada en Bárbula, que es la que transmite.*
> - *En el centro y en órbita, el **satélite geoestacionario** con su transpondedor.*
> - *A la derecha, el **terminal VSAT** que recibe la señal.*
>
> *Los dos haces que ven ondulando son las dos etapas del enlace: el **enlace de subida**, de la
> estación al satélite, y el **enlace de bajada**, del satélite al receptor. No son un adorno: su
> intensidad y su color responden a la calidad real del enlace en cada momento. Cuando el enlace se
> degrada, cambian de color; si el enlace se cae, se ponen en rojo.*
>
> *Y arriba a la derecha hay una **etiqueta flotante** con las tres cifras que resumen el estado del
> enlace: la relación energía por bit a ruido, la tasa de error de bit, y las pérdidas de trayectoria."*

### 2.2 La consola de control — tres columnas

> *"Abajo está la consola, dividida en tres bloques."*

**Columna 1 — Configuración del enlace**

> *"El primer bloque es donde el usuario interviene. Son ocho controles, y vale la pena explicar qué
> representa cada uno, porque cada uno corresponde a una decisión real de ingeniería."*

**Acción:** Ir señalando cada control mientras se explica. *(Si el tiempo aprieta, se pueden resumir
los tres deslizadores y detallar solo los selectores de banda, clima, aislamiento y modulación.)*

---

**Los tres deslizadores — lo que controla el operador de la estación**

> ***Potencia de transmisión.*** *Es la potencia que entrega el amplificador de la estación terrena,
> lo que se le inyecta a la antena. Es la variable más intuitiva: subirla mejora la señal que llega al
> satélite. El rango cubre desde un terminal portátil muy pequeño hasta una estación central de alta
> potencia, del orden de un kilovatio.*
>
> *Es también el control que usaremos más adelante para demostrar algo contraintuitivo: que **hay
> situaciones donde subir la potencia no sirve de nada**.*

> ***Ganancia de la antena transmisora.*** *Representa qué tan grande y directiva es el plato. Una
> antena no crea energía: la **concentra**. En lugar de radiar en todas direcciones, la enfoca hacia el
> satélite, y eso equivale a multiplicar la potencia efectiva.*
>
> *El rango va desde un plato pequeño de terminal móvil hasta una antena de estación central de varios
> metros de diámetro. El valor por defecto corresponde a un plato de aproximadamente metro veinte
> operando en banda Ku, que es el tamaño típico de un terminal profesional.*
>
> *En la práctica, subir ganancia suele ser más barato que subir potencia: se cambia el plato por uno
> más grande, no el amplificador.*

> ***Atenuación atmosférica.*** *Son las pérdidas que la señal sufre al atravesar la atmósfera —
> absorción por oxígeno y vapor de agua, más un margen de diseño. A diferencia de los dos anteriores,
> aquí **subir es empeorar**: es una pérdida, no una ganancia.*
>
> *Este deslizador permite meter atenuación a mano, para explorar el efecto. Pero además, el selector
> de clima le va a **sumar** su propio aporte automáticamente, y eso se ve reflejado en la etiqueta
> del valor: muestra por separado lo que puso el usuario y lo que agregó el clima."*

---

**Los cinco selectores — las decisiones de diseño del enlace**

> ***Banda de frecuencia: C, Ku o Ka.*** *Es la decisión de diseño más importante de todas, porque
> afecta al enlace por dos caminos distintos al mismo tiempo:*
>
> *Por un lado, **a mayor frecuencia, mayores pérdidas de trayectoria**. Ir de banda C a banda Ka ya
> cuesta unos catorce decibelios solo por la frecuencia, sin que haya pasado nada más.*
>
> *Por otro lado, **a mayor frecuencia, mucha más vulnerabilidad a la lluvia**, y eso lo veremos en
> vivo.*
>
> *¿Y entonces por qué alguien usaría banda Ka? Porque ofrece mucho más ancho de banda disponible: es
> la banda del internet satelital de alta velocidad. Es un compromiso: más capacidad, pero un enlace
> más frágil.*

> ***Clima: despejado, lluvia moderada o lluvia intensa.*** *Añade atenuación al enlace, y lo hace de
> forma **distinta según la banda**: en banda Ka la penalización es mucho mayor que en C o Ku, que es
> justamente el comportamiento real.*
>
> *Es el control que produce el efecto visual más claro: al activarlo, aparece y crece una **nube de
> lluvia** sobre el tramo de bajada en la representación superior.*

> ***Polarización: lineal o circular.*** *La polarización es la orientación del campo eléctrico de la
> onda. La lineal es horizontal o vertical; la circular gira mientras avanza.*
>
> *La circular tiene una ventaja concreta: es **menos sensible a la rotación de Faraday**, un efecto de
> la ionosfera que gira el plano de polarización de la onda. Por eso en el simulador, elegir circular
> da un poco más de margen de aislamiento.*

> ***Nivel de aislamiento entre polarizaciones.*** *Este control necesita una explicación previa,
> porque no es obvio.*
>
> *Los satélites transmiten dos señales distintas en la misma frecuencia, usando dos polarizaciones
> ortogonales — por ejemplo horizontal y vertical. Así **duplican la capacidad sin pedir más
> espectro**. Es una técnica estándar.*
>
> *El aislamiento es qué tan bien separadas se mantienen esas dos señales. Si el aislamiento es bueno,
> no se molestan entre sí. Si se degrada —por mal apuntamiento, por una antena imperfecta, o porque la
> lluvia despolariza la onda— **una señal se filtra en la otra y actúa como interferencia**.*
>
> *El selector permite elegir aislamiento alto, medio o bajo, y con eso simular esa falla. Es el
> escenario más interesante del módulo y lo demostraremos en detalle.*

> ***Esquema de modulación: BPSK, QPSK, 16-QAM o 64-QAM.*** *La modulación define **cuántos bits se
> transportan en cada símbolo enviado**.*
>
> *BPSK lleva un bit por símbolo, QPSK lleva dos, 16-QAM cuatro, y 64-QAM seis. Es decir, con la misma
> velocidad de símbolos, 64-QAM transmite seis veces más datos que BPSK.*
>
> *Suena a que conviene siempre usar la más alta. Pero tiene un costo, y también lo veremos en vivo."*

---

> *"Un detalle: la **ganancia de la antena receptora**, la temperatura de ruido del sistema y el ancho
> de banda del transpondedor son parámetros fijos del enlace, configurados según las características
> del satélite. No están como deslizadores para no saturar la interfaz, pero son ajustables desde el
> editor."*

**Columna 2 — Presupuesto de enlace**

> *"El segundo bloque es el corazón del cálculo. Muestra la ecuación del presupuesto de enlace, que es
> la fórmula fundamental de la ingeniería satelital: la potencia recibida es igual a la potencia
> transmitida, más las ganancias de las antenas, menos las pérdidas.*
>
> *Lo importante es que **no muestra solo la fórmula, sino el desglose numérico en vivo**: cada
> término con su valor actual. Si el jurado quiere verificar la cuenta a mano, puede hacerlo — los
> números que están en pantalla suman exactamente el resultado que se muestra.*
>
> *Debajo, un **medidor circular** con la potencia recibida, y un **panel de notas** que explica en
> lenguaje llano qué fenómeno está dominando el enlace en ese instante."*

**Columna 3 — Instrumentación virtual**

> *"El tercer bloque simula los instrumentos que un ingeniero tendría frente a sí en una estación real."*

> ***Analizador de espectro.*** *Muestra cómo se distribuye la energía en la frecuencia. Se ven dos
> cosas: el **piso de ruido**, esa línea irregular de abajo que titila constantemente, y el **pico de
> la portadora**, la campana del centro, que es nuestra señal.*
>
> *Lo que un ingeniero lee de este instrumento es **la distancia entre esos dos niveles**: cuánto
> sobresale la señal por encima del ruido. Cuando el enlace se degrada, el piso de ruido sube y el pico
> se hunde; cuando la señal queda sepultada en el ruido, ya no hay comunicación posible.*

> ***Diagrama de constelación.*** *Es el instrumento más revelador y le dedico un momento aparte
> enseguida.*

> ***Los tres medidores de calidad.*** *Resumen el estado del enlace en tres cifras:*
>
> - *La **relación señal a ruido**, que es el indicador primario de la salud del canal.*
> - *La **tasa de error de bit**, que es la métrica final: qué proporción de los bits llega mal.
>   Se muestra en notación científica y con una calificación en palabras — excelente, aceptable,
>   marginal o degradado — para que se entienda sin interpretar el número.*
> - *El **caudal efectivo** en megabits por segundo: los datos útiles que realmente pasan, ya
>   descontando la corrección de errores.*
>
> *Los medidores además **cambian de color**: azul o verde cuando el enlace está sano, ámbar cuando
> está marginal, y rojo cuando se cayó. Es una lectura de un vistazo.*

> ***Barra de estado.*** *Abajo del todo, en una sola línea: la modulación activa, la velocidad de
> símbolos, la velocidad de transmisión resultante, la latencia y el jitter.*
>
> *Menciono la **latencia**: unos doscientos cincuenta milisegundos. Ese valor no cambia porque es
> puramente geométrico — es lo que tarda la luz en subir treinta y ocho mil kilómetros y volver a
> bajar. Ninguna mejora de equipo la reduce, y es la razón por la que las comunicaciones satelitales
> geoestacionarias no sirven bien para aplicaciones muy sensibles al retardo."*

### 2.3 Un momento para la constelación

> *"Me detengo un segundo en la constelación, porque es el instrumento más revelador.*
>
> *Cada nube de puntos representa uno de los símbolos que el sistema puede transmitir. El centro de la
> nube es lo que se envió; la dispersión alrededor es el ruido que la señal acumuló en el camino.*
>
> *El receptor decide qué se transmitió viendo a qué nube pertenece cada punto. Entonces, **mientras
> las nubes estén separadas y compactas, no hay errores**. Cuando el ruido las hace crecer hasta que
> empiezan a solaparse, el receptor confunde un símbolo con otro — y eso es exactamente lo que
> significa un error de bit. La constelación permite **ver** la tasa de error, no solo leerla."*

---

## 3. Demostración en vivo (≈5 minutos)

> *"Ahora vamos a llevar el enlace por cuatro situaciones distintas."*

### Momento 1 — El enlace sano (punto de partida)

**Acción:** No tocar nada. Señalar los medidores.

> *"Este es un enlace en buenas condiciones: banda C, cielo despejado, buen aislamiento, modulación
> QPSK. La relación señal a ruido está por encima de los 26 decibelios, la tasa de error es
> prácticamente nula, y la constelación muestra cuatro nubes bien compactas y separadas.*
>
> *Este es el escenario de referencia. Todo lo que sigue se compara contra esto."*

---

### Momento 2 — El precio de ir más rápido (modulación)

**Acción:** Cambiar el selector de modulación de **QPSK** a **64-QAM**.

> *"Cambio la modulación a 64-QAM, un esquema de mayor orden.*
>
> *Miren tres cosas a la vez:*
>
> *Primero, la **constelación pasó de 4 nubes a 64**. Cada nube es un símbolo posible, y ahora cada
> símbolo transporta seis bits en lugar de dos.*
>
> *Segundo, en la barra de estado la **velocidad de transmisión se triplicó** — de sesenta a ciento
> ochenta megabits por segundo. El medidor de caudal subió en consecuencia.*
>
> *Pero tercero, y esta es la lección: **la tasa de error empeoró**. Fíjense que las nubes ahora están
> mucho más juntas entre sí. Al meter más puntos en el mismo espacio, hace falta menos ruido para
> confundir uno con otro. El indicador bajó de 'Excelente' a 'Aceptable'.*
>
> *Este es el compromiso fundamental de todo sistema digital: **más velocidad se paga con menos
> robustez**. No existe la modulación gratis."*

**Acción:** Volver a **QPSK** antes de continuar.

---

### Momento 3 — La interferencia que no se arregla con potencia (XPI)

> *"Ahora vamos a un fenómeno que está en el marco teórico y que me parece el más interesante de todo
> el módulo.*
>
> *Los satélites reutilizan la misma frecuencia transmitiendo en dos polarizaciones ortogonales —es la
> forma de duplicar la capacidad sin pedir más espectro—. Pero si el aislamiento entre esas dos
> polarizaciones falla, una se filtra en la otra y actúa como interferencia."*

**Acción:** Cambiar el selector de aislamiento XPI a **"Bajo (falla)"**.

> *"Simulo esa falla de aislamiento. La relación señal a ruido **se desploma** de veintiséis a unos
> doce decibelios. La constelación se ensancha, la tasa de error se degrada varios órdenes de
> magnitud, y el panel de notas explica lo que está pasando."*

**Acción:** Ahora subir el deslizador de **potencia de transmisión al máximo** (30 dBW).

> *"Y aquí viene lo importante. La reacción natural de cualquiera sería: 'si la señal está mal, subo
> la potencia'. Vamos a hacerlo — llevo la potencia de transmisión al máximo.*
>
> ***Y no pasa nada.*** *Miren el medidor: la relación señal a ruido sigue clavada en doce decibelios.
> Dupliqué, tripliqué la potencia, y el enlace no mejora.*
>
> *¿Por qué? Porque al subir la potencia también sube, en la misma proporción, la energía que se está
> filtrando desde la otra polarización. La señal sube, pero la interferencia sube con ella. La
> relación entre ambas no cambia.*
>
> ***El aislamiento impone un techo que la potencia no puede romper.*** *Esto no se arregla con más
> vatios: se arregla mejorando el apuntamiento, el diseño de la antena, o cambiando el tipo de
> polarización.*
>
> *Y esa es una lección que un cálculo estático en papel no transmite: hay que **ver** que el medidor
> no se mueve."*

**Acción (opcional, si hay tiempo):** Cambiar polarización de Lineal a **Circular**.

> *"De hecho, si cambio a polarización circular —que es menos sensible a la rotación de Faraday que
> introduce la ionosfera— el aislamiento mejora un poco y el techo sube. Ahí sí hay mejora, porque
> ataqué la causa correcta."*

**Acción:** Devolver la potencia a su valor por defecto y el aislamiento XPI a **"Alto"**.

---

### Momento 4 — La lluvia y la banda Ka (hasta tumbar el enlace)

> *"Último escenario, y el más dramático. Vamos a la banda Ka, que es la que se usa para internet
> satelital de alta velocidad."*

**Acción:** Cambiar la banda a **Ka**.

> *"Solo por cambiar de frecuencia, las pérdidas de trayectoria ya subieron —a mayor frecuencia,
> mayor atenuación en el espacio libre—. Pero eso todavía es manejable."*

**Acción:** Cambiar el clima a **"Lluvia Moderada"**.

> *"Ahora agrego lluvia moderada. Aparece la **nube sobre el tramo de bajada** en la representación
> superior, y observen la atenuación: el valor subió bastante más de lo que subiría en banda C.*
>
> *La razón es física: en banda C la longitud de onda es de varios centímetros, muchísimo más grande
> que una gota de lluvia, así que la gota casi no la afecta. En banda Ka la longitud de onda es
> comparable al tamaño de la gota — y ahí la lluvia sí absorbe y dispersa la señal con fuerza.*
>
> *El panel de notas lo está explicando en pantalla."*

**Acción:** Cambiar el clima a **"Lluvia Intensa"**.

> *"Y ahora, lluvia intensa.*
>
> ***El enlace se cayó.*** *Miren todo lo que ocurrió simultáneamente:*
>
> - *Los **haces se pusieron rojos**.*
> - *La **constelación colapsó** — ya no hay nubes distinguibles, los puntos llenan toda la pantalla.
>   El receptor perdió el enganche de fase: ya no puede saber qué se transmitió.*
> - *El **caudal cayó a cero**. No es que vaya lento: no pasa ningún dato.*
> - *Y el panel de notas emite la **alerta**, indicando exactamente cuál es la relación señal a ruido
>   y a cuánto se disparó la tasa de error.*
>
> *Esto es lo que un operador ve cuando una tormenta tropical pasa sobre una estación en banda Ka. Y
> es un problema real en Venezuela, por el tipo de clima que tenemos."*

**Acción:** Volver el clima a **"Despejado"**.

> *"Y al despejar, el enlace se recupera solo."*

---

## 4. Cierre (≈40 segundos)

> *"Para cerrar, tres puntos:*
>
> *Primero, **todo lo que vieron sale de un solo cálculo**. La representación del enlace, los
> medidores, el espectro, la constelación y las notas se alimentan del mismo presupuesto de enlace, así
> que nunca pueden contradecirse entre sí. Si la constelación se ensucia, el medidor de error baja y la
> nota lo explica — todo en el mismo instante.*
>
> *Segundo, **los parámetros son configurables**. Las características del satélite, los rangos de los
> controles y los textos explicativos se ajustan desde el editor sin reprogramar nada, lo que permite
> adaptar el módulo si cambian los datos de referencia.*
>
> *Y tercero, el objetivo pedagógico: este módulo permite que un estudiante **explore** el enlace en
> lugar de memorizarlo. Puede equivocarse, tumbar el enlace, ver por qué se cayó y recuperarlo. Esa
> experimentación es difícil de lograr con un satélite real."*

---

## 5. Preguntas probables del jurado

**"¿Los cálculos son reales o es una animación?"**
> *"Son reales. Cada vez que se mueve un control se recalcula el presupuesto de enlace completo con las
> fórmulas del marco teórico. De hecho, el desglose numérico está a la vista precisamente para que se
> pueda verificar la cuenta a mano."*

**"¿Por qué la potencia recibida da un valor tan negativo?"**
> *"Porque así son los enlaces satelitales reales. La señal recorre casi cuarenta mil kilómetros, y las
> pérdidas de trayectoria por sí solas superan los ciento noventa decibelios. Potencias recibidas del
> orden de cien decibelios negativos son lo normal — por eso las estaciones necesitan amplificadores de
> bajo ruido y antenas de alta ganancia. Un valor 'bonito' ahí sería la señal de que el cálculo está mal."*

**"¿De dónde salen los rangos de los deslizadores?"**
> *"De equipos reales. El rango de potencia va desde un terminal portátil pequeño hasta una estación
> central de alta potencia. El rango de ganancia corresponde a antenas desde un plato de terminal móvil
> hasta una antena de estación central de varios metros — de hecho el valor por defecto coincide con el
> cálculo de un plato de aproximadamente un metro veinte en banda Ku."*

**"¿Modela el enlace de subida y el de bajada por separado?"**
> *"En esta versión se modela un tramo equivalente, no los dos enlaces por separado. Es una
> simplificación consciente, orientada a que el fenómeno sea comprensible. La estructura del cálculo
> permite incorporar la separación en subida y bajada sin rehacer el módulo."*

**"¿La atenuación por lluvia sigue algún estándar?"**
> *"Usa un modelo simplificado por nivel de precipitación, con el agravante correspondiente a la banda
> Ka. No implementa los modelos estadísticos completos de la Unión Internacional de Telecomunicaciones,
> que dependen del ángulo de elevación y de la estadística de lluvia de cada zona. El objetivo aquí es
> demostrar el fenómeno y su magnitud relativa, no dimensionar un enlace comercial."*

**"¿Se puede adaptar a otro satélite?"**
> *"Sí. Los parámetros del satélite están separados del programa, así que cambiando esos valores el
> mismo módulo simula otro sistema."*

---

## 6. Plan B (si algo falla en vivo)

| Situación | Qué hacer |
|-----------|-----------|
| El módulo no abre desde el menú | Tener el simulador ya abierto en una segunda ventana antes de empezar |
| Se perdió el estado por tocar de más | Devolver: Banda C · Despejado · Lineal · XPI Alto · QPSK, y los deslizadores al centro-defecto |
| No se distingue la constelación desde el fondo del salón | Anunciar los cambios en voz alta ("pasó de 4 nubes a 64") y apoyarse en los medidores, que son más legibles |
| Falla el equipo por completo | Tener capturas de los cuatro momentos de la demostración como respaldo |

---

## 7. Resumen de la secuencia (chuleta de una página)

| # | Acción | Qué señalar |
|---|--------|-------------|
| 1 | *(estado inicial)* | Enlace sano: buena señal a ruido, 4 nubes compactas |
| 2 | Modulación → **64-QAM** | 64 nubes · caudal se triplica · pero el error empeora |
| 3 | Volver a **QPSK** · XPI → **Bajo** | La señal a ruido se desploma; la nota explica la interferencia |
| 4 | Potencia → **máximo** | **El medidor no se mueve** ← momento clave |
| 5 | Potencia al defecto · XPI → **Alto** · Banda → **Ka** | Suben las pérdidas solo por la frecuencia |
| 6 | Clima → **Lluvia Moderada** | Aparece la nube; la atenuación sube más que en banda C |
| 7 | Clima → **Lluvia Intensa** | Enlace caído: haces rojos, constelación colapsada, caudal en cero |
| 8 | Clima → **Despejado** | Se recupera solo |

---

## 8. Referencia rápida de los controles

Por si el jurado pregunta por un control puntual y hay que responder en una frase.

| Control | Qué representa | Al subirlo / cambiarlo |
|---------|----------------|------------------------|
| **Potencia de transmisión** | Potencia que entrega el amplificador de la estación | Mejora la señal recibida — *salvo si el aislamiento es el que limita* |
| **Ganancia de antena Tx** | Tamaño y directividad del plato transmisor | Concentra más energía hacia el satélite; suele ser más económico que subir potencia |
| **Atenuación atmosférica** | Pérdidas por gases y condiciones del aire | **Empeora** el enlace (es una pérdida). El clima le suma su propio aporte |
| **Banda (C / Ku / Ka)** | Frecuencia de trabajo | A mayor frecuencia: más pérdidas de trayectoria **y** mucha más vulnerabilidad a la lluvia. A cambio, más ancho de banda |
| **Clima** | Nivel de precipitación | Añade atenuación; en banda Ka la penalización es mucho mayor. Hace aparecer la nube en la escena |
| **Polarización** | Orientación del campo eléctrico | La circular resiste mejor la rotación de Faraday de la ionosfera → algo más de margen |
| **Aislamiento XPI** | Qué tan bien separadas se mantienen las dos polarizaciones que reutilizan la frecuencia | Si es bajo, una señal se filtra en la otra como interferencia e **impone un techo que la potencia no rompe** |
| **Modulación** | Bits transportados por símbolo (1, 2, 4 o 6) | Más orden = más velocidad, pero menos tolerancia al ruido |

| Instrumento | Qué se lee en él |
|-------------|------------------|
| **Analizador de espectro** | La distancia entre el piso de ruido y el pico de la portadora |
| **Constelación** | Si las nubes están separadas no hay errores; si se solapan, el receptor confunde símbolos |
| **Medidor de señal a ruido** | Salud general del canal |
| **Medidor de tasa de error** | Métrica final de calidad, con calificación en palabras |
| **Medidor de caudal** | Datos útiles que realmente pasan (cero si el enlace se cayó) |
| **Barra de estado** | Modulación, velocidades, latencia y jitter |
| **Panel de notas** | Explica en lenguaje llano qué fenómeno domina en ese instante |

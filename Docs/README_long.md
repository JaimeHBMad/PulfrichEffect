# TestJaime_HDRP

Proyecto de pruebas en **Unity HDRP** para **Varjo XR** orientado a experimentar con el **efecto Pulfrich** en realidad virtual: se oscurece un solo ojo y, como el cerebro procesa más tarde la imagen más oscura, los objetos que se mueven de lado parecen tener profundidad.

El proyecto incluye:

- Un **filtro por ojo** (shader + controlador) que oscurece el ojo izquierdo o el derecho.
- **Estímulos animados** (columnas y trenzas de pelotas, un coche que va y viene) para provocar el efecto.
- Utilidades de VR: contador de FPS, logger de eye tracking de Varjo y activación/desactivación del Global Volume.

---

## Estructura

```
Assets/
├── Scripts/          ← Scripts propios (explicados abajo)
├── Shaders/          ← Shader Graphs propios (explicados abajo)
├── Shaders_Fails/    ← Versiones antiguas/fallidas de EyeL/EyeR (solo referencia)
├── Materials/        ← M_SphereL (EyeL), M_SphereR (EyeR), M_Sphere, M_Floor, M_Wall, M_Dot
├── Scenes/           ← Prueba1.unity, OutdoorsScene.unity
├── Samples/          ← Ejemplos oficiales del plugin Varjo XR (no son nuestros)
└── TextMesh Pro/     ← Recursos de TMP (no son nuestros)
```

---

## Concepto clave: `Eye Index`

Todos los shaders usan el nodo **Eye Index** de Shader Graph. En VR con *single-pass instanced*, la escena se dibuja **una sola vez para los dos ojos** y este nodo indica qué ojo se está dibujando:

| Ojo       | `Eye Index` |
|-----------|:-----------:|
| Izquierdo | `0`         |
| Derecho   | `1`         |

Si conectamos ese valor (o `1 - valor`) al **Alpha** de un material transparente, el objeto se ve en un ojo y en el otro no. Todos los trucos de este proyecto se basan en esto.

---

## Shaders (`Assets/Shaders`)

Los tres son **HDRP Unlit** en modo **Transparent** (sin ZWrite y con color base negro por defecto).

### `Pulfrich.shadergraph` — Filtro oscuro para un ojo

Es el shader principal. Se aplica a un **Quad delante de la cámara** para que haga de "cristal de gafa de sol" en uno de los dos ojos.

**Propiedades**

| Propiedad   | Referencia  | Rango | Qué hace |
|-------------|-------------|-------|----------|
| Darkness    | `_Darkness` | 0–1   | Cuánto oscurece (0 = transparente, 1 = negro total) |
| EyeSide     | `_EyeSide`  | 0–1   | Qué ojo se oscurece (0 = derecho, 1 = izquierdo) |

**Grafo**

```
Eye Index ──────────────┐
                        ├─ Lerp(A, B, T) ──► Multiply(× Darkness) ──► Alpha
Eye Index ─► One Minus ─┘        ▲
                                 │
                        EyeSide (T)
```

Fórmula: `Alpha = lerp(EyeIndex, 1 - EyeIndex, EyeSide) * Darkness`

| `_EyeSide` | Ojo izq. (idx 0) | Ojo der. (idx 1) | Resultado |
|:----------:|:----------------:|:----------------:|-----------|
| 0          | `0`              | `Darkness`       | Se oscurece el **derecho** |
| 1          | `Darkness`       | `0`              | Se oscurece el **izquierdo** |

El Lerp funciona como un interruptor entre las dos opciones, así que se puede cambiar de ojo desde código sin tener dos materiales distintos. Este shader se controla con [`PulfrichController`](#pulfrichcontrollercs).

### `EyeL.shadergraph` — Visible solo en el ojo izquierdo

- Propiedad: `Color` (`_Color`) → Base Color.
- `Alpha = 1 - EyeIndex` → opaco en el ojo izquierdo (0 → 1) e invisible en el derecho (1 → 0).
- Lo usa el material **`M_SphereL`**.

### `EyeR.shadergraph` — Visible solo en el ojo derecho

- Propiedad: `Color` (`_Color`) → Base Color.
- `Alpha = EyeIndex` → invisible en el ojo izquierdo y opaco en el derecho (sin One Minus).
- Lo usa el material **`M_SphereR`**.

> Con `EyeL` y `EyeR` se puede enseñar a cada ojo un objeto diferente. Sirve para probar la estereoscopía o para simular a mano el desfase del Pulfrich.

### `Shaders_Fails/`

Son versiones anteriores de `EyeL`/`EyeR` que no funcionaban como se esperaba. Se dejan solo como historial y no deberían usarse en los materiales.

---

## Scripts (`Assets/Scripts`)

### `PulfrichController.cs`

Controla desde el teclado el material del shader **Pulfrich**.

| Campo                | Descripción |
|----------------------|-------------|
| `pulfrichMaterial`   | Material que usa el shader Pulfrich |
| `darknessLevel`      | Oscuridad que se aplica (0–1, por defecto 0.8) |
| `eye`                | Último valor enviado a `_EyeSide` (solo para depurar) |

**Controles** (usa el **nuevo Input System**):

| Tecla | Acción |
|:-----:|--------|
| `1`   | `_EyeSide = 1` → oscurece el **ojo izquierdo** y activa el filtro |
| `2`   | `_EyeSide = 0` → oscurece el **ojo derecho** y activa el filtro |
| `0`   | Desactiva el filtro (`_Darkness = 0`) |

En cada frame vuelve a escribir `_Darkness` (con `darknessLevel` si está activo, o con 0 si no), así que se puede mover el slider en Play Mode y el cambio se ve al momento.

> ⚠️ Modifica el **asset del material** directamente, así que los cambios de `_Darkness`/`_EyeSide` siguen guardados en el material después de salir de Play Mode.

---

### `PulfrichColumn.cs` — Columna de pelotas con onda de color

Genera una columna vertical de pelotas que **suben sin parar** y cambian de color siguiendo una onda senoidal, lo que crea un patrón visual de hélice.

| Campo                   | Descripción |
|-------------------------|-------------|
| `prefabPelota`          | Prefab de la pelota |
| `modoColor`             | `GrisABlanco` o `GrisANegro` |
| `cantidadPelotas`       | Número de pelotas |
| `alturaColumna`         | Altura total; al superarla la pelota vuelve abajo |
| `velocidadSubida`       | Velocidad de subida |
| `frecuenciaCicloColor`  | Lo "apretada" que está la hélice de color |
| `velocidadCambioColor`  | Velocidad a la que se desplaza la onda de color |

Cómo funciona:
1. Mueve cada pelota hacia arriba y, cuando llega a la altura máxima, la devuelve a `y = 0`.
2. `fase = sin(y · frecuencia − tiempo · velocidad)` se lleva a `t ∈ [0,1]`.
3. `Color.Lerp(gris, blanco|negro, t)`.
4. Aplica el color con un **`MaterialPropertyBlock`**, sin crear materiales nuevos, porque es más eficiente en VR. Necesita que el shader de la pelota tenga la propiedad `_Color`.

---

### `PulfrichTrenza.cs` — Trenza (doble hélice) que sube

Crea **dos hebras** de pelotas que oscilan en X en contrafase (180°) mientras suben. Vista de frente parece una trenza o una doble hélice, que es el estímulo típico del efecto Pulfrich.

| Campo             | Descripción |
|-------------------|-------------|
| `prefabPelota`    | Prefab de la pelota |
| `paresDePelotas`  | Pelotas por hebra |
| `alturaColumna`   | Altura del bucle vertical |
| `velocidadSubida` | Velocidad de subida |
| `amplitudOnda`    | Desplazamiento lateral máximo en X |
| `frecuenciaOnda`  | Número de curvas a lo largo de la altura |
| `velocidadOnda`   | Velocidad de la oscilación |

Detalles:
- **Y lógica vs. Y visual**: `yLogico` crece sin límite (`espaciado + tiempo · velocidad`) y se usa para calcular la fase. `yVisual = yLogico % alturaColumna` es la altura que se pinta. Así la pelota vuelve abajo sin que la onda dé saltos.
- `fase2 = fase1 + π` hace que las hebras estén siempre en lados opuestos.
- Las hebras están en `z = ±0.001` para evitar *z-fighting* cuando se cruzan.
- El final del archivo tiene comentada una versión anterior que además coloreaba las pelotas.

---

### `PulfrichTrenzaSimple.cs` — Trenza fija con onda que viaja

Es igual que `PulfrichTrenza`, pero **las pelotas no suben**. Cada pareja tiene una altura Y fija y lo único que se mueve es la onda en X (`fase = y · frecuencia − tiempo · velocidad`). Es más fácil de analizar porque el único movimiento es lateral, que es justo el que produce el efecto Pulfrich.

Extras:
- Crea dos GameObjects contenedores, **`Hebra 1`** y **`Hebra 2`**, como hijos del objeto (`SetParent(..., false)` para que empiecen en `(0,0,0)` local), y mete dentro las pelotas con nombres `Pelota_1`, `Pelota_2`, etc. Así la jerarquía queda ordenada y se puede, por ejemplo, poner un material distinto a cada hebra.
- Guarda las alturas en `posicionesY[]` para no recalcularlas.

---

### `CarMovement.cs` — Movimiento lineal en bucle

Mueve el objeto de `startPosition` a `endPosition` (en el plano XY) a velocidad constante con `Vector2.MoveTowards`. Al llegar al final vuelve al inicio de golpe. La **Z original se mantiene**. Sirve como estímulo sencillo de movimiento lateral para el Pulfrich.

| Campo           | Descripción |
|-----------------|-------------|
| `startPosition` | Punto inicial (X, Y) |
| `endPosition`   | Punto final (X, Y) |
| `speed`         | Unidades por segundo |

---

### `FPSViewerVR.cs` — Contador de FPS

Muestra los FPS en un `TextMeshProUGUI`, que se ve más nítido que el texto normal en VR.
- Suaviza el `deltaTime` con una media exponencial (10 %) para que el número no parpadee.
- Se pone **amarillo por debajo de 70 FPS** y **verde** por encima. El umbral es 70 porque ahora Varjo Base está limitado a 70; lo ideal en VR sería 90.

---

### `VarjoEyeTrackingLogger.cs` — Dirección de la mirada (Varjo)

Lee el eye tracking del visor Varjo (probado con XR-4) y clasifica la mirada en **Centro / Izquierda / Derecha / Arriba / Abajo**.

| Campo            | Descripción |
|------------------|-------------|
| `thresholdAngle` | Grados de desviación (1–45) para dejar de considerar "Centro" |
| `hudText`        | Texto TMP del HUD (opcional) |

Flujo:
1. Comprueba que el eye tracking esté permitido en Varjo Base (`IsGazeAllowed`).
2. Comprueba que los datos sean válidos (`GazeStatus.Valid`), porque dejan de serlo al parpadear o al quitarse el visor.
3. Convierte el vector `gaze.forward` en ángulos: **yaw** = `atan2(x, z)` y **pitch** = `asin(y)`.
4. Si algún ángulo supera el umbral, elige el eje dominante.
5. Muestra dirección y ángulos en el HUD y hace un `Debug.Log` **solo cuando cambia** la dirección.

Necesita el paquete **Varjo XR Plugin**.

---

### `GlobalVolumeBehave.cs` — Activar/desactivar el Global Volume

Con la tecla **Espacio** activa o desactiva el GameObject del Global Volume (post-procesado de HDRP) para comparar la escena con y sin efectos. Usa el **nuevo Input System**.

### `GlobalVolumeBehave_oldInputSystem.cs`

Hace lo mismo con el **Input System antiguo** (`Input.GetKeyDown`). Solo funciona si en *Project Settings → Player → Active Input Handling* está activado el sistema antiguo o *Both*.

> Nota: en esta versión se llama a `SetActive(isOn)` **antes** de invertir `isOn`, que empieza en `true`. Por eso la primera pulsación no cambia nada. La versión nueva no tiene este problema.

---

## Montaje típico de una prueba Pulfrich

1. Añadir a la escena un objeto con **`PulfrichTrenzaSimple`** (o `PulfrichTrenza` / `CarMovement`) y asignarle el prefab de la pelota.
2. Poner un **Quad** hijo de la cámara XR, justo delante, con un material que use el shader **Pulfrich**.
3. Añadir **`PulfrichController`** a cualquier objeto y asignarle ese material.
4. En Play Mode: pulsar `1` o `2` para elegir el ojo, `0` para quitar el filtro y ajustar `darknessLevel` para cambiar la intensidad del efecto.

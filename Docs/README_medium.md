# TestJaime_HDRP

Proyecto de pruebas en **Unity HDRP** para **Varjo XR** que reproduce el **efecto Pulfrich** en VR. La idea es oscurecer un solo ojo: el cerebro procesa más tarde la imagen más oscura, y por eso los objetos que se mueven de lado parecen tener profundidad.

> Hay una versión más completa, con todos los scripts y shaders, en [README_long.md](README_long.md).

---

## Shader: `Pulfrich.shadergraph`

*Ruta: `Assets/Shaders/Pulfrich.shadergraph`. Tipo: HDRP Unlit, Transparent, color base negro.*

Se aplica a un **Quad delante de la cámara** para que funcione como el "cristal de gafa de sol" de uno de los dos ojos.

### Cómo sabe qué ojo se está pintando: `Eye Index`

En VR con *single-pass instanced*, la escena se dibuja una sola vez para los dos ojos. El nodo **Eye Index** indica cuál se está pintando: `0` es el izquierdo y `1` el derecho. Si ese valor se lleva al **Alpha**, el Quad se ve en un ojo y es transparente en el otro.

### Propiedades

| Propiedad | Referencia  | Rango | Qué hace |
|-----------|-------------|-------|----------|
| Darkness  | `_Darkness` | 0–1   | Cuánto oscurece (0 = transparente, 1 = negro total) |
| EyeSide   | `_EyeSide`  | 0–1   | Qué ojo se oscurece (0 = derecho, 1 = izquierdo) |

### Grafo

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

El Lerp sirve de interruptor entre las dos opciones, así que basta un único material para oscurecer cualquiera de los dos ojos.

---

## Scripts

### `PulfrichController.cs`

Controla con el teclado el material del shader Pulfrich. Usa el **nuevo Input System**.

| Campo              | Descripción |
|--------------------|-------------|
| `pulfrichMaterial` | Material que usa el shader Pulfrich |
| `darknessLevel`    | Oscuridad que se aplica (0–1, por defecto 0.8) |
| `eye`              | Último valor enviado a `_EyeSide` (solo para depurar) |

| Tecla | Acción |
|:-----:|--------|
| `1`   | `_EyeSide = 1` → oscurece el **ojo izquierdo** y activa el filtro |
| `2`   | `_EyeSide = 0` → oscurece el **ojo derecho** y activa el filtro |
| `0`   | Desactiva el filtro (`_Darkness = 0`) |

En cada frame vuelve a escribir `_Darkness` (con `darknessLevel` si está activo, o con 0 si no), así que al mover el slider en Play Mode el cambio se ve al momento.

> ⚠️ Modifica el **asset del material** directamente, así que los valores siguen guardados en el material después de salir de Play Mode.

---

### `PulfrichTrenzaSimple.cs`

Genera una **trenza de pelotas**: dos hebras en contrafase (desfasadas 180°) que oscilan en X. Cada pelota tiene una **altura Y fija** y lo único que se mueve es la onda lateral, que es justo el movimiento que produce el efecto Pulfrich.

| Campo            | Descripción |
|------------------|-------------|
| `prefabPelota`   | Prefab de la pelota |
| `paresDePelotas` | Pelotas por hebra |
| `alturaColumna`  | Altura total de la trenza |
| `amplitudOnda`   | Desplazamiento lateral máximo en X |
| `frecuenciaOnda` | Número de curvas a lo largo de la altura |
| `velocidadOnda`  | Velocidad a la que avanza la onda |

**Start**
1. Crea dos contenedores, **`Hebra 1`** y **`Hebra 2`**, como hijos del objeto. Usa `SetParent(..., false)` para que empiecen en `(0,0,0)` local.
2. Instancia las pelotas dentro de cada contenedor con nombres `Pelota_1`, `Pelota_2`, etc.
3. Reparte las alturas de forma equidistante (`alturaColumna / paresDePelotas · i`) y las guarda en `posicionesY[]`.

**Update**, para cada pareja:
```
fase1 = y · frecuenciaOnda − tiempo · velocidadOnda
fase2 = fase1 + π                         // lado opuesto
x1 = sin(fase1) · amplitudOnda
x2 = sin(fase2) · amplitudOnda
```
Las hebras están en `z = ±0.001` para evitar el parpadeo por *z-fighting* cuando se cruzan.

---

### `GlobalVolumeBehave.cs`

Con la tecla **Espacio** activa o desactiva el GameObject del **Global Volume** (post-procesado de HDRP) para comparar la escena con y sin efectos. Usa el **nuevo Input System**.

| Campo          | Descripción |
|----------------|-------------|
| `globalVolume` | GameObject del Global Volume |

- Empieza con `isOn = true`.
- Cada pulsación invierte `isOn` y llama a `SetActive(isOn)`.
- Si no se ha asignado el Global Volume en el Inspector, muestra un warning en consola.

---

## Montaje rápido

1. Añadir un objeto con **`PulfrichTrenzaSimple`** y asignarle el prefab de la pelota.
2. Poner un **Quad** hijo de la cámara XR, justo delante, con un material que use el shader **Pulfrich**.
3. Añadir **`PulfrichController`** y asignarle ese material.
4. (Opcional) Añadir **`GlobalVolumeBehave`** y asignarle el Global Volume.
5. En Play Mode:

| Tecla    | Acción |
|:--------:|--------|
| `1`      | Oscurece el ojo izquierdo |
| `2`      | Oscurece el ojo derecho |
| `0`      | Quita el filtro |
| `Espacio`| Activa o desactiva el post-procesado |

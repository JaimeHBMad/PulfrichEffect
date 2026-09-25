# Pulfrich Effect en VR

Pruebas del efecto Pulfrich en VR (Unity HDRP + Varjo). Se oscurece un solo ojo, de tal forma que da lugar a un desfase temporal interocular haciendo que los objetos que se mueven de lado parecen tener profundidad.

<!--  Documentación más detallada en la carpeta [Docs/](Docs/). -->

## Shader

**Pulfrich**: Shader que se coloca en un Quad como hijo y delante de la camara y oscurece solo una de las pantallas de las gafas. Este shader se basa en el nodo _Eye Index_ (0 = ojo izquierdo, 1 = ojo derecho) para saber que ojo esta pintando el buffer.

- `_Darkness`: Variable que controla cuanto se oscurece (0 = nada, 1 = negro).
- `_EyeSide`: Variable que determina que ojo se oscurece (0 = derecho, 1 = izquierdo).

## Scripts

**PulfrichController**: cambia el shader con el teclado.
- `1` → oscurece el ojo izquierdo
- `2` → oscurece el ojo derecho
- `0` → quita el filtro

**PulfrichTrenzaSimple**: crea dos hileras de pelotas que se mueven de lado a lado en direcciones opuestas y forman una doble helice (trenza ADN). Este script se llama simple ya que las pelotas no suben ni bajan; solo se mueven de lado, acentuandose así el efecto Pulfrich sin otros estimulos, ilusiones o efectos.

**GlobalVolumeBehave**: con `Espacio` activa o desactiva el post-procesado (Global Volume).

A continuación adjunto 3 figuras que explican visualmente la ilusión que se crea con el efecto Pulfrich.
<img width="1220" height="612" alt="Captura de pantalla 2026-09-25 115526" src="https://github.com/user-attachments/assets/65ddde01-cfdc-4248-be55-3ebc292d8d27" />
_Esquema del efecto Pulfrich en movimiento de izquierda a derecha, donde el objeto se percibe más lejos de su trayectoria real_
<img width="1223" height="607" alt="Captura de pantalla 2026-09-25 115536" src="https://github.com/user-attachments/assets/ac28de6f-c2a8-4286-80fb-7b888e3f00de" />
_Esquema del efecto Pulfrich en movimiento de derecha a izquierda, donde el objeto se percibe más cerca de su trayectoria real_
<img width="1226" height="647" alt="Captura de pantalla 2026-09-25 115542" src="https://github.com/user-attachments/assets/7be031e2-b707-4c5b-9ec3-19182de5f644" />
_Trayectoria elíptica ilusoria generada por el efecto Pulfrich con un movimiento rectilineo._

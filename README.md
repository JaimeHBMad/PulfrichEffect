# Pulfrich Effect en VR

Pruebas del efecto Pulfrich en VR (Unity HDRP + Varjo). Se oscurece un solo ojo, de tal forma que da lugar a un desfase temporal interocular haciendo que los objetos que se mueven de lado parecen tener profundidad.

<!--  Documentación más detallada en la carpeta [Docs/](Docs/). -->

## Shader `Pulfrich`

Shader que se coloca en un Quad como hijo y delante de la camara y oscurece solo una de las pantallas de las gafas. Este shader se basa en el nodo `Eye Index` (0 = ojo izquierdo, 1 = ojo derecho) para saber que ojo esta pintando el buffer.

- `_Darkness`: Variable que controla cuanto oscurece (0 = nada, 1 = negro).
- `_EyeSide`: Variable que determina que ojo se oscurece (0 = derecho, 1 = izquierdo).

## Scripts

**`PulfrichController`**: cambia el shader con el teclado.
- `1` → oscurece el ojo izquierdo
- `2` → oscurece el ojo derecho
- `0` → quita el filtro

**`PulfrichTrenzaSimple`**: crea dos hileras de pelotas que se mueven de lado a lado en direcciones opuestas y forman una doble helice (trenza ADN). Este script se llama simple ya que las pelotas no suben ni bajan; solo se mueven de lado, acentuandose así el efecto Pulfrich sin otros estimulos, ilusiones o efectos.

**`GlobalVolumeBehave`**: con `Espacio` activa o desactiva el post-procesado (Global Volume).

using UnityEngine;

public class PenroseStaircaseBuilder : MonoBehaviour
{
    public Material stairMaterial;
    public float stepHeight = 0.25f;

    [ContextMenu("Generar Escalera Imposible")]
    void Start()
    {
        // 1. Configurar la cámara en proyección isométrica matemática exacta
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(-10f, 10f, -10f);
            cam.transform.rotation = Quaternion.Euler(35.26439f, 45f, 0f);
        }

        // Para que el final coincida con (0,0,0) en isométrico, debe acabar en (k, k, k).
        // Con 12 peldaños a 0.25 de altura: subimos Y = 3.0.
        // Si los lados miden 5 (+Z), 4 (+X), 2 (-Z) y 1 (-X):
        // X final = 4 - 1 = 3.  Z final = 5 - 2 = 3.  Y final = 12 * 0.25 = 3.
        // Punto final = (3, 3, 3) -> Coincide al milímetro en pantalla con (0, 0, 0).

        int[] tramos = { 5, 4, 2, 1 };
        Vector3[] direcciones = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

        Vector3 currentPos = Vector3.zero;
        int stepIndex = 0;

        // Crear peldaño inicial base
        CreateStep(currentPos, stepIndex++);

        for (int lado = 0; lado < 4; lado++)
        {
            for (int i = 0; i < tramos[lado]; i++)
            {
                currentPos += direcciones[lado] + new Vector3(0, stepHeight, 0);
                // El último peldaño (índice 12) cae en (3,3,3), idéntico en pantalla al (0,0,0),
                // así que no hace falta instanciar el cubo 12 porque el cubo 0 ya ocupa su lugar.
                if (stepIndex < 12) 
                {
                    CreateStep(currentPos, stepIndex++);
                }
            }
        }
    }

    void CreateStep(Vector3 pos, int index)
    {
        GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
        step.name = $"Step_{index}";
        step.transform.SetParent(transform);
        // Escalamos hacia abajo para que parezca un pilar sólido y limpio
        step.transform.localScale = new Vector3(1f, 2f, 1f);
        step.transform.position = pos;

        if (stairMaterial != null)
            step.GetComponent<Renderer>().sharedMaterial = stairMaterial;
    }
}
using UnityEngine;

public class Staircase : MonoBehaviour
{
    public Material stairMaterial;
    public float stepHeight = 0.25f;

    [ContextMenu("Generar Escalera Imposible")]
    public void BuildStaircase()
    {
        // Limpiar peldaños previos si regeneramos en el editor
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // 1. Colocar la cámara exactamente encuadrada
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 4.5f;
            cam.transform.position = new Vector3(-10f, 14f, -10f);
            cam.transform.rotation = Quaternion.Euler(35.26439f, 45f, 0f);
        }

        // Vector de vista de la cámara = (1, -1, 1).
        // Si subimos 12 escalones de 0.25 -> Y final = +3.
        // Para proyectarse en el mismo píxel que (0,0,0), el punto 12 debe caer en (-3, 3, -3).
        // Lados: 2 en (+Z), 1 en (+X), 5 en (-Z), 4 en (-X)
        // Balance X: +1 - 4 = -3 | Balance Z: +2 - 5 = -3 | Balance Y: 12 * 0.25 = +3.
        int[] tramos = { 2, 1, 5, 4 };
        Vector3[] direcciones = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

        Vector3 currentPos = Vector3.zero;
        int stepIndex = 0;

        CreateStep(currentPos, stepIndex++);

        for (int lado = 0; lado < 4; lado++)
        {
            for (int i = 0; i < tramos[lado]; i++)
            {
                currentPos += direcciones[lado] + new Vector3(0f, stepHeight, 0f);
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
        step.name = $"Step_{index:00}";
        step.transform.SetParent(transform);
        // desplazamos el cubo 0.5 hacia abajo para que la cara superior quede en pos.y
        step.transform.localScale = new Vector3(1f, 2f, 1f);
        step.transform.position = pos - new Vector3(0f, 1f, 0f);

        if (stairMaterial != null)
            step.GetComponent<Renderer>().sharedMaterial = stairMaterial;
    }
}
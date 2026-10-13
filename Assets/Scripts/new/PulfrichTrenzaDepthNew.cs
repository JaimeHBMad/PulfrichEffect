using UnityEngine;
using UnityEngine.InputSystem; // Para ajustar depth

public class PulfrichTrenzaDepthNew : MonoBehaviour
{
    [Header("Configuración Principal")]
    public GameObject prefabPelota;

    [Header("Parámetros de la Estructura")]
    public int paresDePelotas = 20;
    public float alturaColumna = 5f;

    [Header("Parámetros del Efecto Visual")]
    public float amplitudOnda = 0.5f;
    public float frecuenciaOnda = 3f;
    public float velocidadOnda = 2f;

    [Header("Anulación Pulfrich (Profundidad)")]
    [Tooltip("Ajusta este valor para contrarrestar la ilusión de profundidad. Valores positivos o negativos simulan movimiento en Z.")]
    [Range(-2f, 2f)]
    public float compensacionProfundidad = 0f;

    [Tooltip("Cantidad exacta que suma o resta cada pulsación.")]
    public float pasoAjusteProfundidad = 0.1f;

    [Tooltip("Valor máximo (en valor absoluto) que puede alcanzar la compensación.")]
    public float limiteCompensacion = 2f;

    [Tooltip("Si está desactivado se ignoran las pulsaciones de ajuste. La prueba lo gestiona sola.")]
    public bool permitirAjuste = true;

    private Transform[] hebra1;
    private Transform[] hebra2;
    private float[] posicionesY;
    private GameObject contenedorHebra1;
    private GameObject contenedorHebra2;

    void Start()
    {
        hebra1 = new Transform[paresDePelotas];
        hebra2 = new Transform[paresDePelotas];
        posicionesY = new float[paresDePelotas];

        // 1. Contenedores vacíos para cada hebra
        contenedorHebra1 = new GameObject("Hebra 1");
        contenedorHebra2 = new GameObject("Hebra 2");

        // 2. Emparentados al objeto principal ('false' para que el localPosition sea (0,0,0))
        contenedorHebra1.transform.SetParent(transform, false);
        contenedorHebra2.transform.SetParent(transform, false);

        for (int i = 0; i < paresDePelotas; i++)
        {
            // 3. Instanciamos las pelotas dentro de su contenedor
            GameObject p1 = Instantiate(prefabPelota, contenedorHebra1.transform);
            p1.name = "Pelota_" + (i + 1);
            hebra1[i] = p1.transform;

            GameObject p2 = Instantiate(prefabPelota, contenedorHebra2.transform);
            p2.name = "Pelota_" + (i + 1);
            hebra2[i] = p2.transform;

            // Posición Y fija de esta pareja
            float espaciado = (alturaColumna / paresDePelotas) * i;
            posicionesY[i] = espaciado;

            // Posicionamiento inicial
            hebra1[i].localPosition = new Vector3(0, espaciado, -0.001f);
            hebra2[i].localPosition = new Vector3(0, espaciado, 0.001f);
        }
    }

    // ---------- API para la prueba ----------

    public void EstablecerCompensacion(float valor)
    {
        valor = Mathf.Clamp(valor, -limiteCompensacion, limiteCompensacion);
        // Redondeo a 4 decimales para no arrastrar error de coma flotante al sumar pasos
        compensacionProfundidad = Mathf.Round(valor * 10000f) / 10000f;
    }

    public void MostrarPelotas(bool visible)
    {
        if (contenedorHebra1 != null) contenedorHebra1.SetActive(visible);
        if (contenedorHebra2 != null) contenedorHebra2.SetActive(visible);
    }

    // ---------- Input (PlayerInput → Invoke Unity Events) ----------

    public void AcercandoseBigL(InputAction.CallbackContext cntxt)
    {
        if (cntxt.performed) Ajustar(pasoAjusteProfundidad);
    }

    public void AcercandoseBigR(InputAction.CallbackContext cntxt)
    {
        if (cntxt.performed) Ajustar(-pasoAjusteProfundidad);
    }

    public void AcercandoseSmallL(InputAction.CallbackContext cntxt)
    {
        if (cntxt.performed) Ajustar(pasoAjusteProfundidad / 10f);
    }

    public void AcercandoseSmallR(InputAction.CallbackContext cntxt)
    {
        if (cntxt.performed) Ajustar(-pasoAjusteProfundidad / 10f);
    }

    private void Ajustar(float delta)
    {
        if (!permitirAjuste) return;
        EstablecerCompensacion(compensacionProfundidad + delta);
        Debug.Log($"Compensación Z: {compensacionProfundidad:F3}");
    }

    void Update()
    {
        float tiempoAnimacion = Time.time * velocidadOnda;

        for (int i = 0; i < paresDePelotas; i++)
        {
            float yFijo = posicionesY[i];

            float fase1 = (yFijo * frecuenciaOnda) - tiempoAnimacion;
            float fase2 = fase1 + Mathf.PI;

            // El coseno está desfasado 90º del seno → movimiento circular/elíptico.
            // 'compensacionProfundidad' introduce rotación física real opuesta a la ilusión.
            float x1 = Mathf.Sin(fase1) * amplitudOnda;
            float z1 = Mathf.Cos(fase1) * compensacionProfundidad;
            hebra1[i].localPosition = new Vector3(x1, yFijo, z1 - 0.001f);

            float x2 = Mathf.Sin(fase2) * amplitudOnda;
            float z2 = Mathf.Cos(fase2) * compensacionProfundidad;
            hebra2[i].localPosition = new Vector3(x2, yFijo, z2 + 0.001f);
        }
    }
}

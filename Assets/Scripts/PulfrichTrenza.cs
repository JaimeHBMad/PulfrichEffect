using UnityEngine;

public class PulfrichTrenza : MonoBehaviour
{
    public enum ConfiguracionColor { GrisABlanco, GrisANegro }

    [Header("Configuración Principal")]
    public GameObject prefabPelota;
    public ConfiguracionColor modoColor = ConfiguracionColor.GrisABlanco;
    
    [Header("Parámetros de las Ondas")]
    public int paresDePelotas = 20; 
    public float alturaColumna = 5f;
    public float velocidadSubida = 1.0f;
    public float amplitudOnda = 0.5f; // Cuánto se desplazan a izquierda y derecha en X
    
    [Header("Parámetros del Efecto Visual")]
    public float frecuenciaCicloColor = 3f; 
    public float velocidadCambioColor = 2f;

// ... existing code ...
    private Transform[] hebra1;
    private Transform[] hebra2;
    // Eliminamos el array alturasY[], ya no necesitamos guardar el estado manualmente
    private MaterialPropertyBlock propBlock;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    void Start()
    {
        hebra1 = new Transform[paresDePelotas];
        hebra2 = new Transform[paresDePelotas];
        propBlock = new MaterialPropertyBlock();

        for (int i = 0; i < paresDePelotas; i++)
        {
            GameObject p1 = Instantiate(prefabPelota, transform);
            hebra1[i] = p1.transform;

            GameObject p2 = Instantiate(prefabPelota, transform);
            hebra2[i] = p2.transform;
        }
    }

    void Update()
    {
        float tiempoColor = Time.time * velocidadCambioColor;
        float desplazamientoVertical = Time.time * velocidadSubida;

        for (int i = 0; i < paresDePelotas; i++)
        {
            // 1. Calculamos la posición base equidistante
            float espaciadoInicial = (alturaColumna / paresDePelotas) * i;
            
            // 2. Altura Lógica (infinita) para evitar saltos en la fase de la onda
            float yLogico = espaciadoInicial + desplazamientoVertical;
            
            // 3. Altura Visual (bucle) con módulo para mantener la distancia perfecta
            float yVisual = yLogico % alturaColumna;

            // 4. Fases calculadas sobre el Y infinito (sin saltos)
            float fase1 = (yLogico * frecuenciaCicloColor) - tiempoColor;
            float fase2 = fase1 + Mathf.PI;

            // 5. Aplicar posiciones 2D
            float x1 = Mathf.Sin(fase1) * amplitudOnda;
            hebra1[i].localPosition = new Vector3(x1, yVisual, -0.001f);

            float x2 = Mathf.Sin(fase2) * amplitudOnda;
            hebra2[i].localPosition = new Vector3(x2, yVisual, 0.001f);

            // 6. Aplicar colores
            float t1 = (Mathf.Sin(fase1) + 1f) / 2f;
            float t2 = (Mathf.Sin(fase2) + 1f) / 2f;

            AplicarColor(hebra1[i], ObtenerColor(t1));
            AplicarColor(hebra2[i], ObtenerColor(t2));
        }
    }

    private Color ObtenerColor(float t)
    {
        return modoColor == ConfiguracionColor.GrisABlanco 
            ? Color.Lerp(Color.gray, Color.white, t) 
            : Color.Lerp(Color.gray, Color.black, t);
    }

    private void AplicarColor(Transform pelota, Color color)
    {
        Renderer rend = pelota.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor(ColorProperty, color);
            rend.SetPropertyBlock(propBlock);
        }
    }
}
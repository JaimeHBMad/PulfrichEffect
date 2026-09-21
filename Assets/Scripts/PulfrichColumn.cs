using UnityEngine;

public class PulfrichColumn : MonoBehaviour
{
    // Las dos opciones que pedías
    public enum ConfiguracionColor
    {
        GrisABlanco,
        GrisANegro
    }

    [Header("Configuración Principal")]
    public GameObject prefabPelota;
    public ConfiguracionColor modoColor = ConfiguracionColor.GrisABlanco;
    
    [Header("Parámetros de la Columna")]
    public int cantidadPelotas = 20;
    public float alturaColumna = 5f;
    public float velocidadSubida = 1.0f;
    
    [Header("Parámetros del Efecto Visual")]
    public float frecuenciaCicloColor = 3f; // Cómo de apretada es la "hélice" de color
    public float velocidadCambioColor = 2f;

    private Transform[] pelotas;
    private MaterialPropertyBlock propBlock;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    void Start()
    {
        pelotas = new Transform[cantidadPelotas];
        propBlock = new MaterialPropertyBlock();

        for (int i = 0; i < cantidadPelotas; i++)
        {
            // Instanciar la pelota como hija de este objeto
            GameObject pelota = Instantiate(prefabPelota, transform);
            
            // Colocarla en su posición vertical inicial
            float posicionY = (alturaColumna / cantidadPelotas) * i;
            pelota.transform.localPosition = new Vector3(0, posicionY, 0);
            
            pelotas[i] = pelota.transform;
        }
    }

    void Update()
    {
        for (int i = 0; i < pelotas.Length; i++)
        {
            Transform pelota = pelotas[i];

            // 1. Movimiento Vertical Lineal
            pelota.Translate(Vector3.up * velocidadSubida * Time.deltaTime);

            // 2. Si la pelota llega arriba del todo, vuelve abajo
            if (pelota.localPosition.y > alturaColumna)
            {
                pelota.localPosition = new Vector3(0, 0, 0);
            }

            // 3. Calcular la fase del color usando la onda senoidal (esto crea el patrón de hélice visual)
            float fase = Mathf.Sin((pelota.localPosition.y * frecuenciaCicloColor) - (Time.time * velocidadCambioColor));
            
            // El seno va de -1 a 1, lo pasamos a rango 0 a 1 para interpolar colores
            float t = (fase + 1f) / 2f; 

            // 4. Seleccionar la paleta de colores según la configuración elegida en el Inspector
            Color colorActual;
            if (modoColor == ConfiguracionColor.GrisABlanco)
            {
                colorActual = Color.Lerp(Color.gray, Color.white, t);
            }
            else // GrisANegro
            {
                colorActual = Color.Lerp(Color.gray, Color.black, t);
            }

            // 5. Aplicar el color a la pelota sin crear nuevos materiales (optimizado para VR)
            Renderer rend = pelota.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(propBlock);
                propBlock.SetColor(ColorProperty, colorActual);
                rend.SetPropertyBlock(propBlock);
            }
        }
    }
}
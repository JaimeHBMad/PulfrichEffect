using UnityEngine;
using UnityEngine.InputSystem; // Para ajustar depth

public class PulfrichTrenzaSimple : MonoBehaviour
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

    private Transform[] hebra1;
    private Transform[] hebra2;
    private float[] posicionesY; 


    [Header("Anulación Pulfrich (Profundidad)")]
    [Tooltip("Ajusta este valor para contrarrestar la ilusión de profundidad. Valores positivos o negativos simulan movimiento en Z.")]
    [Range(-2f, 2f)]
    public float compensacionProfundidad = 0f; // <--- NUEVA VARIABLE
    [Tooltip("Cantidad exacta que suma o resta cada pulsación.")]
    public float pasoAjusteProfundidad = 0.1f; // NUEVO: Controla el tamaño del escalón

    [Header("Input (Nuevo Sistema)")]
    [Tooltip("Asigna aquí tu Input Action llamado 'AjustarProfundidad'.")]
    public InputActionReference accionAjustarProfundidad; // Referencia al Input Action Asset

    void Start()
    {
        hebra1 = new Transform[paresDePelotas];
        hebra2 = new Transform[paresDePelotas];
        posicionesY = new float[paresDePelotas];

        // 1. Creamos los objetos vacíos que servirán de contenedores
        GameObject contenedorHebra1 = new GameObject("Hebra 1");
        GameObject contenedorHebra2 = new GameObject("Hebra 2");

        // 2. Emparentamos estos contenedores al objeto principal (el que lleva el script)
        // EL CAMBIO ESTÁ AQUÍ: Añadimos 'false' para que el localPosition sea (0,0,0)
        contenedorHebra1.transform.SetParent(this.transform,false);
        contenedorHebra2.transform.SetParent(this.transform,false);

        for (int i = 0; i < paresDePelotas; i++)
        {
            // 3. Instanciamos las pelotas directamente dentro de su contenedor correspondiente
            GameObject p1 = Instantiate(prefabPelota, contenedorHebra1.transform);
            p1.name = "Pelota_" + (i + 1); // Nombra "Pelota_1", "Pelota_2", etc.
            hebra1[i] = p1.transform;

            GameObject p2 = Instantiate(prefabPelota, contenedorHebra2.transform);
            p2.name = "Pelota_" + (i + 1);
            hebra2[i] = p2.transform;

            // Calcular y guardar la posición Y
            float espaciado = (alturaColumna / paresDePelotas) * i;
            posicionesY[i] = espaciado;

            // Posicionamiento inicial
            hebra1[i].localPosition = new Vector3(0, espaciado, -0.001f);
            hebra2[i].localPosition = new Vector3(0, espaciado, 0.001f);
        }
    }

    // --- NUEVO: Habilitar y deshabilitar el Input (nuevo system)---
    private void OnEnable()
    {
        if (accionAjustarProfundidad != null)
        {
            // Suscribirse al evento cuando la tecla se pulsa
            accionAjustarProfundidad.action.performed += OnAjustarProfundidad;
            accionAjustarProfundidad.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (accionAjustarProfundidad != null)
        {
            // Desuscribirse del evento al desactivar el script
            accionAjustarProfundidad.action.performed -= OnAjustarProfundidad;
            accionAjustarProfundidad.action.Disable();
        }
    }

    // --- NUEVO: Lógica que se ejecuta al pulsar las flechas ---
    private void OnAjustarProfundidad(InputAction.CallbackContext context)
    {
        // context.ReadValue<float>() devolverá 1 (Positive/Up) o -1 (Negative/Down)
        float direccion = context.ReadValue<float>(); 

        // Si es 1 (Arriba), suma el paso. Si es -1 (Abajo), resta el paso.
        compensacionProfundidad += (direccion * pasoAjusteProfundidad);

        // Limitamos el valor
        compensacionProfundidad = Mathf.Clamp(compensacionProfundidad, -2f, 2f);
    }

    void Update()
    {
        float tiempoAnimacion = Time.time * velocidadOnda;


        for (int i = 0; i < paresDePelotas; i++)
        {
            float yFijo = posicionesY[i];

            float fase1 = (yFijo * frecuenciaOnda) - tiempoAnimacion;
            float fase2 = fase1 + Mathf.PI; 

            // NUEVO: Calculamos una compensación en profundidad (Z) usando coseno
            // El coseno está desfasado 90 grados del seno, creando un movimiento circular/elíptico.
            // Al ajustar 'compensacionProfundidad', introducimos rotación física real opuesta a la ilusión.

            float x1 = Mathf.Sin(fase1) * amplitudOnda;
            float z1 = Mathf.Cos(fase1) * compensacionProfundidad;
            hebra1[i].localPosition = new Vector3(x1, yFijo, z1 -0.001f);
            

            float x2 = Mathf.Sin(fase2) * amplitudOnda;
            float z2 = Mathf.Cos(fase2) * compensacionProfundidad;
            hebra2[i].localPosition = new Vector3(x2, yFijo, z2 + 0.001f);
        }


    }
}


// using UnityEngine;

// public class PulfrichTrenzaSimple : MonoBehaviour
// {
//     [Header("Configuración Principal")]
//     public GameObject prefabPelota;
    
//     [Header("Parámetros de la Estructura")]
//     public int paresDePelotas = 20; 
//     public float alturaColumna = 5f;
    
//     [Header("Parámetros del Efecto Visual")]
//     public float amplitudOnda = 0.5f;   // Desplazamiento máximo a izquierda y derecha
//     public float frecuenciaOnda = 3f;   // Cuántas "curvas" tiene la trenza
//     public float velocidadOnda = 2f;    // Cuánto de rápido "gira" o se desplaza el movimiento

//     private Transform[] hebra1;
//     private Transform[] hebra2;
//     private float[] posicionesY; // Ahora guardamos la Y fija de cada pelota

//     void Start()
//     {
//         hebra1 = new Transform[paresDePelotas];
//         hebra2 = new Transform[paresDePelotas];
//         posicionesY = new float[paresDePelotas];

//         for (int i = 0; i < paresDePelotas; i++)
//         {
//             // Instanciar
//             GameObject p1 = Instantiate(prefabPelota, transform);
//             hebra1[i] = p1.transform;

//             GameObject p2 = Instantiate(prefabPelota, transform);
//             hebra2[i] = p2.transform;

//             // Calcular y guardar la posición Y (fija) para esta pareja
//             float espaciado = (alturaColumna / paresDePelotas) * i;
//             posicionesY[i] = espaciado;

//             // Posicionar inicialmente en Y (X se actualizará en el Update)
//             hebra1[i].localPosition = new Vector3(0, espaciado, -0.001f);
//             hebra2[i].localPosition = new Vector3(0, espaciado, 0.001f);
//         }
//     }

//     void Update()
//     {
//         // El reloj que hace avanzar la onda de izquierda a derecha
//         float tiempoAnimacion = Time.time * velocidadOnda;

//         for (int i = 0; i < paresDePelotas; i++)
//         {
//             float yFijo = posicionesY[i];

//             // 1. Calculamos las fases basándonos en la altura y restándole el tiempo
//             float fase1 = (yFijo * frecuenciaOnda) - tiempoAnimacion;
//             float fase2 = fase1 + Mathf.PI; // 180 grados de desfase para la otra hebra

//             // 2. Aplicamos la función seno para el movimiento lateral
//             float x1 = Mathf.Sin(fase1) * amplitudOnda;
//             hebra1[i].localPosition = new Vector3(x1, yFijo, -0.001f);

//             float x2 = Mathf.Sin(fase2) * amplitudOnda;
//             hebra2[i].localPosition = new Vector3(x2, yFijo, 0.001f);
//         }
//     }
// }
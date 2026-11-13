using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Prueba Pulfrich 2AFC: presenta las compensaciones Z del inspector, cada una
/// X veces y en orden aleatorio. El participante no ajusta nada, solo responde
/// si ve la hélice "front left" (0) o "front right" (1), pulsando el gatillo correspondiente en el mando.
/// Cada ensayo se guarda en un CSV.
/// IMPORTANTE: ponlo en un objeto distinto de la hélice y desactiva PruebaPulfrich.cs
/// </summary>
public class PruebaPulfrich2AFC_new_simple : MonoBehaviour
{
    [Header("Referencias")]
    public PulfrichControllerNew Filtro;         // Filtro VDF
    public PulfrichTrenzaDepthNew Estimulo;                   // Estimulo, helice


    [Header("Condiciones")]
    public float[] compensaciones = { -0.2f, -0.1f, 0f, 0.1f, 0.2f };           // Lista de compensaciones

    public int repeticiones = 20;                               // Veces que se repite cada compensacion

    [SerializeField] private int totalEnsayos;                  // Solo informativo, se calculo automaticamente: total = comepnsacion * repeticiones
    [Tooltip("Densidad óptica CON SIGNO para toda la prueba. Negativa = filtro en ojo izquierdo, positiva = ojo derecho, 0 = sin filtro.")]
    public float densidadOptica = 0.2f;         // OD del filter, Optic Density, densidad optica,

    [Header("Desarrollo")]
    public bool aleatorizarOrden = true;
    public bool empezarAlArrancar = true;

    [Tooltip("Segundos con la hélice oculta entre ensayos.")]
    public float pausaEntreEnsayos = 1f;

    [Header("Registro")]
    public string idParticipante = "P01";           // id con el que se guardaran los datos en el CSV
    public float duracionEstimulo = 3f;         // duracion = 0, Para que no haya tiempo
    private readonly List<float> ensayos = new List<float>();   // compensación de cada ensayo
    private bool enCurso;               
    private bool esperandoRespuesta;        // bool que controla cuando es el tiempo para el usuario responder
    private int respuesta;          // 1 = front right, 0 = front left, -1 = aún sin responder, ademas de ser variable para la psicometrica es variable de control junto al bool esperandoRespuesta
    private string rutaCsv;

    void OnValidate()               // permite ver los cambios en el inspector de Unity en tiempo real
    {
        int nc = compensaciones.Length; // numero de compensaciones
        totalEnsayos = nc * repeticiones;
    }

    void Start()
    {
        if (empezarAlArrancar) IniciarPrueba();     // sim empezarAlArracnar = true, se inicia la prueba al arrancar el juego
    }

    public void IniciarPrueba()         // funcion de control para iniciar la prueba sin entrar a la corrutina, para que no se pueda iniciar dos veces la prueba a la vez
    {
        StartCoroutine(EjecutarPrueba());           // Empieza la corrutina que ejecuta la prueba que depende del tiempo
        if (enCurso)
        {
            Debug.LogWarning("La prueba ya está en curso.");
            return;
        }
    }

    // ---------- Input (PlayerInput → Invoke Unity Events) ----------
    // Se enlaza a las acciones del Input System (PlayerInput) para que el usuario pueda responder con el mando o teclado
    // Estas acciones se escuchan todo el rato, pero solo se guardan si esperandoRespuesta = true, es decir, cuando se esta en un ensayo y se esta esperando la respuesta del usuario

    // Enlazar a una acción "FrontLeft" (Button)
    public void FrontLeft(InputAction.CallbackContext ctx)      // Input action (IA), player input, invoke unity events
    {
        if (ctx.performed && esperandoRespuesta)    // esperandoRespuesta = true, se esta esperando la respuesta del user
        {
            esperandoRespuesta = false;         // ya se ha guardado una respuesta, asi solo se guarda lo primero que pulse el user
            respuesta = 0;                      // elección para la función psicométrica
        }
    }

    // Enlazar a una acción "FrontRight" (Button)
    public void FrontRight(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && esperandoRespuesta)    //performed = cuando la accion se ha realizado, started = cuando se ha empezado a realizar, canceled = cuando se ha cancelado         
        {
            esperandoRespuesta = false;
            respuesta = 1;
        }
    }

    public void IniciarPruebaInput(InputAction.CallbackContext ctx)     // por si se quiere que la prueba no empiece instantaneo
    {
        if (ctx.performed) IniciarPrueba();
    }

    // ---------- Desarrollo de la prueba ----------

    private IEnumerator EjecutarPrueba()            //corrutina, aka funciones con tiempos controlables
    {
        enCurso = true;

        
        yield return null;      // Se espera un frame para que se cree la trenza y se pueda acceder a sus pelotas. Si no, da error de null reference

        GenerarEnsayos();           // crea la lista de ensayos a partir de las compensaciones y repeticiones de forma aleatoria
        if (ensayos.Count == 0)
        {
            Debug.LogError("PruebaPulfrich2AFC: no hay ensayos. Pon al menos una compensación.");
            enCurso = false;
            yield break;            // yield break sale de la corrutina, como return pero para corrutinas
        }

        CrearCsv();                 // se crea el archivo CSV para guardar los datos de la prueba del participante
        Filtro.controlPorTeclado = false;     // no se puede cambiar la densidad optica con el teclado durante la prueba
        Estimulo.permitirAjuste = false;                   // el participante no ajusta, solo responde
        Filtro.AplicarDensidadOptica(densidadOptica);        // Se aplica la OD en el VDF, R +, L -

        Debug.Log($"Prueba 2AFC iniciada: {ensayos.Count} ensayos. Datos en: {rutaCsv}");

        for (int i = 0; i < ensayos.Count; i++)
        {
            Estimulo.MostrarPelotas(false);       // Se oculta la helice entre repeticiones
            if (pausaEntreEnsayos > 0f)
                yield return new WaitForSeconds(pausaEntreEnsayos);     // A los pausaEntreEnsayos segundos se muestra la hélice con la compensación de este ensayo

            Estimulo.EstablecerCompensacion(ensayos[i]);          // Compensacion positiva, resultado = 1 front right,
            Estimulo.MostrarPelotas(true);                    // Aquí es donde se muestran estimulo con la compensacion

            respuesta = -1;                 // variable de control, mientras este en -1 se esta esperando la respuesta del usuario, cuando el user pulse left o right se cambia a 0 o 1 y se guarda la respuesta
            esperandoRespuesta = true;      // variable de control tmb muy asociada a la respuesta, se pone a false cuando el user responde, para que no se pueda responder mas de una vez por repeticion
            Debug.Log($"Repetición {i + 1}/{ensayos.Count} | compensación {ensayos[i]}");

            float tInicio = Time.time;          // guardamos tiempo de inicio de la repeticion 
            while (respuesta == -1)             // mientras no haya respondido
            {
                if (duracionEstimulo > 0f && Time.time - tInicio >= duracionEstimulo)   // y si la duracionEstimulo es mayor a 0 y el tiempo que lleva el usuario en la repeticion es mayor a dicho numero, se oculta la trenza
                    Estimulo.MostrarPelotas(false);
                yield return null;                      
            }
            float tiempo = Time.time - tInicio;     // guardamos el tiempo que ha tardado el usuario en responder desde que se le muestra la trenza

            // Se guarda la compensación que aplicó la trenza de verdad (por si la limitó)
            GuardarFila(i + 1, Estimulo.compensacionProfundidad, respuesta, tiempo);  // se guarda la repeticion en el csv fila a fila
        }

        // Fin de la prueba
        Estimulo.MostrarPelotas(false);             // se oculta la trenza al final de la prueba
        Filtro.AplicarDensidadOptica(0f);           // se aplica densidad neutra
        Filtro.controlPorTeclado = true;            // se vuelve a permitir al experimentador controlar el filtro con el teclado
        enCurso = false;                            // ya no esta enCurso
        Debug.Log($"Prueba terminada. Datos guardados en: {rutaCsv}");
    }

    private void GenerarEnsayos()
    {
        ensayos.Clear();            // se vacia la lista de ensayos anteriores, para que no se acumulen si se vuelve a iniciar la prueba

        for (int r = 0; r < repeticiones; r++)              //aqui antes era reps en vez de repeticiones
            foreach (float c in compensaciones)
                ensayos.Add(c);

        if (aleatorizarOrden)
        {
            // Fisher-Yates
            for (int i = ensayos.Count - 1; i > 0; i--)     // se recorre la lista de ensayos desde el final hasta el principio y se va aleatorizando el orden
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (ensayos[i], ensayos[j]) = (ensayos[j], ensayos[i]);
            }
        }
    }

    // ---------- CSV ----------

    // Formato de Excel en español: columnas separadas por ";" y decimales con ","
    private const string SEPARADOR = ";";
    private static readonly NumberFormatInfo formatoNumeros =
        new NumberFormatInfo { NumberDecimalSeparator = "," };

    private void CrearCsv()
    {
        string nombre = NombreCarpetaSeguro(idParticipante);
        // Resultados/2AFC/<participante>/
        string carpeta = Path.Combine(CarpetaResultados(), "2AFC", nombre);     // antes era sin "2AFC", esto crea la carpeta 2afc si no esta y si no lo mete alli
        Directory.CreateDirectory(carpeta);

        string fecha = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        rutaCsv = Path.Combine(carpeta, $"{nombre}_2AFC_{fecha}.csv");

        string cabecera = string.Join(SEPARADOR,
            "ensayo", "densidad_optica", "compensacion", "respuesta", "tiempo_s", "prefab", "pares_pelotas", "altura_estimulo", "amplitud_onda", "frecuencia_onda", "velocidad_onda");

        // UTF-8 con BOM para que Excel lea bien tildes y eñes
        File.WriteAllText(rutaCsv, cabecera + "\n", Encoding.UTF8);
    }

    private void GuardarFila(int ensayo, float compensacion, int resp, float tiempo)
    {
        bool primeraFila = ensayo == 1;         // Solo en la primera fila se guardan los parámetros del estímulo, para no repetirlos en cada ensayo
        string fila = string.Join(SEPARADOR,
            ensayo.ToString(),
            densidadOptica.ToString(formatoNumeros),
            compensacion.ToString("F4", formatoNumeros),
            resp.ToString(),                             // 1 = front right, 0 = front left
            tiempo.ToString("F3", formatoNumeros),      // segundos, con 3 decimales (ms)
            ensayo == 1 ? Estimulo.prefabPelota.name : "",   // nº de pelotas: solo en la primera fila
            primeraFila ? Estimulo.paresDePelotas.ToString() : "",      // es lo mismo ensayo==1 que primeraFIla
            primeraFila ? Estimulo.alturaColumna.ToString() : "",
            primeraFila ? Estimulo.amplitudOnda.ToString() : "",
            primeraFila ? Estimulo.frecuenciaOnda.ToString() : "",
            primeraFila ? Estimulo.velocidadOnda.ToString() : "");

        // Repeticion a repetición, para no perder datos si algo falla a mitad
        File.AppendAllText(rutaCsv, fila + "\n", Encoding.UTF8);
    }

    private static string CarpetaResultados()
    {
#if UNITY_EDITOR
        // En el editor: raíz del proyecto, al lado de Assets
        string raiz = Directory.GetParent(Application.dataPath).FullName;
#elif UNITY_ANDROID
        string raiz = Application.persistentDataPath;
#else
        // En una build de PC: la carpeta donde está el .exe
        string raiz = Directory.GetParent(Application.dataPath).FullName;
#endif
        return Path.Combine(raiz, "Resultados");
    }

    // Quita caracteres que no se pueden usar en nombres de carpeta (/, :, *, ?...)
    private static string NombreCarpetaSeguro(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "SinNombre";

        foreach (char c in Path.GetInvalidFileNameChars())
            nombre = nombre.Replace(c, '_');

        return nombre.Trim();
    }
}
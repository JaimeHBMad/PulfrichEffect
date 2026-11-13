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
public class PruebaPulfrich2AFC_new : MonoBehaviour
{
    [Header("Referencias")]
    public PulfrichControllerNew controladorFiltro;         // Filtro VDF
    public PulfrichTrenzaDepthNew trenza;                   // Estimulo, helice

    [Header("Condiciones (ensayos = compensaciones × repeticiones)")]
    [Tooltip("Densidad óptica CON SIGNO para toda la prueba. Negativa = filtro en ojo izquierdo, positiva = ojo derecho, 0 = sin filtro.")]
    public float densidadOptica = 0.2f;         // OD del fitler

    [Tooltip("Compensaciones Z fijas que se presentan (los 'saltos').")]
    public float[] compensaciones = { -0.2f, -0.1f, 0f, 0.1f, 0.2f };           // Lista de compensaciones

    [Min(1)]
    [Tooltip("Veces que se presenta cada compensación.")]
    public int repeticiones = 20;                               // Veces que se repite cada compensacion

    [Tooltip("Solo informativo: se recalcula solo.")]
    [SerializeField] private int totalEnsayos;                  // Solo informativo, se calculo automaticamente: total = comepnsacion * repeticiones

    [Header("Desarrollo")]
    public bool aleatorizarOrden = true;
    public bool empezarAlArrancar = true;

    [Min(0f)]
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
        //int nc = compensaciones != null ? compensaciones.Length : 0;
        int nc = compensaciones.Length; // numero de compensaciones
        totalEnsayos = nc * Mathf.Max(1, repeticiones);
    }

    void Start()
    {
        if (empezarAlArrancar) IniciarPrueba();     // sim empezarAlArracnar = true, se inicia la prueba al arrancar el juego
    }

    // ---------- Arranque ----------

    [ContextMenu("Iniciar prueba")]             // opción a los 3 puntos del script en el inspector
    public void IniciarPrueba()
    {
        if (enCurso)
        {
            Debug.LogWarning("La prueba ya está en curso.");
            return;
        }
        if (controladorFiltro == null || trenza == null)
        {
            Debug.LogError("PruebaPulfrich2AFC: asigna el PulfrichControllerNew y la PulfrichTrenzaDepthNew en el inspector.");
            return;
        }
        StartCoroutine(EjecutarPrueba());           // Empieza la corrutina que ejecuta la prueba que depende del tiempo
    }

    // ---------- Input (PlayerInput → Invoke Unity Events) ----------

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
        if (ctx.performed && esperandoRespuesta)            
        {
            esperandoRespuesta = false;
            respuesta = 1;
        }
    }

    // Opcional: para que el experimentador lance la prueba
    public void IniciarPruebaInput(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) IniciarPrueba();
    }

    // ---------- Desarrollo de la prueba ----------

    private IEnumerator EjecutarPrueba()
    {
        enCurso = true;

        // Un frame para que el Start() de la trenza haya creado las pelotas
        yield return null;

        GenerarEnsayos();
        if (ensayos.Count == 0)
        {
            Debug.LogError("PruebaPulfrich2AFC: no hay ensayos. Pon al menos una compensación.");
            enCurso = false;
            yield break;
        }

        CrearCsv();
        controladorFiltro.controlPorTeclado = false;     // nadie toca el filtro durante la prueba
        trenza.permitirAjuste = false;                   // el participante no ajusta, solo responde
        controladorFiltro.AplicarDensidadOptica(densidadOptica);

        Debug.Log($"Prueba 2AFC iniciada: {ensayos.Count} ensayos. Datos en: {rutaCsv}");

        for (int i = 0; i < ensayos.Count; i++)
        {
            // 1. Pausa con la hélice oculta
            trenza.MostrarPelotas(false);
            if (pausaEntreEnsayos > 0f)
                yield return new WaitForSeconds(pausaEntreEnsayos);

            // 2. Hélice visible con la compensación de este ensayo
            trenza.EstablecerCompensacion(ensayos[i]);          // Compensacion positiva, resultado = 1 front right
            trenza.MostrarPelotas(true);

            respuesta = -1;
            esperandoRespuesta = true;
            Debug.Log($"Ensayo {i + 1}/{ensayos.Count} | compensación {ensayos[i]}");

            // 3. Esperar a que diga left o right --- VERSION 1
            //yield return new WaitUntil(() => respuesta != -1);
            //    Pasada la duración del estímulo la trenza se oculta, pero se sigue esperando la respuesta.
            float tInicio = Time.time;
            while (respuesta == -1)
            {
                if (duracionEstimulo > 0f && Time.time - tInicio >= duracionEstimulo)
                    trenza.MostrarPelotas(false);
                yield return null;
            }


            //New con tiempo

            // Tiempo desde que aparece la trenza hasta que responde (s)
            float tiempo = Time.time - tInicio;

            // Se guarda la compensación que aplicó la trenza de verdad (por si la limitó)
            GuardarFila(i + 1, trenza.compensacionProfundidad, respuesta, tiempo);
        }

        // Fin: hélice oculta, sin filtro y teclado del experimentador otra vez activo
        trenza.MostrarPelotas(false);
        controladorFiltro.AplicarDensidadOptica(0f);
        controladorFiltro.controlPorTeclado = true;
        enCurso = false;

        Debug.Log($"Prueba terminada. Datos guardados en: {rutaCsv}");
    }

    private void GenerarEnsayos()
    {
        ensayos.Clear();
        if (compensaciones == null) return;

        int reps = Mathf.Max(1, repeticiones);
        for (int r = 0; r < reps; r++)
            foreach (float c in compensaciones)
                ensayos.Add(c);

        if (aleatorizarOrden)
        {
            // Fisher-Yates
            for (int i = ensayos.Count - 1; i > 0; i--)
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
            "ensayo", "densidad_optica", "compensacion", "respuesta", "tiempo_s", "numero_pelotas");

        // UTF-8 con BOM para que Excel lea bien tildes y eñes
        File.WriteAllText(rutaCsv, cabecera + "\n", Encoding.UTF8);
    }

    private void GuardarFila(int ensayo, float compensacion, int resp, float tiempo)
    {
        string fila = string.Join(SEPARADOR,
            ensayo.ToString(),
            densidadOptica.ToString(formatoNumeros),
            compensacion.ToString("F4", formatoNumeros),
            resp.ToString(),                             // 1 = front right, 0 = front left
            tiempo.ToString("F3", formatoNumeros),      // segundos, con 3 decimales (ms)
            ensayo == 1 ? trenza.paresDePelotas.ToString() : "");   // nº de pelotas: solo en la primera fila

        // Ensayo a ensayo, para no perder datos si algo falla a mitad
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
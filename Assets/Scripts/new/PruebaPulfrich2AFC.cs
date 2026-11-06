using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Prueba Pulfrich por ESTÍMULOS CONSTANTES con elección forzada (2AFC).
///
/// En cada ensayo la hélice aparece con un nivel FIJO de compensación (el
/// participante ya no ajusta nada) y solo responde si la ve girar
/// "delante a la izquierda" (front left) o "delante a la derecha" (front right).
/// Con muchas repeticiones por nivel se obtiene la proporción de "front right"
/// para cada nivel y, ajustando una curva psicométrica, el PSE de cada densidad.
///
/// Ensayos = densidades × niveles × repeticiones, todos mezclados al azar.
///
/// IMPORTANTE:
///  - Ponlo en un objeto distinto de la hélice.
///  - Desactiva el componente PruebaPulfrich (el de ajuste) para que no arranquen los dos.
/// </summary>
public class PruebaPulfrich2AFC : MonoBehaviour
{
    [Header("Referencias")]
    public PulfrichControllerNew controladorFiltro;
    public PulfrichTrenzaDepthNew trenza;

    [Tooltip("Cámara del casco (la Main Camera del XR Origin). Si se deja vacía se usa Camera.main. Sirve para medir la distancia a la hélice.")]
    public Transform camaraParticipante;

    [Header("Condiciones (ensayos = densidades × niveles × repeticiones)")]
    [Tooltip("Densidad óptica CON SIGNO. Negativa = filtro en ojo izquierdo, positiva = ojo derecho, 0 = sin filtro. Transmitancia = 10^(-|DO|).")]
    public float[] densidadesOpticas = { -0.2f, 0f, 0.2f };

    [Tooltip("Activado: los niveles son ms de retardo interocular equivalente y el script calcula la compensación Z de cada ensayo. Desactivado: los niveles son directamente compensaciones Z.")]
    public bool nivelesEnMs = true;

    [Tooltip("Niveles fijos del estímulo (el eje X de la curva). Tienen que quedar a ambos lados del PSE de TODAS las densidades.")]
    public float[] niveles = { -8f, -6f, -4f, -2f, 0f, 2f, 4f, 6f, 8f };

    [Min(1)]
    [Tooltip("Repeticiones de cada combinación densidad × nivel. Con 9 niveles y 20 repeticiones salen 180 ensayos por curva.")]
    public int repeticionesPorNivel = 20;

    [Tooltip("Solo informativo: ensayos que tendrá cada curva (niveles × repeticiones).")]
    [SerializeField] private int ensayosPorCurva;

    [Tooltip("Solo informativo: ensayos de la prueba completa.")]
    [SerializeField] private int totalEnsayos;

    [Header("Conversión a milisegundos")]
    [Tooltip("Intenta leer la distancia interpupilar del visor. Si no se puede, usa la de abajo.")]
    public bool leerIpdDelVisor = true;

    [Tooltip("Distancia interpupilar en metros. Si la mides al participante, ponla aquí y desmarca la casilla de arriba.")]
    public float ipdPorDefecto = 0.063f;

    [Header("Desarrollo")]
    public bool aleatorizarOrden = true;
    public bool empezarAlArrancar = true;

    [Min(0f)]
    [Tooltip("Segundos con la hélice oculta antes de cada ensayo. El filtro ya está puesto para que el ojo se adapte.")]
    public float pausaEntreEnsayos = 1f;

    [Min(0f)]
    [Tooltip("Segundos que se ve la hélice. Se puede responder desde que aparece. 0 = visible hasta que responda.")]
    public float duracionEstimulo = 1.5f;

    [Min(0)]
    [Tooltip("Descanso cada N ensayos. Se sigue con la acción Continuar. 0 = sin descansos.")]
    public int descansoCada = 90;

    [Tooltip("Flecha izquierda = front left, flecha derecha = front right, espacio = continuar. Útil para probar en el editor.")]
    public bool teclasDeRespaldo = true;

    [Header("Registro")]
    public string idParticipante = "P01";

    private struct Ensayo
    {
        public float densidad;
        public float nivel;
    }

    private readonly List<Ensayo> ensayos = new List<Ensayo>();
    private bool enCurso;

    // Respuesta del ensayo actual
    private bool esperandoRespuesta;
    private bool respondido;
    private bool respuestaFrontRight;
    private double tRespuesta;

    // Descansos
    private bool esperandoContinuar;
    private bool continuar;

    private string rutaCsv;

    void OnValidate()
    {
        int nd = densidadesOpticas != null ? densidadesOpticas.Length : 0;
        int nn = niveles != null ? niveles.Length : 0;
        ensayosPorCurva = nn * Mathf.Max(1, repeticionesPorNivel);
        totalEnsayos = nd * ensayosPorCurva;
    }

    void Start()
    {
        if (empezarAlArrancar) IniciarPrueba();
    }

    void Update()
    {
        // Respaldo con teclado (no sustituye a los mandos, es para probar)
        if (!teclasDeRespaldo || Keyboard.current == null) return;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) Responder(false);
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) Responder(true);
        if (Keyboard.current.spaceKey.wasPressedThisFrame) PulsarContinuar();
    }

    // ---------- Arranque ----------

    [ContextMenu("Iniciar prueba")]
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
        if (nivelesEnMs)
        {
            if (CamaraActual() == null)
            {
                Debug.LogError("PruebaPulfrich2AFC: con 'nivelesEnMs' hace falta la cámara del casco para medir la distancia. Asígnala en el inspector o etiquétala como MainCamera.");
                return;
            }
            if (float.IsNaN(MsPorUnidadDeCompensacion(ipdPorDefecto, 1f)))
            {
                Debug.LogError("PruebaPulfrich2AFC: no se puede pasar de ms a compensación porque la amplitud o la velocidad de la hélice es 0.");
                return;
            }
        }
        StartCoroutine(EjecutarPrueba());
    }

    // ---------- Input (PlayerInput → Invoke Unity Events) ----------

    // Enlazar a la acción "FrontLeft" (Button)
    public void FrontLeft(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Responder(false);
    }

    // Enlazar a la acción "FrontRight" (Button)
    public void FrontRight(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Responder(true);
    }

    // Enlazar a la acción "Continuar" (Button): sale de los descansos
    public void Continuar(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) PulsarContinuar();
    }

    // Opcional: para que el experimentador lance la prueba
    public void IniciarPruebaInput(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) IniciarPrueba();
    }

    private void Responder(bool frontRight)
    {
        // Solo cuenta la primera pulsación de cada ensayo, y solo con la hélice ya presentada
        if (!esperandoRespuesta) return;

        esperandoRespuesta = false;
        respuestaFrontRight = frontRight;
        tRespuesta = Time.realtimeSinceStartupAsDouble;
        respondido = true;
    }

    private void PulsarContinuar()
    {
        if (esperandoContinuar) continuar = true;
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
            Debug.LogError("PruebaPulfrich2AFC: no hay ensayos. Revisa que las dos listas tengan al menos un valor.");
            enCurso = false;
            yield break;
        }

        CrearCsv();
        controladorFiltro.controlPorTeclado = false;
        trenza.permitirAjuste = false;          // En 2AFC el participante no ajusta nada
        trenza.MostrarPelotas(false);

        float ipd = ObtenerIpd();
        int porCurva = niveles.Length * Mathf.Max(1, repeticionesPorNivel);
        Debug.Log($"Prueba 2AFC iniciada: {ensayos.Count} ensayos ({porCurva} por curva). IPD = {ipd * 1000f:F1} mm. Datos en: {rutaCsv}");

        for (int i = 0; i < ensayos.Count; i++)
        {
            if (descansoCada > 0 && i > 0 && i % descansoCada == 0)
                yield return StartCoroutine(Descanso(i));

            Ensayo e = ensayos[i];

            // 1. Pausa: hélice oculta y filtro de este ensayo ya puesto (adaptación)
            trenza.MostrarPelotas(false);
            controladorFiltro.AplicarDensidadOptica(e.densidad);

            if (pausaEntreEnsayos > 0f)
                yield return new WaitForSeconds(pausaEntreEnsayos);

            // 2. Compensación FIJA de este ensayo.
            //    La distancia se mide justo ahora porque la cabeza se mueve entre ensayos.
            float distancia = ObtenerDistancia();
            float msPorUnidad = MsPorUnidadDeCompensacion(ipd, distancia);
            float compPedida = nivelesEnMs ? e.nivel / msPorUnidad : e.nivel;

            trenza.EstablecerCompensacion(compPedida);
            float comp = trenza.compensacionProfundidad;     // la que se aplica de verdad (tras limitar y redondear)
            float retardoMs = comp * msPorUnidad;

            if (Mathf.Abs(comp - compPedida) > 0.001f)
                Debug.LogWarning($"Ensayo {i + 1}: se pidió compensación {compPedida:F4} pero la trenza la limitó a {comp:F4}. Sube 'limiteCompensacion' o reduce los niveles.");

            // 3. Estímulo: hélice visible; se puede responder desde ya
            respondido = false;
            esperandoRespuesta = true;
            trenza.MostrarPelotas(true);
            bool visible = true;
            double tInicio = Time.realtimeSinceStartupAsDouble;

            Debug.Log($"Ensayo {i + 1}/{ensayos.Count} | DO {e.densidad} | nivel {e.nivel} | comp {comp:F4} | {retardoMs:F2} ms");

            while (!respondido)
            {
                // Pasado el tiempo de presentación se oculta, pero se sigue esperando la respuesta
                if (visible && duracionEstimulo > 0f && Time.realtimeSinceStartupAsDouble - tInicio >= duracionEstimulo)
                {
                    trenza.MostrarPelotas(false);
                    visible = false;
                }
                yield return null;
            }

            trenza.MostrarPelotas(false);
            GuardarFila(i + 1, e, comp, retardoMs, respuestaFrontRight,
                        (float)(tRespuesta - tInicio), distancia, ipd);
        }

        // Fin: hélice oculta, sin filtro y teclado del experimentador otra vez activo
        trenza.MostrarPelotas(false);
        controladorFiltro.AplicarDensidadOptica(0f);
        controladorFiltro.controlPorTeclado = true;
        enCurso = false;

        Debug.Log($"Prueba terminada. Datos guardados en: {rutaCsv}");
    }

    private IEnumerator Descanso(int hechos)
    {
        // Sin filtro durante el descanso; la pausa del siguiente ensayo vuelve a adaptar el ojo
        trenza.MostrarPelotas(false);
        controladorFiltro.AplicarDensidadOptica(0f);

        continuar = false;
        esperandoContinuar = true;
        Debug.Log($"Descanso ({hechos}/{ensayos.Count} ensayos hechos). Pulsa Continuar para seguir.");

        yield return new WaitUntil(() => continuar);
        esperandoContinuar = false;
    }

    private void GenerarEnsayos()
    {
        ensayos.Clear();
        if (densidadesOpticas == null || niveles == null) return;

        int reps = Mathf.Max(1, repeticionesPorNivel);
        for (int r = 0; r < reps; r++)
            foreach (float d in densidadesOpticas)
                foreach (float n in niveles)
                    ensayos.Add(new Ensayo { densidad = d, nivel = n });

        if (aleatorizarOrden)
        {
            // Fisher-Yates: todas las densidades y niveles mezclados
            for (int i = ensayos.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (ensayos[i], ensayos[j]) = (ensayos[j], ensayos[i]);
            }
        }
    }

    // ---------- Geometría: de compensación Z a ms ----------

    // En la trenza:  x = A·sin(fase),  z = comp·cos(fase),  fase = y·frecuenciaOnda − t·velocidadOnda
    // (así que velocidadOnda es la frecuencia angular ω en rad/s).
    //
    // Un retardo Δt en un ojo hace que ese ojo vea la pelota donde estaba hace Δt:
    // una disparidad horizontal v·Δt, proporcional a la velocidad. Esa disparidad
    // equivale a una profundidad  z ≈ D·v·Δt / IPD  (D = distancia a la hélice).
    // Como v = −A·ω·cos(fase) y la compensación también va con cos(fase), una
    // compensación 'comp' equivale a un retardo de:
    //
    //      Δt = comp · IPD / (D · A · ω)
    //
    // Es la aproximación habitual para profundidades pequeñas frente a D.
    // El signo es un convenio: retardo positivo = compensación positiva.
    private float MsPorUnidadDeCompensacion(float ipd, float distancia)
    {
        Vector3 escala = trenza.transform.lossyScale;
        float amplitudMundo = trenza.amplitudOnda * escala.x;   // en metros
        float omega = trenza.velocidadOnda;                     // rad/s
        float denominador = distancia * amplitudMundo * omega;

        if (Mathf.Abs(denominador) < 1e-6f || float.IsNaN(denominador)) return float.NaN;

        // escala.z pasa la compensación (unidades locales) a metros
        return 1000f * escala.z * ipd / denominador;
    }

    private Transform CamaraActual()
    {
        if (camaraParticipante != null) return camaraParticipante;
        return Camera.main != null ? Camera.main.transform : null;
    }

    // Distancia (m) de la cámara al centro de la columna de pelotas
    private float ObtenerDistancia()
    {
        Transform cam = CamaraActual();
        if (cam == null) return float.NaN;

        Vector3 centro = trenza.transform.TransformPoint(new Vector3(0f, trenza.alturaColumna * 0.5f, 0f));
        return Vector3.Distance(cam.position, centro);
    }

    private float ObtenerIpd()
    {
        if (!leerIpdDelVisor) return ipdPorDefecto;

        // Nombres completos: UnityEngine.InputSystem también tiene un CommonUsages
        var casco = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
        if (casco.isValid &&
            casco.TryGetFeatureValue(UnityEngine.XR.CommonUsages.leftEyePosition, out Vector3 ojoIzq) &&
            casco.TryGetFeatureValue(UnityEngine.XR.CommonUsages.rightEyePosition, out Vector3 ojoDer))
        {
            float ipd = Vector3.Distance(ojoIzq, ojoDer);
            if (ipd > 0.045f && ipd < 0.085f) return ipd;   // fuera de ese rango la lectura no es fiable

            Debug.LogWarning($"IPD leída del visor fuera de rango ({ipd * 1000f:F1} mm). Se usa ipdPorDefecto.");
            return ipdPorDefecto;
        }

        Debug.LogWarning("No se pudo leer la IPD del visor. Se usa ipdPorDefecto.");
        return ipdPorDefecto;
    }

    // ---------- CSV ----------

    // Formato de Excel en español: columnas separadas por ";" y decimales con ","
    private const string SEPARADOR = ";";
    private static readonly NumberFormatInfo formatoNumeros =
        new NumberFormatInfo { NumberDecimalSeparator = "," };

    private void CrearCsv()
    {
        // Resultados/<participante>/<participante>_2AFC_<fecha>.csv
        string nombre = NombreCarpetaSeguro(idParticipante);
        string carpeta = Path.Combine(CarpetaResultados(), nombre);
        Directory.CreateDirectory(carpeta);

        string fecha = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        rutaCsv = Path.Combine(carpeta, $"{nombre}_2AFC_{fecha}.csv");

        string cabecera = string.Join(SEPARADOR,
            "participante", "ensayo", "densidad_optica", "ojo_filtro", "transmitancia",
            "nivel", "unidad_nivel", "comp_z", "retardo_ms",
            "respuesta", "front_right", "tiempo_reaccion_s",
            "distancia_m", "ipd_m", "duracion_estimulo_s",
            // Parámetros de la hélice
            "pares_pelotas", "altura_columna", "amplitud_onda",
            "frecuencia_onda", "velocidad_onda", "escala_x", "escala_z");

        // UTF-8 con BOM para que Excel lea bien tildes y eñes
        File.WriteAllText(rutaCsv, cabecera + "\n", Encoding.UTF8);
    }

    private void GuardarFila(int numero, Ensayo e, float comp, float retardoMs, bool frontRight,
                             float tiempoReaccion, float distancia, float ipd)
    {
        string ojo = e.densidad < 0f ? "izquierdo" : (e.densidad > 0f ? "derecho" : "ninguno");
        float transmitancia = PulfrichControllerNew.TransmitanciaDesdeDensidad(e.densidad);
        Vector3 escala = trenza.transform.lossyScale;

        string fila = string.Join(SEPARADOR,
            idParticipante,
            numero.ToString(),
            e.densidad.ToString(formatoNumeros),
            ojo,
            transmitancia.ToString("F4", formatoNumeros),
            e.nivel.ToString(formatoNumeros),
            nivelesEnMs ? "ms" : "comp_z",
            comp.ToString("F4", formatoNumeros),
            retardoMs.ToString("F4", formatoNumeros),
            frontRight ? "front_right" : "front_left",
            frontRight ? "1" : "0",
            tiempoReaccion.ToString("F3", formatoNumeros),
            distancia.ToString("F4", formatoNumeros),
            ipd.ToString("F4", formatoNumeros),
            duracionEstimulo.ToString(formatoNumeros),
            trenza.paresDePelotas.ToString(formatoNumeros),
            trenza.alturaColumna.ToString(formatoNumeros),
            trenza.amplitudOnda.ToString(formatoNumeros),
            trenza.frecuenciaOnda.ToString(formatoNumeros),
            trenza.velocidadOnda.ToString(formatoNumeros),
            escala.x.ToString(formatoNumeros),
            escala.z.ToString(formatoNumeros));

        // Ensayo a ensayo, para no perder datos si algo falla a mitad
        File.AppendAllText(rutaCsv, fila + "\n", Encoding.UTF8);
    }

    private static string CarpetaResultados()
    {
#if UNITY_EDITOR
        // En el editor: raíz del proyecto, al lado de Assets (fuera para que Unity no importe los CSV)
        string raiz = Directory.GetParent(Application.dataPath).FullName;
#elif UNITY_ANDROID
        // En Quest standalone no se puede escribir junto a la app
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

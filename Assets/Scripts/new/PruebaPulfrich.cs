using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gestiona la prueba Pulfrich: genera todas las combinaciones
/// (densidad óptica × compensación Z inicial × repeticiones), las presenta
/// en orden aleatorio y guarda la respuesta de cada ensayo en un CSV.
/// IMPORTANTE: ponlo en un objeto distinto de la hélice.
/// </summary>
/// 
public class PruebaPulfrich : MonoBehaviour
{
    [Header("Referencias")]
    public PulfrichControllerNew controladorFiltro;
    public PulfrichTrenzaDepthNew trenza;

    [Header("Condiciones (ensayos = densidades × compensaciones × repeticiones)")]
    [Tooltip("Densidad óptica CON SIGNO. Negativa = filtro en ojo izquierdo, positiva = ojo derecho, 0 = sin filtro. Transmitancia = 10^(-|DO|).")]
    public float[] densidadesOpticas = { -0.2f, 0f, 0.2f };

    [Tooltip("Compensación en Z con la que arranca cada ensayo. El participante la ajusta desde ahí hasta anular el efecto.")]
    public float[] compensacionesZ = { -1f, 0f, 1f };

    [Min(1)]
    [Tooltip("Cuántas veces se repite cada combinación. Con 1, total = densidades × compensaciones.")]
    public int repeticionesPorCombinacion = 1;

    [Tooltip("Solo informativo: se recalcula solo.")]
    [SerializeField] private int totalEnsayos;

    [Header("Desarrollo")]
    public bool aleatorizarOrden = true;
    public bool empezarAlArrancar = true;

    [Min(0f)]
    [Tooltip("Segundos con la hélice oculta antes de cada ensayo. El filtro ya está puesto durante la pausa para que el ojo se adapte.")]
    public float pausaEntreEnsayos = 1.5f;

    [Header("Registro")]
    public string idParticipante = "P01";

    private struct Ensayo
    {
        public float densidad;
        public float compInicial;
    }

    private readonly List<Ensayo> ensayos = new List<Ensayo>();
    private bool enCurso;
    private bool esperandoRespuesta;
    private bool confirmado;
    private string rutaCsv;

    void OnValidate()
    {
        int nd = densidadesOpticas != null ? densidadesOpticas.Length : 0;
        int nz = compensacionesZ != null ? compensacionesZ.Length : 0;
        totalEnsayos = nd * nz * Mathf.Max(1, repeticionesPorCombinacion);
    }

    void Start()
    {
        if (empezarAlArrancar) IniciarPrueba();
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
            Debug.LogError("PruebaPulfrich: asigna el PulfrichController y la PulfrichTrenzaDepth en el inspector.");
            return;
        }
        StartCoroutine(EjecutarPrueba());
    }

    // ---------- Input (PlayerInput → Invoke Unity Events) ----------

    // Enlazar a una acción "Confirmar" (Button)
    public void Confirmar(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && esperandoRespuesta) confirmado = true;
    }

    // Opcional: enlazar a una acción para que el experimentador lance la prueba
    public void IniciarPruebaInput(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) IniciarPrueba();
    }

    // ---------- Desarrollo de la prueba ----------

    private IEnumerator EjecutarPrueba()
    {
        enCurso = true;

        // Esperamos un frame para asegurar que el Start() de la trenza ya ha creado las pelotas
        yield return null; 

        GenerarEnsayos();
        if (ensayos.Count == 0)
        {
            Debug.LogError("PruebaPulfrich: no hay ensayos. Revisa que las dos listas tengan al menos un valor.");
            enCurso = false;
            yield break;
        }

        CrearCsv();
        controladorFiltro.controlPorTeclado = false;
        trenza.permitirAjuste = false;

        Debug.Log($"Prueba iniciada: {ensayos.Count} ensayos. Datos en: {rutaCsv}");

        for (int i = 0; i < ensayos.Count; i++)
        {
            Ensayo e = ensayos[i];

            // 1. Pausa: hélice oculta y filtro ya puesto (adaptación)
            trenza.MostrarPelotas(false);
            trenza.permitirAjuste = false;
            controladorFiltro.AplicarDensidadOptica(e.densidad);

            if (pausaEntreEnsayos > 0f)
                yield return new WaitForSeconds(pausaEntreEnsayos);

            // 2. Estímulo: hélice visible con la compensación inicial de esta combinación
            trenza.EstablecerCompensacion(e.compInicial);
            trenza.MostrarPelotas(true);
            trenza.permitirAjuste = true;

            confirmado = false;
            esperandoRespuesta = true;
            float tInicio = Time.time;

            Debug.Log($"Ensayo {i + 1}/{ensayos.Count} | DO {e.densidad} | Z inicial {e.compInicial}");

            // 3. El participante ajusta y confirma
            yield return new WaitUntil(() => confirmado);

            esperandoRespuesta = false;
            GuardarFila(i + 1, e, trenza.compensacionProfundidad, Time.time - tInicio);
        }

        // Fin: hélice oculta, sin filtro y teclado del experimentador otra vez activo
        trenza.MostrarPelotas(false);
        trenza.permitirAjuste = false;
        controladorFiltro.AplicarDensidadOptica(0f);
        controladorFiltro.controlPorTeclado = true;
        enCurso = false;

        Debug.Log("Prueba terminada.");
    }

    private void GenerarEnsayos()
    {
        ensayos.Clear();
        if (densidadesOpticas == null || compensacionesZ == null) return;

        int reps = Mathf.Max(1, repeticionesPorCombinacion);
        for (int r = 0; r < reps; r++)
            foreach (float d in densidadesOpticas)
                foreach (float z in compensacionesZ)
                    ensayos.Add(new Ensayo { densidad = d, compInicial = z });

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

    private void CrearCsv()
    {
        string carpeta = Path.Combine(Application.persistentDataPath, "ResultadosPulfrich");
        Directory.CreateDirectory(carpeta);

        string fecha = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        rutaCsv = Path.Combine(carpeta, $"{idParticipante}_{fecha}.csv");

        File.WriteAllText(rutaCsv,
            "participante,ensayo,densidad_optica,ojo_filtro,transmitancia,comp_inicial,comp_final,tiempo_s\n");
    }

    private void GuardarFila(int numero, Ensayo e, float compFinal, float tiempo)
    {
        // Cultura invariante: punto decimal aunque el PC esté en español
        CultureInfo ci = CultureInfo.InvariantCulture;

        string ojo = e.densidad < 0f ? "izquierdo" : (e.densidad > 0f ? "derecho" : "ninguno");
        float transmitancia = PulfrichControllerNew.TransmitanciaDesdeDensidad(e.densidad);

        string fila = string.Join(",",
            idParticipante,
            numero.ToString(ci),
            e.densidad.ToString(ci),
            ojo,
            transmitancia.ToString("F4", ci),
            e.compInicial.ToString(ci),
            compFinal.ToString("F4", ci),
            tiempo.ToString("F3", ci));

        // Se escribe ensayo a ensayo para no perder datos si algo falla a mitad
        File.AppendAllText(rutaCsv, fila + "\n");
    }
}

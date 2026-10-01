using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controla el efecto OneEyeBlur (HDRP): qué ojo se desenfoca y con qué intensidad.
/// Ponlo en un GameObject vacío de la escena. Crea su propio Volume global (oculto),
/// así que no hace falta añadir el override a ningún Volume a mano.
/// (Sigue siendo necesario tener OneEyeBlur en "Custom Post Process Orders → After Post Process".)
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class OneEyeBlurController : MonoBehaviour
{
    public enum Ojo { Izquierdo, Derecho, Ambos, Ninguno }

    [Header("Efecto")]
    [Tooltip("Ojo que se verá desenfocado.")]
    [SerializeField] private Ojo ojo = Ojo.Izquierdo;

    [Tooltip("Intensidad del desenfoque: 0 = nítido, 1 = radio máximo.")]
    [Range(0f, 1f)] [SerializeField] private float intensidad = 1f;

    [Tooltip("Radio máximo del desenfoque (fracción del ancho de la imagen de cada ojo).")]
    [Range(0f, 0.05f)] [SerializeField] private float radioMaximo = 0.015f;

    [Header("Transición")]
    [Tooltip("Segundos que tarda en pasar de nítido a desenfoque completo. 0 = instantáneo.")]
    [Min(0f)] [SerializeField] private float duracionTransicion = 0.5f;

    [Header("Volume interno")]
    [Tooltip("Prioridad del Volume que crea este script. Déjala por encima de tus otros Volumes.")]
    [SerializeField] private float prioridad = 100f;

    [Header("Atajos de teclado (solo en Play)")]
    [Tooltip("1 = izquierdo, 2 = derecho, 3 = ambos, 0 = ninguno, ↑/↓ = intensidad, Tab = intercambiar.")]
    [SerializeField] private bool atajosTeclado = true;
    [Range(0.01f, 0.5f)] [SerializeField] private float pasoIntensidad = 0.1f;

    private GameObject volumeObject;
    private Volume volume;
    private VolumeProfile profile;
    private OneEyeBlur blur;
    private float actualIzq;
    private float actualDer;

    // ---------- API pública (código, botones, sliders, dropdowns de UI) ----------

    public Ojo OjoActual
    {
        get => ojo;
        set => ojo = value;
    }

    public float Intensidad
    {
        get => intensidad;
        set => intensidad = Mathf.Clamp01(value);
    }

    public float RadioMaximo
    {
        get => radioMaximo;
        set => radioMaximo = Mathf.Clamp(value, 0f, 0.05f);
    }

    // Para botones de UI (sin parámetros)
    public void DesenfocarIzquierdo() => ojo = Ojo.Izquierdo;
    public void DesenfocarDerecho()   => ojo = Ojo.Derecho;
    public void DesenfocarAmbos()     => ojo = Ojo.Ambos;
    public void QuitarDesenfoque()    => ojo = Ojo.Ninguno;

    // Para un Dropdown de UI: 0 = Izquierdo, 1 = Derecho, 2 = Ambos, 3 = Ninguno
    public void SetOjoPorIndice(int indice) => ojo = (Ojo)Mathf.Clamp(indice, 0, 3);

    // Para un Slider de UI (0 a 1)
    public void SetIntensidad(float valor) => Intensidad = valor;

    /// <summary>Pasa el desenfoque de un ojo al otro (izquierdo ↔ derecho).</summary>
    public void IntercambiarOjos()
    {
        if (ojo == Ojo.Izquierdo) ojo = Ojo.Derecho;
        else if (ojo == Ojo.Derecho) ojo = Ojo.Izquierdo;
    }

    /// <summary>Salta al estado actual sin transición.</summary>
    public void AplicarAlInstante()
    {
        actualIzq = ObjetivoIzq;
        actualDer = ObjetivoDer;
        Aplicar();
    }

    // ---------- Ciclo de vida ----------

    private float ObjetivoIzq => (ojo == Ojo.Izquierdo || ojo == Ojo.Ambos) ? intensidad : 0f;
    private float ObjetivoDer => (ojo == Ojo.Derecho   || ojo == Ojo.Ambos) ? intensidad : 0f;

    private void OnEnable()
    {
        CrearVolume();
        AplicarAlInstante();
    }

    private void OnDisable()
    {
        DestruirVolume();
    }

    private void OnValidate()
    {
        if (volume != null) volume.priority = prioridad;
        if (blur == null) return;

        if (Application.isPlaying) Aplicar();   // en Play, Update hace la transición
        else AplicarAlInstante();               // en el editor, cambio inmediato
    }

    private void Update()
    {
        if (blur == null) return;

        if (!Application.isPlaying)
        {
            AplicarAlInstante();
            return;
        }

        if (atajosTeclado) LeerTeclado();

        // unscaledDeltaTime: la transición funciona aunque el juego esté en pausa (timeScale = 0)
        float paso = duracionTransicion > 0f ? Time.unscaledDeltaTime / duracionTransicion : 1f;
        actualIzq = Mathf.MoveTowards(actualIzq, ObjetivoIzq, paso);
        actualDer = Mathf.MoveTowards(actualDer, ObjetivoDer, paso);
        Aplicar();
    }

    private void Aplicar()
    {
        if (blur == null) return;
        blur.ojoIzquierdo.value = actualIzq;
        blur.ojoDerecho.value   = actualDer;
        blur.radio.value        = radioMaximo;
    }

    // ---------- Volume interno ----------

    private void CrearVolume()
    {
        if (volumeObject != null) return;

        volumeObject = new GameObject("OneEyeBlur Volume (auto)")
        {
            hideFlags = HideFlags.HideAndDontSave,
            layer = gameObject.layer   // debe estar en el Volume Layer Mask de la cámara
        };

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        blur = profile.Add<OneEyeBlur>(true);   // true = activa el override de todos los parámetros

        volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = prioridad;
        volume.weight = 1f;
        volume.sharedProfile = profile;
    }

    private void DestruirVolume()
    {
        if (profile != null)
        {
            foreach (var componente in profile.components)
                Destruir(componente);
            Destruir(profile);
        }
        Destruir(volumeObject);

        volumeObject = null;
        volume = null;
        profile = null;
        blur = null;
    }

    private static void Destruir(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    // ---------- Teclado ----------

    private void LeerTeclado()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame)    ojo = Ojo.Izquierdo;
        if (kb.digit2Key.wasPressedThisFrame)    ojo = Ojo.Derecho;
        if (kb.digit3Key.wasPressedThisFrame)    ojo = Ojo.Ambos;
        if (kb.digit0Key.wasPressedThisFrame)    ojo = Ojo.Ninguno;
        if (kb.upArrowKey.wasPressedThisFrame)   Intensidad += pasoIntensidad;
        if (kb.downArrowKey.wasPressedThisFrame) Intensidad -= pasoIntensidad;
        if (kb.tabKey.wasPressedThisFrame)       IntercambiarOjos();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha1))    ojo = Ojo.Izquierdo;
        if (Input.GetKeyDown(KeyCode.Alpha2))    ojo = Ojo.Derecho;
        if (Input.GetKeyDown(KeyCode.Alpha3))    ojo = Ojo.Ambos;
        if (Input.GetKeyDown(KeyCode.Alpha0))    ojo = Ojo.Ninguno;
        if (Input.GetKeyDown(KeyCode.UpArrow))   Intensidad += pasoIntensidad;
        if (Input.GetKeyDown(KeyCode.DownArrow)) Intensidad -= pasoIntensidad;
        if (Input.GetKeyDown(KeyCode.Tab))       IntercambiarOjos();
#endif
    }
}

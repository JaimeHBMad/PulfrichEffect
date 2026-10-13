using UnityEngine;
using UnityEngine.InputSystem; // VIP, añadir el nuevo Input System

public class PulfrichControllerNew : MonoBehaviour
{
    [Tooltip("Material que tiene el shader Custom/Pulfrich")]
    public Material pulfrichMaterial;

    // En el shader: alpha = (1 - Transmitancia) → 1 = transparente, 0 = negro total
    [Range(0f, 1f)]
    [Tooltip("Fracción de luz que deja pasar el filtro. 1 = transparente, 0 = negro total.")]
    public float transmitancia = 0.8f;

    [Tooltip("1 = filtro en ojo izquierdo (One Minus), 0 = filtro en ojo derecho (ruta directa)")]
    public float eye;

    [Tooltip("Si está activo, las teclas 0/1/2 controlan el filtro. La prueba lo desactiva mientras dura.")]
    public bool controlPorTeclado = true;

    private bool estaActivo = true;

    private static readonly int idTransmitancia = Shader.PropertyToID("_Transmitancia");
    private static readonly int idEyeSide = Shader.PropertyToID("_EyeSide");

    void Start()
    {
        AplicarAlMaterial();
    }

    void Update()
    {
        // Keyboard.current es null si no hay teclado (p. ej. Quest standalone)
        if (controlPorTeclado && Keyboard.current != null)
        {
            // Al pulsar 1, filtro en el ojo izquierdo
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                eye = 1f;
                estaActivo = true;
            }

            // Al pulsar 2, filtro en el ojo derecho
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                eye = 0f;
                estaActivo = true;
            }

            // Al pulsar 0, sin filtro
            if (Keyboard.current.digit0Key.wasPressedThisFrame)
            {
                estaActivo = false;
            }
        }

        AplicarAlMaterial();
    }

    /// <summary>
    /// Densidad óptica con signo: negativa = ojo izquierdo, positiva = ojo derecho, 0 = sin filtro.
    /// </summary>
    public void AplicarDensidadOptica(float densidadConSigno)
    {
        if (Mathf.Approximately(densidadConSigno, 0f))
        {
            estaActivo = false;
        }
        else
        {
            eye = densidadConSigno < 0f ? 1f : 0f;
            transmitancia = TransmitanciaDesdeDensidad(densidadConSigno);
            estaActivo = true;
        }
        AplicarAlMaterial();
    }

    /// <summary> T = 10^(-|DO|). Ej.: DO 0.2 → T ≈ 0.631 </summary>
    public static float TransmitanciaDesdeDensidad(float densidad)
    {
        return Mathf.Pow(10f, -Mathf.Abs(densidad));
    }

    private void AplicarAlMaterial()
    {
        if (pulfrichMaterial == null) return;

        pulfrichMaterial.SetFloat(idEyeSide, eye);
        // Sin filtro = transmitancia 1 (transparente). Con 0 el ojo se quedaría en negro.
        pulfrichMaterial.SetFloat(idTransmitancia, estaActivo ? transmitancia : 1f);
    }

    void OnValidate()
    {
        AplicarAlMaterial();
        //pulfrichMaterial.SetFloat("_Transmitancia", transmitancia);
    
    }
}

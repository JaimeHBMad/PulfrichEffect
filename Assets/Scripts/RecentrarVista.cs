using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Al empezar, lee el giro horizontal (eje Y) de la cámara del casco y gira su padre justo lo contrario,
/// para que la cámara arranque mirando a Y = 0. Los ejes X y Z no se tocan.
/// Después el tracking sigue normal desde ahí.
/// </summary>
public class RecentrarVista : MonoBehaviour
{
    [Tooltip("La cámara del casco (la que tiene el Tracked Pose Driver). Si se deja vacío, se usa la Main Camera.")]
    public Transform camara;

    [Tooltip("El objeto padre que se gira (XR Origin o Camera Offset). Si se deja vacío, se usa el padre de la cámara.")]
    public Transform rig;

    [Tooltip("Giro en Y (grados) con el que debe empezar la cámara. 0 = mirando hacia +Z del mundo.")]
    public float rotacionObjetivoY = 0f;

    [Tooltip("Hacerlo automáticamente al empezar.")]
    public bool alEmpezar = true;

    [Tooltip("Tecla para volver a hacerlo a mano (para el experimentador).")]
    public Key teclaRecentrar = Key.R;

    IEnumerator Start()
    {
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        if (rig == null && camara != null) rig = camara.parent;

        if (camara == null || rig == null)
        {
            Debug.LogError("RecentrarVista: asigna la cámara y asegúrate de que tiene un objeto padre (XR Origin o Camera Offset).");
            enabled = false;
            yield break;
        }

        if (!alEmpezar) yield break;

        // Los primeros frames la pose del casco puede no ser válida: esperamos a que se trackee (máx. 5 s)
        float tiempoLimite = Time.realtimeSinceStartup + 5f;
        while (!CascoTrackeado() && Time.realtimeSinceStartup < tiempoLimite)
            yield return null;

        // Un par de frames más para que la pose se asiente
        yield return null;
        yield return null;

        Recentrar();
    }

    void Update()
    {
        if (teclaRecentrar != Key.None && Keyboard.current != null && Keyboard.current[teclaRecentrar].wasPressedThisFrame)
            Recentrar();
    }

    [ContextMenu("Recentrar")]
    public void Recentrar()
    {
        if (camara == null || rig == null) return;

        // 1. Giro en Y que tiene la cámara ahora mismo
        float yInicial = YawCabeza();

        // 2. Cuánto hay que sumar o restar para llegar al objetivo.
        //    DeltaAngle elige el camino corto (p. ej. de 350° a 0° son +10°, no -350°).
        float correccion = Mathf.DeltaAngle(yInicial, rotacionObjetivoY);

        // 3. La rotación de la cámara la escribe el tracking en cada frame, así que la corrección
        //    se aplica al padre, solo alrededor del eje Y (Vector3.up).
        //    Se gira alrededor de la cabeza para que esta no cambie de sitio.
        rig.RotateAround(camara.position, Vector3.up, correccion);

        Debug.Log($"Giro Y inicial de la cámara: {yInicial:F1}°  →  ahora: {YawCabeza():F1}°  (corrección {correccion:F1}°)");
    }

    // Giro en Y a partir de hacia dónde apunta la cabeza en el plano horizontal.
    // Es más fiable que eulerAngles.y cuando la cabeza está inclinada.
    float YawCabeza()
    {
        Vector3 adelante = camara.forward;
        adelante.y = 0f;

        if (adelante.sqrMagnitude < 0.0001f)      // mirando casi recto arriba o abajo
            return camara.eulerAngles.y;

        return Mathf.Atan2(adelante.x, adelante.z) * Mathf.Rad2Deg;
    }

    static bool CascoTrackeado()
    {
        var cabeza = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
        return cabeza.isValid
            && cabeza.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool trackeado)
            && trackeado;
    }
}

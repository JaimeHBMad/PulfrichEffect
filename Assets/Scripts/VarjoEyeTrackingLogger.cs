using UnityEngine;
using Varjo.XR; // Necesitas tener el paquete de Varjo instalado e importado
using TMPro; // Necesario para controlar el HUD

public class VarjoEyeTrackingLogger : MonoBehaviour
{
    [Header("Configuración Eye Tracking")]
    [Tooltip("Grados de desviación de la pupila para registrar el cambio")]
    [Range(1f, 45f)]
    public float thresholdAngle = 15f; // Variable que actua como umbral

    private string currentState = "Centro";         // Centro as default

    public TextMeshProUGUI hudText; // Usa TextMeshPro si no usas Canvas

    void Update()
    {                                                         
        if (!VarjoEyeTracking.IsGazeAllowed())                  // verify si el eye tracking esta habilitado en las opciones de Varjo Base
        {
            UpdateHUD("Permiso Eye Tracking denegado");
            return;
        }

        VarjoEyeTracking.GazeData gazeData = VarjoEyeTracking.GetGaze();    // extraemos el paquete de datos del sensor infrarrojo de las lentes

        if (gazeData.status != VarjoEyeTracking.GazeStatus.Valid)       // comprobamos si el visor esta detectando los ojos correctamente (Valid)
{                                                                       // evita errores si parpadeas o te quitas las gafas
            UpdateHUD("Calibrando / Buscando pupilas...");
            return;
        }
        
        Vector3 gazeForward = gazeData.gaze.forward;        // Obtenemos el vector direccional de la mirada

        // Convertimos el vector tridimensional en angulos 2D (Yaw para X, Pitch para Y)
        float yaw = Mathf.Atan2(gazeForward.x, gazeForward.z) * Mathf.Rad2Deg; // Eje Izquierda/Derecha
        float pitch = Mathf.Asin(gazeForward.y) * Mathf.Rad2Deg;               // Eje Arriba/Abajo

        string newState = "Centro";         // Variable de apoyo para comprobar si cambia la posicion de la pupila 

        // evaluamos si la pupila ha superado el umbral en algun eje
        if (Mathf.Abs(pitch) > thresholdAngle || Mathf.Abs(yaw) > thresholdAngle)
        {
            // Determinamos si el movimiento principal es horizontal o vertical
            if (Mathf.Abs(yaw) > Mathf.Abs(pitch))
            {
                // En Varjo, X positivo es derecha, X negativo es izquierda
                newState = yaw > 0 ? "Derecha" : "Izquierda";
            }
            else
            {
                // En Varjo, Y positivo es arriba, Y negativo es abajo
                newState = pitch > 0 ? "Arriba" : "Abajo";
            }
        }

        // Actualizamos el HUD en tiempo real con los valores exactos
        if (hudText != null)
        {
            hudText.text = $"Direccion: {newState}\nHorizontal: {yaw:F1}°\nVertical: {pitch:F1}°";
        }

        // Si el estado es diferente al del fotograma anterior, lanzamos el log
        if (newState != currentState)
        {
            Debug.Log($"[Varjo XR-4] Mirada desviada hacia: {newState}");
            currentState = newState;
        }
    }

    private void UpdateHUD(string message)    // funcion auxiliar para no repetir codigo para imprimir HUD
    {
        if (hudText != null)
        {
            hudText.text = message;
        }
    }
}

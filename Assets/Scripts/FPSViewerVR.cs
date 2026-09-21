using UnityEngine;
using TMPro; // Usamos TextMeshPro porque en VR se ve mucho más nítido

public class FPSViewerVR : MonoBehaviour
{
    [Tooltip("Arrastra aquí el texto de TextMeshPro de tu Canvas")]
    public TextMeshProUGUI fpsText;
    
    private float deltaTime = 0.0f;     //f confirmar que son float y no doble, mas optimized

    void Update()
    {
        // Time.unscaledDeltaTime da tiempo exacto pero para suavizar el valor y hacerlo legible lo juntamos tomando un 10% junto a el tiempo nuevo
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;           // Calculamos el tiempo entre frames (suavizado para que el texto no parpadee a lo loco)
        float fps = 1.0f / deltaTime;
        
        if (fpsText != null)        // Check que haya GO
        {
            fpsText.text = $"FPS: {Mathf.Ceil(fps)}";               // Ceil para redondear para arriba
            
            // Cambiar de color si bajan de x FPS (lo suyo seria 90 estándar en VR, cambiar varjo base, rn capado a 70)
            if (fps < 70f)
                fpsText.color = Color.yellow;
            else
                fpsText.color = Color.green;
        }
    }
}
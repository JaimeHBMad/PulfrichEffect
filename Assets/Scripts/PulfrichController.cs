using UnityEngine;
using UnityEngine.InputSystem; // VIP, añadir el nuevo Input System

public class PulfrichController : MonoBehaviour
{
    [Tooltip("Material que tiene el shader Custom/Pulfrich")]
    public Material pulfrichMaterial;
    
    [Range(0f, 1f)]             // control desde Unity
    public float darknessLevel = 0.8f;      //1 negro total, 0 transparente
    public float eye;

    void Update()
    {
        if (pulfrichMaterial != null)
        {
            // Actualizamos la variable del shader en cada frame
            pulfrichMaterial.SetFloat("_Darkness", darknessLevel);
        }

        // Al pulsar 1, activamos el One Minus (Quad en el ojo izquierdo)
        if (Keyboard.current.digit1Key.wasPressedThisFrame)   //(Input.GetKeyDown(KeyCode.Alpha1)) antiguo sistema input
        {
            eye=1;
            pulfrichMaterial.SetFloat("_EyeSide", eye);         // Pulso 1 y eye a 1, eyeIndex + Minus, ojo L
        }

        // Al pulsar 2, usamos la ruta directa (Quad en el ojo derecho)
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            eye=0;
            pulfrichMaterial.SetFloat("_EyeSide", eye);         // Pulso 2 y eye a 0, eyeIndex directo, ojo R
        }


    }
}

// using UnityEngine;

// public class PulfrichController : MonoBehaviour
// {
//     private Material pulfrichMaterial;

//     void Start()
//     {
//         // Capturamos el material del Quad al empezar
//         pulfrichMaterial = GetComponent<MeshRenderer>().material;
        
//         // Empezamos por defecto en el ojo izquierdo (1 = Camino B del Lerp)
//         pulfrichMaterial.SetFloat("_EyeSide", 1f); 
//     }

//     void Update()
//     {
//         // Al pulsar 1, activamos el One Minus (Ojo Izquierdo)
//         if (Input.GetKeyDown(KeyCode.Alpha1))
//         {
//             pulfrichMaterial.SetFloat("_EyeSide", 1f);
//         }

//         // Al pulsar 2, usamos la ruta directa (Ojo Derecho)
//         if (Input.GetKeyDown(KeyCode.Alpha2))
//         {
//             pulfrichMaterial.SetFloat("_EyeSide", 0f);
//         }
//     }
// }
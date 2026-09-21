using UnityEngine;
using UnityEngine.InputSystem; // VIP, añadir el nuevo Input System

public class GlobalVolumeBehave : MonoBehaviour
{
    public GameObject globalVolume;
    private bool isOn;          

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isOn = true;
        Debug.Log($"isOn empieza con valor: {isOn}");
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)          // Detecta si la tecla Espacio ha sido pulsada en este frame con el nuevo sistema
        {   
            Debug.Log("Has pulsado el espacio");
            if (globalVolume != null)
            {
                
                // Desactiva el GameObject
                isOn = !isOn;
                Debug.Log("Al pulsar el espacio el valor de isOn ha pasado a :" +isOn);  // 2 formas de debuggear variables con $ o +
                globalVolume.SetActive(isOn);

            }
            else
            {
                Debug.LogWarning("¡No has asignado el Global Volume en el inspector!");
            }
        }
    }
}


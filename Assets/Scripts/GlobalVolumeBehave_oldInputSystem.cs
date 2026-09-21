using UnityEngine;

public class GlobalVolumeBehave_oldInputSystem : MonoBehaviour
{
    public GameObject globalVolume;
    private bool isOn; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isOn = true;
    }


    // Update is called once per frame
    void Update()
    {
        // Detecta si se pulsa la tecla Espacio en este frame
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Comprueba que el objeto ha sido asignado para evitar errores
            if (globalVolume != null)
            {
                // Desactiva el GameObject
                globalVolume.SetActive(isOn);
                isOn = !isOn;
                // TIP: Si en su lugar quieres que actúe como un interruptor (apagar/encender), 
                // borra la línea de arriba y usa esta:
                // globalVolume.SetActive(!globalVolume.activeSelf);
            }
            else
            {
                Debug.LogWarning("¡No has asignado el Global Volume en el inspector!");
            }
        }
    }
}



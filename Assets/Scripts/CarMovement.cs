using UnityEngine;

public class CarMovement : MonoBehaviour
{
    public Vector2 startPosition = new Vector2(-5f, 0f);
    public Vector2 endPosition = new Vector2(5f, 0f);
    public float speed = 3f;

    private float originalZ;

    void Start()
    {
        
        originalZ = transform.position.z;   // Guardamos pos Z inicial para no alterarla
        transform.position = new Vector3(startPosition.x, startPosition.y, originalZ);          // Teletransportamos el objeto a la pos incial
    }

    void Update()
    {
        // Obtenemos la posición actual en 2D
        Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);

        // Calculamos el próximo paso usando MoveTowards (se mueve de forma lineal sin pasarse del final)
        Vector2 nextPos = Vector2.MoveTowards(currentPos, endPosition, speed * Time.deltaTime);

        // Aplicamos la nueva posición manteniendo la Z intacta
        transform.position = new Vector3(nextPos.x, nextPos.y, originalZ);

        // Si la distancia al destino es prácticamente 0, teletransportamos al inicio
        if (Vector2.Distance(nextPos, endPosition) < 0.001f)
        {
            transform.position = new Vector3(startPosition.x, startPosition.y, originalZ);
        }
    }

}
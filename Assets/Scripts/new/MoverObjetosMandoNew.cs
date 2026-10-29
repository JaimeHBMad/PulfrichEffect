using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mueve un objeto a izquierda y derecha con los mandos:
/// mano izquierda → hacia la izquierda (-X), mano derecha → hacia la derecha (+X).
/// Botón principal = paso grande, secundario = paso pequeño.
/// </summary>
public class MoverObjetosMandoNew : MonoBehaviour
{
    [Header("Objeto")]
    [Tooltip("Arrastra aquí el GameObject que quieres mover.")]
    public Transform objeto;

    [Tooltip("Eje en el que se mueve. (1,0,0) = X: la izquierda lo lleva hacia -X y la derecha hacia +X. El tamaño multiplica el paso.")]
    public Vector3 direccion = Vector3.right;

    [Header("Pasos")]
    [Tooltip("Lo que se mueve con el botón principal.")]
    public float pasoGrande = 0.1f;

    [Tooltip("Lo que se mueve con el botón secundario.")]
    public float pasoPequeno = 0.01f;

    [Header("Límites")]
    [Min(0f)]
    [Tooltip("Desplazamiento máximo desde la posición inicial, hacia cada lado. 0 = sin límite.")]
    public float limite = 0f;

    [Tooltip("Si está desactivado se ignoran los botones.")]
    public bool permitirAjuste = true;

    [Header("Estado")]
    [Tooltip("Desplazamiento actual respecto a la posición inicial. Negativo = izquierda, positivo = derecha.")]
    [SerializeField] private float desplazamiento = 0f;

    public float Desplazamiento => desplazamiento;

    private Vector3 posicionInicial;
    private bool inicializado;

    void Start()
    {
        if (objeto == null)
        {
            Debug.LogWarning("MoverObjetosMando: falta asignar el objeto.");
            return;
        }

        // Guardamos dónde empieza; el desplazamiento se mide desde ahí
        posicionInicial = objeto.localPosition;
        inicializado = true;
        Aplicar();
    }

    // ---------- Mano izquierda → hacia la izquierda (-X) ----------

    public void BigL(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(-pasoGrande);
    }

    public void SmallL(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(-pasoPequeno);
    }

    // ---------- Mano derecha → hacia la derecha (+X) ----------

    public void BigR(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(+pasoGrande);
    }

    public void SmallR(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(+pasoPequeno);
    }

    // ---------- API ----------

    public void EstablecerDesplazamiento(float valor)
    {
        if (limite > 0f) valor = Mathf.Clamp(valor, -limite, limite);
        // Redondeo para no arrastrar error de coma flotante al sumar pasos
        desplazamiento = Mathf.Round(valor * 10000f) / 10000f;
        Aplicar();
    }

    [ContextMenu("Volver a la posición inicial")]
    public void Reiniciar()
    {
        EstablecerDesplazamiento(0f);
    }

    // ---------- Interno ----------

    private void Mover(float delta)
    {
        if (!permitirAjuste) return;
        EstablecerDesplazamiento(desplazamiento + delta);
        Debug.Log($"Desplazamiento: {desplazamiento:F4}");
    }

    private void Aplicar()
    {
        if (!inicializado) return;   // aún no sabemos la posición inicial
        objeto.localPosition = posicionInicial + direccion * desplazamiento;
    }
}

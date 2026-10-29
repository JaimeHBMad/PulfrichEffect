using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mueve dos objetos con los mandos: la mano izquierda mueve 'Left' y la derecha 'Right'.
/// Botón principal = paso grande, secundario = paso pequeño.
/// Cada objeto se mueve a lo largo de su propia dirección y lleva su propio desplazamiento.
/// </summary>
public class MoverObjetosMando : MonoBehaviour
{
    [Serializable]
    public class ObjetoMovible
    {
        [Tooltip("Arrastra aquí el GameObject.")]
        public Transform objeto;

        [Tooltip("Hacia dónde se mueve con los botones. (0,0,1) = adelante en Z, (0,0,-1) = al revés, (1,0,0) = en X. El tamaño multiplica el paso.")]
        public Vector3 direccion = Vector3.forward;

        [Tooltip("Desplazamiento actual respecto a la posición inicial.")]
        public float desplazamiento = 0f;

        [NonSerialized] public Vector3 posicionInicial;
    }

    [Header("Objetos")]
    [Tooltip("Lo mueve la mano izquierda (BigL / SmallL).")]
    public ObjetoMovible left = new ObjetoMovible();

    [Tooltip("Lo mueve la mano derecha (BigR / SmallR).")]
    public ObjetoMovible right = new ObjetoMovible();

    [Header("Pasos")]
    [Tooltip("Lo que se mueve con el botón principal.")]
    public float pasoGrande = 0.1f;

    [Tooltip("Lo que se mueve con el botón secundario.")]
    public float pasoPequeno = 0.01f;

    [Header("Límites")]
    [Min(0f)]
    [Tooltip("Desplazamiento máximo de cada objeto desde su posición inicial, hacia cada lado. 0 = sin límite.")]
    public float limite = 0f;

    [Tooltip("Si está desactivado se ignoran los botones.")]
    public bool permitirAjuste = true;

    private bool inicializado;

    void Start()
    {
        // Guardamos dónde empieza cada objeto; el desplazamiento se mide desde ahí
        Inicializar(left, "Left");
        Inicializar(right, "Right");

        inicializado = true;
        Aplicar(left);
        Aplicar(right);
    }

    // ---------- Mano izquierda → Left ----------

    public void BigL(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(left, +pasoGrande, "Left");
    }

    public void SmallL(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(left, +pasoPequeno, "Left");
    }

    // ---------- Mano derecha → Right ----------

    public void BigR(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(right, +pasoGrande, "Right");
    }

    public void SmallR(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(right, +pasoPequeno, "Right");
    }

    // ---------- Opcional: hacia atrás ----------
    // Enlázalos a otros botones (gatillo, grip...) si quieres poder volver hacia atrás.

    public void BigLAtras(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(left, -pasoGrande, "Left");
    }

    public void SmallLAtras(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(left, -pasoPequeno, "Left");
    }

    public void BigRAtras(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(right, -pasoGrande, "Right");
    }

    public void SmallRAtras(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) Mover(right, -pasoPequeno, "Right");
    }

    // ---------- API ----------

    public void EstablecerDesplazamiento(ObjetoMovible o, float valor)
    {
        if (limite > 0f) valor = Mathf.Clamp(valor, -limite, limite);
        // Redondeo para no arrastrar error de coma flotante al sumar pasos
        o.desplazamiento = Mathf.Round(valor * 10000f) / 10000f;
        Aplicar(o);
    }

    [ContextMenu("Volver a la posición inicial")]
    public void Reiniciar()
    {
        EstablecerDesplazamiento(left, 0f);
        EstablecerDesplazamiento(right, 0f);
    }

    // ---------- Interno ----------

    private static void Inicializar(ObjetoMovible o, string nombre)
    {
        if (o.objeto == null)
        {
            Debug.LogWarning($"MoverObjetosMando: falta asignar el objeto {nombre}.");
            return;
        }

        o.posicionInicial = o.objeto.localPosition;

        if (o.direccion == Vector3.zero)
            Debug.LogWarning($"MoverObjetosMando: {nombre} tiene dirección (0,0,0) y no se moverá.");
    }

    private void Mover(ObjetoMovible o, float delta, string nombre)
    {
        if (!permitirAjuste) return;
        EstablecerDesplazamiento(o, o.desplazamiento + delta);
        Debug.Log($"{nombre}: {o.desplazamiento:F4}");
    }

    private void Aplicar(ObjetoMovible o)
    {
        if (!inicializado || o.objeto == null) return;   // aún no sabemos la posición inicial
        o.objeto.localPosition = o.posicionInicial + o.direccion * o.desplazamiento;
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;

/// <summary>
/// Diagnóstico de mandos para el nuevo Input System.
/// - Lista los dispositivos conectados (al empezar, a los 3 s y al pulsar F1).
/// - Avisa cuando un dispositivo se conecta o se desconecta.
/// - Saca por consola cada botón que se pulsa, con la ruta que habría que poner en el binding.
/// - Comprueba si tus acciones (nearL, nearR...) se disparan y qué control las dispara.
/// Todos los mensajes empiezan por [Diagnóstico] para filtrarlos en la consola.
/// </summary>
public class DiagnosticoMandos : MonoBehaviour
{
    [Tooltip("Opcional: el PlayerInput de la escena, para ver qué dispositivos tiene emparejados.")]
    public PlayerInput playerInput;

    [Tooltip("Opcional: acciones de tu Input Actions para comprobar si se disparan (nearL, nearR, Confirmar...).")]
    public InputActionReference[] accionesAProbar;

    [Tooltip("Rutas genéricas que se prueban por su cuenta, sin pasar por el PlayerInput.")]
    public string[] rutasAProbar =
    {
        "<XRController>{LeftHand}/primaryButton",
        "<XRController>{RightHand}/primaryButton",
        "<XRController>{LeftHand}/triggerPressed",
        "<XRController>{RightHand}/triggerPressed",
    };

    [Tooltip("Mostrar también las pulsaciones de teclado y ratón.")]
    public bool incluirTecladoYRaton = false;

    private IDisposable escuchaBotones;
    private readonly List<InputAction> accionesDePrueba = new List<InputAction>();

    void OnEnable()
    {
        // 1. Avisos de conexión / desconexión
        InputSystem.onDeviceChange += AlCambiarDispositivo;

        // 2. Cualquier botón pulsado en cualquier dispositivo
        escuchaBotones = InputSystem.onAnyButtonPress.Call(AlPulsarBoton);

        // 3. Rutas genéricas, independientes de tu Input Actions y del PlayerInput
        foreach (string ruta in rutasAProbar)
        {
            if (string.IsNullOrWhiteSpace(ruta)) continue;

            var accion = new InputAction(type: InputActionType.Button, binding: ruta);
            string rutaCopia = ruta;
            accion.performed += ctx =>
                Debug.Log($"[Diagnóstico] Ruta genérica OK: {rutaCopia}  ← {ctx.control.device.displayName}");
            accion.Enable();
            accionesDePrueba.Add(accion);
        }

        // 4. Tus propias acciones
        if (accionesAProbar != null)
            foreach (var referencia in accionesAProbar)
                if (referencia != null && referencia.action != null)
                    referencia.action.performed += AlDispararAccion;
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= AlCambiarDispositivo;
        escuchaBotones?.Dispose();

        foreach (var accion in accionesDePrueba)
        {
            accion.Disable();
            accion.Dispose();
        }
        accionesDePrueba.Clear();

        if (accionesAProbar != null)
            foreach (var referencia in accionesAProbar)
                if (referencia != null && referencia.action != null)
                    referencia.action.performed -= AlDispararAccion;
    }

    void Start()
    {
        ListarDispositivos();
        // Los mandos XR suelen aparecer un poco después de arrancar, así que se repite a los 3 s
        Invoke(nameof(ListarDispositivos), 3f);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            ListarDispositivos();
    }

    // ---------- Listado ----------

    [ContextMenu("Listar dispositivos")]
    public void ListarDispositivos()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[Diagnóstico] Dispositivos conectados ({InputSystem.devices.Count}):");

        bool hayMandoXR = false;
        foreach (var d in InputSystem.devices)
        {
            sb.AppendLine("  - " + Describir(d));
            if (d is XRController) hayMandoXR = true;
        }

        if (!hayMandoXR)
            sb.AppendLine("  !! No hay ningún mando XR conectado.");

        if (playerInput != null)
        {
            string esquema = string.IsNullOrEmpty(playerInput.currentControlScheme)
                ? "(ninguno)" : playerInput.currentControlScheme;
            sb.AppendLine($"PlayerInput '{playerInput.name}' | esquema: {esquema} | dispositivos emparejados:");
            foreach (var d in playerInput.devices)
                sb.AppendLine("  - " + d.displayName);
        }

        if (accionesAProbar != null)
        {
            foreach (var referencia in accionesAProbar)
            {
                if (referencia == null || referencia.action == null) continue;
                var a = referencia.action;
                sb.AppendLine($"Acción '{a.name}': {(a.enabled ? "habilitada" : "NO habilitada")}");
                foreach (var control in a.controls)
                    sb.AppendLine("  responde a: " + control.path);
            }
        }

        Debug.Log(sb.ToString());
    }

    // ---------- Eventos ----------

    private void AlCambiarDispositivo(InputDevice device, InputDeviceChange cambio)
    {
        string texto;
        switch (cambio)
        {
            case InputDeviceChange.Added: texto = "Conectado"; break;
            case InputDeviceChange.Removed: texto = "Quitado"; break;
            case InputDeviceChange.Disconnected: texto = "Desconectado"; break;
            case InputDeviceChange.Reconnected: texto = "Reconectado"; break;
            default: return;
        }
        Debug.Log($"[Diagnóstico] {texto}: {Describir(device)}");
    }

    private void AlPulsarBoton(InputControl control)
    {
        InputDevice d = control.device;
        if (!incluirTecladoYRaton && (d is Keyboard || d is Mouse)) return;

        Debug.Log($"[Diagnóstico] Botón pulsado: {control.path}  |  ruta para el binding: {RutaParaBinding(control)}");
    }

    private void AlDispararAccion(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[Diagnóstico] Acción '{ctx.action.name}' disparada por {ctx.control.path}");
    }

    // ---------- Utilidades ----------

    private static string Describir(InputDevice d)
    {
        return $"{d.displayName} | layout: {d.layout} | usos: {string.Join(", ", d.usages)} | id: {d.deviceId}";
    }

    // Convierte "/MiMando1/primaryButton" en "<LayoutDelMando>{LeftHand}/primaryButton"
    private static string RutaParaBinding(InputControl control)
    {
        InputDevice d = control.device;
        string relativa = control.path.Length > d.path.Length
            ? control.path.Substring(d.path.Length + 1)
            : control.name;

        string mano = "";
        foreach (var uso in d.usages)
        {
            if (uso == CommonUsages.LeftHand) mano = "{LeftHand}";
            else if (uso == CommonUsages.RightHand) mano = "{RightHand}";
        }

        return $"<{d.layout}>{mano}/{relativa}";
    }
}

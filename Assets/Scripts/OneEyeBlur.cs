using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Custom Post Process de HDRP que desenfoca un solo ojo en VR.
/// IMPORTANTE: añádelo en Project Settings → Graphics → HDRP Global Settings →
/// Custom Post Process Orders → "After Post Process", o no se ejecutará.
/// </summary>
[Serializable, VolumeComponentMenu("Post-processing/Custom/Desenfoque de un ojo (VR)")]
public sealed class OneEyeBlur : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Desenfoque del ojo izquierdo: 0 = nítido, 1 = radio máximo.")]
    public ClampedFloatParameter ojoIzquierdo = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("Desenfoque del ojo derecho: 0 = nítido, 1 = radio máximo.")]
    public ClampedFloatParameter ojoDerecho = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("Radio máximo del desenfoque, como fracción del ancho de la imagen de cada ojo.")]
    public ClampedFloatParameter radio = new ClampedFloatParameter(0.015f, 0f, 0.05f);

    private const string kShaderName = "Hidden/Shader/OneEyeBlur";

    private static readonly int MainTexId    = Shader.PropertyToID("_MainTex");
    private static readonly int BlurLeftId   = Shader.PropertyToID("_BlurLeft");
    private static readonly int BlurRightId  = Shader.PropertyToID("_BlurRight");
    private static readonly int BlurRadiusId = Shader.PropertyToID("_BlurRadius");

    private Material m_Material;

    public bool IsActive() =>
        m_Material != null &&
        radio.value > 0f &&
        (ojoIzquierdo.value > 0f || ojoDerecho.value > 0f);

    public override CustomPostProcessInjectionPoint injectionPoint =>
        CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup()
    {
        var shader = Shader.Find(kShaderName);
        if (shader != null)
            m_Material = CoreUtils.CreateEngineMaterial(shader);
        else
            Debug.LogError($"No se encuentra el shader '{kShaderName}'. " +
                           "Comprueba que OneEyeBlur.shader está en el proyecto " +
                           "(en una carpeta Resources para que entre en la build).");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

        m_Material.SetFloat(BlurLeftId,   ojoIzquierdo.value);
        m_Material.SetFloat(BlurRightId,  ojoDerecho.value);
        m_Material.SetFloat(BlurRadiusId, radio.value);
        m_Material.SetTexture(MainTexId, source);

        HDUtils.DrawFullScreen(cmd, m_Material, destination, shaderPassId: 0);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}

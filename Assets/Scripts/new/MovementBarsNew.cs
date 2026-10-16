using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.UI;         //solo haria falta si lo quiero tener en UI 

/// <summary>
/// Genera una textura procedural (fondo gris degradado + barras blancas verticales)
/// que es "tileable" en horizontal, y la desplaza de izquierda a derecha en bucle infinito.
/// Funciona sobre un Renderer (Quad, Plane...) o sobre un RawImage de UI.
/// Con el Shader Graph "BarrasPorOjo" (nodo Eye Index) se puede elegir qué ojo ve el objeto.
/// </summary>
[ExecuteAlways]
public class MovementBarsNew : MonoBehaviour
{
    public enum OjoVisible { Ambos, Izquierdo, Derecho }            // enum: nueva variable para decidir que ojo ver x plano

    [Header("Textura")]
    [SerializeField] int textureWidth = 1024;
    [SerializeField] int textureHeight = 384;
    [SerializeField] int seed = 12345;
    [SerializeField, Range(4, 80)] int barCount = 24;

    [Header("Colores")]
    [SerializeField] Color topColor = new Color(0.60f, 0.60f, 0.60f);    // #999999
    [SerializeField] Color bottomColor = new Color(0.45f, 0.45f, 0.45f); // #737373
    [SerializeField] Color barColor = Color.white;

    [Header("Barras (en proporción de la textura)")]
    [SerializeField] Vector2 barWidthRange = new Vector2(0.02f, 0.065f);
    [SerializeField] Vector2 barLengthRange = new Vector2(0.3f, 0.75f);

    [Header("Movimiento")]
    [Tooltip("Vueltas completas de la textura por segundo. Positivo = izquierda a derecha.")]
    [SerializeField] float speed = 0.1f;
    [Tooltip("Nombre de la propiedad de textura en el material (Shader Graph: la Reference que le pongas, HDRP Unlit: _UnlitColorMap, URP: _BaseMap, Built-in: _MainTex).")]
    [SerializeField] string textureProperty = "_UnlitColorMap";   //para hdrp

    [Header("Ojo (VR)")]
    [Tooltip("Qué ojo ve este objeto. Solo funciona con un Renderer y el Shader Graph que usa el nodo Eye Index.")]
    [SerializeField] OjoVisible ojoVisible = OjoVisible.Ambos;      // enum visible desde el inspector

    // Variables del shader graph, deben coincidir exactamente con las References del Shader Graph
    static readonly int DesplazamientoID = Shader.PropertyToID("_Desplazamiento");
    static readonly int VerIzquierdoID = Shader.PropertyToID("_VerL");
    static readonly int VerDerechoID = Shader.PropertyToID("_VerR");

    Texture2D _texture;     // variables para trabajar la textura
    Renderer _renderer;
    //RawImage _rawImage;           //UI
    MaterialPropertyBlock _mpb;
    float _offset;

    struct Bar { public int x, width, yMin, yMax; }

    void OnEnable()
    {
        _renderer = GetComponent<Renderer>();
        //_rawImage = GetComponent<RawImage>();     // UI
        _mpb = new MaterialPropertyBlock();         // copia del material para que cada objeto pueda tener valores distintos
        Generate();
    }

    void OnValidate()
    {
        if (isActiveAndEnabled) Generate();
    }

    void OnDisable()
    {
        if (_texture != null)
        {
            if (Application.isPlaying) Destroy(_texture);
            else DestroyImmediate(_texture);
        }
    }

    [ContextMenu("Regenerar")]
    public void Generate()
    {
        if (_texture == null || _texture.width != textureWidth || _texture.height != textureHeight)
        {
            _texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                name = "MovingBarsTexture",
                wrapMode = TextureWrapMode.Repeat, // imprescindible para el loop
                filterMode = FilterMode.Bilinear
            };
        }

        var pixels = new Color32[textureWidth * textureHeight];

        // 1) Fondo: degradado vertical
        for (int y = 0; y < textureHeight; y++)
        {
            Color32 c = Color.Lerp(bottomColor, topColor, y / (float)(textureHeight - 1));
            int row = y * textureWidth;
            for (int x = 0; x < textureWidth; x++) pixels[row + x] = c;
        }

        // 2) Barras
        Color32 bar = barColor;
        foreach (var b in CreateBars())
        {
            for (int y = b.yMin; y < b.yMax; y++)
            {
                int row = y * textureWidth;
                for (int i = 0; i < b.width; i++)
                {
                    // El módulo hace que la barra que se sale por la derecha
                    // aparezca por la izquierda -> textura tileable sin costuras
                    int x = (b.x + i) % textureWidth;
                    pixels[row + x] = bar;
                }
            }
        }

        _texture.SetPixels32(pixels);
        _texture.Apply();
        ApplyTexture();
    }

    //List<Bar> CreateBars()            //OLD
    //{
    //    var rng = new System.Random(seed);
    //    float R(float min, float max) => min + (float)rng.NextDouble() * (max - min);

    //    var bars = new List<Bar>(barCount);
    //    for (int i = 0; i < barCount; i++)
    //    {
    //        int width = Mathf.Max(2, Mathf.RoundToInt(R(barWidthRange.x, barWidthRange.y) * textureWidth));
    //        int length = Mathf.RoundToInt(R(barLengthRange.x, barLengthRange.y) * textureHeight);
    //        int x = rng.Next(0, textureWidth);

    //        int yMin, yMax;
    //        double type = rng.NextDouble();
    //        if (type < 0.4)        // cuelga desde arriba
    //        {
    //            yMax = textureHeight;
    //            yMin = textureHeight - length;
    //        }
    //        else if (type < 0.75)  // sube desde abajo
    //        {
    //            yMin = 0;
    //            yMax = length;
    //        }
    //        else                   // flotando en medio
    //        {
    //            length = Mathf.RoundToInt(length * 0.7f);
    //            yMin = rng.Next(0, Mathf.Max(1, textureHeight - length));
    //            yMax = yMin + length;
    //        }

    //        bars.Add(new Bar
    //        {
    //            x = x,
    //            width = width,
    //            yMin = Mathf.Clamp(yMin, 0, textureHeight),
    //            yMax = Mathf.Clamp(yMax, 0, textureHeight)
    //        });
    //    }
    //    return bars;
    //}

    List<Bar> CreateBars()          // nueva funcion para crear las barras rng
    {
        var rng = new System.Random(seed);
        float R(float min, float max) => min + (float)rng.NextDouble() * (max - min);

        var bars = new List<Bar>(barCount);
        for (int i = 0; i < barCount; i++)
        {
            int width = Mathf.Max(2, Mathf.RoundToInt(R(barWidthRange.x, barWidthRange.y) * textureWidth));
            int length = Mathf.Clamp(Mathf.RoundToInt(R(barLengthRange.x, barLengthRange.y) * textureHeight), 1, textureHeight);
            int x = rng.Next(0, textureWidth);

            // Altura aleatoria: la barra puede empezar en cualquier punto siempre que quepa entera dentro de la textura

            int yMin = rng.Next(0, textureHeight - length + 1);
            int yMax = yMin + length;

            bars.Add(new Bar { x = x, width = width, yMin = yMin, yMax = yMax });
        }
        return bars;
    }

    void ApplyTexture()
    {
        //if (_rawImage != null)            // Con UI
        //{
        //    _rawImage.texture = _texture;
        //}
        //else if (_renderer != null)
        //{
        //    _renderer.GetPropertyBlock(_mpb);
        //    _mpb.SetTexture(textureProperty, _texture);
        //    _renderer.SetPropertyBlock(_mpb);
        //}
        //UpdateOffset();

        if (_renderer == null) return;

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetTexture(textureProperty, _texture);
        _renderer.SetPropertyBlock(_mpb);

        UpdateOffset();
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        _offset = Mathf.Repeat(_offset - speed * Time.deltaTime, 1f);       // restar la imagen = desplazamiento derecha, el repeat lo mantiene en [0,1) para evitar pérdida de precisión con el tiempo
        UpdateOffset();
    }

    void UpdateOffset()
    {
        //if (_rawImage != null)        //con UI
        //{
        //    var r = _rawImage.uvRect;
        //    r.x = _offset;
        //    _rawImage.uvRect = r;
        //}
        //else if (_renderer != null)   
        if ( _renderer == null ) return;    
            _renderer.GetPropertyBlock(_mpb);

            // Vector4: (tilingX, tilingY, offsetX, offsetY) -> propiedad _ST del shader (lo usan los materiales estándar como HDRP/Unlit)

            _mpb.SetVector(textureProperty + "_ST", new Vector4(1f, 1f, _offset, 0f));          // ST variable tilling (x y w z), esto es lo mismo que hace el nodo (texture, tilling (1,1), offset (desplazamiento (en X),0) 

            // Lo mismo para el Shader Graph: entra en el Offset del nodo Tiling And Offset
            _mpb.SetFloat(DesplazamientoID, _offset);

            // 1 = ese ojo ve el objeto, 0 = ese ojo no lo ve
            _mpb.SetFloat(VerIzquierdoID, ojoVisible != OjoVisible.Derecho ? 1f : 0f);
            _mpb.SetFloat(VerDerechoID, ojoVisible != OjoVisible.Izquierdo ? 1f : 0f);

            _renderer.SetPropertyBlock(_mpb);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera una textura procedural (fondo gris degradado + barras blancas verticales)
/// que es "tileable" en horizontal, y la desplaza de izquierda a derecha en bucle infinito.
/// Funciona sobre un Renderer (Quad, Plane...) o sobre un RawImage de UI.
/// </summary>
/// 
[ExecuteAlways]     // esto para que se vean los cambios hasta cuando no esta en play
public class MovingBarsTexture : MonoBehaviour
{
    [Header("Textura")]
    [SerializeField] int textureWidth = 1024;  //por defecto las variables son private con SerilzeField se pueden ver en inspector
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
    [Tooltip("Nombre de la propiedad de textura en el material (HDRP: _UnlitColorMap, URP: _BaseMap, Built-in: _MainTex).")]
    [SerializeField] string textureProperty = "_BaseMap";

    Texture2D _texture;
    Renderer _renderer;
    RawImage _rawImage;
    MaterialPropertyBlock _mpb;
    float _offset;

    struct Bar { 
        public int x, width, yMin, yMax; 
        }

    void OnEnable()
    {
        _renderer = GetComponent<Renderer>();
        _rawImage = GetComponent<RawImage>();
        _mpb = new MaterialPropertyBlock();
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
            else DestroyImmediate(_texture);   // para que se destruya cuando no esta en play
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

    List<Bar> CreateBars()
    {
        var rng = new System.Random(seed);
        float R(float min, float max) => min + (float)rng.NextDouble() * (max - min);

        var bars = new List<Bar>(barCount);
        for (int i = 0; i < barCount; i++)
        {
            int width = Mathf.Max(2, Mathf.RoundToInt(R(barWidthRange.x, barWidthRange.y) * textureWidth));
            int length = Mathf.RoundToInt(R(barLengthRange.x, barLengthRange.y) * textureHeight);
            int x = rng.Next(0, textureWidth);

            int yMin, yMax;
            double type = rng.NextDouble();
            if (type < 0.4)        // cuelga desde arriba
            {
                yMax = textureHeight;
                yMin = textureHeight - length;
            }
            else if (type < 0.75)  // sube desde abajo
            {
                yMin = 0;
                yMax = length;
            }
            else                   // flotando en medio
            {
                length = Mathf.RoundToInt(length * 0.7f);
                yMin = rng.Next(0, Mathf.Max(1, textureHeight - length));
                yMax = yMin + length;
            }

            bars.Add(new Bar
            {
                x = x,
                width = width,
                yMin = Mathf.Clamp(yMin, 0, textureHeight),
                yMax = Mathf.Clamp(yMax, 0, textureHeight)
            });
        }
        return bars;
    }

    void ApplyTexture()
    {
        if (_rawImage != null)
        {
            _rawImage.texture = _texture;
        }
        else if (_renderer != null)
        {
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetTexture(textureProperty, _texture);
            _renderer.SetPropertyBlock(_mpb);
        }
        UpdateOffset();
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        // Restar el offset desplaza la imagen hacia la derecha.
        // Repeat lo mantiene en [0,1) para evitar pérdida de precisión con el tiempo.
        _offset = Mathf.Repeat(_offset - speed * Time.deltaTime, 1f);
        UpdateOffset();
    }

    void UpdateOffset()
    {
        if (_rawImage != null)
        {
            var r = _rawImage.uvRect;
            r.x = _offset;
            _rawImage.uvRect = r;
        }
        else if (_renderer != null)
        {
            // Vector4: (tilingX, tilingY, offsetX, offsetY) -> propiedad _ST del shader
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetVector(textureProperty + "_ST", new Vector4(1f, 1f, _offset, 0f));
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}

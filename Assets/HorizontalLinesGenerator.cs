using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class HorizontalLineGenerator : MonoBehaviour
{
    [Header("Resolución de Textura")]
    public int resolution = 512;
    
    [Header("Parámetros de Líneas")]
    public int lineCount = 8;
    [Range(0.05f, 1f)] public float minThickness = 0.1f;
    [Range(0.05f, 1f)] public float maxThickness = 0.6f;
    public int seed = 42;
    
    [Header("Animación")]
    public float scrollSpeed = 0.5f;

    [Header("Colores")]
    public Color lineColor = Color.white;
    public Color backgroundColor = Color.black;

    private Renderer rend;
    private Texture2D tex;

    void Start()
    {
        rend = GetComponent<Renderer>();
        GenerateTexture();
    }

    public void GenerateTexture()
    {
        // 1. Crear textura preparada para repetirse cíclicamente
        tex = new Texture2D(resolution, resolution);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[resolution * resolution];
        float cellHeight = (float)resolution / lineCount;

        for (int y = 0; y < resolution; y++)
        {
            float cellFloat = y / cellHeight;
            int cellID = Mathf.FloorToInt(cellFloat);
            float localY = cellFloat - cellID; 

            System.Random rng = new System.Random(seed + cellID);
            float baseThickness = (float)rng.NextDouble() * (maxThickness - minThickness) + minThickness;

            for (int x = 0; x < resolution; x++)
            {
                float noise = Mathf.PerlinNoise((float)x / resolution * 5f, seed + cellID * 10f) * 0.4f - 0.2f;
                float finalThickness = Mathf.Clamp(baseThickness + noise, 0.05f, 0.95f);
                
                float minBounds = 0.5f - (finalThickness / 2f);
                float maxBounds = 0.5f + (finalThickness / 2f);

                bool isLine = localY >= minBounds && localY <= maxBounds;
                pixels[y * resolution + x] = isLine ? lineColor : backgroundColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        
        // Asignación universal de la textura principal
        rend.material.mainTexture = tex;
    }

    void Update()
    {
        // Desplazamiento cíclico usando la propiedad nativa de Offset
        if (rend != null && rend.material != null)
        {
            Vector2 offset = rend.material.mainTextureOffset;
            offset.x -= scrollSpeed * Time.deltaTime; 
            rend.material.mainTextureOffset = offset;
        }
    }
}
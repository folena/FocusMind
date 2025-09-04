using UnityEngine;
using System.IO;
using System.Text;

[DisallowMultipleComponent]
public class HeatmapArea : MonoBehaviour
{
    [Header("Alvos")]
    public Renderer wallRenderer;   // MeshRenderer do Quad
    public Collider wallCollider;   // MeshCollider do Quad

    [Header("Textura do Heatmap")]
    public int textureWidth = 256;
    public int textureHeight = 256;
    [Tooltip("Deixe em branco p/ auto-detectar (_BaseMap no URP/Lit, senão _MainTex).")]
    public string materialTextureProperty = "";
    public FilterMode filterMode = FilterMode.Bilinear;
    public TextureWrapMode wrapMode = TextureWrapMode.Clamp;

    [Header("Pincel")]
    [Range(1, 64)] public int brushRadiusPixels = 8;
    public float brushStrength = 1f;
    [Range(0f, 1f)] public float brushFeather = 0.7f;

    [Header("Cores")]
    public Gradient colorGradient;

    [Header("Atualização")]
    public float textureUpdateHz = 5f;

    [Header("Diagnóstico")]
    public bool selfTestOnStart = false;
    public bool logChosenProperty = true;

    // internos
    private float[,] density;          // matriz usada p/ CSV e snapshot
    private Texture2D heatmapTex;
    private Color32[] pixels;
    private float maxDensitySeen = 1f;
    private float texTimer = 0f;
    private string _prop = "_BaseMap";

    private void Reset()
    {
        colorGradient = new Gradient();
        colorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.0f, 0.0f, 0.3f), 0f),
                new GradientColorKey(new Color(0.0f, 0.7f, 1.0f), 0.35f),
                new GradientColorKey(new Color(1.0f, 1.0f, 0.0f), 0.65f),
                new GradientColorKey(new Color(1.0f, 0.27f, 0.0f), 0.85f),
                new GradientColorKey(Color.white, 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.35f),
                new GradientAlphaKey(1f, 1f),
            }
        );
    }

    private void Awake()
    {
        if (!wallRenderer) wallRenderer = GetComponent<Renderer>();
        if (!wallCollider) wallCollider = GetComponent<Collider>();

        density = new float[textureWidth, textureHeight];
        pixels  = new Color32[textureWidth * textureHeight];

        heatmapTex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        heatmapTex.filterMode = filterMode;
        heatmapTex.wrapMode   = wrapMode;

        if (wallRenderer != null)
        {
            var inst = Instantiate(wallRenderer.material);
            wallRenderer.material = inst;

            _prop = ChooseTextureProperty(inst, materialTextureProperty);
            wallRenderer.material.SetTexture(_prop, heatmapTex);

            if (logChosenProperty)
                Debug.Log($"[HeatmapArea] Usando propriedade '{_prop}' no shader '{inst.shader.name}'.");
        }

        ClearTexture();
        ApplyTextureNow();

        if (selfTestOnStart)
        {
            for (int i = -40; i <= 40; i++)
            {
                AccumulateUV(new Vector2(0.5f + i / (float)textureWidth, 0.5f), 1f);
                AccumulateUV(new Vector2(0.5f, 0.5f + i / (float)textureHeight), 1f);
            }
            RegeneratePixels();
            ApplyTextureNow();
        }
    }

    private string ChooseTextureProperty(Material mat, string desired)
    {
        if (!string.IsNullOrEmpty(desired) && mat.HasProperty(desired)) return desired;
        if (mat.HasProperty("_BaseMap")) return "_BaseMap";
        if (mat.HasProperty("_MainTex")) return "_MainTex";
        return "_BaseMap";
    }

    private void Update()
    {
        texTimer += Time.deltaTime;
        float texInterval = 1f / Mathf.Max(1e-3f, textureUpdateHz);
        if (texTimer >= texInterval)
        {
            RegeneratePixels();
            ApplyTextureNow();
            texTimer = 0f;
        }
    }

    // ======= API de desenho =======
    public void AccumulateUV(Vector2 uv, float strengthMul = 1f)
    {
        if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f) return;

        int cx = Mathf.Clamp(Mathf.RoundToInt(uv.x * (textureWidth - 1)), 0, textureWidth - 1);
        int cy = Mathf.Clamp(Mathf.RoundToInt(uv.y * (textureHeight - 1)), 0, textureHeight - 1);

        PaintBrush(cx, cy, brushRadiusPixels, brushStrength * strengthMul, brushFeather);
    }

    public void ClearHeatmap()
    {
        System.Array.Clear(density, 0, density.Length);
        maxDensitySeen = 1f;
        ClearTexture();
        ApplyTextureNow();
    }

    public Collider GetCollider() => wallCollider;

    // ======= EXPORTS =======
    public byte[] EncodeToPNG()
    {
        RegeneratePixels();
        ApplyTextureNow();
        return heatmapTex.EncodeToPNG();
    }

    public void ExportDensityCsv(string absolutePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("x,y,density");
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float d = density[x, y];
                if (d <= 0f) continue;
                sb.Append(x).Append(',').Append(y).Append(',').Append(d.ToString("G9")).AppendLine();
            }
        }
        File.WriteAllText(absolutePath, sb.ToString(), Encoding.UTF8);
    }

    /// NOVO: snapshot (cópia) da densidade para acumular no DataExportService
    public (int width, int height, float[, ] grid) GetDensitySnapshot()
    {
        int w = textureWidth;
        int h = textureHeight;
        var copy = new float[w, h];
        System.Buffer.BlockCopy(density, 0, copy, 0, sizeof(float) * density.Length);
        return (w, h, copy);
    }

    // ======= internos =======
    private void PaintBrush(int cx, int cy, int radius, float strength, float feather)
    {
        int r2 = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            if (y < 0 || y >= textureHeight) continue;
            int dy = y - cy;
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= textureWidth) continue;
                int dx = x - cx;
                int d2 = dx * dx + dy * dy;
                if (d2 > r2) continue;

                float d = Mathf.Sqrt(d2) / Mathf.Max(1, radius);
                float w = 1f - d;
                if (feather > 0f)
                    w = Mathf.Pow(Mathf.Clamp01(w), Mathf.Lerp(1f, 3f, feather));

                density[x, y] += strength * w;
                if (density[x, y] > maxDensitySeen) maxDensitySeen = density[x, y];
            }
        }
    }

    private void RegeneratePixels()
    {
        float invMax = 1f / Mathf.Max(1e-6f, maxDensitySeen);
        int i = 0;
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++, i++)
            {
                float t = density[x, y] * invMax;
                if (t <= 0f)
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                }
                else
                {
                    Color c = colorGradient.Evaluate(Mathf.Clamp01(t));
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(t) * 255f);
                    pixels[i] = new Color32(
                        (byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), a
                    );
                }
            }
        }
        heatmapTex.SetPixels32(pixels);
    }

    private void ClearTexture()
    {
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
        heatmapTex.SetPixels32(pixels);
    }

    private void ApplyTextureNow()
    {
        heatmapTex.Apply(false, false);
    }
}

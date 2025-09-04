using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class DataExportService : MonoBehaviour
{
    public static DataExportService I { get; private set; }

    [Header("Identificação da Sessão")]
    public string participanteId = "P001";
    public string sessaoId = "S001";
    public string estudoId = "FocusMind";
    public bool salvarAutomaticamenteNoQuit = true;

    [Header("Destino")]
    public string pastaRelativa = "exports"; // dentro de Application.persistentDataPath

    // infos da última exportação
    public string LastExportFolder { get; private set; }
    public string LastExportEventsPath { get; private set; }
    public string LastExportSummaryPath { get; private set; }

    public event Action<string, string> OnFilesSaved; // (eventsPath, summaryPath)

    [Serializable]
    public class Evento
    {
        public DateTime utc;
        public string fase;
        public string tipo;      // TargetShown, Hit, Error, Omission, Distractor, PhaseStart, PhaseEnd
        public int    alvoIndex;
        public float  reactionTime;
        public string letra;
        public string cor;
        public bool   isTarget;
    }

    private readonly List<Evento> _eventos = new List<Evento>();
    private readonly Dictionary<string, List<float>> _reactionsPorFase = new Dictionary<string, List<float>>();
    private readonly Dictionary<string, (int acertos, int erros, int omissoes, int distratores, int alvos)> _contadores
        = new Dictionary<string, (int, int, int, int, int)>();

    // ---------- NOVO: acumulador de heatmap geral ----------
    private float[,] _heatmapGeral; // soma de todas as fases
    private int _hmW = 0, _hmH = 0;
    private float _hmMax = 0f;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnApplicationQuit()
    {
        if (salvarAutomaticamenteNoQuit)
        {
            try { SalvarArquivosCsv(); }
            catch (Exception e) { Debug.LogError($"[DataExport] Falha ao salvar no quit: {e}"); }
        }
    }

    // ---------- API de LOG ----------
    public void LogPhaseStart(string fase)
    {
        _eventos.Add(new Evento { utc = DateTime.UtcNow, fase = fase, tipo = "PhaseStart" });
        GarantirChaves(fase);
    }

    public void LogPhaseEnd(string fase)
    {
        _eventos.Add(new Evento { utc = DateTime.UtcNow, fase = fase, tipo = "PhaseEnd" });
        GarantirChaves(fase);
    }

    public void LogTargetShown(string fase, int alvoIndex, string letra = null, string cor = null)
    {
        _eventos.Add(new Evento {
            utc = DateTime.UtcNow, fase = fase, tipo = "TargetShown",
            alvoIndex = alvoIndex, letra = letra, cor = cor
        });
        GarantirChaves(fase);
        var c = _contadores[fase]; c.alvos += 1; _contadores[fase] = c;
    }

    public void LogHit(string fase, float reactionTime, string letra = null, string cor = null)
    {
        _eventos.Add(new Evento {
            utc = DateTime.UtcNow, fase = fase, tipo = "Hit",
            reactionTime = reactionTime, letra = letra, cor = cor, isTarget = true
        });
        GarantirChaves(fase);
        _reactionsPorFase[fase].Add(reactionTime);
        var c = _contadores[fase]; c.acertos += 1; _contadores[fase] = c;
    }

    public void LogError(string fase, float reactionTime = 0f, string letra = null, string cor = null, bool isTarget = false)
    {
        _eventos.Add(new Evento {
            utc = DateTime.UtcNow, fase = fase, tipo = "Error",
            reactionTime = reactionTime, letra = letra, cor = cor, isTarget = isTarget
        });
        GarantirChaves(fase);
        var c = _contadores[fase]; c.erros += 1; _contadores[fase] = c;
    }

    public void LogOmission(string fase, string letra = null, string cor = null)
    {
        _eventos.Add(new Evento {
            utc = DateTime.UtcNow, fase = fase, tipo = "Omission",
            letra = letra, cor = cor
        });
        GarantirChaves(fase);
        var c = _contadores[fase]; c.omissoes += 1; _contadores[fase] = c;
    }

    public void LogDistractor(string fase, string detalhe = null)
    {
        _eventos.Add(new Evento {
            utc = DateTime.UtcNow, fase = fase, tipo = "Distractor", letra = detalhe
        });
        GarantirChaves(fase);
        var c = _contadores[fase]; c.distratores += 1; _contadores[fase] = c;
    }

    // ---------- EXPORT CSVs (eventos + resumo) ----------
    public void SalvarArquivosCsv()
    {
        string root = GetOrCreateRoot();
        string carimbo = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string baseName = $"{estudoId}_{participanteId}_{sessaoId}_{carimbo}";

        string eventosPath = Path.Combine(root, baseName + "_events.csv");
        string resumoPath  = Path.Combine(root, baseName + "_summary.csv");

        SalvarEventosCsv(eventosPath);
        SalvarResumoCsv(resumoPath);

        AtualizarMarker(root, eventosPath, resumoPath, null);

        LastExportFolder = root;
        LastExportEventsPath = eventosPath;
        LastExportSummaryPath = resumoPath;

        Debug.Log($"[DataExport] Arquivos salvos:\n- Events : {eventosPath}\n- Summary: {resumoPath}\n- Marker : {Path.Combine(root, "last_export.txt")}");

        OnFilesSaved?.Invoke(eventosPath, resumoPath);
    }

    // ---------- NOVO: acumular + exportar HEATMAP ----------
    private void AcumularHeatmap(HeatmapArea area)
    {
        var (w, h, grid) = area.GetDensitySnapshot();

        if (_heatmapGeral == null)
        {
            _heatmapGeral = new float[w, h];
            _hmW = w; _hmH = h; _hmMax = 0f;
        }
        else
        {
            // se tamanhos diferentes, faz um aviso e ignora (ou poderia reamostrar)
            if (w != _hmW || h != _hmH)
            {
                Debug.LogWarning($"[DataExport] Tamanho do heatmap da fase ({w}x{h}) difere do acumulador ({_hmW}x{_hmH}). Acúmulo ignorado para esta fase.");
                return;
            }
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float v = grid[x, y];
                if (v <= 0f) continue;
                _heatmapGeral[x, y] += v;
                if (_heatmapGeral[x, y] > _hmMax) _hmMax = _heatmapGeral[x, y];
            }
        }
    }

    public (string pngPath, string csvPath) SalvarHeatmapDaFase(string fase, HeatmapArea area, bool salvarPng = true, bool salvarCsv = true)
    {
        if (area == null)
        {
            Debug.LogWarning("[DataExport] SalvarHeatmapDaFase chamado com HeatmapArea nulo.");
            return (null, null);
        }

        // NOVO: acumula esta fase no "Geral"
        AcumularHeatmap(area);

        string root = GetOrCreateRoot();
        string carimbo = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string baseName = $"{estudoId}_{participanteId}_{sessaoId}_{fase}_{carimbo}";

        string pngPath = null, csvPath = null;

        try
        {
            if (salvarPng)
            {
                pngPath = Path.Combine(root, baseName + "_heatmap.png");
                var bytes = area.EncodeToPNG();
                File.WriteAllBytes(pngPath, bytes);
                Debug.Log("[DataExport] Heatmap PNG salvo em: " + pngPath);
            }

            if (salvarCsv)
            {
                csvPath = Path.Combine(root, baseName + "_heatmap_bins.csv");
                area.ExportDensityCsv(csvPath);
                Debug.Log("[DataExport] Heatmap CSV salvo em: " + csvPath);
            }

            AtualizarMarker(root, LastExportEventsPath, LastExportSummaryPath, (pngPath, csvPath));
        }
        catch (Exception e)
        {
            Debug.LogError("[DataExport] Falha ao salvar heatmap: " + e);
        }

        return (pngPath, csvPath);
    }

    /// NOVO: salva o heatmap GERAL (soma de todas as fases) — chame no final do protocolo
    public (string pngPath, string csvPath) SalvarHeatmapGeral(HeatmapArea area, bool salvarPng = true, bool salvarCsv = true)
    {
        if (_heatmapGeral == null)
        {
            Debug.LogWarning("[DataExport] HeatmapGeral vazio. Nada para salvar.");
            return (null, null);
        }

        string root = GetOrCreateRoot();
        string carimbo = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string baseName = $"{estudoId}_{participanteId}_{sessaoId}_GERAL_{carimbo}";

        string pngPath = null, csvPath = null;

        try
        {
            if (salvarPng && area != null)
            {
                // Renderiza PNG usando o gradiente do HeatmapArea
                Texture2D tex = RenderTextureFromDensity(_heatmapGeral, _hmW, _hmH, area.colorGradient);
                pngPath = Path.Combine(root, baseName + "_heatmap.png");
                File.WriteAllBytes(pngPath, tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex);
                Debug.Log("[DataExport] Heatmap GERAL PNG salvo em: " + pngPath);
            }

            if (salvarCsv)
            {
                csvPath = Path.Combine(root, baseName + "_heatmap_bins.csv");
                ExportDensityCsv(_heatmapGeral, _hmW, _hmH, csvPath);
                Debug.Log("[DataExport] Heatmap GERAL CSV salvo em: " + csvPath);
            }

            AtualizarMarker(root, LastExportEventsPath, LastExportSummaryPath, (pngPath, csvPath));
        }
        catch (Exception e)
        {
            Debug.LogError("[DataExport] Falha ao salvar heatmap GERAL: " + e);
        }

        return (pngPath, csvPath);
    }

    // ---------- internos ----------
    private string GetOrCreateRoot()
    {
        string root = Path.Combine(Application.persistentDataPath, pastaRelativa);
        Directory.CreateDirectory(root);
        return root;
    }

    private void SalvarEventosCsv(string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("utc_iso,fase,tipo,alvoIndex,reactionTime_s,letra,cor,isTarget");

        foreach (var e in _eventos)
        {
            sb.Append(e.utc.ToString("o")).Append(',')
              .Append(Escape(e.fase)).Append(',')
              .Append(e.tipo).Append(',')
              .Append(e.alvoIndex).Append(',')
              .Append(e.reactionTime.ToString("G9", CultureInfo.InvariantCulture)).Append(',')
              .Append(Escape(e.letra)).Append(',')
              .Append(Escape(e.cor)).Append(',')
              .Append(e.isTarget ? "1" : "0").Append('\n');
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private void SalvarResumoCsv(string path)
    {
        var fases = _contadores.Keys.OrderBy(k => k).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("fase,alvos,acertos,erros,omissoes,distratores,media_rt_s,desvio_padrao_rt_s");

        foreach (var f in fases)
        {
            _reactionsPorFase.TryGetValue(f, out var rts);
            float media = 0f, dp = 0f;
            if (rts != null && rts.Count > 0)
            {
                media = rts.Average();
                float var = rts.Select(x => (x - media) * (x - media)).Sum() / rts.Count;
                dp = Mathf.Sqrt(var);
            }

            var c = _contadores[f];
            sb.Append(Escape(f)).Append(',')
              .Append(c.alvos).Append(',')
              .Append(c.acertos).Append(',')
              .Append(c.erros).Append(',')
              .Append(c.omissoes).Append(',')
              .Append(c.distratores).Append(',')
              .Append(media.ToString("G9", CultureInfo.InvariantCulture)).Append(',')
              .Append(dp.ToString("G9", CultureInfo.InvariantCulture)).Append('\n');
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private void GarantirChaves(string fase)
    {
        if (!_reactionsPorFase.ContainsKey(fase)) _reactionsPorFase[fase] = new List<float>();
        if (!_contadores.ContainsKey(fase)) _contadores[fase] = (0, 0, 0, 0, 0);
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private void AtualizarMarker(string root, string eventosPath, string resumoPath, (string png, string csv)? heatmap = null)
    {
        string markerPath = Path.Combine(root, "last_export.txt");
        var sb = new StringBuilder();

        if (File.Exists(markerPath))
        {
            try { sb.AppendLine(File.ReadAllText(markerPath)); }
            catch { /* ignore */ }
        }

        sb.AppendLine($"UTC: {DateTime.UtcNow:o}");
        if (!string.IsNullOrEmpty(eventosPath)) sb.AppendLine($"Events: {eventosPath}");
        if (!string.IsNullOrEmpty(resumoPath))  sb.AppendLine($"Summary: {resumoPath}");
        if (heatmap != null)
        {
            if (!string.IsNullOrEmpty(heatmap.Value.png)) sb.AppendLine($"HeatmapPNG: {heatmap.Value.png}");
            if (!string.IsNullOrEmpty(heatmap.Value.csv)) sb.AppendLine($"HeatmapCSV: {heatmap.Value.csv}");
        }
        sb.AppendLine(new string('-', 60));

        File.WriteAllText(markerPath, sb.ToString(), Encoding.UTF8);
    }

    // ------- helpers p/ render e CSV do heatmap geral -------
    private Texture2D RenderTextureFromDensity(float[,] grid, int w, int h, Gradient grad)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color32[w * h];

        // normaliza pelo maior visto
        float max = 0f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (grid[x, y] > max) max = grid[x, y];
        if (max <= 1e-6f) max = 1f;

        int i = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++, i++)
            {
                float t = grid[x, y] / max;
                if (t <= 0f)
                {
                    px[i] = new Color32(0, 0, 0, 0);
                }
                else
                {
                    Color c = grad != null ? grad.Evaluate(Mathf.Clamp01(t)) : new Color(t, 0f, 0f, t);
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(t) * 255f);
                    px[i] = new Color32(
                        (byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), a
                    );
                }
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false, false);
        return tex;
    }

    private void ExportDensityCsv(float[,] grid, int w, int h, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("x,y,density");
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = grid[x, y];
                if (d <= 0f) continue;
                sb.Append(x).Append(',').Append(y).Append(',').Append(d.ToString("G9")).AppendLine();
            }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}

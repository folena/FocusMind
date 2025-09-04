using UnityEngine;

public class GazeHeatmapRecorder : MonoBehaviour
{
    [Header("Entrada")]
    public Transform cameraTransform;   // Main Camera (XR)
    public LayerMask wallMask;          // Layer da parede (ex.: Wall)

    [Header("Saída")]
    public HeatmapArea heatmapArea;     // Componente no Quad

    [Header("Amostragem")]
    public float sampleHz = 30f;
    public float maxDistance = 100f;

    [Header("Estado")]
    public bool recordingOnStart = false;

    [Tooltip("Debug: numero total de amostras registradas nesta execução.")]
    public int totalSamples;

    private bool isRecording = false;
    private float sampleTimer = 0f;

    private void Awake()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        if (recordingOnStart) StartRecording();
    }

    private void Update()
    {
        if (!isRecording || cameraTransform == null || heatmapArea == null) return;

        sampleTimer += Time.deltaTime;
        float interval = 1f / Mathf.Max(1e-3f, sampleHz);
        while (sampleTimer >= interval)
        {
            SampleOnce();
            sampleTimer -= interval;
        }
    }

    private void SampleOnce()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        // Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.green, 0.1f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, wallMask))
        {
            if (hit.collider == heatmapArea.GetCollider())
            {
                Vector2 uv = hit.textureCoord;
                // Se parecer invertido, descomente:
                // uv.x = 1f - uv.x;
                // uv.y = 1f - uv.y;

                heatmapArea.AccumulateUV(uv, 1f);
                totalSamples++;
            }
        }
    }

    public void StartRecording()
    {
        isRecording = true;
        sampleTimer = 0f;
    }

    public void StopRecording()
    {
        isRecording = false;
    }

    public void ClearHeatmap()
    {
        heatmapArea?.ClearHeatmap();
        totalSamples = 0;
    }
}

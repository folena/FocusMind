using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class TesteManager : MonoBehaviour
{
    public enum TipoTarefa
    {
        AtencaoConcentrada,
        AtencaoAlternada,
        AtencaoSustentada
    }

    [Header("Controle de Fases")]
    public List<TipoTarefa> sequenciaDeFases = new List<TipoTarefa>();
    private int indiceFaseAtual = 0;

    public float tempoConcentrada = 120f;
    public float tempoAlternada = 120f;
    public float tempoSustentada = 90f;

    [Header("Referências")]
    public StimulusSpawner spawner;
    public ColorSpawner colorSpawner;
    public GameObject botaoIniciar;
    public TextMeshPro textoInstrucao;
    public ResultadoUIManager resultadoUI;

    [Header("Heatmap")]
    public GazeHeatmapRecorder gazeRecorder;     // arraste no Inspector
    public bool exportarHeatmapPNG = true;       // exporta PNG por fase e geral
    public bool exportarHeatmapCSV = true;       // exporta CSV por fase e geral

    [Header("Fluxo final")]
    public GameObject botaoFinalTrocarCena;      // <-- arraste seu botão aqui
    public string cenaDestinoAoFinal = "";       // opcional: define a cena aqui

    private float tempoFaseAtual;
    private bool faseAtiva;

    void Start()
    {
        if (gazeRecorder == null) gazeRecorder = FindObjectOfType<GazeHeatmapRecorder>(true);

        if (DataExportService.I != null)
        {
            DataExportService.I.OnFilesSaved += (eventsPath, summaryPath) =>
            {
                ExibirMensagemTemporaria($"Export salvo!\nEvents:\n{eventsPath}\nSummary:\n{summaryPath}", 5f);
            };
        }

        // Garantir que o botão final comece invisível
        if (botaoFinalTrocarCena != null)
        {
            botaoFinalTrocarCena.SetActive(false);

            // se quiser já configurar a cena destino no componente do botão
            if (!string.IsNullOrEmpty(cenaDestinoAoFinal))
            {
                var trocar = botaoFinalTrocarCena.GetComponent<BotaoMudarCena>();
                if (trocar != null) trocar.nomeCenaDestino = cenaDestinoAoFinal;
            }
        }

        if (sequenciaDeFases.Count > 0)
        {
            indiceFaseAtual = 0;
            PrepararFaseAtual();
        }
        else
        {
            if (botaoIniciar != null) botaoIniciar.SetActive(false);
        }
    }

    void Update()
    {
        if (faseAtiva)
        {
            tempoFaseAtual -= Time.deltaTime;
            if (tempoFaseAtual <= 0f)
            {
                EncerrarFaseAtual();
            }
        }
    }

    public void IniciarFase()
    {
        if (indiceFaseAtual >= sequenciaDeFases.Count) return;

        if (botaoIniciar != null) botaoIniciar.SetActive(false);
        if (textoInstrucao != null) textoInstrucao.gameObject.SetActive(false);

        TipoTarefa fase = sequenciaDeFases[indiceFaseAtual];
        tempoFaseAtual = ObterTempoDaFase(fase);

        DataExportService.I?.LogPhaseStart(fase.ToString());

        if (fase == TesteManager.TipoTarefa.AtencaoAlternada && colorSpawner != null)
        {
            colorSpawner.Iniciar();          // (no seu ColorSpawner, isso faz um Parar/Limpar)
        }
        
        spawner.ConfigurarParaFase(fase, tempoFaseAtual);
        spawner.IniciarTeste();

        if (gazeRecorder != null)
        {
            gazeRecorder.ClearHeatmap();
            gazeRecorder.StartRecording();
        }

        if (fase == TipoTarefa.AtencaoAlternada && colorSpawner != null)
        {
            colorSpawner.Iniciar();
        }

        faseAtiva = true;
    }

    void EncerrarFaseAtual()
    {
        if (!faseAtiva) return;
        faseAtiva = false;

        if (gazeRecorder != null)
        {
            gazeRecorder.StopRecording();

            if (gazeRecorder.heatmapArea != null && DataExportService.I != null)
            {
                string faseNome = sequenciaDeFases[indiceFaseAtual].ToString();
                DataExportService.I.SalvarHeatmapDaFase(
                    faseNome,
                    gazeRecorder.heatmapArea,
                    salvarPng: exportarHeatmapPNG,
                    salvarCsv: exportarHeatmapCSV
                );
            }
        }

        spawner.PararTeste();

        var faseEncerrada = sequenciaDeFases[indiceFaseAtual];
        DataExportService.I?.LogPhaseEnd(faseEncerrada.ToString());

        if (colorSpawner != null) colorSpawner.Parar();

        if (indiceFaseAtual >= sequenciaDeFases.Count - 1)
        {
            if (resultadoUI != null) resultadoUI.MostrarResultadosNaTela();

            // CSVs gerais (events/summary)
            DataExportService.I?.SalvarArquivosCsv();

            // HEATMAP GERAL (soma de todas as fases)
            if (gazeRecorder != null && gazeRecorder.heatmapArea != null)
            {
                DataExportService.I?.SalvarHeatmapGeral(
                    gazeRecorder.heatmapArea,
                    salvarPng: exportarHeatmapPNG,
                    salvarCsv: exportarHeatmapCSV
                );
            }

            // ATIVAR BOTÃO FINAL APÓS TUDO CONCLUÍDO
            if (botaoFinalTrocarCena != null)
            {
                // garante que o componente tem a cena correta, se você definiu aqui
                if (!string.IsNullOrEmpty(cenaDestinoAoFinal))
                {
                    var trocar = botaoFinalTrocarCena.GetComponent<BotaoMudarCena>();
                    if (trocar != null) trocar.nomeCenaDestino = cenaDestinoAoFinal;
                }

                botaoFinalTrocarCena.SetActive(true);
                ExibirMensagemTemporaria("Teste concluído! Toque no botão para continuar.", 4f);
            }
        }
        else
        {
            AvancarParaProximaFase();
        }
    }

    void AvancarParaProximaFase()
    {
        indiceFaseAtual++;
        PrepararFaseAtual();
    }

    void PrepararFaseAtual()
    {
        if (indiceFaseAtual >= sequenciaDeFases.Count) return;

        if (botaoIniciar != null)
            botaoIniciar.SetActive(true);

        if (textoInstrucao != null)
        {
            TipoTarefa fase = sequenciaDeFases[indiceFaseAtual];
            textoInstrucao.text = "Próxima fase: " + fase.ToString();
            textoInstrucao.gameObject.SetActive(true);
        }
    }

    float ObterTempoDaFase(TipoTarefa fase)
    {
        switch (fase)
        {
            case TipoTarefa.AtencaoConcentrada: return tempoConcentrada;
            case TipoTarefa.AtencaoAlternada:   return tempoAlternada;
            case TipoTarefa.AtencaoSustentada:  return tempoSustentada;
            default:                            return 60f;
        }
    }

    public void ExibirMensagemTemporaria(string mensagem, float duracao)
    {
        if (textoInstrucao != null)
        {
            StopAllCoroutines();
            StartCoroutine(MostrarTextoTemporario(mensagem, duracao));
        }
    }

    private System.Collections.IEnumerator MostrarTextoTemporario(string mensagem, float duracao)
    {
        textoInstrucao.text = mensagem;
        textoInstrucao.gameObject.SetActive(true);
        yield return new WaitForSeconds(duracao);
        textoInstrucao.gameObject.SetActive(false);
    }
}

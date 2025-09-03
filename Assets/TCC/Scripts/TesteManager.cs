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

    private float tempoFaseAtual;
    private bool faseAtiva;

    void Start()
    {
        if (DataExportService.I != null)
        {
            DataExportService.I.OnFilesSaved += (eventsPath, summaryPath) =>
            {
                // Exemplo simples: mensagem na UI
                ExibirMensagemTemporaria($"Export salvo!\nEvents:\n{eventsPath}\nSummary:\n{summaryPath}", 5f);
            };
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

        botaoIniciar.SetActive(false);
        textoInstrucao.gameObject.SetActive(false);

        TipoTarefa fase = sequenciaDeFases[indiceFaseAtual];
        tempoFaseAtual = ObterTempoDaFase(fase);

        // Export: início da fase
        DataExportService.I?.LogPhaseStart(fase.ToString());

        // Passa a duração da fase para o spawner preparar o plano de estímulos
        spawner.ConfigurarParaFase(fase, tempoFaseAtual);
        spawner.IniciarTeste();

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
        spawner.PararTeste();

        // Export: fim da fase
        var faseEncerrada = sequenciaDeFases[indiceFaseAtual];
        DataExportService.I?.LogPhaseEnd(faseEncerrada.ToString());

        if (colorSpawner != null)
        {
            colorSpawner.Parar();
        }

        if (indiceFaseAtual >= sequenciaDeFases.Count - 1)
        {
            if (resultadoUI != null)
            {
                resultadoUI.MostrarResultadosNaTela();
            }

            // Export: salva tudo ao final do protocolo
            DataExportService.I?.SalvarArquivosCsv();
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
            case TipoTarefa.AtencaoAlternada: return tempoAlternada;
            case TipoTarefa.AtencaoSustentada: return tempoSustentada;
            default: return 60f;
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

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Serialization; // Adicionado para usar List

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
    public GameObject botaoIniciar;
    public TextMeshPro textoInstrucao;
    public ResultadoUIManager resultadoUI;

    private float tempoFaseAtual;
    private bool faseAtiva;

    void Start()
    {
        // Garante que o índice é válido antes de preparar a fase
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

        botaoIniciar.SetActive(false);
        textoInstrucao.gameObject.SetActive(false);

        TipoTarefa fase = sequenciaDeFases[indiceFaseAtual];
        spawner.ConfigurarParaFase(fase);
        spawner.IniciarTeste();

        tempoFaseAtual = ObterTempoDaFase(fase);
        faseAtiva = true;
    }

    void EncerrarFaseAtual()
    {
        faseAtiva = false;
        spawner.PararTeste();

        // Verifica se o índice atual é o último da lista
        if (indiceFaseAtual >= sequenciaDeFases.Count - 1)
        {
            if (resultadoUI != null)
            {
                resultadoUI.MostrarResultadosNaTela();
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
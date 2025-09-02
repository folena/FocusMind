using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TesteManager : MonoBehaviour
{
    public enum TipoTarefa
    {
        AtencaoSeletiva,
        AtencaoAlternada,
        AtencaoDividida
    }

    [Header("Controle de Fases")]
    public TipoTarefa faseAtual = TipoTarefa.AtencaoSeletiva;
    public float tempoSeletiva = 120f;
    public float tempoAlternada = 120f;
    public float tempoDividida = 90f;

    [Header("Referências")]
    public StimulusSpawner spawner;
    public GameObject botaoIniciar;
    public TextMeshPro textoInstrucao;
    public ResultadoUIManager resultadoUI;

    private float tempoFaseAtual;
    private bool faseAtiva;

    void Start()
    {
        PrepararFaseAtual();
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
        botaoIniciar.SetActive(false);
        textoInstrucao.gameObject.SetActive(false);

        spawner.ConfigurarParaFase(faseAtual);
        spawner.IniciarTeste();

        tempoFaseAtual = ObterTempoDaFase(faseAtual);
        faseAtiva = true;
    }

    void EncerrarFaseAtual()
    {
        faseAtiva = false;
        spawner.PararTeste();

        if (faseAtual == TipoTarefa.AtencaoDividida)
        {
            Debug.Log("Todas as fases concluídas!");

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
        faseAtual = (TipoTarefa)(((int)faseAtual) + 1);
        PrepararFaseAtual();
    }

    void PrepararFaseAtual()
    {
        if (botaoIniciar != null)
            botaoIniciar.SetActive(true);

        if (textoInstrucao != null)
        {
            textoInstrucao.text = "Próxima fase: " + faseAtual.ToString();
            textoInstrucao.gameObject.SetActive(true);
        }
    }

    float ObterTempoDaFase(TipoTarefa fase)
    {
        switch (fase)
        {
            case TipoTarefa.AtencaoSeletiva: return tempoSeletiva;
            case TipoTarefa.AtencaoAlternada: return tempoAlternada;
            case TipoTarefa.AtencaoDividida: return tempoDividida;
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

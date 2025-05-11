using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TesteManager : MonoBehaviour
{
    public enum TipoTarefa
    {
        AtencaoConcentrada,
        AtencaoAlternada,
        AtencaoDividida
    }

    [Header("Controle de Fases")]
    public TipoTarefa faseAtual = TipoTarefa.AtencaoConcentrada;
    public float tempoConcentrada = 120f;
    public float tempoAlternada = 120f;
    public float tempoDividida = 90f;

    [Header("Referências")]
    public StimulusSpawner spawner;
    public GameObject botaoIniciar;
    public TextMeshPro textoInstrucao;

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
        if (botaoIniciar)
            botaoIniciar.SetActive(false);

        if (textoInstrucao)
            textoInstrucao.gameObject.SetActive(false);

        tempoFaseAtual = ObterTempoDaFase(faseAtual);
        faseAtiva = true;

        if (spawner)
        {
            spawner.ConfigurarParaFase(faseAtual);
            spawner.IniciarTeste();
        }
    }


    void EncerrarFaseAtual()
    {
        faseAtiva = false;
        spawner.PararTeste();

        Debug.Log($"✅ Fase {faseAtual} encerrada.");

        if (faseAtual == TipoTarefa.AtencaoDividida)
        {
            Debug.Log("🎉 Todas as fases concluídas!");
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
        if (botaoIniciar)
            botaoIniciar.SetActive(true);

        if (textoInstrucao)
        {
            textoInstrucao.text = "Próxima fase: " + faseAtual.ToString();
            textoInstrucao.gameObject.SetActive(true);
        }

        Debug.Log($"🕹 Pronto para iniciar fase: {faseAtual}");
    }

    public void ExibirMensagemTemporaria(string mensagem, float duracao)
    {
        StopAllCoroutines();
        StartCoroutine(MostrarTextoTemporario(mensagem, duracao));
    }

    private IEnumerator MostrarTextoTemporario(string mensagem, float duracao)
    {
        if (textoInstrucao)
        {
            textoInstrucao.text = mensagem;
            textoInstrucao.gameObject.SetActive(true);

            yield return new WaitForSeconds(duracao);

            textoInstrucao.gameObject.SetActive(false);
        }
    }
    
    float ObterTempoDaFase(TipoTarefa fase)
    {
        switch (fase)
        {
            case TipoTarefa.AtencaoConcentrada: return tempoConcentrada;
            case TipoTarefa.AtencaoAlternada: return tempoAlternada;
            case TipoTarefa.AtencaoDividida: return tempoDividida;
            default: return 60f;
        }
    }
}

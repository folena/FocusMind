using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR;

[System.Serializable]
public struct AlvoAlternado
{
    public GameObject letraPrefab;
    public GameObject corPrefab;
}

public class StimulusSpawner : MonoBehaviour
{
    [Header("Configuração de Estímulos")]
    public GameObject[] letrasAlvo;
    public GameObject[] letrasDistrator;
    public Transform[] pontosSpawn;

    [Header("Configuração Atenção Alternada")]
    public AlvoAlternado[] alvosDaFaseAlternada;
    public ColorSpawner colorSpawner;

    [Header("Controle de Tempo")]
    public float intervaloEntreEstimulos = 2f;
    public float delayEntreLetras = 0.5f;

    [Header("Controle de Quantidade de Alvos por Fase")]
    public int quantidadeAlvosConcentrada = 15;
    public int quantidadeAlvosSustentada = 10;
    public int quantidadeAlvosAlternada = 12;

    [Header("Referências")]
    public DistratorController distratorController;
    public TesteManager testeManager;
    public ResultadoUIManager resultadoUI;

    // Variáveis de estado
    private TesteManager.TipoTarefa faseAtual;
    private GameObject letraAtual;
    private bool podeInteragir = false;
    private bool testeAtivo = false;
    private float tempoDeAparecimento = 0f;
    private float tempoReacao = 0f;
    private string letraAlvoAtual = "A";
    private List<bool> planoDeEstimulos;
    private int indicePlanoAtual = 0;

    // Estatísticas
    private int acertos = 0;
    private int erros = 0;
    private int omissoes = 0;
    private int distratoresInteragidos = 0;

    public void IniciarTeste()
    {
        testeAtivo = true;
        StartCoroutine(ControlarEstimulos());

        if (distratorController != null)
            distratorController.IniciarDistratores(faseAtual);
    }

    void Update()
    {
        if (!testeAtivo || !podeInteragir || letraAtual == null) return;

        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed) && isPressed)
        {
            VerificarLetra();
        }
    }

    IEnumerator ControlarEstimulos()
    {
        while (testeAtivo)
        {
            SpawnLetra();
            yield return new WaitForSeconds(intervaloEntreEstimulos);

            VerificarOmissaoAtual();

            if (letraAtual != null)
            {
                Destroy(letraAtual);
                letraAtual = null;
            }
            if (faseAtual == TesteManager.TipoTarefa.AtencaoAlternada && colorSpawner != null)
            {
                colorSpawner.LimparCor();
            }

            yield return new WaitForSeconds(delayEntreLetras);
        }
    }

    void PrepararPlanoDeEstimulos(TesteManager.TipoTarefa fase, float duracaoFase)
    {
        planoDeEstimulos = new List<bool>();
        indicePlanoAtual = 0;

        int quantidadeAlvos = 0;
        switch (fase)
        {
            case TesteManager.TipoTarefa.AtencaoConcentrada:
                quantidadeAlvos = quantidadeAlvosConcentrada;
                break;
            case TesteManager.TipoTarefa.AtencaoSustentada:
                quantidadeAlvos = quantidadeAlvosSustentada;
                break;
            case TesteManager.TipoTarefa.AtencaoAlternada:
                quantidadeAlvos = quantidadeAlvosAlternada;
                break;
        }

        float tempoPorEstimulo = intervaloEntreEstimulos + delayEntreLetras;
        int totalEstimulos = Mathf.FloorToInt(duracaoFase / tempoPorEstimulo);
        int quantidadeDistratores = totalEstimulos - quantidadeAlvos;

        if (quantidadeDistratores < 0)
        {
            Debug.LogWarning("A quantidade de alvos configurada é maior que o total de estímulos possíveis. Todos os estímulos serão alvos.");
            quantidadeAlvos = totalEstimulos;
            quantidadeDistratores = 0;
        }

        for (int i = 0; i < quantidadeAlvos; i++) planoDeEstimulos.Add(true);
        for (int i = 0; i < quantidadeDistratores; i++) planoDeEstimulos.Add(false);

        for (int i = 0; i < planoDeEstimulos.Count; i++)
        {
            int randomIndex = Random.Range(i, planoDeEstimulos.Count);
            bool temp = planoDeEstimulos[i];
            planoDeEstimulos[i] = planoDeEstimulos[randomIndex];
            planoDeEstimulos[randomIndex] = temp;
        }
    }

    void SpawnLetra()
{
    if (indicePlanoAtual >= planoDeEstimulos.Count)
    {
        return;
    }

    bool gerarAlvo = planoDeEstimulos[indicePlanoAtual];
    indicePlanoAtual++;

    GameObject prefabLetra = null;
    bool isAlvo = false;

    if (gerarAlvo)
    {
        isAlvo = true;
        switch (faseAtual)
        {
            case TesteManager.TipoTarefa.AtencaoConcentrada:
            case TesteManager.TipoTarefa.AtencaoSustentada:
                prefabLetra = letrasAlvo.FirstOrDefault(l => l.name.Trim().ToUpper().Contains(letraAlvoAtual.ToUpper()));
                break;
            case TesteManager.TipoTarefa.AtencaoAlternada:
                if (alvosDaFaseAlternada.Length > 0)
                {
                    // Seleciona uma combinação válida de alvo/cor primeiro
                    AlvoAlternado alvoEscolhido = alvosDaFaseAlternada[Random.Range(0, alvosDaFaseAlternada.Length)];
                    prefabLetra = alvoEscolhido.letraPrefab;
                    
                    // Garante que a cor correspondente seja exibida
                    if (colorSpawner != null)
                    {
                        colorSpawner.SpawnCorEspecifica(alvoEscolhido.corPrefab);
                    }
                }
                else
                {
                    // Se não houver alvos configurados, trata como distrator
                    isAlvo = false; 
                }
                break;
        }
    }

    // Se não for para gerar um alvo, ou se a tentativa de gerar um alvo falhou
    if (!gerarAlvo || prefabLetra == null)
    {
        isAlvo = false;
        if (letrasDistrator.Length > 0)
        {
            prefabLetra = letrasDistrator[Random.Range(0, letrasDistrator.Length)];
        }
        
        // Garante que uma cor aleatória apareça para os distratores na fase alternada
        if (faseAtual == TesteManager.TipoTarefa.AtencaoAlternada && colorSpawner != null)
        {
            colorSpawner.SpawnNovaCor();
        }
    }

    if (prefabLetra == null)
    {
        Debug.LogError("Nenhum prefab de letra pôde ser selecionado. Verifique as configurações.");
        return;
    }

    Transform ponto = pontosSpawn[Random.Range(0, pontosSpawn.Length)];
    letraAtual = Instantiate(prefabLetra, ponto.position, ponto.rotation);

    LetraStimulo letraScript = letraAtual.GetComponent<LetraStimulo>();
    letraScript.isAlvo = isAlvo;

    if (isAlvo && resultadoUI != null)
    {
        resultadoUI.RegistrarLetraAlvo(faseAtual);
    }

    podeInteragir = true;
    tempoDeAparecimento = Time.time;
}

    public void RegistrarDistratorInteragido()
    {
        distratoresInteragidos++;
        Debug.Log("Distrator ativado! Total: " + distratoresInteragidos);

        if (resultadoUI != null)
            resultadoUI.RegistrarDistrator(faseAtual);
    }

    void VerificarOmissaoAtual()
    {
        if (letraAtual == null) return;

        LetraStimulo letraScript = letraAtual.GetComponent<LetraStimulo>();
        if (letraScript != null && letraScript.isAlvo && !letraScript.foiInteragido)
        {
            omissoes++;
            Debug.Log("Omissão! Total: " + omissoes);

            if (resultadoUI != null)
                resultadoUI.RegistrarOmissao(faseAtual);
        }
    }

    public void PararTeste()
    {
        StopAllCoroutines();
        podeInteragir = false;
        testeAtivo = false;

        if (letraAtual != null)
        {
            Destroy(letraAtual);
            letraAtual = null;
        }

        if (colorSpawner != null)
        {
            colorSpawner.Parar();
        }
    }

    public void ConfigurarParaFase(TesteManager.TipoTarefa fase, float duracaoFase)
    {
        faseAtual = fase;
        acertos = 0;
        erros = 0;
        omissoes = 0;
        distratoresInteragidos = 0;

        switch (faseAtual)
        {
            case TesteManager.TipoTarefa.AtencaoConcentrada:
            case TesteManager.TipoTarefa.AtencaoSustentada:
                letraAlvoAtual = "A";
                break;
            case TesteManager.TipoTarefa.AtencaoAlternada:
                break;
        }

        PrepararPlanoDeEstimulos(fase, duracaoFase);
    }

    void VerificarLetra()
    {
        if (letraAtual == null) return;

        LetraStimulo letraScript = letraAtual.GetComponent<LetraStimulo>();
        if (letraScript != null && !letraScript.foiInteragido)
        {
            letraScript.Interagir();
            tempoReacao = Time.time - tempoDeAparecimento;

            if (letraScript.isAlvo)
            {
                acertos++;
                Debug.Log($"Acerto! Tempo de reação: {tempoReacao:F2} segundos");

                if (resultadoUI != null)
                    resultadoUI.RegistrarAcerto(faseAtual, tempoReacao);
            }
            else
            {
                erros++;
                Debug.Log("Erro!");

                if (resultadoUI != null)
                    resultadoUI.RegistrarErro(faseAtual);
            }
        }
        podeInteragir = false;
    }
}
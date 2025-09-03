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
    public int quantidadeAlvosSustentada = 10;  // nº de aparições na Sustentada
    public int quantidadeAlvosAlternada = 12;

    [Header("Atenção Sustentada (alvo único piscante)")]
    public Transform pontoSustentada;                 // ponto fixo (lousa)
    public float sustentadaTempoVisivel = 2f;         // alvo visível por 2s
    public Vector2 sustentadaIntervaloOculta = new Vector2(4f, 8f); // intervalo aleatório oculto
    public bool sustentadaIntervaloFixo = false;      // usar intervalo fixo?
    public float sustentadaIntervaloFixoSeg = 5f;     // valor fixo do intervalo
    public bool contarErroQuandoOculta = false;       // contar erro se apertar fora da janela

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

    // Sustentada
    private Coroutine rotinaSustentada;
    private float sustentadaDuracaoFase = 0f; // vindo do TesteManager

    // Export
    private int contadorAlvosApresentados = 0; // reinicia por fase

    public void IniciarTeste()
    {
        testeAtivo = true;

        if (faseAtual == TesteManager.TipoTarefa.AtencaoSustentada)
        {
            rotinaSustentada = StartCoroutine(LoopSustentada());
        }
        else
        {
            StartCoroutine(ControlarEstimulos());
        }

        if (distratorController != null)
            distratorController.IniciarDistratores(faseAtual);
    }

    void Update()
    {
        if (!testeAtivo) return;

        // opcional: erro se apertar fora da janela na Sustentada
        if (faseAtual == TesteManager.TipoTarefa.AtencaoSustentada && contarErroQuandoOculta)
        {
            InputDevice deviceAll = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!podeInteragir && deviceAll.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed) && pressed)
            {
                erros++;
                resultadoUI?.RegistrarErro(faseAtual);
                DataExportService.I?.LogError(faseAtual.ToString(), 0f, null,
                    colorSpawner?.CorAtualPrefab ? colorSpawner.CorAtualPrefab.name : null, false);
            }
        }

        if (!podeInteragir || letraAtual == null) return;

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

    IEnumerator LoopSustentada()
    {
        // cria um único alvo fixo
        if (letraAtual == null)
        {
            GameObject prefabLetra = letrasAlvo.FirstOrDefault(l => l.name.Trim().ToUpper().Contains(letraAlvoAtual.ToUpper()));
            if (prefabLetra == null && letrasAlvo.Length > 0) prefabLetra = letrasAlvo[0];

            Transform ponto = pontoSustentada != null
                ? pontoSustentada
                : (pontosSpawn != null && pontosSpawn.Length > 0
                    ? pontosSpawn[Random.Range(0, pontosSpawn.Length)]
                    : transform);

            letraAtual = Instantiate(prefabLetra, ponto.position, ponto.rotation);

            var s = letraAtual.GetComponent<LetraStimulo>();
            if (s != null) { s.isAlvo = true; s.foiInteragido = false; }
        }

        // começa oculta
        SetLetraVisivel(false);
        podeInteragir = false;

        int restantes = Mathf.Max(0, quantidadeAlvosSustentada);
        float tRestante = Mathf.Max(0f, sustentadaDuracaoFase);

        if (restantes == 0)
        {
            if (tRestante > 0f) yield return new WaitForSeconds(tRestante);
            yield break;
        }

        while (testeAtivo && tRestante > 0f && restantes > 0)
        {
            // distribuir o slack para caberem as aparições restantes
            float slackMax = Mathf.Max(0f, tRestante - (restantes * sustentadaTempoVisivel));
            float maxEsperaParaEsta = slackMax / restantes;

            float esperaDesejada = sustentadaIntervaloFixo
                ? sustentadaIntervaloFixoSeg
                : Random.Range(sustentadaIntervaloOculta.x, sustentadaIntervaloOculta.y);

            float espera = Mathf.Min(esperaDesejada, maxEsperaParaEsta);

            if (espera > 0f)
            {
                yield return new WaitForSeconds(espera);
                tRestante -= espera;
            }

            if (!testeAtivo || tRestante <= 0f) break;

            // Mostrar alvo
            ResetInteracaoAtual();
            SetLetraVisivel(true);
            podeInteragir = true;
            tempoDeAparecimento = Time.time;

            resultadoUI?.RegistrarLetraAlvo(faseAtual);
            contadorAlvosApresentados++;
            DataExportService.I?.LogTargetShown(faseAtual.ToString(), contadorAlvosApresentados, letraAlvoAtual,
                colorSpawner?.CorAtualPrefab ? colorSpawner.CorAtualPrefab.name : null);

            float vis = Mathf.Min(sustentadaTempoVisivel, tRestante);
            if (vis > 0f) yield return new WaitForSeconds(vis);

            // Omissão se não interagir
            var ls = letraAtual != null ? letraAtual.GetComponent<LetraStimulo>() : null;
            if (ls != null && !ls.foiInteragido)
            {
                omissoes++;
                resultadoUI?.RegistrarOmissao(faseAtual);
                DataExportService.I?.LogOmission(faseAtual.ToString(), letraAlvoAtual,
                    colorSpawner?.CorAtualPrefab ? colorSpawner.CorAtualPrefab.name : null);
            }

            // Oculta de novo
            SetLetraVisivel(false);
            podeInteragir = false;

            tRestante -= vis;
            restantes--;
        }

        // Se sobrou tempo, apenas aguarda oculto (TesteManager encerra pelo tempo)
        if (testeAtivo && tRestante > 0f)
        {
            yield return new WaitForSeconds(tRestante);
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
            Debug.LogWarning("Alvos configurados > total de estímulos possíveis. Todos serão alvos.");
            quantidadeAlvos = totalEstimulos;
            quantidadeDistratores = 0;
        }

        for (int i = 0; i < quantidadeAlvos; i++) planoDeEstimulos.Add(true);
        for (int i = 0; i < quantidadeDistratores; i++) planoDeEstimulos.Add(false);

        // Shuffle
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
        if (indicePlanoAtual >= planoDeEstimulos.Count) return;

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
                        AlvoAlternado alvoEscolhido = alvosDaFaseAlternada[Random.Range(0, alvosDaFaseAlternada.Length)];
                        prefabLetra = alvoEscolhido.letraPrefab;

                        if (colorSpawner != null)
                        {
                            colorSpawner.SpawnCorEspecifica(alvoEscolhido.corPrefab);
                        }
                    }
                    else
                    {
                        isAlvo = false;
                    }
                    break;
            }
        }

        if (!gerarAlvo || prefabLetra == null)
        {
            isAlvo = false;
            if (letrasDistrator.Length > 0)
            {
                prefabLetra = letrasDistrator[Random.Range(0, letrasDistrator.Length)];
            }

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

        if (isAlvo)
        {
            resultadoUI?.RegistrarLetraAlvo(faseAtual);
            contadorAlvosApresentados++;
            DataExportService.I?.LogTargetShown(faseAtual.ToString(), contadorAlvosApresentados, letraAlvoAtual,
                colorSpawner?.CorAtualPrefab ? colorSpawner.CorAtualPrefab.name : null);
        }

        podeInteragir = true;
        tempoDeAparecimento = Time.time;
    }

    public void RegistrarDistratorInteragido()
    {
        distratoresInteragidos++;
        Debug.Log("Distrator ativado! Total: " + distratoresInteragidos);

        resultadoUI?.RegistrarDistrator(faseAtual);
        DataExportService.I?.LogDistractor(faseAtual.ToString(), "hit");
    }

    void VerificarOmissaoAtual()
    {
        if (letraAtual == null) return;

        LetraStimulo letraScript = letraAtual.GetComponent<LetraStimulo>();
        if (letraScript != null && letraScript.isAlvo && !letraScript.foiInteragido)
        {
            omissoes++;
            Debug.Log("Omissão! Total: " + omissoes);

            resultadoUI?.RegistrarOmissao(faseAtual);
            DataExportService.I?.LogOmission(faseAtual.ToString(), letraAlvoAtual,
                colorSpawner?.CorAtualPrefab ? colorSpawner.CorAtualPrefab.name : null);
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
        contadorAlvosApresentados = 0;

        switch (faseAtual)
        {
            case TesteManager.TipoTarefa.AtencaoConcentrada:
            case TesteManager.TipoTarefa.AtencaoSustentada:
                letraAlvoAtual = "A";
                sustentadaDuracaoFase = duracaoFase; // usado no loop da sustentada
                break;
            case TesteManager.TipoTarefa.AtencaoAlternada:
                sustentadaDuracaoFase = 0f;
                break;
        }

        if (faseAtual != TesteManager.TipoTarefa.AtencaoSustentada)
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
                Debug.Log($"Acerto! Tempo de reação: {tempoReacao:F2} s");

                resultadoUI?.RegistrarAcerto(faseAtual, tempoReacao);
                DataExportService.I?.LogHit(faseAtual.ToString(), tempoReacao, letraAlvoAtual,
                    colorSpawner?.CorAtualPrefab ? colorSpawner.CorAtualPrefab.name : null);
            }
            else
            {
                erros++;
                Debug.Log("Erro!");

                resultadoUI?.RegistrarErro(faseAtual);
                DataExportService.I?.LogError(faseAtual.ToString(), 0f, null,
                    colorSpawner?.CorAtualPrefab ? colorSpawner.CorAtualPrefab.name : null, false);
            }
        }
        podeInteragir = false;
    }

    void SetLetraVisivel(bool visivel)
    {
        if (letraAtual == null) return;
        letraAtual.SetActive(visivel);
    }

    void ResetInteracaoAtual()
    {
        if (letraAtual == null) return;
        var s = letraAtual.GetComponent<LetraStimulo>();
        if (s != null) s.foiInteragido = false;
    }
}

using UnityEngine.XR;
using UnityEngine;
using System.Collections;
using System.Linq;

public class StimulusSpawner : MonoBehaviour
{
    public GameObject[] letrasAlvo;
    public GameObject[] letrasDistrator;
    public Transform[] pontosSpawn;

    public float intervaloEntreEstimulos = 2f;
    public float delayEntreLetras = 0.5f;
    private float tempoDeAparecimento;
    private float tempoReacao;

    private string letraAlvoAtual = "A";
    private float tempoInicioFase;
    private int indiceLetraAtual;
    private string[] letrasAlternadas = { "A", "E", "F", "A" }; // 4 blocos de 30s

    private GameObject letraAtual;
    private bool podeInteragir;
    private bool testeAtivo;
    private GameObject ultimaLetraPrefab;

    private int acertos;
    private int erros;
    private int omissoes;
    private int distratoresInteragidos;

    public DistratorController distratorController;
    public TesteManager.TipoTarefa faseAtual = TesteManager.TipoTarefa.AtencaoConcentrada;
    public TesteManager testeManager; // nova referencia

    public void IniciarTeste()
    {
        testeAtivo = true;
        StartCoroutine(ControlarEstimulos());

        if (distratorController)
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

            if (letraAtual)
                Destroy(letraAtual);

            yield return StartCoroutine(PiscarAntesDaProxima());
        }
    }

    void SpawnLetra()
    {
        if (letraAtual)
            Destroy(letraAtual);

        // Atualiza o alvo se estiver na fase alternada
        if (faseAtual == TesteManager.TipoTarefa.AtencaoAlternada)
        {
            float tempoDecorrido = Time.time - tempoInicioFase;
            int novoIndice = Mathf.FloorToInt(tempoDecorrido / 30f);

            if (novoIndice != indiceLetraAtual && novoIndice < letrasAlternadas.Length)
            {
                indiceLetraAtual = novoIndice;
                letraAlvoAtual = letrasAlternadas[indiceLetraAtual];
                Debug.Log($"🔁 Novo alvo: {letraAlvoAtual} (t = {tempoDecorrido:F0}s)");

                if (testeManager&& testeManager.textoInstrucao)
                {
                    testeManager.ExibirMensagemTemporaria("Novo alvo: " + letraAlvoAtual, 2f);
                }
            }
        }

        GameObject[] todasAsLetras = letrasAlvo.Concat(letrasDistrator).ToArray();

        int tentativasSorteio = 0;
        int maxTentativas = 10;
        GameObject prefabSelecionado = null;

        do
        {
            prefabSelecionado = todasAsLetras[Random.Range(0, todasAsLetras.Length)];
            tentativasSorteio++;
        }
        while (prefabSelecionado == ultimaLetraPrefab && tentativasSorteio < maxTentativas);

        ultimaLetraPrefab = prefabSelecionado;

        string letraNome = prefabSelecionado.name.Trim().ToUpper();
        bool isAlvo = false;

        if (faseAtual == TesteManager.TipoTarefa.AtencaoConcentrada ||
            faseAtual == TesteManager.TipoTarefa.AtencaoAlternada)
        {
            isAlvo = letraNome == $"LETRA_{letraAlvoAtual.ToUpper()}";
        }
        else if (faseAtual == TesteManager.TipoTarefa.AtencaoDividida)
        {
            isAlvo = Random.value > 0.5f;
        }

        Transform ponto = pontosSpawn[Random.Range(0, pontosSpawn.Length)];
        letraAtual = Instantiate(prefabSelecionado, ponto.position, ponto.rotation);

        LetraStimulo letraScript = letraAtual.GetComponent<LetraStimulo>();
        letraScript.isAlvo = isAlvo;

        podeInteragir = true;
        tempoDeAparecimento = Time.time;
    }

    IEnumerator PiscarAntesDaProxima()
    {
        float blinkTime = 0.5f;
        float totalBlinkDuration = delayEntreLetras;
        float elapsed = 0f;

        foreach (Transform ponto in pontosSpawn)
        {
            Renderer renderer = ponto.GetComponent<Renderer>();
            if (renderer)
                renderer.enabled = false;
        }

        while (elapsed < totalBlinkDuration)
        {
            foreach (Transform ponto in pontosSpawn)
            {
                Renderer renderer = ponto.GetComponent<Renderer>();
                if (renderer)
                    renderer.enabled = !renderer.enabled;
            }

            yield return new WaitForSeconds(blinkTime);
            elapsed += blinkTime;
        }

        foreach (Transform ponto in pontosSpawn)
        {
            Renderer renderer = ponto.GetComponent<Renderer>();
            if (renderer)
                renderer.enabled = true;
        }
    }

    public void RegistrarDistratorInteragido()
    {
        distratoresInteragidos++;
        Debug.Log("Distrator ativado! Total: " + distratoresInteragidos);
    }

    void VerificarOmissaoAtual()
    {
        if (letraAtual == null) return;

        LetraStimulo letraScript = letraAtual.GetComponent<LetraStimulo>();
        if (letraScript&& letraScript.isAlvo && !letraScript.foiInteragido)
        {
            omissoes++;
            Debug.Log("Omissão! Total: " + omissoes);
        }
    }

    public void PararTeste()
    {
        StopAllCoroutines();
        podeInteragir = false;
        testeAtivo = false;

        if (letraAtual)
        {
            Destroy(letraAtual);
            letraAtual = null;
        }
    }

    public void ConfigurarParaFase(TesteManager.TipoTarefa fase)
    {
        faseAtual = fase;

        switch (faseAtual)
        {
            case TesteManager.TipoTarefa.AtencaoConcentrada:
                letraAlvoAtual = "A";
                break;

            case TesteManager.TipoTarefa.AtencaoAlternada:
                letraAlvoAtual = letrasAlternadas[0];
                indiceLetraAtual = 0;
                tempoInicioFase = Time.time;
                break;

            case TesteManager.TipoTarefa.AtencaoDividida:
                // futura lógica de spawn duplo
                break;
        }
    }

    void VerificarLetra()
    {
        if (letraAtual == null) return;

        LetraStimulo letraScript = letraAtual.GetComponent<LetraStimulo>();
        if (letraScript && !letraScript.foiInteragido)
        {
            letraScript.Interagir();
            tempoReacao = Time.time - tempoDeAparecimento;

            if (letraScript.isAlvo)
            {
                acertos++;
                Debug.Log($"Acerto! Tempo de reação: {tempoReacao:F2} segundos");
            }
            else
            {
                erros++;
                Debug.Log("Erro!");
            }
        }

        podeInteragir = false;
    }
}

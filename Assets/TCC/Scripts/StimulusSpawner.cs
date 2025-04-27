using UnityEngine.XR;
using UnityEngine;
using System.Collections;

public class StimulusSpawner : MonoBehaviour
{
    [Header("Letras")]
    public GameObject[] letrasAlvo;
    public GameObject[] letrasDistrator;
    public Transform[] pontosSpawn;

    [Header("Tempo")]
    public float intervaloEntreEstimulos = 2f;
    public float delayEntreLetras = 0.5f;          

    [Header("Distratores")]
    public DistratorController distratorController;

    private GameObject letraAtual;
    private bool podeInteragir = false;
    private bool testeAtivo = false;

    private GameObject ultimaLetraPrefab = null;
    
    private int acertos = 0;
    private int erros = 0;
    private int omissoes = 0;
    private int distratoresInteragidos = 0;
    
    public void IniciarTeste()
    {
        testeAtivo = true;
        StartCoroutine(ControlarEstimulos());

        if (distratorController != null)
        {
            distratorController.IniciarDistratores();
        }
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

            yield return new WaitForSeconds(intervaloEntreEstimulos); // tempo que a letra fica visível

            VerificarOmissaoAtual();

            if (letraAtual != null)
                Destroy(letraAtual);

            yield return StartCoroutine(PiscarAntesDaProxima()); // piscada + delay
        }
    }

    void SpawnLetra()
    {
        if (letraAtual != null)
            Destroy(letraAtual);

        bool isAlvo = Random.value > 0.5f;
        GameObject prefabSelecionado = null;

        int tentativas = 0;
        int maxTentativas = 10;

        do
        {
            prefabSelecionado = isAlvo
                ? letrasAlvo[Random.Range(0, letrasAlvo.Length)]
                : letrasDistrator[Random.Range(0, letrasDistrator.Length)];

            tentativas++;
        }
        while (prefabSelecionado == ultimaLetraPrefab && tentativas < maxTentativas);

        ultimaLetraPrefab = prefabSelecionado;

        Transform ponto = pontosSpawn[Random.Range(0, pontosSpawn.Length)];
        letraAtual = Instantiate(prefabSelecionado, ponto.position, ponto.rotation);

        LetraStimulo letraScript = letraAtual.GetComponent<LetraStimulo>();
        letraScript.isAlvo = isAlvo;
        podeInteragir = true;
    }

    IEnumerator PiscarAntesDaProxima()
    {
        float blinkTime = 0.5f;
        float totalBlinkDuration = delayEntreLetras;
        float elapsed = 0f;

        // Aqui você pode ajustar para todos os pontos de spawn piscarem ou um objeto específico
        foreach (Transform ponto in pontosSpawn)
        {
            Renderer renderer = ponto.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = false; // esconde temporariamente, se tiver renderizador
        }

        while (elapsed < totalBlinkDuration)
        {
            foreach (Transform ponto in pontosSpawn)
            {
                Renderer renderer = ponto.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.enabled = !renderer.enabled; // pisca on/off
            }

            yield return new WaitForSeconds(blinkTime);
            elapsed += blinkTime;
        }

        // Garante que ao final o render fique ativado
        foreach (Transform ponto in pontosSpawn)
        {
            Renderer renderer = ponto.GetComponent<Renderer>();
            if (renderer != null)
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
        if (letraScript != null)
        {
            if (letraScript.isAlvo && !letraScript.foiInteragido)
            {
                omissoes++;
                Debug.Log("Omissão! Total: " + omissoes);
            }
        }
    }


    void VerificarLetra()
    {
        if (letraAtual == null) return;

        LetraStimulo letraScript = letraAtual.GetComponent<LetraStimulo>();
        if (letraScript != null && !letraScript.foiInteragido) // Garante que só conta uma vez
        {
            letraScript.Interagir();

            if (letraScript.isAlvo)
            {
                acertos++;
                Debug.Log("Acerto! Total: " + acertos);
            }
            else
            {
                erros++;
                Debug.Log("Erro! Total: " + erros);
            }
        }

        podeInteragir = false;
    }
}

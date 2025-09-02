using UnityEngine;
using System.Collections;

[System.Serializable]
public class Distrator
{
    public AudioSource audioSource;
    public AudioClip somAlarme;
    public GameObject hitboxDistrator;
    public float tempoInicial;
}

public class DistratorController : MonoBehaviour
{
    [Header("Som ambiente continuo")]
    public AudioSource somAmbiente;
    public AudioClip clipAmbiente;

    [Header("Distratores por Fase")]
    public Distrator[] distratoresConcentrada;
    public Distrator[] distratoresAlternada;
    public Distrator[] distratoresSustentada;

    void Start()
    {
        if (somAmbiente&& clipAmbiente)
        {
            somAmbiente.clip = clipAmbiente;
            somAmbiente.loop = true;
            somAmbiente.Play();
        }

        DesativarTodosDistratores();
    }

    public void IniciarDistratores(TesteManager.TipoTarefa fase)
    {
        DesativarTodosDistratores();

        Distrator[] listaSelecionada = null;

        switch (fase)
        {
            case TesteManager.TipoTarefa.AtencaoConcentrada:
                listaSelecionada = distratoresConcentrada;
                break;
            case TesteManager.TipoTarefa.AtencaoAlternada:
                listaSelecionada = distratoresAlternada;
                break;
            case TesteManager.TipoTarefa.AtencaoSustentada:
                listaSelecionada = distratoresSustentada;
                break;
        }

        if (listaSelecionada != null)
        {
            foreach (Distrator d in listaSelecionada)
            {
                StartCoroutine(AtivarDistratorComDelay(d));
            }
        }
    }

    private IEnumerator AtivarDistratorComDelay(Distrator d)
    {
        yield return new WaitForSeconds(d.tempoInicial);

        if (d.hitboxDistrator)
            d.hitboxDistrator.SetActive(true);

        if (d.audioSource&& d.somAlarme)
        {
            d.audioSource.clip = d.somAlarme;
            d.audioSource.loop = false;
            d.audioSource.Play();

            yield return new WaitForSeconds(d.somAlarme.length);

            d.audioSource.Stop();
        }

        if (d.hitboxDistrator)
            d.hitboxDistrator.SetActive(false);
    }

    private void DesativarTodosDistratores()
    {
        foreach (var d in distratoresConcentrada)
            if (d.hitboxDistrator) d.hitboxDistrator.SetActive(false);

        foreach (var d in distratoresAlternada)
            if (d.hitboxDistrator) d.hitboxDistrator.SetActive(false);

        foreach (var d in distratoresSustentada)
            if (d.hitboxDistrator) d.hitboxDistrator.SetActive(false);
    }
}

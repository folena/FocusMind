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
    [Header("Som ambiente contínuo")]
    public AudioSource somAmbiente;
    public AudioClip clipAmbiente;

    [Header("Distratores múltiplos")]
    public Distrator[] distratores;

    void Start()
    {
        if (somAmbiente != null && clipAmbiente != null)
        {
            somAmbiente.clip = clipAmbiente;
            somAmbiente.loop = true;
            somAmbiente.Play();
        }

        foreach (Distrator d in distratores)
        {
            if (d.hitboxDistrator != null)
                d.hitboxDistrator.SetActive(false);
        }
    }

    public void IniciarDistratores()
    {
        foreach (Distrator d in distratores)
        {
            StartCoroutine(AtivarDistratorComDelay(d));
        }
    }

    private IEnumerator AtivarDistratorComDelay(Distrator d)
    {
        yield return new WaitForSeconds(d.tempoInicial);

        // Ativa a hitbox
        if (d.hitboxDistrator != null)
            d.hitboxDistrator.SetActive(true);

        // Toca o som do alarme
        if (d.audioSource != null && d.somAlarme != null)
        {
            d.audioSource.clip = d.somAlarme;
            d.audioSource.loop = false;
            d.audioSource.Play();

            // Espera o tempo de duração do som
            yield return new WaitForSeconds(d.somAlarme.length);

            d.audioSource.Stop();
        }

        // Desativa a hitbox
        if (d.hitboxDistrator != null)
            d.hitboxDistrator.SetActive(false);
    }
}





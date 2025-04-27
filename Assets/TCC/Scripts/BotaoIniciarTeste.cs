using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BotaoIniciarTeste : MonoBehaviour
{
    public StimulusSpawner spawner; // arraste o objeto com o spawner aqui

    private void Start()
    {
        // Garante que o bot�o possa ser clicado
        GetComponent<XRBaseInteractable>().selectEntered.AddListener(IniciarTeste);
    }

    private void IniciarTeste(SelectEnterEventArgs args)
    {
        Debug.Log("Iniciando teste...");
        spawner.IniciarTeste();

        // Opcional: desativa o bot�o ap�s clique
        gameObject.SetActive(false);
    }
}

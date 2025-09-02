using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BotaoIniciarTeste : MonoBehaviour
{
    public TesteManager testeManager; // <- arraste o GameObject com TesteManager no Inspector

    private void Start()
    {
        GetComponent<XRBaseInteractable>().selectEntered.AddListener(IniciarFase);
    }

    private void IniciarFase(SelectEnterEventArgs args)
    {
        testeManager.IniciarFase();

        gameObject.SetActive(false);
    }
}
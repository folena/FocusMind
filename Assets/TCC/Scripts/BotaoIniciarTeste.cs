using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BotaoIniciarTeste : MonoBehaviour
{
    public TesteManager testeManager; 
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
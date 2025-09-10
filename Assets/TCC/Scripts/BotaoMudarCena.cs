using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(XRBaseInteractable))]
public class BotaoMudarCena : MonoBehaviour
{
    [Header("Cena de destino (deve estar no Build Settings)")]
    public string nomeCenaDestino = "NomeDaCena";

    private XRBaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!string.IsNullOrEmpty(nomeCenaDestino))
        {
            Debug.Log($"[BotaoMudarCena] Carregando cena: {nomeCenaDestino}");
            SceneManager.LoadScene(nomeCenaDestino);
        }
        else
        {
            Debug.LogWarning("[BotaoMudarCena] Nome da cena destino não definido!");
        }
    }
}
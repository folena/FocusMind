using UnityEngine;

public class LookToDestroy : MonoBehaviour
{
    public Camera playerCamera;                  // A câmera do jogador
    public float maxDistance = 10f;              // Distância máxima para detectar o objeto
    public string targetTag = "Distrator";       // Tag usada para os distratores (ajuste se necessário)

    void Update()
    {
        CheckForObject();
    }

    void CheckForObject()
    {
        int layerMask = ~LayerMask.GetMask("Ignore Raycast");

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, layerMask))
        {
            if (hit.collider.CompareTag(targetTag))
            {
                // Garante que só registra uma vez (desativa a hitbox)
                hit.collider.gameObject.SetActive(false);

                // Chama o StimulusSpawner para registrar a interação com o distrator
                StimulusSpawner spawner = FindObjectOfType<StimulusSpawner>();
                if (spawner != null)
                {
                    spawner.RegistrarDistratorInteragido();
                }

                Debug.Log("Distrator ativado: " + hit.collider.gameObject.name);
            }
        }
    }
}
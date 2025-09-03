using UnityEngine;

public class LookToDestroy : MonoBehaviour
{
    public Camera playerCamera;
    public float maxDistance = 10f;
    public string targetTag = "Distrator";

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
                hit.collider.gameObject.SetActive(false);

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
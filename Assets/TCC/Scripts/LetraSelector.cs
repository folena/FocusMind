using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class LetraSelector : MonoBehaviour
{
    public XRController controller; // Controlador XR (Right ou Left)
    public LayerMask letraMask; // Camada das letras

    void Update()
    {
        if (controller.inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed) && isPressed)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 10f, letraMask))
            {
                // Se bateu em algo com LetraStimulo
                LetraStimulo letra = hit.collider.GetComponent<LetraStimulo>();
                if (letra != null)
                {
                    letra.Interagir(); // Chama a interação
                }
            }
        }
    }
}

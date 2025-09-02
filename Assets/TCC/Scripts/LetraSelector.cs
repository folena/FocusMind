using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class LetraSelector : MonoBehaviour
{
    public XRController controller;
    public LayerMask letraMask;

    void Update()
    {
        if (controller.inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed) && isPressed)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 10f, letraMask))
            {
                LetraStimulo letra = hit.collider.GetComponent<LetraStimulo>();
                if (letra != null)
                {
                    letra.Interagir();
                }
            }
        }
    }
}

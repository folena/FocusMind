using UnityEngine;

public class LetraStimulo : MonoBehaviour
{
    public bool isAlvo;
    public bool foiInteragido = false;

    public void Interagir()
    {
        if (foiInteragido) return;

        foiInteragido = true;
    }
}

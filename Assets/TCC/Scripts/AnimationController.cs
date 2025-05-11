using UnityEngine;
using System.Collections;

public class AnimationController : MonoBehaviour
{
    public Animator animator;
    public Transform pontoPorta;   // Primeiro destino: porta
    public Transform pontoFinal;  // Segundo destino: final
    public float velocidade = 2f;
    public Animator portaAnimator; // arraste o Animator da porta aqui no Inspector

    private enum Estado { IndoParaPorta, AbrindoPorta, IndoParaFinal, Parado }
    private Estado estadoAtual = Estado.IndoParaPorta;

    private bool executandoCoroutine = false; // Prevê chamadas múltiplas

    void Update()
    {
        switch (estadoAtual)
        {
            case Estado.IndoParaPorta:
                MoverAte(pontoPorta.position, Estado.AbrindoPorta);
                break;

            case Estado.IndoParaFinal:
                MoverAte(pontoFinal.position, Estado.Parado);
                break;
        }
    }

    void MoverAte(Vector3 destino, Estado proximoEstado)
    {
        float distancia = Vector3.Distance(transform.position, destino);

        if (distancia > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidade * Time.deltaTime);
            transform.LookAt(destino);
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking", false);

            // Garante que não chamamos a coroutine várias vezes
            if (!executandoCoroutine)
            {
                StartCoroutine(EsperarAntesDeMudarEstado(proximoEstado));
            }
        }
    }

    IEnumerator EsperarAntesDeMudarEstado(Estado proximoEstado)
    {
        executandoCoroutine = true;

        yield return new WaitForSeconds(0.2f);

        if (estadoAtual == Estado.AbrindoPorta)
        {
            executandoCoroutine = false;
            yield break;
        }

        if (proximoEstado == Estado.AbrindoPorta)
        {
            estadoAtual = Estado.AbrindoPorta;
            animator.SetTrigger("abrirPorta");

            // Aguarda a animação começar
            yield return new WaitUntil(() =>
                animator.GetCurrentAnimatorStateInfo(0).IsName("AbrirPorta"));

            // IMPORTANTE: reseta o trigger para evitar repetição
            animator.ResetTrigger("abrirPorta");

            // Aguarda a animação terminar
            yield return new WaitUntil(() =>
                !animator.GetCurrentAnimatorStateInfo(0).IsName("AbrirPorta"));

            estadoAtual = Estado.IndoParaFinal;
        }
        else
        {
            estadoAtual = proximoEstado;
        }

        executandoCoroutine = false;
    }
}

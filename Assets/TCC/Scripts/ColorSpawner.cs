// ColorSpawner.cs
using UnityEngine;

public class ColorSpawner : MonoBehaviour
{
    [Header("Configuração das Cores")]
    public Transform[] pontosSpawnCor;
    public GameObject[] corPrefabs;

    public GameObject CorAtualPrefab { get; private set; }
    private GameObject objetoCorInstanciado;

    // Chamado pelo StimulusSpawner para garantir a sincronização
    public void SpawnNovaCor()
    {
        if (objetoCorInstanciado != null)
        {
            Destroy(objetoCorInstanciado);
        }

        if (corPrefabs.Length == 0)
        {
            return;
        }

        // Sorteia e guarda o prefab da cor
        CorAtualPrefab = corPrefabs[Random.Range(0, corPrefabs.Length)];
        
        // Sorteia o local e instancia a cor
        Transform pontoSpawn = pontosSpawnCor[Random.Range(0, pontosSpawnCor.Length)];
        objetoCorInstanciado = Instantiate(CorAtualPrefab, pontoSpawn.position, pontoSpawn.rotation);
    }
    
    public void SpawnCorEspecifica(GameObject corPrefab)
    {
        LimparCor();
        if (corPrefab != null && pontosSpawnCor.Length > 0)
        {
            CorAtualPrefab = corPrefab;
            Transform pontoSpawn = pontosSpawnCor[Random.Range(0, pontosSpawnCor.Length)];
            objetoCorInstanciado = Instantiate(CorAtualPrefab, pontoSpawn.position, pontoSpawn.rotation);
        }
    }

    public void Iniciar()
    {
        Parar();
    }
    
    public void LimparCor()
    {
        if (objetoCorInstanciado != null)
        {
            Destroy(objetoCorInstanciado);
            objetoCorInstanciado = null;
        }
        CorAtualPrefab = null;
    }

    public void Parar()
    {
        if (objetoCorInstanciado != null)
        {
            Destroy(objetoCorInstanciado);
            objetoCorInstanciado = null;
        }
        CorAtualPrefab = null;
    }
}
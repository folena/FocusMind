using System.Collections;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    public GameObject[] carPrefabs;
    public Transform spawnPoint;
    public Transform endPoint;
    public float minSpawnTime = 2f;
    public float maxSpawnTime = 5f;
    public float carSpeed = 5f;
    public bool reverseDirection = false;

    void Start()
    {
        StartCoroutine(SpawnCarRoutine());
    }

    IEnumerator SpawnCarRoutine()
    {
        while (true)
        {
            SpawnCar();
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void SpawnCar()
    {
        int index = Random.Range(0, carPrefabs.Length);

        Quaternion rotation = reverseDirection ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        GameObject newCar = Instantiate(carPrefabs[index], spawnPoint.position, rotation);
        newCar.AddComponent<CarMover>().Initialize(endPoint.position, carSpeed);
    }
}

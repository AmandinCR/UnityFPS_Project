using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class EnemyManager : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int enemyCount = 1;
    [SerializeField] private List<Transform> spawningTransforms = new List<Transform>();
    [SerializeField] private bool spawned = false;
    [HideInInspector] public int enemiesAlive = 0;
    [SerializeField] private float spawnCooldown = 0.0f;
    [SerializeField] private int spawnWaves = 1;
    private int spawnedWavesCount = 0;
    private float spawnTimer = 0f;
    private CameraSetup cam;

    private void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraSetup>();
    }

    [ServerCallback]
    private void Update()
    {
        if (!spawned) { return; }
        if (spawnedWavesCount >= spawnWaves) { return; }
        if (enemiesAlive > 0) { return;}

        if (spawnTimer > spawnCooldown)
        {
            spawnTimer = 0f;
            SpawnEnemies();
        }
        else
        {
            spawnTimer += Time.deltaTime;
        }
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider co)
    {
        if (spawned) { return; }

        if (co.transform.root.gameObject.layer == 6 || co.transform.root.gameObject.layer == 7)
        {
            spawned = true;
            SpawnEnemies();
        }
    }

    [ServerCallback]
    public void SpawnEnemies()
    {
        enemiesAlive = enemyCount;
        spawnedWavesCount++;

        for (int i=0; i<enemyCount; i++)
        {
            int j = i % spawningTransforms.Count;
            // can't move an agent unless his pathfinding is off
            GameObject enemy = Instantiate(enemyPrefab, spawningTransforms[j].position, spawningTransforms[j].rotation);
            enemy.GetComponent<Enemy>().enemyManager = this;
            enemy.GetComponent<Enemy>().SetupHealthBar(cam.canvas, cam.GetComponent<Camera>());
            NetworkServer.Spawn(enemy);
        }
    }
}

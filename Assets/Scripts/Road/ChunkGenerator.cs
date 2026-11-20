using Pools;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    [SerializeField] private LaneSystem LaneSystem;
    [field: SerializeField] public CoinPool CoinPool { get; private set; }
    [field: SerializeField] public List<ObstaclePool> ObstaclePools { get; private set; }

    [Header("Start safe zone")]
    [SerializeField] private float noObstaclesBeforeZ = 40f;
    
    [Header("Coins")]
    [SerializeField] private int coinsPerChunk = 5;
    [SerializeField] private float coinSpacing = 2f;
    [SerializeField] private float coinHeight = 1f;
    [SerializeField] private float obstacleClearance = 2.5f;

    public Chunk Generate(Chunk chunkToFill)
    {
        Obstacle obstacle = null;
        if (!ObstaclePools.IsEmpty() && chunkToFill.transform.position.z >= noObstaclesBeforeZ)
        {
            var obstaclePool = ObstaclePools.GetRandomElement();
            obstacle = obstaclePool.Spawn();
            chunkToFill.Obstacles.Add(obstacle);
            obstacle.transform.SetParent(chunkToFill.transform, true);
            obstacle.transform.localPosition = chunkToFill.Grid.GetRandomPosition();
            if (obstacle.IsOnAllLanes)
            {
                                obstacle.transform.localPosition = new Vector3(LaneSystem.CenterLane * LaneSystem.LaneWidth, obstacle.transform.localPosition.y, obstacle.transform.localPosition.z);
            }
        }
        GenerateCoins(chunkToFill, obstacle);
        return chunkToFill;
    }

    private void GenerateCoins(Chunk chunk, Obstacle obstacle)
    {
        if (CoinPool == null)
            return;

        int lane = LaneSystem.Lanes[Random.Range(0, LaneSystem.Lanes.Count)];
        float laneX = lane * LaneSystem.LaneWidth;
        float startZ = chunk.Begin.localPosition.z + coinSpacing;

        for (int i = 0; i < coinsPerChunk; i++)
        {
            float z = startZ + i * coinSpacing;

            // don't put coins inside an obstacle
            if (obstacle != null)
            {
                Vector3 obstaclePos = obstacle.transform.localPosition;
                bool sameLane = obstacle.IsOnAllLanes || Mathf.Abs(obstaclePos.x - laneX) < 0.1f;
                if (sameLane && Mathf.Abs(z - obstaclePos.z) < obstacleClearance)
                    continue;
            }

            Coin coin = CoinPool.Spawn();
            chunk.Coins.Add(coin);
            coin.transform.SetParent(chunk.transform, false);
            coin.transform.localPosition = new Vector3(laneX, coinHeight, z);
            coin.UpdateStartPositionForSinAnimator();
        }
    }
}
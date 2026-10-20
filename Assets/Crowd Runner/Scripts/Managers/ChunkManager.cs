using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager instance;

    [Header(" Elements ")]
    [SerializeField] private Chunk firstChunk;
    [SerializeField] private Chunk[] randomChunks;
    [SerializeField] private Chunk finishChunk;

    [Header(" Play Time Settings ")]
    [SerializeField] private float referenceMoveSpeed = 5f;
    [SerializeField] private float minPlayTime = 60f;
    [SerializeField] private float maxPlayTime = 90f;

    private GameObject finishLine;

    private void Awake()
    {
        if (instance != null)
            Destroy(gameObject);
        else
            instance = this;
    }

    private void Start()
    {
        GenerateLevel();

        finishLine = GameObject.FindWithTag("Finish");
    }

    private void GenerateLevel()
    {
        float startZ = PlayerController.instance != null ? PlayerController.instance.transform.position.z : 0f;
        float finishLineOffset = GetFinishLineOffset();

        // 플레이어 시작 지점 ~ 결승선 사이 거리 범위 (moveSpeed 5 기준 1분 ~ 1분 30초)
        float minDistance = minPlayTime * referenceMoveSpeed;
        float maxDistance = maxPlayTime * referenceMoveSpeed;
        float targetDistance = Random.Range(minDistance, maxDistance);

        List<Chunk> levelChunks = new List<Chunk> { firstChunk };

        // 첫 청크는 원점에 중심을 두므로 끝 지점은 길이의 절반
        float chunkEndZ = firstChunk.GetLength() / 2;

        while (GetFinishDistance(chunkEndZ, finishLineOffset, startZ) < targetDistance)
        {
            List<Chunk> candidates = new List<Chunk>();

            foreach (Chunk chunk in randomChunks)
            {
                if (GetFinishDistance(chunkEndZ + chunk.GetLength(), finishLineOffset, startZ) <= maxDistance)
                    candidates.Add(chunk);
            }

            if (candidates.Count == 0)
                break;

            Chunk randomChunk = candidates[Random.Range(0, candidates.Count)];
            levelChunks.Add(randomChunk);
            chunkEndZ += randomChunk.GetLength();
        }

        levelChunks.Add(finishChunk);

        CreateLevel(levelChunks);
    }

    private float GetFinishDistance(float chunkEndZ, float finishLineOffset, float startZ)
    {
        return chunkEndZ + finishChunk.GetLength() / 2 + finishLineOffset - startZ;
    }

    private float GetFinishLineOffset()
    {
        foreach (Transform child in finishChunk.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("Finish"))
                return child.position.z - finishChunk.transform.position.z;
        }

        return 0f;
    }

    private void CreateLevel(List<Chunk> levelChunks)
    {
        Vector3 chunkPosition = Vector3.zero;

        for (int i = 0; i < levelChunks.Count; i++)
        {
            Chunk chunkToCreate = levelChunks[i];

            if (i > 0)
                chunkPosition.z += chunkToCreate.GetLength() / 2;

            Chunk chunkInstance = Instantiate(chunkToCreate, chunkPosition, Quaternion.identity, transform);

            chunkPosition.z += chunkInstance.GetLength() / 2;
        }
    }

    public float GetFinishZ()
    {
        return finishLine.transform.position.z;
    }

    public int GetLevel()
    {
        return PlayerPrefs.GetInt("level", 0);
    }
}

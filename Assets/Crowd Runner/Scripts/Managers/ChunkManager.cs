using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager instance;

    [Header(" Elements ")]
    [SerializeField] private LevelSO[] levels;
    private GameObject finishLine;

    private void Awake()
    {
        if (instance != null)
            Destroy(gameObject);
        else
            instance = this;
    }
    
    void Start()
    {
        GenerateLevel();

        finishLine = GameObject.FindWithTag("Finish");
    }

    private void GenerateLevel()
    {
        int currentLevel = Random.Range(0, levels.Length);

        LevelSO level = levels[currentLevel];

        CreateRandomLevel(level.chunks);
    }

    private void CreateRandomLevel(Chunk[] levelChunks)
    {
        for (int i = 1; i < levelChunks.Length - 1; i++)
        {
            int randomIndex = Random.Range(i, levelChunks.Length - 1);
            
            /*
             Chunk temp = levelChunks[randomIndex];
            levelChunks[randomIndex] = levelChunks[i];
            levelChunks[i] = temp;
            */
            
            // 위의 코드를 튜플 구문 방식으로 변환
            (levelChunks[randomIndex], levelChunks[i]) =  (levelChunks[i], levelChunks[randomIndex]);
        }

        Vector3 chunkPosition = Vector3.zero;

        for (int i = 0; i < levelChunks.Length; i++)
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

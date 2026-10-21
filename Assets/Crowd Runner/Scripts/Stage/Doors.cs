using UnityEngine;
using TMPro;

public enum BonusType { Addition, Difference, Product, Division }

public class Doors : MonoBehaviour
{
    [Header(" Elements ")]
    [SerializeField] private SpriteRenderer rightDoorRenderer;
    [SerializeField] private SpriteRenderer leftDoorRenderer;
    [SerializeField] private TextMeshPro rightDoorText;
    [SerializeField] private TextMeshPro leftDoorText;
    [SerializeField] private Collider collider;

    [Header(" Settings ")]
    [SerializeField] private BonusType rightDoorBonusType;
    [SerializeField] private BonusType leftDoorBonusType;
    [Tooltip("수식이 길어져도 문 안에 들어가도록 자동 크기 조절의 최소 폰트 크기를 낮춘다.")]
    [SerializeField] private float minFontSize = 4f;

    [Header(" Colors ")]
    [SerializeField] private Color bonusColor;
    [SerializeField] private Color penaltyColor;
    [Tooltip("켜면 계산 결과에 따라 이득/손해 색을 보여준다. 끄면 정답이 드러나지 않도록 두 문 모두 같은 색을 쓴다.")]
    [SerializeField] private bool showResultColor;
    [SerializeField] private Color quizColor = new Color(0.25f, 0.6f, 1f, 0.6f);

    private DoorQuiz rightQuiz;
    private DoorQuiz leftQuiz;

    private void Start()
    {
        RandomDoor();
        ConfigureDoors();
    }

    private void RandomDoor()
    {
        if (gameObject.CompareTag("First"))
        {
            rightQuiz = DoorQuizGenerator.CreateEasy();
            leftQuiz = DoorQuizGenerator.CreateEasy();
        }
        else
        {
            rightQuiz = DoorQuizGenerator.Create(GetRandomBonusType());
            leftQuiz = DoorQuizGenerator.Create(GetRandomBonusType());
        }

        rightDoorBonusType = rightQuiz.Type;
        leftDoorBonusType = leftQuiz.Type;
    }

    private BonusType GetRandomBonusType()
    {
        return (BonusType)Random.Range(0, 4);
    }

    private void ConfigureDoors()
    {
        ConfigureDoor(rightDoorRenderer, rightDoorText, rightQuiz);
        ConfigureDoor(leftDoorRenderer, leftDoorText, leftQuiz);
    }

    private void ConfigureDoor(SpriteRenderer doorRenderer, TextMeshPro doorText, DoorQuiz quiz)
    {
        if (showResultColor)
            doorRenderer.color = IsBonus(quiz) ? bonusColor : penaltyColor;
        else
            doorRenderer.color = quizColor;

        doorText.enableAutoSizing = true;
        doorText.fontSizeMin = Mathf.Min(doorText.fontSizeMin, minFontSize);
        doorText.text = quiz.Text;
    }

    private bool IsBonus(DoorQuiz quiz)
    {
        switch (quiz.Type)
        {
            case BonusType.Addition: return quiz.Value > 0;
            case BonusType.Difference: return quiz.Value < 0;
            case BonusType.Product: return quiz.Value > 1;
            default: return quiz.Value < 1;
        }
    }

    public int GetBonusAmount(float xPosition)
    {
        if (xPosition > 0)
            return rightQuiz.Value;

        return leftQuiz.Value;
    }

    public BonusType GetBonusType(float xPosition)
    {
        if (xPosition > 0)
            return rightDoorBonusType;

        return leftDoorBonusType;
    }

    public void Disable()
    {
        collider.enabled = false;
    }
}

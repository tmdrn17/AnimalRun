using System.Collections;
using System.Collections.Generic;
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
    int rightPMAmount;
    int rightMDAmount;
    int rightRandomType;

    [SerializeField] private BonusType leftDoorBonusType;
    int leftPMAmount;
    int leftMDAmount;
    int leftRandomType;

    [SerializeField] private Color bonusColor;
    [SerializeField] private Color penaltyColor;




    // Start is called before the first frame update
    void Start()
    {
        RandomDoor();
        ConfigureDoors();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void RandomDoor()
    {
        if (gameObject.CompareTag("First"))
        {
            rightRandomType = Random.Range(1, 2);
            leftRandomType = Random.Range(1, 2);

            rightPMAmount = Random.Range(1, 21);
            rightMDAmount = Random.Range(1, 4);
            leftPMAmount = Random.Range(1, 21);
            leftMDAmount = Random.Range(1, 4);
        }
        else
        {
            rightRandomType = Random.Range(1, 5);
            leftRandomType = Random.Range(1, 5);

            rightPMAmount = Random.Range(1, 101);
            rightMDAmount = Random.Range(1, 6);
            leftPMAmount = Random.Range(1, 101);
            leftMDAmount = Random.Range(1, 6);
        }

        if (rightRandomType == 1)
            rightDoorBonusType = BonusType.Addition;
        else if (rightRandomType == 2)
            rightDoorBonusType = BonusType.Division;
        else if (rightRandomType == 3)
            rightDoorBonusType = BonusType.Difference;
        else if (rightRandomType == 4)
            rightDoorBonusType = BonusType.Product;

        if (leftRandomType == 1)
            leftDoorBonusType = BonusType.Addition;
        else if (leftRandomType == 2)
            leftDoorBonusType = BonusType.Division;
        else if (leftRandomType == 3)
            leftDoorBonusType = BonusType.Difference;
        else if (leftRandomType == 4)
            leftDoorBonusType = BonusType.Product;
    }

    private void ConfigureDoors()
    {
        // Configure the right door

        switch(rightDoorBonusType)
        {
            case BonusType.Addition:
                rightDoorRenderer.color = bonusColor;
                rightDoorText.text = "+" + rightPMAmount;
                break;

            case BonusType.Difference:
                rightDoorRenderer.color = penaltyColor;
                rightDoorText.text = "-" + rightPMAmount;
                break;

            case BonusType.Product:
                rightDoorRenderer.color = bonusColor;
                rightDoorText.text = "x" + rightMDAmount;
                break;

            case BonusType.Division:
                rightDoorRenderer.color = penaltyColor;
                rightDoorText.text = "/" + rightMDAmount;
                break;
        }


        switch (leftDoorBonusType)
        {
            case BonusType.Addition:
                leftDoorRenderer.color = bonusColor;
                leftDoorText.text = "+" + leftPMAmount;
                break;

            case BonusType.Difference:
                leftDoorRenderer.color = penaltyColor;
                leftDoorText.text = "-" + leftPMAmount;
                break;

            case BonusType.Product:
                leftDoorRenderer.color = bonusColor;
                leftDoorText.text = "x" + leftMDAmount;
                break;

            case BonusType.Division:
                leftDoorRenderer.color = penaltyColor;
                leftDoorText.text = "/" + leftMDAmount;
                break;
        }


    }

    public int GetBonusAmount(float xPosition)
    {
        if (xPosition > 0)
        {
            if ((rightDoorBonusType == BonusType.Addition) || (rightDoorBonusType == BonusType.Difference))
                return rightPMAmount;
            else
                return rightMDAmount;
        }


        else
        {
            if ((leftDoorBonusType == BonusType.Addition) || (leftDoorBonusType == BonusType.Difference))
                return leftPMAmount;
            else
                return leftMDAmount;
        }
    }

    public BonusType GetBonusType(float xPosition)
    {
        if (xPosition > 0)
            return rightDoorBonusType;
        else
            return leftDoorBonusType;
    }

    public void Disable()
    {
        collider.enabled = false;
    }
}

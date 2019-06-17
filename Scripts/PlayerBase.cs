using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBase : MonoBehaviour {
    
    public GameObject[] unit = new GameObject[10];

    public List<GameObject> unitList;
    public bool isHuman = true; // true - Human controlled, false - CPU controlled
    public int controllerPlayer;
    public int health = 5;
    public int money = 0;
    public int jewels = 0;
    public int range = 3;
    public int nTurns = 0;
    public int[] improvements = new int[] { 0, 0, 0, 0, 0 };
    public static int[] improvementCost = new int[] { 1, 3, 5, 5, 7, 0 };
    public int score = 0;
    public int[] scoreTypes = new int[4];
    // Scores types:
    // 0 - Nº units generated
    // 1 - Nº units defeated
    // 2 - Nº bases destroyed
    // 3 - Nº improvements bought
    public Color baseColor;

    public void createUnit(float posX, float posY, int unitType)
    {
        GameObject newUnitObject = Instantiate(unit[unitType], new Vector3(posX, posY), Quaternion.identity);
        Unit newUnit = newUnitObject.GetComponent<Unit>();
        newUnit.controllerPlayer = controllerPlayer;
        newUnit.initializeStats(unitType);
        newUnit.applyStatBoosts(improvements);
        newUnit.GetComponent<SpriteRenderer>().color = baseColor;
        unitList.Add(newUnitObject);
        money -= newUnit.cost;
    }

    public void calculateScore()
    {
        score = 0;
        // 0 - Nº units generated
        score += scoreTypes[0] * 100; // 100pts for each
        // 1 - Nº units defeated
        score += scoreTypes[1] * 250; // 250pts for each
        // 2 - Nº bases destroyed
        score += scoreTypes[2] * 1000; // 1000pts for each
        // 3 - Nº improvements bought
        score += scoreTypes[3] * 100; // 100pts for each
        // Add health left as pts
        score += health * 300;
    }

    public void chooseColor()
    {
        switch (controllerPlayer)
        {
            case 0:
                baseColor = new Color(0.25f, 0.25f, 1f);
                break;
            case 1:
                baseColor = new Color(1f, 0.25f, 0.25f);
                break;
            case 2:
                baseColor = new Color(0.25f, 1f, 0.25f);
                break;
            case 3:
                baseColor = new Color(1f, 1f, 0.25f);
                break;
            case 4:
                baseColor = new Color(1f, 0.25f, 1f);
                break;
            case 5:
                baseColor = new Color(0.25f, 1f, 1f);
                break;
            case 6:
                baseColor = new Color(0.25f, 0.25f, 0.25f);
                break;
            case 7:
                baseColor = new Color(1f, 1f, 1f);
                break;
            case 8:
                baseColor = new Color(0.5f, 0.5f, 0.5f);
                break;
            default: // Choose a random color for players above position 8
                float randomR = Random.Range(0f, 1f);
                float randomG = Random.Range(0f, 1f);
                float randomB = Random.Range(0f, 1f);
                baseColor = new Color(randomR, randomG, randomB);
                break;
        }
    }

}

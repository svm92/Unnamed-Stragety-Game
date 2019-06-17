using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour {
    
    public int controllerPlayer { get; set; }
    public bool alreadyMoved = false;
    public bool alreadyAttacked = false;

    public string unitName;
    public int movementRange;
    public int health;
    public int power;
    public int range;
    public int cost;

    public static string[] baseUnitName = new string[] { "Bat", "Beetle", "Snake", "Goblin", "Skeleton", "Ghost",
        "Big Rat", "Scorpion",  "Guardian", "Reaper"};
    public static int[] baseMovementRange = new int[] { 3, 2, 4, 3, 3, 3, 2, 5, 1, 4};
    public static int[] baseHealth = new int[] { 1, 2, 1, 2, 3, 1, 4, 1, 6, 4 };
    public static int[] basePower = new int[] { 1, 1, 1, 2, 1, 2, 2, 3, 4, 3 };
    public static int[] baseRange = new int[] { 1, 1, 1, 1, 2, 3, 1, 1, 2, 1 };
    public static int[] baseCost = new int[] { 100, 150, 200, 300, 300, 400, 400, 500, 600, 600};
    
    public void initializeStats(int unitType)
    {
        unitName = baseUnitName[unitType];
        movementRange = baseMovementRange[unitType];
        health = baseHealth[unitType];
        power = basePower[unitType];
        range = baseRange[unitType];
        cost = baseCost[unitType];
    }

    public void applyStatBoosts(int[] improvements)
    {
        movementRange += improvements[1];
        health += improvements[2];
        power += improvements[3];
        range += improvements[4];
    }

}

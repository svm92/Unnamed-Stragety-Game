using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AI : MonoBehaviour {

    Cursor cursor;
    int positionChosen;
    bool occupied;
    bool firstTurnActionTaken;
    public PlayerBase CPU { get; set; }
    BattleMenuManager battleMenuManager;

    private void Awake()
    {
        cursor = GetComponentInParent<Cursor>();
        battleMenuManager = GameObject.Find("BattleMenuManager").GetComponent<BattleMenuManager>();
    }

    public IEnumerator beginAITurn()
    {
        firstTurnActionTaken = false;

        // Buy base improvements
        occupied = true;
        StartCoroutine(buyImprovements());
        while (occupied)
            yield return new WaitForSeconds(0.1f);

        // Generate units
        occupied = true;
        StartCoroutine(generateRandomUnit());
        while (occupied)
            yield return new WaitForSeconds(0.1f);

        // Move/attack
        occupied = true;
        StartCoroutine(moveAndAttackWithAllUnits());
        while (occupied)
            yield return new WaitForSeconds(0.1f);

        if (cursor.endGame)
            yield break; // Stop if the game is over

        // End turn
        yield return new WaitForSeconds(0.5f);
        cursor.beginNextPlayerTurn();
    }

    IEnumerator buyImprovements()
    {
        while (true)
        {
            if (CPU.jewels < 1) // If not enough jewels, skip
            {
                occupied = false;
                yield break;
            }

            // If the AI has an high nº of jewels, spend them
            if (CPU.jewels >= 5)
            {
                int rnd = Random.Range(2, 5); // Choose 2, 3 or 4
                if (CPU.jewels >= PlayerBase.improvementCost[rnd]) // If enough money, buy that improvement
                {
                    if (!firstTurnActionTaken)
                        yield return new WaitForSeconds(1f);
                    buyImprov(rnd);
                    yield return new WaitForSeconds(0.5f);
                }
            } else
            {
                // If the AI has few jewels, give it a 20% chance of not spending any (saving them for next turn)
                int rnd = Random.Range(0, 5);
                if (rnd == 0)
                {
                    occupied = false;
                    yield break;
                }
                else {
                    while (true)
                    {
                        rnd = Random.Range(0, 2); // Choose 0 or 1
                        if (CPU.jewels >= PlayerBase.improvementCost[rnd]) // If enough money, buy that improvement
                        {
                            if (!firstTurnActionTaken)
                                yield return new WaitForSeconds(1f);
                            buyImprov(rnd);
                            yield return new WaitForSeconds(0.5f);
                            break;
                        }
                        // If not, keep trying
                    }
                }
            }
        }
    }

    void buyImprov(int i)
    {
        battleMenuManager.increaseStat(i);
        string improvementText = "";
        switch (i)
        {
            case 0:
                improvementText = "Summon Range +1";
                break;
            case 1:
                improvementText = "Movement +1";
                break;
            case 2:
                improvementText = "Health +1";
                break;
            case 3:
                improvementText = "Power +1";
                break;
            case 4:
                improvementText = "Attack Range +1";
                break;
            default:
                break;
        }

        StartCoroutine(cursor.popupText(improvementText, CPU.gameObject, Color.white));
        firstTurnActionTaken = true;
    }

    IEnumerator generateRandomUnit()
    {
        cursor.transform.position = CPU.transform.position; // Move cursor to base
        cursor.printValidUnitGenerationTargets();
        if (cursor.validTargets.Count == 0 || CPU.money < 100) // If there aren't valid targets or not enough money
        {
            cursor.targetSelectionMode = 0;
            cursor.GetComponent<Animator>().SetInteger("animationState", 0);
            cursor.clearTargets();
            occupied = false;
            yield break;
        }

        // Choose type of unit to generate
        int unitTypeChosen;
        while (true)
        {
            unitTypeChosen = Random.Range(0, CPU.unit.Length);
            if (CPU.money >= Unit.baseCost[unitTypeChosen]) // If enough money, use that type of unit
                break;
        }

        // Choose random cursor position to generate unit
        yield return StartCoroutine(decideMovementTarget());
        int x = (int)cursor.validTargets[positionChosen].x;
        int y = (int)cursor.validTargets[positionChosen].y;
        cursor.transform.position = new Vector3(x, y, cursor.transform.position.z);
        yield return new WaitForSeconds(0.25f);

        // Generate unit
        cursor.typeOfUnitToGenerateNext = unitTypeChosen;
        cursor.generateUnit();
        yield return new WaitForSeconds(0.1f);

        StartCoroutine(generateRandomUnit()); // Keep trying to generate more units
    }

    IEnumerator moveAndAttackWithAllUnits()
    {
        foreach (GameObject u in CPU.unitList)
        {
            // Choose unit
            cursor.currentlySelectedUnit = u.GetComponent<Unit>();

            // Move unit
            yield return StartCoroutine(moveUnit(u));

            // Attack with unit
            yield return StartCoroutine(attackWithUnit(u));

            if (cursor.endGame) // Stop if the game has ended (because the CPU just won with an attack)
            {
                occupied = false;
                yield break;
            }
        }

        cursor.targetSelectionMode = 0;
        cursor.GetComponent<Animator>().SetInteger("animationState", 0);
        cursor.clearTargets();
        occupied = false;
    }

    IEnumerator moveUnit(GameObject u)
    {
        // Move cursor to unit
        cursor.transform.position = u.transform.position;
        cursor.printValidMovementDestinations();
        yield return new WaitForSeconds(0.1f);

        if (cursor.validTargets.Count > 0) // If there is any valid destination tile
        {
            // Add the unit current's position as possible destination (possibilty of choosing not to move that turn)
            cursor.validTargets.Add(u.transform.position);

            // Choose destination
            yield return StartCoroutine(decideMovementTarget());
            int x = (int)cursor.validTargets[positionChosen].x;
            int y = (int)cursor.validTargets[positionChosen].y;
            cursor.transform.position = new Vector3(x, y, cursor.transform.position.z);
            yield return new WaitForSeconds(0.25f);

            // Move unit to destination
            StartCoroutine(cursor.moveUnitToTargetTile(cursor.transform.position));
            // Wait until movement animation ends ("moveUnitToTargetTile" sets "activeCursor" to true upon completion)
            while (!cursor.activeCursor)
                yield return new WaitForSeconds(0.1f);
            cursor.activeCursor = false; // Set to false again, since it's not the player's turn
        }

        cursor.targetSelectionMode = 0;
        cursor.GetComponent<Animator>().SetInteger("animationState", 0);
        cursor.clearTargets();
    }

    IEnumerator decideMovementTarget() // Analyzes the CPU's possible movements to determine a 'good' one
    {
        int[] scores = new int[cursor.validTargets.Count];
        // Assign scores to the possible destinations according to how 'good' they are
        for (int i = 0; i < cursor.validTargets.Count; i++)
        {
            Vector3 tile = cursor.validTargets[i];
            // Increase score if there are adjacent enemies or bases to attack
            Vector3[] neighboringTiles = new Vector3[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
            foreach (Vector3 n in neighboringTiles)
            {
                GameObject tileToCheck = cursor.attackTargetIn((int)(tile.x + n.x), (int)(tile.y + n.y));
                if (thereIsAUnitIn(tileToCheck))
                    scores[i] += 5;
                else if (thereIsABaseIn(tileToCheck))
                    scores[i] += 20;
            }

            // Decrease score the farther the tile is from enemy bases
            foreach (PlayerBase p in cursor.playerBase)
            {
                if (p == CPU || p == null)
                    continue; // Skip own base and defeated bases

                // Find distance from the tile to an enemy base
                yield return PathFinding.findDistance(tile, p.transform.position, cursor);
                scores[i] -= PathFinding.distance;
            }
        }

        // Fetch the destination with the highest score (or tied for the highest score)
        int maxScore = scores.Max();
        positionChosen = scores.ToList().IndexOf(maxScore);
    }

    bool thereIsAUnitIn(GameObject tileToCheck)
    {
        return (tileToCheck != null && tileToCheck.GetComponent<Unit>() != null);
    }

    bool thereIsABaseIn(GameObject tileToCheck)
    {
        return (tileToCheck != null && tileToCheck.GetComponent<PlayerBase>() != null);
    }

    IEnumerator attackWithUnit(GameObject u)
    {
        // Move cursor to unit
        cursor.transform.position = u.transform.position;
        cursor.printValidAttackTargets();

        if (cursor.validAttackTargets.Count > 0) // If there is any valid target
        {
            yield return new WaitForSeconds(0.1f);
            // Choose target
            positionChosen = decideAttackTarget();
            int x = (int)cursor.validAttackTargets[positionChosen].x;
            int y = (int)cursor.validAttackTargets[positionChosen].y;
            cursor.transform.position = new Vector3(x, y, cursor.transform.position.z);
            yield return new WaitForSeconds(0.25f);

            // Attack
            cursor.attack(x, y);
            yield return new WaitForSeconds(0.1f);
        }

        cursor.targetSelectionMode = 0;
        cursor.GetComponent<Animator>().SetInteger("animationState", 0);
        cursor.clearTargets();
    }

    int decideAttackTarget() // Analyzes the CPU's possible attack targets to determine a 'good' one
    {
        int[] scores = new int[cursor.validAttackTargets.Count];
        // Assign scores to the possible targets according to how 'good' they are
        for (int i = 0; i < cursor.validAttackTargets.Count; i++)
        {
            Vector3 tile = cursor.validAttackTargets[i];
            // Increase score according to how dangerous an enemy is
            GameObject tileToCheck = cursor.attackTargetIn((int)tile.x, (int)tile.y);
            if (thereIsAUnitIn(tileToCheck))
                scores[i] = enemyDangerRanking(tileToCheck.GetComponent<Unit>());
            // Increase score significantly if there is a base
            else if (thereIsABaseIn(tileToCheck))
                scores[i] = 100;
        }

        // Fetch the target with the highest score (or tied for the highest score)
        int maxScore = scores.Max();
        return scores.ToList().IndexOf(maxScore);
    }

    int enemyDangerRanking(Unit unit)
    {
        // Assigns a value to an enemy according to its stats
        return ((int)(unit.movementRange*0.25f) + unit.health + unit.power + unit.range*2);
    }

}

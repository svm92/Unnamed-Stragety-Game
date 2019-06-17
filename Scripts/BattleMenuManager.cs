using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class BattleMenuManager : MonoBehaviour {

    public GameObject cursorObject;

    Canvas unitActionCanvas;
    Canvas mainMenuCanvas;
    Canvas improvementCanvas;
    Button moveButton;
    Button attackButton;
    Canvas unitGenerationCanvas;
    Cursor cursor;

    private void Awake()
    {
        unitActionCanvas = GameObject.Find("unitActionCanvas").GetComponent<Canvas>();
        mainMenuCanvas = GameObject.Find("mainMenuCanvas").GetComponent<Canvas>();
        improvementCanvas = GameObject.Find("improvementCanvas").GetComponent<Canvas>();
        moveButton = GameObject.Find("moveButton").GetComponent<Button>();
        attackButton = GameObject.Find("attackButton").GetComponent<Button>();
        unitGenerationCanvas = GameObject.Find("unitGenerationCanvas").GetComponent<Canvas>();
        cursor = cursorObject.GetComponent<Cursor>();

        improvementCanvas.enabled = false;
    }

    public void move()
    {
        reallowCursorMovement();
        cursor.printValidMovementDestinations();
    }

	public void attack()
    {
        reallowCursorMovement();
        cursor.printValidAttackTargets();
    }

    public static int getButtonNumber(string buttonName)
    {
        // Naming schema: generationButton0, generationButton1...
        int buttonPosition = int.Parse(buttonName.Substring(16)); // Gets the number at the end
        return buttonPosition; // +1 because the first button is the cancel button
    }

    public void create()
    {
        string buttonName = EventSystem.current.currentSelectedGameObject.name;
        int buttonPosition = getButtonNumber(buttonName);
        cursor.typeOfUnitToGenerateNext = buttonPosition;

        cursor.printValidUnitGenerationTargets();
        reallowCursorMovement();
    }

    public void openImproveBaseMenu()
    {
        // Inhabilite menu buttons
        Transform mainMenuPanel = mainMenuCanvas.transform.GetChild(0);
        foreach (Button b in mainMenuPanel.GetComponentsInChildren<Button>())
            b.interactable = false;

        // Open base improvement menu and enable its buttons
        improvementCanvas.enabled = true;
        enableImprovementButtons();
    }

    void enableImprovementButtons()
    {
        // Disable all
        foreach (Button b in improvementCanvas.GetComponentsInChildren<Button>())
            b.interactable = false;

        // Disable buttons the player can afford
        int costIndex = 0;
        foreach (Button b in improvementCanvas.GetComponentsInChildren<Button>())
        {
            int buttonCost = PlayerBase.improvementCost[costIndex];
            if (cursor.activePlayer.jewels >= buttonCost)
                b.interactable = true;
            costIndex++;
        }

        cursor.updateImprovementCanvasText();

        Button firstButton = improvementCanvas.GetComponentInChildren<Button>();
        //if (firstButton.interactable) // If enough money, reselect first button
        //    firstButton.Select();
        //else // Otherwise, leave
        //    reallowCursorMovement();
        if (!firstButton.interactable)
            reallowCursorMovement();
    }

    public void increaseStat(int i)
    {
        cursor.activePlayer.improvements[i]++;
        cursor.activePlayer.jewels -= PlayerBase.improvementCost[i];
        cursor.activePlayer.scoreTypes[3]++; // Increase improvement score

        if (i == 0)
            cursor.activePlayer.range++;

        if ( cursor.activePlayer.isHuman)
            enableImprovementButtons();

        // Play sound
        cursor.soundSource.clip = cursor.dingSound;
        cursor.soundSource.Play();
    }

    public void endTurn()
    {
        reallowCursorMovement();
        cursor.beginNextPlayerTurn();
    }

    public void reallowCursorMovement()
    {
        // Disable every canvas and all buttons contained within each
        if (unitActionCanvas.GetComponent<Canvas>().enabled)
        {
            unitActionCanvas.GetComponent<Canvas>().enabled = false;
            moveButton.interactable = false;
            attackButton.interactable = false;
        }
        
        if (unitGenerationCanvas.enabled)
        {
            unitGenerationCanvas.enabled = false;
            cursor.disableAllUnitGenerationButtons();
        }
        
        if (mainMenuCanvas.enabled)
        {
            mainMenuCanvas.enabled = false;
            Transform mainMenuPanel = mainMenuCanvas.transform.GetChild(0);
            foreach (Button b in mainMenuPanel.GetComponentsInChildren<Button>())
                b.interactable = false;
        }

        if (improvementCanvas.enabled)
        {
            improvementCanvas.enabled = false;
            foreach (Button b in improvementCanvas.GetComponentsInChildren<Button>())
                b.interactable = false;
        }

        cursor.activeCursor = true;
    }

    public void cancel()
    {
        reallowCursorMovement();
        cursor.targetSelectionMode = 0;
        cursor.GetComponent<Animator>().SetInteger("animationState", 0);
        cursor.clearTargets();
    }

    public void returnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

}

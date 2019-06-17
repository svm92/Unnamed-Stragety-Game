using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour {

    Canvas mainCanvas;
    List<Button> mainMenuButtons = new List<Button>();
    RectTransform optionsPanel;
    Button optionsReturnButton;
    RectTransform howToPlayPanel;
    Button howToPlayReturnButton;
    RectTransform creditsPanel;
    Button creditsReturnButton;
    Slider nOfPlayersSlider;
    Slider cpuSlider;
    Text nOfPlayersText;
    Text nOfCPUText;
    Dropdown battlefieldDropdown;

    Canvas randomMapGenerationCanvas;
    Text mapSizeText;
    string chosenBattlefield;

    AudioSource musicSource;
    AudioSource clickSound;
    AudioSource[] audioSources;

    private void Awake()
    {
        // Canvas/Panels/Buttons
        mainCanvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        for (int i = 0; i < 5; i++)
            mainMenuButtons.Add(mainCanvas.GetComponentsInChildren<Button>()[i]);

        optionsPanel = GameObject.Find("optionsPanel").GetComponent<RectTransform>();
        optionsReturnButton = optionsPanel.GetChild(0).GetComponent<Button>();
        howToPlayPanel = GameObject.Find("howToPlayPanel").GetComponent<RectTransform>();
        howToPlayReturnButton = howToPlayPanel.GetChild(0).GetComponent<Button>();
        creditsPanel = GameObject.Find("creditsPanel").GetComponent<RectTransform>();
        creditsReturnButton = creditsPanel.GetChild(0).GetComponent<Button>();
        nOfPlayersSlider = GameObject.Find("nPlayersSlider").GetComponent<Slider>();
        cpuSlider = GameObject.Find("cpuSlider").GetComponent<Slider>();
        nOfPlayersText = GameObject.Find("nOfPlayersText").GetComponent<Text>();
        nOfCPUText = GameObject.Find("nOfCPUText").GetComponent<Text>();
        battlefieldDropdown = GameObject.Find("battlefieldDropdown").GetComponent<Dropdown>();

        randomMapGenerationCanvas = GameObject.Find("randomMapGenerationCanvas").GetComponent<Canvas>();
        mapSizeText = GameObject.Find("mapSizeText").GetComponent<Text>();
        enableMapGenerationCanvas(false);

        // Music
        musicSource = GameObject.Find("Main Camera").GetComponent<AudioSource>();
        clickSound = GetComponent<AudioSource>();
        audioSources = new AudioSource[] { musicSource, clickSound };
        GameObject.Find("muteToggle").GetComponent<Toggle>().isOn = Cursor.muted;
        muteOrUnmuteMusic();

        // Sliders
        if (Cursor.nOfPlayers == 0)
            Cursor.nOfPlayers = 2; // Default value;
        nOfPlayersSlider.value = Cursor.nOfPlayers;

        // Dropdown
        List<string> stages = new List<string> { "Closed Forest", "Forest Corridor", "Open Field", "Small Field", "Small Island", "Farm" , "Long Forest ", "Square Island", "Random"};
        battlefieldDropdown.AddOptions(stages);
        chosenBattlefield = "Battlefield0"; // Default choice
    }
    /*
    private void LateUpdate() // Update dropdown scroll position
    {
        if (battlefieldDropdown.GetComponentInChildren<ScrollRect>() != null) // If the dropdown is expanded
        {
            // Unity treats dropdown options as toggles
            Toggle[] listOfOptions = battlefieldDropdown.GetComponentsInChildren<Toggle>();
            int highlightedOption = 0;
            for (int i=0; i < battlefieldDropdown.options.Count; i++)
            {
                Toggle option = listOfOptions[i];
                if (EventSystem.current.currentSelectedGameObject == option.gameObject)
                {
                    highlightedOption = i;
                    break;
                }
            }

            int nOptions = battlefieldDropdown.options.Count - 1; // -1 because Unity adds a dummy option
            // The scroll position is 1 at the top of the dropdown and 0 at the bottom
            float scrollPosition = 1 - ((float)highlightedOption / nOptions);
            ScrollRect scroll = battlefieldDropdown.GetComponentInChildren<ScrollRect>();
            scroll.verticalNormalizedPosition = scrollPosition;
        }
    }*/

    public void startGame()
    {
        clickSound.Play();
        SceneManager.LoadScene(chosenBattlefield); // Load the terrain
        SceneManager.LoadScene("BattleScene", LoadSceneMode.Additive); // Add the HUD / battle logic
    }

    void enableMainMenuButtons(bool enable)
    {
        for (int i = 0; i < 5; i++)
            mainMenuButtons[i].interactable = enable;
    }

    public void openOptionsMenu()
    {
        clickSound.Play();
        enableMainMenuButtons(false);
        optionsReturnButton.interactable = true;
        optionsPanel.GetComponent<Animator>().SetTrigger("show");
        //nOfPlayersSlider.Select();
    }

    public void openHowToPlay()
    {
        clickSound.Play();
        enableMainMenuButtons(false);
        howToPlayReturnButton.interactable = true;
        howToPlayPanel.GetComponent<Animator>().SetTrigger("show");
        //howToPlayReturnButton.Select();
    }
    public void openCredits()
    {
        clickSound.Play();
        enableMainMenuButtons(false);
        creditsReturnButton.interactable = true;
        creditsPanel.GetComponent<Animator>().SetTrigger("show");
        //creditsReturnButton.Select();
    }

    public void exitGame()
    {
        clickSound.Play();
        Application.Quit();
    }

    // Options
    public void changeNOfPlayers(Slider slider)
    {
        Cursor.nOfPlayers = (int) slider.value;
        nOfPlayersText.text = slider.value + "";
        cpuSlider.maxValue = slider.value;
        Cursor.nOfCPU = (int) cpuSlider.value;
    }

    public void changeNOfCPU(Slider slider)
    {
        Cursor.nOfCPU = (int) slider.value;
        nOfCPUText.text = slider.value + "";
    }

    public void updateDropdown()
    {
        int option = battlefieldDropdown.value;
        if (battlefieldDropdown.options[option].text == "Random") // If player chooses random map
        {
            enableMapGenerationCanvas(true);
            chosenBattlefield = "BattlefieldRandom";
        } else
        {
            enableMapGenerationCanvas(false);
            // Other battlefield scene names follow the format "Battlefield0", "Battlefield1" ...
            chosenBattlefield = "Battlefield" + option;
        }
    }

    void enableMapGenerationCanvas(bool enable)
    {
        randomMapGenerationCanvas.enabled = enable;
        foreach (Slider s in randomMapGenerationCanvas.GetComponentsInChildren<Slider>())
            s.enabled = enable;
    }

    public void changeMapSize(Slider slider)
    {
        StageGeneration.mapSize = (int)slider.value;
        mapSizeText.text = slider.value + "";
    }

    public void toggleMute(Toggle toggle)
    {
        Cursor.muted = toggle.isOn;
        muteOrUnmuteMusic();
    }

    void muteOrUnmuteMusic()
    {
        foreach (AudioSource a in audioSources)
            a.mute = Cursor.muted;
    }

    // Return from options/how to play/credits
    public void returnToMenu(int buttonToReturnTo)
    {
        clickSound.Play();
        switch (buttonToReturnTo)
        {
            case 1:
                optionsReturnButton.interactable = false;
                optionsPanel.GetComponent<Animator>().SetTrigger("hide");
                break;
            case 2:
                howToPlayReturnButton.interactable = false;
                howToPlayPanel.GetComponent<Animator>().SetTrigger("hide");
                break;
            case 3:
                creditsReturnButton.interactable = false;
                creditsPanel.GetComponent<Animator>().SetTrigger("hide");
                break;
            default:
                break;
        }
        enableMainMenuButtons(true);
        //mainCanvas.transform.GetChild(buttonToReturnTo).GetComponent<Button>().Select();
    }

}

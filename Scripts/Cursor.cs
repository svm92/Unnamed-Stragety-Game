using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class Cursor : MonoBehaviour {

    BattleMenuManager battleMenuManager;

    public GameObject playerBaseModel;
    public GameObject unitGenerationButton;
    public GameObject unitGenerationText;

    public Text infoText;
    public Text damageText;
    public Tile tileSelectorGeneration;
    public Tile tileSelectorMovement;
    public Tile tileSelectorAttack;

    public AudioClip humanTurnMusic;
    public AudioClip cpuTurnMusic;
    public AudioClip victoryMusic;
    public AudioClip hitUnitSound;
    public AudioClip hitBaseSound;
    public AudioClip buzzerSound;
    public AudioClip dingSound;
    public AudioClip summonSound;
    AudioSource musicSource;
    public AudioSource soundSource;
    public static bool muted;

    Canvas infoCanvas;
    Canvas unitActionCanvas;
    Canvas unitGenerationCanvas;
    Canvas mainMenuCanvas;
    Canvas damageCanvas;
    Canvas victoryCanvas;
    Text newTurnText;
    RectTransform infoPanel;
    RectTransform victoryPanel;
    Button moveButton;
    Button attackButton;
    Tilemap terrainTilemap;
    Tilemap secondTilemap;
    Tilemap upperTilemap;
    Transform cameraPosition;

    public PlayerBase[] playerBase;
    public Unit currentlySelectedUnit;
    public int activePlayerIndex = 0;
    public PlayerBase activePlayer;
    public List<Vector3> validTargets = new List<Vector3>();
    public List<Vector3> validAttackTargets = new List<Vector3>();
    List<TileToCheck> listOfAlreadyCheckedTiles = new List<TileToCheck>();
    public int typeOfUnitToGenerateNext = 0;

    struct TileToCheck
    {
        public Vector3 pos;
        public Vector3 posOfParentTile;
        public int maxRangeSoFar;
        public TileToCheck(Vector3 pos, Vector3 posOfParentTile, int maxRangeSoFar)
        {
            this.pos = pos;
            this.posOfParentTile = posOfParentTile;
            this.maxRangeSoFar = maxRangeSoFar;
        }
    }

    int mapX;
    int mapY;

    public static int nOfPlayers;
    public static int nOfCPU;
    int nOfPlayersLeft;
    float timeSinceMoving = 0;
    float timeToWaitUntilMovingAgain = 0.5f;
    float timeSinceLastTouch = 0;
    bool doubleTapping = false;
    public bool activeCursor = true;
    public bool endGame = false;
    AI ai;

    // Modes: 0 - Default, 1 - Generate unit, 2 - Move unit, 3 - Select attack target
    public int targetSelectionMode = 0;
    

    private void Awake()
    {
        battleMenuManager = GameObject.Find("BattleMenuManager").GetComponent<BattleMenuManager>();

        infoCanvas = GameObject.Find("infoCanvas").GetComponent<Canvas>();
        unitActionCanvas = GameObject.Find("unitActionCanvas").GetComponent<Canvas>();
        unitGenerationCanvas = GameObject.Find("unitGenerationCanvas").GetComponent<Canvas>();
        mainMenuCanvas = GameObject.Find("mainMenuCanvas").GetComponent<Canvas>();
        damageCanvas = GameObject.Find("damageCanvas").GetComponent<Canvas>();
        victoryCanvas = GameObject.Find("victoryCanvas").GetComponent<Canvas>();
        infoPanel = GameObject.Find("infoPanel").GetComponent<RectTransform>();
        victoryPanel = GameObject.Find("victoryPanel").GetComponent<RectTransform>();
        moveButton = GameObject.Find("moveButton").GetComponent<Button>();
        attackButton = GameObject.Find("attackButton").GetComponent<Button>();
        terrainTilemap = GameObject.Find("terrainTilemap").GetComponent<Tilemap>();
        secondTilemap = GameObject.Find("secondTilemap").GetComponent<Tilemap>();
        upperTilemap = GameObject.Find("upperTilemap").GetComponent<Tilemap>();
        cameraPosition = GameObject.Find("Main Camera").transform;
        newTurnText = GameObject.Find("newTurnText").GetComponent<Text>();
        musicSource = GetComponents<AudioSource>()[0];
        soundSource = GetComponents<AudioSource>()[1];

        ai = gameObject.AddComponent<AI>();

        unitActionCanvas.enabled = false;
        unitGenerationCanvas.enabled = false;
        mainMenuCanvas.enabled = false;
        victoryCanvas.enabled = false;

        if (muted)
        {
            musicSource.mute = true;
            soundSource.mute = true;
        }
            
    }

    // Use this for initialization
    void Start () {
        // Find X of map
        mapX = 0;
        while (true)
        {
            TileBase currentTile = terrainTilemap.GetTile(new Vector3Int(mapX, 0, 0));
            if (currentTile != null)
                mapX++;
            else
                break;
        }
        // Find Y of map
        mapY = 0;
        while (true)
        {
            TileBase currentTile = terrainTilemap.GetTile(new Vector3Int(0, mapY, 0));
            if (currentTile != null)
                mapY++;
            else
                break;
        }
        mapX--; mapY--;

        if (nOfPlayers == 0)
            nOfPlayers = 2; // Default value
        nOfPlayersLeft = nOfPlayers;
        playerBase = new PlayerBase[nOfPlayers];

        for (int i=0; i < nOfPlayers; i++)
        {
            playerBase[i] = Instantiate(playerBaseModel, chooseRandomPosition(), Quaternion.identity).GetComponent<PlayerBase>();
            playerBase[i].controllerPlayer = i;
            playerBase[i].chooseColor();
            playerBase[i].GetComponent<SpriteRenderer>().color = playerBase[i].baseColor;
        }
        
        activePlayerIndex = chooseRandomStartingPlayer();
        assignCPUPlayers();
        beginNextPlayerTurn();
        createUnitGenerationMenu();

        // If this is a randomly generated stage, generate the map
        GameObject stageGenerationManager = GameObject.Find("stageGenerationManager");
        if (stageGenerationManager != null)
            stageGenerationManager.GetComponent<StageGeneration>().fillMap();
    }

    // Update is called once per frame
    void Update () {
        // Check if double-tapping
        doubleTapping = false;
        if (Input.GetMouseButtonUp(0)) // When lifting the finger, track the time
            timeSinceLastTouch = Time.time;
        else if (Input.GetMouseButtonDown(0)) // When pressing the finger
            if (Time.time - 0.25f <= timeSinceLastTouch) // Check if less than x has passed
                doubleTapping = true;

        if (activeCursor)
        {
            // Smooth cursor movement
            if (Input.GetAxisRaw("Horizontal") > 0 && transform.position.x < mapX)
                moveTowards(Vector3.right);
            if (Input.GetAxisRaw("Horizontal") < 0 && transform.position.x > 0)
                moveTowards(Vector3.left);
            if (Input.GetAxisRaw("Vertical") > 0 && transform.position.y < mapY)
                moveTowards(Vector3.up);
            if (Input.GetAxisRaw("Vertical") < 0 && transform.position.y > 0)
                moveTowards(Vector3.down);

            if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
            {
                timeSinceMoving = 0;
                timeToWaitUntilMovingAgain = 0.5f;
            }

            // On mouse press, move cursor to mouse position
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                int mouseX = (int) Mathf.Round(mousePosition.x);
                mouseX = Mathf.Min(mouseX, mapX);
                mouseX = Mathf.Max(mouseX, 0);
                int mouseY = (int) Mathf.Round(mousePosition.y);
                mouseY = Mathf.Min(mouseY, mapY);
                mouseY = Mathf.Max(mouseY, 0);
                transform.position = new Vector3(mouseX, mouseY);
            }

            /*
            // Touch controls
            if (Input.touches.Count() > 0)
            {
                Vector3 touchPosition = Camera.main.ScreenToWorldPoint(Input.touches[0].position);
                int touchX = (int)Mathf.Round(touchPosition.x);
                touchX = Mathf.Min(touchX, mapX);
                touchX = Mathf.Max(touchX, 0);
                int touchY = (int)Mathf.Round(touchPosition.y);
                touchY = Mathf.Min(touchY, mapY);
                touchY = Mathf.Max(touchY, 0);
                transform.position = new Vector3(touchX, touchY);
            }*/

            if (Input.GetButtonDown("Fire1"))
            {
                switch (targetSelectionMode)
                {
                    case 0: // Default
                        // Select base
                        if (transform.position == activePlayer.transform.position)
                        {
                            if (activePlayer.money >= 100) // If enough money for at least 1 unit
                                chooseTypeOfUnitToGenerate();
                            else
                                buzzer();
                        }
                        else // Select unit
                        {
                            // For each unit under the active player's control
                            foreach (GameObject unit in activePlayer.unitList)
                            {
                                // If the cursor is on that unit
                                if (transform.position == unit.transform.position)
                                {
                                    currentlySelectedUnit = unit.GetComponent<Unit>();
                                    // If the unit hasn't moved or attacked this turn -> Let the player choose
                                    if (!currentlySelectedUnit.alreadyMoved && !currentlySelectedUnit.alreadyAttacked)
                                    {
                                        unitActionCanvas.enabled = true;
                                        moveButton.interactable = true;
                                        attackButton.interactable = true;
                                        //moveButton.Select();
                                        //moveButton.OnSelect(null); // Line needed to highlight the button
                                        activeCursor = false;
                                    } // Else, if the unit hasn't moved this turn -> Move
                                    else if (!currentlySelectedUnit.alreadyMoved)
                                    {
                                        printValidMovementDestinations();
                                    } // Else, if the unit hasn't attacked this turn -> Attack
                                    else if (!currentlySelectedUnit.alreadyAttacked)
                                    {
                                        printValidAttackTargets();
                                    }
                                    else
                                    {
                                        buzzer();
                                    }
                                }
                            }
                        }
                        break;
                    case 1: // Generate Unit
                        if (validTargets.Contains(transform.position))
                            generateUnit();
                        break;
                    case 2: // Move Unit
                        if (validTargets.Contains(transform.position))
                        {
                            activeCursor = false;
                            StartCoroutine(moveUnitToTargetTile(transform.position));
                            targetSelectionMode = 0;
                            GetComponent<Animator>().SetInteger("animationState", 0);
                            currentlySelectedUnit.alreadyMoved = true;
                            clearTargets();
                        }
                        break;
                    case 3: // Select Attack Target
                        if (validAttackTargets.Contains(transform.position))
                        {
                            targetSelectionMode = 0;
                            GetComponent<Animator>().SetInteger("animationState", 0);
                            attack((int)transform.position.x, (int)transform.position.y);
                            currentlySelectedUnit.alreadyAttacked = true;
                            clearTargets();
                        }
                        break;
                    default:
                        break;
                }
            }

            if (Input.GetButtonDown("Fire2")) // Cancel
            {
                targetSelectionMode = 0;
                GetComponent<Animator>().SetInteger("animationState", 0);
                clearTargets();
            }

            if (targetSelectionMode == 0 && doubleTapping && activePlayer.isHuman)
            {
                doubleTapping = false;
                openMainMenu();
            }
        } else if (!activePlayer.isHuman && doubleTapping) // Even during CPU's turn, allow player to abandon
        {
            doubleTapping = false;
            openMainMenu();
        }

        // Cancel out of menus when double tapping
        // Canceling allowed when cursor is inactive or when in any targetting mode other than default (0)
        if (doubleTapping && !endGame && activePlayer.isHuman && (!activeCursor || targetSelectionMode > 0))
            battleMenuManager.cancel();

    }

    private void FixedUpdate()
    {
        // Update game UI
        bool anythingOnCursor = false;
        foreach (GameObject b in GameObject.FindGameObjectsWithTag("Base"))
        {
            if (transform.position == b.transform.position)
            {
                Color panelColor = b.GetComponent<PlayerBase>().baseColor;
                panelColor.a = 0.75f; // Change alpha
                infoPanel.GetComponent<Image>().color = panelColor;
                infoCanvas.enabled = true;
                infoText.text = "<color=red>Health: " + b.GetComponent<PlayerBase>().health + "</color>"
                    + "\n<color=yellow>Gold: " + b.GetComponent<PlayerBase>().money + "</color>"
                    + "\n<color=cyan>Jewels: " + b.GetComponent<PlayerBase>().jewels + "</color>";
                anythingOnCursor = true;
                break;
            }
        }
        if (!anythingOnCursor)
        {
            foreach (GameObject unit in GameObject.FindGameObjectsWithTag("Unit"))
            {
                if (transform.position == unit.transform.position)
                {
                    Color panelColor = playerBase[unit.GetComponent<Unit>().controllerPlayer].baseColor;
                    panelColor.a = 0.75f; // Change alpha
                    infoPanel.GetComponent<Image>().color = panelColor;
                    infoCanvas.enabled = true;
                    Unit u = unit.GetComponent<Unit>();
                    infoText.text = "Hlth: " + u.health + "\tPow: " + u.power + "\n" +
                        "Mov: " + u.movementRange + "\tRng: " + u.range;
                    anythingOnCursor = true;
                    break;
                }
            }
        }
        if (!anythingOnCursor)
        {
            infoCanvas.enabled = false;
            infoText.text = "";
        }
    }

    void LateUpdate()
    {
        // Make camera follow cursor, displaced by (0.5, 0.5)
        Vector3 velocity = Vector3.zero;
        cameraPosition.position = Vector3.SmoothDamp(cameraPosition.position, new Vector3(transform.position.x + 0.5f, transform.position.y + 0.5f, cameraPosition.position.z), ref velocity, 0.1f); 
    }

    void moveTowards(Vector3 direction) // As a key remains pressed, the cursor moves faster with time
    {
        if (timeSinceMoving == 0)
        {
            transform.position += direction;
            timeSinceMoving = Time.time;
        }
        else if (timeSinceMoving < Time.time - timeToWaitUntilMovingAgain) // If enough time has passed since last moving
        {
            transform.position += direction;
            timeSinceMoving = Time.time;
            timeToWaitUntilMovingAgain -= 0.15f;
            if (timeToWaitUntilMovingAgain < 0.05f) // Hard lower limit to avoid an uncontrollable cursor
                timeToWaitUntilMovingAgain = 0.05f;
        }
    }

    void assignCPUPlayers()
    {
        for (int i = 1; i <= nOfCPU; i++)
            playerBase[nOfPlayers - i].isHuman = false; // Gives CPU control to bases (starting from right to left)
    }

    void openMainMenu()
    {
        mainMenuCanvas.enabled = true;
        Transform mainMenuPanel = mainMenuCanvas.transform.GetChild(0);
        mainMenuPanel.transform.GetComponent<Image>().enabled = true;
        foreach (Button b in mainMenuPanel.GetComponentsInChildren<Button>())
        {
            b.interactable = true;
            b.transform.localScale = Vector3.one;
        }
            
        if (activePlayer.jewels > 0)
        {
            //mainMenuPanel.GetChild(0).GetComponent<Button>().Select(); // Select first button
        }
        else
        {
            mainMenuPanel.GetComponentInChildren<Button>().interactable = false; // Disable first button
            //mainMenuPanel.GetChild(1).GetComponent<Button>().Select(); // Select second button
        }

        if (!activePlayer.isHuman) // During CPU's turn, allow only third button (Abandon)
        {
            mainMenuPanel.GetComponentsInChildren<Button>()[0].interactable = false;
            mainMenuPanel.GetComponentsInChildren<Button>()[1].interactable = false;
            mainMenuPanel.GetComponentsInChildren<Button>()[0].transform.localScale = Vector3.zero; // Make them invisible
            mainMenuPanel.GetComponentsInChildren<Button>()[1].transform.localScale = Vector3.zero;
            mainMenuPanel.GetComponent<Image>().enabled = false;
        }

        activeCursor = false;
    }

    public void updateImprovementCanvasText()
    {
        // Change jewel text
        GameObject.Find("jewelText").GetComponent<Text>().text = "Jewels: " + activePlayer.jewels;
        // Change text of levels
        for (int i = 0; i < 5; i++)
            GameObject.Find("levelText" + i).GetComponent<Text>().text = "Lv " + activePlayer.improvements[i];
    }

    void createUnitGenerationMenu()
    {
        int nUnitTypes = activePlayer.unit.Length;
        GameObject[] buttons = new GameObject[nUnitTypes];
        GameObject[] descriptions = new GameObject[nUnitTypes];
        float xPosition = -300f;
        float yPosition = -20f;
        BattleMenuManager battleMenuManager = GameObject.Find("BattleMenuManager").GetComponent<BattleMenuManager>();
        Transform unitGenerationPanel = unitGenerationCanvas.transform.GetChild(0);
        for (int i = 0; i < nUnitTypes; i++)
        {
            // Create buttons
            buttons[i] = Instantiate(unitGenerationButton, Vector3.zero, Quaternion.identity);
            buttons[i].transform.SetParent(unitGenerationPanel); // Set as child of unitGenerationPanel
            buttons[i].transform.name = "generationButton" + i;
            buttons[i].transform.localScale = Vector3.one; // Scale to x1
            buttons[i].transform.localPosition = new Vector3(xPosition, 100f);
            buttons[i].transform.localPosition += Vector3.down * yPosition;
            buttons[i].GetComponent<Button>().onClick.AddListener(battleMenuManager.create);
            buttons[i].GetComponent<Button>().interactable = false;
            
            // Create unit descriptions
            descriptions[i] = Instantiate(unitGenerationText, Vector3.zero, Quaternion.identity);
            descriptions[i].transform.SetParent(unitGenerationPanel); // Set as child of unitGenerationPanel
            descriptions[i].transform.name = "generationText" + i;
            descriptions[i].transform.localScale = Vector3.one; // Scale to x1
            descriptions[i].transform.localPosition = new Vector3(xPosition + 150f, 100f);
            descriptions[i].transform.localPosition += Vector3.down * yPosition;
            descriptions[i].GetComponent<Text>().text = Unit.baseUnitName[i] + " (" + Unit.baseCost[i] + ")\n" +
                "Health: " + Unit.baseHealth[i] + "\tPower: " + Unit.basePower[i] + "\n" +
                "Move: " + Unit.baseMovementRange[i] + "\tRange: " + Unit.baseRange[i];

            yPosition += 65f;
            if (i == 4) // Prepare for second column
            {
                xPosition += 350f;
                yPosition = -20f;
            }  
        }
    }

    void updateGenerationMenu()
    {
        int nUnitTypes = activePlayer.unit.Length;
        for (int i = 0; i < nUnitTypes; i++)
        {
            // Update unit descriptions
            GameObject.Find("generationText" + i).GetComponent<Text>().text = Unit.baseUnitName[i] + " (" + Unit.baseCost[i] + ")\n" +
                "Health: " + (Unit.baseHealth[i] + activePlayer.improvements[2]) +
                "\tPower: " + (Unit.basePower[i] + activePlayer.improvements[3]) + "\n" +
                "Move: " + (Unit.baseMovementRange[i] + activePlayer.improvements[1]) +
                "\tRange: " + (Unit.baseRange[i] + activePlayer.improvements[4]);
        }
    }

    public void enableAllUnitGenerationButtons()
    {
        Transform unitGenerationPanel = unitGenerationCanvas.transform.GetChild(0).transform;
        foreach (Button b in unitGenerationPanel.GetComponentsInChildren<Button>())
        {
            if (b.name == "Cancel")
            {
                b.interactable = true;
                continue;
            }

            // Find cost of the unit
            int buttonNumber = BattleMenuManager.getButtonNumber(b.name);
            int neededMoney = Unit.baseCost[buttonNumber];

            // Only allow interaction with a button if that player has enough money to buy the unit
            if (activePlayer.money >= neededMoney)
                b.interactable = true;
        }
    }

    public void disableAllUnitGenerationButtons()
    {
        Transform unitGenerationPanel = unitGenerationCanvas.transform.GetChild(0).transform;
        foreach (Button b in unitGenerationPanel.GetComponentsInChildren<Button>())
            b.interactable = false;
    }

    int chooseRandomStartingPlayer()
    {
        int player = Random.Range(0, nOfPlayers);
        return player;
    }

    Vector3 chooseRandomPosition()
    {
        int x = 0; int y = 0;
        bool validTile = false;
        while (!validTile)
        {
            x = Random.Range(0, mapX+1);
            y = Random.Range(0, mapY+1);
            if (!tileIsImpassable(x, y)) // If tile is passable
            {
                validTile = true;
                foreach (PlayerBase p in playerBase) // Check that there is no base in the randomly chosen tile
                    if (p != null && x == p.transform.position.x && y == p.transform.position.y)
                    {
                        validTile = false;
                        break;
                    }
            }
        }
        return new Vector3(x, y);
    }

    void chooseTypeOfUnitToGenerate()
    {
        updateGenerationMenu();
        unitGenerationCanvas.GetComponent<Animator>().SetTrigger("showMenu");
        unitGenerationCanvas.enabled = true;
        GameObject.Find("moneyText").GetComponent<Text>().text = "Gold: " + activePlayer.money;
        enableAllUnitGenerationButtons();
        //Transform unitGenerationPanel = unitGenerationCanvas.transform.GetChild(0).transform;
        //Button firstButton = unitGenerationPanel.GetChild(1).GetComponent<Button>();
        //firstButton.Select(); // Select first button
        //firstButton.OnSelect(null); // Line needed to highlight the button
        activeCursor = false;
    }

    // Shows valid targets for unit generation (ignores blocked paths)
    public void printValidUnitGenerationTargets()
    {
        int posX = (int)transform.position.x;
        int posY = (int)transform.position.y;
        targetSelectionMode = 1;
        GetComponent<Animator>().SetInteger("animationState", 1);
        int maxRange = activePlayer.range;
        validTargets.Clear();
        

        checkValidUnitGenerationTargets(posX, posY, maxRange);

        if (validTargets.Count == 0)
        {
            targetSelectionMode = 0;
            GetComponent<Animator>().SetInteger("animationState", 0);
            buzzer();
        }
    }

    // Adds to "validTargets" all valid tiles within range, ignoring blocked paths
    // Creates a rhombus shape centered on (x, y) with radius = maxRange
    void checkValidUnitGenerationTargets(int posX, int posY, int maxRange)
    {
        int rhombusWidth = 0;
        bool rhombusCenterReached = false;
        for (int i=posX - maxRange; i <= posX + maxRange; i++)
        {
            for (int j=posY - rhombusWidth; j <= posY + rhombusWidth; j++)
            {
                if (validDestination(i, j))
                {
                    // Add target to list of valid targets
                    upperTilemap.SetTile(new Vector3Int(i, j, 0), tileSelectorGeneration);
                    validTargets.Add(new Vector3(i, j));
                }
            }

            if (rhombusCenterReached)
                rhombusWidth--;
            else
                rhombusWidth++;

            if (rhombusWidth == maxRange)
                rhombusCenterReached = true;

        }
    }

    public void generateUnit()
    {
        soundSource.PlayOneShot(summonSound);

        targetSelectionMode = 0;
        GetComponent<Animator>().SetInteger("animationState", 0);
        activePlayer.createUnit(transform.position.x, transform.position.y, typeOfUnitToGenerateNext);
        activePlayer.scoreTypes[0]++; // Increase score for unit generation
        clearTargets();
    }

    // Shows valid targets for movement (takes into account blocked paths)
    public void printValidMovementDestinations()
    {
        targetSelectionMode = 2;
        GetComponent<Animator>().SetInteger("animationState", 2);
        int posX = (int) currentlySelectedUnit.transform.position.x;
        int posY = (int) currentlySelectedUnit.transform.position.y;
        int maxRange = currentlySelectedUnit.movementRange;
        validTargets.Clear();
        listOfAlreadyCheckedTiles.Clear();

        checkValidMovementDestinationsRecursively(posX, posY, maxRange);

        if (validTargets.Count == 0)
        {
            targetSelectionMode = 0;
            GetComponent<Animator>().SetInteger("animationState", 0);
            buzzer();
        }
    }

    // Adds to "validTargets" all valid tiles within range, taking into account blocked paths
    void checkValidMovementDestinationsRecursively(int posX, int posY, int rangeLeft)
    {
        Vector3 pos = new Vector3(posX, posY);

        // Upper tile
        checkIfTileIsValidForMovement(posX, posY+1, pos, rangeLeft);
        // Lower tile
        checkIfTileIsValidForMovement(posX, posY-1, pos, rangeLeft);
        // Right tile
        checkIfTileIsValidForMovement(posX+1, posY, pos, rangeLeft);
        // Left tile
        checkIfTileIsValidForMovement(posX-1, posY, pos, rangeLeft);
    }

    void checkIfTileIsValidForMovement(int posX, int posY, Vector3 parent, int rangeLeft)
    {
        Vector3 pos = new Vector3(posX, posY);

        // Check if the tile has already been explored (FindIndex returns -1 otherwise)
        if (listOfAlreadyCheckedTiles.FindIndex(x => x.pos == pos) >= 0)
        {
            TileToCheck tile = listOfAlreadyCheckedTiles.Find(x => x.pos == pos);
            if (tile.maxRangeSoFar >= rangeLeft)
                return; // Stop exploring, since that path has already been explored before
            else // Otherwise, remove the tile (it has an incorrect maxRangeSoFar)
                listOfAlreadyCheckedTiles.Remove(tile);
            // Then keep exploring (the tile will be readded later)
        }

        if (validDestination(posX, posY))
        { // Add tile to list of valid targets
            upperTilemap.SetTile(new Vector3Int(posX, posY, 0), tileSelectorMovement);
            validTargets.Add(pos);
            listOfAlreadyCheckedTiles.Add( new TileToCheck(pos, parent, rangeLeft) );
            // Keep checking if there is still range left
            if (rangeLeft > 1)
                checkValidMovementDestinationsRecursively(posX, posY, rangeLeft - 1);
        } else if (allyInTile(posX, posY))
        {
            listOfAlreadyCheckedTiles.Add(new TileToCheck(pos, parent, rangeLeft));
            // If the tile is occupied by an ally, the unit cannot move there, but can pass through there
            if (rangeLeft > 1)
                checkValidMovementDestinationsRecursively(posX, posY, rangeLeft - 1);
        }
    }

    bool validDestination(int posX, int posY)
    {
        Vector3 tilePosition = new Vector3(posX, posY);
        // Invalid if tile is out of map
        if (posX < 0 || posX > mapX || posY < 0 || posY > mapY)
            return false;
        // Invalid if terrain is impassable
        if (tileIsImpassable(posX, posY))
            return false;
        // Invalid if terrain occupied by unit
        foreach (GameObject u in GameObject.FindGameObjectsWithTag("Unit"))
            if (u.transform.position == tilePosition)
                return false;
        // Invalid if terrain occupied by base
        foreach (GameObject b in GameObject.FindGameObjectsWithTag("Base"))
            if (b.transform.position == tilePosition)
                return false;
        // Valid otherwise
        return true;
    }

    bool allyInTile(int posX, int posY)
    {
        Vector3 tilePosition = new Vector3(posX, posY);
        // Tile occupied by ally unit
        foreach (GameObject u in activePlayer.unitList)
        {
            if (u.transform.position == tilePosition)
                return true;
        }
        // Tile occupied by own base
        if (activePlayer.transform.position == tilePosition)
            return true;
        // Invalid otherwise
        return false;
    }

    public IEnumerator moveUnitToTargetTile(Vector3 destination)
    {
        List<Vector3> path = new List<Vector3>();
        Vector3 nextTile = destination;

        // Create path backwards (going from the destination to the unit's starting position)
        while (nextTile != currentlySelectedUnit.transform.position)
        {
            TileToCheck t = listOfAlreadyCheckedTiles.Find(x => x.pos == nextTile);
            path.Add(t.pos);
            nextTile = t.posOfParentTile;
        }
        path.Reverse();
        int pathIndex = 0;
        float timeSinceStarted = 0f;
        float movementSpeed = path.Count * 2f;

        // Move the unit tile by tile
        while (pathIndex < path.Count)
        {
            // Make the unit move smoothly towards the next tile in the path
            timeSinceStarted += Time.deltaTime;
            currentlySelectedUnit.transform.position = Vector3.Lerp(currentlySelectedUnit.transform.position, path[pathIndex], timeSinceStarted * movementSpeed);
            
            // If the unit reaches the tile, prepare for the next tile in the path
            if (currentlySelectedUnit.transform.position == path[pathIndex])
            {
                pathIndex++;
                timeSinceStarted = 0;
            }

            // Go on to next frame8
            yield return null;
        }

        // Once the unit finishes the path, stop the coroutine and return control to the cursor
        activeCursor = true;
        currentlySelectedUnit.transform.position = destination; // Correct decimal deviations
        yield break;
    }

    // Creates a rhombus centered on unit's position
    public void printValidAttackTargets()
    {
        targetSelectionMode = 3;
        GetComponent<Animator>().SetInteger("animationState", 3);
        int posX = (int) currentlySelectedUnit.transform.position.x;
        int posY = (int)currentlySelectedUnit.transform.position.y;
        int maxRange = currentlySelectedUnit.range;
        validTargets.Clear();
        validAttackTargets.Clear();

        int rhombusWidth = 0;
        bool rhombusCenterReached = false;
        for (int i = posX - maxRange; i <= posX + maxRange; i++)
        {
            for (int j = posY - rhombusWidth; j <= posY + rhombusWidth; j++)
            {
                if (posX == i && posY == j) // Skip unit's own tile
                    continue;

                // Skil if tile is out of map
                if (i < 0 || i > mapX || j < 0 || j > mapY)
                    continue;

                // Show the attack range
                upperTilemap.SetTile(new Vector3Int(i, j, 0), tileSelectorAttack);
                validTargets.Add(new Vector3(i, j)); // Tiles within range (regardless of whether there is an enemy there)

                if (attackTargetIn(i, j) != null)
                {
                    // Add target to list of valid targets
                    validAttackTargets.Add(new Vector3(i, j));
                }
            }

            if (rhombusCenterReached)
                rhombusWidth--;
            else
                rhombusWidth++;

            if (rhombusWidth == maxRange)
                rhombusCenterReached = true;
        }

        if (validAttackTargets.Count == 0)
        {
            targetSelectionMode = 0;
            clearTargets();
            GetComponent<Animator>().SetInteger("animationState", 0);
            buzzer();
        }
    }

    public GameObject attackTargetIn(int posX, int posY) // Returns the target in tile (x,y), or null if it's empty
    {
        Vector3 tilePosition = new Vector3(posX, posY);

        // Check all bases
        for (int i=0; i < playerBase.Length; i++)
        {
            if (playerBase[i] == null) // Skip defeated players
                continue;
            // Skip the currently active player's base
            if (i == activePlayerIndex)
                continue;
            // Check if it is an enemy base
            if (playerBase[i].transform.position == tilePosition)
                return playerBase[i].gameObject;
            // Check if it is an enemy unit
            foreach (GameObject unit in playerBase[i].unitList)
                if (unit.transform.position == tilePosition) // If there is an enemy unit in that tile
                    return unit;
        }
        return null;
    }

    public void attack(int posX, int posY)
    {
        Vector3 tilePosition = new Vector3(posX, posY);
        // Check all bases
        for (int i = 0; i < playerBase.Length; i++)
        {
            if (playerBase[i] == null) // Skip defeated players
                continue;
            // Skip the currently active player's base
            if (i == activePlayerIndex)
                continue;
            // Check if it is an enemy base
            if (playerBase[i].transform.position == tilePosition)
            {
                dealDamageToBase(currentlySelectedUnit.power, playerBase[i]);
                return;
            }
            // Check if it is an enemy unit
            foreach (GameObject unit in playerBase[i].unitList)
            {
                if (unit.transform.position == tilePosition) // Find the attack target
                {
                    // Attacker - currentlySelectedUnit, Attacked - unit
                    dealDamageToUnit(currentlySelectedUnit.power, unit.GetComponent<Unit>());
                    return;
                }
            }
        }
    }

    void dealDamageToUnit(int damage, Unit targetUnit)
    {
        soundSource.PlayOneShot(hitUnitSound);

        StartCoroutine(popupText(damage + "", targetUnit.gameObject, Color.red));
        targetUnit.health -= damage;
        if (targetUnit.health <= 0)
        {
            kill(targetUnit);
            activePlayer.scoreTypes[1]++;
        }
    }

    void dealDamageToBase(int damage, PlayerBase targetBase)
    {
        soundSource.PlayOneShot(hitBaseSound);

        StartCoroutine(popupText(damage + "", targetBase.gameObject, Color.red));
        targetBase.health -= damage;
        if (targetBase.health <= 0)
        {
            defeatPlayer(targetBase.controllerPlayer);
            activePlayer.scoreTypes[2]++;
        }
    }

    void defeatPlayer(int target)
    {
        foreach (GameObject u in playerBase[target].unitList)
            Destroy(u);

        Destroy(playerBase[target].gameObject);
        playerBase[target] = null;
        nOfPlayersLeft--;

        if (nOfPlayersLeft == 1) // If there is only one base left, declare a winner
        {
            endGame = true;
            activeCursor = false;
            StartCoroutine(showVictoryScreen());
        }
    }

    IEnumerator showVictoryScreen()
    {
        musicSource.clip = victoryMusic;
        musicSource.Play();

        yield return new WaitForSeconds(1);
        victoryCanvas.enabled = true;
        activePlayer.calculateScore();

        // Prepare texts to show
        string victoryTextTitle = "Player " + (activePlayerIndex + 1) + " wins!";
        string victoryTextLeft = "Nº units generated: " + activePlayer.scoreTypes[0] + "\n" +
        "Nº units defeated: " + activePlayer.scoreTypes[1] + "\n" +
        "Nº bases destroyed: " + activePlayer.scoreTypes[2] + "\n" +
        "Nº improvements: " + activePlayer.scoreTypes[3] + "\n" +
        "Health left: " + activePlayer.health + "\n" +
        "Total score:";
        string victoryTextRight = activePlayer.scoreTypes[0] * 100 + "pts\n" +
            activePlayer.scoreTypes[1] * 250 + "pts\n" +
            activePlayer.scoreTypes[2] * 1000 + "pts\n" +
            activePlayer.scoreTypes[3] * 100 + "pts\n" +
            activePlayer.health * 300 + "pts\n" +
            activePlayer.score + "pts";

        // Assign texts
        victoryPanel.GetChild(1).GetComponent<Text>().text = victoryTextTitle;
        victoryPanel.GetChild(1).GetComponent<Text>().color = activePlayer.baseColor;
        victoryPanel.GetChild(2).GetComponent<Text>().text = victoryTextLeft;
        victoryPanel.GetChild(3).GetComponent<Text>().text = victoryTextRight;
        victoryPanel.GetComponent<Animator>().SetTrigger("show");

        victoryPanel.GetChild(0).GetComponent<Button>().interactable = true;
        //victoryPanel.GetChild(0).GetComponent<Button>().Select(); // Select return button
    }

    public IEnumerator popupText(string textToShow, GameObject target, Color color)
    {
        damageCanvas.transform.position = target.transform.position;

        Text textPopup = Instantiate(damageText, target.transform.position, Quaternion.identity);
        textPopup.transform.SetParent(damageCanvas.transform);
        textPopup.text = textToShow;
        textPopup.color = color;
        textPopup.GetComponent<Animator>().SetTrigger("startAnimation");
        yield return new WaitForSeconds(1f);
        Destroy(textPopup);
    }

    void kill(Unit unit)
    {
        int unitController = unit.controllerPlayer;
        playerBase[unitController].unitList.Remove(unit.gameObject);
        Destroy(unit.gameObject);
    }

    public void clearTargets()
    {
        foreach (Vector3 pos in validTargets)
        {
            upperTilemap.SetTile(new Vector3Int((int)pos.x, (int)pos.y, 0), null);
        }
    }

    public void beginNextPlayerTurn()
    {
        // Change active player
        chooseNextPlayer();

        // Cancel out of active menus when starting a human player's turn
        if (activePlayer.isHuman)
            battleMenuManager.cancel();

        // Change cursor color
        GetComponent<SpriteRenderer>().color = activePlayer.baseColor;

        // Move cursor to base
        transform.position = new Vector3(activePlayer.transform.position.x, activePlayer.transform.position.y);

        // Add 100 money for each turn played
        activePlayer.nTurns++;
        int moneyToAdd = 100 * activePlayer.nTurns;
        activePlayer.money += moneyToAdd;

        // Add 1 jewel for each turn played
        int jewelsToAdd = activePlayer.nTurns;
        activePlayer.jewels += jewelsToAdd;

        // Change music depeneding on whether player is human or CPU
        if (activePlayer.isHuman && musicSource.clip != humanTurnMusic)
        {
            musicSource.clip = humanTurnMusic;
            musicSource.Play();
        }
        else if (!activePlayer.isHuman && musicSource.clip != cpuTurnMusic)
        {
            musicSource.clip = cpuTurnMusic;
            musicSource.Play();
        }
            
        // Reallow movement/attack for that player's units
        foreach (GameObject unit in activePlayer.unitList)
        {
            unit.GetComponent<Unit>().alreadyMoved = false;
            unit.GetComponent<Unit>().alreadyAttacked = false;
        }
        StartCoroutine(showNewTurnPanel(moneyToAdd, jewelsToAdd));
    }

    void chooseNextPlayer()
    {
        while (true)
        {
            activePlayerIndex++;
            if (activePlayerIndex >= nOfPlayers) // If it is the last player, roll back to the first one
                activePlayerIndex = 0;

            activePlayer = playerBase[activePlayerIndex];

            // Check that the player exists (hasn't been defeated)
            if (activePlayer != null) // Player exists, so choose that player
                return;
            // Otherwise, keep trying to find an existing player
        }
    }

    IEnumerator showNewTurnPanel(int moneyToAdd, int jewelsToAdd)
    {
        activeCursor = false;
        newTurnText.color = activePlayer.baseColor;
        newTurnText.text = "Player " + (activePlayerIndex + 1); // +1 because players start at 0
        newTurnText.GetComponent<Animator>().SetTrigger("show");
        yield return new WaitForSeconds(1.5f);
        newTurnText.GetComponent<Animator>().SetTrigger("hide");
        yield return new WaitForSeconds(0.25f);

        if (activePlayer.isHuman)
            activeCursor = true;
        else // If next player is CPU
        {
            ai.CPU = activePlayer;
            StartCoroutine(ai.beginAITurn());
        }

        // Play money sound
        soundSource.PlayOneShot(dingSound);

        // Show money/jewels popups
        StartCoroutine(popupText(moneyToAdd + "", activePlayer.gameObject, Color.yellow));
        yield return new WaitForSeconds(0.35f);
        StartCoroutine(popupText(jewelsToAdd + "", activePlayer.gameObject, Color.cyan));
    }

    string getCurrentCursorTile()
    {
        TileBase currentTile = secondTilemap.GetTile(new Vector3Int((int)transform.position.x, (int)transform.position.y, 0));
        if (currentTile == null)
            return "";
        return currentTile.name;
    }

    string getTileAt(int posX, int posY)
    {
        // If out of bounds, return prematurely to avoid crashing
        if (posX < 0 || posX > mapX || posY < 0 || posY > mapY)
            return "";
        TileBase currentTile = secondTilemap.GetTile(new Vector3Int(posX, posY, 0));
        if (currentTile == null)
            return "";
        return currentTile.name;
    }

    public bool tileIsImpassable(int posX, int posY)
    {
        string tileName = getTileAt(posX, posY);

        //tileset0_29, tileset0_30 - Trees
        //tileset0_148 - 170 - Water
        List<string> impassableTiles = new List<string> { "tileset0_29", "tileset0_30" };
        for (int i = 148; i <= 170; i++)
            impassableTiles.Add("tileset0_" + i); // Add all water tiles

        return impassableTiles.Contains(tileName);
        
    }

    public void buzzer()
    {
        if (activePlayer.isHuman)
            soundSource.PlayOneShot(buzzerSound);
    }

}

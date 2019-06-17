using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StageGeneration : MonoBehaviour {

    public Tile groundTile;
    public Tile obstacleTile;

    Tilemap terrainTilemap;
    Tilemap secondTilemap;

    public static int mapSize = 10;

    struct RandomTile
    {
        public Vector3 position;
        public int owner; // -2 for obstacle tiles, -1 for passable tiles, 0+ for passable tiles surrounding a base
        public RandomTile(Vector3 position)
        {
            this.position = position;
            owner = -2; // Considered an obstacle by default
        }

        public bool isObstacle()
        {
            return owner == -2;
        }
    }
    List<RandomTile> randomTiles = new List<RandomTile>();

    private void Awake()
    {
        terrainTilemap = GameObject.Find("terrainTilemap").GetComponent<Tilemap>();
        secondTilemap = GameObject.Find("secondTilemap").GetComponent<Tilemap>();

        generateFloor();
    }

    void generateFloor()
    {
        generateGround();
        generateSurroundingWall();
    }

    public void fillMap()
    {
        createPaths();
        generateObstacles();
    }

    void generateGround()
    {
        for (int i=0; i < mapSize+2; i++) // +2 to account for the surrounding stage wall
            for (int j=0; j < mapSize+2; j++)
            {
                terrainTilemap.SetTile(new Vector3Int(i, j, 0), groundTile);

                if (i != 0 && j != 0 && i != mapSize+1 && j != mapSize+1) // If not a tile from the surrounding wall
                    randomTiles.Add( new RandomTile( new Vector3(i,j) ) ); // Add tile to list of tiles to consider
            }      
    }

    void generateSurroundingWall()
    {
        // Upper wall
        for (int i = 0; i < mapSize + 2; i++)
            secondTilemap.SetTile(new Vector3Int(i, 0, 0), obstacleTile);
        // Lower wall
        for (int i = 0; i < mapSize + 2; i++)
            secondTilemap.SetTile(new Vector3Int(i, mapSize + 1, 0), obstacleTile);
        // Left wall
        for (int i = 1; i < mapSize + 1; i++)
            secondTilemap.SetTile(new Vector3Int(0, i, 0), obstacleTile);
        // Right wall
        for (int i = 1; i < mapSize + 1; i++)
            secondTilemap.SetTile(new Vector3Int(mapSize + 1, i, 0), obstacleTile);
    }

    void createPaths()
    {
        Cursor cursor = GameObject.Find("Cursor").GetComponent<Cursor>();
        PlayerBase[] listOfBases = cursor.playerBase;

        foreach (PlayerBase b in listOfBases)
            determineBaseTerritory(b);

        // Choose random base to start creating paths to join all bases
        tracePaths(listOfBases);
    }

    void determineBaseTerritory(PlayerBase b) // For a given base, make it own all of its surrounding tiles
    {
        foreach (Vector3 vVertical in new Vector3[] { Vector3.up, Vector3.down, Vector3.zero })
            foreach (Vector3 vHorizontal in new Vector3[] { Vector3.right, Vector3.left, Vector3.zero })
            {
                Vector3 positionToCheck = b.transform.position + vVertical + vHorizontal;
                if (positionToCheck == b.transform.position) // If checking the base's own position
                    forceChangeTileOwner(positionToCheck, b.controllerPlayer);
                else
                    changeTileOwner(positionToCheck, b.controllerPlayer);
            }
    }

    void changeTileOwner(Vector3 pos, int newOwner, bool forceChange) // Change owner of tile at position "pos"
    {
        if (!randomTiles.Exists(x => x.position == pos)) // Skip if no tile in that position
            return;

        RandomTile t = randomTiles.Find(x => x.position == pos); // Fetch struct value (NOT passed by reference)
        randomTiles.Remove(t); // Remove old struct
        if (t.owner == -2 || forceChange) // If no owner has been defined yet, give it a new owner
            t.owner = newOwner;
        randomTiles.Add(t);
    }

    void changeTileOwner(Vector3 pos, int newOwner)
    {
        changeTileOwner(pos, newOwner, false); // By default, don't force the change
    }
 
    void forceChangeTileOwner(Vector3 pos, int newOwner)
    {
        changeTileOwner(pos, newOwner, true);
    }

    // Start at b and move in random directions until all other bases have been found
    void tracePaths(PlayerBase[] listOfBases)
    {
        int rnd = Random.Range(0, listOfBases.Length);
        PlayerBase b = listOfBases[rnd]; // Choose random base to start the search
        Vector3 currentPosition = b.transform.position;

        bool[] listOfFoundBases = new bool[ listOfBases.Length ]; // false for bases not yet found, true for found
        listOfFoundBases[b.controllerPlayer] = true; // Consider 'b' as found
        bool allBasesFound = false;

        while (!allBasesFound)
        {
            Vector3 nextPosition = moveInRandomDirection(currentPosition);
            // IF there is a passable tile in the position to check
            if (randomTiles.Exists( x => x.position == nextPosition))
            {
                currentPosition = nextPosition;
                // -2 is impassable, -1 is passable, 0+ are the tiles surrounding a base
                int owner = randomTiles.Find(x => x.position == currentPosition).owner;
                if (owner == -2)
                    changeTileOwner(currentPosition, -1); // Change from -2 to -1 (make it passable terrain)
                else if (owner >= 0)
                {
                    listOfFoundBases[owner] = true; // Find a new base
                    allBasesFound = checkIfAllBasesFound(listOfFoundBases);
                }
            }
        }
    }

    Vector3 moveInRandomDirection(Vector3 currentPosition)
    {
        Vector3[] randomDirs = new Vector3[] { Vector3.up, Vector3.down, Vector3.right, Vector3.left };
        int rnd = Random.Range(0, 4);
        Vector3 randomDir = randomDirs[rnd];

        return currentPosition + randomDir;
    }

    bool checkIfAllBasesFound(bool[] listOfFound)
    {
        foreach (bool b in listOfFound)
            if (!b)
                return false; // If even a single base hasn't been found yet, false
        return true; // true only if all bases have been foound
    }

    void generateObstacles()
    {
        foreach (RandomTile t in randomTiles)
            if (t.isObstacle())
                secondTilemap.SetTile(new Vector3Int((int)t.position.x, (int)t.position.y, 0), obstacleTile);
    }

}

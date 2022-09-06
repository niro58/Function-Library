using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BSP_Generation : EditorWindow
{
    private int iterations = 0;
    private Vector2Int gridSize;
    private Vector2Int minRoomSize;

    private Tilemap bspTilemap;
    private TileBase borderTile;
    private TileBase roomTile;
    private TileBase corridorTile;

    private System.Random random;
    private int seed = 0;
    [MenuItem("Library/BSP Generation")]
    public static void ShowWindow()
    {
        GetWindow<BSP_Generation>("BSP Generation");
        
    }
    private void OnGUI()
    {
        int iterations_ = iterations;
        Vector2Int gridSize_ = gridSize;
        int seed_ = seed;
        Vector2Int minRoomSize_ = minRoomSize;
        TileBase borderTile_ = borderTile;
        TileBase roomTile_ = roomTile;
        TileBase corridorTile_ = corridorTile;

        iterations = EditorGUILayout.IntField("Iterations : ", iterations);
        gridSize = EditorGUILayout.Vector2IntField("Grid Size : ", gridSize);
        minRoomSize = EditorGUILayout.Vector2IntField("Min Room Size : ", minRoomSize);
        seed = EditorGUILayout.IntField("Seed : ", seed);
        borderTile = (TileBase)EditorGUILayout.ObjectField("Border Tile : ", borderTile, typeof(TileBase), false);
        roomTile = (TileBase)EditorGUILayout.ObjectField("Room Tile : ", roomTile, typeof(TileBase), false);
        corridorTile = (TileBase)EditorGUILayout.ObjectField("Corridor Tile : ", corridorTile, typeof(TileBase), false);

        OnValidate();
        bool OnValueChanged = iterations != iterations_ || gridSize != gridSize_ || seed != seed_ || minRoomSize != minRoomSize_ || borderTile != borderTile_ || roomTile != roomTile_ || corridorTile != corridorTile_;
        bool OnCorrectRules = roomTile != null && borderTile != null && corridorTile != null && gridSize.x > minRoomSize.x && gridSize.y > minRoomSize.y;
        if (OnValueChanged && OnCorrectRules)
        {


            BSPInitialize("BSP_Generation/");
            BSPGenerate(iterations, gridSize, seed);
        }

    }
    public void OnValidate()
    {
        if (iterations < 1) iterations = 1;
        if (gridSize.x < 1) gridSize.x = 1;
        if (gridSize.y < 1) gridSize.y = 1;
        if (minRoomSize.x < 1) minRoomSize.x = 1;
        if (minRoomSize.y < 1) minRoomSize.y = 1;
        if (seed < 1) seed = 1;
    }
    private void BSPInitialize(string folderPath)
    {
        if (bspTilemap != null)
        {
            bspTilemap.ClearAllTiles(); 
            return;
        }

        GameObject grid = new GameObject("BSP Generation");
        grid.AddComponent<Grid>();
        GameObject bspTilemap_ = new GameObject("Tilemap");
        bspTilemap_.transform.parent = grid.transform;
        bspTilemap_.AddComponent<Tilemap>();
        bspTilemap_.AddComponent<TilemapRenderer>();
        bspTilemap = bspTilemap_.GetComponent<Tilemap>();

    }
    private void BSPGenerate(int iterations, Vector2Int gridSize, int seed)
    {
        random = new System.Random(seed);
        Dungeon dungeon = new Dungeon(new Vector2Int(0, gridSize.x), new Vector2Int(0, gridSize.y));
        CreateDungeon(iterations, dungeon);
        CreateCorridors(dungeon);
    }

    public void CreateDungeon(int iterations, Dungeon dungeon)
    {
        DrawTiles(dungeon.xPos, dungeon.yPos, borderTile, false);
        iterations -= 1;
        if (iterations == 0 || !SplitDungeon(dungeon))
        {
            CreateRoom(dungeon);
            return;
        }
        CreateDungeon(iterations, dungeon.right);
        CreateDungeon(iterations, dungeon.left);
    }
    public bool SplitDungeon(Dungeon currDungeon)
    {
        Vector2Int xPosLeft = currDungeon.xPos;
        Vector2Int yPosLeft = currDungeon.yPos;

        Vector2Int xPosRight = currDungeon.xPos;
        Vector2Int yPosRight = currDungeon.yPos;

        int dir = random.Next(0, 2); // 0 = x, 1 = y
        int additionalSpace = 3;
        if (dir == 0)
        {
            if (currDungeon.xPos.x + minRoomSize.x + additionalSpace >= currDungeon.xPos.y - minRoomSize.x - additionalSpace) return false;

            int splitPos = random.Next(currDungeon.xPos.x + minRoomSize.x + additionalSpace, currDungeon.xPos.y - minRoomSize.x - additionalSpace);
            xPosLeft = new Vector2Int(currDungeon.xPos.x, splitPos);
            xPosRight = new Vector2Int(splitPos, currDungeon.xPos.y);
        }
        else if (dir == 1)
        {
            if (currDungeon.yPos.x + minRoomSize.y + additionalSpace >= currDungeon.yPos.y - minRoomSize.y - additionalSpace) return false;
            int splitPos = random.Next(currDungeon.yPos.x + minRoomSize.y + additionalSpace, currDungeon.yPos.y - minRoomSize.y - additionalSpace);

            yPosLeft = new Vector2Int(currDungeon.yPos.x, splitPos);
            yPosRight = new Vector2Int(splitPos, currDungeon.yPos.y);
        }
        currDungeon.left = new Dungeon(xPosLeft, yPosLeft);
        currDungeon.right = new Dungeon(xPosRight, yPosRight);
        return true;
    }
    public void CreateRoom(Dungeon currDung)
    {
        Vector2Int availableSpace = currDung.GetRoomSize();
        Vector2Int roomSize = new Vector2Int(random.Next(minRoomSize.x, availableSpace.x), random.Next(minRoomSize.y, availableSpace.y));
        int xRandRange = random.Next(0, availableSpace.x - roomSize.x);
        int yRandRange = random.Next(0, availableSpace.y - roomSize.y);
        currDung.xRoomPos = new Vector2Int(xRandRange + currDung.xPos.x + 1, xRandRange + roomSize.x + currDung.xPos.x);
        currDung.yRoomPos = new Vector2Int(yRandRange + currDung.yPos.x + 1, yRandRange + roomSize.y + currDung.yPos.x);
        DrawTiles(currDung.xRoomPos, currDung.yRoomPos, roomTile, false);
    }
    public void CreateCorridors(Dungeon dungeon)
    {
        List<Dungeon> individualRooms = new List<Dungeon>();
        GetIndividualRooms(dungeon, individualRooms);
        Dungeon currRoom = individualRooms[0];
        while(individualRooms.Count > 1)
        {
            Dungeon roomToConnect = null;
            int distance = 0;
            Vector2Int currRoomMiddle = new Vector2Int((currRoom.xRoomPos.x + currRoom.xRoomPos.y) / 2, (currRoom.yRoomPos.x + currRoom.yRoomPos.y) / 2);
            for (int i = 0; i < individualRooms.Count; i += 1)
            {
                Vector2Int middle = new Vector2Int((individualRooms[i].xRoomPos.x + individualRooms[i].xRoomPos.y) / 2, (individualRooms[i].yRoomPos.x + individualRooms[i].yRoomPos.y) / 2);
                if ((roomToConnect == null || GetDistanceBetweenTwoPoints(middle, currRoomMiddle) < distance) && currRoomMiddle != middle)
                {
                    roomToConnect = individualRooms[i];
                    distance = GetDistanceBetweenTwoPoints(middle, currRoomMiddle);
                }
            }
            ConnectRooms(currRoom, roomToConnect);
            individualRooms.Remove(currRoom);
            currRoom = roomToConnect;
        }

    }
    public void GetIndividualRooms(Dungeon dungeon, List<Dungeon> sortedRooms)
    {
        if (dungeon == null)
            return;
        if (dungeon.left == null && dungeon.right == null)
        {
            sortedRooms.Add(dungeon);
            return;
        }
        if (dungeon.left != null)
            GetIndividualRooms(dungeon.left, sortedRooms);
        if (dungeon.right != null)
            GetIndividualRooms(dungeon.right, sortedRooms);
    }
    public void ConnectRooms(Dungeon firstRoom, Dungeon secondRoom)
    {
        
        Vector2Int startPoint = new Vector2Int((firstRoom.xRoomPos.x + firstRoom.xRoomPos.y) / 2, (firstRoom.yRoomPos.x + firstRoom.yRoomPos.y) / 2);
        Vector2Int endPoint = new Vector2Int((secondRoom.xRoomPos.x + secondRoom.xRoomPos.y) / 2, (secondRoom.yRoomPos.x + secondRoom.yRoomPos.y) / 2);
        Vector2Int difference = new Vector2Int(Mathf.Abs(startPoint.x - endPoint.x), Mathf.Abs(startPoint.y - endPoint.y));
        if(difference.x > difference.y)// Finding a correct position of the corridor start
        {
            if (startPoint.x > endPoint.x)
            {
                endPoint.x = secondRoom.xRoomPos.y + 1;
            }
            else
            {
                endPoint.x = secondRoom.xRoomPos.x - 1;
            }
        }
        else
        {
            if (startPoint.y > endPoint.y)
            {
                endPoint.y = secondRoom.yRoomPos.y + 1;
            }
            else
            {
                endPoint.y = secondRoom.yRoomPos.x - 1;
            }
        }

        bool canDraw = false;
        while (endPoint != startPoint)
        {
            Vector3Int pos = new Vector3Int(startPoint.x, startPoint.y, 1);
            if (canDraw) bspTilemap.SetTile(pos, corridorTile);
            if (pos.x == firstRoom.xRoomPos.x || pos.x == firstRoom.xRoomPos.y || pos.y == firstRoom.yRoomPos.x || pos.y == firstRoom.yRoomPos.y) canDraw = true;

            if (startPoint.y < endPoint.y)
            {
                startPoint.y += 1;
            }
            else if (startPoint.y > endPoint.y)
            {
                startPoint.y -= 1;
            }
            else if (startPoint.x < endPoint.x)
            {
                startPoint.x += 1;
            }else if(startPoint.x > endPoint.x)
            {
                startPoint.x -= 1;
            }
        }
        bspTilemap.SetTile(new Vector3Int(startPoint.x, startPoint.y, 1), corridorTile);
        
    }
    public void DrawTiles(Vector2Int xPos, Vector2Int yPos, TileBase tile, bool fill)
    {
        if (fill)
        {
            for (int x = xPos.x; x <= xPos.y; x++)
            {
                for (int y = yPos.x; y <= yPos.y; y++)
                {
                    bspTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }
        else
        {
            for (int x = xPos.x; x <= xPos.y; x += 1)
            {
                for (int y = yPos.x; y <= yPos.y; y += 1)
                {
                    if (x == xPos.x || x == xPos.y || y == yPos.x || y == yPos.y) bspTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }
    }
    public int GetDistanceBetweenTwoPoints(Vector2Int firstPoint, Vector2Int secondPoint)
    {
        return (int)Mathf.Sqrt(Mathf.Pow(secondPoint.x - firstPoint.x, 2) + Mathf.Pow(secondPoint.y - firstPoint.y, 2));
    }
    public class Dungeon
    {
        public Vector2Int xPos;
        public Vector2Int yPos;
        public Vector2Int xRoomPos;
        public Vector2Int yRoomPos;
        public Dungeon left;
        public Dungeon right;
        public Dungeon(Vector2Int xPos, Vector2Int yPos)
        {
            this.xPos = xPos;
            this.yPos = yPos;
        }
        public Vector2Int GetRoomSize()
        {
            return new Vector2Int(xPos.y - xPos.x - 2, yPos.y - yPos.x - 2);
        }
    }
}

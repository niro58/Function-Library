using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
public class WFController : EditorWindow
{
    public Tilemap refTilemap;
    public Tilemap genTilemap;
    [SerializeField]
    public Dictionary<TileBase, TileRule> tileRules = new Dictionary<TileBase, TileRule>();
    public Dictionary<Vector3Int, TileBase[]> tiles = new Dictionary<Vector3Int, TileBase[]>();
    public Dictionary<Vector3Int, TileBase> generatedTile = new Dictionary<Vector3Int, TileBase>();

    public int lastValue;
    public int lastUpdatedValue;
    [MenuItem("Library/Wave Function Collapse")]
    public static void ShowWindow()
    {
        GetWindow<WFController>("Wave Function Collapse");
    }
    private void OnGUI()
    {
        refTilemap = (Tilemap)EditorGUI.ObjectField(new Rect(3, 3, position.width - 6, 20), "Reference Tilemap", refTilemap, typeof(Tilemap), true);
        if (refTilemap)
        {
            if (GUI.Button(new Rect(3, 25, position.width - 6, 20), "Initialize"))
            {
                WFInitialize();
            }
            if (GUI.Button(new Rect(3, 47, position.width - 6, 20), "Generate"))
            {
                WFGenerate();
            }
        }

        lastValue = EditorGUI.IntField(new Rect(0, 71, position.width, 15), "Number of Tiles:", lastValue);
        if (lastUpdatedValue != lastValue) GenerateMap(lastValue);
    }
    public void GenerateMap(int amount)
    {
        lastUpdatedValue = amount;
        genTilemap.ClearAllTiles();
        for(int i =0; i < lastUpdatedValue; i++)
        {
            genTilemap.SetTile(generatedTile.ElementAt(i).Key,generatedTile.ElementAt(i).Value);
        }
    }
    private void WFInitialize()
    {
        tileRules.Clear();
        tiles.Clear();

        CreateTilemap();

        for (int y = refTilemap.cellBounds.yMin; y < refTilemap.cellBounds.yMax; y++)
        {
            for (int x = refTilemap.cellBounds.xMin; x < refTilemap.cellBounds.xMax; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                TileBase tile = refTilemap.GetTile(pos);
                if (tile == null) continue;
                if (!tileRules.ContainsKey(tile))
                {
                    tileRules.Add(tile, new TileRule());
                }
                tileRules[tile].probability += 1;
                tileRules[tile].AddSurroundedTiles(refTilemap, pos);
            }
        }
        int prob = 0;
        foreach(KeyValuePair<TileBase,TileRule> rule in tileRules)
        {
            prob += rule.Value.probability;
            rule.Value.probability = prob;
        }
        foreach(KeyValuePair<TileBase,TileRule> rule in tileRules)
        {
            //Debug.Log(rule.Key.name + "!" + rule.Value.probability);
            foreach(KeyValuePair<TileBase,Direction> allowedTile in rule.Value.allowedTiles)
            {
                Debug.Log("Parent : " +  rule.Key.name + "+" + allowedTile.Key + "!" + allowedTile.Value);
            }
        }
    }
    private void WFGenerate()
    {
        generatedTile.Clear();
        genTilemap.ClearAllTiles();
        tiles.Clear();
        tiles.Add(new Vector3Int(0, 0, 1), tileRules.Keys.ToArray());
        int step = 1;
        while (tiles.Count != 0 && step < 10000)
        {
            Vector3Int lowestEntropyVector = GetLowestEntropy();
            TileBase[] selectedTilePossibilities = tiles[lowestEntropyVector];
            TileBase selectedRandTile = GetRandomTile(selectedTilePossibilities);
            genTilemap.SetTile(lowestEntropyVector, selectedRandTile);
            generatedTile.Add(lowestEntropyVector, selectedRandTile);
            tiles.Remove(lowestEntropyVector);
            RecalculateEntropy(lowestEntropyVector, step);
            step++;
        }
    }
    private void RecalculateEntropy(Vector3Int position, int step)
    {
        for(int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)// Get All Surrounded Tiles
            {
                Vector3Int pos = position + new Vector3Int(x, y, 0);
                pos.z = 1;
                // If tile is already created move to the next one
                if ((x != 0 && y != 0) || genTilemap.GetTile(pos)) continue;

                // If it is first time doing something with this tile, add him to array
                if (!tiles.ContainsKey(pos)) tiles.Add(pos, tileRules.Keys.ToArray());

                // Get all possible tiles of this array
                List<TileBase> possibleTiles = tiles[pos].ToList();// Get all possible tiles of this array

                // Get opposite direction of values x and y , so we can check what tiles are compatible with it
                Direction dir = TileRule.GetDirectionByVector(new Vector3Int(-x, -y, 0));

                int index = 0;
                while (index < possibleTiles.Count)
                {
                    TileRule rule = tileRules[possibleTiles[index]];
                    if (rule.IsAllowed(genTilemap.GetTile(position), dir))
                    {
                        index++;
                        continue;
                    }
                    possibleTiles.RemoveAt(index);
                }
                if (possibleTiles.Count == 0)
                {
                    tiles.Remove(pos);
                    continue;
                }
                //Debug.Log(step + "Position " + pos + " 1Has " + possibleTiles.Count());
                tiles[pos] = possibleTiles.ToArray();

            }
        }
    }
    private TileBase GetRandomTile(TileBase[] tileArray)
    {
        if (tileArray.Count() == 0) return null;
        if (tileArray.Count() == 1) return tileArray[0];
        int rollNum = 0;
        Dictionary<TileBase, int> rollNums = new Dictionary<TileBase, int>();
        foreach(TileBase tile in tileArray)
        {
            rollNum += tileRules[tile].probability;
            rollNums.Add(tile, rollNum);
        }
        return rollNums.Where(x => GetRandomNumber(0, rollNum) <= x.Value).First().Key;
    }
    private int GetRandomNumber(int from, int to)
    {
        UnityEngine.Random.InitState(DateTime.Now.Millisecond);
        return UnityEngine.Random.Range(from, to);
    }
    private Vector3Int GetLowestEntropy()
    {
        Dictionary<Vector3Int, TileBase[]> lowestEntropyTiles = new Dictionary<Vector3Int, TileBase[]>();
        lowestEntropyTiles = tiles.Where(x => x.Value.Length == tiles.Min(x => x.Value.Length)).ToDictionary(t => t.Key, t => t.Value);
        
        if (lowestEntropyTiles.Count == 0) return Vector3Int.zero;
        int rand = GetRandomNumber(0, lowestEntropyTiles.Count);
        return lowestEntropyTiles.ElementAt(rand).Key;
    }
    private void CreateTilemap()
    {
        DestroyImmediate(GameObject.Find("Generated_Grid"));
        GameObject grid = new GameObject("Generated_Grid");
        grid.AddComponent<Grid>();

        GameObject tilemap = new GameObject("Generated_Tilemap");
        tilemap.transform.SetParent(grid.transform);
        tilemap.AddComponent<Tilemap>();
        tilemap.AddComponent<TilemapRenderer>();
        genTilemap = tilemap.GetComponent<Tilemap>();
    }
}
public class TileRule
{
    public Dictionary<TileBase,Direction> allowedTiles = new Dictionary<TileBase,Direction>();
    public int probability;
    public TileRule()
    {
    }
    public bool IsAllowed(TileBase tile, Direction dir)
    {
        if (allowedTiles.ContainsKey(tile) && allowedTiles[tile].HasFlag(dir)) return true;
        return false;
    }
    public void AddSurroundedTiles(Tilemap tileMap, Vector3Int pos)
    {
        for(int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x != 0 && y != 0) continue;
                Vector3Int dir = new Vector3Int(x, y, 0);
                TileBase tile = tileMap.GetTile(pos + dir);
                if(tile == null) continue;
                if (!allowedTiles.ContainsKey(tile))
                {
                    allowedTiles.Add(tile, GetDirectionByVector(dir));
                    continue;
                }
                if (!allowedTiles[tile].HasFlag(GetDirectionByVector(dir)))
                {
                    allowedTiles[tile] |= GetDirectionByVector(dir);
                }
            }
        }
    }
    public static Direction GetDirectionByVector(Vector3Int vector)
    {
        if(vector == new Vector3Int(1, 0, 0))
        {
            return Direction.Right;
        }
        else if (vector == new Vector3Int(-1, 0, 0))
        {
            return Direction.Left;
        }
        else if (vector == new Vector3Int(0, 1, 0))
        {
            return Direction.Top;
        }
        else if (vector == new Vector3Int(0, -1, 0))
        {
            return Direction.Bottom;
        }
        return Direction.None;
    }
    public static Vector3Int GetVectorByDirection(Direction dir)
    {
        if (dir == Direction.Right)
        {
            return new Vector3Int(1, 0, 0);
        }
        else if (dir == Direction.Left)
        {
            return new Vector3Int(-1, 0, 0);
        }
        else if (dir == Direction.Top)
        {
            return new Vector3Int(0, 1, 0);
        }
        else if (dir == Direction.Bottom)
        {
            return new Vector3Int(0, -1, 0);
        }
        return Vector3Int.zero;
    }
}
[Flags]
public enum Direction {None = 0, Left = 1, Right = 2, Top = 4, Bottom = 8 };
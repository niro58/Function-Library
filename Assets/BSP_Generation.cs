using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BSP_Generation : EditorWindow
{
    [MenuItem("Library/BSP Generation")]
    public static void ShowWindow()
    {
        GetWindow<BSP_Generation>("BSP Generation");
    }
    private void OnGUI()
    {
        List<Tilemap> list = new List<Tilemap>();
        int newCount = Mathf.Max(0, EditorGUILayout.IntField("size", list.Count));


        if (GUI.Button(new Rect(3, 47, position.width - 6, 20), "Generate") && list != null)
        {
            BSPGenerate(list);
        }
    }
    private void BSPGenerate(List<Tilemap> possibleRooms)
    {

    }
}

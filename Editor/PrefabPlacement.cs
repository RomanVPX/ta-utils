using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TechArtUtils
{
    public class PrefabPlacement : EditorWindow
    {

        public GameObject objectToScatterOn;
        public GameObject prefabToScatter;

        const float minScaleLimit = 0.1f;
        const float maxScaleLimit = 5f;

        float minScale = 0.9f;
        float maxScale = 1.2f;

        float rndRotateCoeff = 0f;

        const int minPlaceNumberLimit = 1;
        int maxPlaceNumberLimit = 500;
        int prefabsToPlaceNumber = 1;
        bool safeNumberEnable = true;

        bool keepDistanceEnable;
        float distanceBetweenPrefabs = 0f;
        float distSnd = -1f;

        //Separate variables
        GameObject parent01;
        GameObject parent02;





        [MenuItem("TA Tools/Prefab Placement")]

        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(PrefabPlacement));
        }

        private void OnGUI()
        {
            GUILayout.Label("Scatter", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical();

            GUILayout.Box("", new GUILayoutOption[] {GUILayout.ExpandWidth(true), GUILayout.Height(1)}); //разделитель

            GameObject objectToScatterOnChk =
                EditorGUILayout.ObjectField("Scatter on", objectToScatterOn, typeof(GameObject), true) as GameObject;
            GameObject prefabToScatterChk =
                EditorGUILayout.ObjectField("Prefab to scatter", prefabToScatter, typeof(GameObject), false) as
                    GameObject;

            if (prefabToScatterChk != null && PrefabUtility.GetPrefabType(prefabToScatterChk) == PrefabType.Prefab)
                prefabToScatter = prefabToScatterChk;

            if ((objectToScatterOnChk != null) && (objectToScatterOnChk.scene.IsValid()))
                objectToScatterOn = objectToScatterOnChk;

            #region Ranges Calculations

            if (prefabToScatter != null && objectToScatterOn != null)
            {
                //float xSize = prefabToScatter.GetComponent<MeshFilter>().sharedMesh.bounds.size.x;
                //float zSize = prefabToScatter.GetComponent<MeshFilter>().sharedMesh.bounds.size.z;
                //float toScatterArea = xSize * zSize;

                //костыль! 
                float toScatterArea = 5f;
                float xSize = 0f;
                float zSize = 0f;
                //закончился

                xSize = objectToScatterOn.GetComponent<MeshFilter>().sharedMesh.bounds.size.x *
                        objectToScatterOn.transform.localScale.x;
                zSize = objectToScatterOn.GetComponent<MeshFilter>().sharedMesh.bounds.size.z *
                        objectToScatterOn.transform.localScale.z;
                float objectArea = xSize * zSize;
                if (safeNumberEnable)
                    maxPlaceNumberLimit = Mathf.RoundToInt(objectArea / toScatterArea);
                else
                    maxPlaceNumberLimit = 1000;
            }



            #endregion


            GUILayout.Box("", new GUILayoutOption[] {GUILayout.ExpandWidth(true), GUILayout.Height(1)}); //разделитель
            EditorGUILayout.Space();


            #region Scale range

            EditorGUILayout.LabelField("Scale range");
            EditorGUILayout.MinMaxSlider(ref minScale, ref maxScale, minScaleLimit, maxScaleLimit);

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 30;
            minScale = EditorGUILayout.FloatField("Min:", minScale);
            maxScale = EditorGUILayout.FloatField("Max:", maxScale);
            EditorGUILayout.EndHorizontal();

            if (minScale >= maxScale)
                maxScale = minScale;

            EditorGUILayout.Space();

            #endregion


            #region Rotation settings

            EditorGUILayout.LabelField("Rotation randomness");
            rndRotateCoeff = EditorGUILayout.Slider(rndRotateCoeff, 0, 1);
            EditorGUILayout.Space();

            #endregion


            #region Quantity

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quantity");
            safeNumberEnable = EditorGUILayout.Toggle("Safe", safeNumberEnable);
            EditorGUILayout.EndHorizontal();
            prefabsToPlaceNumber =
                EditorGUILayout.IntSlider(prefabsToPlaceNumber, minPlaceNumberLimit, maxPlaceNumberLimit);

            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.Space();

            #endregion


            #region Distance

            keepDistanceEnable = EditorGUILayout.BeginToggleGroup("Keep minimum distance", keepDistanceEnable);
            distanceBetweenPrefabs = EditorGUILayout.Slider(distanceBetweenPrefabs, 0f, 1f);
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();
            GUILayout.Box("", new GUILayoutOption[] {GUILayout.ExpandWidth(true), GUILayout.Height(1)}); //разделитель

            #endregion


            if (GUILayout.Button("Scatter"))
            {
                if (!keepDistanceEnable)
                    distSnd = -1f;
                else
                    distSnd = distanceBetweenPrefabs;

                if (objectToScatterOn != null && prefabToScatter != null)
                    RandomPropsPlacement.PlacePrefabsOnPlane(objectToScatterOn, prefabToScatter,
                        minScale, maxScale, rndRotateCoeff,
                        prefabsToPlaceNumber, distSnd);
                else
                    EditorUtility.DisplayDialog("Nope", "Pick object and prefab", "Ok");
            }

            GUILayout.Box("", new GUILayoutOption[] {GUILayout.ExpandWidth(true), GUILayout.Height(1)}); //разделитель

            #region Separate objects

            GUILayout.Space(30);
            GUILayout.Label("Separate objects", EditorStyles.boldLabel);
            GUILayout.Box("", new GUILayoutOption[] {GUILayout.ExpandWidth(true), GUILayout.Height(1)}); //разделитель
            GUILayout.EndVertical();

            GameObject objectsToSeparateParent01Chk =
                EditorGUILayout.ObjectField("Parent 01", parent01, typeof(GameObject), true) as GameObject;
            if (objectsToSeparateParent01Chk != null && objectsToSeparateParent01Chk.scene.IsValid())
                parent01 = objectsToSeparateParent01Chk;


            #endregion
        }


    }
//GameObject objectToScatterOnChk = EditorGUILayout.ObjectField("Scatter on", objectToScatterOn, typeof(GameObject), true) as GameObject;
//GameObject prefabToScatterChk = EditorGUILayout.ObjectField("Prefab to scatter", prefabToScatter, typeof(GameObject), false) as GameObject;

//        if ((objectToScatterOnChk != null) && (objectToScatterOnChk.scene.IsValid()))
//            objectToScatterOn = objectToScatterOnChk;
}
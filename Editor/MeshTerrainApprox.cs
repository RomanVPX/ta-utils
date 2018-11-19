using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace TechArtUtils
{
    public class MeshTerrainApprox : EditorWindow
    {
        public Terrain terrainToApprox;

        int xDivisions, zDivisions, iterations;

        private class SinglePointData
        {
            public Vector3 pointVector;
            public float pointWeight;
            public int pointIndex;
            public bool edgeFlag, cornerFlag;

            public SinglePointData()
            {
                pointVector = Vector3.zero;
                pointWeight = new float();
                pointIndex = new int();
                edgeFlag = new bool();
                cornerFlag = new bool();
            }

            public SinglePointData(Vector3 pointVector, float pointWeight, int pointIndex, bool edgeFlag,
                bool cornerFlag)
            {
                this.pointVector = pointVector;
                this.pointWeight = pointWeight;
                this.pointIndex = pointIndex;
                this.edgeFlag = edgeFlag;
                this.cornerFlag = cornerFlag;
            }
        }

        private class PointsData
        {
            public Vector3[] pointVector;
            public float[] pointWeight;
            public int[] pointIndex;
            public bool[] edgeFlag, cornerFlag;

            public PointsData(int size)
            {
                pointVector = new Vector3[size];
                pointWeight = new float[size];
                pointIndex = new int[size];
                edgeFlag = new bool[size];
                cornerFlag = new bool[size];
            }

            public SinglePointData[] SinglePointDataArray()
            {
                if (pointVector.Length > 0)
                {
                    SinglePointData[] pointsArray = new SinglePointData[pointVector.Length];
                    for (int i = 0; i < pointVector.Length; i++)
                    {
                        pointsArray[i] = new SinglePointData(pointVector[i], pointWeight[i], pointIndex[i], edgeFlag[i],
                            cornerFlag[i]);
                    }

                    return pointsArray;
                }
                else
                    return null;
            }

            public List<SinglePointData> ListSortedByWeight()
            {
                if (pointVector.Length > 0)
                    return SinglePointDataArray().OrderBy(si => si.pointWeight).ToList();
                else
                    return null;
            }

            public List<SinglePointData> ListSortedByWeightInRow(int row, int xDivisions)
            {
                if (pointVector.Length > 0)
                    return ListSortedByWeight().Where(x =>
                        (x.pointIndex >= row * xDivisions && x.pointIndex < (row + 1) * xDivisions)).ToList();
                else
                    return null;
            }
        }

        [MenuItem("TA Tools/Terrain Approx. Mesh")]

        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(MeshTerrainApprox));
        }




        public void OnGUI()
        {

            GUILayout.Label("Generate mesh", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical();

            GUILayout.Box("", new GUILayoutOption[] {GUILayout.ExpandWidth(true), GUILayout.Height(1)}); //разделитель

            Terrain inputTerrain =
                EditorGUILayout.ObjectField("Terrain GO", terrainToApprox, typeof(Terrain), true) as Terrain;
            if ((inputTerrain != null) && (inputTerrain.gameObject.scene.IsValid()))
                terrainToApprox = inputTerrain;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("X divisions");
            xDivisions = EditorGUILayout.IntSlider(xDivisions, 1, 100);
            EditorGUILayout.LabelField("Z divisions");
            zDivisions = EditorGUILayout.IntSlider(zDivisions, 1, 100);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Subdivisions");
            iterations = EditorGUILayout.IntSlider(iterations, 1, 6);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate") && terrainToApprox != null)
            {
                GameObject newApproxMesh = new GameObject("Approx_mesh");
                newApproxMesh.transform.SetParent(terrainToApprox.transform);
                if (LayerMask.NameToLayer("Terrain") != -1)
                    newApproxMesh.layer = LayerMask.NameToLayer("Terrain");
                newApproxMesh.isStatic = true;
                newApproxMesh.AddComponent<MeshFilter>();
                //newApproxMesh.AddComponent<KillMyself>();  //потерял компонент при переезде)
                newApproxMesh.AddComponent<MeshRenderer>();
                newApproxMesh.GetComponent<MeshRenderer>().enabled = false;
                GenerateMesh(newApproxMesh);
            }

            GUILayout.EndVertical();
        }

        private void GenerateMesh(GameObject go)
        {

            Vector3[] subdividedPointsGrid = MakeDenseGrid(iterations);
            Debug.Log(subdividedPointsGrid.Length);
            PointsData generatedPointsData = GeneratePointsData(subdividedPointsGrid);


            go.GetComponent<MeshFilter>().sharedMesh = MakeSharedMesh();
            Mesh newMesh = go.GetComponent<MeshFilter>().sharedMesh;

            //newMesh.SetVertices(...) //быстрее работает, чем строчка ниже
            //newMesh.vertices = subdividedPointsGrid;
            newMesh.triangles = MakeTriangles(xDivisions * iterations, zDivisions * iterations);

        }

        private Mesh MakeSharedMesh()
        {

            string savePath = ("Assets/terrainapproxmesh.asset");

            Mesh newTmpMesh = new Mesh();
            newTmpMesh.name = ("ProcMesh");

            if (!(newTmpMesh = AssetDatabase.LoadAssetAtPath<Mesh>(savePath)))
                AssetDatabase.CreateAsset(newTmpMesh, savePath);


            if (!newTmpMesh)
                AssetDatabase.AddObjectToAsset(newTmpMesh, savePath);
            else
                newTmpMesh.Clear();

            return newTmpMesh;
        }

        private Vector3[] MakeDenseGrid(int iterations)
        {
            float xStep = terrainToApprox.terrainData.size.x / xDivisions / iterations;
            float zStep = terrainToApprox.terrainData.size.z / zDivisions / iterations;

            Vector3[] verticlesToOptimize = new Vector3[(xDivisions * iterations + 1) * (zDivisions * iterations + 1)];

            for (int z = 0, i = 0; z <= zDivisions * iterations; z++)
            {
                for (int x = 0; x <= xDivisions * iterations; x++, i++)
                {
                    Vector3 currVert = new Vector3(xStep * x, 0, zStep * z);
                    currVert.y = terrainToApprox.SampleHeight(currVert) + terrainToApprox.transform.position.y;
                    verticlesToOptimize[i] = currVert;
                }
            }

            return verticlesToOptimize;

        }

        private PointsData GeneratePointsData(Vector3[] verticlesToOptimize)
        {
            PointsData pointsData = new PointsData((xDivisions * iterations + 1) * (zDivisions * iterations + 1));
            for (int z = 0, i = 0; z <= zDivisions * iterations; z++)
            {
                for (int x = 0; x <= xDivisions * iterations; x++, i++)
                {
                    pointsData.pointVector[i] = verticlesToOptimize[i];
                    pointsData.pointIndex[i] = i;
                    pointsData.pointWeight[i] = 0f;

                    byte cornerCount = 0;

                    //флаг грани на нижний ряд
                    if (i < xDivisions * iterations + 1)
                    {
                        pointsData.edgeFlag[i] = true;
                        cornerCount++;
                    }
                    else
                        pointsData.pointWeight[i] +=
                            Mathf.Abs(verticlesToOptimize[i].y -
                                      verticlesToOptimize[i - xDivisions * iterations].y); //добавили вес снизу

                    //флаг грани на верхний ряд
                    if (i > pointsData.pointVector.Length - (xDivisions * iterations + 1))
                    {
                        pointsData.edgeFlag[i] = true;
                        cornerCount++;
                    }
                    else
                        pointsData.pointWeight[i] +=
                            Mathf.Abs(verticlesToOptimize[i].y -
                                      verticlesToOptimize[i + xDivisions * iterations].y); //добавли вес сверху

                    //флаг на правый столбец
                    if ((i + 1) % (xDivisions * iterations + 1) == 0)
                    {
                        pointsData.edgeFlag[i] = true;
                        cornerCount++;
                    }
                    else
                        pointsData.pointWeight[i] +=
                            Mathf.Abs(verticlesToOptimize[i].y - verticlesToOptimize[i + 1].y); // добавили вес справа

                    //флаг на левый столбец
                    if (i % (xDivisions * iterations + 1) == 0)
                    {
                        pointsData.edgeFlag[i] = true;
                        cornerCount++;
                    }
                    else
                        pointsData.pointWeight[i] +=
                            Mathf.Abs(verticlesToOptimize[i].y - verticlesToOptimize[i - 1].y); //добавили вес слева

                    if (cornerCount == 2)
                        pointsData.cornerFlag[i] = true;

                    if (i > 0)
                        Debug.DrawLine(pointsData.pointVector[i], pointsData.pointVector[i - 1], Color.red, 2f, false);
                }
            }

            return pointsData;
        }

        private int[] MakeTriangles(int xDiv, int zDiv)
        {
            int[] meshTriangles = new int[(xDiv + 1) * (zDiv + 1) * 6];

            for (int ti = 0, vi = 0, z = 0; z < zDiv; z++, vi++)
            {
                for (int x = 0; x < xDiv; x++, ti += 6, vi++)
                {
                    meshTriangles[ti] = vi;
                    meshTriangles[ti + 3] = meshTriangles[ti + 2] = vi + 1;
                    meshTriangles[ti + 4] = meshTriangles[ti + 1] = vi + xDiv + 1;
                    meshTriangles[ti + 5] = vi + xDiv + 2;
                }
            }

            return meshTriangles;
        }

        private List<Vector3> OptimizePointsdata(PointsData points)
        {
            //Vector3[] optimizedVectorsArray = new Vector3[(xDivisions + 1) * (zDivisions + 1)];
            List<Vector3> optimizedVertices;

            for (int z = 0; z < zDivisions * iterations + 1; z++)
            {
                var currentRowPoints = points.ListSortedByWeightInRow(z, xDivisions * iterations + 1);
                //Debug.Log("Row " + z + " is " + currentRowPonts.ToString());
                int pointsLeft = xDivisions + 1;
                List<SinglePointData> optimisedRowPoints = currentRowPoints; //ВОТ ВСЁ В НЕЁ ПИХАЙ!!!111
                for (int i = 0; i < currentRowPoints.Count + 1; i++)
                {
                    if (!currentRowPoints[i].cornerFlag && pointsLeft == 0)
                        optimisedRowPoints.RemoveAt(i);
                    else if (currentRowPoints[i].cornerFlag)
                        pointsLeft--;
                    pointsLeft--;
                }



                //{
                //Debug.Log("Index = " + item.pointIndex + ", weight = " + item.pointWeight);
                // if (item.cornerFlag || item.edgeFlag)

                //}
                ////foreach (SinglePointData in currentRowPonts)
                //{

                //}
                //var currentRowPoints = terrainsList.GroupBy(i => i.name).Select(g => g.First()).ToList();
                //InitTerrainList();
            }

            return null;
        }
    }
}
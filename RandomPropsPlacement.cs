using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TechArtUtils
{
    public class RandomPropsPlacement : MonoBehaviour
    {

        public static void PlacePrefabsOnPlane(GameObject scatterOn, GameObject prefabToScatter,
            float minScale, float maxScale, float rotRnd,
            int quantity, float distBetween)
        {

            int placementRetries = 0;
            const int maxPlacementRetries = 500;
            bool distanceFailed = false;

            GameObject folderGO = new GameObject(prefabToScatter.name + "_folder");
            //folderGO.transform.position = scatterOn.transform.position;
            folderGO.transform.position =
                scatterOn.transform.TransformPoint(scatterOn.GetComponent<MeshFilter>().sharedMesh.bounds.center);
            folderGO.transform.SetParent(scatterOn.transform);
            //Local space!!!
            float halfX = scatterOn.GetComponent<MeshFilter>().sharedMesh.bounds.size.x *
                          scatterOn.transform.localScale.x / 2;
            float halfZ = scatterOn.GetComponent<MeshFilter>().sharedMesh.bounds.size.z *
                          scatterOn.transform.localScale.z / 2;

            //костыль
            // float minDistanceBetween = Mathf.Max((prefabToScatter.GetComponent<MeshFilter>().sharedMesh.bounds.size.x),
            //    (prefabToScatter.GetComponent<MeshFilter>().sharedMesh.bounds.size.z));
            float minDistanceBetween = 3f;
            //костыль
            float maxDistanceBetween = Mathf.Sqrt(4 * halfX * halfZ / quantity) + minDistanceBetween;
            float calculatedDistanceBetween = Mathf.Lerp(minDistanceBetween, maxDistanceBetween, distBetween);
            Debug.Log("minDist = " + minDistanceBetween);
            Debug.Log("maxDist = " + maxDistanceBetween);
            Debug.Log("Interpolated = " + calculatedDistanceBetween);




            for (int i = 1; i <= quantity;)
            {
                float randX = Random.Range(-halfX, halfX) + folderGO.transform.position.x;
                float randZ = Random.Range(-halfZ, halfZ) + folderGO.transform.position.z;
                float randScaleCoeff = Random.Range(minScale, maxScale);
                float randRotation = Random.Range(-180f, 180f) * rotRnd;

                const float rayStartHeight = 100f;
                Vector3 currentPoint = new Vector3(randX, rayStartHeight, randZ);
                RaycastHit rHit;
                Physics.Raycast(currentPoint, -Vector3.up, out rHit, 150f);

                if ((distBetween < 0f) || NotNear(folderGO, new Vector2(randX, randZ), calculatedDistanceBetween) ||
                    placementRetries > maxPlacementRetries)
                {

                    GameObject g = PrefabUtility.InstantiatePrefab(prefabToScatter) as GameObject;
                    g.transform.SetParent(folderGO.transform);

                    g.name = (prefabToScatter.name + "_" + i);
                    g.transform.position = new Vector3(randX, rayStartHeight - rHit.distance, randZ);

                    //тут цвета 
                    //Mesh currMesh = g.GetComponent<MeshFilter>().mesh;
                    //currMesh.colors32 = RandomiseVertexColors(currMesh.vertices);

                    //закончились 

                    g.transform.localScale = g.transform.localScale * randScaleCoeff;
                    g.transform.Rotate(0, randRotation, 0);

                    g.transform.SetParent(folderGO.transform);

                    i++;

                    placementRetries = 0;
                }

                placementRetries++;
                if (placementRetries > maxPlacementRetries)
                    distanceFailed = true;

            }

            folderGO.transform.rotation = scatterOn.transform.rotation;
            if (distanceFailed)
                Debug.LogWarning("Couldn't keep distance");

        }

        public static bool NotNear(GameObject parentGO, Vector2 currentCoords, float distanceToNear)
        {
            Vector2 iterativeCoords;

            foreach (Transform child in parentGO.transform)
            {
                iterativeCoords.x = child.position.x;
                iterativeCoords.y = child.position.z;
                if (Vector2.Distance(iterativeCoords, currentCoords) < distanceToNear)
                    return false;
            }

            return true;
        }

        public static Color32[] RandomiseVertexColors(Vector3[] vertexArray)
        {
            Color32 currRndColor = new Color32();

            Color32[] newColors = new Color32[vertexArray.Length];
            currRndColor.r = (byte) Random.Range(20, 255);
            currRndColor.g = (byte) Random.Range(20, 255);
            currRndColor.b = (byte) Random.Range(20, 255);
            for (int i = 0; i < newColors.Length; i++)
            {

                newColors[i] = Color32.Lerp(currRndColor, Color.red, vertexArray[i].y / 10);
            }

            return newColors;

        }


    }
}
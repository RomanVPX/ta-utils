using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TechArtUtils
{
    public class RandomVertexColor : MonoBehaviour {

        public GameObject treePrefab;

        void Awake () {
            int qnty = 10;
            for (int i = 0; i < qnty; i++)
            {
                var g = Instantiate(treePrefab, transform);
                g.transform.localPosition = new Vector3 (Random.Range(-20f, 20f), -1f, Random.Range(-20f, 20f));
                float goScale = Random.Range(0.8f, 1.2f);
                float goRotate = Random.Range(-90f, 90f);

                g.transform.localScale = new Vector3(goScale, goScale, goScale);
                g.transform.Rotate(0, goRotate, 0); ;

                Mesh currMesh = g.GetComponent<MeshFilter>().mesh;
                currMesh.colors32 = RandomiseVertexColors(currMesh.vertices);
            }
        }

        private Color32[] RandomiseVertexColors(Vector3[] vertexArray)
        {
            Color32 currRndColor = new Color32();

            Color32[] newColors = new Color32[vertexArray.Length];
            currRndColor.r = (byte)Random.Range(20, 255);
            currRndColor.g = (byte)Random.Range(20, 255);
            currRndColor.b = (byte)Random.Range(20, 255);
            for (int i = 0; i < newColors.Length; i++)
            {

                newColors[i] = Color32.Lerp(currRndColor, Color.yellow, vertexArray[i].y/5);
            }

            return newColors;

        }

    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TechArtUtils
{
    public class LPContol : MonoBehaviour
    {

        struct LPTransformPair
        {
            public GameObject playerGO;
            public Transform pointerTransform;
        }

        List<LPTransformPair> pairsList;
        Matrix4x4 currentTRSMatrix;

        bool doUpdates = false;


        void Awake()
        {

            pairsList = new List<LPTransformPair>();

        }

        // Update is called once per frame
        void Update()
        {
            if (!doUpdates)
                return;

            foreach (LPTransformPair pair in pairsList)
            {
                //PlayerLayout currentLO = pair.playerGO.GetComponentInParent<PlayerLayout>();

                pair.pointerTransform.position = currentTRSMatrix.MultiplyPoint3x4(pair.playerGO.transform.position);

            }

        }

        public void DoUpdates(Matrix4x4 currentMatrix)
        {
            currentTRSMatrix = currentMatrix;
            doUpdates = true;
        }

        public void StopUpdates()
        {
            doUpdates = false;
        }

        public void AddPlayer(Renderer playerCustomGORender)
        {
            GameObject lpPointer = new GameObject("lpPointer");

            playerCustomGORender.probeAnchor = lpPointer.transform;

            LPTransformPair playerPair = new LPTransformPair
            {
                playerGO = playerCustomGORender.gameObject,
                pointerTransform = lpPointer.transform
            };

            pairsList.Add(playerPair);

        }
    }
}
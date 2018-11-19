using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TechArtUtils
{
    public class MakeScreenshot : MonoBehaviour {

        public MakeScreenshotSettings settings;

        private GameObject uiGO;
        private GameObject arrowGO;

        private void MakeSS()
        {
            if (settings.turnOffUI)
                uiGO = GameObject.Find(settings.uiName);
            if (settings.turnOffArrow)
                arrowGO = GameObject.Find(settings.arrowName);

            if (uiGO && settings.turnOffUI)
                uiGO.SetActive(false);
            if (arrowGO && settings.turnOffArrow)
                arrowGO.SetActive(false);

            ScreenCapture.CaptureScreenshot("screenshot.png",settings.superSize);

            StartCoroutine("TurnEverythingOn");
        }

        private IEnumerator TurnEverythingOn()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.2f);
                if (uiGO && settings.turnOffUI)
                    uiGO.SetActive(true);
                if (uiGO && settings.turnOffArrow)
                    arrowGO.SetActive(true);
            }
        }
	
        void Update () {
            if (Input.GetKeyDown("t"))
                MakeSS();
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TechArtUtils
{
    [CreateAssetMenu(fileName = "screnshotsettings", menuName = "TA Tools/ScreenshotSettings", order = 1)]
    public class MakeScreenshotSettings : ScriptableObject {

        public bool turnOffUI = false;
        public bool turnOffArrow = false;

        [Range(0, 9)]
        public int superSize = 0;

        public string FilePrefix = "screenshot";

        public string uiName = ("Canvas");
        public string arrowName = ("arrow");
    }
}
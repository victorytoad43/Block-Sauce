using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlockSauce.Editor
{
    /// <summary>
    /// Applies the Block Sauce import settings (docs/PLAN.md, Art pipeline) to every model under Assets/Art/, so
    /// Jackson's FBX exports need no manual setup in Unity: scale factor 0.4 (his art is authored at about 2.5x
    /// real-world scale), Generic rig, no cameras or lights, and clips named after the Blender action without the
    /// exporter's "armature|" prefix. Clips whose names mark them as cycles loop.
    /// </summary>
    public class ArtImportPostprocessor : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Art/";
        private const float ArtScaleFactor = 0.4f;
        private static readonly string[] LoopingClipWords = { "Idle", "Walk", "Run", "Loop", "Pose", "Hold", "Spin", "Strafe", "Slide" };

        private void OnPreprocessModel()
        {
            if (assetPath.StartsWith(ArtFolder) == false)
                return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = ArtScaleFactor;
            importer.useFileScale = true;
            // Off: with Blender's Forward -Z / Up Y export, baking the axis conversion turns the character to face -Z.
            importer.bakeAxisConversion = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importBlendShapes = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            // Squash and stretch animates bone scale; keep those curves even where they look constant.
            importer.removeConstantScaleCurves = false;
        }

        private void OnPreprocessAnimation()
        {
            if (assetPath.StartsWith(ArtFolder) == false)
                return;

            var importer = (ModelImporter)assetImporter;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
                return;

            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = CleanClipName(clip.takeName);
                clip.loopTime = LoopingClipWords.Any(word => clip.name.IndexOf(word, System.StringComparison.OrdinalIgnoreCase) >= 0);
            }

            importer.clipAnimations = clips;
        }

        /// <summary>
        /// "armature_guy|Guy.WalkBasic" becomes "WalkBasic": the armature prefix and the character prefix go.
        /// </summary>
        private static string CleanClipName(string takeName)
        {
            string name = takeName;
            int bar = name.LastIndexOf('|');
            if (bar >= 0)
                name = name.Substring(bar + 1);
            int dot = name.IndexOf('.');
            if (dot >= 0 && dot < name.Length - 1)
                name = name.Substring(dot + 1);
            return name;
        }
    }
}

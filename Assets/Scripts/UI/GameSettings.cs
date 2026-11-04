using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// The player's settings from the menu (docs/PLAN.md, Phase 6): volume, look sensitivity, invert Y, fullscreen and
    /// resolution. Saved on this machine (PlayerPrefs) and applied at once; the orbit camera reads sensitivity and
    /// invert Y each frame. View only: none of it reaches the simulation except through the camera's input.
    /// </summary>
    public static class GameSettings
    {
        private const string VolumeKey = "BlockSauce.Volume";
        private const string SensitivityKey = "BlockSauce.LookSensitivity";
        private const string InvertYKey = "BlockSauce.InvertY";
        private const string FullscreenKey = "BlockSauce.Fullscreen";
        private const string ResolutionKey = "BlockSauce.Resolution";

        /// <summary>
        /// The slider range for look sensitivity, as a multiplier on the camera's own turn speeds.
        /// </summary>
        public const float MinSensitivity = 0.25f;
        public const float MaxSensitivity = 3f;

        private static bool _loaded;
        private static float _volume = 1f;
        private static float _sensitivity = 1f;
        private static bool _invertY;

        /// <summary>
        /// Master volume, 0 to 1.
        /// </summary>
        public static float Volume
        {
            get { Load(); return _volume; }
            set
            {
                _volume = Mathf.Clamp01(value);
                AudioListener.volume = _volume;
                PlayerPrefs.SetFloat(VolumeKey, _volume);
            }
        }

        /// <summary>
        /// Look sensitivity, a multiplier on the camera's mouse and stick speeds (1 is the default feel).
        /// </summary>
        public static float LookSensitivity
        {
            get { Load(); return _sensitivity; }
            set
            {
                _sensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
                PlayerPrefs.SetFloat(SensitivityKey, _sensitivity);
            }
        }

        /// <summary>
        /// Whether pushing the mouse or stick up looks down.
        /// </summary>
        public static bool InvertY
        {
            get { Load(); return _invertY; }
            set
            {
                _invertY = value;
                PlayerPrefs.SetInt(InvertYKey, value ? 1 : 0);
            }
        }

        /// <summary>
        /// Whether the game fills the screen.
        /// </summary>
        public static bool Fullscreen
        {
            get => Screen.fullScreen;
            set
            {
                Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            }
        }

        /// <summary>
        /// The resolutions this screen offers, one per size (the highest refresh rate of each).
        /// </summary>
        public static Resolution[] GetResolutions()
        {
            var sizes = new System.Collections.Generic.List<Resolution>();
            foreach (Resolution resolution in Screen.resolutions)
            {
                int existing = sizes.FindIndex(r => r.width == resolution.width && r.height == resolution.height);
                if (existing < 0)
                {
                    sizes.Add(resolution);
                }
                else if (resolution.refreshRateRatio.value > sizes[existing].refreshRateRatio.value)
                {
                    sizes[existing] = resolution;
                }
            }

            return sizes.ToArray();
        }

        /// <summary>
        /// Switches to <paramref name="resolution"/>, keeping fullscreen as it is.
        /// </summary>
        public static void SetResolution(Resolution resolution)
        {
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode, resolution.refreshRateRatio);
            PlayerPrefs.SetString(ResolutionKey, $"{resolution.width}x{resolution.height}");
        }

        /// <summary>
        /// Saves any changed settings to disk.
        /// </summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Reads the saved settings and applies the ones the engine holds (volume, window), once per run.
        /// </summary>
        public static void Load()
        {
            if (_loaded)
                return;

            _loaded = true;
            _volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            _sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);
            _invertY = PlayerPrefs.GetInt(InvertYKey, 0) == 1;
            AudioListener.volume = _volume;

#if !UNITY_EDITOR
            if (PlayerPrefs.HasKey(FullscreenKey))
            {
                Screen.fullScreenMode = PlayerPrefs.GetInt(FullscreenKey) == 1
                    ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            }

            string[] size = PlayerPrefs.GetString(ResolutionKey, string.Empty).Split('x');
            if (size.Length == 2 && int.TryParse(size[0], out int width) && int.TryParse(size[1], out int height))
            {
                Screen.SetResolution(width, height, Screen.fullScreenMode);
            }
#endif
        }
    }
}

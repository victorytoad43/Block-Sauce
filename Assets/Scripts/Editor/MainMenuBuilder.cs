using Quantum;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Navigation = UnityEngine.UI.Navigation;

namespace BlockSauce.Editor
{
    /// <summary>
    /// Builds the menu (docs/PLAN.md, Phase 6) from Jackson's art (A-002): TextMeshPro font assets from the Jaro fonts,
    /// the PH_MainMenu prefab laid out after his mock-up (logo top right, the button column under it, 1920×1080), and the
    /// Lobby's front end: a GameFlow object with the GameConnection (taking over the stock start UI's connection
    /// settings), an instance of the menu, the stock Quantum start UI removed and the local debug runner switched off
    /// (it would start a game of its own without a start UI). Rerunning it rebuilds the prefab; edits made by hand in
    /// the prefab are lost. Controls with no art yet (sliders, toggles, the dropdown, the code field) are drafts in the
    /// button's colors.
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string PrefabPath = "Assets/Placeholders/PH_MainMenu.prefab";
        private const string FontFolder = "Assets/Art/UI/Fonts";
        private const string LobbyPath = "Assets/Scenes/Lobby.unity";

        // Jackson's mock-up, 1920×1080: the column's center, 462 px in from the right edge, and the button size there.
        private const float ColumnX = -462f;
        private const float ButtonWidth = 357f;
        private const float ButtonHeight = 72f;
        private const float ButtonFontSize = 46f;
        private const float RowStep = 112f;

        private static readonly Color Ink = Color.white;
        private static readonly Color Shade = new Color(0.10f, 0.13f, 0.20f, 0.85f);
        private static readonly Color Steel = new Color(0.45f, 0.58f, 0.75f, 1f);
        private static readonly Color Sauce = new Color(0.86f, 0.25f, 0.20f, 1f);

        private static TMP_FontAsset _heading;
        private static TMP_FontAsset _body;
        private static Sprite _button;
        private static Sprite _buttonHover;
        private static Sprite _buttonPressed;

        [MenuItem("Block Sauce/Build Main Menu")]
        public static void Build()
        {
            _heading = MakeFont("UI_Jaro_36pt");
            _body = MakeFont("UI_Jaro_9pt");
            _button = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_MenuButton.png");
            _buttonHover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_MenuButton_Hover.png");
            _buttonPressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_MenuButton_Pressed.png");

            GameObject menu = BuildMenu();
            PrefabUtility.SaveAsPrefabAsset(menu, PrefabPath);
            Object.DestroyImmediate(menu);
            SetUpLobby();
            Debug.Log($"Built {PrefabPath} and set up the Lobby's menu.");
        }

        /// <summary>
        /// Makes (or reuses) a dynamic TextMeshPro font asset beside the TTF, so any text it meets gets its glyphs.
        /// </summary>
        private static TMP_FontAsset MakeFont(string name)
        {
            string path = $"{FontFolder}/{name} SDF.asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
                return existing;

            Font font = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{name}.ttf");
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic);
            asset.name = $"{name} SDF";
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = $"{name} Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = $"{name} Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static GameObject BuildMenu()
        {
            var canvasObject = new GameObject("PH_MainMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform canvasTransform = canvasObject.transform;

            // Everything but the room badge sits under Root, hidden while playing in a room.
            RectTransform root = Stretch(NewChild("Root", canvasTransform));
            Image column = NewChild("ColumnShade", root).gameObject.AddComponent<Image>();
            column.color = new Color(0f, 0f, 0f, 0.35f);
            // From the column's left edge (380 px left of its center) to the screen's right edge.
            Place(column.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2((ColumnX - 380f) * 0.5f, 0f),
                new Vector2(380f - ColumnX, 0f));
            column.raycastTarget = false;

            Image logo = NewChild("Logo", root).gameObject.AddComponent<Image>();
            logo.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Logo.png");
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            PlaceInColumn(logo.rectTransform, 342f, new Vector2(636f, 368f));

            // Main: Jackson's three buttons, plus Quit.
            RectTransform main = Stretch(NewChild("MainPanel", root));
            Button start = MakeButton(main, "StartButton", "START GAME", 628f);
            Button join = MakeButton(main, "JoinButton", "JOIN GAME", 628f + RowStep);
            Button settings = MakeButton(main, "SettingsButton", "SETTINGS", 628f + RowStep * 2);
            Button quit = MakeButton(main, "QuitButton", "QUIT", 628f + RowStep * 3);

            // Join: a code field, join, join any, back.
            RectTransform joinPanel = Stretch(NewChild("JoinPanel", root));
            TMP_InputField code = MakeCodeField(joinPanel, 628f);
            Button joinCode = MakeButton(joinPanel, "JoinCodeButton", "JOIN", 628f + RowStep);
            Button joinAny = MakeButton(joinPanel, "JoinAnyButton", "JOIN ANY ROOM", 628f + RowStep * 2);
            Button joinBack = MakeButton(joinPanel, "BackButton", "BACK", 628f + RowStep * 3);

            // Settings: label on the left of the column, control on the right.
            RectTransform settingsPanel = Stretch(NewChild("SettingsPanel", root));
            Slider volume = MakeSlider(settingsPanel, "Volume", "VOLUME", 590f, 0f, 1f);
            Slider sensitivity = MakeSlider(settingsPanel, "Sensitivity", "LOOK SPEED", 660f, 0.25f, 3f);
            Toggle invertY = MakeToggle(settingsPanel, "InvertY", "INVERT Y", 730f);
            Toggle fullscreen = MakeToggle(settingsPanel, "Fullscreen", "FULLSCREEN", 800f);
            TMP_Dropdown resolution = MakeDropdown(settingsPanel, "Resolution", "RESOLUTION", 870f);
            Button settingsBack = MakeButton(settingsPanel, "BackButton", "BACK", 964f);
            Navigate(volume, sensitivity, invertY, fullscreen, resolution, settingsBack);

            // Pause, in a room: dim the game, the room code to share, resume, settings, leave.
            RectTransform pause = Stretch(NewChild("PausePanel", root));
            pause.SetAsFirstSibling();
            Image dim = Stretch(NewChild("Dim", pause)).gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.45f);
            TMP_Text pauseCode = MakeText(pause, "RoomCode", "Room code: XXXXX", _heading, 40f, 584f);
            Button resume = MakeButton(pause, "ResumeButton", "RESUME", 676f);
            Button pauseSettings = MakeButton(pause, "SettingsButton", "SETTINGS", 676f + RowStep);
            Button leave = MakeButton(pause, "LeaveButton", "LEAVE ROOM", 676f + RowStep * 2);

            // Connecting notice and errors, under the column.
            RectTransform status = Stretch(NewChild("StatusPanel", root));
            TMP_Text statusText = MakeText(status, "StatusText", "Connecting…", _heading, 38f, 1040f);
            TMP_Text error = MakeText(root, "ErrorText", "", _body, 30f, 1040f);
            error.color = new Color(1f, 0.55f, 0.45f);

            // The room code in a corner while playing, so it can be shared.
            Image badge = NewChild("RoomBadge", canvasTransform).gameObject.AddComponent<Image>();
            badge.sprite = _button;
            badge.type = Image.Type.Sliced;
            badge.color = Shade;
            badge.raycastTarget = false;
            Place(badge.rectTransform, Vector2.one, Vector2.one, new Vector2(-220f, -60f), new Vector2(380f, 64f));
            TMP_Text badgeText = MakeText(badge.rectTransform, "Text", "ROOM  XXXXX", _heading, 36f, 0f);
            Stretch(badgeText.rectTransform);

            MainMenu controller = canvasObject.AddComponent<MainMenu>();
            var serialized = new SerializedObject(controller);
            Set(serialized, "_root", root.gameObject);
            Set(serialized, "_mainPanel", main.gameObject);
            Set(serialized, "_joinPanel", joinPanel.gameObject);
            Set(serialized, "_settingsPanel", settingsPanel.gameObject);
            Set(serialized, "_pausePanel", pause.gameObject);
            Set(serialized, "_statusPanel", status.gameObject);
            Set(serialized, "_roomBadge", badge.gameObject);
            Set(serialized, "_startButton", start);
            Set(serialized, "_joinButton", join);
            Set(serialized, "_settingsButton", settings);
            Set(serialized, "_quitButton", quit);
            Set(serialized, "_errorText", error);
            Set(serialized, "_codeInput", code);
            Set(serialized, "_joinCodeButton", joinCode);
            Set(serialized, "_joinAnyButton", joinAny);
            Set(serialized, "_joinBackButton", joinBack);
            Set(serialized, "_volumeSlider", volume);
            Set(serialized, "_sensitivitySlider", sensitivity);
            Set(serialized, "_invertYToggle", invertY);
            Set(serialized, "_fullscreenToggle", fullscreen);
            Set(serialized, "_resolutionDropdown", resolution);
            Set(serialized, "_settingsBackButton", settingsBack);
            Set(serialized, "_pauseCodeText", pauseCode);
            Set(serialized, "_resumeButton", resume);
            Set(serialized, "_pauseSettingsButton", pauseSettings);
            Set(serialized, "_leaveButton", leave);
            Set(serialized, "_statusText", statusText);
            Set(serialized, "_roomBadgeText", badgeText);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            joinPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            status.gameObject.SetActive(false);
            error.gameObject.SetActive(false);
            badge.gameObject.SetActive(false);
            return canvasObject;
        }

        /// <summary>
        /// Jackson's button: the bar with its hover and pressed looks (hover doubles as the controller's selection).
        /// </summary>
        private static Button MakeButton(Transform parent, string name, string label, float y)
        {
            RectTransform rect = NewChild(name, parent);
            PlaceInColumn(rect, y, new Vector2(ButtonWidth, ButtonHeight));
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = _button;
            image.type = Image.Type.Sliced;
            Button button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = _buttonHover,
                selectedSprite = _buttonHover,
                pressedSprite = _buttonPressed,
            };

            TMP_Text text = MakeText(rect, "Label", label, _heading, ButtonFontSize, 0f);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(16f, 2f);
            text.rectTransform.offsetMax = new Vector2(-16f, -2f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 24f;
            text.fontSizeMax = ButtonFontSize;
            return button;
        }

        private static TMP_InputField MakeCodeField(Transform parent, float y)
        {
            GameObject field = TMP_DefaultControls.CreateInputField(Resources());
            field.name = "CodeInput";
            field.transform.SetParent(parent, false);
            PlaceInColumn((RectTransform)field.transform, y, new Vector2(ButtonWidth, ButtonHeight));
            Image image = field.GetComponent<Image>();
            image.sprite = _button;
            image.type = Image.Type.Sliced;

            TMP_InputField input = field.GetComponent<TMP_InputField>();
            input.characterLimit = 8;
            input.contentType = TMP_InputField.ContentType.Alphanumeric;
            ShowFocus(input, image);
            foreach (TMP_Text text in field.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = _heading;
                text.fontSize = ButtonFontSize;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Ink;
                text.characterSpacing = 8f;
            }

            ((TMP_Text)input.placeholder).text = "ROOM CODE";
            ((TMP_Text)input.placeholder).color = new Color(1f, 1f, 1f, 0.4f);
            return input;
        }

        private static Slider MakeSlider(Transform parent, string name, string label, float y, float min, float max)
        {
            MakeRowLabel(parent, name + "Label", label, y);
            GameObject sliderObject = DefaultControls.CreateSlider(UiResources());
            sliderObject.name = name + "Slider";
            sliderObject.transform.SetParent(parent, false);
            PlaceInColumn((RectTransform)sliderObject.transform, y, new Vector2(230f, 28f), 120f);
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            foreach (Image image in sliderObject.GetComponentsInChildren<Image>(true))
            {
                image.color = image.name == "Fill" ? Sauce : image.name == "Handle" ? Color.white : Shade;
            }

            ShowFocus(slider, (Image)slider.targetGraphic, Ink);
            ((RectTransform)slider.handleRect).sizeDelta = new Vector2(30f, 0f);
            return slider;
        }

        private static Toggle MakeToggle(Transform parent, string name, string label, float y)
        {
            MakeRowLabel(parent, name + "Label", label, y);
            GameObject toggleObject = DefaultControls.CreateToggle(UiResources());
            toggleObject.name = name + "Toggle";
            toggleObject.transform.SetParent(parent, false);
            PlaceInColumn((RectTransform)toggleObject.transform, y, new Vector2(40f, 40f), 120f);
            Object.DestroyImmediate(toggleObject.GetComponentInChildren<Text>(true).gameObject);
            RectTransform background = (RectTransform)toggleObject.transform.Find("Background");
            Stretch(background);
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            ShowFocus(toggle, background.GetComponent<Image>());
            RectTransform check = (RectTransform)background.Find("Checkmark");
            Stretch(check);
            check.offsetMin = new Vector2(6f, 6f);
            check.offsetMax = new Vector2(-6f, -6f);
            check.GetComponent<Image>().color = Sauce;
            return toggle;
        }

        private static TMP_Dropdown MakeDropdown(Transform parent, string name, string label, float y)
        {
            MakeRowLabel(parent, name + "Label", label, y);
            GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(Resources());
            dropdownObject.name = name + "Dropdown";
            dropdownObject.transform.SetParent(parent, false);
            PlaceInColumn((RectTransform)dropdownObject.transform, y, new Vector2(230f, 44f), 120f);
            dropdownObject.transform.Find("Template").GetComponent<Image>().color = Shade;
            TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            ShowFocus(dropdown, dropdownObject.GetComponent<Image>());

            foreach (TMP_Text text in dropdownObject.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = _body;
                text.fontSize = 24f;
                text.color = Ink;
            }

            Transform item = dropdownObject.transform.Find("Template/Viewport/Content/Item/Item Background");
            if (item != null)
            {
                item.GetComponent<Image>().color = Steel;
            }

            return dropdown;
        }

        /// <summary>
        /// Makes a control show clearly which one has focus (a controller needs to see it): its graphic is
        /// <paramref name="normal"/> (the dark shade by default), steel blue under the mouse, sauce orange when selected.
        /// </summary>
        private static void ShowFocus(Selectable selectable, Image graphic, Color? normal = null)
        {
            graphic.color = Color.white;
            selectable.targetGraphic = graphic;
            selectable.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = selectable.colors;
            colors.normalColor = normal ?? Shade;
            colors.highlightedColor = Steel;
            colors.selectedColor = new Color(1f, 0.62f, 0.25f, 1f);
            colors.pressedColor = Sauce;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            selectable.colors = colors;
        }

        private static void MakeRowLabel(Transform parent, string name, string label, float y)
        {
            TMP_Text text = MakeText(parent, name, label, _heading, 32f, y);
            PlaceInColumn(text.rectTransform, y, new Vector2(250f, 50f), -135f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }

        /// <summary>
        /// Up and down between the settings rows, in order, rather than Unity's guess from their positions.
        /// </summary>
        private static void Navigate(params Selectable[] rows)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                Navigation navigation = rows[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = rows[(i + rows.Length - 1) % rows.Length];
                navigation.selectOnDown = rows[(i + 1) % rows.Length];
                rows[i].navigation = navigation;
            }
        }

        private static TMP_Text MakeText(Transform parent, string name, string value, TMP_FontAsset font, float size, float y)
        {
            RectTransform rect = NewChild(name, parent);
            PlaceInColumn(rect, y, new Vector2(700f, 60f));
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = Ink;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform NewChild(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        private static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>
        /// Places a control in the button column, centered <paramref name="y"/> pixels from the top of the 1920×1080
        /// layout and <paramref name="xOffset"/> to the right of the column's center.
        /// </summary>
        private static void PlaceInColumn(RectTransform rect, float y, Vector2 size, float xOffset = 0f)
        {
            Place(rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(ColumnX + xOffset, -y), size);
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Set(SerializedObject serialized, string field, Object value)
        {
            serialized.FindProperty(field).objectReferenceValue = value;
        }

        private static TMP_DefaultControls.Resources Resources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
            };
        }

        private static DefaultControls.Resources UiResources()
        {
            TMP_DefaultControls.Resources tmp = Resources();
            return new DefaultControls.Resources
            {
                standard = tmp.standard,
                background = tmp.background,
                inputField = tmp.inputField,
                knob = tmp.knob,
                checkmark = tmp.checkmark,
                dropdown = tmp.dropdown,
                mask = tmp.mask,
            };
        }

        /// <summary>
        /// Puts the front end into the Lobby: GameFlow with its connection (settings taken from the stock start UI's),
        /// the menu, no stock start UI, and the local debug runner off. Saves the scene.
        /// </summary>
        private static void SetUpLobby()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != LobbyPath)
            {
                scene = EditorSceneManager.OpenScene(LobbyPath);
            }

            GameObject flowObject = GameObject.Find("GameFlow");
            if (flowObject == null)
            {
                flowObject = new GameObject("GameFlow");
            }

            GameConnection connection = flowObject.GetComponent<GameConnection>() ?? flowObject.AddComponent<GameConnection>();
            GameFlow flow = flowObject.GetComponent<GameFlow>() ?? flowObject.AddComponent<GameFlow>();

            QuantumStartUIConnection stock = null;
            foreach (QuantumStartUIConnection candidate in Object.FindObjectsByType<QuantumStartUIConnection>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate is GameConnection == false)
                {
                    stock = candidate;
                }
            }

            if (stock != null)
            {
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(stock), connection);
                Object.DestroyImmediate(stock.GetComponentInParent<QuantumStartUI>(true)?.gameObject ?? stock.gameObject);
            }

            var flowSerialized = new SerializedObject(flow);
            flowSerialized.FindProperty("_connection").objectReferenceValue = connection;
            flowSerialized.ApplyModifiedPropertiesWithoutUndo();

            // The stock UI's empty canvas goes too.
            GameObject oldCanvas = GameObject.Find("Canvas");
            if (oldCanvas != null && oldCanvas.transform.childCount == 0)
            {
                Object.DestroyImmediate(oldCanvas);
            }

            QuantumRunnerLocalDebug debugRunner = Object.FindAnyObjectByType<QuantumRunnerLocalDebug>(FindObjectsInactive.Include);
            if (debugRunner != null)
            {
                debugRunner.gameObject.SetActive(false);
            }

            GameObject existingMenu = GameObject.Find("PH_MainMenu");
            if (existingMenu != null)
            {
                Object.DestroyImmediate(existingMenu);
            }

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}

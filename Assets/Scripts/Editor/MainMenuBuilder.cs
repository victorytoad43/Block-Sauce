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
    /// Builds the menu (docs/PLAN.md, Phase 6) from Jackson's art (A-002): TextMeshPro font assets from the Jaro and DynaPuff fonts,
    /// the PH_MainMenu prefab laid out after his mock-up (logo top right, the button column under it, 1920×1080), and the
    /// Lobby's front end: a GameFlow object with the GameConnection (taking over the stock start UI's connection
    /// settings), an instance of the menu, the stock Quantum start UI removed and the local debug runner switched off
    /// (it would start a game of its own without a start UI). Rerunning it rebuilds the prefab; edits made by hand in
    /// the prefab are lost. Controls with no art yet (sliders, toggles, the dropdown, the code field) are drafts in the
    /// button's colors. It also builds the room screen (in the menu) and the match HUD (PH_MatchHud), drafts in the same
    /// style until Jackson's room and HUD art (A-002, A-006), and swaps the Lobby's IMGUI HUD for them and DevKeys.
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string PrefabPath = "Assets/Placeholders/PH_MainMenu.prefab";
        private const string FontFolder = "Assets/Art/UI/Fonts";
        private const string LobbyPath = "Assets/Scenes/Lobby.unity";
        private const string HudPath = "Assets/Placeholders/PH_MatchHud.prefab";
        private const string PalettePath = "Assets/Placeholders/PH_PlayerColors.asset";
        private const int MaxPlayers = 6;

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

        private static TMP_FontAsset _display;
        private static TMP_FontAsset _heading;
        private static TMP_FontAsset _body;
        private static Sprite _button;
        private static Sprite _buttonHover;
        private static Sprite _buttonPressed;
        private static PlayerPalette _palette;

        [MenuItem("Block Sauce/Build Main Menu")]
        public static void Build()
        {
            // Jaro (the logo's first line) for the big display text; DynaPuff (its second line) for everything read
            // closely, since Jaro's V reads as U (Jackson, 2026-11-04).
            _display = MakeFont("UI_Jaro_36pt");
            _heading = MakeFont("UI_DynaPuff_Bold");
            _body = MakeFont("UI_DynaPuff_Regular");
            _button = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_MenuButton.png");
            _buttonHover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_MenuButton_Hover.png");
            _buttonPressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_MenuButton_Pressed.png");
            _palette = AssetDatabase.LoadAssetAtPath<PlayerPalette>(PalettePath);

            GameObject menu = BuildMenu();
            PrefabUtility.SaveAsPrefabAsset(menu, PrefabPath);
            Object.DestroyImmediate(menu);
            GameObject hud = BuildHud();
            PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
            Object.DestroyImmediate(hud);
            SetUpLobby();
            Debug.Log($"Built {PrefabPath} and {HudPath} and set up the Lobby's menu and HUD.");
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

            // Only over the menu screens: the room screen's column starts higher.
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

            // The room screen, in a room's lobby: the players on the left, the room's controls in the column.
            RectTransform roomPanel = Stretch(NewChild("RoomPanel", root));
            roomPanel.SetAsFirstSibling();
            Image roomDim = Stretch(NewChild("Dim", roomPanel)).gameObject.AddComponent<Image>();
            roomDim.color = new Color(0f, 0f, 0f, 0.3f);
            RoomScreen roomScreen = BuildRoomList(roomPanel, out TMP_Text roomCode, out RoomScreen.Row[] roomRows);
            const float roomTop = 300f;
            const float roomStep = 100f;
            Button roomStart = MakeButton(roomPanel, "StartMatchButton", "START MATCH", roomTop);
            Button addBot = MakeButton(roomPanel, "AddBotButton", "ADD BOT", roomTop + roomStep);
            Button removeBot = MakeButton(roomPanel, "RemoveBotButton", "REMOVE BOT", roomTop + roomStep * 2);
            RectTransform fillRow = Stretch(NewChild("FillRow", roomPanel));
            Toggle fill = MakeToggle(fillRow, "BotFill", "BOTS FILL SLOTS", roomTop + roomStep * 3);
            Button roomPlay = MakeButton(roomPanel, "PlayButton", "PLAY IN LOBBY", roomTop + roomStep * 4);
            Button roomSettings = MakeButton(roomPanel, "SettingsButton", "SETTINGS", roomTop + roomStep * 5);
            Button roomLeave = MakeButton(roomPanel, "LeaveButton", "LEAVE ROOM", roomTop + roomStep * 6);
            TMP_Text waiting = MakeText(roomPanel, "WaitingText", "WAITING FOR THE HOST\nTO START THE MATCH", _heading, 40f,
                roomTop + roomStep);
            waiting.rectTransform.sizeDelta = new Vector2(500f, 160f);

            var roomSerialized = new SerializedObject(roomScreen);
            roomSerialized.FindProperty("_palette").objectReferenceValue = _palette;
            Set(roomSerialized, "_codeText", roomCode);
            SetRows(roomSerialized.FindProperty("_rows"), roomRows.Length, (row, i) =>
            {
                row.FindPropertyRelative("Root").objectReferenceValue = roomRows[i].Root;
                row.FindPropertyRelative("Swatch").objectReferenceValue = roomRows[i].Swatch;
                row.FindPropertyRelative("Name").objectReferenceValue = roomRows[i].Name;
                row.FindPropertyRelative("Tag").objectReferenceValue = roomRows[i].Tag;
            });
            Set(roomSerialized, "_startButton", roomStart);
            Set(roomSerialized, "_addBotButton", addBot);
            Set(roomSerialized, "_removeBotButton", removeBot);
            Set(roomSerialized, "_fillToggle", fill);
            Set(roomSerialized, "_fillRow", fillRow.gameObject);
            Set(roomSerialized, "_waitingText", waiting);
            roomSerialized.ApplyModifiedPropertiesWithoutUndo();

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
            Set(serialized, "_roomPanel", roomPanel.gameObject);
            Set(serialized, "_logo", logo.gameObject);
            Set(serialized, "_roomScreen", roomScreen);
            Set(serialized, "_roomPlayButton", roomPlay);
            Set(serialized, "_roomSettingsButton", roomSettings);
            Set(serialized, "_roomLeaveButton", roomLeave);
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
            roomPanel.gameObject.SetActive(false);
            status.gameObject.SetActive(false);
            error.gameObject.SetActive(false);
            badge.gameObject.SetActive(false);
            return canvasObject;
        }

        /// <summary>
        /// The room screen's left side: the room code to share and one row per player slot (color, name, tag).
        /// </summary>
        private static RoomScreen BuildRoomList(RectTransform panel, out TMP_Text code, out RoomScreen.Row[] rows)
        {
            RectTransform box = NewChild("PlayerList", panel);
            Place(box, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f + 360f, -160f - 340f),
                new Vector2(720f, 680f));
            Image boxImage = box.gameObject.AddComponent<Image>();
            boxImage.color = Shade;
            boxImage.raycastTarget = false;
            RoomScreen screen = box.gameObject.AddComponent<RoomScreen>();

            code = MakeLeftText(box, "RoomCode", "ROOM  XXXXX", _heading, 72f, new Vector2(40f, -70f), 640f);
            MakeLeftText(box, "Hint", "SHARE THE CODE SO FRIENDS CAN JOIN", _body, 26f, new Vector2(40f, -130f), 640f)
                .color = new Color(1f, 1f, 1f, 0.7f);

            rows = new RoomScreen.Row[MaxPlayers];
            for (int i = 0; i < MaxPlayers; i++)
            {
                RectTransform row = NewChild($"Slot{i + 1}", box);
                Place(row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, -210f - i * 76f),
                    new Vector2(640f, 64f));
                Image swatch = NewChild("Swatch", row).gameObject.AddComponent<Image>();
                Place(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f),
                    new Vector2(52f, 52f));
                swatch.raycastTarget = false;
                rows[i] = new RoomScreen.Row
                {
                    Root = row.gameObject,
                    Swatch = swatch,
                    Name = MakeLeftText(row, "Name", "PLAYER", _heading, 40f, new Vector2(76f, 0f), 340f, middle: true),
                    Tag = MakeLeftText(row, "Tag", "", _body, 26f, new Vector2(420f, 0f), 220f, middle: true),
                };
                rows[i].Tag.alignment = TextAlignmentOptions.MidlineRight;
            }

            return screen;
        }

        /// <summary>
        /// Left-aligned text, its left edge <paramref name="position"/>.x from the parent's left, its center
        /// <paramref name="position"/>.y from the parent's top (or its middle, with <paramref name="middle"/>).
        /// </summary>
        private static TMP_Text MakeLeftText(Transform parent, string name, string value, TMP_FontAsset font, float size,
            Vector2 position, float width, bool middle = false)
        {
            RectTransform rect = NewChild(name, parent);
            Vector2 anchor = new Vector2(0f, middle ? 0.5f : 1f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, size * 1.4f);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = Ink;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// The match HUD: the headline bar, the big center text (countdown, GO!, the winner), the lobby prompt, every
        /// player's tower slots top left, and the results panel. Nothing in it takes clicks.
        /// </summary>
        private static GameObject BuildHud()
        {
            var canvasObject = new GameObject("PH_MatchHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform root = Stretch(NewChild("Root", canvasObject.transform));

            Image headline = MakeBar(root, "Headline", new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(900f, 72f));
            TMP_Text headlineText = MakeCenterText(headline.rectTransform, "Text", "GET READY", _heading, 40f);
            Stretch(headlineText.rectTransform);

            TMP_Text big = MakeCenterText(root, "BigText", "3", _display, 200f);
            Place(big.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f),
                new Vector2(1600f, 280f));
            big.enableAutoSizing = true;
            big.fontSizeMin = 60f;
            big.fontSizeMax = 200f;

            Image prompt = MakeBar(root, "Prompt", new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1100f, 64f));
            TMP_Text promptText = MakeCenterText(prompt.rectTransform, "Text", "ENTER / START  START MATCH", _heading, 32f);
            Stretch(promptText.rectTransform);

            // Every player's best tower: color, name, and the three slots.
            RectTransform towers = NewChild("Towers", root);
            Place(towers, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f + 250f, -40f - 190f),
                new Vector2(500f, 380f));
            Image towersImage = towers.gameObject.AddComponent<Image>();
            towersImage.color = new Color(Shade.r, Shade.g, Shade.b, 0.6f);
            towersImage.raycastTarget = false;
            var towerRows = new MatchHud.TowerRow[MaxPlayers];
            for (int i = 0; i < MaxPlayers; i++)
            {
                RectTransform row = NewChild($"Row{i + 1}", towers);
                Place(row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -40f - i * 60f), new Vector2(480f, 52f));
                Image swatch = NewChild("Swatch", row).gameObject.AddComponent<Image>();
                Place(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f),
                    new Vector2(36f, 36f));
                swatch.raycastTarget = false;
                TMP_Text name = MakeLeftText(row, "Name", "PLAYER", _heading, 32f, new Vector2(62f, 0f), 220f, middle: true);
                var slots = new Image[3];
                for (int level = 0; level < slots.Length; level++)
                {
                    // Left to right is bottom to top of the tower.
                    Image back = NewChild($"Slot{level + 1}", row).gameObject.AddComponent<Image>();
                    Place(back.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(310f + level * 54f, 0f), new Vector2(46f, 40f));
                    back.color = new Color(0f, 0f, 0f, 0.5f);
                    back.raycastTarget = false;
                    Image fill = Stretch(NewChild("Fill", back.rectTransform)).gameObject.AddComponent<Image>();
                    fill.raycastTarget = false;
                    fill.rectTransform.anchorMax = new Vector2(1f, 0f);
                    slots[level] = fill;
                }

                towerRows[i] = new MatchHud.TowerRow { Root = row.gameObject, Swatch = swatch, Name = name, Slots = slots };
            }

            // Results: the winner, everyone ranked, and the time until the room screen.
            RectTransform results = NewChild("Results", root);
            Place(results, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 700f));
            Image resultsImage = results.gameObject.AddComponent<Image>();
            resultsImage.color = Shade;
            resultsImage.raycastTarget = false;
            TMP_Text resultsTitle = MakeCenterText(results, "Title", "RESULTS", _display, 48f);
            Place(resultsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f),
                new Vector2(760f, 70f));
            resultsTitle.color = new Color(1f, 1f, 1f, 0.7f);
            TMP_Text winner = MakeCenterText(results, "Winner", "PLAYER 1 WINS!", _display, 72f);
            Place(winner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -125f),
                new Vector2(760f, 90f));
            var resultRows = new MatchHud.ResultRow[MaxPlayers];
            for (int i = 0; i < MaxPlayers; i++)
            {
                RectTransform row = NewChild($"Row{i + 1}", results);
                Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -215f - i * 64f),
                    new Vector2(680f, 56f));
                Image swatch = NewChild("Swatch", row).gameObject.AddComponent<Image>();
                Place(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f),
                    new Vector2(40f, 40f));
                swatch.raycastTarget = false;
                TMP_Text name = MakeLeftText(row, "Name", "1.  PLAYER 1", _heading, 38f, new Vector2(60f, 0f), 440f,
                    middle: true);
                TMP_Text count = MakeLeftText(row, "Count", "3 / 3", _heading, 38f, new Vector2(500f, 0f), 180f,
                    middle: true);
                count.alignment = TextAlignmentOptions.MidlineRight;
                resultRows[i] = new MatchHud.ResultRow { Root = row.gameObject, Swatch = swatch, Name = name, Count = count };
            }

            TMP_Text footer = MakeCenterText(results, "Footer", "BACK TO THE ROOM IN 5", _body, 28f);
            Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 45f),
                new Vector2(760f, 50f));

            MatchHud hud = canvasObject.AddComponent<MatchHud>();
            var serialized = new SerializedObject(hud);
            serialized.FindProperty("_palette").objectReferenceValue = _palette;
            Set(serialized, "_root", root.gameObject);
            Set(serialized, "_headline", headline.gameObject);
            Set(serialized, "_headlineText", headlineText);
            Set(serialized, "_bigText", big);
            Set(serialized, "_promptText", promptText);
            Set(serialized, "_towerPanel", towers.gameObject);
            SetRows(serialized.FindProperty("_towerRows"), towerRows.Length, (row, i) =>
            {
                row.FindPropertyRelative("Root").objectReferenceValue = towerRows[i].Root;
                row.FindPropertyRelative("Swatch").objectReferenceValue = towerRows[i].Swatch;
                row.FindPropertyRelative("Name").objectReferenceValue = towerRows[i].Name;
                SerializedProperty slots = row.FindPropertyRelative("Slots");
                slots.arraySize = towerRows[i].Slots.Length;
                for (int level = 0; level < slots.arraySize; level++)
                {
                    slots.GetArrayElementAtIndex(level).objectReferenceValue = towerRows[i].Slots[level];
                }
            });
            Set(serialized, "_resultsPanel", results.gameObject);
            Set(serialized, "_resultsWinner", winner);
            SetRows(serialized.FindProperty("_resultRows"), resultRows.Length, (row, i) =>
            {
                row.FindPropertyRelative("Root").objectReferenceValue = resultRows[i].Root;
                row.FindPropertyRelative("Swatch").objectReferenceValue = resultRows[i].Swatch;
                row.FindPropertyRelative("Name").objectReferenceValue = resultRows[i].Name;
                row.FindPropertyRelative("Count").objectReferenceValue = resultRows[i].Count;
            });
            Set(serialized, "_resultsFooter", footer);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            headline.gameObject.SetActive(false);
            big.gameObject.SetActive(false);
            towers.gameObject.SetActive(false);
            results.gameObject.SetActive(false);
            return canvasObject;
        }

        /// <summary>
        /// A shaded bar in Jackson's button shape, for HUD text.
        /// </summary>
        private static Image MakeBar(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image bar = NewChild(name, parent).gameObject.AddComponent<Image>();
            bar.sprite = _button;
            bar.type = Image.Type.Sliced;
            bar.color = Shade;
            bar.raycastTarget = false;
            Place(bar.rectTransform, anchor, anchor, position, size);
            return bar;
        }

        private static TMP_Text MakeCenterText(Transform parent, string name, string value, TMP_FontAsset font, float size)
        {
            RectTransform rect = NewChild(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = Ink;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRows(SerializedProperty array, int count, System.Action<SerializedProperty, int> set)
        {
            array.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                set(array.GetArrayElementAtIndex(i), i);
            }
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

            foreach (string name in new[] { "PH_MainMenu", "PH_MatchHud" })
            {
                GameObject existing = GameObject.Find(name);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }
            }

            // The IMGUI HUD goes; its dev keys move to DevKeys.
            GameObject oldHud = GameObject.Find("MatchHud");
            if (oldHud != null)
            {
                Object.DestroyImmediate(oldHud);
            }

            if (Object.FindAnyObjectByType<DevKeys>(FindObjectsInactive.Include) == null)
            {
                new GameObject("DevKeys", typeof(DevKeys));
            }

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath), scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}

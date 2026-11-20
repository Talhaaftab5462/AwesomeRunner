using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RunnerUIBuilder
{
    // ---------- small helpers ----------
    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        return t;
    }

    static Button MakeButton(Transform parent, string name, string label, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 80);
        var img = go.GetComponent<Image>();
        img.color = color;
        go.GetComponent<Button>().targetGraphic = img;
        MakeText(go.transform, "Label", label, 36, 320, 80);
        return go.GetComponent<Button>();
    }

    static void SetupVerticalLayout(GameObject go, float spacing)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = spacing;
        v.childControlWidth = false;
        v.childControlHeight = false;
        v.childForceExpandWidth = false;
        v.childForceExpandHeight = false;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static string FindScenePath(string sceneName)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sceneName)
                return path;
        }
        return null;
    }

    // ---------- MENU 1: game scene ----------
    [MenuItem("Tools/Runner/1 - Add Game Over screen and Coin counter")]
    public static void BuildGameSceneUI()
    {
        var hud = Object.FindFirstObjectByType<PlayerHUDView>(FindObjectsInactive.Include);
        var stats = Object.FindFirstObjectByType<Statistics>(FindObjectsInactive.Include);
        var viewManager = Object.FindFirstObjectByType<ViewManager>(FindObjectsInactive.Include);

        if (hud == null || stats == null || viewManager == null)
        {
            EditorUtility.DisplayDialog("Runner UI",
                "Could not find the game objects. Open GameScene first (double-click it in Assets/Scenes), then try again.", "OK");
            return;
        }
        if (Object.FindFirstObjectByType<GameOverView>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("Runner UI", "A Game Over screen already exists in this scene.", "OK");
            return;
        }

        AddCoinText(hud);

        var canvas = hud.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        var root = canvas.rootCanvas.transform;

        var gameOver = CreateGameOverPanel(root, stats);
        AddToViewManager(viewManager, gameOver);

        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        EditorUtility.DisplayDialog("Runner UI",
            "Done! Game Over screen and coin counter added.\n\nPress Ctrl+S to save the scene.", "OK");
    }

    static void AddCoinText(PlayerHUDView hud)
    {
        var so = new SerializedObject(hud);
        var coinProp = so.FindProperty("coinText");
        if (coinProp.objectReferenceValue != null) return;

        var scoreText = so.FindProperty("scoreText").objectReferenceValue as TMP_Text;
        if (scoreText == null)
        {
            Debug.LogWarning("RunnerUIBuilder: PlayerHUDView has no Score Text assigned, so no coin text was created.");
            return;
        }
        var copy = Object.Instantiate(scoreText.gameObject, scoreText.transform.parent);
        copy.name = "CoinText";
        var t = copy.GetComponent<TMP_Text>();
        t.text = "Coins: 0";
        copy.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, -60);
        Undo.RegisterCreatedObjectUndo(copy, "Create CoinText");

        coinProp.objectReferenceValue = t;
        so.ApplyModifiedProperties();
    }

    static GameOverView CreateGameOverPanel(Transform canvasRoot, Statistics stats)
    {
        var panel = new GameObject("GameOverView", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasRoot, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
        SetupVerticalLayout(panel, 20f);

        var title = MakeText(panel.transform, "TitleText", "GAME OVER", 90, 800, 120);
        var score = MakeText(panel.transform, "ScoreText", "Score: 0", 50, 800, 70);
        var coins = MakeText(panel.transform, "CoinsText", "Coins: 0", 50, 800, 70);
        var best = MakeText(panel.transform, "HighScoreText", "High Score: 0", 40, 800, 70);
        var restart = MakeButton(panel.transform, "RestartButton", "Restart", new Color(0.15f, 0.65f, 0.3f, 1f));
        var menu = MakeButton(panel.transform, "MainMenuButton", "Main Menu", new Color(0.2f, 0.45f, 0.9f, 1f));

        var view = panel.AddComponent<GameOverView>();
        var so = new SerializedObject(view);
        so.FindProperty("playerStatistics").objectReferenceValue = stats;
        so.FindProperty("scoreText").objectReferenceValue = score;
        so.FindProperty("coinsText").objectReferenceValue = coins;
        so.FindProperty("highScoreText").objectReferenceValue = best;
        so.FindProperty("restartButton").objectReferenceValue = restart;
        so.FindProperty("mainMenuButton").objectReferenceValue = menu;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(panel, "Create GameOverView");
        panel.SetActive(false); // ViewManager shows it when the player dies
        return view;
    }

    static void AddToViewManager(ViewManager vm, GameOverView view)
    {
        var so = new SerializedObject(vm);
        var views = so.FindProperty("views");
        for (int i = 0; i < views.arraySize; i++)
            if (views.GetArrayElementAtIndex(i).objectReferenceValue == view) return;

        views.arraySize++;
        views.GetArrayElementAtIndex(views.arraySize - 1).objectReferenceValue = view;
        so.ApplyModifiedProperties();

        if (!so.FindProperty("autoInitialize").boolValue)
            Debug.LogWarning("RunnerUIBuilder: ViewManager 'Auto Initialize' is OFF. Tick it so the Game Over buttons work.");
    }

    // ---------- MENU 2: main menu scene ----------
    [MenuItem("Tools/Runner/2 - Create Main Menu scene")]
    public static void BuildMainMenuScene()
    {
        string path = FindScenePath("MainMenu");
        if (path != null)
        {
            if (!EditorUtility.DisplayDialog("Runner UI",
                "A MainMenu scene already exists at:\n" + path + "\n\nReplace it with a new one?", "Replace", "Cancel"))
                return;
        }
        else
        {
            Directory.CreateDirectory("Assets/Scenes");
            path = "Assets/Scenes/MainMenu.unity";
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.14f);
        }

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var menuRoot = new GameObject("MenuLayout", typeof(RectTransform));
        menuRoot.transform.SetParent(canvasGO.transform, false);
        Stretch(menuRoot.GetComponent<RectTransform>());
        SetupVerticalLayout(menuRoot, 30f);

        MakeText(menuRoot.transform, "TitleText", "AWESOME RUNNER", 120, 1400, 180);
        var best = MakeText(menuRoot.transform, "HighScoreText", "High Score: 0", 44, 800, 80);
        var play = MakeButton(menuRoot.transform, "PlayButton", "Play", new Color(0.15f, 0.65f, 0.3f, 1f));
        var quit = MakeButton(menuRoot.transform, "QuitButton", "Quit", new Color(0.7f, 0.2f, 0.2f, 1f));

        // event system (makes buttons clickable); let Unity pick the right input module
        EditorApplication.ExecuteMenuItem("GameObject/UI/Event System");
        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var menuGO = new GameObject("MainMenu");
        var menu = menuGO.AddComponent<MainMenu>();
        var so = new SerializedObject(menu);
        so.FindProperty("playButton").objectReferenceValue = play;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.FindProperty("highScoreText").objectReferenceValue = best;
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, path);

        // build settings: MainMenu first, GameScene second
        string gamePath = FindScenePath("GameScene");
        var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(path, true) };
        if (gamePath != null)
            list.Add(new EditorBuildSettingsScene(gamePath, true));
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != path && s.path != gamePath)
                list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();

        EditorUtility.DisplayDialog("Runner UI",
            "Main Menu scene created and both scenes added to Build Settings.\n\nPress Play now to try it.", "OK");
    }
}
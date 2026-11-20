using System.IO;
using Pools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RunnerObstacleBuilder
{
    static Shader FindObstacleShader()
    {
        // reuse the shader your other obstacles use so the barrel curves with the world like everything else
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (go != null && go.GetComponent<Obstacle>() != null)
            {
                var r = go.GetComponentInChildren<Renderer>();
                if (r != null && r.sharedMaterial != null)
                    return r.sharedMaterial.shader;
            }
        }
        var s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Sprites/Default");
        return s;
    }

    static Material MakeMaterial(string path, Shader shader, Color color, bool glow)
    {
        var mat = new Material(shader);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (glow && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 3f);
        }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static GameObject MakePart(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // visual only
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    [MenuItem("Tools/Runner/3 - Add slide-under laser barrier")]
    public static void BuildLaserBarrier()
    {
        var player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        var generator = Object.FindFirstObjectByType<ChunkGenerator>(FindObjectsInactive.Include);
        var lanes = Object.FindFirstObjectByType<LaneSystem>(FindObjectsInactive.Include);
        if (player == null || generator == null || lanes == null)
        {
            EditorUtility.DisplayDialog("Runner", "Open GameScene first (double-click it in Assets/Scenes), then try again.", "OK");
            return;
        }
        if (GameObject.Find("LaserBarrierPool") != null)
        {
            EditorUtility.DisplayDialog("Runner", "The laser barrier was already added to this scene.", "OK");
            return;
        }

        // sizes
        var cc = player.GetComponent<CharacterController>();
        float playerHeight = cc.height * player.transform.lossyScale.y;
        float slideTop = playerHeight * 0.5f;          // slide halves the collider height
        float bottom = slideTop + 0.15f;               // just above a sliding player
        float top = bottom + 3f;                       // too tall to jump over
        var laneSo = new SerializedObject(lanes);
        int laneCount = Mathf.Max(1, laneSo.FindProperty("laneCount").intValue);
        float width = (laneCount + 1) * lanes.LaneWidth;

        // assets
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Materials");
        var shader = FindObstacleShader();
        var beamMat = MakeMaterial("Assets/Materials/LaserBeam.mat", shader, new Color(1f, 0.1f, 0.1f), true);
        var postMat = MakeMaterial("Assets/Materials/LaserPost.mat", shader, new Color(0.2f, 0.2f, 0.25f), false);

        // prefab
        var root = new GameObject("LaserBarrier");
        var box = root.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, (bottom + top) * 0.5f, 0f);
        box.size = new Vector3(width, top - bottom, 0.5f);

        var obstacle = root.AddComponent<Obstacle>();
        var oso = new SerializedObject(obstacle);
        oso.FindProperty("<IsOnAllLanes>k__BackingField").boolValue = true;
        oso.ApplyModifiedProperties();

        MakePart(root.transform, "Beam", new Vector3(0f, bottom + 0.06f, 0f), new Vector3(width, 0.12f, 0.12f), beamMat);
        float postHeight = bottom + 0.8f;
        MakePart(root.transform, "PostLeft", new Vector3(-width * 0.5f, postHeight * 0.5f, 0f), new Vector3(0.3f, postHeight, 0.3f), postMat);
        MakePart(root.transform, "PostRight", new Vector3(width * 0.5f, postHeight * 0.5f, 0f), new Vector3(0.3f, postHeight, 0.3f), postMat);

        var prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/LaserBarrier.prefab");
        Object.DestroyImmediate(root);
        var prefabObstacle = prefabAsset.GetComponent<Obstacle>();

        // pool in the scene
        var poolsParent = GameObject.Find("Pools");
        var poolGO = new GameObject("LaserBarrierPool");
        if (poolsParent != null) poolGO.transform.SetParent(poolsParent.transform, false);
        var pool = poolGO.AddComponent<ObstaclePool>();
        var pso = new SerializedObject(pool);
        pso.FindProperty("capacity").intValue = 4;
        pso.FindProperty("prefab").objectReferenceValue = prefabObstacle;
        pso.ApplyModifiedProperties();
        Undo.RegisterCreatedObjectUndo(poolGO, "Create LaserBarrierPool");

        // register the pool with the generator
        var gso = new SerializedObject(generator);
        var list = gso.FindProperty("<ObstaclePools>k__BackingField");
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = pool;
        gso.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(poolGO.scene);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Runner",
            "Laser barrier added.\n\nPress Ctrl+S to save the scene, then press Play. Slide (S or Down arrow) to go under it.", "OK");
    }

    static void ShiftBarrier(float delta)
    {
        const string path = "Assets/Prefabs/LaserBarrier.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);

        var box = root.GetComponent<BoxCollider>();
        box.center += new Vector3(0f, delta * 0.5f, 0f);   // raises the bottom edge,
        box.size -= new Vector3(0f, delta, 0f);            // keeps the top where it is

        var beam = root.transform.Find("Beam");
        if (beam != null) beam.localPosition += new Vector3(0f, delta, 0f);

        foreach (var postName in new[] { "PostLeft", "PostRight" })
        {
            var post = root.transform.Find(postName);
            if (post == null) continue;
            post.localScale += new Vector3(0f, delta, 0f);
            post.localPosition += new Vector3(0f, delta * 0.5f, 0f);
        }

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Runner/4 - Raise laser barrier a bit")]
    public static void RaiseBarrier() { ShiftBarrier(0.2f); }

    [MenuItem("Tools/Runner/5 - Lower laser barrier a bit")]
    public static void LowerBarrier() { ShiftBarrier(-0.2f); }
}
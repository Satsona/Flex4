#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time (or re-runnable) setup: replaces capsule mesh on Player prefab with Ch14 + AC_Ch14 animator.
/// Also wires the ThirdPersonPlayer.animator reference automatically.
/// </summary>
public static class PlayerCh14Setup
{
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    const string Ch14ModelPath = "Assets/Characters/Ch14/Ch14_nonPBR.fbx";
    const string AnimatorControllerPath = "Assets/Characters/Ch14/Animations/AC_Ch14.controller";

    public static void RunFromBatch()
    {
        AttachCh14ToPlayerPrefab();
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    [MenuItem("Tools/Player/Attach Ch14 to Player Prefab")]
    public static void AttachCh14ToPlayerPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

        try
        {
            RemoveComponentIfExists<MeshFilter>(root);
            RemoveComponentIfExists<MeshRenderer>(root);
            RemoveComponentIfExists<CapsuleCollider>(root);

            Transform existing = root.transform.Find("Ch14_nonPBR");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var ch14Asset = AssetDatabase.LoadAssetAtPath<GameObject>(Ch14ModelPath);
            if (ch14Asset == null)
            {
                Debug.LogError($"Missing model at {Ch14ModelPath}");
                return;
            }

            var controllerAsset =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (controllerAsset == null)
            {
                Debug.LogError($"Missing animator controller at {AnimatorControllerPath}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(ch14Asset, root.transform);
            instance.name = "Ch14_nonPBR";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var animator = instance.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogError("No Animator found under Ch14 model.");
                return;
            }

            animator.runtimeAnimatorController = controllerAsset;
            animator.applyRootMotion = false;

            var cc = root.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.height = 2f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0f, 1f, 0f);
            }

            var playerScript = root.GetComponent<ThirdPersonPlayer>();
            if (playerScript != null)
            {
                playerScript.animator = animator;
                EditorUtility.SetDirty(playerScript);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Debug.Log("Player prefab updated with Ch14 and animator wired.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void RemoveComponentIfExists<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c != null)
            Object.DestroyImmediate(c);
    }
}
#endif
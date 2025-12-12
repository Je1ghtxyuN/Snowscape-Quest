using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor; // 引入Editor命名空间，用于保存文件
#endif

[ExecuteInEditMode]
public class LevelUpEffectBuilder : MonoBehaviour
{
    [Header("素材引用")]
    [Tooltip("拖入 URP_IceShield 材质")]
    public Material iceMaterial;

    [Header("操作")]
    public bool _clickRightMouseOnTitle_ = true;

    [ContextMenu("🔥 生成并保存网格 (Prefab安全版) 🔥")]
    public void BuildEffect()
    {
        // 1. 清理
        var children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        children.ForEach(child => DestroyImmediate(child));

        // =========================================================
        // 2. 创建外壳 (正常圆柱)
        // =========================================================
        GameObject outerPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        outerPillar.name = "Visual_Pillar_Outer";
        outerPillar.transform.SetParent(transform);
        outerPillar.transform.localScale = new Vector3(4f, 0.1f, 4f);
        SetupPillar(outerPillar);

        // =========================================================
        // 3. 创建内胆 (物理翻转网格)
        // =========================================================
        GameObject innerPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerPillar.name = "Visual_Pillar_Inner";
        innerPillar.transform.SetParent(transform);

        // 使用正缩放
        innerPillar.transform.localScale = new Vector3(3.9f, 0.1f, 3.9f);
        SetupPillar(innerPillar);

        // 🔥 翻转并保存网格 🔥
        InvertAndSaveMesh(innerPillar);

        // =========================================================
        // 4. 创建粒子系统
        // =========================================================
        GameObject particleObj = new GameObject("Ice_Particles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        particleObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psRend = particleObj.GetComponent<ParticleSystemRenderer>();

        // Main
        var main = ps.main;
        main.duration = 5.0f;
        main.loop = true;
        main.startLifetime = 3.0f;
        main.startSpeed = 0f;
        main.startSize = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // 或 Local，视需求而定
        main.maxParticles = 1000;
        main.playOnAwake = false;

        // Emission
        var emission = ps.emission;
        emission.rateOverTime = 60;

        // Shape
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 2.0f;
        shape.radiusThickness = 0.1f;

        // Velocity
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.y = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);

        // Color
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.cyan, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1.0f) }
        );
        col.color = grad;

        // Size
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.2f);
        curve.AddKey(0.2f, 1.0f);
        curve.AddKey(1.0f, 0.0f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        // 材质
        if (iceMaterial != null) psRend.material = iceMaterial;
        psRend.renderMode = ParticleSystemRenderMode.Billboard;

        // =========================================================
        // 5. 挂载控制器
        // =========================================================
        LevelUpEffectController controller = GetComponent<LevelUpEffectController>();
        if (controller == null) controller = gameObject.AddComponent<LevelUpEffectController>();

        controller.outerMesh = outerPillar.transform;
        controller.innerMesh = innerPillar.transform;
        controller.iceParticles = ps;

        if (GetComponent<AudioSource>() == null) gameObject.AddComponent<AudioSource>();
        controller.audioSource = GetComponent<AudioSource>();

        Debug.Log("✨ 生成完毕！内胆网格已保存为资产文件，Prefab 将永久有效。");
    }

    void SetupPillar(GameObject obj)
    {
        obj.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        obj.transform.localRotation = Quaternion.identity;
        DestroyImmediate(obj.GetComponent<Collider>());
        Renderer rend = obj.GetComponent<Renderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (iceMaterial != null) rend.sharedMaterial = iceMaterial;
    }

    void InvertAndSaveMesh(GameObject obj)
    {
        MeshFilter filter = obj.GetComponent<MeshFilter>();
        if (filter != null)
        {
            Mesh mesh = filter.sharedMesh;
            // 复制 Mesh
            Mesh newMesh = Instantiate(mesh);
            newMesh.name = "Inverted_Cylinder";

            // 1. 翻转三角形
            int[] triangles = newMesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i];
                triangles[i] = triangles[i + 1];
                triangles[i + 1] = temp;
            }
            newMesh.triangles = triangles;

            // 2. 翻转法线
            Vector3[] normals = newMesh.normals;
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = -normals[i];
            }
            newMesh.normals = normals;

#if UNITY_EDITOR
            // 🔥🔥🔥 核心：保存到硬盘 🔥🔥🔥
            string folderPath = "Assets/GeneratedMeshes"; // 你可以在这里改路径
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            // 资产路径
            string assetPath = folderPath + "/Inverted_Cylinder.asset";

            // 如果文件已存在，先删除，防止报错或不更新
            AssetDatabase.DeleteAsset(assetPath);

            // 创建新的资产文件
            AssetDatabase.CreateAsset(newMesh, assetPath);
            AssetDatabase.SaveAssets();

            // 关键一步：重新加载刚才保存的资产，并赋值给Filter
            // 这样 Filter 引用就是硬盘上的文件，而不是内存里的 newMesh
            filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);

            Debug.Log($"<color=green>网格已保存到: {assetPath}</color>");
#else
            // 如果是在打包后的游戏里运行 (基本不会发生)，直接用内存网格
            filter.sharedMesh = newMesh;
#endif
        }
    }
}
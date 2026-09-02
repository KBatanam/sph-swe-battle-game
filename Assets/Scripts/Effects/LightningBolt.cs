using UnityEngine;

namespace Effects
{
    /// <summary>
    /// 2点間に稲妻を撃つコンポーネント。
    /// LineRendererには一直線の頂点を渡すだけにして、
    /// ジグザグの形はシェーダー(Lightning.shader)のvertex stageで作る。
    /// このスクリプトは寿命の管理だけを担当する。
    /// </summary>
    public class LightningBolt : MonoBehaviour
    {
        [Header("見た目")]
        [SerializeField] private Material boltMaterial;
        [SerializeField] private float width = 0.06f;

        [Header("形状")]
        [SerializeField] private int segments = 24;

        [Header("時間")]
        [SerializeField] private float lifetime = 0.3f;

        private static readonly int StartId = Shader.PropertyToID("_Start");
        private static readonly int EndId = Shader.PropertyToID("_End");

        private LineRenderer line;
        private MaterialPropertyBlock propertyBlock;
        private float lifeRemaining;

        private void Awake()
        {
            // LineRendererを1本、自分の子に用意する。
            var child = new GameObject("Line");
            child.transform.SetParent(transform, false);
            line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.material = boltMaterial;
            line.widthMultiplier = width;
            line.positionCount = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            propertyBlock = new MaterialPropertyBlock();

            // Fire() が呼ばれるまで更新しない。
            enabled = false;
        }

        /// <summary>startからendへ稲妻を撃つ。</summary>
        public void Fire(Vector3 start, Vector3 end)
        {
            // 一直線の頂点を並べる。形の加工はシェーダーが担当。
            var points = new Vector3[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                points[i] = Vector3.Lerp(start, end, (float)i / segments);
            }
            line.positionCount = points.Length;
            line.SetPositions(points);

            // 始点と終点をシェーダーに伝える。
            propertyBlock.SetVector(StartId, start);
            propertyBlock.SetVector(EndId, end);
            line.SetPropertyBlock(propertyBlock);

            lifeRemaining = lifetime;
            enabled = true;
        }

        private void Update()
        {
            lifeRemaining -= Time.deltaTime;

            // 寿命が尽きたら線を消して待機する。
            if (lifeRemaining <= 0f)
            {
                enabled = false;
                line.positionCount = 0;
                return;
            }

            // 残り寿命の割合でフェードする（頂点カラーのalphaで伝える）。
            var fade = lifeRemaining / lifetime;
            var color = new Color(1f, 1f, 1f, fade);
            line.startColor = color;
            line.endColor = color;
        }
    }
}

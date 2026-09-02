using UnityEngine;

namespace Effects
{
    /// <summary>
    /// 2点間にジグザグの稲妻を生成してLineRendererで描く。
    /// 一定間隔で形を作り直してちらつかせ、寿命で消える。
    /// </summary>
    public class LightningBolt : MonoBehaviour
    {
        [Header("見た目")]
        [SerializeField] private Material boltMaterial;
        [SerializeField] private float width = 0.06f;

        [Header("形状")]
        [SerializeField] private int segments = 14;
        [SerializeField] [Range(0f, 0.4f)] private float amplitude = 0.16f;

        [Header("時間")]
        [SerializeField] private float lifetime = 0.3f;
        [SerializeField] [Range(0f, 1f)] private float regenInterval = 0.045f;

        private LineRenderer line;
        private float lifeRemaining;
        private float nextRegenTime;
        private Vector3 from;
        private Vector3 to;

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

            // Fire() が呼ばれるまで更新しない。
            enabled = false;
        }

        /// <summary>startからendへ稲妻を撃つ。</summary>
        public void Fire(Vector3 start, Vector3 end)
        {
            from = start;
            to = end;
            lifeRemaining = lifetime;
            nextRegenTime = 0f;
            enabled = true;
            UpdatePoints();
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

            // 一定間隔で形を作り直す（稲妻のちらつき）。
            if (Time.time >= nextRegenTime)
            {
                nextRegenTime = Time.time + regenInterval;
                UpdatePoints();
            }

            // 残り寿命の割合でフェードする（頂点カラーのalphaで伝える）。
            var fade = lifeRemaining / lifetime;
            var color = new Color(1f, 1f, 1f, fade);
            line.startColor = color;
            line.endColor = color;
        }

        /// <summary>
        /// from-to間の折れ線を作り直す。
        /// 両端は固定して、中間をランダムにずらしてジグザグを作る。
        /// </summary>
        private void UpdatePoints()
        {
            // 線の方向と、それに垂直な2軸を用意する（ずらす方向）。
            var direction = (to - from).normalized;
            var side1 = Vector3.Cross(direction, Vector3.up);
            if (side1.sqrMagnitude < 0.001f)
            {
                // 線が真上を向いているときはCrossが退化するので別軸を使う。
                side1 = Vector3.Cross(direction, Vector3.right);
            }
            side1.Normalize();
            var side2 = Vector3.Cross(direction, side1).normalized;

            // 距離が長いほど大きく揺らす。
            var shakeScale = Vector3.Distance(from, to) * amplitude;

            var points = new Vector3[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var t = (float)i / segments;
                var point = Vector3.Lerp(from, to, t);

                // sinカーブで端ほど揺れを小さくする（両端が外れないように）。
                var envelope = Mathf.Sin(Mathf.PI * t);
                var offset1 = (Random.value * 2f - 1f) * shakeScale * envelope;
                var offset2 = (Random.value * 2f - 1f) * shakeScale * envelope;
                points[i] = point + side1 * offset1 + side2 * offset2;
            }

            line.positionCount = points.Length;
            line.SetPositions(points);
        }
    }
}

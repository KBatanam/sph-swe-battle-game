using UnityEngine;

namespace Effects
{
    /// <summary>
    /// デモ用: 一定間隔でcasterからtargetへ稲妻を撃つ。
    /// </summary>
    public class LightningDemo : MonoBehaviour
    {
        [SerializeField] private Transform caster;
        [SerializeField] private Transform target;
        [SerializeField] private LightningBolt bolt;
        [SerializeField] private float interval = 4f;

        private float nextFireTime;

        private void OnValidate()
        {
            interval = Mathf.Max(interval, 0.1f);
        }

        private void Update()
        {
            if (Time.time < nextFireTime)
            {
                return;
            }

            nextFireTime = Time.time + interval;
            bolt.Fire(caster.position, target.position);
        }
    }
}

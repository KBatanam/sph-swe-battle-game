using UnityEngine;
using UnityEngine.InputSystem;

namespace Effects
{
    /// <summary>
    /// デモ用: キーを押したらcasterからtargetへ稲妻を撃つ。
    /// </summary>
    public class LightningDemo : MonoBehaviour
    {
        [SerializeField] private Transform caster;
        [SerializeField] private Transform target;
        [SerializeField] private LightningBolt bolt;

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                bolt.Fire(caster.position, target.position);
            }
        }
    }
}

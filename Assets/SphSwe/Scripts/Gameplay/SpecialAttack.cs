using UnityEngine;

namespace SphSwe.Gameplay
{
    /// <summary>
    /// スロットから選択して使用できる必殺技の共通基底クラス。
    /// クールタイムは全必殺技で共有するため、
    /// SpecialAttackControllerが管理する。
    /// </summary>
    public abstract class SpecialAttack : MonoBehaviour
    {
        [Header("Special Attack")]

        [SerializeField]
        private string displayName = "SPECIAL ATTACK";

        /// <summary>
        /// 必殺技選択UIへ表示する名前。
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// 必殺技固有の使用処理を実行する。
        /// 実際に使用できた場合はtrueを返す。
        /// </summary>
        public abstract bool TryUse();
    }
}
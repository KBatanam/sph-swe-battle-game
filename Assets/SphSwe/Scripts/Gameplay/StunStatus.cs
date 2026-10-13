using System;
using UnityEngine;

namespace SphSwe.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StunStatus : MonoBehaviour
    {
        private float remainingStunDuration;

        public bool IsStunned => remainingStunDuration > 0f;

        public float RemainingStunDuration => remainingStunDuration;

        /// <summary>
        /// 硬直状態の開始と終了を通知する。
        /// trueで硬直開始、falseで硬直終了を表す。
        /// </summary>
        public event Action<bool> StunStateChanged;

        /// <summary>
        /// 硬直していない場合に、指定された時間だけ硬直させる。
        /// 既に硬直中の場合、新しい硬直は適用しない。
        /// </summary>
        public void ApplyStun(float stunDuration)
        {
            if (stunDuration <= 0f || IsStunned)
            {
                return;
            }

            remainingStunDuration = stunDuration;
            StunStateChanged?.Invoke(true);

#if UNITY_EDITOR
            Debug.Log(
                $"Stun applied. Duration: {stunDuration:F2} seconds.",
                this
            );
#endif
        }

        /// <summary>
        /// 硬直残り時間を権威側の値で上書きする。
        /// クライアント側ではサーバーが同期した値を受け取って適用する。
        /// </summary>
        public void SetAuthoritativeRemainingStunDuration(float remainingStun)
        {
            var wasStunned = IsStunned;
            remainingStunDuration = Mathf.Max(0f, remainingStun);
            var isStunned = IsStunned;

            if (!wasStunned && isStunned)
            {
                StunStateChanged?.Invoke(true);
            }
            else if (wasStunned && !isStunned)
            {
                StunStateChanged?.Invoke(false);
            }
        }

        private void Update()
        {
            if (!IsStunned)
            {
                return;
            }

            remainingStunDuration = Mathf.Max(0f, remainingStunDuration - Time.deltaTime);

            if (!IsStunned)
            {
                StunStateChanged?.Invoke(false);
            }
        }

        private void OnDisable()
        {
            var wasStunned = IsStunned;
            remainingStunDuration = 0f;

            if (wasStunned)
            {
                StunStateChanged?.Invoke(false);
            }
        }
    }
}

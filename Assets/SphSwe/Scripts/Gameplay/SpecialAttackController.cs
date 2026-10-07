using System;
using UnityEngine;

namespace SphSwe.Gameplay
{
    /// <summary>
    /// 必殺技スロットの選択と、全必殺技で共有するクールタイムを管理する。
    /// nullのスロットは空の必殺技として扱う。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpecialAttackController : MonoBehaviour
    {
        private const string EmptySlotDisplayName = "EMPTY";

        [Header("Slots")]

        [Tooltip("nullの要素は空の必殺技スロットとして扱う。")]
        [SerializeField]
        private SpecialAttack[] specialAttackSlots = Array.Empty<SpecialAttack>();

        [SerializeField, Min(0)]
        private int initialSelectedSlotIndex;

        [Header("Cooldown")]

        [SerializeField, Min(0f)]
        private float cooldownDuration = 5f;

        private int selectedSlotIndex;
        private float remainingCooldownDuration;

        /// <summary>
        /// 選択中のスロットが変更されたときに通知する。
        /// </summary>
        public event Action SelectedSlotChanged;

        public int SlotCount => specialAttackSlots?.Length ?? 0;

        public int SelectedSlotIndex => selectedSlotIndex;

        public SpecialAttack SelectedSpecialAttack => GetSpecialAttack(selectedSlotIndex);

        public bool IsReady => remainingCooldownDuration <= 0f;

        public float RemainingCooldownDuration => remainingCooldownDuration;

        private void Awake()
        {
            if (SlotCount == 0)
            {
                throw new InvalidOperationException("At least one Special Attack Slot is required.");
            }

            selectedSlotIndex = Mathf.Clamp(initialSelectedSlotIndex, 0, SlotCount - 1);

            // ゲーム開始直後には必殺技を使用できない仕様のため、
            // 共通クールタイムを最大値から開始する。
            remainingCooldownDuration = cooldownDuration;
        }

        private void Update()
        {
            if (IsReady)
            {
                return;
            }

            remainingCooldownDuration = Mathf.Max(0f, remainingCooldownDuration - Time.deltaTime);
        }

        /// <summary>
        /// 共通クールタイムが完了している場合に、
        /// 選択中の必殺技の使用を試みる。
        /// 実際に使用できた場合のみ共通クールタイムを開始する。
        /// </summary>
        public bool TryUseSelectedSpecialAttack()
        {
            if (!IsReady || SelectedSpecialAttack == null)
            {
                return false;
            }

            if (!SelectedSpecialAttack.TryUse())
            {
                return false;
            }

            remainingCooldownDuration = cooldownDuration;

            return true;
        }

        public void SelectPreviousSlot()
        {
            SelectSlot(selectedSlotIndex - 1);
        }

        public void SelectNextSlot()
        {
            SelectSlot(selectedSlotIndex + 1);
        }

        /// <summary>
        /// 指定位置の必殺技を返す。
        /// スロット番号は循環するため、範囲外の番号も指定できる。
        /// </summary>
        public SpecialAttack GetSpecialAttack(int slotIndex)
        {
            if (SlotCount == 0)
            {
                return null;
            }

            return specialAttackSlots[WrapSlotIndex(slotIndex)];
        }

        /// <summary>
        /// 指定位置のUI表示名を返す。
        /// 空スロットの場合はEMPTYを返す。
        /// </summary>
        public string GetDisplayName(int slotIndex)
        {
            var specialAttack = GetSpecialAttack(slotIndex);

            return specialAttack != null ? specialAttack.DisplayName : EmptySlotDisplayName;
        }

        private void SelectSlot(int slotIndex)
        {
            if (SlotCount <= 1)
            {
                return;
            }

            var nextSelectedSlotIndex = WrapSlotIndex(slotIndex);

            if (nextSelectedSlotIndex == selectedSlotIndex)
            {
                return;
            }

            selectedSlotIndex = nextSelectedSlotIndex;
            SelectedSlotChanged?.Invoke();
        }

        private int WrapSlotIndex(int slotIndex)
        {
            return (slotIndex % SlotCount + SlotCount) % SlotCount;
        }

        private void OnValidate()
        {
            initialSelectedSlotIndex = Mathf.Max(0, initialSelectedSlotIndex);
            cooldownDuration = Mathf.Max(0f, cooldownDuration);
        }
    }
}
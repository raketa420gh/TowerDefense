using System;
using UnityEngine;

namespace MagicStaff.Staff
{
    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public float value;
    }

    [CreateAssetMenu(fileName = "StaffPart", menuName = "MagicStaff/Staff Part")]
    public class StaffPartConfig : ScriptableObject
    {
        [SerializeField]
        string _partName;
        [SerializeField]
        StaffSlot _slot;
        [SerializeField]
        Sprite _icon;
        [SerializeField]
        StatModifier[] _modifiers;
        [SerializeField]
        string _description;

        public string partName     => _partName;
        public StaffSlot slot      => _slot;
        public Sprite icon         => _icon;
        public StatModifier[] modifiers => _modifiers;
        public string description  => _description;
    }
}

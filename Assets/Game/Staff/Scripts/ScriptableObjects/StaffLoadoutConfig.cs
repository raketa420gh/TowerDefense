using UnityEngine;

namespace MagicStaff.Staff
{
    [CreateAssetMenu(fileName = "StaffLoadout", menuName = "MagicStaff/Staff Loadout")]
    public class StaffLoadoutConfig : ScriptableObject
    {
        [SerializeField]
        StaffPartConfig _artifact;
        [SerializeField]
        StaffPartConfig _topCap;
        [SerializeField]
        StaffPartConfig _grip;
        [SerializeField]
        StaffPartConfig _shaft;
        [SerializeField]
        StaffPartConfig _bottomCap;

        public StaffPartConfig artifact  => _artifact;
        public StaffPartConfig topCap    => _topCap;
        public StaffPartConfig grip      => _grip;
        public StaffPartConfig shaft     => _shaft;
        public StaffPartConfig bottomCap => _bottomCap;

        public StaffPartConfig GetPart(StaffSlot slot) => slot switch
        {
            StaffSlot.Artifact   => _artifact,
            StaffSlot.TopCap     => _topCap,
            StaffSlot.Grip       => _grip,
            StaffSlot.Shaft      => _shaft,
            StaffSlot.BottomCap  => _bottomCap,
            _                    => null
        };

        public void SetPart(StaffSlot slot, StaffPartConfig part)
        {
            switch (slot)
            {
                case StaffSlot.Artifact:  _artifact  = part; break;
                case StaffSlot.TopCap:    _topCap    = part; break;
                case StaffSlot.Grip:      _grip      = part; break;
                case StaffSlot.Shaft:     _shaft     = part; break;
                case StaffSlot.BottomCap: _bottomCap = part; break;
            }
        }
    }
}

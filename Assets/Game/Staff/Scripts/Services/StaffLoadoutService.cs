using System;
using UnityEngine;

namespace MagicStaff.Staff
{
    public class StaffLoadoutService
    {
        StaffLoadoutConfig _loadout;

        public StaffLoadoutConfig ActiveLoadout => _loadout;

        public StaffLoadoutService()
        {
            LoadOrDefault();
        }

        public void SetPart(StaffSlot slot, StaffPartConfig part)
        {
            _loadout.SetPart(slot, part);
            PlayerPrefs.SetString(PrefKey(slot), part.name);
            PlayerPrefs.Save();
        }

        void LoadOrDefault()
        {
            var defaultAsset = Resources.Load<StaffLoadoutConfig>("DefaultLoadout");
            _loadout = UnityEngine.Object.Instantiate(defaultAsset);

            foreach (StaffSlot slot in Enum.GetValues(typeof(StaffSlot)))
            {
                var savedName = PlayerPrefs.GetString(PrefKey(slot), string.Empty);
                if (string.IsNullOrEmpty(savedName)) continue;

                var part = Resources.Load<StaffPartConfig>($"StaffParts/{savedName}");
                if (part != null)
                    _loadout.SetPart(slot, part);
            }
        }

        static string PrefKey(StaffSlot slot) => $"Staff_{slot}";
    }
}

using MagicStaff.Staff;
using UnityEngine;
using Zenject;

public class StaffVisualBuilder : MonoBehaviour
{
    [SerializeField]
    Renderer _artifact;
    [SerializeField]
    Renderer _topCap;
    [SerializeField]
    Renderer _grip;
    [SerializeField]
    Renderer _shaft;
    [SerializeField]
    Renderer _bottomCap;

    [Inject]
    StaffLoadoutService _loadout;

    void Start() => ApplyLoadout();

    void ApplyLoadout()
    {
        // Минимум: сборка работает, визуал виден
        // Расширяется в PLAN_Gameplay_Combat: маппинг слотов → цвет материала
    }
}

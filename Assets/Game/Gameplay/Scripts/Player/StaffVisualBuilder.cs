using MagicStaff.Staff;
using UnityEngine;
using Zenject;

public class StaffVisualBuilder : MonoBehaviour
{
    [SerializeField]
    private Renderer _artifact;
    [SerializeField]
    private Renderer _topCap;
    [SerializeField]
    private Renderer _grip;
    [SerializeField]
    private Renderer _shaft;
    [SerializeField]
    private Renderer _bottomCap;

    [Inject]
    private StaffLoadoutService _loadout;

    private void Start() => ApplyLoadout();

    private void ApplyLoadout()
    {
        // Минимум: сборка работает, визуал виден
        // Расширяется в PLAN_Gameplay_Combat: маппинг слотов → цвет материала
    }
}

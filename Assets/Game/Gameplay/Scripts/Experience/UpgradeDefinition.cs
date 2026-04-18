using System;
using MagicStaff.Staff;
using UnityEngine;

[Serializable]
public sealed class UpgradeDefinition
{
    public string   id;
    public string   title;
    public string   description;
    public Sprite   icon;
    public StatType stat;
    public float    value;
    public bool     isPercent;
}

using System.Collections.Generic;
using MagicStaff.Views;
using UnityEngine;

public class ActivePassivesView : DisplayableView
{
    [SerializeField]
    private Transform       _listRoot;
    [SerializeField]
    private PassiveItemView _itemPrefab;

    public void Render(IReadOnlyList<IActivePassive> passives)
    {
        foreach (Transform child in _listRoot)
            Destroy(child.gameObject);

        foreach (var p in passives)
        {
            var item = Instantiate(_itemPrefab, _listRoot);
            item.Render(p);
        }

        if (passives.Count > 0) Show(); else Hide();
    }
}

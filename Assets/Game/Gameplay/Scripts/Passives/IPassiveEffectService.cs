using System;
using System.Collections.Generic;

public interface IPassiveEffectService
{
    IReadOnlyList<IActivePassive> ActivePassives { get; }
    event Action OnPassivesChanged;
}

using System.Collections;
using UnityEngine;

public class CoroutineRunner : MonoBehaviour, ICoroutineRunner
{
    public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);

    public void Stop(Coroutine routine)
    {
        if (routine != null && this) StopCoroutine(routine);
    }
}

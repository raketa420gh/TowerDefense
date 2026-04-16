using UnityEngine;

namespace MagicStaff.Views
{
    public abstract class DisplayableView : MonoBehaviour
    {
        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);
    }
}

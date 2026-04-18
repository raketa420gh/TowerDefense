using UnityEngine.SceneManagement;

namespace MagicStaff
{
    public class SceneLoader : ISceneLoader
    {
        public void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    }
}

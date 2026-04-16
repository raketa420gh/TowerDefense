using UnityEngine.SceneManagement;

namespace MagicStaff.MainMenu
{
    public class SceneLoader : ISceneLoader
    {
        public void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    }
}

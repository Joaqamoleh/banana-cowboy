using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LevelSwitch : MonoBehaviour
{
    public string menuScene;
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject.tag == "Player" && !menuScene.IsUnityNull())
        {
            ChangeScene(menuScene);
        }
    }

    public static void ChangeScene(string scene)
    {
        if (scene == "Menu")
        {
            if (SoundManager.Instance() != null)
            {
                SoundManager.Instance().StopAllMusic();
                SoundManager.Instance().PlayMusic("Main Menu");
            }
        }
        else if (scene == "Orange Level")
        {
            if (SoundManager.Instance() != null)
            {
                SoundManager.Instance().StopAllMusic();
                SoundManager.Instance().PlayMusic("Orange Planet");
            }
        }
        else if (scene == "Orange Boss Scene")
        {
            LevelData.SetCheckpoint(0);
            LevelData.BeatLevel(); // save all your SS from last checkpoint
            print(LevelData.starSparkleTotal);
            if (SoundManager.Instance() != null)
            {
                SoundManager.Instance().StopAllMusic();
                SoundManager.Instance().PlayMusic("Orange Boss");
            }
        }
        else if (scene == "Tutorial Level")
        {
            SoundManager.Instance().StopAllMusic();
            SoundManager.Instance().PlayMusic("Tutorial");
        }
        if (scene == "Menu")
        {
            PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.UI);
        }
        else
        {
            PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.LASSO_AIM);
        }
        LevelData.ResetLevelData();
        CutsceneManager.ResetPlayStartingCutscene();
        SoundManager.Instance().StopAllSFX();
        SceneManager.LoadScene(scene);
    }
}

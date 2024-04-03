using System.Collections;
using System.Collections.Generic;
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

    private static Dictionary<string, string> levelToMusic = new Dictionary<string, string>() { 
        { "Menu", "Main Menu" }, 
        { "Level Select", "Main Menu" },
        { "Orange Level", "Orange Planet"},
        { "Orange Boss Scene", "Orange Boss" },
        { "Tutorial Level", "Tutorial" },
        { "Blender Boss Room", "Blender Boss" },
        { "Cutscene1", "Main Menu" },
        { "Cutscene2", "Main Menu" },
        { "Blueberry Level", "Blueberry Planet" }
    };

    public static void ChangeScene(string scene)
    {
        if (scene == "Orange Boss Scene")
        {
            LevelData.SetCheckpoint(0);
            LevelData.BeatLevel(); // save all your SS from last checkpoint
            //print(LevelData.starSparkleTotal);
        }

        if (SoundManager.Instance() != null)
        {
            SoundManager.Instance().PlayMusic(levelToMusic[scene]);
            SoundManager.Instance().StopAllSFX();
        }

        LevelData.ResetLevelData();
        CutsceneManager.ResetPlayStartingCutscene();
        SceneManager.LoadScene(scene);
        if (scene == "Menu" || scene == "Level Select" || scene == "Cutscene1" || scene == "Cutscene2")
        {
            PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.UI);
        }
        else
        {
            PlayerCursor.SetActiveCursorType(PlayerCursor.CursorType.LASSO_AIM);
        }
    }
}

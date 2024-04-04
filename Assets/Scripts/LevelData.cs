using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelData : MonoBehaviour
{

    public GameObject[] checkpoints = null;
    public static LevelData instance;

    public static int starSparkleTotal = 0;
    public static int starSparkleCheckpoint = 0;
    public static int starSparkleTemp = 0;

    public static Dictionary<Vector3, bool> starSparkleObjectTemp = new Dictionary<Vector3, bool>();
    public static Dictionary<Vector3, bool> starSparkleObjectCheckpoint = new Dictionary<Vector3, bool>();


    // set these manually
    /*    private static Vector3[] OrangeRespawnArray = new[] {
            new Vector3(-40.4f, 26.1f, 38.65f),
            new Vector3(144.26f, 66.95f, 42.81f)
        };

        private static Vector3[] TutorialRespawnArray = new[]
        {
            new Vector3(0,3,0),
            new Vector3(38.2f,37.6f,32.7f)
        };*/

    private static int checkpointReached = 0; // stores latest checkpoint reached

    private void Start()
    {
        instance = this;
    }

    // resets temporary data. Do this when loading into a level or leaving a level.
    public static void ResetLevelData()
    {
        checkpointReached = 0;

        starSparkleTemp = 0;
        starSparkleCheckpoint = 0;
        starSparkleObjectTemp.Clear();
        starSparkleObjectCheckpoint.Clear();

        UIManager.UpdateStars();
    }

    public static void ResetCheckpointData()
    {
        starSparkleTemp = 0;
        starSparkleObjectTemp.Clear();
        foreach (var temp in starSparkleObjectCheckpoint)
        {
            starSparkleObjectTemp.Add(temp.Key, temp.Value);
        }
        UIManager.UpdateStars();

    }

    public static void BeatLevel()
    {
        starSparkleTotal += starSparkleCheckpoint;
        ResetLevelData();
    }

    // get respawn position for player based on last checkpoint reached.
    public static Vector3 GetRespawnPos()
    {
        Debug.Log("GIVING RESPAWN POSITION TO PLAYER CONTROLLER: "+ checkpointReached);
        SoundManager.Instance().StopAllSFX();
        return instance.checkpoints[checkpointReached].transform.position;
    }

    public static void SetCheckpoint(int c)
    {
        starSparkleCheckpoint += starSparkleTemp;
        starSparkleTemp = 0;
        GameObject.Find("Player Prefab").GetComponent<Health>().Damage(-3, Vector3.zero);
        foreach (var temp in starSparkleObjectTemp)
        {
            if (!starSparkleObjectCheckpoint.ContainsKey(temp.Key))
            {
                starSparkleObjectCheckpoint.Add(temp.Key, temp.Value);
            }
            else
            {
                starSparkleObjectCheckpoint[temp.Key] = temp.Value;
            }
        }
        /* prevent player from setting themselves back
         * by only storing if they've found a "greater"
         * checkpoint. */
        if (c > checkpointReached)
            checkpointReached = c;
    }
}
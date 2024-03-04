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

    public static Dictionary<Vector3, bool> starSparkleObjectCheckpoint = new Dictionary<Vector3, bool>();
    public static Dictionary<Vector3, bool> starSparkleObject= new Dictionary<Vector3, bool>();


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
        starSparkleObjectCheckpoint.Clear();
        starSparkleObject.Clear();
        UIManager.UpdateStars();
    }

    public static void ResetCheckpointData()
    {
        starSparkleTemp = 0;
        UIManager.UpdateStars();
        starSparkleObjectCheckpoint.Clear();
        foreach (var item in starSparkleObject)
        {
            starSparkleObjectCheckpoint.Add(item.Key, item.Value);
        }
    }

    public static void BeatLevel()
    {
        starSparkleTotal += starSparkleCheckpoint;
        starSparkleObjectCheckpoint.Clear();
        starSparkleObject.Clear();
        UIManager.UpdateStars();
    }

    // get respawn position for player based on last checkpoint reached.
    // TODO - works for the base level rn, expand to other levels as they are made
    public static Vector3 GetRespawnPos()
    {
        Debug.Log("GIVING RESPAWN POSITION TO PLAYER CONTROLLER: "+ checkpointReached);
        return instance.checkpoints[checkpointReached].transform.position;
    }

    public static void SetCheckpoint(int c)
    {
        starSparkleCheckpoint = starSparkleTemp;
        foreach (var item in starSparkleObjectCheckpoint)
        {
            starSparkleObject.Add(item.Key, item.Value);
        }
        starSparkleTemp = 0;
        /* prevent player from setting themselves back
         * by only storing if they've found a "greater"
         * checkpoint. */
        if (c > checkpointReached)
            checkpointReached = c;
    }
}
using System.Collections;
using UnityEngine;

public class BlueberryBombTrigger : MonoBehaviour
{
    public GameObject bombIndicator;
    public GameObject bombObject;
    public float time;

    private bool isBombDropped = false;
    private GameObject indicatorInstance;

    private void OnTriggerEnter(Collider other)
    {
        if (!isBombDropped && other.CompareTag("Player"))
        {
            StartCoroutine(DropBomb());
        }
    }

    IEnumerator DropBomb()
    {
        isBombDropped = true;

        // Object pooling
        if (indicatorInstance == null)
            indicatorInstance = Instantiate(bombIndicator, transform.position, Quaternion.identity);
        else
            indicatorInstance.SetActive(true);

        yield return new WaitForSeconds(time);

        if (indicatorInstance != null)
            indicatorInstance.SetActive(false);

        Instantiate(bombObject, new Vector3(transform.position.x, bombObject.transform.position.y, transform.position.z), Quaternion.identity);
        isBombDropped = false;
    }
}

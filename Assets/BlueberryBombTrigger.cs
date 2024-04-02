using System.Collections;
using UnityEngine;

public class BlueberryBombTrigger : MonoBehaviour
{
    public GameObject bombIndicator;
    public GameObject bombObject;
    public float time;

    private bool isBombDropped = false;
    private GameObject indicatorInstance;
    private GameObject bombObjectInstance;

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

        indicatorInstance = Instantiate(bombIndicator, transform.position, Quaternion.identity);
        indicatorInstance.transform.localScale = transform.localScale * 0.4f;

        yield return new WaitForSeconds(time);

        Destroy(indicatorInstance);

        bombObjectInstance = Instantiate(bombObject, new Vector3(transform.position.x, bombObject.transform.position.y, transform.position.z), Quaternion.identity);
        bombObjectInstance.transform.localScale = transform.localScale * 2f;
        isBombDropped = false;
    }
}

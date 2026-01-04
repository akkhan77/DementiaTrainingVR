using UnityEngine;
using System.Collections;

public class DelayDisable : MonoBehaviour
{
    [SerializeField] private float _delay = 2f;
    IEnumerator Start()
    {
        yield return new WaitForSeconds(_delay);
        gameObject.SetActive(false);
    }
}

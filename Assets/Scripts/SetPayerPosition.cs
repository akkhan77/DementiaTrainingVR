using UnityEngine;
using System.Collections;

public class SetPayerPosition : MonoBehaviour
{
    [SerializeField] private Vector3 _playerPosition;
    [SerializeField] private Vector3 _playerRotation;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        gameObject.transform.position = _playerPosition;
        gameObject.transform.rotation = Quaternion.Euler(_playerRotation);
    }
}

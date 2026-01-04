using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Outline))]
public class OutlineBlinker : MonoBehaviour
{
    private Outline _outline;
    private bool _isBlinking = false;
    private static WaitForSeconds _waitForSeconds0_4 = new(0.4f);

    private void OnEnable()
    {
        _outline = GetComponent<Outline>();
        _outline.enabled = true;
        _isBlinking = true;
        StartCoroutine(Blink());
    }

    private IEnumerator Blink()
    {
        while (_isBlinking)
        {
            _outline.OutlineWidth = 4.5f;
            yield return _waitForSeconds0_4;
            _outline.OutlineWidth = 0f;
            yield return _waitForSeconds0_4;
        }
    }

    void OnDisable()
    {
        _isBlinking = false;
        _outline.enabled = false;
        StopCoroutine(Blink());
    }
}

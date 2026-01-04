using UnityEngine;

public class GuideCharacter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource _instructionAudioSource;
    [SerializeField] private SkinnedMeshRenderer _faceRenderer;
    [SerializeField] private Animator _animator;

    [Header("Blend Shape")]
    [SerializeField] private int _mouthBlendShapeIndex = 0;
   

    [Header("Talking Settings")]
    [SerializeField] private float _talkSpeed = 4f;
    [SerializeField] private float _maxOpen = 1f;
    [SerializeField] private float _smooth = 10f;

    [Header("Slide Settings")]
    [SerializeField] private int _slideIndexToAnimate = 3; // Slide 4 (0-based)
    [SerializeField] private int _slideIndexToReset = 4; // Slide 5 (0-based)

    private float _currentValue;

    private void Update()
    {
        float targetValue = 0f;

        if (_instructionAudioSource.isPlaying)
        {
            targetValue = Mathf.PingPong(Time.time * _talkSpeed, _maxOpen);

            _currentValue = Mathf.Lerp(
                _currentValue,
                targetValue,
                Time.deltaTime * _smooth
            );

            _faceRenderer.SetBlendShapeWeight(
                _mouthBlendShapeIndex,
                Mathf.Clamp01(_currentValue)
            );
        }
        else
        {
            return;
        }
    }

    // THIS IS CALLED BY PreBriefing EVENT
    public void On3rdSlideOpened(int slideIndex)
    {
        if (slideIndex == _slideIndexToAnimate)
        {
            PlaySlide4Animation();
        }
        else if (slideIndex == _slideIndexToReset)
        {
            if (_animator == null)
                return;

            _animator.SetBool("Talk", false);
        }
    }

    private void PlaySlide4Animation()
    {
        if (_animator == null)
            return;

        _animator.SetBool("Talk", true);
    }
}
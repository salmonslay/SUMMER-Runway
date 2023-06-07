using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    private Pose[] _poses;
    private Pose _currentPose;
    private int _poseIndex = 0;

    private Character[] _players;
    private AudioSource _audioSource;

    [SerializeField] private Image _frame;

    [Header("Audio")] [SerializeField] private AudioClip _correctSound;

    // Start is called before the first frame update
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = _correctSound;

        _poses = Resources.LoadAll<Pose>("Positions");
        Debug.Log($"Loaded {_poses.Length} poses.");

        // scramble the poses
        for (int i = 0; i < _poses.Length; i++)
        {
            Pose temp = _poses[i];
            int randomIndex = Random.Range(i, _poses.Length);
            _poses[i] = _poses[randomIndex];
            _poses[randomIndex] = temp;
        }

        _players = FindObjectsOfType<Character>();
        SetNextPose();
    }

    public bool CheckCombination(Character character, int combination)
    {
        if (_currentPose == null || _currentPose.Code != combination)
            return false;

        Score(character);

        return true;
    }

    private void Score(Character character)
    {
        character.Score++;
        Debug.Log($"{character.name} scored! Score: {character.Score}");

        _audioSource.time = 0;
        _audioSource.Play();

        SetNextPose();
    }

    private void SetNextPose()
    {
        _currentPose = _poses[_poseIndex];
        _poseIndex = (_poseIndex + 1) % _poses.Length;

        _frame.sprite = _currentPose.Reference;
    }
}
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

    [SerializeField] private GameObject _parentUI;
    [SerializeField] private GameObject _parentGame;
    [SerializeField] private GameObject _parentIntro;
    
    public bool PlayingAnimation { get; set; } = false;
    
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

        if (!Application.isEditor)
        {
            _parentGame.SetActive(false);
            _parentUI.SetActive(false);
            _parentIntro.SetActive(true);
        }
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

        StartCoroutine(character.PlayCameraFlashes());
    }

    public void SetNextPose()
    {
        _currentPose = _poses[_poseIndex];
        _poseIndex = (_poseIndex + 1) % _poses.Length;

        _frame.sprite = _currentPose.Reference;
    }

    public void StartGame()
    {
        _parentGame.SetActive(true);
        _parentUI.SetActive(true);
        _parentIntro.SetActive(false);
    }
}
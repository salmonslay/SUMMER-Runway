using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// A character is a collection of 4 parts that can be switched out. Each part has X number of sprites that can be switched between.
/// I've made everything public instead of serializing it because I'm lazy and this is just a demo.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class Character : MonoBehaviour
{
    [Header("Controls")] public KeyCode SwitchA;
    public KeyCode SwitchB;
    public KeyCode SwitchC;
    public KeyCode SwitchD;
    public KeyCode SwitchCamera;

    [Tooltip("The joystick number is 1-based")]
    public int JoystickNum = 1;

    [Header("Character parts")] public Sprite[] PartA;
    public Sprite[] PartB;
    public Sprite[] PartC;
    public Sprite[] PartD;
    public Image ImageA;
    public Image ImageB;
    public Image ImageC;
    public Image ImageD;

    [Header("Particle systems")] public ParticleSystem ParticleLike;
    public ParticleSystem ParticleLove;
    public ParticleSystem ParticleWow;
    public ParticleSystem ParticleHaha;
    public ParticleSystem ParticleSad;
    public ParticleSystem ParticleAngry;

    private readonly int[] _indexes = {0, 0, 0, 0};

    [SerializeField] private AudioClip _switchSound;
    [SerializeField] private AudioClip[] _cameraFlashSounds;
    private AudioSource _audioSource;
    private GameManager _gameManager;
    [SerializeField] private Text _scoreText;
    [SerializeField] private GameObject _polaroidPrefab;
    [SerializeField] private AudioClip _powerfulCamera;

    private float _followerMultiplier = 0.1f;
    private float _secondsBetweenFollowerUpdates = 0.1f;

    private int _changeIndex = -1;


    public int Combination => _indexes[0] * 1000 + _indexes[1] * 100 + _indexes[2] * 10 + _indexes[3]; // will make a number like 1234

    public int Score { get; set; } = 0;

    private Sprite[][] _parts;

    public Sprite[][] Parts => _parts ??= new[] {PartA, PartB, PartC, PartD}; // lazy load this

    private void Start()
    {
        UpdatePositions(); // default will always be 0000

        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = _switchSound;
        _gameManager = FindObjectOfType<GameManager>();

        if (!name.Contains("clone"))
        {
            // this is the original character, start all particles
            ParticleLike.Play();
            ParticleLove.Play();
            ParticleWow.Play();
            ParticleHaha.Play();
            ParticleSad.Play();
            ParticleAngry.Play();
        }
    }

    private void Update()
    {
        // update follower count
        if (Time.time > _secondsBetweenFollowerUpdates)
        {
            _secondsBetweenFollowerUpdates = Time.time + 0.1f;
            _scoreText.text = $"Followers: {Score}";

            Score += Mathf.RoundToInt(Random.Range(0, 10) * _followerMultiplier);
        }

        int previousIndex = _changeIndex;
        // if keyboard W, if controller Y or if controller stick up
        if (Input.GetKeyDown(SwitchA) || Input.GetKeyDown($"joystick {JoystickNum} button 3") || Input.GetAxis($"Vertical{JoystickNum}") > 0.5f)
            _changeIndex = 0;
        else if (Input.GetKeyDown(SwitchB) || Input.GetKeyDown($"joystick {JoystickNum} button 2") || Input.GetAxis($"Horizontal{JoystickNum}") < -0.5f)
            _changeIndex = 1;
        else if (Input.GetKeyDown(SwitchC) || Input.GetKeyDown($"joystick {JoystickNum} button 1") || Input.GetAxis($"Horizontal{JoystickNum}") > 0.5f)
            _changeIndex = 2;
        else if (Input.GetKeyDown(SwitchD) || Input.GetKeyDown($"joystick {JoystickNum} button 0") || Input.GetAxis($"Vertical{JoystickNum}") < -0.5f)
            _changeIndex = 3;
        else if (Input.GetKeyDown(SwitchCamera) && Application.isEditor)
            StartCoroutine(PlayCameraFlashes());
        else
        {
            _changeIndex = -1;
            return;
        }

        if (!_gameManager || _gameManager.PlayingAnimation)
            return;

        if (_changeIndex == previousIndex) // we need a -1 before we can change again
            return;

        _indexes[_changeIndex] = (_indexes[_changeIndex] + 1) % Parts[_changeIndex].Length;

        _audioSource.time = 0;
        _audioSource.Play();

        UpdatePositions();

        if (_gameManager != null)
            _gameManager.CheckCombination(this, Combination);

        Debug.Log($"{name}: {Combination}");
    }

    private void UpdatePositions()
    {
        ImageA.sprite = PartA[_indexes[0]];
        ImageB.sprite = PartB[_indexes[1]];
        ImageC.sprite = PartC[_indexes[2]];
        ImageD.sprite = PartD[_indexes[3]];
    }

    public IEnumerator PlayCameraFlashes()
    {
        _gameManager.PlayingAnimation = true;

        CreatePolaroid();

        StartCoroutine(PositiveReactions());

        for (int i = 0; i < 10; i++)
        {
            GameObject flash = PlayCameraFlash();
            Destroy(flash, Random.Range(0.2f, 0.3f));

            // create a new temporary audio source
            AudioSource audioSource = flash.AddComponent<AudioSource>();
            audioSource.clip = _cameraFlashSounds[Random.Range(0, _cameraFlashSounds.Length)];
            audioSource.Play();
            Destroy(audioSource, audioSource.clip.length);
            yield return new WaitForSeconds(Random.Range(0.06f, 0.2f));
        }

        _gameManager.SetNextPose();

        _gameManager.PlayingAnimation = false;
    }

    public GameObject PlayCameraFlash()
    {
        GameObject flash = CreateClone();
        flash.name = $"{name} (flash)";

        // move it behind the original
        flash.transform.SetSiblingIndex(0);

        flash.transform.localScale = Vector3.one;

        // make all images black
        foreach (Image image in flash.GetComponentsInChildren<Image>())
        {
            image.color = new Color(0, 0, 0, 0.4f);
        }

        // move X & Y around by a few units each
        Vector3 forward = new Vector2(Random.Range(-50, 50), Random.Range(2, 20));
        flash.transform.localPosition += forward;

        return flash;
    }

    public void CreatePolaroid()
    {
        GameObject polaroid = Instantiate(_polaroidPrefab, GameObject.Find("Canvas/UI").transform, false);
        GameObject clone = CreateClone();
        GameObject polaroidCharacter = polaroid.transform.Find("Character").gameObject;
        clone.transform.SetParent(polaroidCharacter.transform);
        clone.transform.localPosition = polaroidCharacter.transform.localPosition;
        clone.transform.localRotation = polaroidCharacter.transform.localRotation;
        clone.transform.localScale = Vector3.one;

        AudioSource audioSource = polaroid.AddComponent<AudioSource>();
        audioSource.clip = _powerfulCamera;
        audioSource.Play();
    }

    public GameObject CreateClone()
    {
        Transform t = transform;
        GameObject clone = Instantiate(gameObject, t.position, t.rotation);
        clone.name = name + " (clone)";

        Destroy(clone.GetComponent<Character>());

        clone.transform.SetParent(t.parent);
        return clone;
    }

    public IEnumerator PositiveReactions()
    {
        float originalFollowerMultiplier = _followerMultiplier;
        _followerMultiplier = 1f;

        for (int i = 0; i < Random.Range(15, 20); i++)
        {
            ParticleLike.Emit(Random.Range(1, 3));
            ParticleLove.Emit(Random.Range(1, 3));
            ParticleWow.Emit(Random.Range(1, 2));
            ParticleHaha.Emit(1);

            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
        }

        _followerMultiplier = originalFollowerMultiplier;
    }

    public IEnumerator NegativeReactions()
    {
        float originalFollowerMultiplier = _followerMultiplier;
        _followerMultiplier = -0.1f;

        for (int i = 0; i < Random.Range(15, 20); i++)
        {
            ParticleSad.Emit(Random.Range(1, 3));
            ParticleAngry.Emit(Random.Range(1, 3));

            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
        }

        _followerMultiplier = originalFollowerMultiplier;
    }
}
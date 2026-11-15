using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum GameState
{
    Initialize,
    AwaitStart,
    GenerateKnockSequence,
    WaitingGameplayInput
}

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource knockSound;

    [SerializeField]
    private AudioSource doorOpenSound;

    [SerializeField]
    private UnityEngine.UI.Button hearKnockButton;

    [SerializeField]
    private UnityEngine.UI.Button knockButton;

    private GameState gameState;

    [SerializeField]
    private GameObject[] doors = new GameObject[5];

    [SerializeField]
    private TMP_Text winText;

    private List<float> knockSequence = new List<float>();
    private int currentKnockLength = 2;
    private float knockStartTime = 0f;
    private List<float> knockElapsedTimes = new List<float>();
    private bool knockInProgress = false;

    private List<float> knockLengths = new List<float> { 0.5f, 1.0f };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameState = GameState.Initialize;
    }

    private void Initialize()
    {
        gameState = GameState.AwaitStart;
        currentKnockLength = 2;
        knockSequence.Clear();
        knockElapsedTimes.Clear();
        hearKnockButton.onClick.AddListener(OnHearKnockButtonClicked);
        knockButton.onClick.AddListener(OnKnockButtonClicked);
        InitializeDoor();
        gameState = GameState.GenerateKnockSequence;
    }

    // Update is called once per frame
    void Update()
    {
        switch(gameState)
        {
            case GameState.Initialize:
                Initialize();
                break;
            case GameState.AwaitStart:                
                break;
            case GameState.GenerateKnockSequence:
                GenerateKnockSequence();
                break;
            case GameState.WaitingGameplayInput:
                break;
        }  
    }

    private void GenerateKnockSequence()
    {
        knockSequence.Clear();
        for(int i = 0; i < currentKnockLength - 1; ++i)
        {
            knockSequence.Add(knockLengths[UnityEngine.Random.Range(0, knockLengths.Count)]);
        }

        gameState = GameState.WaitingGameplayInput;
    }

    private void OnHearKnockButtonClicked()
    {
        knockSound.Play();        
        StartCoroutine(PlayKnockSequence());
    }

    private void OnKnockButtonClicked()
    {
        knockSound.Play();

        if (!knockInProgress)
        {
            hearKnockButton.gameObject.SetActive(false);
            knockStartTime = Time.time;
            knockInProgress = true;
        }
        else
        {
            var previousKnockTime = knockStartTime;
            knockStartTime = Time.time;
            var knockElapsedTime = knockStartTime - previousKnockTime;

            knockElapsedTimes.Add(knockElapsedTime);

            if (knockElapsedTimes.Count == knockSequence.Count)
            {
                hearKnockButton.gameObject.SetActive(true);
                CompareSequences();
                knockElapsedTimes.Clear();
                knockInProgress = false;
            }            
        }
    }

    private void CompareSequences()
    {
        var knockMatch = true;
        for (int i = 0; i < knockSequence.Count; ++i)
        {
            knockMatch = CheckKnockMatch(knockElapsedTimes[i], knockSequence[i]);
            if (!knockMatch)
            {
                break;
            }
        }

        if (knockMatch)
        {
            if (currentKnockLength == 6)
            {
                winText.gameObject.SetActive(true);
                hearKnockButton.gameObject.SetActive(false);
                knockButton.gameObject.SetActive(false);
                foreach(var door in doors)
                {
                    door.gameObject.SetActive(false);
                }
            }
            else
            {
                doorOpenSound.Play(); 
                StartCoroutine(WaitForDoorAnimation());
                ++currentKnockLength;               
            }
        }
    }

    private System.Collections.IEnumerator WaitForDoorAnimation()
    {
        var currentDoor = doors[currentKnockLength - 2];
        var animator = currentDoor.GetComponent<Animator>();
        animator.Play("DoorOpen");
        yield return null;

        while (true)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);

            if (state.IsName("DoorOpen") && state.normalizedTime >= 1.25f)
            {
                break;
            }

            yield return null;
        }
        
        InitializeDoor();
        GenerateKnockSequence();
    }

    private void InitializeDoor()
    {
        foreach(var door in doors)
        {
            door.gameObject.SetActive(false);
        }

        doors[currentKnockLength - 2].gameObject.SetActive(true);
    }

    private bool CheckKnockMatch(float knockElapsedTime, float knockExpectedTime) =>
        knockExpectedTime == 0.5f && knockElapsedTime < 0.75f ||
            knockExpectedTime == 1.0f && knockElapsedTime > 0.75f;

    private System.Collections.IEnumerator PlayKnockSequence()
    {        
        knockButton.gameObject.SetActive(false);
        hearKnockButton.gameObject.SetActive(false);
        foreach (float knockPause in knockSequence)
        {
            yield return new WaitForSeconds(knockPause);
            knockSound.Play();
        }

        knockButton.gameObject.SetActive(true);
        hearKnockButton.gameObject.SetActive(true);
    }
}

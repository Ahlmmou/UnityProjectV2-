using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PickAndPlaceGamemode : MonoBehaviour
{
    [Header("Refs")]
    public PickPlaceTaskManager task;

    [Header("VFX")]
    public ParticleSystem[] successVfxAtBins;   // ? drag your confetti particles here

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text timerText;
    public TMP_Text goalText;
    public TMP_Text comboText;   // optional
    public Image timerBar;

    [Header("Game Settings")]
    public int goalScore = 3;        // number of blocks
    public float timeLimit = 180f;   // 3 minutes
    public int comboEvery = 3;

    float timeLeft;
    bool running;
    int lastScore;
    int combo;

    void Start()
    {
        running = false;
        timeLeft = 0f;
        lastScore = 0;
        combo = 0;
        UpdateHUD();

        // Uncomment if you want automatic start:
        // StartRound();
    }

    void Update()
    {
        // Start or restart with B
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.B))
            StartRound();

        if (!running || task == null)
            return;

        // Timer countdown
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            running = false;
        }

        // Score increased?
        if (task.score > lastScore)
        {
            int delta = task.score - lastScore;
            lastScore = task.score;
            combo += delta;

            // ?? Play confetti for EXACTLY 1 second
            PlayConfetti();

            // Goal reached?
            if (task.score >= goalScore)
                running = false;
        }

        UpdateHUD();
    }

    // ----------------------------------------------------------------------
    // ? CONFETTI SYSTEM ?
    // ----------------------------------------------------------------------

    void PlayConfetti()
    {
        foreach (var ps in successVfxAtBins)
        {
            if (ps != null)
                StartCoroutine(PlayConfettiForOneSecond(ps));
        }
    }

    IEnumerator PlayConfettiForOneSecond(ParticleSystem ps)
    {
        ps.Play();
        yield return new WaitForSeconds(1f);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // ----------------------------------------------------------------------
    // ? ROUND CONTROL ?
    // ----------------------------------------------------------------------

    public void StartRound()
    {
        if (task)
            task.ResetSession();   // spawns blocks + resets score

        timeLeft = timeLimit;
        running = true;
        lastScore = 0;
        combo = 0;
        UpdateHUD();
    }

    // ----------------------------------------------------------------------
    // ? UI UPDATE ?
    // ----------------------------------------------------------------------

    void UpdateHUD()
    {
        if (task && scoreText)
            scoreText.text = $"Score: {task.score}";

        if (goalText)
            goalText.text = $"Goal: {goalScore}";

        if (timerText)
        {
            int t = Mathf.CeilToInt(timeLeft);
            timerText.text = $"{t / 60:00}:{t % 60:00}";
        }

        if (timerBar)
            timerBar.fillAmount = (timeLimit > 0f) ? timeLeft / timeLimit : 0f;

        if (comboText)
        {
            comboText.text = combo > 1 ? $"Combo x{combo}" : "";
            comboText.alpha = combo > 1 ? 1f : 0f;
        }
    }
}

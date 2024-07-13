using DeltaReality.NucleusXR.CustomAttributes;
using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class ThreadManager : MonoBehaviour
{
    [Header("Settings:")]
    [SerializeField] private int TargetFrameCount = 100;
    [SerializeField] private int IterationsCount = 2;

    [Header("Runtime Info:")]
    [SerializeField] private double _deltaTime = default;
    [SerializeField] private int _threadSleepTimeMilliseconds = default;

    private bool _isThreadRunning = default;
    private Thread _thread = default;

    [SerializeField] private bool _isApplicationPaused = default;

    public delegate void ThreadManagerEventHandler(double deltaTime);
    public static event ThreadManagerEventHandler Updated;

    private void OnValidate()
    {
        _deltaTime = 1d / TargetFrameCount;
        _threadSleepTimeMilliseconds = Mathf.RoundToInt((float)_deltaTime * 1000);
    }

    private void Start()
    {
        _thread = new Thread(ThreadedLoop);
        _thread.Start();
        EditorApplication.pauseStateChanged += OnPauseStateChanged;
    }


    private void OnDestroy()
    {
        _isThreadRunning = default;
        EditorApplication.pauseStateChanged -= OnPauseStateChanged;
    }

    private void OnPauseStateChanged(PauseState pauseState)
    {
        //_isApplicationPaused = pauseState.Equals(PauseState.Paused);
    }

    private void Update()
    {
        //_isApplicationPaused = EditorApplication.isPaused;
    }

    private bool _step = default;

    [Button]
    public void Step()
    {
        _step = true;
    }

    private void ThreadedLoop()
    {
        _isThreadRunning = true;
        Thread.Sleep(250);
        while (_isApplicationPaused)
        {
            Thread.Sleep(1);
        }
        DateTime previousFrameTime = DateTime.Now;
        while (_isThreadRunning)
        {
            if (!_isApplicationPaused || _step)
            {
                _step = default;
                DateTime currentFrameTime = DateTime.Now;
                float deltaTime = (float)(currentFrameTime - previousFrameTime).TotalSeconds;
                previousFrameTime = currentFrameTime;

                deltaTime = deltaTime / (float)IterationsCount;
                for (int i = 0; i < IterationsCount; i++)
                {
                    // Update physics with the calculated deltaTime
                    Updated?.Invoke(deltaTime);
                }

                // Calculate the time to sleep to maintain the frame rate
                DateTime frameEndTime = DateTime.Now;
                int frameElapsedTime = (int)(frameEndTime - currentFrameTime).TotalMilliseconds;
                int sleepTime = _threadSleepTimeMilliseconds - frameElapsedTime;

                if (sleepTime > 0)
                {
                    Thread.Sleep(sleepTime);
                }
                else
                {
                    // If the sleep time is zero or negative, we skip sleeping and log a warning if necessary
                    UnityEngine.Debug.LogWarning("Frame rate is too high for the current workload. Consider reducing frame rate or optimizing code.");
                }
            }
        }
    }
}

using System.Threading;
using UnityEngine;

public class ThreadManager : MonoBehaviour
{
    [Header("Settings:")]
    [SerializeField] private int TargetFrameCount = 100;

    [Header("Runtime Info:")]
    [SerializeField] private float _deltaTime = default;
    [SerializeField] private int _threadSleepTimeMilliseconds = default;

    private bool _isThreadRunning = default;
    private Thread _thread = default;

    public delegate void ThreadManagerEventHandler(double deltaTime);
    public static event ThreadManagerEventHandler Updated;

    private void OnValidate()
    {
        _deltaTime = 1f / TargetFrameCount;
        _threadSleepTimeMilliseconds = Mathf.RoundToInt(_deltaTime * 1000);
    }

    private void Start()
    {
        _thread = new Thread(ThreadedLoop);
        _thread.Start();
    }

    private void OnDestroy()
    {
        _isThreadRunning = default;
    }

    private void ThreadedLoop()
    {
        _isThreadRunning = true;
        while (_isThreadRunning)
        {
            Updated?.Invoke(_deltaTime);
            Thread.Sleep(_threadSleepTimeMilliseconds);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using MixedReality.Toolkit.Input;
using UnityEngine;

public class EyeTracker : MonoBehaviour
{
    //ATTRIBUTES
    public GazeInteractor gazeInteractor;
    public GameObject gazeIndicator;
    private GameObject _gazePointer;
    public GameObject userCamera;
    private GameObject _currentGazingObject;
    private float _currentGazingTimer;

    [SerializeField] private float exportInterval = 1f;

    [Header("Export Settings")] [SerializeField]
    private string exportFileName = "eye-tracking";

    private string _eyeTrackingFilePath;
    private float _lastFlushedTime;
    private readonly List<EyeTrackingDatum> _eyeTrackingDataBuffer = new();

    //METHODS
    private static GameObject GetGazingObject(RaycastHit hit)
    {
        var currentObject = hit.collider.gameObject.transform;

        while (currentObject.parent)
        {
            currentObject = currentObject.parent;
        }

        return currentObject.gameObject;
    }

    private void ExportRecentData()
    {
        if (_eyeTrackingDataBuffer.Count == 0) return;

        if (Time.time - _lastFlushedTime >= exportInterval)
        {
            FlushEyeTrackingData();
        }
    }

    private void FlushEyeTrackingData()
    {
        var isFileExisting = File.Exists(_eyeTrackingFilePath);

        using var writer = new StreamWriter(_eyeTrackingFilePath, true);

        if (!isFileExisting)
        {
            writer.WriteLine("Time(s),GazingObject,GazingTimer(s)");
        }
        else
        {
            foreach (var datum in _eyeTrackingDataBuffer)
            {
                writer.WriteLine($"{datum.TimeStamp},{datum.GazingObject},{datum.GazingTimer}");
            }

            writer.Flush();
            _eyeTrackingDataBuffer.Clear();
            _lastFlushedTime = Time.time;
        }
    }

    private void Awake()
    {
        var timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _eyeTrackingFilePath = Application.persistentDataPath + $"/{exportFileName}_{timeStamp}.csv";

        Debug.Log($"Exporting velocity data to {_eyeTrackingFilePath}");
    }

    private void Start()
    {
        _gazePointer = Instantiate(gazeIndicator);
    }

    private void Update()
    {
        UpdateGazeTracking();
        ExportRecentData();
    }

    private void UpdateGazeTracking()
    {
        const float maxDistance             = 3f;
        const float fallbackPointerDistance = 2f;

        var rayOrigin    = gazeInteractor.rayOriginTransform.position;
        var rayDirection = gazeInteractor.rayOriginTransform.forward;
        var ray          = new Ray(rayOrigin, rayDirection);

        if (Physics.Raycast(ray, out var hit, maxDistance))
        {
            HandleGazeHit(hit);
        }
        else
        {
            HandleGazeMiss(fallbackPointerDistance);
        }
    }

    private void HandleGazeHit(RaycastHit hit)
    {
        // Update gaze indicator position
        _gazePointer.transform.position = hit.point;

        // Get what the user is currently gazing at this frame
        var gazingObject = GetGazingObject(hit);

        if (gazingObject == _currentGazingObject)
        {
            // Still gazing at the same object, increment timer
            _currentGazingTimer += Time.deltaTime;
        }
        else
        {
            // Gazing at a different object
            _eyeTrackingDataBuffer.Add(new EyeTrackingDatum
                                       {
                                           TimeStamp = Time.time,
                                           GazingObject = gazingObject.name,
                                           GazingTimer = _currentGazingTimer
                                       });

            SwitchToNewGazingObject(gazingObject);
        }
    }

    private void HandleGazeMiss(float fallbackDistance)
    {
        // Reset gaze indicator position below camera
        _gazePointer.transform.position = userCamera.transform.position - Vector3.down * fallbackDistance;

        if (!_currentGazingObject) return;

        // Record data if users were previously gazing at something
        _eyeTrackingDataBuffer.Add(new EyeTrackingDatum
                                   {
                                       TimeStamp = Time.time,
                                       GazingObject = _currentGazingObject.name,
                                       GazingTimer = _currentGazingTimer
                                   });

        ResetGazeTracking();
    }

    private void SwitchToNewGazingObject(GameObject newObject)
    {
        _currentGazingObject = newObject;
        _currentGazingTimer = 0f;
    }

    private void ResetGazeTracking()
    {
        _currentGazingObject = null;
        _currentGazingTimer = 0f;
    }

    private void FlushAllRemainingEyeTrackingData()
    {
        if (_eyeTrackingDataBuffer.Count == 0) return;

        FlushEyeTrackingData();
    }

    private void OnDestroy()
    {
        FlushAllRemainingEyeTrackingData();
    }
}

internal record EyeTrackingDatum
{
    public float TimeStamp { get; set; }
    public string GazingObject { get; set; }
    public float GazingTimer { get; set; }
}
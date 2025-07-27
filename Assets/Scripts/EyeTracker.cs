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
    private CsvExporter _eyeTrackingExporter;

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

    private void Awake()
    {
        var          timeStamp           = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var          eyeTrackingFilePath = Application.persistentDataPath + $"/{exportFileName}_{timeStamp}.csv";
        const string csvHeader           = "Time (s),GazingObject,GazingTimer (s)";

        _eyeTrackingExporter = new CsvExporter(eyeTrackingFilePath, exportInterval, csvHeader);

        Debug.Log($"Exporting eye tracking data to {eyeTrackingFilePath}");
    }

    private void Start()
    {
        _gazePointer = Instantiate(gazeIndicator);
    }

    private void Update()
    {
        UpdateGazeTracking();
        _eyeTrackingExporter.ExportRecentData();
    }

    private void UpdateGazeTracking()
    {
        const float maxDistance             = 50f;
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
            _eyeTrackingExporter.AddData(new EyeTrackingDatum
                                         {
                                             TimeStamp = Time.time,
                                             GazingObject = gazingObject.name,
                                             GazingTimer = _currentGazingTimer
                                         }.ToString());

            SwitchToNewGazingObject(gazingObject);
        }
    }

    private void HandleGazeMiss(float fallbackDistance)
    {
        // Reset gaze indicator position below camera
        _gazePointer.transform.position = userCamera.transform.position - Vector3.down * fallbackDistance;

        if (!_currentGazingObject) return;

        // Record data if users were previously gazing at something
        _eyeTrackingExporter.AddData(new EyeTrackingDatum
                                     {
                                         TimeStamp = Time.time,
                                         GazingObject = "Nothing",
                                         GazingTimer = _currentGazingTimer
                                     }.ToString());

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
        if (_eyeTrackingExporter.BufferCount == 0) return;

        _eyeTrackingExporter.ForceFlush();
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

    public override string ToString()
    {
        return $"{TimeStamp},{GazingObject},{GazingTimer}";
    }
}
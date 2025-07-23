using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CsvExporter
{
    private readonly List<string> _dataBuffer = new();
    private readonly string _filePath;
    private readonly float _exportInterval;
    private readonly string _csvHeader;
    private float _lastFlushedTime = Time.time;
    public int BufferCount => _dataBuffer.Count;
    
    public CsvExporter(string filePath, float exportInterval, string csvHeader)
    {
        _filePath = filePath;
        _exportInterval = exportInterval;
        _csvHeader = csvHeader;
    }
    
    public void AddData(string data)
    {
        _dataBuffer.Add(data);
    }
    
    public void ExportRecentData()
    {
        if (_dataBuffer.Count == 0) return;

        if (Time.time - _lastFlushedTime >= _exportInterval)
        {
            FlushData();
        }
    }

    private void FlushData()
    {
        if (_dataBuffer.Count == 0) return;
        
        var isFileExisting = File.Exists(_filePath);

        using var writer = new StreamWriter(_filePath, true);

        if (!isFileExisting)
        {
            writer.WriteLine(_csvHeader);
        }
        
        foreach (var datum in _dataBuffer)
        {
            writer.WriteLine(datum);
        }

        writer.Flush();
        _dataBuffer.Clear();
        _lastFlushedTime = Time.time;
    }
    
    public void ForceFlush()
    {
        FlushData();
    }
}
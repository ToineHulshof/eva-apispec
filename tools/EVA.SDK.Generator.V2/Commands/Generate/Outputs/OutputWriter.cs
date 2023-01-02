﻿using System.Text;
using EVA.SDK.Generator.V2.Helpers;
using Microsoft.Extensions.Logging;

namespace EVA.SDK.Generator.V2.Commands.Generate.Outputs;

public class OutputWriter
{
  private readonly string _directory;

  private readonly List<(string name, long size)> _writtenFiles = new();

  public OutputWriter(string directory)
  {
    _directory = directory;
  }

  private void EnsureDirectoryExists(string file)
  {
    var directory = Path.GetDirectoryName(file);
    if (directory == null) return;
    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
  }

  public async Task WriteFileAsync(string file, string content)
  {
    var path = Path.Combine(_directory, file);
    EnsureDirectoryExists(path);
    await File.WriteAllTextAsync(path, content);
    _writtenFiles.Add((file, new FileInfo(path).Length));
  }

  internal DisposableCallback<Stream> WriteStreamAsync(string file)
  {
    var path = Path.Combine(_directory, file);
    EnsureDirectoryExists(path);
    return new DisposableCallback<Stream>(File.OpenWrite(path), () =>
    {
      _writtenFiles.Add((file, new FileInfo(path).Length));
    });
  }

  internal string ToReport()
  {
    var sb = new StringBuilder();

    var totalSize = _writtenFiles.Sum(x => x.size);
    sb.Append($"Wrote {_writtenFiles.Count} files with total size of {StringHelpers.FormatSize(totalSize)}");
    if (_writtenFiles.Count is <= 1 or > 40) return sb.ToString();

    var records = _writtenFiles.Select(f => (f.name, sizeStr:StringHelpers.FormatSize(f.size), f.size)).OrderByDescending(x => x.size).ToList();
    var sizeColumnWidth = records.Max(x => x.sizeStr.Length);

    sb.AppendLine(":");

    foreach (var f in records)
    {
      sb.AppendLine($"  {f.sizeStr.PadLeft(sizeColumnWidth)} {((int)(100.0f * f.size / totalSize)).ToString().PadLeft(3)}% {f.name}");
    }

    return sb.ToString();
  }

  internal class DisposableCallback<T> : IAsyncDisposable where T : IAsyncDisposable
  {
    private readonly Action _callback;

    internal T Value { get; }

    public DisposableCallback(T wrapped, Action callback)

    {
      Value = wrapped;
      _callback = callback;
    }

    public async ValueTask DisposeAsync()
    {
      await Value.DisposeAsync();
      _callback.Invoke();
    }
  }
}
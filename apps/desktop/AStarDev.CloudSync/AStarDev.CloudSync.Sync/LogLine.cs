using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>One entry read back from a local log file for the log viewer (#1411). Its text is already free of personal data, because that is all the writer ever wrote.</summary>
/// <param name="Time">When the entry was written.</param>
/// <param name="Level">The entry's level.</param>
/// <param name="AccountHash">The hashed account id the entry was made for, or none.</param>
/// <param name="Category">The logger category.</param>
/// <param name="Message">The message. An exception's lines are part of it.</param>
public sealed record LogLine(DateTimeOffset Time, LogLevel Level, Option<string> AccountHash, string Category, string Message);

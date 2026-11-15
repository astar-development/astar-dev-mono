using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>What the log viewer asks for (#1411).</summary>
/// <param name="AccountHash">Only entries for this hashed account id, or none for every account.</param>
/// <param name="MinimumLevel">Only entries at this level or above.</param>
/// <param name="Text">Only entries whose message or category contains this text, ignoring case. Empty matches everything.</param>
public sealed record LogQuery(Option<string> AccountHash, LogLevel MinimumLevel, string Text);

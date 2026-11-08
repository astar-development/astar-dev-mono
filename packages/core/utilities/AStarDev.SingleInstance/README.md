# AStarDev.SingleInstance

One running instance per user for AStar Development desktop apps.

## Usage

```csharp
services.AddSingleInstance();

var options = SingleInstanceOptionsFactory.Create(dataFolder, "myapp", handOffTimeout: null);
var claim = await guard.ClaimAsync(options, cancellationToken);

switch (claim)
{
    case InstanceClaim.Primary primary:
        primary.Instance.ActivationRequested += (_, _) => BringWindowForward();
        break;
    case InstanceClaim.HandedOff or InstanceClaim.NotAnswered:
        Shutdown(0);
        break;
}
```

- The lock is `<dataFolder>/<name>.lock`, opened with `FileShare.None` and held until the `PrimaryInstance` is disposed. The OS releases it when the process dies, so a crash never leaves a stale lock.
- The hand-off socket is `<dataFolder>/<name>.sock`. Use a per-user data folder so instances of different users never meet.
- Keep the data folder path short: Unix domain socket paths are limited to about 100 characters.

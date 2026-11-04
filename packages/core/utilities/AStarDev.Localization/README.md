# AStarDev.Localization

JSON-file localisation for AStar Development desktop apps.

## Usage

1. Embed one JSON file per culture in your app at `Assets/Localization/{culture}.json`:

   ```xml
   <EmbeddedResource Include="Assets\Localization\*.json" />
   ```

   ```json
   {
     "locale": "en-GB",
     "App.Title": "My app",
     "Greeting": "Hello, {0}"
   }
   ```

2. Register the service, passing the assembly that holds the files:

   ```csharp
   services.AddLocalizationService(typeof(App).Assembly, new CultureInfo("en-GB"));
   ```

3. Read text through `ILocalizationService`:

   ```csharp
   localization.GetLocal("App.Title");
   localization.GetLocal("Greeting", userName);
   ```

A missing key returns the key itself, so the UI never shows blank text. If the requested culture has no file, the service falls back to the two-letter language, then to `en-GB`.

## Culture switching

`SetCultureAsync` loads the new culture and raises `CultureChanged`. Derive view models that display text from `LocalizedObservableObject`: it raises `PropertyChanged` for all properties when the culture changes, and holds only a weak subscription, so short-lived view models are not kept alive.

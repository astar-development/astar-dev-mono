namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Auth;

// NOTE: AuthServiceTests deliberately left empty.
// AuthService and TokenCacheService are sealed classes that cannot be mocked with NSubstitute.
// AuthService requires MSAL infrastructure (token cache, browser) that is not available in unit tests.
// Testing this service requires integration tests with a mock HTTP server or real browser.


using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Core.Accounts;

/// <summary>The provider's own identifier for a signed-in account, for OneDrive the MSAL <c>HomeAccountId.Identifier</c>. Used to detect an account that is added twice.</summary>
[StrongType(typeof(string))]
public partial record struct ProviderAccountKey;

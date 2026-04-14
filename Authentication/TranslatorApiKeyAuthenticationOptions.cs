using Microsoft.AspNetCore.Authentication;

namespace LibNode.Api.Authentication;

public class TranslatorApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string ApiKey { get; set; } = string.Empty;
}

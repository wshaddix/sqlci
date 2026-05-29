using SqlCi.ScriptRunner;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace SqlCi.ScriptRunner.Tests;

public class SecretRedactorTests
{
    [Test]
    [Arguments("Server=.;Database=db;Password=secret;", "Password=xxxxxx")]
    [Arguments("Server=.;Database=db;password=secret;", "password=xxxxxx")]
    [Arguments("Server=.;Database=db;Password = secret;", "Password=xxxxxx")]
    [Arguments("Host=h;Username=u;Pwd=secret", "Pwd=xxxxxx")]
    public async Task Redact_RemovesPasswordValue(string input, string expectedFragment)
    {
        var result = SecretRedactor.Redact(input);

        await Assert.That(result).Contains(expectedFragment);
        await Assert.That(result).DoesNotContain("secret");
    }

    [Test]
    public async Task Redact_PasswordAsLastTokenWithoutSemicolon_IsRedacted()
    {
        var result = SecretRedactor.Redact("Server=.;Database=db;Password=topsecret");

        await Assert.That(result).DoesNotContain("topsecret");
    }

    [Test]
    public async Task Redact_MessageWithoutPassword_IsUnchanged()
    {
        const string input = "Opening connection to database ...";
        var result = SecretRedactor.Redact(input);

        await Assert.That(result).IsEqualTo(input);
    }
}

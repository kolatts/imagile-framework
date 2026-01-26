using Azure;
using Azure.Security.KeyVault.Secrets;
using Imagile.Framework.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace Imagile.Framework.Configuration.Tests.Extensions
{
    public class KeyVaultConfigurationExtensionsTests
    {
        [Fact]
        public void ReplaceKeyVaultReferences_ReplacesSimpleValue()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Secret"] = "@KeyVault(MySecret)"
                })
                .Build();

            var secretClient = CreateMockSecretClient(new Dictionary<string, string>
            {
                ["MySecret"] = "ActualValue"
            });

            configuration.ReplaceKeyVaultReferences(secretClient);

            Assert.Equal("ActualValue", configuration["Secret"]);
        }

        [Fact]
        public void ReplaceKeyVaultReferences_ReplacesNestedValues()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:Password"] = "@KeyVault(DbPassword)"
                })
                .Build();

            var secretClient = CreateMockSecretClient(new Dictionary<string, string>
            {
                ["DbPassword"] = "p@ssw0rd"
            });

            configuration.ReplaceKeyVaultReferences(secretClient);

            Assert.Equal("p@ssw0rd", configuration["Database:Password"]);
        }

        [Fact]
        public void ReplaceKeyVaultReferences_HandlesDeepNesting()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["A:B:C:Secret"] = "@KeyVault(Deep)",
                    ["A:B:Normal"] = "plaintext"
                })
                .Build();

            var secretClient = CreateMockSecretClient(new Dictionary<string, string>
            {
                ["Deep"] = "DeepValue"
            });

            configuration.ReplaceKeyVaultReferences(secretClient);

            Assert.Equal("DeepValue", configuration["A:B:C:Secret"]);
            Assert.Equal("plaintext", configuration["A:B:Normal"]);
        }

        [Fact]
        public void ReplaceKeyVaultReferences_IgnoresNonKeyVaultValues()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Normal"] = "plaintext",
                    ["AnotherValue"] = "test123"
                })
                .Build();

            var secretClient = CreateMockSecretClient(new Dictionary<string, string>());

            configuration.ReplaceKeyVaultReferences(secretClient);

            Assert.Equal("plaintext", configuration["Normal"]);
            Assert.Equal("test123", configuration["AnotherValue"]);
        }

        [Fact]
        public void ReplaceKeyVaultReferences_HandlesMixedConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["App:Name"] = "MyApp",
                    ["App:Secret"] = "@KeyVault(AppSecret)",
                    ["Database:Host"] = "localhost",
                    ["Database:Password"] = "@KeyVault(DbPassword)",
                    ["Cache:Enabled"] = "true"
                })
                .Build();

            var secretClient = CreateMockSecretClient(new Dictionary<string, string>
            {
                ["AppSecret"] = "SuperSecretValue",
                ["DbPassword"] = "DbP@ssw0rd"
            });

            configuration.ReplaceKeyVaultReferences(secretClient);

            Assert.Equal("MyApp", configuration["App:Name"]);
            Assert.Equal("SuperSecretValue", configuration["App:Secret"]);
            Assert.Equal("localhost", configuration["Database:Host"]);
            Assert.Equal("DbP@ssw0rd", configuration["Database:Password"]);
            Assert.Equal("true", configuration["Cache:Enabled"]);
        }

        [Fact]
        public void ReplaceKeyVaultReferences_IgnoresInvalidKeyVaultFormat()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["InvalidFormat1"] = "@KeyVault(",
                    ["InvalidFormat2"] = "KeyVault(Secret)",
                    ["InvalidFormat3"] = "@KeyVaultSecret)"
                })
                .Build();

            var secretClient = CreateMockSecretClient(new Dictionary<string, string>());

            configuration.ReplaceKeyVaultReferences(secretClient);

            Assert.Equal("@KeyVault(", configuration["InvalidFormat1"]);
            Assert.Equal("KeyVault(Secret)", configuration["InvalidFormat2"]);
            Assert.Equal("@KeyVaultSecret)", configuration["InvalidFormat3"]);
        }

        [Fact]
        public void ReplaceKeyVaultReferences_ThrowsOnNullConfiguration()
        {
            var secretClient = Substitute.For<SecretClient>();

            Assert.Throws<ArgumentNullException>(() =>
                ((IConfiguration)null!).ReplaceKeyVaultReferences(secretClient));
        }

        [Fact]
        public void ReplaceKeyVaultReferences_ThrowsOnNullSecretClient()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            Assert.Throws<ArgumentNullException>(() =>
                configuration.ReplaceKeyVaultReferences(null!));
        }

        [Fact]
        public void ReplaceKeyVaultReferences_PropagatesSecretClientException()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Secret"] = "@KeyVault(MissingSecret)"
                })
                .Build();

            var secretClient = Substitute.For<SecretClient>();
            secretClient.GetSecret("MissingSecret")
                .Returns(_ => throw new RequestFailedException("Secret not found"));

            Assert.Throws<RequestFailedException>(() =>
                configuration.ReplaceKeyVaultReferences(secretClient));
        }

        private static SecretClient CreateMockSecretClient(Dictionary<string, string> secrets)
        {
            var secretClient = Substitute.For<SecretClient>();

            foreach (var kvp in secrets)
            {
                var secret = SecretModelFactory.KeyVaultSecret(
                    new SecretProperties(kvp.Key),
                    kvp.Value);

                var response = Response.FromValue(secret, Substitute.For<Response>());
                secretClient.GetSecret(kvp.Key).Returns(response);
            }

            return secretClient;
        }
    }
}

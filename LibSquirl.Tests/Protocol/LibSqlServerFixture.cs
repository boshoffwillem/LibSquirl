using LibSquirl.Protocol;

namespace LibSquirl.Tests.Protocol;

public sealed class LibSqlServerFixture : IAsyncLifetime
{
    private const string BaseUrl = "http://localhost:8080";
    public ILibSqlClient Client { get; private set; } = null!;
    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        HttpClient = new HttpClient();
        LibSqlClientOptions options = new() { Url = BaseUrl };
        Client = new LibSqlClient(HttpClient, options);

        // Wait for the server to be ready
        int retries = 30;
        while (retries > 0)
        {
            try
            {
                bool healthy = await Client.HealthCheckAsync();
                if (healthy)
                {
                    return;
                }
            }
            catch
            {
                // Server not ready yet
            }

            await Task.Delay(1000);
            retries--;
        }

        throw new InvalidOperationException(
            "libsql-server did not become healthy. Run 'docker compose up -d' first."
        );
    }

    public Task DisposeAsync()
    {
        HttpClient.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition("LibSqlServer")]
public class LibSqlServerCollection : ICollectionFixture<LibSqlServerFixture>;

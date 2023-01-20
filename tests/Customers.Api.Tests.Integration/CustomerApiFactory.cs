using System.Data.Common;
using Customers.Api.Database;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Xunit;

namespace Customers.Api.Tests.Integration;

public class CustomerApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    //private readonly TestcontainersContainer _dbContainer =
    //    new TestcontainersBuilder<TestcontainersContainer>()
    //        .WithImage("postgres:latest")
    //        .WithEnvironment("POSTGRES_USER", "nick")
    //        .WithEnvironment("POSTGRES_PASSWORD", "chapsas")
    //        .WithEnvironment("POSTGRES_DB", "mydb")
    //        .WithPortBinding(5555, 5432)
    //        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
    //        .Build();

    private readonly PostgreSqlTestcontainer _dbContainer =
     new TestcontainersBuilder<PostgreSqlTestcontainer>()
         .WithDatabase(new PostgreSqlTestcontainerConfiguration
         {
             Database = "mydb",
             Username = "nick",
             Password = "chapsas"
         })
        .WithImage("postgres:latest")
        .Build();

    private DbConnection _dbConnection = default!;
    private Respawner _respawner = default!;

    public HttpClient HttpClient { get; private set; } = default!;
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(IDbConnectionFactory));
            services.AddSingleton<IDbConnectionFactory>(_ =>
                //new NpgsqlConnectionFactory("Server=localhost; Port=5432; Database=mydb; User ID=nick; Password=chapsas;"));
                new NpgsqlConnectionFactory(_dbContainer.ConnectionString));
            });     
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        _dbConnection = new NpgsqlConnection(_dbContainer.ConnectionString);
        //new NpgsqlConnection("Server=localhost; Port=5432; Database=mydb; User ID=nick; Password=chapsas;");

        HttpClient = CreateClient();
        await InitializeRespawner();
    }

    private async Task InitializeRespawner()
    {
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" }
        });
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}

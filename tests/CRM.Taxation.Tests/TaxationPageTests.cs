using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.EndPoint.FrontEnd.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed class TaxationPageTests
{
    [Theory]
    [InlineData("Production", "https://sandboxrc.tax.gov.ir/requestsmanager/api/v2/")]
    [InlineData("Development", null)]
    [InlineData("Development", "https://tp.tax.gov.ir/requestsmanager/api/v2/")]
    public async Task UnsupportedSandboxConfigurationNeverDispatchesSubmit(string environment, string address)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Taxation:EnableLocalPreview"] = "true",
            ["Taxation:Sandbox:Enabled"] = "true",
            ["Taxation:Sandbox:BaseAddress"] = address
        }).Build();
        var controller = new TaxationController(new RejectDispatcher(), new TestEnvironment { EnvironmentName = environment }, configuration);
        Assert.IsType<NotFoundResult>(await controller.Submit(Guid.NewGuid(), null, CancellationToken.None));
    }

    private sealed class RejectDispatcher : ICqrsDispatcher
    {
        public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The blocked route must not dispatch a query.");
        public Task<TResult> CommandAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The blocked route must not dispatch a command.");
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public string WebRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

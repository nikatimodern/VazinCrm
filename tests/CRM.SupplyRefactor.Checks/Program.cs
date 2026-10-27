using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Commands.AddPayment;
using CRM.Core.ApplicationServices.Supplies.Commands.DeleteSalesAllocation;
using CRM.Core.ApplicationServices.Supplies.Commands.SaveSalesAllocation;
using CRM.Core.ApplicationServices.Supplies.Commands.SaveSupplyAllocation;
using CRM.Core.ApplicationServices.Supplies.Commands.UpdatePayment;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyPayments;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.Core.Domain.Supplies.Rules;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.EndPoint.FrontEnd.Composition;
using CRM.EndPoint.FrontEnd.Controllers;
using CRM.EndPoint.FrontEnd.Services;
using CRM.Infrastructures.Sql;
using CRM.Infrastructures.Sql.Supplies.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

var checks = 0;
void Check(bool condition, string description)
{
    if (!condition)
        throw new Exception(description);
    checks++;
}

var json = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SupplyHttpContract.json")))!;
var actions = typeof(SupplyController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(m => typeof(IActionResult).IsAssignableFrom(m.ReturnType) || m.ReturnType == typeof(Task<IActionResult>)).ToDictionary(m => m.Name);
Check(actions.Count == 35 && actions.Keys.Order().SequenceEqual(json.Keys.Order()), "All 35 action names are unchanged");
Check(typeof(SupplyController).GetCustomAttribute<AuthorizeAttribute>()?.Policy == "NotCustomerOnly", "Controller authorization unchanged");
foreach (var(name, attributes)in json)
{
    var action = actions[name];
    var expectedVerbs = attributes.Where(a => a.StartsWith("[Http")).Select(a => a.Contains("Get") ? "GET" : "POST").Order().ToArray();
    Check(action.GetCustomAttributes<HttpMethodAttribute>().SelectMany(a => a.HttpMethods).Order().SequenceEqual(expectedVerbs), name + " HTTP verbs");
    Check(action.IsDefined(typeof(ValidateAntiForgeryTokenAttribute)) == attributes.Contains("[ValidateAntiForgeryToken]"), name + " anti-forgery");
    var roleAttribute = attributes.SingleOrDefault(a => a.StartsWith("[Authorize"));
    var expectedRoles = roleAttribute == null ? null : roleAttribute.Contains("SalesAllocationRoles") ? "مدیر تامین,کارشناس تامین,مدیر ارشد,مدیر اجرایی,مدیر عامل,مدیر مالی,کارشناس مالی,مدیر فروش,کارشناس فروش" : roleAttribute.Split('"')[1];
    Check(action.GetCustomAttribute<AuthorizeAttribute>()?.Roles == expectedRoles, name + " authorization roles");
}

Check(!typeof(SupplyController).GetConstructors().SelectMany(c => c.GetParameters()).Any(p => typeof(DbContext).IsAssignableFrom(p.ParameterType)), "Controller has no DbContext dependency");
var appAssembly = typeof(SaveSalesAllocationCommand).Assembly;
Check(!appAssembly.GetReferencedAssemblies().Any(a => a.Name!.Contains("EntityFramework") || a.Name.Contains("Infrastructures") || a.Name.Contains("AspNetCore.Mvc")), "Application dependency direction");
Check(SupplyAllocationRules.TryParseSalesWeight("۱٬۲۳۴٫۵۰", out var parsed) && parsed == 1234.50m, "Persian decimal parsing");
Check(SupplyAllocationRules.TryParseSalesWeight("١٢٣٫٤٥", out parsed) && parsed == 123.45m, "Arabic decimal parsing");
Check(!SupplyAllocationRules.IsValidWeight(0) && !SupplyAllocationRules.IsValidWeight(-1) && !SupplyAllocationRules.IsValidWeight(.001m) && SupplyAllocationRules.IsValidWeight(.01m), "Allocation precision and positivity");
Check(!SupplyAllocationRules.HaveSameUnit(1, 2) && SupplyAllocationRules.HaveSameUnit(2, 2), "Canonical units");
Check(SupplyAllocationRules.SalesCapacity("100", 80, 10) == 30 && SupplyAllocationRules.WarehouseCapacity(15, 20, 5) == 20, "Own weight restored once");
var targets = SupplyAllocationRules.StockTargets(100, 60, 20, 15);
Check(targets[SupplyAllocationRules.CirculationId] == 40 && targets[SupplyAllocationRules.WasteId] == 20 && targets[SupplyAllocationRules.StorageId] == 15, "Stock formulas unchanged");
try
{
    SupplyAllocationRules.StockTargets(10, 11, 0, 0);
    Check(false, "Overallocated supply must fail");
}
catch (InvalidOperationException)
{
    checks++;
}

var user = new AppUser
{
    Id = "fixture",
    FirstName = "Test"
};
var source = new SupplyFactorItem
{
    Id = Guid.NewGuid(),
    SupplyFactorId = Guid.NewGuid(),
    WeightCount = 80,
    Unit = UnitTypeEnums.کیلوگرم
};
var sale = new FactorItem
{
    Id = Guid.NewGuid(),
    factorId = Guid.NewGuid(),
    WeightCount = "100.50",
    Unit = 2,
    UnitTxt = "کیلوگرم"
};
var transaction = new FakeTransaction();
var calls = new List<string>();
var allocations = new List<SupplyFactorItemAllocation>();
var duplicate = false;
decimal allocated = 0;
var origin = Guid.NewGuid();
var write = Stub.Create<ISupplyWriteRepository>((m, a) =>
{
    calls.Add(m.Name);
    return m.Name switch
    {
        "BeginTransactionAsync" => Task.FromResult<ISupplyTransaction>(transaction),
        "LockSupplyItemAsync" => Task.FromResult(source),
        "LockSalesItemAsync" => Task.FromResult(sale),
        "SupplyFactorExistsAsync" or "SalesFactorExistsAsync" or "WarehouseExistsAsync" => Task.FromResult(true),
        "GetWarehouseSourceAsync" => Task.FromResult(new WarehouseProduct { Id = origin, SupplyFactorItemId = source.Id, WarehouseId = SupplyAllocationRules.StorageId, Unit = UnitTypeEnums.کیلوگرم }),
        "GetAllocationAsync" => Task.FromResult(allocations.FirstOrDefault(x => x.Id == (Guid)a![0]!)),
        "SumAllocationsAsync" => Task.FromResult(allocated),
        "AllocationExistsAsync" => Task.FromResult(duplicate),
        "AddAllocation" => Add((SupplyFactorItemAllocation)a![0]!),
        "SaveChangesAsync" => Task.CompletedTask,
        _ => throw new NotSupportedException(m.Name)};
});
object? Add(SupplyFactorItemAllocation allocation)
{
    allocations.Add(allocation);
    return null;
}

var inventory = Stub.Create<ISupplyInventoryRepository>((m, a) =>
{
    calls.Add(m.Name);
    return Task.CompletedTask;
});
var input = new SupplyFactorAllocationRequest
{
    SupplyFactorItemId = source.Id,
    FactorItemId = sale.Id,
    Weight = 20
};
var supplyHandler = new SaveSupplyAllocationCommandHandler(write, inventory);
sale.Unit = 1;
var result = await supplyHandler.HandleAsync(new(input, null, user), default);
Check(result.Kind == SupplyResultKind.Invalid && !calls.Contains("SaveChangesAsync") && !transaction.Committed, "Mismatched unit rolls back before writing");
sale.Unit = 2;
calls.Clear();
transaction.Reset();
allocated = 90;
result = await supplyHandler.HandleAsync(new(input, null, user), default);
Check(result.Kind == SupplyResultKind.Invalid && !calls.Contains("SaveChangesAsync"), "Sales aggregate capacity");
allocated = 0;
duplicate = true;
calls.Clear();
transaction.Reset();
result = await supplyHandler.HandleAsync(new(input, null, user), default);
Check(result.Kind == SupplyResultKind.Invalid && !calls.Contains("AddAllocation"), "Duplicate protection");
duplicate = false;
calls.Clear();
transaction.Reset();
result = await supplyHandler.HandleAsync(new(input, null, user), default);
Check(result.Kind == SupplyResultKind.Succeeded && transaction.Committed, "Supply allocation commits");
Check(calls.IndexOf("LockSupplyItemAsync") < calls.IndexOf("LockSalesItemAsync") && calls.IndexOf("SynchronizeAsync") > calls.IndexOf("SaveChangesAsync"), "Source-before-sales lock and stock update order");
allocations.Clear();
calls.Clear();
transaction.Reset();
var salesHandler = new SaveSalesAllocationCommandHandler(write, inventory);
result = await salesHandler.HandleAsync(new(new SupplyFactorAllocationRequest { SourceWarehouseProductId = origin, WarehouseId = SupplyAllocationRules.StorageId, FactorItemId = sale.Id, Weight = 12.5m }, null, user), default);
Check(result.Kind == SupplyResultKind.Succeeded && allocations.Single().SourceWarehouseProductId == origin && allocations.Single().SupplyFactorItemId == source.Id && allocations.Single().WarehouseId == null, "Warehouse withdrawal retains supply provenance");
Check(calls.Contains("ChangeWarehouseSourceAsync") && calls.Contains("ReverseLegacySalesReceiptsAsync") && transaction.Committed, "Warehouse operations remain in the same transaction");
var paymentTransaction = new FakeTransaction();
var paymentCalls = new List<string>();
var auditFails = false;
var paymentDate = new DateTime(2026, 10, 27);
var payment = new Payment
{
    Id = Guid.NewGuid(),
    Amount = 123.45m,
    PaymentSupplyFactors = [new PaymentSupplyFactor
    {
        SupplyFactorId = source.SupplyFactorId
    }

    ]
};
var savedPayment = new Payment
{
    Id = payment.Id,
    Amount = 10m
};
var attachment = new Attachment
{
    Id = Guid.NewGuid()
};
var paymentRepository = Stub.Create<ISupplyWriteRepository>((method, args) =>
{
    paymentCalls.Add(method.Name);
    switch (method.Name)
    {
        case "BeginTransactionAsync":
            return Task.FromResult<ISupplyTransaction>(paymentTransaction);
        case "AddPaymentAsync":
        case "SaveChangesAsync":
        case "AddAttachmentsAsync":
            return Task.CompletedTask;
        case "GetPaymentForUpdateAsync":
            return Task.FromResult(savedPayment);
        case "ApplyPaymentChanges":
            savedPayment.Amount = ((Payment)args![1]!).Amount;
            return null;
        case "UpdateAttachments":
            return null;
        default:
            throw new NotSupportedException(method.Name);
    }
});
var integration = Stub.Create<ISupplyIntegrationServices>((method, args) =>
{
    paymentCalls.Add(method.Name);
    return method.Name switch
    {
        "GetDateMiladi" => paymentDate,
        "GetDateTimePersian" => "1405/08/05 12:00:00",
        "SaveAttachments" => new List<Attachment>
        {
            attachment
        },
        "LogChangeAsync" => auditFails ? throw new InvalidOperationException("fixture audit failure") : Task.CompletedTask,
        _ => throw new NotSupportedException(method.Name)};
});
var addPayment = new AddPaymentCommandHandler(paymentRepository, integration);
result = await addPayment.HandleAsync(new(payment, user), default);
Check(result.Kind == SupplyResultKind.Succeeded && paymentTransaction.Committed && payment.Amount == 123.45m, "Payment decimal amount and commit");
Check(payment.UserCreatorID == user.Id && payment.PaymentDate == paymentDate && attachment.EntityId == payment.Id, "Payment actor/date/attachment ownership");
Check(paymentCalls.Count(name => name == "SaveChangesAsync") == 2 && paymentCalls.Contains("LogChangeAsync"), "Payment save/audit sequence");
payment.Amount = 201.33m;
paymentCalls.Clear();
paymentTransaction.Reset();
var updatePayment = new UpdatePaymentCommandHandler(paymentRepository, integration);
result = await updatePayment.HandleAsync(new(payment, user), default);
Check(result.Kind == SupplyResultKind.Succeeded && savedPayment.Amount == 201.33m && paymentTransaction.Committed, "Payment update amount");
Check(ReferenceEquals(savedPayment.PaymentSupplyFactors, payment.PaymentSupplyFactors) && payment.UserModifyID == user.Id, "Payment invoice links and modification actor");
auditFails = true;
paymentTransaction.Reset();
payment.Amount = 999m;
result = await updatePayment.HandleAsync(new(payment, user), default);
Check(result.Kind == SupplyResultKind.Invalid && !paymentTransaction.Committed && savedPayment.Amount == 201.33m, "Failed financial update rolls back before applying changes");
var services = new ServiceCollection();
services.AddApplicationServices();
services.AddSingleton(write);
services.AddSingleton(inventory);
services.AddSingleton(Stub.Create<ISupplyIntegrationServices>((m, a) => throw new NotSupportedException(m.Name)));
services.AddSingleton(Stub.Create<ISupplyQueryRepository>((m, a) => Task.FromResult(SupplyResult.Success())));
using (var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }))
using (var scope = provider.CreateScope())
{
    var handlers = appAssembly.GetTypes().Where(t => t.Namespace?.StartsWith("CRM.Core.ApplicationServices.Supplies.") == true).SelectMany(t => t.GetInterfaces()).Where(t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IQueryHandler<, >) || t.GetGenericTypeDefinition() == typeof(ICommandHandler<, >))).Distinct().ToArray();
    Check(handlers.Length == 29, "All operation handlers are discoverable");
    foreach (var handler in handlers)
        Check(scope.ServiceProvider.GetRequiredService(handler) != null, "CQRS DI: " + handler.Name);
    Check(scope.ServiceProvider.GetRequiredService<ISupplyRequestDispatcher>() != null, "Scoped dispatcher DI");
}

// Never connect to a database: connection opening and command readers are suppressed.
var sql = new OfflineSql();
var options = new DbContextOptionsBuilder<CRMContextDb>().UseSqlServer("Server=unit-test.invalid;Database=Unused;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=1").AddInterceptors(sql).Options;
await using (var db = new CRMContextDb(options))
{
    var reads = new SupplyQueryRepository(db);
    result = await reads.GetSupplyFactorAllocationsAsync(Guid.NewGuid(), default);
    Check(result.Kind == SupplyResultKind.Succeeded, "Supply projection executes with fixture-only readers");
    var shape = JsonSerializer.SerializeToElement(result.Data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    Check(shape.TryGetProperty("factor", out _) && shape.TryGetProperty("items", out _) && shape.TryGetProperty("allocations", out _) && shape.TryGetProperty("isAllocationComplete", out _), "Allocation response shape");
    result = await reads.SearchAllocationSupplyItemsAsync(null, sale.Id, null, default);
    Check(result.Kind == SupplyResultKind.Succeeded && sql.Commands.Any(s => s.Contains("[Unit] =")), "Unit filtering is in SQL");
    var tableRequest = new SupplyTableRequest("1", 0, 10, "DealDate", "desc", null, null, false, null, null, false, null);
    var additionalReads = new (Func<Task<SupplyResult>> Read, string Name)[]
    {
        (() => reads.GetSalesFactorAllocationsAsync(Guid.NewGuid(), default), "sales allocations"),
        (() => reads.SearchAllocationFactorItemsAsync(null, source.Id, null, default), "sales selector"),
        (() => reads.GetPaymentAsync(null, default), "payment detail"),
        (() => reads.GetSupplyPaymentsAsync(tableRequest, default), "payment table"),
        (() => reads.GetSupplierFinancialSummaryAsync(Guid.NewGuid(), default), "financial summary"),
        (() => reads.GetSupplyFactorsAsync(null, null, default), "supply cards"),
        (() => reads.SearchSupplyFactorsAsync(new(null, []), default), "supply selection"),
        (() => reads.GetSupplyFactorAsync(Guid.NewGuid(), default), "supply detail"),
        (() => reads.GetSupplyBaseDataAsync(default), "base data"),
        (() => reads.GetSupplierOrdersAsync(default), "supplier orders"),
        (() => reads.GetSuppliesAsync(Guid.NewGuid(), default), "prefactor supplies"),
        (() => reads.GetSupplyDemandsAsync(tableRequest, default), "demand table"),
        (() => reads.GetDealSuppliesAsync(Guid.NewGuid(), default), "deal supplies"),
        (() => reads.GetOrderHeadsBySupplierAsync(Guid.NewGuid(), default), "supplier order heads")
    };
    foreach (var read in additionalReads)
        Check((await read.Read()).Kind == SupplyResultKind.Succeeded, "Offline SQL translation: " + read.Name);
    var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot!;
    var initialized = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot.Model, designTime: true);
    Check(!db.GetService<IMigrationsModelDiffer>().HasDifferences(initialized.GetRelationalModel(), db.GetService<IDesignTimeModel>().Model.GetRelationalModel()), "EF schema model is unchanged; no migration required");
    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    try
    {
        await reads.GetSupplyFactorAllocationsAsync(Guid.NewGuid(), canceled.Token);
        Check(false, "Cancellation must propagate");
    }
    catch (OperationCanceledException)
    {
        checks++;
    }
}

var dispatcher = new FixtureDispatcher();
var controller = new SupplyController(dispatcher, null!, new DateTimeServices());
dispatcher.Result = SupplyResult.Missing("fixture");
Check(await controller.GetSupplyFactorAllocations(Guid.NewGuid())is NotFoundObjectResult { Value: "fixture" }, "Missing response mapping");
dispatcher.Result = SupplyResult.Success(null);
Check(await controller.GetPayment(null)is OkObjectResult { Value: null }, "Null object response remains distinct from empty OK");
dispatcher.Result = SupplyResult.Success(new List<SupplyFactorVModel>());
var partial = (PartialViewResult)await controller.GetAllSupplyFactorsPartial(null!);
Check(partial.ViewName == "GetAllSupplyFactorsPartial" && partial.Model is List<CRM.EndPoint.FrontEnd.Models.Supply.SupplyFactorVModel>, "Razor model compatibility");
controller.ControllerContext = new ControllerContext
{
    HttpContext = new DefaultHttpContext()
};
controller.Request.ContentType = "application/x-www-form-urlencoded";
controller.Request.Form = new FormCollection(new Dictionary<string, StringValues> { { "draw", "3" }, { "start", "10" }, { "length", "5" }, { "model", "{}" }, { "order[0][column]", "2" }, { "columns[2][name]", "PaymentNumber" }, { "order[0][dir]", "asc" } });
dispatcher.Result = SupplyResult.Success(new { draw = "3", recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>() });
Check(await controller.GetAllSupplyPaymentDt() is OkObjectResult, "DataTables response mapping");
Check(dispatcher.LastQuery is GetSupplyPaymentsQuery { Request: { Draw: "3", Start: 10, Length: 5, SortColumn: "PaymentNumber", SortDirection: "asc" } }, "DataTables input contract unchanged");
Console.WriteLine($"PASS: {checks} offline regression checks; no database connections or writes.");
public class Stub : DispatchProxy
{
    public Func<MethodInfo, object? []?, object?> InvokeMethod = null!;
    public static T Create<T>(Func<MethodInfo, object? []?, object?> invoke)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, Stub>();
        ((Stub)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object? []? args) => InvokeMethod(targetMethod!, args);
}

public sealed class FakeTransaction : ISupplyTransaction
{
    public bool Committed;
    public bool Disposed;
    public Task CommitAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Committed = true;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct)
    {
        Committed = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }

    public void Reset()
    {
        Committed = false;
        Disposed = false;
    }
}

public sealed class OfflineSql : DbCommandInterceptor, IDbConnectionInterceptor
{
    public List<string> Commands { get; } = [];

    public InterceptionResult ConnectionOpening(DbConnection c, ConnectionEventData e, InterceptionResult r) => InterceptionResult.Suppress();
    public ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection c, ConnectionEventData e, InterceptionResult r, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult(InterceptionResult.Suppress());
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData e, InterceptionResult<DbDataReader> r, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Commands.Add(command.CommandText);
        return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(Reader(command.CommandText)));
    }

    private static DbDataReader Reader(string sql)
    {
        var table = new DataTable();
        // First-row invoice/unit lookups establish the query owner; all collection results are empty.
        var select = sql[..sql.IndexOf("FROM", StringComparison.Ordinal)];
        if (Regex.IsMatch(sql, @"^\s*SELECT COUNT(?:_BIG)?\("))
        {
            table.Columns.Add("Count", typeof(int));
            table.Rows.Add(0);
        }
        else if (Regex.IsMatch(sql, @"^\s*SELECT (?:COALESCE\()?SUM\("))
        {
            table.Columns.Add("Sum", typeof(decimal));
            table.Rows.Add(0m);
        }
        else if (Regex.IsMatch(sql, @"^\s*SELECT TOP\(1\) \[[^\]]+\]\.\[Id\], \[[^\]]+\]\.\[SerialNumber\]\s+FROM"))
        {
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("SerialNumber", sql.Contains("FROM [SupplyFactors]") ? typeof(string) : typeof(int));
            table.Rows.Add(Guid.NewGuid(), sql.Contains("FROM [SupplyFactors]") ? (object)"fixture" : 1);
        }
        else if (Regex.IsMatch(sql, @"^\s*SELECT TOP\(1\) \[[^\]]+\]\.\[Unit\]\s+FROM"))
        {
            table.Columns.Add("Unit", typeof(int));
            table.Rows.Add(2);
        }
        else
        {
            // SQL projection uses ordinal reads; empty rows need only their column metadata.
            var start = sql.IndexOf("SELECT", StringComparison.Ordinal) + 6;
            var depth = 0;
            var columns = 1;
            for (var i = start; i < sql.Length; i++)
            {
                if (sql[i] == '(')
                    depth++;
                if (sql[i] == ')')
                    depth--;
                if (depth == 0 && sql.AsSpan(i).StartsWith("FROM "))
                    break;
                if (depth == 0 && sql[i] == ',')
                    columns++;
            }

            for (var i = 0; i < columns; i++)
                table.Columns.Add("Column" + i, typeof(object));
        }

        return table.CreateDataReader();
    }
}

public sealed class FixtureDispatcher : ISupplyRequestDispatcher
{
    public SupplyResult Result = SupplyResult.Success();
    public object? LastQuery;
    public Task<SupplyResult> QueryAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IQuery<SupplyResult>
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastQuery = query;
        return Task.FromResult(Result);
    }

    public Task<SupplyResult> CommandAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand<SupplyResult>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Result);
    }
}

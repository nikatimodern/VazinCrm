namespace CRM.Core.ApplicationServices.Accounts.Dtos;

public sealed record AccountRedirect(string Action, string Controller = "Account", string? Url = null);
public sealed record AccountChoice(string Id, string? UserName, string FullName);
public sealed record AccountOutcome(int Status, string Message, AccountRedirect? Redirect = null,
    List<string>? Errors = null, List<AccountChoice>? Accounts = null,
    int? RetryAfterSeconds = null, int? RemainingAttempts = null);
public sealed record AccountUpload(byte[] Contents, string FileName, string ContentType);
public sealed record AccountImageResult(byte[] Image, string? Url);

#nullable enable
using System;

namespace CRM.Core.Domain.QualityControls.Models;

// Status of a sales invoice item in the customer-centered review workflow.
public enum QualityControlStatus
{
    NotReviewed = 0,
    NoDiscrepancy = 1,
    HasDiscrepancy = 2
}

// Old QualityControls rows can contain temporary outcomes (4 and 5).
// Retain persisted enum values for historical DB compatibility.
public enum QualityControlOutcome : byte
{
    NoDiscrepancy = 1,
    HasDiscrepancy = 2,
    NoResponse = 4,
    InvalidNumber = 5
}

public sealed record QualityControlAccess(string UserId, string UserName, bool ReadAll);

public sealed record QualityControlQuestionDto(Guid Id, string Title, string QuestionText,
    string ZeroAnswerText, string OneAnswerText, int SortOrder, int? Answer);

public sealed record QualityControlAnswerInput(Guid QuestionId, int? Answer);

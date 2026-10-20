using System;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.Sepidar;

public sealed class PrepareExternalItemMappingRequest
{
    public Guid PreFactorId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
}

public sealed class PreFactorItemSelectionDto
{
    public Guid PreFactorItemId { get; set; }
    public int Row { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string WeightCount { get; set; }
    public int Unit { get; set; }
    public string UnitTitle { get; set; }
}

public sealed class ExternalItemMappingResponseDto
{
    public Guid ExternalSoftwareId { get; set; }
    public string ExternalSoftwareTitle { get; set; } = string.Empty;

    public List<PreFactorItemSelectionDto> PreFactorItems { get; set; } = new();
    public List<SepidarItemDto> SepidarItems { get; set; } = new();
}
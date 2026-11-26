using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CRM.Core.Domain.PreFactors.Repositories;

namespace CRM.EndPoint.FrontEnd.Models.Setting;

public sealed class PrefactorTermInput
{
    public int? Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "اولویت باید بزرگتر از صفر باشد.")]
    public int Order { get; set; }

    [Required(ErrorMessage = "متن بند الزامی است.")]
    public string Text { get; set; }
}

public sealed class PrefactorTermsPageModel
{
    public List<PrefactorTerm> Terms { get; init; }
    public PrefactorTermInput Input { get; init; }
}

public sealed class PrefactorTermsReorderInput
{
    public int[] OriginalIds { get; set; }
    public int[] OrderedIds { get; set; }
}

using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.Taxation;

public sealed record TaxProductUnit(int Code, string Title, string GoodsId, string TaxUnit);
public sealed record TaxProductRow(Guid Id, string Title, IReadOnlyList<TaxProductUnit> Units);
public sealed record TaxProductPage(IReadOnlyList<TaxProductRow> Items, int Page, int PageCount);

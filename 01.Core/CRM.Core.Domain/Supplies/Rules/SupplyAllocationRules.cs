using System;
using System.Collections.Generic;
using System.Globalization;

namespace CRM.Core.Domain.Supplies.Rules;
public static class SupplyAllocationRules
{
    public static readonly Guid WasteId = new("a732c1a0-8710-4b2f-9501-000000000001");
    public static readonly Guid CirculationId = new("a732c1a0-8710-4b2f-9501-000000000002");
    public static readonly Guid StorageId = new("a732c1a0-8710-4b2f-9501-000000000003");
    public static bool IsSystemWarehouse(Guid id) => id == WasteId || id == CirculationId || id == StorageId;
    public static bool IsAllocationDestination(Guid? id) => id == WasteId || id == StorageId;
    public static decimal SalesCapacity(string total, decimal allocated, decimal restored = 0) => TryParseSalesWeight(total, out var weight) ? Math.Max(0, weight - allocated + restored) : 0;
    public static bool TryParseSalesWeight(string value, out decimal weight)
    {
        var normalized = (value ?? "").Trim().Replace(",", "").Replace("٬", "").Replace("٫", ".").Replace(" ", "").Replace("\u00a0", "");
        for (var i = 0; i <= 9; i++)
            normalized = normalized.Replace((char)('۰' + i), (char)('0' + i)).Replace((char)('٠' + i), (char)('0' + i));
        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out weight);
    }

    public static bool IsValidWeight(decimal weight) => weight > 0 && decimal.Round(weight, 2) == weight;
    public static bool HasCapacity(decimal requested, decimal total, decimal allocated) => requested <= total - allocated;
    public static bool HaveSameUnit(int sourceUnit, int targetUnit) => sourceUnit == targetUnit;
    public static decimal WarehouseCapacity(decimal available, decimal remainder, decimal restored = 0) => Math.Min(available + restored, remainder + restored);
    public static Dictionary<Guid, decimal> StockTargets(decimal total, decimal allocated, decimal waste, decimal storage)
    {
        if (total < allocated || total < 0)
            throw new InvalidOperationException("مقدار تامین از مجموع تخصیص‌ها کمتر است.");
        return new Dictionary<Guid, decimal>
        {
            [CirculationId] = total - allocated,
            [WasteId] = waste,
            [StorageId] = storage
        };
    }
}

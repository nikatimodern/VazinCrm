using System;
using System.ComponentModel;
using System.Reflection;

namespace CRM.EndPoint.FrontEnd.Services.Extensions;

public static class EnumExtensions {
    public static string GetDescription(this Enum value)
    {
        var field = value?.GetType().GetField(value.ToString());

        return field?
            .GetCustomAttribute<DescriptionAttribute>()?
            .Description;
    }
}
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.Domain.QualityControls.Models;

namespace CRM.Core.ApplicationServices.QualityControls.Services;

public static class QualityControlAccessPolicy
{
    public const string Roles = "مدیر ارشد,مدیر فروش,مدیر عامل,مدیر تامین,کارشناس فروش,کارشناس تامین";

    public static QualityControlAccess For(ApplicationActor actor)
    {
        if (!actor.HasRole(Roles.Split(',')))
            throw new UnauthorizedAccessException();
        return new(actor.User.Id, actor.User.FullName,
            actor.HasRole("مدیر ارشد", "مدیر فروش", "مدیر عامل"));
    }
}

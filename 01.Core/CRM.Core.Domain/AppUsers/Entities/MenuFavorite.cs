namespace CRM.Core.Domain.AppUsers.Entities;

// One record per user and canonical menu link. Inaccessible favorites are kept
// so they reappear if the user's permission is restored.
public sealed class MenuFavorite
{
    public string UserId { get; set; } = "";
    public string MenuId { get; set; } = "";
    public int SortOrder { get; set; }
}

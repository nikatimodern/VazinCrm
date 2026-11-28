---
name: crm-menu-architecture
description: Add, remove, move, or maintain Vazin CRM navigation menus, categories, settings links, mobile shortcuts, access filtering, favorites, badges, and ordering using the single-source MenuDefinitionService architecture.
---

# CRM menu architecture

Read repository AGENTS.md, erp-clean-code, and applicable frontend/backend skills first. Use this skill for every change affecting CRM navigation, including new features that need a menu entry. Inspect the target branch's actual implementation before editing. If the branch predates this architecture, do not silently restore the legacy two-pass design; report the mismatch and integrate the existing architecture only with appropriate authorization.

## Single source of truth

The menu code belongs in `03.EndPoints/CRM.EndPoint.FrontEnd/Services/Menu/`. Define all ordinary navigation in `MenuDefinitionService.BuildMenus` directly in its final category and position. Use readable `new MenuItem { ... }` initializers, C# collection expressions and English region names. Preserve Persian UI titles.

| Component | Responsibility |
| --- | --- |
| MenuDefinitionService.cs | Final category hierarchy, menu IDs, titles, URLs/actions, icons, ordering, access metadata, visibility and conditional definitions |
| MenuService.cs | Orchestrate definitions, access filtering, visible release version, badges and favorites in that order |
| MenuAccessFilter.cs | Recursive access filtering, empty-category removal and ordering |
| MenuBadgeService.cs | Badge computation and removal of items configured to hide when their badge is empty |
| MenuFavoritesService.cs | Attach eligible favorite metadata and build the favorites group from authorized visible links |
| MenuIcons.cs | Shared menu icon definitions |
| Models/Menu/MenuItem.cs (relative to FrontEnd) | Existing menu model |
| IServices/Menu/IMenuService.cs (relative to FrontEnd) | Stable consumer-facing contract |
| Startup.cs (relative to FrontEnd) | Scoped dependency registrations |

Do not introduce another menu registry, hard-coded navigation tree in a view or script, or a second pass that moves menus between categories by ID. Do not reintroduce OrganizeMenus, Take, Child, Missing, title-based category inference or ungrouped fallback placement. Keep orchestration free of menu definitions and feature-specific badge queries. Return fresh mutable menu items per request; do not cache or share a user-specific filtered tree.

## Add a menu

1. Verify the actual controller/action or route, intended audience, existing access policy and target category.
2. Locate the final category in `BuildMenus`. Add the item directly to its `SubItems` using a unique stable ID, Persian title, verified URL or existing modal action, appropriate icon and explicit sibling order. Do not add it to an obsolete category for later relocation.
3. Set the required visibility and access metadata. Declare role lists directly in each MenuItem using explicit AppRoles constants; do not introduce shared role arrays just to remove duplication. Use inheritance only when the actual parent is intended to grant the same audience to its children. Preserve existing authoritative feature access policies (such as QualityControlAccessPolicy) instead of duplicating or weakening them. For a new feature follow the target branch's current permission model; do not introduce new role-only checks when the feature uses entity permissions.
4. Verify desktop, mobile drawer, search and favorites. Add a separate mobile bottom shortcut only when requested or required by the existing design. For settings, define items in the settings modal's SubItems and use the same filtered tree for Razor rendering, search and favorites.
5. Run focused build/tests and report the result; do not claim validation that was not executed.

Example: the existing production category's warehouse entry illustrates a direct definition, including its explicit effective role audience:

```csharp
new MenuItem {
    Id = "warehouse",
    DisplayType = MenuDisplayType.Link,
    Title = "انبار",
    Url = "/Warehouse/Index",
    Roles = [
        AppRoles.Admin,
        AppRoles.ManagingDirector,
        AppRoles.ExecutiveDirector,
        AppRoles.TaminExpert,
        AppRoles.TaminAdmin,
        AppRoles.FinancialExpert,
        AppRoles.FinancialAdmin
    ],
    DisplayOrder = 3
}
```

This is an existing entry, not a new entry to duplicate. Use a suitable audience for each new menu.

## Category and ordering conventions

The existing root layout is dashboard and mobile shortcuts (1–5), daily activities (10), sales (20), supply (30), production/warehouse/quality (40), finance/treasury (50), management reports (60), settings (70), and fixed actions (998–1000). Read the current definitions rather than treating this list as a permanent inventory. Add a category directly in the intended place when needed; do not rearrange existing categories without a request.

Use explicit, meaningful sibling DisplayOrder values for normal entries; preserve intentional existing exceptions. Sort visible items by DisplayOrder only. Do not renumber or mutate DisplayOrder after filtering; gaps are valid. Do not introduce separate compaction or order-preservation flags. Preserve existing IDs, URLs, titles, icon inheritance and visibility flags unless the task explicitly changes them. IDs are used for persisted favorites and navigation state; do not rename or repurpose them casually.

## Access and security

Menu visibility is not endpoint authorization. Verify the actual server-side permission and record Scope independently, including direct requests and indirect operations. Never expand a user's audience simply because a page has moved to a different category.

In the current legacy-compatible role filter, a nonempty Roles list is checked against role claims; an empty list inherits the nearest effective parent audience, and an empty effective audience has no role restriction. When moving an entry to a category with a different audience, declare its previous effective roles directly on the item. Do not introduce a separate parent-role override property. An inaccessible parent excludes all descendants. Preserve those semantics for existing entries; do not grant access by omitting a role list or broadening the effective audience. Reuse current centralized permission checks when the target feature uses the newer access model; update the centralized filter if explicitly required rather than scattering access logic. Do not bypass security or weaken direct endpoint checks.

Preserve TaxationAvailability and the existing Admin plus local-preview gate for the taxation item. Keep visibility checks centralized; never invent environment or feature-flag bypasses.

## Desktop, mobile, settings and dynamic entries

- Reuse MenuIcons and the existing Razor menu rendering. Inherited icons are valid where the current renderer supplies them; do not create a parallel icon system.
- Keep ShowInDesktop, ShowInMobile and ShowInMobileBottomMenu consistent with the intended surfaces. Existing desktop category entries and mobile-only shortcuts have distinct stable IDs where needed.
- Declare DisplayType explicitly for each menu definition: Link for URL navigation, Group for expandable groups, Action for an operation, and Modal for a modal trigger. OnClick describes behavior only; do not infer display type from it or from the presence of SubItems. The favorites group retains its dedicated renderer.
- Settings is a Modal with Group children and Link/Modal/Action entries beneath them. Define titles, icons, roles, order and routes/actions only in MenuDefinitionService. _SettingModal receives the filtered groups, renders real links and favorite buttons directly, and is refreshed from the same menu snapshot. Search and active-page highlighting consume those real links. Do not introduce a hidden supplemental link catalog, a second settings definition, or URL-based favorite matching. Preserve the phonebook modal action; URL-free actions are not eligible for page favorites. Keep existing settings IDs for persisted favorites.
- Preserve the fixed footer actions overdueOpenDeals, warnings and releaseNotes. Overdue open deals must not be starred or duplicated. Release notes must keep the existing audience-aware latest version logic. Warnings retain their existing empty-badge hiding rule.
- Calculate badges through MenuBadgeService and the existing authorized query or service; preserve user scope, asynchronous behavior and badge-hide rules. Do not introduce new service-locator usage or database queries into definitions.
- Keep favorites attached after filtering and badge handling. Never render an inaccessible saved favorite; keep stored menu IDs and ordering intact. For favorite changes also inspect MenuController's server-side allowed-link validation.
- Preserve existing search, favorite drag-and-drop, expanded/collapsed state, tooltips, mobile navigation and layout unless explicitly requested. Follow the existing UI skills for visual changes.

## Verification checklist

- [ ] Menu appears directly in the correct final category; no duplicate definition or reorganization pass.
- [ ] Stable unique ID, verified route/action, intended title/icon, order and visibility.
- [ ] Authorized and unauthorized users tested, including multiple roles, inherited constraints, no roles and direct endpoint access where relevant.
- [ ] Empty categories are pruned; conditional entries and special ordering are preserved.
- [ ] Desktop/sidebar, mobile drawer/bottom navigation, search, settings and favorites checked as applicable.
- [ ] Existing favorites, badges, release version, fixed actions and navigation state unchanged outside requested scope.
- [ ] Dependency injection and public contracts preserved; no unrelated files or packages changed.
- [ ] Build and relevant regression checks actually run where available; report unrun checks and remaining risks in Persian. Do not commit, push or merge without authorization.

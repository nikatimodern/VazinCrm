/* Persistent ERP menu: delegated interactions survive Virtual DOM page swaps. */
(() => {
    "use strict";
    const navIds = ["desktopNavbar", "mobileDrawerNavbar"];
    const replaceIds = ["desktopNavbar", "desktopFixedActions", "mobileNavbar", "mobileDrawerNavbar", "mobileDrawerFixedActions", "menuSupplementalLinks"];
    const user = window.userContext?.id || "anonymous";
    const groupKey = id => "vazin:menu-groups:" + user + ":" + id;
    const normalizeText = s => (s || "").toLocaleLowerCase("fa")
        .replace(/[يى]/g, "ی").replace(/ك/g, "ک")
        .replace(/[\u200c\u200f\u200e]/g, " ").replace(/[أإآ]/g, "ا").trim();
    const getGroups = id => {
        try {
            const stored = JSON.parse(localStorage.getItem(groupKey(id)) || "[]");
            return new Set(Array.isArray(stored) ? stored : []);
        } catch { return new Set(); }
    };
    const saveGroups = nav => {
        const ids = Array.from(nav.querySelectorAll("[data-menu-group].open"))
            .map(item => item.dataset.menuGroup);
        localStorage.setItem(groupKey(nav.id), JSON.stringify(ids));
    };
    function restoreGroups() {
        for (const id of navIds) {
            const nav = document.getElementById(id);
            if (!nav) continue;
            const open = getGroups(id);
            nav.querySelectorAll("[data-menu-group]").forEach((group, i) => {
                const enabled = open.has(group.dataset.menuGroup);
                group.classList.toggle("open", enabled);
                const toggle = group.querySelector(":scope > .submenu-toggle");
                const submenu = group.querySelector(":scope > .submenu");
                if (submenu) {
                    submenu.id = id + "-submenu-" + i;
                    toggle?.setAttribute("aria-controls", submenu.id);
                }
                toggle?.setAttribute("aria-expanded", String(enabled));
            });
        }
    }
    function matches(link, current) {
        if (link.origin !== current.origin) return false;
        if (link.pathname.toLowerCase().replace(/\/$/, "") !== current.pathname.toLowerCase().replace(/\/$/, "")) return false;
        if (link.pathname.toLowerCase().replace(/\/$/, "") === "/customer/index")
            return (link.searchParams.get("roleType") || "") === (current.searchParams.get("roleType") || "");
        return true;
    }
    function highlight(url = location.href) {
        const current = new URL(url, location.origin);
        document.querySelectorAll(".menu-item.active-menu,.menu-item.active-ancestor,.menu-item.menu-click-orbit").forEach(item => {
            item.classList.remove("active-menu", "active-ancestor", "menu-click-orbit");
            item.removeAttribute("aria-current");
        });
        document.querySelectorAll(".menu-link[href]").forEach(anchor => {
            if (!matches(new URL(anchor.href), current)) return;
            anchor.classList.add("active-menu", "menu-click-orbit");
            anchor.setAttribute("aria-current", "page");
            let parent = anchor.closest("[data-menu-group]");
            while (parent) {
                // Highlight parents without changing their user's open/closed preference.
                parent.querySelector(":scope > .submenu-toggle")?.classList.add("active-ancestor");
                parent = parent.parentElement?.closest("[data-menu-group]");
            }
        });
        const inSettings = Array.from(document.querySelectorAll("#menuSupplementalLinks [data-menu-extra-link]"))
            .some(link => matches(new URL(link.href), current));
        if (inSettings) document.querySelectorAll(".menu-item[onclick*='showModalSetting']")
            .forEach(button => button.classList.add("active-ancestor"));
    }
    const antiforgery = () => document.querySelector('#menuFavoritesAntiforgery input[name="__RequestVerificationToken"]')?.value;
    async function send(path, body) {
        const response = await fetch(path, {
            method: "POST", credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": antiforgery() || "",
                "X-Requested-With": "XMLHttpRequest"
            },
            body: JSON.stringify(body)
        });
        if (!response.ok) throw new Error("HTTP " + response.status);
        return response.json();
    }
    const notifyError = () => {
        if (typeof window.toastifyError === "function")
            window.toastifyError("تغییرات منو ذخیره نشد. دوباره تلاش کنید.");
    };
    let refreshing = null;
    async function refresh() {
        if (refreshing) return refreshing;
        refreshing = (async () => {
            const response = await fetch("/Menu/Snapshot", { credentials: "same-origin", headers: { "X-Requested-With": "XMLHttpRequest" } });
            if (!response.ok) throw new Error("Menu refresh failed: " + response.status);
            const doc = new DOMParser().parseFromString(await response.text(), "text/html");
            for (const id of replaceIds) {
                const previous = document.getElementById(id);
                const updated = doc.getElementById(id);
                if (previous && updated) previous.replaceWith(updated);
            }
            // Layout.js binds the original bottom-bar "More" button once.
            // A server-driven menu refresh replaces that button, so bind only
            // the new instance while keeping the first-load handler untouched.
            document.getElementById("mobileMoreMenuBtn")?.addEventListener("click", () =>
                window.toggleMobileDrawer?.(true));
            restoreGroups();
            highlight();
            initSortable();
            syncSettingsFavorites();
            // Refresh replaces the navigation subtree; the expanded textbox
            // stays visible by CSS and the results start closed.
            document.querySelectorAll("[data-menu-search-row]").forEach(row => row.classList.remove("is-search-open"));
        })().finally(() => { refreshing = null; });
        return refreshing;
    }
    function buildCandidates(panel) {
        const mobile = Boolean(panel.closest("#mobileDrawer"));
        const nav = document.getElementById(mobile ? "mobileDrawerNavbar" : "desktopNavbar");
        const footer = document.getElementById(mobile ? "mobileDrawerFixedActions" : "desktopFixedActions");
        if (!nav) return [];
        const shortcuts = mobile ? document.querySelectorAll("#mobileNavbar .menu-link[href]") : [];
        const links = [...nav.querySelectorAll(".menu-link[href]"),
            ...(footer?.querySelectorAll(".menu-link[href]") || []), ...shortcuts];
        const seen = new Set();
        const canonical = links.filter(link => !link.closest("[data-menu-favorites]")).map(link => {
            const path = [];
            let group = link.closest("[data-menu-group]");
            while (group) {
                path.unshift(group.querySelector(":scope > .submenu-toggle .menu-item-title > span")?.textContent?.trim() || "");
                group = group.parentElement?.closest("[data-menu-group]");
            }
            const title = link.querySelector(".menu-item-title > span")?.textContent?.trim() || link.textContent.trim();
            const route = path.filter(Boolean).join(" ← ")
                || (link.closest("#mobileNavbar") ? "دسترسی سریع" : "");
            return { title, route, href: link.href };
        });
        const extras = Array.from(document.querySelectorAll("#menuSupplementalLinks [data-menu-extra-link]"))
            .map(link => ({ title: link.dataset.menuExtraTitle, route: "تنظیمات", href: link.href }));
        return canonical.concat(extras).filter(item => {
            if (seen.has(item.href)) return false;
            seen.add(item.href); return true;
        });
    }
    function renderSearch(panel) {
        const results = panel?.querySelector("[data-menu-search-results]");
        const query = normalizeText(panel?.querySelector("[data-menu-search-input]")?.value);
        if (!results) return;
        results.replaceChildren();
        // The textbox is always visible in expanded mode, but search results
        // are shown only for an actual search term.
        results.hidden = !query;
        if (!query) return;
        const candidates = buildCandidates(panel)
            .filter(x => normalizeText(x.title + " " + x.route).includes(query));
        if (!candidates.length) {
            const empty = document.createElement("div");
            empty.className = "menu-search-empty";
            empty.textContent = "صفحه‌ای با این عنوان پیدا نشد.";
            results.append(empty);
            return;
        }
        for (const item of candidates.slice(0, 40)) {
            const link = document.createElement("a");
            link.className = "menu-search-result";
            link.href = item.href;
            const title = document.createElement("span");
            title.textContent = item.title;
            const path = document.createElement("small");
            path.textContent = item.route;
            link.append(title, path);
            results.append(link);
        }
    }
    function closeSearch(row) {
        if (!row) return;
        row.classList.remove("is-search-open");
        row.querySelector("[data-menu-search-toggle]")?.setAttribute("aria-expanded", "false");
        const results = row.querySelector("[data-menu-search-results]");
        if (results) results.hidden = true;
    }
    function toggleSearch(toggle) {
        const row = toggle.closest("[data-menu-search-row]");
        if (!row) return;
        const shouldOpen = !row.classList.contains("is-search-open");
        document.querySelectorAll("[data-menu-search-row]").forEach(closeSearch);
        if (!shouldOpen) return;
        row.classList.add("is-search-open");
        toggle.setAttribute("aria-expanded", "true");
        const input = row.querySelector("[data-menu-search-input]");
        input?.focus();
        renderSearch(row.querySelector("[data-menu-search-panel]"));
    }
    function syncSettingsFavorites() {
        // ASP.NET MVC can generate /Controller for an Index action even when
        // the catalog stores /Controller/Index. Treat both as the same page.
        const settingsPath = href => {
            const pathname = new URL(href, window.location.origin).pathname.toLowerCase();
            return pathname.replace(/\/index\/?$/, "").replace(/\/$/, "") || "/";
        };
        const catalog = new Map(Array.from(document.querySelectorAll("#menuSupplementalLinks [data-menu-extra-link]"))
            .map(link => [settingsPath(link.href), link]));
        document.querySelectorAll("#BaseSetting a[href]").forEach(link => {
            const meta = catalog.get(settingsPath(link.href));
            if (!meta) return;
            let star = link.parentElement?.querySelector(":scope > .erp-settings-favorite-button");
            if (!star) {
                const wrapper = document.createElement("div");
                wrapper.className = "erp-settings-favorite-row";
                link.replaceWith(wrapper);
                wrapper.append(link);
                star = document.createElement("button");
                star.type = "button";
                star.className = "erp-settings-favorite-button";
                star.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" aria-hidden="true"><path d="m12 2 3.09 6.26L22 9.27l-5 4.87L18.18 21 12 17.77 5.82 21 7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>';
                wrapper.append(star);
            }
            star.dataset.menuFavorite = meta.dataset.menuExtraLink;
            const selected = meta.dataset.menuExtraFavorite === "true";
            star.classList.toggle("is-favorite", selected);
            star.setAttribute("aria-pressed", String(selected));
            star.setAttribute("aria-label", selected ? "حذف از علاقه‌مندی‌ها" : "افزودن به علاقه‌مندی‌ها");
            star.title = star.getAttribute("aria-label");
            star.querySelector("svg")?.setAttribute("fill", selected ? "currentColor" : "none");
        });
    }
    function initSortable() {
        if (!window.Sortable) return;
        document.querySelectorAll("[data-favorites-list]").forEach(list => {
            if (list.dataset.sortableBound) return;
            list.dataset.sortableBound = "true";
            new Sortable(list, {
                handle: ".menu-favorite-grip",
                animation: 140,
                onEnd: async () => {
                    const ids = Array.from(list.querySelectorAll("[data-favorite-row]"))
                        .map(row => row.dataset.favoriteId);
                    try { await send("/Menu/Reorder", { menuIds: ids }); await refresh(); }
                    catch { notifyError(); await refresh().catch(() => {}); }
                }
            });
        });
    }
    const busyFavorites = new Set();
    document.addEventListener("click", async event => {
        const toggle = event.target.closest("[data-menu-search-toggle]");
        if (toggle) { event.preventDefault(); toggleSearch(toggle); return; }
        const close = event.target.closest("[data-menu-search-close]");
        if (close) {
            const row = close.closest("[data-menu-search-row]");
            const input = row?.querySelector("[data-menu-search-input]");
            if (input) { input.value = ""; input.focus(); }
            renderSearch(row?.querySelector("[data-menu-search-panel]"));
            return;
        }
        const groupButton = event.target.closest(".submenu-toggle");
        if (groupButton) {
            const group = groupButton.closest("[data-menu-group]");
            const nav = group?.closest(".menu-nav");
            if (!group || !nav) return;
            const opened = group.classList.toggle("open");
            groupButton.setAttribute("aria-expanded", String(opened));
            saveGroups(nav);
            return;
        }
        const more = event.target.closest("[data-favorites-more]");
        if (more) {
            const section = more.closest("[data-menu-favorites]");
            const opened = section.classList.toggle("is-expanded");
            more.setAttribute("aria-expanded", String(opened));
            more.textContent = opened ? "نمایش کمتر" : "نمایش بیشتر (" + section.querySelectorAll(".menu-favorite-overflow").length + ")";
            return;
        }
        const star = event.target.closest("[data-menu-favorite]");
        if (star) {
            event.preventDefault();
            const id = star.dataset.menuFavorite;
            if (!id || busyFavorites.has(id)) return;
            busyFavorites.add(id);
            try {
                await send("/Menu/Favorite", { menuId: id, favorite: !star.classList.contains("is-favorite") });
                await refresh();
            } catch { notifyError(); }
            finally { busyFavorites.delete(id); }
            return;
        }
        const searchResult = event.target.closest(".menu-search-result");
        if (searchResult) closeSearch(searchResult.closest("[data-menu-search-row]"));
        if (!event.target.closest("[data-menu-search-row]")) {
            document.querySelectorAll("[data-menu-search-row]").forEach(closeSearch);
        }
    });
    document.addEventListener("input", event => {
        const input = event.target.closest("[data-menu-search-input]");
        if (input) renderSearch(input.closest("[data-menu-search-panel]"));
    });
    document.addEventListener("keydown", event => {
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
            event.preventDefault();
            const mobile = window.matchMedia("(max-width: 767.98px)").matches;
            const sidebar = document.querySelector(mobile ? "#mobileDrawer" : "#desktopSidebar");
            const row = sidebar?.querySelector("[data-menu-search-row]");
            if (!row) return;
            if (!mobile && document.documentElement.classList.contains("sidebar-collapsed")) {
                if (!row.classList.contains("is-search-open")) {
                    toggleSearch(row.querySelector("[data-menu-search-toggle]"));
                } else {
                    row.querySelector("[data-menu-search-input]")?.focus();
                }
            } else {
                row.querySelector("[data-menu-search-input]")?.focus();
            }
        } else if (event.key === "Escape") {
            document.querySelectorAll("[data-menu-search-row]").forEach(closeSearch);
            document.activeElement?.blur?.();
        }
    });
    window.erpMenu = Object.freeze({ refresh, highlight });
    restoreGroups();
    highlight();
    initSortable();
    syncSettingsFavorites();
})();

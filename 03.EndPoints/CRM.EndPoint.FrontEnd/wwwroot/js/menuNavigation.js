/* Persistent ERP menu: delegated interactions survive Virtual DOM page swaps. */
(() => {
    "use strict";
    const navIds = ["desktopNavbar", "mobileDrawerNavbar"];
    const replaceIds = ["desktopNavbar", "desktopFixedActions", "mobileNavbar", "mobileDrawerNavbar", "mobileDrawerFixedActions"];
    const user = window.userContext?.id || "anonymous";
    const groupKey = id => "vazin:menu-groups:" + user + ":" + id;
    const normalizeText = s => (s || "").toLocaleLowerCase("fa")
        .replace(/[يى]/g, "ی").replace(/ك/g, "ک")
        .replace(/[\u200c\u200f\u200e]/g, " ").trim();
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
            restoreGroups();
            highlight();
            initSortable();
            for (const panel of document.querySelectorAll("[data-menu-search-panel]:not([hidden])")) renderSearch(panel);
        })().finally(() => { refreshing = null; });
        return refreshing;
    }
    function buildCandidates(panel) {
        const mobile = Boolean(panel.closest("#mobileDrawer"));
        const nav = document.getElementById(mobile ? "mobileDrawerNavbar" : "desktopNavbar");
        const footer = document.getElementById(mobile ? "mobileDrawerFixedActions" : "desktopFixedActions");
        if (!nav) return [];
        const links = [...nav.querySelectorAll(".menu-link[href]"), ...(footer?.querySelectorAll(".menu-link[href]") || [])];
        const seen = new Set();
        return links.filter(link => !link.closest("[data-menu-favorites]")).map(link => {
            const path = [];
            let group = link.closest("[data-menu-group]");
            while (group) {
                path.unshift(group.querySelector(":scope > .submenu-toggle .menu-item-title > span")?.textContent?.trim() || "");
                group = group.parentElement?.closest("[data-menu-group]");
            }
            const title = link.querySelector(".menu-item-title > span")?.textContent?.trim() || link.textContent.trim();
            return { title, route: path.filter(Boolean).join(" ← "), href: link.href };
        }).filter(item => {
            if (seen.has(item.href)) return false;
            seen.add(item.href); return true;
        });
    }
    function renderSearch(panel) {
        const results = panel.querySelector("[data-menu-search-results]");
        const query = normalizeText(panel.querySelector("[data-menu-search-input]")?.value);
        if (!results) return;
        results.replaceChildren();
        let candidates = buildCandidates(panel);
        if (query) candidates = candidates.filter(x => normalizeText(x.title + " " + x.route).includes(query));
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
    const closePanel = panel => {
        panel.hidden = true;
        const toggle = panel.closest(".menuHeader")?.querySelector("[data-menu-search-toggle]");
        toggle?.setAttribute("aria-expanded", "false");
    };
    function toggleSearch(toggle) {
        const panel = toggle.closest(".menuHeader")?.querySelector("[data-menu-search-panel]");
        if (!panel) return;
        const show = panel.hidden;
        document.querySelectorAll("[data-menu-search-panel]").forEach(closePanel);
        if (show) {
            panel.hidden = false;
            toggle.setAttribute("aria-expanded", "true");
            renderSearch(panel);
            panel.querySelector("[data-menu-search-input]")?.focus();
        }
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
        if (close) { closePanel(close.closest("[data-menu-search-panel]")); return; }
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
        if (searchResult) closePanel(searchResult.closest("[data-menu-search-panel]"));
        if (!event.target.closest(".menuHeader")) {
            document.querySelectorAll("[data-menu-search-panel]:not([hidden])").forEach(closePanel);
        }
    });
    document.addEventListener("input", event => {
        const input = event.target.closest("[data-menu-search-input]");
        if (input) renderSearch(input.closest("[data-menu-search-panel]"));
    });
    document.addEventListener("keydown", event => {
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
            event.preventDefault();
            const toggle = document.querySelector("#desktopSidebar [data-menu-search-toggle]") || document.querySelector("#mobileDrawer [data-menu-search-toggle]");
            if (toggle) toggleSearch(toggle);
        } else if (event.key === "Escape") {
            document.querySelectorAll("[data-menu-search-panel]:not([hidden])").forEach(closePanel);
        }
    });
    window.erpMenu = Object.freeze({ refresh, highlight });
    restoreGroups();
    highlight();
    initSortable();
})();

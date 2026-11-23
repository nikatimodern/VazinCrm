/* Live permission invalidation shared by all ERP pages. The server decides access. */
(() => {
    "use strict";

    const token = document.querySelector("#accessLiveToken input[name='__RequestVerificationToken']");
    if (!token) return;

    let state = null;
    let formDirty = false;
    let editorLocked = false;
    let generation = 0;
    let accessBanner = null;

    const moduleForPath = () => {
        const path = location.pathname.toLowerCase();
        if (/^\/customer(?:\/|$)/.test(path)) return "customer";
        if (/^\/(?:deal|overduedeal)(?:\/|$)/.test(path)) return "deal";
        return null;
    };
    const can = (entity, permission) => {
        const module = String(entity || "").toLowerCase();
        const key = String(permission || "");
        if (editorLocked) return false;
        const permissions = state?.[module];
        // If status has not been fetched, the existing server checks remain the authority.
        if (!permissions) return true;
        if (!Object.prototype.hasOwnProperty.call(permissions, key))
            return permissions.view !== 0; // Detail scope is fetched when its page opens.
        return Number(permissions[key]) > 0;
    };

    // Changed input in an actual form, not table searches/filters.
    const recordEdit = event => {
        const element = event.target;
        if (!element.matches?.("input, textarea, select")) return;
        if (element.matches("[type=search], [readonly], [disabled]")) return;
        if (!element.closest("form")) return;
        if (element.closest("#accessLiveToken")) return;
        formDirty = true;
    };
    document.addEventListener("input", recordEdit, true);
    document.addEventListener("change", recordEdit, true);
    // A discarded modal or cancelled person form is no longer an open draft.
    document.addEventListener("hidden.bs.modal", () => {
        if (!document.querySelector(".modal.show") &&
            document.getElementById("formAddCustom")?.style.display === "none")
            formDirty = false;
    });
    document.addEventListener("click", event => {
        if (event.target.closest?.("#btnCancel")) formDirty = false;
    });

    const hasOpenEditor = () =>
        formDirty ||
        !!document.querySelector(".modal.show form, #dealModal.show, #updateCustomerModal.show") ||
        (document.getElementById("formAddCustom")?.style.display !== "none" &&
         !!document.getElementById("formAddCustom"));

    function displayWarning(message, blocked = false) {
        if (!accessBanner) {
            accessBanner = document.createElement("div");
            accessBanner.id = "accessLiveBanner";
            accessBanner.setAttribute("role", "alert");
            accessBanner.setAttribute("dir", "rtl");
            Object.assign(accessBanner.style, {
                position: "fixed", top: "75px", left: "50%",
                transform: "translateX(-50%)", zIndex: "2147483000",
                background: "#fff8e8", color: "#633d0a",
                border: "1px solid #e5bc72", borderRadius: "9px",
                boxShadow: "0 3px 18px rgba(0,0,0,.15)",
                padding: "10px 14px", maxWidth: "min(94vw,630px)",
                display: "flex", flexWrap: "wrap", alignItems: "center",
                justifyContent: "space-between", gap: "10px", fontSize: "13px"
            });
            const label = document.createElement("span");
            label.dataset.accessLiveMessage = "";
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = "بارگذاری مجدد";
            Object.assign(button.style, {
                background: "#00665c", color: "#fff", border: "none",
                padding: "6px 12px", borderRadius: "6px", cursor: "pointer"
            });
            accessBanner.append(label, button);
            document.body.append(accessBanner);
        }
        accessBanner.querySelector("[data-access-live-message]").textContent = message;
        accessBanner.querySelector("button").textContent = blocked ? "بازگشت به داشبورد" : "بارگذاری مجدد";
        accessBanner.querySelector("button").onclick = blocked
            ? () => location.assign("/Dashboard/Index")
            : () => location.reload();
    }

    function lockVisibleActions(module, permissions) {
        // Preserve entered fields, but never allow submitting a stale form.
        editorLocked = true;
        if (module === "customer") {
            const create = document.getElementById("btnNew");
            if (create && !permissions?.create) create.disabled = true;
            const createSubmit = document.getElementById("btnIn");
            if (createSubmit) createSubmit.disabled = true;
            document.querySelectorAll("#updateCustomerModal .buttonGreen3").forEach(button => {
                button.disabled = true;
            });
        } else if (module === "deal") {
            document.querySelectorAll(".deal-create-button, #dealModal .buttonGreen3").forEach(button => {
                button.disabled = true;
            });
        }
    }

    // Inline onclick handlers and JS-driven modals still run through here.
    // Server actions independently check permissions; this only protects the open editor.
    document.addEventListener("submit", event => {
        if (!editorLocked || !moduleForPath()) return;
        event.preventDefault();
        event.stopImmediatePropagation();
    }, true);
    document.addEventListener("click", event => {
        if (!editorLocked || !moduleForPath()) return;
        const action = event.target.closest(
            "#btnIn, #updateCustomerModal .buttonGreen3, #dealModal .buttonGreen3, " +
            ".deal-create-button, #btnNew, button[type=submit]"
        );
        if (!action) return;
        event.preventDefault();
        event.stopImmediatePropagation();
    }, true);

    async function getState(refreshSession) {
        const endpoint = refreshSession ? "/AccessLive/Sync" : "/AccessLive/State";
        const module = moduleForPath();
        const response = await fetch(endpoint + (module ? "?module=" + encodeURIComponent(module) : ""), {
            method: refreshSession ? "POST" : "GET",
            credentials: "same-origin",
            cache: "no-store",
            headers: refreshSession
                ? { "RequestVerificationToken": token.value }
                : { "Accept": "application/json" }
        });
        if (!response.ok) throw new Error("Access state request failed: " + response.status);
        return response.json();
    }

    async function reconcile(event = { kind: "reconnect" }) {
        const version = ++generation;
        const previous = state;
        let latest;
        try {
            latest = await getState(true);
        } catch (error) {
            if (version !== generation) return;
            console.error("Could not synchronize live access.", error);
            editorLocked = true;
            displayWarning("سطح دسترسی تغییر کرده اما بررسی دوباره آن انجام نشد. برای ادامه صفحه را تازه‌سازی کنید.");
            return;
        }
        if (version !== generation) return;
        state = latest;

        const module = moduleForPath();
        const changed = JSON.stringify(previous) !== JSON.stringify(latest);
        const rolesChanged = !!previous &&
            JSON.stringify(previous.roles) !== JSON.stringify(latest.roles);
        const forceRefresh = event?.kind === "user" || event?.kind === "reconnect";
        if (!changed && !forceRefresh) return;

        window.dispatchEvent(new CustomEvent("erp:access-state", {
            detail: { previous, current: latest, module, reason: event?.kind }
        }));

        // The administrator page already reloads the management model on save.
        if (/^\/accesscatalogadmin(?:\/|$)/i.test(location.pathname)) return;

        const viewRevoked = module && latest[module]?.view === 0;
        if (!hasOpenEditor()) {
            if (viewRevoked) location.replace("/Dashboard/Index");
            else location.reload(); // New server HTML, cards, menu and scopes.
            return;
        }

        if (rolesChanged) window.dispatchEvent(new CustomEvent("erp:roles-refreshed"));
        if (module) lockVisibleActions(module, latest[module]);
        else editorLocked = true;
        displayWarning(viewRevoked
            ? "مجوز مشاهده این صفحه لغو شده است. اطلاعات فرم باز حفظ شده‌اند، اما ثبت و ارسال غیرفعال است."
            : "دسترسی شما تغییر کرده است. اطلاعات فرم باز حفظ شده‌اند؛ برای ادامه، صفحه را بارگذاری مجدد کنید.",
            !!viewRevoked);
    }

    // The shell uses in-place navigation; once a different page mounts, stale
    // editor locks and form-dirty flags must not be carried into that page.
    const workspace = document.getElementById("mainContent");
    if (workspace) {
        let lastPage = workspace.querySelector("[data-page-root]");
        let lastPageKey = lastPage?.dataset.pageKey;
        new MutationObserver(() => {
            const page = workspace.querySelector("[data-page-root]");
            const currentKey = page?.dataset.pageKey;
            if (page === lastPage && currentKey === lastPageKey) return;
            lastPage = page;
            lastPageKey = currentKey;
            formDirty = false;
            editorLocked = false;
            accessBanner?.remove();
            accessBanner = null;
            const version = ++generation;
            void getState(false).then(next => {
                if (version === generation) state = next;
            }).catch(error => console.warn("Could not refresh page access.", error));
        }).observe(workspace, { childList: true });
    }

    window.AccessLive = Object.freeze({
        can,
        isEditingLocked: () => editorLocked,
        refresh: () => reconcile({ kind: "reconnect" })
    });
    window.addEventListener("erp:access-changed", event => { void reconcile(event.detail); });
    window.addEventListener("erp:access-reconnected", () => { void reconcile({ kind: "reconnect" }); });

    // Get a baseline so a role/catalog change unrelated to this user does not
    // reload the tab unnecessarily.
    void getState(false).then(value => { if (!state) state = value; })
        .catch(error => console.warn("Live access baseline unavailable.", error));
})();

/**
 * Shared history presentation and destination selection for financial transfers.
 * Feature modules retain their HTTP contracts and document-specific actions.
 */
class ExternalFinancialHistory {
    constructor({ modal, list }) {
        this.modal = typeof modal === "string" ? document.getElementById(modal) : modal;
        this.list = typeof list === "string" ? document.getElementById(list) : list;
        if (!this.modal || !this.list) throw new Error("Financial history modal or list is missing.");
    }

    static element(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined && text !== null) node.textContent = String(text);
        return node;
    }

    static fields(entries) {
        const info = this.element("div", "external-pf-info");
        for (const [label, rawValue, accent, ltr] of entries) {
            const field = this.element("div", "external-pf-field");
            const display = rawValue === null || rawValue === undefined || rawValue === ""
                ? "—" : rawValue;
            const value = this.element("span", "external-pf-value" +
                (accent ? " external-pf-software" : ""), display);
            if (rawValue !== null && rawValue !== undefined && rawValue !== "")
                value.title = String(rawValue);
            if (ltr) value.dir = "ltr";
            field.append(this.element("span", "external-pf-label", label + ":"), value);
            info.append(field);
        }
        return info;
    }

    static action(label, { onClick, href, danger = false, icon } = {}) {
        const action = this.element(href ? "a" : "button", "external-pf-action" +
            (danger ? " external-pf-remove" : ""));
        if (href) {
            action.href = href;
            action.target = "_blank";
            action.rel = "noopener noreferrer";
        } else {
            action.type = "button";
            action.onclick = onClick;
        }
        if (icon) {
            const glyph = this.element("i", "fas " + icon);
            glyph.setAttribute("aria-hidden", "true");
            action.append(glyph);
        }
        action.append(label);
        return action;
    }

    static operations(actions) {
        const group = this.element("div", "external-pf-operations");
        const toggle = this.action("⋯");
        toggle.classList.add("external-pf-operations-toggle");
        toggle.setAttribute("aria-label", "عملیات سند");
        toggle.setAttribute("aria-expanded", "false");
        const menu = this.element("div", "external-pf-menu");
        menu.setAttribute("popover", "auto");
        menu.setAttribute("role", "group");
        menu.setAttribute("aria-label", "عملیات سند");
        toggle.popoverTargetElement = menu;
        menu.append(...actions);
        menu.addEventListener("beforetoggle", event => {
            const opening = event.newState === "open";
            toggle.setAttribute("aria-expanded", String(opening));
            if (!opening) return;
            const rect = toggle.getBoundingClientRect();
            menu.style.left = Math.max(8, Math.min(rect.left, document.documentElement.clientWidth - 168)) + "px";
            menu.style.top = rect.bottom + 4 + "px";
        });
        menu.addEventListener("toggle", event => {
            if (event.newState !== "open" || !menu.matches(":popover-open")) return;
            const rect = toggle.getBoundingClientRect();
            if (menu.getBoundingClientRect().bottom > window.innerHeight - 8)
                menu.style.top = Math.max(8, rect.top - menu.offsetHeight - 4) + "px";
        });
        menu.addEventListener("click", event => {
            if (!event.target.closest(".external-pf-action")) return;
            menu.hidePopover();
            toggle.focus({ preventScroll: true });
        }, true);
        group.addEventListener("keydown", event => {
            if (event.key !== "Escape" || !menu.matches(":popover-open")) return;
            event.preventDefault();
            event.stopPropagation();
            menu.hidePopover();
            toggle.focus({ preventScroll: true });
        });
        group.append(toggle, menu);
        return group;
    }

    static recordFields(fields, registration) {
        const documentFields = this.fields(fields);
        documentFields.classList.add("external-pf-record-fields");
        const registeredAt = registration?.date?.replace(/^(\S+) (\d{2}:\d{2})(?::\d{2})?$/, "$1 | $2");
        const metadata = this.fields([
            ["تاریخ ثبت", registeredAt, false, true],
            ["ثبت‌کننده", registration?.by],
        ]);
        metadata.classList.add("external-pf-record-fields", "external-pf-registration");
        return [documentFields, metadata];
    }

    static statusMessage(status) {
        const message = this.element("p", "external-pf-missing-message");
        const icon = this.element("i", "fas fa-exclamation-circle");
        icon.setAttribute("aria-hidden", "true");
        message.append(icon, this.element("span", null, status));
        return message;
    }

    static async confirmEdit(modal, question, details) {
        const content = this.element("div", "text-start");
        content.append(this.element("p", details.length ? "mb-2" : "mb-0", question));
        if (details.length) {
            const list = this.element("ul", "small mb-0");
            for (const detail of details) list.append(this.element("li", null, detail));
            content.append(list);
        }
        const result = await Swal.fire({
            target: modal, title: "تأیید ویرایش در مقصد", html: content, icon: "warning",
            showCancelButton: true, focusCancel: true, confirmButtonText: "تأیید و ذخیره", cancelButtonText: "انصراف",
        });
        return result.isConfirmed;
    }

    static card({ title, fields = [], registration, view, operations = [], action, related, status, missing = false }) {
        const card = this.element("article", "external-pf-card" + (missing ? " external-pf-missing" : ""));
        const header = this.element("header", "external-pf-card-header");
        header.append(this.element("strong", "external-pf-software", title));
        if (view || operations.length || action) {
            const controls = this.element("div", "external-pf-actions");
            if (view) controls.append(view);
            if (action) controls.append(action);
            if (operations.length) controls.append(this.operations(operations));
            header.append(controls);
        }
        card.append(header);
        if (status) card.append(this.statusMessage(status));
        card.append(...this.recordFields(fields, registration));
        if (related) card.append(this.related(related));
        return card;
    }

    static relatedItem({ title, fields = [], registration, view, action, operations = [], status, missing = false }) {
        const item = this.element("div", "external-pf-related-item" + (missing ? " external-pf-missing" : ""));
        const header = this.element("div", "external-pf-card-header");
        const caption = this.element("strong", "external-pf-related-title");
        const icon = this.element("i", "far fa-file-alt");
        icon.setAttribute("aria-hidden", "true");
        caption.append(icon, title);
        header.append(caption);
        if (operations.length) {
            const controls = this.element("div", "external-pf-actions");
            if (view) controls.append(view);
            if (action) controls.append(action);
            controls.append(this.operations(operations));
            header.append(controls);
        } else if (view || action) {
            header.append(view ?? action);
        }
        item.append(header);
        if (status) item.append(this.statusMessage(status));
        if (fields.length || registration) item.append(...this.recordFields(fields, registration));
        return item;
    }

    static related({ title, entries = [], load, action, open = false, emptyText = "سندی ثبت نشده است." }) {
        const section = this.element("section", "external-pf-related");
        const bar = this.element("div", "external-pf-related-bar");
        const hasPanel = Boolean(load || entries.length);
        const heading = this.element(hasPanel ? "button" : "span", "external-pf-related-caption", title);
        bar.append(heading);
        if (action) bar.append(action);
        section.append(bar);
        if (!hasPanel) return section;

        const panel = this.element("div", "external-pf-related-panel");
        panel.setAttribute("aria-live", "polite");
        const chevron = this.element("i", "fas fa-chevron-down");
        chevron.setAttribute("aria-hidden", "true");
        const count = load ? this.element("span", "external-pf-related-count") : null;
        heading.type = "button";
        heading.prepend(chevron);
        if (count) heading.append(count);
        section.append(panel);
        let loaded = !load, loading = false;
        const renderEntries = items => panel.replaceChildren(...(items.length ? items :
            [this.element("p", "external-pf-related-message", emptyText)]));
        if (loaded) renderEntries(entries);
        const setOpen = value => {
            panel.hidden = !value;
            heading.setAttribute("aria-expanded", String(value));
        };
        setOpen(open);
        const loadEntries = async () => {
            if (loaded || loading) return;
            loading = true;
            panel.replaceChildren(this.element("p", "external-pf-related-message", "در حال دریافت..."));
            try {
                const items = await load();
                if (!panel.isConnected) return;
                if (items) {
                    loaded = true;
                    count.textContent = items.length ? "(" + items.length + ")" : "";
                    renderEntries(items);
                } else {
                    const retry = this.action("تلاش دوباره", { onClick: loadEntries });
                    panel.replaceChildren(this.element("p", "external-pf-related-message", "دریافت اسناد مرتبط انجام نشد."), retry);
                }
            } finally {
                loading = false;
            }
        };
        heading.onclick = () => {
            setOpen(panel.hidden);
            if (!panel.hidden) void loadEntries();
        };
        if (open) queueMicrotask(loadEntries);
        return section;
    }

    /** Delete only a mapping whose destination document is rechecked as missing server-side. */
    static missingMappingAction({ modal, sourceId, softwareId, mappingId, softwareTitle, url, onDeleted }) {
        const button = this.element("button", "external-pf-action external-pf-remove", "حذف سابقه");
        button.type = "button";
        button.addEventListener("click", async () => {
            if (button.disabled) return;
            button.disabled = true;
            try {
                const confirmation = await Swal.fire({
                    target: modal,
                    title: "حذف سابقه ارسال",
                    text: "ارتباط این سند با " + softwareTitle + " از ERP حذف شود؟",
                    icon: "warning",
                    showCancelButton: true,
                    confirmButtonText: "حذف سابقه",
                    cancelButtonText: "انصراف",
                    focusCancel: true,
                });
                if (!confirmation.isConfirmed || !modal.isConnected) return;
                const token = modal.querySelector('[name="__RequestVerificationToken"]')?.value;
                if (!token) {
                    toastifyError("امکان تأیید امنیتی درخواست وجود ندارد.");
                    return;
                }
                const result = await sendApiRequest({
                    url, method: "POST", successMsg: null, showLoading: false,
                    headers: { RequestVerificationToken: token },
                    data: { sourceId, externalSoftwareId: softwareId, mappingId },
                });
                if (!result?.deleted) return;
                toastifySuccess("سابقه ارسال حذف شد.");
                await onDeleted?.();
            } finally {
                button.disabled = false;
            }
        });
        return button;
    }

    setTitle(title) {
        const heading = this.modal.querySelector(".modal-title");
        if (heading) heading.textContent = title;
    }

    show({ title, loadingText } = {}) {
        if (title) this.setTitle(title);
        if (loadingText !== undefined) this.message(loadingText);
        if (isModalOpen(this.modal)) return;
        const lifetime = new AbortController();
        const { signal } = lifetime;
        const closeMenu = () => this.list.querySelector(".external-pf-menu:popover-open")?.hidePopover();
        this.modal.querySelector(".modal-body")?.addEventListener("scroll", closeMenu, { signal });
        this.modal.addEventListener("hide.bs.modal", closeMenu, { signal });
        this.modal.addEventListener("hidden.bs.modal", () => lifetime.abort(), { once: true, signal });
        enableMomentumScrollScope(this.modal, { signal });
        ModalToggle(this.modal, "show");
    }

    message(value) {
        this.list.replaceChildren(this.constructor.element("div", "external-pf-empty", value));
    }

    render(records, { emptyText, card } = {}) {
        if (!records?.length) return this.message(emptyText ?? "سابقه‌ای ثبت نشده است.");
        const fragment = document.createDocumentFragment();
        for (const record of records) fragment.append(this.constructor.card(card(record)));
        this.list.replaceChildren(fragment);
    }

    /**
     * Keep the history modal underneath the destination picker. Only start the
     * next wizard after Bootstrap has fully hidden the chooser.
     * Returns an array of software IDs, or null when cancelled/unavailable.
     */
    static async chooseDestinations({ ids, mapType, signal } = {}) {
        const chooser = document.getElementById("chooseExternalSoftwareModal");
        if (!chooser || signal?.aborted || this.chooserPending || isModalOpen(chooser))
            return null;
        this.chooserPending = true;

        let selection = null;
        let settle;
        const completed = new Promise(resolve => { settle = resolve; });
        const listeners = new AbortController();
        let done = false;
        const finish = value => {
            if (done) return;
            done = true;
            listeners.abort();
            settle(value);
        };

        chooser.addEventListener("shown.bs.modal", () => {
            if (selection?.length) ModalToggle(chooser, "hide");
        }, { signal: listeners.signal });
        chooser.addEventListener("hidden.bs.modal", () => finish(selection),
            { signal: listeners.signal });
        signal?.addEventListener("abort", () => finish(null),
            { once: true, signal: listeners.signal });

        let result;
        try {
            result = await showExternalSoftwareModal(ids, {
                mapType,
                onConfirm: ({ externalSoftwareIds }) => {
                    selection = [...externalSoftwareIds];
                    if (isModalOpen(chooser)) ModalToggle(chooser, "hide");
                },
            });
        } catch (error) {
            finish(null);
            this.chooserPending = false;
            throw error;
        }
        if (!result) finish(null);
        try {
            return await completed;
        } finally {
            this.chooserPending = false;
        }
    }
}

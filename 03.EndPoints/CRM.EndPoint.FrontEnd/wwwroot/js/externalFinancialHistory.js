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

    static actions(elements) {
        const actions = this.element("div", "external-pf-actions");
        for (const action of elements) if (action) actions.append(action);
        return actions;
    }

    static card(entries, actionElements = [], extraClass = "") {
        const card = this.element("div", "external-pf-card" +
            (extraClass ? " " + extraClass : ""));
        card.append(this.fields(entries));
        if (actionElements?.length) card.append(this.actions(actionElements));
        return card;
    }

    setTitle(title) {
        const heading = this.modal.querySelector(".modal-title");
        if (heading) heading.textContent = title;
    }

    show({ title, loadingText } = {}) {
        if (title) this.setTitle(title);
        if (loadingText !== undefined) this.message(loadingText);
        if (!isModalOpen(this.modal)) ModalToggle(this.modal, "show");
    }

    message(value) {
        this.list.replaceChildren(this.constructor.element("div", "external-pf-empty", value));
    }

    render(records, { emptyText, fields, actions } = {}) {
        if (!records?.length) return this.message(emptyText ?? "سابقه‌ای ثبت نشده است.");
        const fragment = document.createDocumentFragment();
        for (const record of records) {
            const actionElements = actions?.(record) ?? [];
            fragment.append(this.constructor.card(fields(record), actionElements));
        }
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

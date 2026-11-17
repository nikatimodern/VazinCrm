/**
 * Shared UI shell for CRM -> financial-software transfer wizards.
 * Feature modules own requests, validation, navigation and persistence.
 */
class ExternalFinancialWizard {
    constructor({ modal, container, back, confirm }) {
        this.modal = typeof modal === "string" ? document.getElementById(modal) : modal;
        this.container = typeof container === "string" ? document.getElementById(container) : container;
        this.back = typeof back === "string" ? document.getElementById(back) : back;
        this.confirm = typeof confirm === "string" ? document.getElementById(confirm) : confirm;
        if (!this.modal || !this.container || !this.back || !this.confirm)
            throw new Error("ExternalFinancialWizard requires its modal, body and action buttons.");
        this.destinations = [];
        this.destinationIndex = 0;
    }

    /** Shared compact field markup for sale type, customer and other lookups. */
    static selectField({ id, label, options = [], selected = "", placeholder = null }) {
        const safe = value => escapeHtml(String(value ?? ""));
        return `<div class="external-financial-field">
            <label for="${safe(id)}" class="form-label mb-1">${safe(label)}</label>
            <select id="${safe(id)}" class="show-dropdown-arrow">
                ${placeholder !== null ? `<option value="">${safe(placeholder)}</option>` : ""}
                ${options.map(option => `<option value="${safe(option.value)}"
                    ${String(option.value) === String(selected ?? "") ? "selected" : ""}>
                    ${safe(option.label)}</option>`).join("")}
            </select>
        </div>`;
    }

    /** The CRM-side customer identity is intentionally identical across sales documents. */
    static crmCustomer(name) {
        return `<div class="external-mapping-entity">
            <span class="external-mapping-label">مشتری در CRM</span>
            <span class="external-mapping-entity-name fbold">${escapeHtml(String(name ?? "—"))}</span>
        </div>`;
    }

    /**
     * Bind an inventory selector to a selected Sepidar item. Stocks are loaded from
     * the existing item-specific endpoint, never from a global warehouse list.
     * `cache` belongs to one financial-software destination, not the entire app.
     */
    static linkedStockField(target, {
        id, softwareId, cache = new Map(), isCurrent = () => true,
        onChange = () => {}, onLoaded = () => {}, onLoading = () => {},
    }) {
        const label = document.createElement("label");
        label.className = "form-label external-item-match-stock-label";
        label.htmlFor = id;
        label.textContent = "انبار کالای انتخاب‌شده";

        const select = document.createElement("select");
        select.id = id;
        select.className = "external-mapping-stock-select show-dropdown-arrow";
        select.disabled = true;
        select.append(new Option("", ""));

        const hint = document.createElement("small");
        hint.className = "external-item-match-stock-hint";
        hint.textContent = "ابتدا کالای معادل را انتخاب کنید.";
        target.append(label, select, hint);

        const control = new TomSelect(select, {
            placeholder: "جستجو و انتخاب انبار",
            onChange,
        });
        control.disable();

        let generation = 0;
        const update = async (itemId, preferredStockId = "") => {
            const version = ++generation;
            control.clear(true);
            control.clearOptions();
            control.disable();
            hint.textContent = itemId
                ? "در حال دریافت انبارهای مرتبط…"
                : "ابتدا کالای معادل را انتخاب کنید.";
            if (!itemId) {
                onLoading(false);
                return;
            }

            onLoading(true);
            const key = String(itemId);
            let pending = cache.get(key);
            if (!pending) {
                pending = sendApiRequest({
                    url: "/Factor/PrepareExternalStockMapping",
                    method: "POST",
                    successMsg: null,
                    showLoading: false,
                    data: { externalSoftwareId: softwareId, itemIds: [Number(itemId)] },
                }).then(result => {
                    if (!Array.isArray(result?.stocksByItem)) return null;
                    return result.stocksByItem.find(item => String(item.itemRef) === key)?.stocks ?? [];
                }).catch(() => null);
                cache.set(key, pending);
            }

            const stocks = await pending;
            if (stocks === null) cache.delete(key);
            if (version !== generation || !select.isConnected || !isCurrent()) return;
            onLoading(false);
            if (stocks === null) {
                hint.textContent = "دریافت انبارها انجام نشد. کالا را دوباره انتخاب کنید.";
                return;
            }

            onLoaded(key, stocks);
            if (!stocks.length) {
                hint.textContent = "انبار مرتبطی برای این کالا پیدا نشد.";
                return;
            }

            control.addOptions(stocks.map(stock => ({
                value: String(stock.id),
                text: [stock.code, stock.title].filter(Boolean).join(" - "),
            })));
            control.refreshOptions(false);
            control.enable();

            const selected = stocks.some(stock => String(stock.id) === String(preferredStockId))
                ? String(preferredStockId) : "";
            control.setValue(selected, true);
            if (!selected && preferredStockId) onChange("");
            hint.textContent = "";
        };

        return { update, select };
    }

    setDestinations(ids) {
        this.destinations = [...new Set(ids ?? [])];
        this.destinationIndex = 0;
    }

    get currentDestination() {
        return this.destinations[this.destinationIndex] ?? null;
    }

    get hasNextDestination() {
        return this.destinationIndex + 1 < this.destinations.length;
    }

    nextDestination() {
        if (!this.hasNextDestination) return null;
        return this.destinations[++this.destinationIndex];
    }

    get positionLabel() {
        return `نرم‌افزار ${this.destinationIndex + 1} از ${this.destinations.length}`;
    }

    clearSelects() {
        this.container.querySelectorAll("select").forEach(select => select.tomselect?.destroy());
    }

    render({ destination, position, title, subtitle, icon, step, content, large = false }) {
        this.clearSelects();
        renderExternalFinancialStage(this.modal, this.container, {
            destination,
            position: position ?? this.positionLabel,
            title, subtitle, icon, step, content, large,
        });
    }

    setActions({ confirmText, backText, confirmDisabled, backDisabled, backHidden } = {}) {
        if (confirmText !== undefined) this.confirm.textContent = confirmText;
        if (backText !== undefined) {
            const caption = this.back.querySelector("[data-external-mapping-back-label]");
            if (caption) caption.textContent = backText;
        }
        if (backHidden !== undefined) this.back.hidden = backHidden;
        if (confirmDisabled !== undefined) toggleElementsDisabled(this.confirm, confirmDisabled);
        if (backDisabled !== undefined) toggleElementsDisabled(this.back, backDisabled);
    }

    destroy() {
        this.clearSelects();
        this.modal.querySelector(".modal-dialog")?.classList.remove("modal-lg");
        this.destinations = [];
        this.destinationIndex = 0;
    }
}

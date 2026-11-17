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

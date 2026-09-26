// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


var report = {};
var oldReport = null;
let dealIdReport = null;
let dealSerialNumberReport = null;
let typeReportSubmit = 0;
var ser;
var baseUrl;
if ("@Url.Action(\"\", \"\")" == "/") {
    
    baseUrl = "@Url.Action(\"\", \"\")";
} else {
    
    baseUrl = "@Url.Action(\"\", \"\")/";
}


document.addEventListener("DOMContentLoaded", () => {
    Mask();
    MaskDate();
});

/**
 * Sends HTTP API requests with automatic loading management, toast notifications, and success callbacks.
 *
 * @async
 * @param {Object} options - Request configurations.
 * @param {string} options.url - Target API endpoint URL.
 * @param {"GET"|"POST"|"PUT"|"DELETE"|"PATCH"} [options.method='GET'] - HTTP method.
 * @param {Object|Array|FormData|null} [options.data=null] - Request body payload (for POST, PUT, PATCH).
 * @param {Object|null} [options.params=null] - URL query parameters (`?key=value`) for any HTTP method.
 * @param {Object|null} [options.headers=null] - Custom HTTP request headers.
 * @param {"json"|"blob"|"arraybuffer"|"text"|"document"|"stream"} [options.responseType="json"] - Expected response type.
 * @param {Function|null} [options.onSuccess=null] - Success callback function `(response) => {}`.
 * @param {string|null} [options.successMsg=undefined] - Success toast message (set empty string "" or null to disable).
 * @param {boolean} [options.showLoading=true] - Toggle loading indicator display.
 * @returns {Promise<Object|null>} Returns response data on success, or null on failure.
 */
async function sendApiRequest(
    {
        url,
        method = "GET",
        data = null,
        params = null,
        headers = null,
        responseType = "json",
        onSuccess = null,
        onError = null,
        successMsg = undefined,
        showLoading = true,
    }) {
    try {
        
        method = method.toUpperCase();
        
        if (successMsg === undefined) {
            successMsg = {
                GET: null,
                POST: "با موفقیت ثبت شد",
                PUT: "با موفقیت ویرایش شد",
                DELETE: "با موفقیت حذف شد",
                PATCH: "با موفقیت ویرایش شد",
            }[method] ?? null;
        }
        
        if (showLoading && typeof FLshowLoading === "function") {
            FLshowLoading(true);
        }
        
        const config = {
            url,
            method,
            responseType,
            headers: {
                "X-Requested-With": "XMLHttpRequest",
                "X-Current-Page": window.location.pathname + window.location.search,
                ...(headers ?? {}),
            },
        };
        
        if (params !== null) {
            config.params = params;
        }
        
        if (data !== null) {
            config.data = data;
        }
        
        const response = await axios(config);
        
        if (response.status === 200) {
            if (successMsg && typeof toastifySuccess === "function") {
                toastifySuccess(successMsg);
            }
            
            if (typeof onSuccess === "function") {
                await onSuccess(response);
            }
            
            return response.data;
        }
        
    } catch (err) {
        const response = err?.response;
        
        if (response?.status === 401 && response.headers?.["x-auth-expired"] === "true") {
            const loginUrl = response?.headers?.["x-login-url"];
            location.href = loginUrl || "/Account/Login";
            return null;
        }
        
        if (typeof onError === "function") {
            await onError(err);
        } else {
            await handleApiError(err);
        }
        
        return null;
    } finally {
        if (showLoading && typeof FLshowLoading === "function") {
            FLshowLoading(false);
        }
    }
}

async function handleApiError(err) {
    console.error("API Error:", err);
    const response = err?.response;
    const status = err?.status || response?.status || "";
    
    const contentType = response?.headers?.["content-type"] ?? "";
    
    let errorMessage;
    
    if (status >= 500 || contentType.includes("text/html")) {
        errorMessage = null;
    } else if (response?.data instanceof Blob) {
        errorMessage = await response.data.text();
    } else if (typeof response?.data === "string") {
        errorMessage = response.data;
    } else if (response?.data?.message) {
        errorMessage = response.data.message;
    } else if (err?.responseText) {
        errorMessage = err.responseText;
    }
    
    errorMessage = errorMessage || `خطایی در ارتباط با سرور رخ داده است${status ? ` (${status})` : ""}`;
    
    if (typeof toastifyError === "function") {
        toastifyError(errorMessage);
    }
}

/**
 * Provides a reusable modal-based selector component for loading,
 * searching, selecting, restoring, and submitting items.
 *
 * Supports both checkbox and radio inputs and allows the caller
 * to define custom behavior through callbacks.
 */
class ModalSelector {
    
    /**
     * Creates a new ModalSelector instance.
     *
     * @param {Object} options
     * @param {string} [options.modal="#modalSelector"] Modal element selector.
     * @param {string} [options.title="#modalSelectorTitle"] Title element selector.
     * @param {string} [options.search="#modalSelectorSearch"] Search input selector.
     * @param {string} [options.container="#modalSelectorContainer"] Container selector.
     * @param {string} [options.submit="#modalSelectorSubmit"] Submit button selector.
     * @param {string} [options.cancel="#modalSelectorCancel"] Cancel button selector.
     */
    constructor(options = {}) {
        this.modal = document.querySelector(options.modal ?? "#modalSelector");
        
        if (!this.modal) {
            return;
        }
        
        this.titleElement = this.modal.querySelector(options.title ?? "#modalSelectorTitle");
        this.searchInput = this.modal.querySelector(options.search ?? "#modalSelectorSearch");
        this.container = this.modal.querySelector(options.container ?? "#modalSelectorContainer");
        this.submitButton = this.modal.querySelector(options.submit ?? "#modalSelectorSubmit");
        this.cancelButton = this.modal.querySelector(options.cancel ?? "#modalSelectorCancel");
        
        this.tempSelectedIds = [];
        this.tempSelectedMap = new Map();
        
        this.config = null;
        
        this.bindEvents();
    }
    
    
    /**
     * Opens the selector modal using the provided configuration.
     *
     * @param {Object} config Selector configuration.
     * @param {string} [config.title] Modal title.
     * @param {Array<string|number>} [config.selectedIds=[]] Initially selected IDs.
     * @param {Function} config.load Function responsible for loading the modal content.
     * @param {Function} [config.onSubmit] Callback executed after successful submission.
     * @param {Function} [config.onCancel] Callback executed when the modal is canceled.
     * @param {Function} [config.onSelectionChange] Callback executed whenever selection changes.
     * @param {boolean} [config.clearSearch=true] Whether to clear the search input when opening.
     */
    async open(config) {
        
        if (!config || typeof config.load !== "function") {
            throw new Error(
                "ModalSelector: 'load' callback is required.",
            );
        }
        
        this.config = config;
        
        this.tempSelectedIds = [...(config.selectedIds ?? [])].map(String);
        
        this.tempSelectedMap.clear();
        if (this.titleElement)
            this.titleElement.textContent = config.title ?? "";
        
        if (config.clearSearch !== false)
            this.searchInput.value = "";
        
        this.updateSubmitButton();
        
        await this.load();
        
        ModalToggle(this.modal);
    }
    
    
    /**
     * Loads selector content using the configured load callback.
     *
     * @param {string} [search=""] Search value.
     * @returns {Promise<void>}
     */
    async load(search = "") {
        if (!this.config?.load) {
            throw new Error("ModalSelector: no active configuration. Call open() before loading.");
        }
        
        const result = await this.config.load({
            search,
            selectedIds: [...this.tempSelectedIds],
        });
        
        this.container.innerHTML = typeof result === "string" ? result : result?.data ?? "";
        
        this.restoreSelected();
    }
    
    
    /**
     * Binds all modal events.
     *
     * @returns {void}
     */
    bindEvents() {
        
        this.container.addEventListener("change", event => {
            
            const input = event.target.closest("input[type='checkbox'], input[type='radio']");
            
            if (!input) return;
            
            if (input.type === "radio")
                this.handleRadio(input);
            else
                this.handleCheckbox(input);
            
            this.updateSubmitButton();
            
            this.config?.onSelectionChange?.(this.getSelection());
        });
        
        
        this.searchInput.addEventListener("keyup", async event => {
            if (event.key !== "Enter") return;
            
            toggleElementsDisabled(event.target);
            
            try {
                await this.load(event.target.value.trim());
            } finally {
                toggleElementsDisabled(event.target, false);
                
                event.target.focus();
            }
        });
        
        
        this.submitButton.addEventListener("click", () => this.submit());
        this.cancelButton.addEventListener("click", () => this.cancel());
    }
    
    
    /**
     * Handles selection of a radio input.
     *
     * @param {HTMLInputElement} input Selected radio input.
     * @returns {void}
     */
    handleRadio(input) {
        this.tempSelectedIds = [String(input.value)];
        
        this.tempSelectedMap.clear();
        
        this.setSelectedItem(input);
    }
    
    
    /**
     * Handles selection or deselection of a checkbox.
     *
     * @param {HTMLInputElement} input Changed checkbox input.
     * @returns {void}
     */
    handleCheckbox(input) {
        const id = String(input.value);
        
        if (input.checked) {
            
            if (!this.tempSelectedIds.includes(id)) {
                this.tempSelectedIds.push(id);
                this.setSelectedItem(input);
            }
            
            return;
        }
        
        const index = this.tempSelectedIds.indexOf(id);
        
        if (index !== -1)
            this.tempSelectedIds.splice(index, 1);
        
        this.tempSelectedMap.delete(id);
    }
    
    
    /**
     * Stores metadata for a selected input.
     *
     * @param {HTMLInputElement} input Selected input.
     * @returns {void}
     */
    setSelectedItem(input) {
        
        this.tempSelectedMap.set(
            String(input.value),
            input.dataset.number ?? input.value,
        );
    }
    
    
    /**
     * Restores selected items after the content has been reloaded.
     *
     * @returns {void}
     */
    restoreSelected() {
        
        this.container.querySelectorAll("input[type='checkbox'], input[type='radio']").forEach(input => {
            const id = String(input.value);
            
            if (!this.tempSelectedIds.includes(id)) return;
            
            input.checked = true;
            this.setSelectedItem(input);
        });
        
        this.updateSubmitButton();
    }
    
    
    /**
     * Updates the visibility of the submit button based on the current selection.
     *
     * @returns {void}
     */
    updateSubmitButton() {
        this.submitButton.classList.toggle("d-none", this.tempSelectedIds.length === 0);
    }
    
    
    /**
     * Returns the current temporary selection.
     *
     * @returns {{ids: string[], map: Map<string, string>, numbers: string[]}}
     */
    getSelection() {
        
        return {
            ids: [...this.tempSelectedIds],
            map: new Map(this.tempSelectedMap),
            numbers: [...this.tempSelectedMap.values()],
        };
    }
    
    
    /**
     * Submits the current selection and executes the configured submit callback.
     *
     * @returns {void}
     */
    submit() {
        if (this.tempSelectedIds.length === 0) return;
        
        const selection = this.getSelection();
        this.config?.onSubmit?.(selection);
        
        ModalToggle(this.modal);
    }
    
    
    /**
     * Cancels the current selection and closes the modal.
     *
     * @returns {void}
     */
    cancel() {
        const selection = this.getSelection();
        this.config?.onCancel?.(selection);
        
        ModalToggle(this.modal);
    }
    
    /**
     * Resets the temporary selection state and clears the current selection.
     *
     * @returns {void}
     */
    reset() {
        this.tempSelectedIds = [];
        this.tempSelectedMap.clear();
        this.config = null;
        
        if (this.searchInput)
            this.searchInput.value = "";
        
        this.updateSubmitButton();
    }
}


class SelectionManager {
    /**
     * @param {Object} options
     * @param {string|HTMLElement} options.container
     * @param {string|HTMLElement} [options.selectionBar=".selection-bar"]
     * @param {string|HTMLElement} [options.toggleBtn=".selection-toggle-btn"]
     * @param {string} [options.itemSelector=".selection-item"]
     * @param {string|null} [options.checkboxSelector=".selection-checkbox"]
     * @param {(item: HTMLElement) => string|number} [options.getItemKey]
     * @param {(item: HTMLElement) => boolean} [options.isSelectable]
     * @param {() => Iterable<HTMLElement>} [options.itemsProvider]
     * @param {string} [options.selectedClass="selected"]
     * @param {string} [options.selectionModeClass="selection-mode"]
     * @param {boolean} [options.enableShiftSelect=true] - پشتیبانی از Shift+Click
     * @param {(selectedIds: (string|number)[], instance: SelectionManager) => void|Promise<void>} [options.onAction]
     * @param {(instance: SelectionManager) => void} [options.onSelectionModeEnter]
     * @param {(instance: SelectionManager) => void} [options.onSelectionModeExit]
     * @param {(selectedIds: (string|number)[], instance: SelectionManager) => void} [options.onSelectionChanged]
     */
    constructor(options = {}) {
        this.options = {
            container: null,
            selectionBar: ".selection-bar",
            toggleBtn: ".selection-toggle-btn",
            itemSelector: ".selection-item",
            checkboxSelector: ".selection-checkbox",
            getItemKey: item => item.dataset.id,
            selectedClass: "selected",
            selectionModeClass: "selection-mode",
            enableShiftSelect: false,
            onAction: null,
            onSelectionModeEnter: null,
            onSelectionModeExit: null,
            onSelectionChanged: null,
            isSelectable: null,
            itemsProvider: null,
            ...options,
        };
        
        this.selectedIds = new Set();
        this.selectionMode = false;
        this.lastClickedItem = null; // برای پشتیبانی از Shift + Click
        
        // ذخیره رفرنس Handlerها برای پاک‌سازی راحت در destroy
        this._handlers = {};
        
        this.init();
    }
    
    // ---------------------------------------------------------
    // Elements
    // ---------------------------------------------------------
    
    resolveElement(elementOrSelector) {
        if (elementOrSelector instanceof HTMLElement) return elementOrSelector;
        if (typeof elementOrSelector !== "string") return null;
        return document.querySelector(elementOrSelector);
    }
    
    get container() {
        const element = this.resolveElement(this.options.container);
        if (!element) {
            throw new Error("SelectionManager: container element not found.");
        }
        return element;
    }
    
    get selectionBar() {
        return this.resolveElement(this.options.selectionBar);
    }
    
    get toggleBtn() {
        return this.resolveElement(this.options.toggleBtn);
    }
    
    get cancelBtn() {
        return this.selectionBar?.querySelector(".cancel-selection");
    }
    
    get clearBtn() {
        return this.selectionBar?.querySelector(".clear-selection");
    }
    
    get selectAll() {
        return this.selectionBar?.querySelector(".select-all");
    }
    
    get selectedCounts() {
        return this.selectionBar?.querySelectorAll(".selected-count") ?? [];
    }
    
    get actionBtn() {
        return this.selectionBar?.querySelector(".selection-action-btn");
    }
    
    // ---------------------------------------------------------
    // Initialization & Event Binding
    // ---------------------------------------------------------
    
    init() {
        this.bindEvents();
        this.decorateItems();
        this.updateUI();
    }
    
    bindEvents() {
        
        this._handlers.documentClick = (e) => {
            const btn = e.target.closest(this.options.toggleBtn);
            if (!btn) return;
            this.showSelectionMode();
        };
        document.addEventListener("click", this._handlers.documentClick);
        
        this._handlers.cancelClick = () => this.hideSelectionMode();
        this._handlers.clearClick = () => this.clearSelection();
        this._handlers.selectAllChange = () => this.toggleSelectAll();
        this._handlers.actionClick = async () => {
            if (typeof this.options.onAction === "function") {
                await this.options.onAction(this.getSelectedIds(), this);
            }
        };
        
        this.cancelBtn?.addEventListener("click", this._handlers.cancelClick);
        this.clearBtn?.addEventListener("click", this._handlers.clearClick);
        this.selectAll?.addEventListener("change", this._handlers.selectAllChange);
        this.actionBtn?.addEventListener("click", this._handlers.actionClick);
        
        
        this._handlers.containerClick = (e) => this.handleItemClick(e);
        this.container.addEventListener("click", this._handlers.containerClick);
    }
    
    destroy() {
        document.removeEventListener("click", this._handlers.documentClick);
        this.cancelBtn?.removeEventListener("click", this._handlers.cancelClick);
        this.clearBtn?.removeEventListener("click", this._handlers.clearClick);
        this.selectAll?.removeEventListener("change", this._handlers.selectAllChange);
        this.actionBtn?.removeEventListener("click", this._handlers.actionClick);
        this.container?.removeEventListener("click", this._handlers.containerClick);
        
        this.selectedIds.clear();
        this._handlers = {};
    }
    
    // ---------------------------------------------------------
    // Items & Keys
    // ---------------------------------------------------------
    
    getItems() {
        if (typeof this.options.itemsProvider === "function") {
            const result = this.options.itemsProvider();
            return Array.from(result ?? []);
        }
        return Array.from(this.container.querySelectorAll(this.options.itemSelector));
    }
    
    getItemKey(item) {
        const key = this.options.getItemKey?.(item);
        if (key === undefined || key === null || key === "") {
            return null;
        }
        return String(key);
    }
    
    #getSelectableItems() {
        return this.getItems().filter(item => this.#isItemSelectable(item));
    }
    
    #isItemSelectable(item) {
        if (typeof this.options.isSelectable === "function") {
            return this.options.isSelectable(item);
        }
        if (this.options.checkboxSelector) {
            return !!item.querySelector(this.options.checkboxSelector);
        }
        return true;
    }
    
    // ---------------------------------------------------------
    // Selection Mode
    // ---------------------------------------------------------
    
    #toggleSelectionMode(enable) {
        if (this.selectionMode === enable) return;
        
        this.selectionMode = enable;
        this.container.classList.toggle(this.options.selectionModeClass, enable);
        this.selectionBar?.classList.toggle("d-none", !enable);
        this.toggleBtn?.classList.toggle("d-none", enable);
        
        if (enable) {
            this.options.onSelectionModeEnter?.(this);
        } else {
            this.options.onSelectionModeExit?.(this);
        }
        
        this.updateUI();
    }
    
    showSelectionMode() {
        this.#toggleSelectionMode(true);
    }
    
    hideSelectionMode() {
        this.#toggleSelectionMode(false);
        this.clearSelection();
    }
    
    // ---------------------------------------------------------
    // Item Interaction
    // ---------------------------------------------------------
    
    handleItemClick(e) {
        if (!this.selectionMode) return;
        
        const item = e.target.closest(this.options.itemSelector);
        if (!item || !this.container.contains(item) || !this.#isItemSelectable(item)) return;
        
        const isCheckboxClick = this.options.checkboxSelector && e.target.closest(this.options.checkboxSelector);
        const key = this.getItemKey(item);
        if (!key) return;
        
        // پشتیبانی از Range Selection با کلید Shift
        if (this.options.enableShiftSelect && e.shiftKey && this.lastClickedItem) {
            this.#handleShiftSelect(item);
            this.lastClickedItem = item;
            return;
        }
        
        let isChecked;
        if (this.options.checkboxSelector) {
            const checkbox = item.querySelector(this.options.checkboxSelector);
            if (!checkbox) return;
            
            if (!isCheckboxClick) {
                checkbox.checked = !checkbox.checked;
            }
            isChecked = checkbox.checked;
        } else {
            isChecked = !this.selectedIds.has(key);
        }
        
        this.toggleItem(item, isChecked);
        this.lastClickedItem = item;
    }
    
    #handleShiftSelect(targetItem) {
        const selectableItems = this.#getSelectableItems();
        const startIdx = selectableItems.indexOf(this.lastClickedItem);
        const endIdx = selectableItems.indexOf(targetItem);
        
        if (startIdx === -1 || endIdx === -1) return;
        
        const min = Math.min(startIdx, endIdx);
        const max = Math.max(startIdx, endIdx);
        
        const targetKey = this.getItemKey(targetItem);
        const shouldSelect = !this.selectedIds.has(targetKey);
        
        for (let i = min; i <= max; i++) {
            this.toggleItem(selectableItems[i], shouldSelect, false);
        }
        
        this.updateUI();
    }
    
    toggleItem(item, isSelected, update = true) {
        const key = this.getItemKey(item);
        if (!key) return;
        
        item.classList.toggle(this.options.selectedClass, isSelected);
        
        if (this.options.checkboxSelector) {
            const checkbox = item.querySelector(this.options.checkboxSelector);
            if (checkbox) {
                checkbox.checked = isSelected;
            }
        }
        
        if (isSelected) {
            this.selectedIds.add(key);
        } else {
            this.selectedIds.delete(key);
        }
        
        if (update) {
            this.updateUI();
        }
    }
    
    // ---------------------------------------------------------
    // Select All / Deselect All
    // ---------------------------------------------------------
    
    toggleSelectAll() {
        const selectAll = this.selectAll;
        if (!selectAll) return;
        
        const isChecked = selectAll.checked;
        const items = this.#getSelectableItems();
        
        items.forEach(item => this.toggleItem(item, isChecked, false));
        this.updateUI();
    }
    
    selectAllItems() {
        const items = this.#getSelectableItems();
        items.forEach(item => this.toggleItem(item, true, false));
        this.updateUI();
    }
    
    deselectAllItems() {
        const items = this.#getSelectableItems();
        items.forEach(item => this.toggleItem(item, false, false));
        this.updateUI();
    }
    
    // ---------------------------------------------------------
    // Selection Management
    // ---------------------------------------------------------
    
    clearSelection() {
        this.selectedIds.clear();
        this.getItems().forEach(item => {
            item.classList.remove(this.options.selectedClass);
            if (this.options.checkboxSelector) {
                const checkbox = item.querySelector(this.options.checkboxSelector);
                if (checkbox) checkbox.checked = false;
            }
        });
        this.updateUI();
    }
    
    restoreSelection() {
        if (!this.selectionMode) return;
        
        this.getItems().forEach(item => {
            const key = this.getItemKey(item);
            const isSelected = key ? this.selectedIds.has(key) : false;
            
            item.classList.toggle(this.options.selectedClass, isSelected);
            
            if (this.options.checkboxSelector) {
                const checkbox = item.querySelector(this.options.checkboxSelector);
                if (checkbox) checkbox.checked = isSelected;
            }
        });
        
        this.updateUI();
    }
    
    // ---------------------------------------------------------
    // UI Update
    // ---------------------------------------------------------
    
    updateUI() {
        const selectableItems = this.#getSelectableItems();
        
        const selectedVisibleCount = selectableItems.filter(item => {
            const key = this.getItemKey(item);
            return key && this.selectedIds.has(key);
        }).length;
        
        const totalSelected = this.selectedIds.size;
        
        this.selectedCounts.forEach(element => {
            element.textContent = totalSelected;
        });
        
        if (this.actionBtn) {
            this.actionBtn.disabled = totalSelected === 0;
        }
        
        if (this.selectAll) {
            const allSelected = selectableItems.length > 0 && selectedVisibleCount === selectableItems.length;
            const someSelected = selectedVisibleCount > 0 && selectedVisibleCount < selectableItems.length;
            
            this.selectAll.checked = allSelected;
            this.selectAll.indeterminate = someSelected;
        }
        
        this.options.onSelectionChanged?.(this.getSelectedIds(), this);
    }
    
    // ---------------------------------------------------------
    // Decoration & Refresh
    // ---------------------------------------------------------
    
    decorateItems() {
        this.getItems().forEach(item => {
            const selectable = this.#isItemSelectable(item);
            item.classList.toggle("selection-enabled", selectable);
        });
    }
    
    refresh() {
        this.decorateItems();
        if (this.selectionMode) {
            this.restoreSelection();
        } else {
            this.updateUI();
        }
    }
    
    // ---------------------------------------------------------
    // Public API
    // ---------------------------------------------------------
    
    getSelectedIds() {
        return Array.from(this.selectedIds);
    }
    
    hasSelection() {
        return this.selectedIds.size > 0;
    }
    
    isSelected(item) {
        const key = this.getItemKey(item);
        return key ? this.selectedIds.has(key) : false;
    }
    
    setSelectedIds(ids = []) {
        if (!Array.isArray(ids)) {
            throw new TypeError("SelectionManager: ids must be an array.");
        }
        // کست تمام IDها به String جهت تضمین همخوانی
        this.selectedIds = new Set(ids.map(String));
        this.restoreSelection();
    }
    
    addSelection(id) {
        if (id === undefined || id === null || id === "") {
            throw new Error("SelectionManager: invalid selection id.");
        }
        this.selectedIds.add(String(id));
        this.restoreSelection();
    }
    
    removeSelection(id) {
        this.selectedIds.delete(String(id));
        this.restoreSelection();
    }
}

class Dropdown {
    /**
     * @param {{button: Element, content: Element, effect: string, transitionDuration: number, placement: string, zIndex: number}} options
     * @param {HTMLElement} options.button
     * @param {HTMLElement|string} options.content
     * @param {HTMLElement} [options.portal]
     * @param {number} [options.transitionDuration]
     * @param {number} [options.zIndex]
     * @param {string} [options.effect]
     * @param {string} [options.originalDisplay]
     * @param {string} [options.placement]
     */
    constructor(options) {
        this.id = options.id;
        this.button = options.button;
        this.content = options.content instanceof HTMLElement ? options.content : document.querySelector(options.content);
        this.portal = options.portal || document.getElementById("dropdown-portal");
        this.transitionDuration = options.transitionDuration || 300;
        this.zIndex = options.zIndex || 10;
        this.effect = options.effect || "fade";
        this.originalDisplay = options.originalDisplay || "block";
        this.placement = options.placement || "left";
        
        this.container = this.findScrollContainer(this.button);
        this.isContainerBody = this.container === document.body;
        
        this.isOpen = false;
        this.content.style.display = "none";
        if (!this.portal.contains(this.content)) {
            this.portal.appendChild(this.content);
        }
        
        this.content.classList.add("dropdown-content");
        this.content.classList.add(this.effect);
        this.content.style.transitionDuration = `${this.transitionDuration}ms`;
        this.content.style.zIndex = this.zIndex;
        
        this.boundPositionDropdown = this.positionDropdown.bind(this);
        
    }
    
    static setup({ toggle, content, onOpen, closeOnOutsideClick = true, options = {} }) {
        if (!toggle) return;
        if (toggle.dropdownInstance) return toggle.dropdownInstance;
        
        toggle.addEventListener("click", async (e) => {
            e.stopPropagation();
            
            toggle.dropdownInstance ??= new Dropdown({
                button: toggle,
                content,
                ...options,
            });
            
            await onOpen?.({
                toggle,
                dropdown: toggle.dropdownInstance,
                content,
            });
            
            toggle.dropdownInstance.toggle();
        });
        
        if (closeOnOutsideClick) {
            document.addEventListener("click", e => {
                if (toggle.dropdownInstance?.isOpen && !content.contains(e.target) && !toggle.contains(e.target)) {
                    toggle.dropdownInstance.close();
                }
            });
        }
        
        return {
            close() {
                toggle.dropdownInstance?.close();
            },
            
            get dropdown() {
                return toggle.dropdownInstance ?? null;
            },
            
            get toggle() {
                return toggle;
            },
        };
    }
    
    
    static setupDelegate({ container, toggleSelector, getContent = toggle => toggle.nextElementSibling, onOpen, options = {} }) {
        const state = {
            activeToggle: null,
            
            close() {
                this.activeToggle?.dropdownInstance?.close();
                this.activeToggle = null;
            },
            
            get dropdown() {
                return this.activeToggle?.dropdownInstance ?? null;
            },
            
            get toggle() {
                return this.activeToggle;
            },
        };
        
        container?.addEventListener("click", async (e) => {
            const toggle = e.target.closest(toggleSelector);
            if (!toggle) {
                state.close();
                return;
            }
            
            if (state.toggle && state.toggle !== toggle && state.dropdown?.isOpen) {
                state.close();
            }
            
            if (!toggle.dropdownInstance) {
                toggle.dropdownInstance = new Dropdown({
                    button: toggle,
                    content: getContent(toggle),
                    ...options,
                });
                
                await onOpen?.({
                    toggle,
                    dropdown: toggle.dropdownInstance,
                    content: toggle.dropdownInstance.content,
                    state,
                });
            }
            toggle.dropdownInstance.toggle();
            
            state.activeToggle = toggle.dropdownInstance.isOpen ? toggle : null;
        });
        
        return state;
    }
    
    
    open() {
        if (this.isOpen) return;
        this.isOpen = true;
        
        this.content.style.display = this.originalDisplay;
        this.positionDropdown();
        
        requestAnimationFrame(() => {
            this.content.classList.add("show");
        });
        
        const targetElement = this.isContainerBody ? window : this.container;
        targetElement.addEventListener("scroll", this.boundPositionDropdown, true);
        
        window.addEventListener("resize", this.boundPositionDropdown);
    }
    
    close() {
        if (!this.isOpen) return;
        this.isOpen = false;
        
        this.content.classList.remove("show");
        
        let transitionEnded = false;
        const onTransitionEnd = () => {
            if (!transitionEnded) {
                transitionEnded = true;
                this.content.style.display = "none";
                clearTimeout(fallbackTimeout);
                this.destroy();
            }
        };
        
        this.content.addEventListener("transitionend", onTransitionEnd, { once: true });
        
        const fallbackTimeout = setTimeout(onTransitionEnd, this.transitionDuration + 50);
        
        const targetElement = this.isContainerBody ? window : this.container;
        targetElement.removeEventListener("scroll", this.boundPositionDropdown, true);
        
        window.removeEventListener("resize", this.boundPositionDropdown);
    }
    
    toggle() {
        this.isOpen ? this.close() : this.open();
    }
    
    findScrollContainer(el) {
        let parent = el.parentElement;
        while (parent) {
            const style = window.getComputedStyle(parent);
            if (style.overflow === "auto" || style.overflow === "scroll" || style.overflowY === "auto" || style.overflowY === "scroll" || style.overflowX === "auto" || style.overflowX === "scroll") {
                return parent;
            }
            parent = parent.parentElement;
        }
        return document.body;
    }
    
    positionDropdown() {
        const containerRect = this.container.getBoundingClientRect();
        const btnRect = this.button.getBoundingClientRect();
        const dropdownHeight = this.content.offsetHeight;
        const dropdownWidth = this.content.offsetWidth;
        const scrollbarWidth = 15;
        const headerHeight = document.querySelector("header").offsetHeight;
        
        let btnOffsetFromTop = this.isContainerBody ? btnRect.bottom - headerHeight : btnRect.bottom - containerRect.top;
        const btnOffsetFromLeft = btnRect.left;
        
        const spaceBelow = containerRect.height - btnOffsetFromTop;
        let spaceAbove = this.isContainerBody ? btnRect.top - headerHeight : btnRect.top - containerRect.top;
        
        
        let top;
        if (spaceBelow >= dropdownHeight || spaceBelow >= spaceAbove) {
            top = this.isContainerBody ? btnOffsetFromTop + headerHeight + window.scrollY : btnOffsetFromTop + containerRect.top;
            this.content.style.transformOrigin = "top";
        } else {
            top = btnOffsetFromTop - dropdownHeight + containerRect.top - btnRect.height;
            this.content.style.transformOrigin = "bottom";
        }
        
        if (btnOffsetFromTop <= 0) {
            top = this.isContainerBody ? window.scrollY + headerHeight + 2 : containerRect.top + 2;
        } else if (spaceBelow + btnRect.height <= 0) {
            top = containerRect.bottom - dropdownHeight - 2;
        }
        
        
        let left = this.placement === "right" ? btnOffsetFromLeft - dropdownWidth + btnRect.width : btnOffsetFromLeft - dropdownWidth;
        if (left - containerRect.left - scrollbarWidth <= 0) {
            left = this.isContainerBody ? btnOffsetFromLeft : containerRect.left + scrollbarWidth;
        }
        
        const edgePadding = 4;
        const maxLeft = window.innerWidth - dropdownWidth - edgePadding;
        
        if (left < edgePadding) {
            left = edgePadding;
        } else if (left > maxLeft) {
            left = maxLeft;
        }
        
        this.content.style.top = top + "px";
        this.content.style.left = left + "px";
    }
    
    //مخرب
    destroy() {
        //console.log("پاک شد");
    }
    
    
}

//////////////////CSS////////////////

//.dropdown-content {
//    position: absolute;
//    opacity: 0;
//    transition-property: transform, opacity;
//  }

//  .dropdown-content.fade.show {
//    opacity: 1;
//  }

//  .dropdown-content.slide {
//    transform: translateY(1rem);
//  }
//  .dropdown-content.slide.show {
//    transform: translateY(0);
//    opacity: 1;
//  }

//  .dropdown-content.zoom {
//    transform: scale(0.8);
//  }
//  .dropdown-content.zoom.show {
//    transform: scale(1);
//    opacity: 1;
//  }

const dateTimePicker = (container = document) => {
    container.querySelectorAll(".sdate").forEach((datePicker) => {
        //console.log($(datePicker).data('myPersianDatepicker'))
        if (!$(datePicker).data("myPersianDatepicker")) {
            const positionX = datePicker.getAttribute("data-x") || "28";
            const positionY = datePicker.getAttribute("data-y") || "0";
            const instance = $(datePicker).persianDatepicker({
                onSelect: function (dateText) {
                    const onSelectCallback = datePicker.getAttribute("data-onSelect");
                    const onSelectId = datePicker.getAttribute("data-id");
                    if ($(this.model.inputElement).val().length > 0) {
                        $(this.model.inputElement).removeClass("AlertReq");
                    }
                    if (onSelectCallback) {
                        if (onSelectId) {
                            window[onSelectCallback](this, onSelectId);
                        } else {
                            window[onSelectCallback](this);
                        }
                    }
                },
                initialValueType: "persian",
                position: [Number(positionX), Number(positionY)],
                initialValue: false,
                responsive: true,
                observer: false,
                format: "YYYY/MM/DD",
                autoClose: true,
                calendar: {
                    persian: {
                        locale: "fa",
                        showHint: false,
                        leapYearMode: "astronomical",
                    },
                    gregorian: {
                        locale: "en",
                        showHint: false,
                    },
                },
                toolbox: {
                    enabled: false,
                    calendarSwitch: {
                        enabled: false,
                    },
                },
            });
            
            //console.log(instance)
            $(datePicker).data("myPersianDatepicker", instance);
        }
    });
    
    container.querySelectorAll(".sdateTime").forEach((dateTimePickerElem) => {
        if (!$(dateTimePickerElem).data("myPersianDatepicker")) {
            const instance = $(dateTimePickerElem).persianDatepicker({
                onSelect: function (dateText) {
                    if ($(this.model.inputElement).val().length > 0) {
                        $(this.model.inputElement).removeClass("AlertReq");
                    }
                },
                initialValueType: "persian",
                initialValue: false,
                responsive: true,
                observer: false,
                format: "YYYY/MM/DD, HH:mm",
                autoClose: true,
                calendar: {
                    persian: {
                        locale: "fa",
                        showHint: false,
                        leapYearMode: "algorithmic",
                    },
                    gregorian: {
                        locale: "en",
                        showHint: false,
                    },
                },
                toolbox: {
                    enabled: false,
                    calendarSwitch: {
                        enabled: false,
                    },
                },
                timePicker: {
                    enabled: true,
                    minute: {
                        step: 5,
                    },
                    second: {
                        enabled: false,
                    },
                    meridiem: {
                        enabled: true,
                    },
                },
            });
            $(dateTimePickerElem).data("myPersianDatepicker", instance);
        }
    });
    
    container.querySelectorAll(".stime").forEach((timePickerElem) => {
        if (!$(timePickerElem).data("myPersianDatepicker")) {
            const positionX = timePickerElem.getAttribute("data-x") || "28";
            const positionY = timePickerElem.getAttribute("data-y") || "0";
            const instance = $(timePickerElem).persianDatepicker({
                onSelect: function () {
                    const onSelectCallback = timePickerElem.getAttribute("data-onSelect");
                    if (onSelectCallback) {
                        window[onSelectCallback](this);
                    }
                },
                onlyTimePicker: true,
                initialValue: true,
                initialValueType: "persian",
                autoClose: true,
                position: [Number(positionX), Number(positionY)],
                format: "HH:mm",
                timePicker: {
                    enabled: true,
                    hour: { enabled: true },
                    minute: { enabled: true },
                    second: { enabled: false },
                    meridian: { enabled: false },
                },
            });
            $(timePickerElem).data("myPersianDatepicker", instance);
        }
    });
};


/*const dateTimePicker = () => {

    document.querySelectorAll('.sdate').forEach((datePicker) => {
        const positionX = datePicker.getAttribute('data-x') || '28';
        const positionY = datePicker.getAttribute('data-y') || '0';


        $(datePicker).persianDatepicker({
            onSelect: function (dateText) {
                const onSelectCallback = datePicker.getAttribute('data-onSelect')
                const onSelectId = datePicker.getAttribute('data-id');
                if ($(this.model.inputElement).val().length > 0) {
                    $(this.model.inputElement).removeClass("AlertReq")
                }

                if (onSelectCallback) {
                    if (onSelectId) {
                        window[onSelectCallback](this, onSelectId);
                    }
                    window[onSelectCallback](this);
                }
                //console.log("Selected date: " + dateText + "; input's current value: " + this.value);
            },
            initialValueType: 'persian',
            position: [Number(positionX), Number(positionY)],
            initialValue: false,
            responsive: true,
            observer: false,
            format: 'YYYY/MM/DD',
            autoClose: true,
            "calendar": {
                "persian": {
                    "locale": "fa",
                    "showHint": false,
                    "leapYearMode": "algorithmic"
                },
                "gregorian": {
                    "locale": "en",
                    "showHint": false
                }
            },
            "toolbox": {
                "enabled": false,
                "calendarSwitch": {
                    "enabled": false
                }
            },

        });
    })

    $('.sdateTime').persianDatepicker({
        onSelect: function (dateText) {

            if ($(this.model.inputElement).val().length > 0) {
                $(this.model.inputElement).removeClass("AlertReq")
            }

            //console.log("Selected date: " + dateText + "; input's current value: " + this.value);
        },
        initialValueType: 'persian',
        initialValue: false,
        responsive: true,
        observer: false,
        format: 'YYYY/MM/DD, HH:mm',
        autoClose: true,
        "calendar": {
            "persian": {
                "locale": "fa",
                "showHint": false,
                "leapYearMode": "algorithmic"
            },
            "gregorian": {
                "locale": "en",
                "showHint": false
            }
        },
        "toolbox": {
            "enabled": false,
            "calendarSwitch": {
                "enabled": false
            }
        },
        "timePicker": {
            enabled: true,
            minute: {
                step: 5
            },
            second: {
                enabled: false
            },
            meridiem: {
                enabled: true
            }
        }

    });

    document.querySelectorAll('.stime').forEach((timePicker) => {
        const positionX = timePicker.getAttribute('data-x') || '28';
        const positionY = timePicker.getAttribute('data-y') || '0';

        $(timePicker).persianDatepicker({
            onSelect: function () {
                const onSelectCallback = timePicker.getAttribute('data-onSelect')
                if (onSelectCallback) {
                    window[onSelectCallback](this);
                }
            },
            onlyTimePicker: true,
            initialValue: true,
            initialValueType: 'persian',
            autoClose: true,
            position: [Number(positionX), Number(positionY)],
            format: 'HH:mm',
            "timePicker": {
                "enabled": true,
                "hour": {
                    "enabled": true,
                },
                "minute": {
                    "enabled": true,
                },
                "second": {
                    "enabled": false,
                },
                "meridian": {
                    "enabled": false
                }
            }
        });
    });

}
*/

//function initTooltips()     {
//    const elementsWithTitle = document.querySelectorAll('[title]');
//    elementsWithTitle.forEach((elm) => {
//        elm.setAttribute('data-bs-toggle', "tooltip");
//        elm.setAttribute('data-bs-title', elm.title);
//        elm.setAttribute('data-bs-custom-class', "customFontTooltip");
//        elm.removeAttribute('title');
//    });
//}

//setTimeout(() => {
//    initTooltips();
//},1000)

//Object.defineProperty(Element.prototype, 'setInnerHTML', {
//    set(value) {
//        this.innerHTML = value;

//        const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
//        const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
//    }
//});


function getRelativeTime(dateTimeStr) {
    const inputDate = new Date(dateTimeStr);
    const now = new Date();
    let diffMs = now - inputDate;
    
    const diffMinutes = Math.floor(diffMs / (1000 * 60));
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    
    if (diffMinutes < 1) {
        return "لحظاتی پیش";
    }
    
    if (diffHours < 24) {
        const hours = Math.floor(diffMinutes / 60);
        const minutes = diffMinutes % 60;
        if (hours > 0 && minutes > 0) {
            return `${hours} ساعت و ${minutes} دقیقه پیش`;
        } else if (hours > 0) {
            return `${hours} ساعت پیش`;
        } else {
            return `${minutes} دقیقه پیش`;
        }
    } else {
        const days = diffDays;
        const remainingHours = diffHours - days * 24;
        if (remainingHours > 0) {
            return `${days} روز و ${remainingHours} ساعت پیش`;
        } else {
            return `${days} روز پیش`;
        }
    }
}

const notificationChange = (text, { link, isBlank = true }, category) => {
    const notifStyles = {
        actionRequired: { bg: "#e3f2fd", color: "#1565c0", iconSrc: "activate.gif", title: "نیاز به تایید" },
        info: { bg: "#e8f5e9", color: "#2e7d32", iconSrc: "information.gif", title: "اطلاع رسانی" },
        task: { bg: "#f3e5f5", color: "#6a1b9a", iconSrc: "toDo.gif", title: "کاربان" },
        chat: { bg: "#d9eceb", color: "#008077", iconSrc: "notification.gif", title: "پیام رسان" },
        default: { bg: "#eceff1", color: "#37474f", iconSrc: "notification.gif", title: "اعلان سیستم" },
    };
    
    const { bg, color, iconSrc, title } = notifStyles[category] || notifStyles.default;
    
    const notifDiv = document.createElement("div");
    notifDiv.style.display = "flex";
    notifDiv.style.alignItems = "flex-start";
    notifDiv.style.gap = "6px";
    notifDiv.style.width = "100%";
    
    notifDiv.innerHTML = `
        <div style="width: clamp(32px, 4vw, 44px); color: ${color}; margin-top: -3px">
          <img src="/Img/${iconSrc}" style="width: 100%" alt="">
        </div>
        <div style="flex: 1; display: flex; flex-direction: column; gap: clamp(5px, 0.6vw, 6px);">
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <span class="fbold" style="font-size: clamp(12px, 1.2vw, 14px); color: ${color}">${title}</span>
               
                <div class="d-flex align-items-center gap-1" style="color: ${color};">
                  <span id="counterNotif" style="line-height: 1; font-size: 12px;"></span>
                  <span class="d-flex align-items-center justify-content-center" style="cursor:pointer;" onclick="dismissNotif(null, this)">
                     <svg style="width: 16px" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                         <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
                     </svg>
                  </span>
                </div>
            </div>
            <div  style="font-size: clamp(11px, 1.1vw, 13px); color: #212529;">${text}</div>
            ${link ? `
                <div class="text-end">
                    <button onclick="goToLink('${link}', this, ${isBlank})" style="font-size: clamp(10px, 1vw, 12px); padding: clamp(3px, 0.5vw, 4px) clamp(6px, 1vw, 8px);background: ${color}; color: #fff; border: none; border-radius: 4px; cursor: pointer;">
                        مشاهده
                    </button>
                </div>
            ` : ""}
        </div>
    `;
    
    
    let i = 7;
    let paused = false;
    notifDiv.querySelector("#counterNotif").innerText = i;
    const counterNotifId = setInterval(() => {
        if (!paused) {
            i--;
            notifDiv.querySelector("#counterNotif").innerText = i;
            if (i <= 0) {
                clearInterval(counterNotifId);
                toastInstance.hideToast();
            }
        }
    }, 1000);
    
    notifDiv.addEventListener("mouseenter", () => paused = true);
    notifDiv.addEventListener("mouseleave", () => paused = false);
    
    const isMobile = window.innerWidth <= 768;
    
    const toastInstance = Toastify({
        node: notifDiv,
        duration: i * 1000,
        close: true,
        gravity: isMobile ? "top" : "bottom",
        position: isMobile ? "center" : "left",
        style: {
            background: bg,
            borderLeft: `5px solid ${color}`,
            boxShadow: `0 6px 18px rgba(0, 0, 0, 0.2)`,
            color: "#212529",
            padding: "clamp(8px, 1.2vw, 12px) clamp(10px, 1.5vw, 14px)",
            cursor: "auto",
            borderRadius: "8px",
            width: "450px",
            minWidth: "280px",
            maxWidth: isMobile ? "90%" : "450px",
        },
        callback: () => clearInterval(counterNotifId),
    });
    
    toastInstance.showToast();
    
    /*if (Notification.permission === "granted") {
        sendBrowserNotification(title, text, `/Img/${iconSrc}`, link);
    }*/
    
};
async function requestNotificationPermission() {
    if (!("Notification" in window)) {
        console.log("مرورگر از نوتیفیکیشن پشتیبانی نمی‌کند");
        return;
    }
    
    const permission = await Notification.requestPermission();
    return permission === "granted";
}

function sendBrowserNotification(title, body, icon, link) {
    if (Notification.permission !== "granted") return;
    
    const notif = new Notification(title, {
        body,
        icon: icon,
        dir: "rtl",
    });
    
    notif.onclick = () => {
        if (link) window.open(link, "_blank");
    };
}


const notification = (id, title, description, dateMiladi, entityType, entityId) => {
    
    const notifDiv = document.createElement("div");
    notifDiv.className = "d-flex flex-column p-0 justify-content-evenly h-100 w-100";
    notifDiv.style.position = "relative";
    notifDiv.id = id;
    
    notifDiv.innerHTML = `
        <div style="display:flex;align-items:flex-start;gap:6px;width:100%;padding:8px 12px;">
            <div style="width: clamp(32px, 4vw, 44px); color: #6a1b9a; margin-top: -3px">
                <img src="/Img/toDo.gif" style="width: 100%" alt="">
            </div>
            <div style="flex: 1; display: flex; flex-direction: column; gap: clamp(5px, 0.6vw, 6px);">
                <div style="display: flex; justify-content: space-between; align-items: start;">
                    <span class="fbold" style="font-size: clamp(12px, 1.2vw, 14px); color: #3a005d">${title}</span>
                    <div class="d-flex align-items-center gap-1" style="color: #6a1b9a;">
                        <span id="counterNotif" style="line-height: 1; font-size: 12px;"></span>
                        <span class="d-flex align-items-center justify-content-center" style="cursor:pointer;" onclick="dismissNotif('${id}', this)">
                            <svg style="width: 16px" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
                            </svg>
                        </span>
                    </div>
                </div>
                <div style="font-size: clamp(11px, 1.1vw, 13px); color: #212529; white-space: pre-line;">${description}</div>
                <div class="d-flex align-items-start justify-content-between gap-2 w-100">
                    <div class="d-flex align-items-center" style="gap: 4px">
                        <span style="font-size:clamp(9px, 2vw, 10px);color:#8f8f8f" id="relativeTime"></span>
                        <span class="d-flex align-items-end gap-1 snooze-notification" style="font-size:clamp(9px, 2vw, 10px);cursor:pointer;color:#000" onclick="snoozeNotif('${id}', this)">
                            <svg style="width:19px;height:19px" fill="none" viewBox="0 0 24 24"><g fill="currentColor"><path d="m19.6916 7.34849c-.25-.33-.72-.4-1.05-.15s-.4.72-.15 1.05c1.08 1.44 1.65 3.12001 1.65 4.86001 0 4.49-3.65 8.14-8.14 8.14-4.49004 0-8.14004-3.65-8.14004-8.14 0-4.49001 3.65-8.13001 8.14004-8.13001.58 0 1.17.07 1.81.22.03.01.06 0 .1 0 .02 0 .05.02.07.02.03 0 .05-.01.08-.01.04 0 .07-.01.1-.02.05-.01.1-.03.15-.06.03-.02.07-.03.1-.05.01-.01.03-.01.04-.02.03-.02.04-.05.06-.07.04-.04.07-.07.1-.12.03-.04.04-.09.06-.13.01-.03.03-.06.04-.09 0-.02 0-.03 0-.05.01-.05.01-.1 0-.15 0-.05 0-.09-.01-.14-.01-.04-.03-.08-.05-.13s-.04-.1-.07-.14c-.01-.02-.01-.03-.02-.04l-1.98-2.47c-.26-.32-.73-.37-1.05-.12-.32.26-.37.73-.12 1.05l.82 1.02c-.08 0-.16-.01-.24-.01-5.31004 0-9.64004 4.32-9.64004 9.64001 0 5.32 4.32 9.64 9.64004 9.64 5.32 0 9.64-4.32 9.64-9.64.01-2.07-.67-4.06001-1.94-5.76001z" /><path d="m9.5415 16.6708c-.41 0-.75-.34-.75-.75v-3.39l-.19.22c-.28.31-.75.33-1.06.06-.31-.28-.33-.75-.06-1.06l1.5-1.67c.21-.22999.54-.30999.83-.19999s.48.38999.48.69999v5.35c0 .41-.33.74-.75.74z" /><path d="m14 16.6703c-1.52 0-2.75-1.23-2.75-2.75v-1.35c0-1.52 1.23-2.74999 2.75-2.74999s2.75 1.22999 2.75 2.74999v1.35c0 1.52-1.23 2.75-2.75 2.75zm0-5.34c-.69 0-1.25.56-1.25 1.25v1.35c0 .69.56 1.25 1.25 1.25s1.25-.56 1.25-1.25v-1.35c0-.69-.56-1.25-1.25-1.25z" /></g></svg>
                            <span>تعویق</span>
                        </span>
                    </div>
                    <div class="d-flex align-items-center justify-content-center" style="cursor:pointer">
                        <button onclick="readNotifAction('${id}', this, '${entityType}', '${entityId}')" style="font-size: clamp(10px, 1vw, 12px); padding: clamp(3px, 0.5vw, 4px) clamp(6px, 1vw, 8px);background: #6a1b9a; color: #fff; border: none; border-radius: 4px; cursor: pointer;">
                          مشاهده
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    notifDiv.querySelector("#relativeTime").innerHTML = getRelativeTime(dateMiladi);
    /*const updateTimeId = setInterval(() => {
        notifDiv.querySelector("#relativeTime").innerHTML = getRelativeTime(dateMiladi);
    }, 60000);*/
    
    
    let i = 7;
    let paused = false;
    notifDiv.querySelector("#counterNotif").innerText = i;
    const counterNotifId = setInterval(() => {
        if (!paused) {
            i--;
            notifDiv.querySelector("#counterNotif").innerText = i;
            if (i <= 0) {
                //clearAllInterval();
                toastInstance.hideToast();
            }
        }
    }, 1000);
    
    notifDiv.addEventListener("mouseenter", () => paused = true);
    notifDiv.addEventListener("mouseleave", () => paused = false);
    
    const clearAllInterval = () => {
        clearInterval(counterNotifId);
        handleNotifAction(id, false);
        //clearInterval(updateTimeId);
    };
    
    const isMobile = window.innerWidth <= 768;
    
    const toastInstance = Toastify({
        node: notifDiv,
        duration: i * 1000,
        close: true,
        gravity: isMobile ? "top" : "bottom",
        position: isMobile ? "center" : "left",
        style: {
            background: "#f3e5f5",
            //border: "1px solid #6a1b9a1a",
            borderLeft: `5px solid #6a1b9a`,
            boxShadow: `0 6px 18px rgba(0, 0, 0, 0.2)`,
            color: "#212529",
            padding: "0",
            cursor: "auto",
            borderRadius: "8px",
            width: "450px",
            maxWidth: isMobile ? "90%" : "450px",
        },
        callback: () => clearAllInterval(),
    });
    
    toastInstance.showToast();
};

const activeNotifCall = {};
function notificationIncomingCall(callerNumber, uniqueid, destNumber) {
    if (!callerNumber || !uniqueid || !destNumber) return;
    if (activeNotifCall[uniqueid]) return;
    
    const callToast = Toastify({
        text: `
            <div style="display: flex; align-items: center; gap: 14px;">
                <img id="incomingCallIcon" class="notifCallImg" src="/Img/Calling.gif" style="width: 42px;" alt=""/>
                <img id="endedCallIcon" class="notifCallImg" src="/Img/Call_ended.gif" style="width: 42px;display: none" alt=""/>
                <img id="answeredCallIcon" class="notifCallImg" src="/Img/Calling_answergif.gif" style="width: 42px;display: none" alt=""/>
                
                <div class="d-flex align-items-start justify-content-between w-100">
                    <div class="d-flex flex-grow-1 notifCallText" style="gap:6px;flex-direction: column">
                        <div class="d-flex align-items-center justify-content-between">
                            <div class="fbold notifCallTitle" style="font-size: 16px">تماس ورودی</div>
                        </div>
                        
                        <div class="notifCallDetail d-flex align-items-center gap-1" style="font-size: 14px;">
                             <span>${callerNumber}</span>
                        </div>
                    </div>
                    <div class="notifCallTimer" style="font-size: 13px; color: #f1f1f1;">
                        <span id="callTimer"></span>
                    </div>
                </div>
                <div class="d-flex align-items-center" style="align-self: start;position: absolute;bottom: -2px;left: 4px;gap:2px">
                    <button class="minimize-btn" onclick="minimizeCallNotif('${uniqueid}')">
                          <svg style="width: 12px;height: 12px" viewBox="0 0 16 16" fill="currentColor"><path d="M3.75 7.25a.75.75 0 0 0 0 1.5h8.5a.75.75 0 0 0 0-1.5h-8.5Z" /></svg>
                    </button>
                    <button class="dismissNotif-btn" onclick="dismissCallNotif(this,'${uniqueid}')">
                          <svg style="width: 12px;height: 12px" viewBox="0 0 16 16" fill="currentColor"><path d="M5.28 4.22a.75.75 0 0 0-1.06 1.06L6.94 8l-2.72 2.72a.75.75 0 1 0 1.06 1.06L8 9.06l2.72 2.72a.75.75 0 1 0 1.06-1.06L9.06 8l2.72-2.72a.75.75 0 0 0-1.06-1.06L8 6.94 5.28 4.22Z" /></svg>
                    </button>
                </div>
            </div>
        `,
        duration: -1,
        gravity: "top",
        position: "center",
        escapeMarkup: false,
        stopOnFocus: false,
        close: true,
        style: {
            background: "#2f3e46",
            color: "#fff",
            borderRadius: "10px",
            padding: "14px 16px",
            boxShadow: "0 8px 24px rgba(0,0,0,0.3)",
            width: "310px",
            maxWidth: "90%",
            cursor: "auto",
        },
    });
    callToast.showToast();
    activeNotifCall[uniqueid] = callToast;
}

function closeCallNotif(uniqueid) {
    const notif = activeNotifCall[uniqueid];
    if (!notif) return;
    
    if (notif._interval) {
        clearInterval(notif._interval);
        delete notif._interval;
    }
    
    notif.toastElement.style.background = "darkred";
    notif.toastElement.querySelector("#incomingCallIcon").style.display = "none";
    notif.toastElement.querySelector("#answeredCallIcon").style.display = "none";
    notif.toastElement.querySelector("#endedCallIcon").style.display = "";
    
    setTimeout(() => {
        notif.hideToast();
        delete activeNotifCall[uniqueid];
    }, 2000);
}

function answerCallNotif(uniqueid) {
    const notif = activeNotifCall[uniqueid];
    if (!notif) return;
    
    if (!notif._interval) {
        notif.toastElement.style.background = "#1b4332";
        notif.toastElement.querySelector("#incomingCallIcon").style.display = "none";
        notif.toastElement.querySelector("#endedCallIcon").style.display = "none";
        notif.toastElement.querySelector("#answeredCallIcon").style.display = "";
        
        let seconds = 0;
        const timerSpan = notif.toastElement.querySelector(`#callTimer`);
        if (!timerSpan) return;
        
        timerSpan.textContent = "00:00";
        notif._interval = setInterval(() => {
            seconds++;
            const min = Math.floor(seconds / 60);
            const sec = seconds % 60;
            timerSpan.textContent = `${min.toString().padStart(2, "0")}:${sec.toString().padStart(2, "0")}`;
        }, 1000);
    }
    
}

function minimizeCallNotif(uniqueid) {
    const notif = activeNotifCall[uniqueid];
    if (!notif) return;
    
    notif.toastElement.classList.toggle("minimized-bar");
}
function dismissCallNotif(btn, uniqueid) {
    /*  const notif = activeNotifCall[uniqueid];
      if (!notif) return;*/
    
    // notif.hideToast();
    // delete activeNotifCall[uniqueid];
    
    if (btn) {
        const closeBtn = btn.closest(".toastify").querySelector(".toast-close");
        closeBtn.click();
    }
}


const toastifyError = (text, duration = 5000) => {
    Toastify({
        text,
        duration,
        close: false,
        gravity: "top",
        position: "center",
        stopOnFocus: false,
        style: {
            background: "linear-gradient(135deg, rgba(121,9,9,1) 21%, rgba(78,0,0,0.9) 100%)",
            borderRadius: "8px",
        },
    }).showToast();
};

const toastifySuccess = (text, duration = 3000) => {
    Toastify({
        text,
        duration,
        close: false,
        gravity: "top",
        position: "center",
        escapeMarkup: false,
        stopOnFocus: false,
        style: {
            background: "linear-gradient(135deg, rgba(2,81,75,1) 21%, rgba(0,59,54,1) 100%)",
            borderRadius: "8px",
        },
    }).showToast();
};

const toastifyInfo = (text, duration = 3000) => {
    Toastify({
        text,
        duration,
        close: false,
        gravity: "top",
        position: "center",
        stopOnFocus: false,
        style: {
            background: "linear-gradient(135deg, rgba(110,73,0,1) 18%,  rgba(185,124,0,1) 100%",
            borderRadius: "8px",
        },
    }).showToast();
};

const toastifyInfo2 = (text, options = {}) => {
    const defaultStyle = {
        background: "linear-gradient(135deg, rgba(110,73,0,1) 18%, rgba(185,124,0,1) 100%)",
        borderRadius: "8px",
    };
    
    const toast = Toastify({
        text: text,
        node: options.node,
        duration: options.duration ?? 5000,
        close: options.close ?? false,
        gravity: options.gravity ?? "top",
        position: options.position ?? "center",
        stopOnFocus: options.stopOnFocus ?? true,
        style: { ...defaultStyle, ...(options.style || {}) },
    });
    
    toast.showToast();
    return toast;
};

const toastifyError2 = (text, options = {}) => {
    const defaultStyle = {
        background: "linear-gradient(135deg, rgba(121,9,9,1) 21%, rgba(78,0,0,0.9) 100%)",
        borderRadius: "8px",
    };
    
    const toast = Toastify({
        text: text,
        node: options.node,
        duration: options.duration ?? 5000,
        close: options.close ?? false,
        gravity: options.gravity ?? "top",
        position: options.position ?? "center",
        stopOnFocus: options.stopOnFocus ?? true,
        style: { ...defaultStyle, ...(options.style || {}) },
    });
    
    toast.showToast();
    return toast;
};


let smsToast = null;
function showSmsSendingToast() {
    
    if (smsToast) {
        smsToast.hideToast();
    }
    const notifDiv = document.createElement("div");
    notifDiv.className = "flex-center gap-3";
    notifDiv.innerHTML = `
        در حال ارسال پیامک
        <span class="spinner-border spinner-border-sm"></span>
    `;
    
    smsToast = Toastify({
        //text: "در حال ارسال پیامک...",
        node: notifDiv,
        duration: -1,
        close: false,
        gravity: "top",
        position: "center",
        stopOnFocus: false,
        style: {
            background: "linear-gradient(135deg, rgba(110,73,0,1) 18%, rgba(185,124,0,1) 100%)",
            borderRadius: "8px",
            cursor: "auto",
        },
    }).showToast();
    
}

function updateSmsToast(text, type) {
    if (!smsToast) return;
    const smsToastElement = smsToast.toastElement;
    
    smsToastElement.textContent = text;
    
    switch (type) {
        case "success":
            smsToastElement.style.background = "linear-gradient(135deg, rgba(2,81,75,1) 21%, rgba(0,59,54,1) 100%)";
            setTimeout(() => {
                smsToast?.hideToast();
                smsToast = null;
            }, 3000);
            
            break;
        
        case "error":
            smsToastElement.style.background = "linear-gradient(135deg, rgba(121,9,9,1) 21%, rgba(78,0,0,0.9) 100%)";
            setTimeout(() => {
                smsToast?.hideToast();
                smsToast = null;
            }, 5000);
            
            break;
    }
}


const checkAccess = async (elId) => {
    let rols = JSON.parse(sessionStorage.getItem("Rols"));
    let hasAccess = false;
    
    rols.forEach((rol) => {
        if (document.getElementById(elId) && document.getElementById(elId).classList.contains(rol?.name)) {
            hasAccess = true;
            return hasAccess;
        }
        
    });
    return hasAccess;
};


// بهینه شده

/*const Mask = () => {
    for (const el of document.querySelectorAll("[placeholder][data-slots]")) {

        el.addEventListener('focusout', () => {
            el.value = el.value.replace(/_/g, '').replace(/y/g, '').replace(/m/g, '').replace(/d/g, '');
            el.value = el.value.replace(/\/?mm\/?dd/g, '').replace(/\/?mm/g, '').replace(/\/?dd/g, '');
        });

        const pattern = el.getAttribute("placeholder"),
            slots = new Set(el.dataset.slots || "_"),
            prev = (j => Array.from(pattern, (c, i) => slots.has(c) ? j = i + 1 : j))(0),
            first = [...pattern].findIndex(c => slots.has(c)),
            accept = new RegExp(el.dataset.accept || "\\d", "g"),
            clean = input => {
                input = input.match(accept) || [];
                return Array.from(pattern, c =>
                    input[0] === c || slots.has(c) ? input.shift() || c : c
                );
            },
            format = () => {
                const [i, j] = [el.selectionStart, el.selectionEnd].map(i => {
                    i = clean(el.value.slice(0, i)).findIndex(c => slots.has(c));
                    return i < 0 ? prev[prev.length - 1] : back ? prev[i - 1] || first : i;
                });
                el.value = clean(el.value).join``;
                el.setSelectionRange(i, j);
                back = false;
            };

        let back = false;

        el.addEventListener("keydown", (e) => {
            back = e.key === "Backspace";
        });

        el.addEventListener("keyup", () => {
            back = false;
        });

        el.addEventListener("input", format);
        // el.addEventListener("focus", format);

        el.addEventListener("blur", () => {
            if (el.value.replace(/_/g, '') === '') {
                el.value = "";
            }
        });
    }
};*/


const Mask = () => {
    for (const el of document.querySelectorAll("[placeholder][data-slots]")) {
        el.addEventListener("focusout", () => {
            
            if (el.value.includes("_")) {
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
                el.value = el.value.replace("_", "");
            }
            
            
            if (el.value.includes("y")) {
                el.value = el.value.replace("y", "");
                el.value = el.value.replace("y", "");
                el.value = el.value.replace("y", "");
                el.value = el.value.replace("y", "");
            }
            
            if (el.value.includes("mm") && el.value.includes("dd")) {
                el.value = el.value.replace("/mm/dd", "");
            }
            if (el.value.includes("mm") || el.value.includes("m")) {
                el.value = el.value.replace("/mm", "");
                el.value = el.value.replace("m", "");
            }
            if (el.value.includes("dd") || el.value.includes("d")) {
                el.value = el.value.replace("/dd", "");
                el.value = el.value.replace("d", "");
            }
            
            
        });
        
        const pattern = el.getAttribute("placeholder"),
            slots = new Set(el.dataset.slots || "_"),
            prev = (j => Array.from(pattern, (c, i) => slots.has(c) ? j = i + 1 : j))(0),
            first = [...pattern].findIndex(c => slots.has(c)),
            accept = new RegExp(el.dataset.accept || "\\d", "g"),
            clean = input => {
                
                input = input.match(accept) || [];
                
                
                return Array.from(pattern, c =>
                    input[0] === c || slots.has(c) ? input.shift() || c : c,
                );
            },
            format = () => {
                
                const [i, j] = [el.selectionStart, el.selectionEnd].map(i => {
                    
                    i = clean(el.value.slice(0, i)).findIndex(c => slots.has(c));
                    return i < 0 ? prev[prev.length - 1] : back ? prev[i - 1] || first : i;
                });
                el.value = clean(el.value).join``;
                el.setSelectionRange(i, j);
                back = false;
            };
        let back = false;
        
        el.addEventListener("keydown", (e) => {
            back = e.key === "Backspace";
        });
        el.addEventListener("input", format);
        //el.addEventListener("focus", format);
        el.addEventListener("blur", () => {
            el.value === pattern && (el.value = "");
        });
    }
};


const MaskDate = () => {
    document.addEventListener("input", function (e) {
        if (e.target.matches("input[data-mask-date]")) {
            let value = e.target.value.replace(/\D/g, "");
            
            let year = value.slice(0, 4);
            let month = value.slice(4, 6);
            let day = value.slice(6, 8);
            
            if (year > 1500) year = "1500";
            if (year.length === 4 && year < 1400) year = "1400";
            if (month > 12) month = "12";
            if (month.length === 2 && month <= 0) month = "01";
            if (day > 31) day = "31";
            if (day.length === 2 && day <= 0) day = "01";
            
            e.target.value = `${year}${month ? "/" + month : ""}${day ? "/" + day : ""}`;
        }
    });
};


const ShowPreviewPfactor = (id) => {
    window.open("/factor/PreviewPfactorPrint/" + id, "_blank", "noopener,noreferrer");
};
const ShowPreviewFactor = (id) => {
    window.open("/factor/PreviewFactorPrint/" + id, "_blank", "noopener,noreferrer");
};
const ShowPreviewFactorSepidar = (id) => {
    window.open("/factor/PreviewFactorSepidarPrint/" + id, "_blank", "noopener,noreferrer");
};
const getAllProduct = async (searchValue = "", from) => {
    try {
        const res = await fetch(`/Amount/ListProduct?searchValue=${searchValue}&from=${from}`);
        if (res.ok) {
            return await res.text();
        }
    } catch (err) {
        console.log(err);
    }
};

const SubmitReportForm = async () => {
    if (await mySer.CheckValidForm("#ReportFormId")) {
        let data = mySer.convertFormToJSON("#ReportFormId");
        const el = document.getElementById("repCustomerTxt");
        
        //data.customerId = data.repCustomerTxt;
        data.customerTxt = el.options[el.selectedIndex].text;
        data.dealId = dealIdReport;
        data.dealSerialNumber = dealSerialNumberReport;
        
        if (oldReport != null) {
            data = { ...oldReport, ...data };
            oldReport = null;
        }
        
        const resultCall = document.getElementById("resultCall");
        const finalResultCall = document.getElementById("finalResultCall");
        const reasonCancel = document.getElementById("reasonCancel");
        data.resultCallTxt = resultCall.value ? resultCall.options[resultCall.selectedIndex].text : "";
        data.finalResultCallTxt = finalResultCall.value ? finalResultCall.options[finalResultCall.selectedIndex].text : "";
        data.reasonCancelTxt = reasonCancel.value ? reasonCancel.options[reasonCancel.selectedIndex].text : "";
        
        FLshowLoading();
        $.post(`/Report/AddReport`, data, async (res) => {
            if (res.status === 200) {
                if (typeReportSubmit === 0) {
                    const currentPath = window.location.pathname.toLowerCase();
                    if (currentPath.includes("report")) {
                        await getAllReports();
                    } else if (currentPath.includes("dashboard")) {
                        await getDashReports();
                    }
                } else if (typeReportSubmit === 4) {
                    await refreshDealReports();
                    if (typeof getAllDeals === "function") {
                        await getAllDeals();
                    }
                }
                
                toastifySuccess(res.message);
            } else if (res.status === 201) {
                toastifyError(res.message);
            }
            FLshowLoading(false);
            closeModalReport();
        }).fail(() => {
            setTimeout(async () => await SubmitReportForm(), 3000);
        });
    }
    
};

const getBaseItemsReport = async () => {
    try {
        const res = await axios(`/Report/GetBaseItemsReport`);
        if (res.status === 200) {
            return res.data;
            /*
            return res.data.reduce((acc, cur) => {
                if (cur.typeCode === 4) return { ...acc, resultCall: [...acc.resultCall, { value: cur.id, text: cur.title }] };
                if (cur.typeCode === 5) return { ...acc, finalResultCall: [...acc.finalResultCall, { value: cur.id, text: cur.title }] };
                if (cur.typeCode === 6) return { ...acc, reasonCancel: [...acc.reasonCancel, { value: cur.id, text: cur.title }] };
            }, { resultCall: [], finalResultCall: [], reasonCancel: [] });
            */
        }
    } catch (err) {
        console.log(err);
    }
};


async function initReportModal() {
    const baseItem = await getBaseItemsReport();
    createOption("resultCall", baseItem.resultCall);
    createOption("finalResultCall", baseItem.finalResultCall);
    createOption("reasonCancel", baseItem.reasonCancel);
    
    const container = document.getElementById("ReportFormId");
    
    const resultCallWrapper = container.querySelector(".resultCallWrapper");
    const finalResultCallWrapper = container.querySelector(".finalResultCallWrapper");
    const reasonCancelWrapper = container.querySelector(".reasonCancelWrapper");
    
    const resultCallSelect = resultCallWrapper.querySelector("select");
    const finalResultCallSelect = finalResultCallWrapper.querySelector("select");
    const reasonCancelSelect = reasonCancelWrapper.querySelector("select");
    
    container.querySelectorAll("input[name='reportType']").forEach(elm => elm.addEventListener("change", (e) => {
        const showResultFollowUpContainer = Number(e.target.value) === 1;
        container.querySelector("#resultFollowUpContainer").classList.toggle("d-none", !showResultFollowUpContainer);
        toggleElementsRequired(resultCallSelect, showResultFollowUpContainer);
        toggleElementsRequired([finalResultCallSelect, reasonCancelSelect], false);
        finalResultCallWrapper.classList.toggle("d-none", true);
        reasonCancelWrapper.classList.toggle("d-none", true);
        resultCallSelect.value = "";
        finalResultCallSelect.value = "";
        reasonCancelSelect.value = "";
    }));
    
    resultCallSelect.addEventListener("change", (e) => {
        const value = e.target.value;
        const showFinalResult = value === "ed0feaf4-9264-4028-b8cf-1a268bfc1c9a"
            || value === "310df88c-b65b-4f26-9f1e-ab72f489bc1e"
            || value === "986e7e82-9853-402f-a7f6-36252965ff82";
        const showReasonCancel = value === "986e7e82-9853-402f-a7f6-36252965ff82";
        
        
        finalResultCallWrapper.classList.toggle("d-none", !showFinalResult);
        reasonCancelWrapper.classList.toggle("d-none", !showReasonCancel);
        toggleElementsRequired(finalResultCallSelect, showFinalResult);
        
        finalResultCallSelect.value = showReasonCancel ? "82769710-880f-43aa-bd83-f5c6016ce38c" : "";
        toggleElementsDisabled(finalResultCallSelect, showReasonCancel);
        reasonCancelSelect.value = "";
        toggleElementsRequired(reasonCancelSelect, showReasonCancel);
    });
    
    finalResultCallSelect.addEventListener("change", (e) => {
        const value = e.target.value;
        const showReasonCancel = value === "82769710-880f-43aa-bd83-f5c6016ce38c";
        
        reasonCancelSelect.value = "";
        reasonCancelWrapper.classList.toggle("d-none", !showReasonCancel);
        toggleElementsRequired(reasonCancelSelect, showReasonCancel);
    });
}


const initCustomerOpReport = async () => {
    /*    $("#repCustomerTxt").select2({
            tags: true,
            dir: "rtl",
            placeholder: "انتخاب",
            selectionCssClass: "form-control",
            dropdownParent: $("#ModalReport"),
            ajax: {
                url: baseUrl + "Customer/GetCustomerOp",
                delay: 250,
                cache: true,
                data: params => ({ search: params.term || "" }),
                processResults: data => ({
                    results: data.map(item => ({ id: item.id, text: item.text })),
                }),
            },
        });*/
    
    
    const customerRes = await axios("/Customer/GetAllCustomers");
    const repTelElm = document.getElementById("repTel");
    
    
    const cusTomSelect = new TomSelect("#repCustomerTxt", {
        options: customerRes.data.map(c => ({ text: `${c.customerName} (کد مشتری: ${c.customerCode})`, value: c.id })),
        maxItems: 1,
        persist: false,
        openOnFocus: false,
        createOnBlur: true,
        selectOnTab: true,
        placeholder: "نام مشتری ...",
        
        create: (input, callback) => callback({ value: `custom-${input}`, text: input }),
        
        async onChange(value) {
            if (!value) return;
            if (!value.startsWith("custom")) {
                toggleElementsDisabled(repTelElm, true);
                cusTomSelect.disable();
                
                const res = await axios(`/Customer/GetCustomer/${value}`);
                // repTelElm.tomselect.setValue(res.data.mobile, true);
                repTelElm.value = res.data.mobile;
                flashItem(repTelElm);
                // flashItem(repTelElm.tomselect.control);
                cusTomSelect.enable();
                toggleElementsDisabled(repTelElm, false);
            }
        },
        
        onDelete(values) {
            const value = values[0];
            if (value.startsWith("custom")) {
                const opt = cusTomSelect.options[value];
                
                cusTomSelect.removeItem(value, true);
                cusTomSelect.setTextboxValue(opt.text.trim());
                cusTomSelect.refreshOptions(true);
                return false;
            }
            
            // repTelElm.tomselect.clear(true);
            repTelElm.value = "";
            return true;
        },
    });
    /*
        new TomSelect(repTelElm, {
            options: customerRes.data.map(c => ({ text: `${c.customerName} (${c.mobile})`, value: c.mobile })),
            openOnFocus: false,
            searchField: ["value"],
            maxItems: 1,
            persist: false,
            create: true,
            createOnBlur: true,
            selectOnTab: true,
            placeholder: "...شماره موبایل",
            
            render: {
                item(data, escape) {
                    return `<div>${escape(data.value)}</div>`;
                },
            },
            
            onChange(value) {
                if (!value) return;
                const customer = customerRes.data.find(x => x.mobile === value);
                if (!customer) return;
                
                cusTomSelect.setValue(customer.id);
                flashItem(cusTomSelect.control);
            },
        });
    const repTelTsInput = document.getElementById("repTel-ts-control");
    repTelTsInput.setAttribute("inputmode", "numeric");
    repTelTsInput.addEventListener("input", handlerMobileInput);*/
};

let reportModalLoaded = false;
const ShowModalReport = async (model = null, passType = 0, dealId = null, dealSerialNumber = null) => {
    const repTelElm = document.getElementById("repTel");
    const selectElm = document.getElementById("repCustomerTxt");
    
    if (!reportModalLoaded) {
        await Promise.all([initReportModal(), initCustomerOpReport()]);
        reportModalLoaded = true;
    }
    
    dealIdReport = dealId;
    dealSerialNumberReport = dealSerialNumber;
    typeReportSubmit = passType;
    
    if (model) {
        let { id = null, agentName, companyName, customerCode, mobile } = model;
        repTelElm.value = mobile;
        
        if (id) {
            selectElm.tomselect.setValue(id, true);
        } else {
            const customerCodeText = customerCode ? ` (کد مشتری ${customerCode})` : "";
            const text = `${companyName ?? ""} ${agentName}${customerCodeText}`.trim();
            
            const customId = `custom-${text}`;
            selectElm.tomselect.addOption({ value: customId, text });
            selectElm.tomselect.setValue(customId, true);
        }
        selectElm.tomselect.disable();
        toggleElementsDisabled(repTelElm, true);
    }
    
    /*createRating("reportRate", {
        count: 10,
        value: 0,
        resultId: "reportRateResult",
        onRate: (val) => {
            console.log(val);
        },
    });*/
    
    ModalToggle("#ModalReport");
};
const closeModalReport = () => {
    oldReport = null;
    $("#repCustomerTxt").val(null).trigger("change");
    document.getElementById("resultFollowUpContainer").classList.add("d-none");
    ModalToggle("#ModalReport");
    mySer.emptyForm("ReportFormId");
    const repTelElm = document.getElementById("repTel");
    const selectElm = document.getElementById("repCustomerTxt");
    selectElm.tomselect.enable();
    toggleElementsDisabled(repTelElm, false);
};


async function getAllReports(showLoading = true) {
    const containerReport = document.getElementById("allReportsId");
    if (!containerReport) return;
    
    return new Promise(async (resolve, reject) => {
        const data = filterReportConfig || {};
        
        const model = {
            ...data,
            filt: data.filterVar,
            serVal: data.varSearch,
            count: data.count ?? 50,
            _type: data._type ?? -1,
        };
        
        try {
            showLoading && FLshowLoading();
            
            const res = await axios.post("/Report/GetAllReportPartial", model);
            if (res.status === 200) {
                containerReport.innerHTML = res.data.html;
                
                const select = document.getElementById("reportCountSelect");
                const lastOption = select?.querySelector("option[value='-1']");
                const totalCount = res.data.count ?? 0;
                
                if (select && lastOption) {
                    lastOption.textContent = select.value === "-1" ? `همه (${totalCount})` : "همه";
                    
                    if (!data.count) {
                        if (totalCount < 50) {
                            select.value = "-1";
                            lastOption.textContent = `همه (${totalCount})`;
                        } else {
                            select.value = "50";
                        }
                    }
                }
            }
            
            return resolve();
        } catch (err) {
            console.error(err);
            containerReport.innerHTML = err.response.data; // "خطا در دریافت گزارش ها :(";
            return reject();
        } finally {
            FLshowLoading(false);
        }
    });
}

const ShowUpdateReport = async (id, passType = 0) => {
    if (!reportModalLoaded) {
        await initReportModal();
        await initCustomerOpReport();
        reportModalLoaded = true;
    }
    
    typeReportSubmit = passType;
    $.get(baseUrl + "Report/GetReport/" + id, async (res) => {
        oldReport = res;
        dealIdReport = res.dealId;
        dealSerialNumberReport = res.dealSerialNumber;
        /*const data = {
            id: res.customerId,
            text: res.customerTxt,
        };
        
        const newOption = new Option(data.text, data.id, true, true);
        $("#repCustomerTxt").append(newOption).trigger("change");*/
        
        mySer.convertJsonToForm(res, "ReportFormId");
        if (res.customerId === null && res.customerTxt) {
            const selectTomCustomers = document.getElementById("repCustomerTxt");
            const customId = `custom-${res.customerTxt}`;
            selectTomCustomers.tomselect.addOption({ value: customId, text: res.customerTxt });
            selectTomCustomers.tomselect.setValue(customId, true);
        }
        $("#ModalReport").modal("toggle");
    });
};


const DelReport = (item, passType = 0) => {
    Swal.fire({
        title: "حذف!",
        text: "آیا مطمئن هستید که میخواهید حذف کنید؟",
        icon: "warning",
        showCancelButton: true,
        cancelButtonColor: "#adadad",
        confirmButtonColor: "#e64942",
        cancelButtonText: "انصراف",
        confirmButtonText: "بله، حذف شود",
    }).then((willDelete) => {
        if (willDelete.isConfirmed) {
            delReportOk(item, passType);
        }
    });
};


const delReportOk = (item, passType) => {
    FLshowLoading();
    $.get(baseUrl + "Report/DelReport/" + item, async function (res) {
        if (res.status === 200) {
            if (passType === 0) {
                const currentPath = window.location.pathname.toLowerCase();
                if (currentPath.includes("report")) {
                    await getAllReports();
                } else if (currentPath.includes("dashboard")) {
                    await getDashReports();
                }
            } else if (passType === 4) {
                await refreshDealReports();
            }
            
            toastifySuccess(res.message);
        } else if (res.status === 201) {
            toastifyError(res.message);
        }
        FLshowLoading(false);
    }).fail(() => {
        setTimeout(() => delReportOk(item, passType), 3000);
    });
};


const goTo = (url, blank = false) => {
    if (blank) {
        window.open(url);
    } else {
        window.location.href = url;
    }
};

var GotoTimeLine = (id) => {
    //window.location.href = '/TimeLine/Index?Id=' + id;
    window.open("/TimeLine/Index?Id=" + id);
};
function generateCodePColor(PCode) {
    let res = "";
    PCode.forEach(function (item) {
        if (item == "I" || item == "V" || item == "X") {
            res = res + "<spam style='color:darkred'>" + item + "</spam>";
        } else {
            res = res + "<spam>" + item + "</spam>";
        }
    });
    return res;
}
//جداکردن اعداد در
//////////////////////////////////////////////////

document.addEventListener("DOMContentLoaded", function () {
    document.addEventListener("input", function (e) {
        const el = e.target;
        if (!(el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement)) return;
        
        if (el.classList.contains("currencyNumber")) {
            handlerSeparateCurrencyNumbers(el);
            updateCurrencyHint(el);
        } else if (el.classList.contains("digitNumber") || el.classList.contains("digitNumberString")) {
            handlerSeparateDigitNumbers(e);
        } else if (el.classList.contains("mobileNumber")) {
            handlerMobileInput(e);
        } else if (el.classList.contains("IranMobileNumber")) {
            handlerIranMobileInput(e);
        }
        
        applyMinMaxValue(el);
    });
    
});


function applyMinMaxValue(el) {
    const min = el.getAttribute("min");
    const max = el.getAttribute("max");
    
    if (min === null && max === null) return;
    
    let value = ConvertToNumEnglishCharNoCallBack(el.value)
        .replace(/,/g, "")
        .trim();
    
    if (value === "" || isNaN(value)) return;
    
    let numValue = Number(value);
    
    if (min !== null && !isNaN(min) && numValue < Number(min)) {
        numValue = Number(min);
    }
    
    if (max !== null && !isNaN(max) && numValue > Number(max)) {
        numValue = Number(max);
    }
    
    if (el.classList.contains("currencyNumber")) {
        el.value = numValue.toLocaleString();
    } else {
        el.value = numValue;
    }
}
const getGeneralRef = () => {
    return new Promise(resolve => {
        $.get(baseUrl + "Person/_GeneralReferencePartial?FormName=Person", (res) => {
            $("#GeneralRefId").children().remove();
            $("#GeneralRefId").append(res);
            if ($("#PId").val().length > 0)
                getallPersonAction();
            resolve();
        });
    });
};


// functions
function funcReverseString(str) {
    return str.split("").reverse().join("");
}

// event handlers
function handlerSeparateDigitNumbers(e) {
    e.target.value = e.target.value.replace(/[^\d.]/g, "").replace(/(\..*?)\.+/g, "$1");
}

function handlerMobileInput(e) {
    let el = e.target;
    
    if (el.tagName === "TEXTAREA") {
        let lines = el.value.split("\n").map(line => normalizeMobile(line));
        el.value = lines.join("\n");
    } else {
        el.value = normalizeMobile(el.value);
    }
    function normalizeMobile(val) {
        val = toEnglishDigits(val).replace(/[\s\-()]/g, "");
        
        if (val.startsWith("00")) {
            val = "+" + val.slice(2);
        }
        
        val = val.replace(/(?!^\+)\D/g, "");
        
        if (val.startsWith("+")) {
            val = val.slice(0, 16);
        } else {
            val = val.slice(0, 15);
        }
        
        return val;
    }
}
function handlerIranMobileInput(e) {
    let val = e.target.value;
    //val = "09" + val.slice(2).replace(/\D/g, "");
    if (val.length > 9) {
        val = val.slice(0, 9);
    }
    e.target.value = val;
}


function handlerSeparateCurrencyNumbers(el) {
    const numericValue = getNumericValue(el.value);
    
    el.value = numericValue
        ? Number(numericValue).toLocaleString()
        : "";
    
    /*const thisElement = e.target;
    
    let thisElementValue = ConvertToNumEnglishCharNoCallBack(thisElement.value);
    
    thisElementValue = thisElementValue.replace(/\D/g, "");
    thisElementValue = thisElementValue.replace(/,/g, "");
    if (isNaN(Number(thisElementValue))) {
        alert("لطفا از وارد کردن حروف خودداری فرمایید !");
        return false;
    }
    let seperatedNumber = thisElementValue.toString();
    seperatedNumber = funcReverseString(seperatedNumber);
    seperatedNumber = seperatedNumber.split("");
    let tmpSeperatedNumber = "";
    j = 0;
    for (let i = 0; i < seperatedNumber.length; i++) {
        tmpSeperatedNumber += seperatedNumber[i];
        j++;
        if (j === 3) {
            tmpSeperatedNumber += ",";
            j = 0;
        }
    }
    seperatedNumber = funcReverseString(tmpSeperatedNumber);
    if (seperatedNumber[0] === ",") seperatedNumber = seperatedNumber.replace(",", "");
    thisElement.value = seperatedNumber;*/
}

function getNumericValue(value) {
    if (!value) return "";
    
    return ConvertToNumEnglishCharNoCallBack(value).replace(/\D/g, "");
}


function getPriceTextElement(input) {
    
    const selector = input.dataset.priceText;
    
    if (!selector)
        return null;
    
    if (selector === "next")
        return input.nextElementSibling;
    
    return document.querySelector(selector);
}

function updateCurrencyHint(input) {
    const targetSelector = input.dataset.currencyHint;
    if (!targetSelector) return;
    const target = targetSelector === "next" ? input.nextElementSibling : document.querySelector(targetSelector);
    if (!target) return;
    if (!target.classList.contains("currencyHint")) return;
    
    const amount = Number(getNumericValue(input.value));
    
    target.textContent = formatAmountHint(amount);
}

function formatAmountHint(rialAmount) {
    if (isNaN(rialAmount)) return "";
    
    const toman = Math.floor(rialAmount / 10);
    
    if (!toman) return "";
    
    const billion = Math.floor(toman / 1_000_000_000);
    const million = Math.floor((toman % 1_000_000_000) / 1_000_000);
    const thousand = Math.floor((toman % 1_000_000) / 1_000);
    const remain = toman % 1_000;
    
    const parts = [];
    
    if (billion)
        parts.push(`${billion} میلیارد`);
    
    if (million)
        parts.push(`${million} میلیون`);
    
    if (thousand)
        parts.push(`${thousand} هزار`);
    
    if (remain)
        parts.push(remain.toString());
    
    return parts.join(" و ") + " تومان";
}


const SeperatedNumberGenerate = (val) => {
    let seperatedNumber = SeparatedNumberRemove(val.toString());
    seperatedNumber = funcReverseString(seperatedNumber);
    seperatedNumber = seperatedNumber.split("");
    let tmpSeperatedNumber = "";
    j = 0;
    for (let i = 0; i < seperatedNumber.length; i++) {
        tmpSeperatedNumber += seperatedNumber[i];
        j++;
        if (j == 3) {
            tmpSeperatedNumber += ",";
            j = 0;
        }
    }
    seperatedNumber = funcReverseString(tmpSeperatedNumber);
    if (seperatedNumber[0] === ",") seperatedNumber = seperatedNumber.replace(",", "");
    
    return seperatedNumber;
    
};


const SeparatedNumberRemove = (val) => {
    if (typeof val !== "string") return val;
    return val.replace(/[,\u066C\u060C]/g, "").trim();
};

//////////////////////////////////////////////////


function PublicConvertToTable(Selector, _data, SelectFunction, RemoveFunction, UpdateFunction) {
    
    var tb = "<tr class=mp>";
    $.each(_data, function (Key, Val) {
        $.each(Val, function (k, v) {
            if (k !== "Id") {
                tb = tb + "<td class=mp>" + v + "</td>";
            }
        });
        tb = tb + "<td class='mp '>";
        if ((SelectFunction !== null))
            tb = tb + "<button title='انتخاب'  class='mp btn btn-success' onclick='" + SelectFunction + "(\"" + Val.Id + "\")' ><i class='mp fas fa-check'></i></button>";
        if ((RemoveFunction !== null))
            tb = tb + "<button title='حذف' class='mp btn btn-danger' onclick='" + RemoveFunction + "(\"" + Val.Id + "\")' ><i class='mp fas fa-trash-alt'></i></button>";
        if ((UpdateFunction !== null))
            tb = tb + "<button title='ویرایش' class='mp btn btn-warning' onclick='" + UpdateFunction + "(\"" + Val.Id + "\")' ><i class='mp fas fa-pencil-alt'></i></button>";
        tb = tb + "</td></tr>";
    });
    var _Selector = "#" + Selector;
    $(_Selector + " tr").remove();
    $(_Selector).append(tb);
}
function PublicConvertToTableNoID(Selector, _data, SelectFunction, RemoveFunction, UpdateFunction, FildAbort) {
    
    var tb = "<tr class=mp>";
    $.each(_data, function (Key, Val) {
        
        if (!Val.isDeleted) {
            $.each(Val, function (k, v) {
                var flag = true;
                var items = FildAbort;
                
                items.map((item) => {
                    if (k == item)
                        flag = false;
                });
                if (flag) {
                    if (k == "phones") {
                        var phonestr = "";
                        if (v.length > 0) {
                            $.each(v, (code, data) => {
                                phonestr = phonestr + "&nbsp; (" + data.proviceCode + "-" + data.number + ") ";
                                
                            });
                        }
                        tb = tb + "<td class=mp>" + phonestr + "</td>";
                    } else {
                        tb = tb + "<td class=mp>" + v + "</td>";
                    }
                }
            });
            tb = tb + "<td class='mp '>";
            if ((SelectFunction !== null))
                tb = tb + "<button title='انتخاب' style='height: 100%; width: 20%;'  class='buttonGreen' onclick='" + SelectFunction + "(\"" + Key + "\")' ><i class='mp fas fa-check'></i></button>";
            if ((UpdateFunction !== null))
                tb = tb + "<button title='ویرایش' style='height: 100%; width: 20%;' class='buttonGreen' onclick='" + UpdateFunction + "(\"" + Key + "\")' ><i class='mp fa fa-pencil'></i></button>";
            if ((RemoveFunction !== null))
                tb = tb + "<button title='حذف' style='height: 100%; width: 20%;' class='buttonGreen' onclick='" + RemoveFunction + "(\"" + Key + "\")' ><i class='mp fa fa-trash'></i></button>";
            
            tb = tb + "</td></tr>";
        }
    });
    var _Selector = "#" + Selector;
    $(_Selector + " tr").remove();
    $(_Selector).append(tb);
}


/////////

var AddItem = (_grCode, _grTxt, _parentCode, _Target, _type) => {
    Swal.fire({
        title: "اضافه کردن آیتم جدید",
        input: "text",
        inputPlaceholder: _grTxt,
        confirmButtonText: "ثبت",
    }).then((result) => {
        if (result.isConfirmed && result.value) {
            var model = {
                grCode: _grCode,
                grTxt: _grTxt,
                itemText: result.value,
                parentCode: _parentCode,
            };
            
            $.post(baseUrl + "TreeRegister/AddItem", model, function () {
                toastifySuccess("عملیات با موفقیت انجام شد.");
                getOpItem(_grCode, _Target, _type);
            });
        }
    });
    /*
        swal("اضافه کردن آیتم جدید", {
            content: "input",
            title: _grTxt,
            button: "ثبت"
        })
            .then((value) => {
    
                var model = {
                    grCode: _grCode,
                    grTxt: _grTxt,
                    itemText: value,
                    parentCode: _parentCode
                }
    
                if (value != null) {
                    $.post(baseUrl + "TreeRegister/AddItem", model, function () {
    
                        toastifySuccess('عملیات با موفقیت انجام شد. ');
                        getOpItem(_grCode, _Target, _type);
                    })
                }
            });*/
};


var getOpItem = (grCode, _target, _type) => {
    
    
    $.get(baseUrl + "TreeRegister/getItems?groupCode=" + grCode, function (result) {
        $("#" + _target).children().remove();
        
        switch (_type) {
            case 1:
                $("#" + _target).select2({
                    data: result,
                    dir: "rtl",
                    placeholder: "انتخاب",
                    selectionCssClass: "form-control",
                });
                
                
                $("#" + _target).val(null).trigger("change");
                
                break;
            case 2:
                
                
                var arr = [];
                if (grCode == 6) {
                    AllPhenotip = result;
                    if (CheckedPhenotips != null) {
                        CheckedPhenotips.forEach
                                        ((item) => {
                                                arr.push(item.phenoCode);
                                            },
                                        );
                    }
                    
                }
                
                var ul = [];
                result.forEach((item, key) => {
                    const inpEl = $(document.createElement("input"));
                    if (arr == null) {
                        inpEl.attr
                             ({
                                 id: `checkBox${item.id}`,
                                 name: item.text,
                                 value: item.id,
                                 type: "checkbox",
                                 class: " form-check-input",
                                 style: "cursor:pointer",
                                 onchange: "AddOrRemovePhenotip(\"checkBox" + item.id + "\")",
                                 checked: false,
                             });
                    } else {
                        const found = arr.find(element => element == item.id);
                        const flag = found ? true : false;
                        
                        if (flag) {
                            inpEl.attr
                                 ({
                                     id: `checkBox${item.id}`,
                                     name: item.text,
                                     value: item.id,
                                     type: "checkbox",
                                     class: " form-check-input",
                                     style: "cursor:pointer",
                                     onchange: "AddOrRemovePhenotip(\"checkBox" + item.id + "\")",
                                     checked: true,
                                 });
                        } else {
                            inpEl.attr
                                 ({
                                     id: `checkBox${item.id}`,
                                     name: item.text,
                                     value: item.id,
                                     type: "checkbox",
                                     class: " form-check-input",
                                     style: "cursor:pointer",
                                     onchange: "AddOrRemovePhenotip(\"checkBox" + item.id + "\")",
                                     checked: false,
                                 });
                        }
                    }
                    
                    
                    ul.push(inpEl);
                    const labelEl = "<label class=' phenotic' for='checkBox" + item.id + "'  >" + item.text + "</label>";
                    ul.push(labelEl);
                });
                
                $("#" + _target).children().remove();
                $("#" + _target).append(ul);
                break;
            default:
                
                break;
        }
    });
};


function insertParam(key, value) {
    key = encodeURIComponent(key);
    value = encodeURIComponent(value);
    
    // kvp looks like ['key1=value1', 'key2=value2', ...]
    var kvp = document.location.search.substr(1).split("&");
    let i = 0;
    
    for (; i < kvp.length; i++) {
        if (kvp[i].startsWith(key + "=")) {
            let pair = kvp[i].split("=");
            pair[1] = value;
            kvp[i] = pair.join("=");
            break;
        }
    }
    
    if (i >= kvp.length) {
        kvp[kvp.length] = [key, value].join("=");
    }
    
    // can return this or...
    let params = kvp.join("&");
    
    // reload page with new params
    document.location.search = params;
}


const getTodayDateTime = () => {
    return new Promise(resolve => {
        $.get(baseUrl + "home/GetTodayDateTime", (res) => {
            resolve(res.toString());
        });
    });
};

const todayDate = () => {
    return new Promise(resolve => {
        $.get(baseUrl + "home/GetTodayDate", (res) => {
            resolve(res);
        });
    });
};

const tomorrowDate = () => {
    return new Promise(resolve => {
        $.get(baseUrl + "home/GetTomorrowDate", (res) => {
            resolve(res);
        });
    });
};

class GetDate {
    getTodayDateTime = () => this.#getDate(5);
    
    getTodayDate = () => this.#getDate(4);
    
    getTomorrowDate = () => this.#getDate(6);
    
    getCurrentWeek = () => this.#getDate(1);
    
    getCurrentMonth = () => this.#getDate(2);
    
    getCurrentYear = () => this.#getDate(3);
    
    #getDate(type) {
        return new Promise(resolve => {
            $.get(baseUrl + `home/GetDate?type=${type}`, (res) => {
                if ([1, 2, 3].includes(type)) {
                    res = res.split("_");
                    if (type === 2) {
                        res.push(this.#getMonthName(res[0].slice(5, 7)), res[0].slice(0, 4));
                    } else if (type === 3) {
                        res.push(res[0].slice(0, 4));
                    }
                }
                resolve(res);
            });
        });
    }
    
    #getMonthName = (monthNum) => {
        const monthName = {
            "01": "فروردین",
            "02": "اردیبهشت",
            "03": "خرداد",
            "04": "تیر",
            "05": "مرداد",
            "06": "شهریور",
            "07": "مهر",
            "08": "آبان",
            "09": "آذر",
            "10": "دی",
            "11": "بهمن",
            "12": "اسفند",
        };
        return monthName[monthNum];
    };
    
}


class CrmService {
    /**
     * دریافت لیست کاربران بر اساس نوع کاربر.
     * @param {number} type
     * @returns {Promise<Array>} لیست کاربران
     *
     * @example
     * const users = await CrmSer.getUsers(1);
     *  // 1: همه کاربران
     *  // 2: کارشناسان فروش
     *  // 3: کارشناسان تامین
     */
    async getUsers(type) {
        const response = await fetch(`/Account/GetUsers?type=${type}`);
        
        if (!response.ok) {
            throw new Error("خطا در دریافت لیست کاربران");
        }
        
        return await response.json();
    }
}

class MyService {
    emptyForm(
        formId,
        defaultVals = [], // [{ name: 'Live', val: 'true' },{ name: 'address', val: 'رسالت' }]
        blacklist = [],    // ['name1', 'name2']
    ) {
        this.RemoveAlertForm(formId);
        const form = document.getElementById(formId);
        const elements = form.elements;
        for (const elm of elements) {
            const name = elm.name;
            let defVal = null;
            
            if (name) {
                if (blacklist.includes(name)) continue;
                defVal = defaultVals.find(v => v.name === name)?.val ?? null;
            }
            switch (elm.type) {
                case "text":
                case "textarea":
                case "number":
                case "tel":
                    if (elm.tomselect) {
                        elm.tomselect.clear(true);
                        if (defVal != null) elm.tomselect.setValue(defVal, true);
                    } else {
                        elm.value = defVal ?? "";
                    }
                    break;
                case "select-one":
                case "select-multiple":
                    if (elm.tomselect) {
                        
                        elm.tomselect.clear(true);
                        if (defVal != null) elm.tomselect.setValue(defVal, true);
                    } else {
                        elm.value = defVal ?? "";
                        elm.dispatchEvent(new Event("change"));
                    }
                    break;
                
                case "checkbox":
                    elm.checked = Boolean(defVal);
                    break;
                
                case "file":
                    elm.value = defVal ?? "";
                    const previewId = elm.getAttribute("data-showList");
                    if (previewId) {
                        const previewDiv = document.getElementById(previewId);
                        if (previewDiv) previewDiv.replaceChildren();
                    }
                    const listName = elm.getAttribute("data-varList");
                    if (listName && window[listName]) {
                        window[listName] = [];
                    }
                    break;
                
                case "radio":
                    elm.checked = elm.hasAttribute("checked");
                    if (elm.checked) elm.dispatchEvent(new Event("change"));
                    break;
                
                default:
                    break;
            }
        }
        form.querySelectorAll(".currencyHint").forEach(elm => elm.innerHTML = "");
    }
    
    convertJsonToForm(js, formId, blackListArray = []) {
        const form = document.getElementById(formId);
        if (!form) return;
        
        for (const [key, value] of Object.entries(js)) {
            if (blackListArray.includes(key)) continue;
            
            try {
                const elm = form.elements[key];
                if (!elm) continue;
                
                if (elm.length > 1 && elm[0].type === "radio") {
                    for (const radio of elm) {
                        radio.checked = radio.value === String(value);
                        if (radio.checked) radio.dispatchEvent(new Event("change"));
                    }
                    continue;
                }
                
                switch (elm.type) {
                    case "text":
                    case "textarea":
                    case "number":
                    case "tel":
                        elm.value = value;
                        if (elm.dataset.currencyHint) {
                            updateCurrencyHint(elm);
                        }
                        break;
                    case "select-one":
                    case "select-multiple":
                        if (elm.tomselect) {
                            elm.tomselect.setValue(value ?? [], true);
                        } else {
                            if (elm.multiple) {
                                const values = Array.isArray(value) ? value.map(String) : [];
                                Array.from(elm.options).forEach(option => {
                                    option.selected = values.includes(option.value);
                                });
                            } else {
                                elm.value = value ?? "";
                            }
                            elm.dispatchEvent(new Event("change"));
                        }
                        
                        break;
                    case "radio":
                        const radioBtn = form.querySelector(`input[name="${key}"][value="${value}"]`);
                        if (radioBtn) radioBtn.checked = true;
                        if (radioBtn.checked) radioBtn.dispatchEvent(new Event("change"));
                        
                        break;
                    case "checkbox":
                        elm.checked = Boolean(value);
                        elm.dispatchEvent(new Event("change"));
                        break;
                    default:
                        break;
                }
            } catch (err) {
                console.log(err);
            }
        }
        
        
        const allAmounts = form.querySelectorAll(".inpAmount");
        allAmounts.forEach(item => {
            const num = Number(item.value);
            item.value = num === 0 ? "" : num.toLocaleString();
        });
    }
    
    
    convertFormToJSON(formId, blackListArray = [], elm = null, selector = null) {
        let formEl;
        
        if (formId) {
            formEl = document.getElementById(formId.replace("#", ""));
        } else if (elm) {
            formEl = elm;
        } else if (selector) {
            formEl = document.querySelector(selector);
        }
        
        const disabledEls = Array.from(formEl.querySelectorAll("input:disabled, select:disabled, textarea:disabled"));
        disabledEls.forEach(el => el.removeAttribute("disabled"));
        
        if (formEl.tagName.toLowerCase() !== "form") {
            return this.getJsonFromDiv(formEl, blackListArray);
        }
        
        const formData = new FormData(formEl);
        const jsonData = {};
        for (let [name, value] of formData.entries()) {
            if (blackListArray.includes(name)) continue;
            
            const field = formEl.querySelector(`[name="${name}"]`);
            
            if (value === "null") {
                jsonData[name] = null;
                continue;
            }
            
            let parsedValue = value;
            
            if (field?.classList.contains("currencyNumber") || field?.classList.contains("digitNumber")) {
                parsedValue = Number(SeparatedNumberRemove(value));
                
            } else if (field?.classList.contains("valueNumber")) {
                parsedValue = Number(value);
            }
            
            if (field?.multiple) {
                if (!jsonData[name]) {
                    jsonData[name] = [];
                }
                
                jsonData[name].push(parsedValue);
            } else {
                jsonData[name] = parsedValue;
            }
        }
        
        const checkboxes = formEl.querySelectorAll(`input[type="checkbox"]`);
        checkboxes.forEach(cb => {
            if (blackListArray.includes(cb.name)) return;
            jsonData[cb.name] = cb.checked;
        });
        
        disabledEls.forEach(el => el.setAttribute("disabled", "disabled"));
        return jsonData;
    }
    
    getJsonFromDiv(divEl, blackListArray = []) {
        if (!divEl) return;
        
        const jsonData = {};
        const elements = divEl.querySelectorAll("input, select, textarea");
        
        for (const el of elements) {
            if (!el.name || blackListArray.includes(el.name)) continue;
            
            let value;
            if (el.type === "radio") {
                if (el.checked) value = el.value !== "null" ? el.value : null;
                else continue;
            } else if (el.type === "checkbox") {
                value = el.checked;
            } else {
                value = el.value !== "null" ? el.value : null;
            }
            
            
            if (value != null && typeof value === "string") {
                value = toEnglishDigits(value);
            }
            
            if (el.classList.contains("currencyNumber") || el.classList.contains("digitNumber")) {
                jsonData[el.name] = Number(SeparatedNumberRemove(value));
            } else if (el.classList.contains("valueNumber")) {
                jsonData[el.name] = Number(value);
            } else {
                jsonData[el.name] = value;
            }
        }
        
        return jsonData;
    }
    
    
    async CheckValidForm(formId) {
        return new Promise(async (resolve) => {
            const form = document.getElementById(formId.replace("#", ""));
            const elements = Array.from(form.elements);
            
            let isValid = true;
            let firstInvalidElement = null;
            const addAlert = (el, className = "AlertReq") => {
                if (!el) return;
                
                if (!firstInvalidElement) {
                    firstInvalidElement = el;
                    
                    setTimeout(() => {
                        if (el.tomselect) {
                            el.tomselect.focus();
                        } else if (el.hasAttribute("data-select2-id")) {
                            $(`#select2-${el.id}-container`).parent().focus();
                        } else {
                            el.focus();
                        }
                    });
                }
                
                if (el.tomselect) {
                    el.tomselect.control.classList.add(className);
                    el.tomselect.on("change", remove);
                } else {
                    el.classList.add(className);
                    el.addEventListener("input", remove);
                    el.addEventListener("change", remove);
                }
                
                function remove() {
                    if (el.tomselect) {
                        el.tomselect.control.classList.remove(className);
                        el.tomselect.off("change", remove);
                    } else {
                        el.classList.remove(className);
                        el.removeEventListener("input", remove);
                        el.removeEventListener("change", remove);
                    }
                }
            };
            
            for (const el of elements) {
                if (el.required) {
                    switch (el.tagName) {
                        case "INPUT":
                            if (el.type === "radio") {
                                const radios = elements.filter(f => f.name === el.name && f.required);
                                if (!radios.some(r => r.checked)) {
                                    radios.forEach(r => {
                                        const target = r.getAttribute("data-alertId");
                                        addAlert(document.getElementById(target), "AlertReqRadio");
                                    });
                                    isValid = false;
                                }
                                break;
                            }
                            
                            if (["text", "number", "password", "tel"].includes(el.type)) {
                                if (el.value.trim().length === 0) {
                                    addAlert(el);
                                    isValid = false;
                                }
                                
                                
                                if ((el.getAttribute("data-slots") === "ymd" || el.hasAttribute("data-mask-date")) && el.value && !isValidDate(el.value)) {
                                    toastifyError("تاریخ معتبر نیست!");
                                    addAlert(el);
                                    isValid = false;
                                }
                                break;
                            }
                            
                            if (el.type === "checkbox" && !el.checked) {
                                const target = r.getAttribute("data-alertId");
                                addAlert(document.getElementById(target), "AlertReqRadio");
                                isValid = false;
                                break;
                            }
                            break;
                        
                        case "TEXTAREA":
                            if (el.value.trim().length === 0) {
                                addAlert(el);
                                isValid = false;
                                break;
                            }
                            if (el.classList.contains("mobileNumber")) {
                                let lines = el.value.split("\n").map(l => l.trim()).filter(l => l.length > 0);
                                for (let line of lines) {
                                    if (line.length <= 8) {
                                        toastifyError("شماره موبایل معتبر نیست: " + line);
                                        addAlert(el);
                                        isValid = false;
                                        break;
                                    }
                                }
                            }
                            break;
                        
                        case "SELECT":
                            if (el.hasAttribute("data-select2-id")) {
                                let hasSelection = Array.from(el.options).some(opt => opt.selected);
                                if (!hasSelection) {
                                    $("#select2-" + el.id + "-container").parent().addClass("AlertReq");
                                    $(el).on("select2:select", function () {
                                        $("#select2-" + el.id + "-container").parent().removeClass("AlertReq");
                                    });
                                    isValid = false;
                                }
                            } else if (el.value.trim().length === 0) {
                                addAlert(el);
                                isValid = false;
                            }
                            break;
                    }
                    
                    if (el.classList.contains("mobileNumber") && el.value.trim().length > 0 && el.value.trim().length < 8) {
                        toastifyError("شماره موبایل معتبر نیست!");
                        addAlert(el);
                        isValid = false;
                    }
                } else {
                    if (el.getAttribute("data-slots") === "ymd" && el.value.trim()) {
                        isValid = await _checkdate(el.value) && isValid;
                    }
                }
                
            }
            resolve(isValid);
        });
    }
    
    RemoveAlertForm(formId) {
        const elements = Array.from(document.getElementById(formId.replace("#", "")).elements);
        
        const removeAlert = (el, className = "AlertReq") => {
            el?.classList.remove(className);
        };
        
        
        for (const item of elements) {
            if (!item.required) continue;
            
            switch (item.tagName) {
                case "INPUT":
                    if (item.type === "radio") {
                        item.checked = false;
                        const target = item.getAttribute("data-alertId");
                        removeAlert(document.getElementById(target), "AlertReqRadio");
                        break;
                    }
                    if (["text", "number", "password", "tel"].includes(item.type)) {
                        removeAlert(item);
                        break;
                    }
                    if (item.type === "checkbox") {
                        const target = item.getAttribute("data-alertId");
                        removeAlert(document.getElementById(target), "AlertReqRadio");
                        break;
                    }
                    break;
                case "TEXTAREA":
                    removeAlert(item);
                    break;
                case "SELECT":
                    if (item.hasAttribute("data-select2-id")) {
                        $("#select2-" + item.id + "-container").parent().removeClass("AlertReq");
                    } else {
                        removeAlert(item.tomselect ? item.tomselect.control : item);
                    }
                    break;
                default:
                    break;
            }
            
        }
    }
}


////////////////////
const combineJson = (js1, js2) => {
    for (var i in js2) {
        js1[i] = js2[i];
    }
    
    return js1;
    
};

function toNetDateTimeString(raw) {
    const pad = (n) => (n < 10 ? "0" + n : n);
    const date = new Date(raw);
    return (
        date.getFullYear() +
        "-" +
        pad(date.getMonth() + 1) +
        "-" +
        pad(date.getDate()) +
        "T" +
        pad(date.getHours()) +
        ":" +
        pad(date.getMinutes()) +
        ":" +
        pad(date.getSeconds())
    );
}


/*$.MypostAjax = function (url, data, callbackSucces, callBackError) {
    return jQuery.ajax({
        type: "POST",
        url: url,
        contentType: "application/json",
        data: data,
        success: callbackSucces,
        error: callBackError,
    });
};*/


function AlertFormStatus(status, Message, liss) {
    
    
    if (status == 0) {
        $("#" + liss).text(Message);
        $("#" + liss).removeClass("bg-success text-white");
        $("#" + liss).addClass("bg-danger text-white");
        
        return false;
        
    } else {
        
        $("#" + liss).text("");
        $("#" + liss).removeClass("bg-danger text-white");
        $("#" + liss).addClass("bg-success text-white");
        
        return true;
    }
    
}


function ConvertToNumEnglishCharNoCallBack(value) {
    if (value == null) return null;
    
    return value.replace(/[۰-۹]/g, d => "۰۱۲۳۴۵۶۷۸۹".indexOf(d));
    
    /*    if (value === null) {
            return null;
        } else {
            
            let s = value;
            for (let i = 0; i < value.length; i++) {
                
                s = s.replace("۰", "0").replace("۱", "1")
                     .replace("۲", "2").replace("۳", "3")
                     .replace("۴", "4").replace("۵", "5")
                     .replace("۶", "6").replace("۷", "7")
                     .replace("۸", "8").replace("۹", "9");
            }
            
            return s;
        }*/
}

function ConvertToNumEnglishChar(item, callback) {
    if (item != null) {
        var j = item.length;
        var s = item;
        for (i = 0; i < j; i++) {
            
            s = s.replace("۰", "0").replace("۱", "1")
                 .replace("۲", "2").replace("۳", "3")
                 .replace("۴", "4").replace("۵", "5")
                 .replace("۶", "6").replace("۷", "7")
                 .replace("۸", "8").replace("۹", "9");
        }
        
        
        var detail = callback(s);
        
        return detail;
    }
}


function ConvertToNumPersianChar(item) {
    
    
    if (item === null) {
        return item;
    } else {
        
        var j = item.length;
        
        var s = item;
        for (i = 0; i < j; i++) {
            
            s = s.replace("0", "۰").replace("1", "۱")
                 .replace("2", "۲").replace("3", "۳")
                 .replace("4", "۴").replace("5", "۵")
                 .replace("6", "۶").replace("7", "۷")
                 .replace("8", "۸").replace("9", "۹");
        }
        
        return s;
    }
    
    
}


document.querySelectorAll(".chip").forEach(chip => {
    chip.addEventListener("click", () => {
        //const group = chip.dataset.group;
        const value = chip.dataset.value;
        
        /*document.querySelectorAll(`.chip[data-group='${group}']`)
                .forEach(c => c.classList.remove("active"));*/
        
        chip.classList.toggle("active");
    });
});


const toggleElementsDisabled = (elms, isDisabled = true) => {
    if (!elms) return;
    
    const items = elms instanceof NodeList || elms instanceof Array ? elms : [elms];
    items.forEach(el => {
        if (el.tomselect) {
            isDisabled ? el.tomselect.disable() : el.tomselect.enable();
        } else {
            el.disabled = isDisabled;
        }
    });
};

const toggleElementsRequired = (elms, isRequired) => {
    const items = elms instanceof NodeList || elms instanceof Array ? elms : [elms];
    items.forEach(el => el.toggleAttribute("required", isRequired));
};

(function () {
    const baseZ = 1060;
    const step = 10;
    const stack = [];
    let preventBackHandler = null;
    
    document.addEventListener("show.bs.modal", (e) => {
        const modal = e.target;
        
        if (!stack.includes(modal)) {
            stack.push(modal);
        }
        
        if (window.innerWidth <= 768 && stack.length === 1) {// وقتی اولین مودال باز میشه، بک رو ببند
            history.pushState(null, "", location.href);
            
            preventBackHandler = () => history.go(1);
            window.addEventListener("popstate", preventBackHandler);
        }
        
        setTimeout(() => updateZIndex(), 200);
    });
    
    document.addEventListener("hidden.bs.modal", (e) => {
        const modal = e.target;
        
        const idx = stack.indexOf(modal);
        if (idx > -1) stack.splice(idx, 1);
        
        if (stack.length === 0 && preventBackHandler) { // وقتی آخرین مودال بسته شد، بک رو آزاد کن
            window.removeEventListener("popstate", preventBackHandler);
            preventBackHandler = null;
        }
        
        updateZIndex();
    });
    
    function updateZIndex() {
        stack.forEach((m, i) => {
            const z = baseZ + i * step;
            m.style.zIndex = z;
            
            const backdrop = document.querySelectorAll(".modal-backdrop")[i];
            if (backdrop) backdrop.style.zIndex = z - 5;
        });
        
        document.body.classList.toggle("modal-open", stack.length > 0);
    }
})();


/*(function () {
    document.addEventListener("show.bs.modal", (e) => {
        const modal = e.target;
        if (!modal.classList.contains("modal")) return;
        
        const visible = document.querySelectorAll(".modal.show").length;
        const zIndex = 1060 + (10 * visible);
        modal.style.zIndex = zIndex;
        
        setTimeout(() => {
            const backdrops = Array.from(document.querySelectorAll(".modal-backdrop:not(.stacked)"));
            if (backdrops.length > 0) {
                const lastBackdrop = backdrops[backdrops.length - 1];
                lastBackdrop.style.zIndex = (zIndex - 5);
                lastBackdrop.classList.add("stacked");
            }
        });
    });
    
    document.addEventListener("hidden.bs.modal", () => {
        if (document.querySelectorAll(".modal.show").length > 0) {
            document.body.classList.add("modal-open");
        } else {
            document.body.classList.remove("modal-open");
        }
    });
})();*/

const fLshowLoadingElm = document.getElementById("FLshowLoading");
function FLshowLoading(show = true) {
    fLshowLoadingElm.style.display = show ? "" : "none";
}

function toBoolean(str) {
    const v = str?.trim().toLowerCase();
    if (v === "true" || v === "1" || v === "yes" || v === "on") return true;
    if (v === "false" || v === "0" || v === "no" || v === "off") return false;
    return Boolean(str);
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text)
             .then(() => {
                 toastifySuccess("کپی شد");
                 return true;
             })
             .catch(err => {
                 console.log(err);
                 return false;
             });
}

async function readOtp(inputElm, btnElm, timeout = 60000) {
    if (!navigator.credentials || !navigator.credentials.get) return;
    if (!inputElm) return;
    
    const ac = new AbortController();
    const t = setTimeout(() => ac.abort(), timeout);
    try {
        const cred = await navigator.credentials.get({
            otp: { transport: ["sms"] },
            signal: ac.signal,
        });
        if (cred && cred.code) {
            inputElm.value = cred.code;
            btnElm.click();
        }
    } catch (err) {
    }
    clearTimeout(t);
}


function createRating(containerId, options = {}) {
    const container = document.getElementById(containerId);
    if (!container) return;
    
    const result = options.resultId ? document.getElementById(options.resultId) : null;
    const starCount = options.count || 5;
    const defaultValue = options.value || 0;
    const onRate = options.onRate;
    
    container.innerHTML = "";
    for (let i = 1; i <= starCount; i++) {
        const star = document.createElement("span");
        star.innerHTML = `<svg style="width: 24px" viewBox="0 0 24 24" fill="currentColor"><path fill-rule="evenodd" d="M10.788 3.21c.448-1.077 1.976-1.077 2.424 0l2.082 5.006 5.404.434c1.164.093 1.636 1.545.749 2.305l-4.117 3.527 1.257 5.273c.271 1.136-.964 2.033-1.96 1.425L12 18.354 7.373 21.18c-.996.608-2.231-.29-1.96-1.425l1.257-5.273-4.117-3.527c-.887-.76-.415-2.212.749-2.305l5.404-.434 2.082-5.005Z" clip-rule="evenodd" /></svg>`;
        star.dataset.value = i;
        container.appendChild(star);
    }
    
    const stars = container.querySelectorAll("span");
    let selected = defaultValue;
    
    const highlight = (v) => {
        stars.forEach(s => s.classList.toggle("active", Number(s.dataset.value) <= v));
        if (result) result.textContent = `${v} از ${starCount}`;
    };
    
    stars.forEach(star => {
        const value = Number(star.dataset.value);
        star.addEventListener("mouseover", () => highlight(value));
        star.addEventListener("mouseout", () => highlight(selected));
        star.addEventListener("click", () => {
            selected = (selected === value) ? 0 : value;
            highlight(selected);
            onRate(selected);
        });
    });
    
    highlight(defaultValue);
}


// document.getElementById("InvoiceListModalId")?.addEventListener("click", function (e) {
document.addEventListener("click", function (e) {
    const target = e.target.closest("[data-bs-toggle='popover']");
    
    if (target) {
        
        let pop = bootstrap.Popover.getInstance(target);
        
        if (pop) {
            pop.toggle();
            return;
        }
        const options = {
            placement: target.dataset.bsPlacement || "top",
            customClass: target.dataset.bsCustomClass || "fMedium",
            trigger: "manual",
            html: target.dataset.bsHtml === "true",
        };
        
        pop = new bootstrap.Popover(target, options);
        pop.show();
    } else {
        document.querySelectorAll("[data-bs-toggle='popover']").forEach(el => {
            const openPop = bootstrap.Popover.getInstance(el);
            if (openPop) openPop.hide();
        });
    }
});

/*const openPopovers = new Set();
document.addEventListener("click", function (e) {
    const target = e.target.closest("[data-bs-toggle='popover']");
    
    if (target) {
        let pop = bootstrap.Popover.getInstance(target);
        if (pop) {
            pop.toggle();
            if (target.classList.contains("show")) {
                openPopovers.add(target);
            } else {
                openPopovers.delete(target);
            }
        } else {
            pop = new bootstrap.Popover(target, {
                placement: "top",
                customClass: "fMedium",
                trigger: "manual",
                html: true,
            });
            pop.show();
            openPopovers.add(target);
        }
    } else {
        openPopovers.forEach(el => {
            const pop = bootstrap.Popover.getInstance(el);
            if (pop) {
                pop.hide();
            }
        });
        openPopovers.clear();
    }
});*/

function debounce(fn, delay = 300) {
    let timer;
    return (...args) => {
        clearTimeout(timer);
        timer = setTimeout(() => fn(...args), delay);
    };
}


/**
 * Bootstrap modal.
 * @param {string|HTMLElement} target - Modal ID or element.
 * @param {"toggle" | "show" | "hide"} [action="toggle"] - Modal action.
 */
function ModalToggle(target, action = "toggle") {
    if (typeof target === "string" && !target.startsWith("#")) {
        target = "#" + target;
    }
    const modal = bootstrap.Modal.getOrCreateInstance(target);
    
    switch (action) {
        case "toggle":
            modal.toggle();
            break;
        case "show":
            modal.show();
            break;
        case "hide":
            modal.hide();
            break;
    }
}

const isModalOpen = (modal) => {
    const element = typeof modal === "string" ? document.getElementById(modal) : modal;
    return element?.classList.contains("show") === true;
};

/**
 * Bootstrap Tab.
 * @param {string|HTMLElement} target - Tab ID or element.
 */
function ShowTab(target) {
    if (typeof target === "string" && !target.startsWith("#")) {
        target = "#" + target;
    }
    const tab = bootstrap.Tab.getOrCreateInstance(target);
    tab?.show();
}


/**
 * createOption.
 * @param {string|HTMLElement} targetSelect - select ID or element.
 * @param {Array<text: String, value: String>} optionItems - optionItems
 * @param {boolean} [addDefault=true] - add default option
 * @param {string} [defaultText="انتخاب"] - default Text
 * @param {boolean} [selectTarget=false] - select target option
 */
function createOption(targetSelect, optionItems, addDefault = true, defaultText = "انتخاب", selectTarget = false) {
    let selectElm;
    if (typeof targetSelect === "string") {
        selectElm = document.getElementById(targetSelect.replace("#", ""));
    } else if (targetSelect instanceof HTMLElement) {
        selectElm = targetSelect;
    }
    
    if (!selectElm || selectElm.tagName !== "SELECT") {
        throw new Error("عنوان select پیدا نشد");
    }
    
    selectElm.innerHTML = "";
    
    const hasTarget = optionItems.some(item => item.target);
    
    if (addDefault && !hasTarget) {
        const o = document.createElement("option");
        o.innerText = defaultText;
        o.value = "";
        o.selected = true;
        selectElm.appendChild(o);
    }
    
    
    optionItems.forEach(item => {
        const o = document.createElement("option");
        
        o.innerText = item.text;
        o.value = item.value;
        
        if (selectTarget && item.target) {
            o.selected = true;
            o.style.backgroundColor = "#1967d24a";
        }
        
        selectElm.appendChild(o);
    });
    
}


const toEnglishDigits = (val) => {
    if (val === null || val === undefined) return val;
    
    return String(val)
        .replace(/[۰-۹]/g, d => String("۰۱۲۳۴۵۶۷۸۹".indexOf(d))) // Persian
        .replace(/[٠-٩]/g, d => String("٠١٢٣٤٥٦٧٨٩".indexOf(d)));// Arabic
};


function calculateTotalPrice(weight, unitPrice) {
    const w = parseFloat(SeparatedNumberRemove(weight)) || 0;
    const u = parseFloat(SeparatedNumberRemove(unitPrice)) || 0;
    
    return w * u;
}

function getUrlParams() {
    const params = new URLSearchParams(window.location.search);
    const state = {};
    
    for (const [key, value] of params.entries()) {
        state[key] = value;
    }
    
    return state;
}


function setUrlParams(paramsObj, options = { replace: true }) {
    const url = new URL(window.location.href);
    Object.entries(paramsObj).forEach(([key, value]) => {
        if (value === null || value === "" || value === undefined) {
            url.searchParams.delete(key);
        } else {
            url.searchParams.set(key, value.toString());
        }
    });
    if (options.replace) {
        history.replaceState(history.state, "", url);
    } else {
        history.pushState(history.state, "", url);
    }
}

/**
 * Flash highlight
 * @param {HTMLElement} el
 * @param {{duration?: number}, {color?: string}} options
 * @param {number} options.duration - animation duration in ms
 * @param {string} options.className - CSS class to apply
 */
function flashItem(el, options = {}) {
    if (!el) return;
    
    const {
        duration = 1200,
        color = "#e3f2fd",
    } = options;
    
    el.style.setProperty("--flash-duration", duration + "ms");
    el.style.setProperty("--flash-color", color);
    
    el.classList.remove("fill-flash");
    void el.offsetWidth;
    el.classList.add("fill-flash");
    
    setTimeout(() => el.classList.remove("fill-flash"), duration);
}


/*
function flashItem(el) {
    console.log(el);
    if (!el) return;
    el.classList.remove('highlight','fadeout'); // reset
    void el.offsetWidth; // force reflow to restart animation
    el.classList.add('highlight','fadeout');
}

document.addEventListener("click", e => {
    if (e.target.closest(".InfoDataItem_Prefactor")) {
        flashItem(e.currentTarget)
    }
})*/


const updateFilterSwitchState = ({ wrapperSelector, filterData, currentFilterValue, searchValue, onChange }) => {
    const wrapper = document.querySelector(wrapperSelector);
    if (!wrapper) return;
    
    const hasSearch = !!searchValue?.trim();
    if (!hasSearch) {
        wrapper.dataset.userInteracted = "false";
        
        wrapper.querySelectorAll(".filter-switch-item").forEach(item => {
            item.style.opacity = "1";
            const badge = item.querySelector(".filter-count-badge");
            if (badge) {
                badge.style.display = "none";
            }
        });
        
        return;
    }
    if (!filterData) return;
    
    let firstAvailableValue = null;
    const radios = wrapper.querySelectorAll(`.filter-switch-item input[type="radio"]`);
    if (!radios.length) return;
    radios.forEach(radio => {
        if (!radio.dataset.bound) {
            radio.addEventListener("click", () => {
                wrapper.dataset.userInteracted = "true";
            });
            radio.dataset.bound = "1";
        }
        
        const value = radio.value;
        const count = filterData?.[value];
        
        const item = radio.closest(".filter-switch-item");
        const badge = item?.querySelector(".filter-count-badge");
        
        if (count === undefined) {
            item.style.opacity = "1";
            if (badge) {
                badge.style.display = "none";
            }
            return;
        }
        
        if (badge) {
            badge.style.display = count > 0 ? "flex" : "none";
            badge.textContent = count;
        }
        
        item.style.opacity = count > 0 ? "1" : ".5";
        
        if (value !== "-1" && count > 0 && firstAvailableValue === null) {
            firstAvailableValue = value;
        }
    });
    if (wrapper.dataset.userInteracted === "true") return;
    if (filterData[currentFilterValue] > 0) return;
    if (!firstAvailableValue) return;
    
    const targetRadio = wrapper.querySelector(`input[value="${firstAvailableValue}"]`);
    if (!targetRadio) return;
    if (targetRadio.checked) return;
    
    targetRadio.checked = true;
    
    onChange?.(firstAvailableValue);
};


const updateTabSwitchState = ({ wrapperSelector, tabData, searchValue, filter }) => {
    const wrapper = document.querySelector(wrapperSelector);
    if (!wrapper) return;
    
    const hasSearch = !!searchValue?.trim();
    const hasFilter = filter !== undefined && filter !== null;
    let firstAvailableTab = null;
    
    const activeTab = wrapper.querySelector(".nav-link.active");
    const activeKey = activeTab?.dataset.tab;
    
    const tabs = wrapper.querySelectorAll(".nav-link");
    
    tabs.forEach(tab => {
        const key = tab.dataset.tab;
        const count = tabData[key]?.length ?? tabData[key] ?? 0;
        const badge = tab.querySelector(".filter-count-badge");
        
        if (!hasSearch && !hasFilter) {
            tab.style.opacity = "1";
            if (badge) {
                badge.style.display = "none";
            }
            return;
        }
        
        if (badge) {
            badge.style.display = count > 0 ? "flex" : "none";
            badge.textContent = count;
        }
        
        tab.style.opacity = count > 0 ? "1" : ".5";
        
        if (count > 0 && !firstAvailableTab) {
            firstAvailableTab = tab;
        }
    });
    
    if (tabData[activeKey]?.length) return;
    
    if (!firstAvailableTab) return;
    ShowTab(firstAvailableTab);
};


function initEyeToggle({ selector, onShow, onHide, showTitle = "نمایش", hideTitle = "مخفی کردن", titlePosition = "" }) {
    const btn = document.querySelector(selector);
    if (!btn) return;
    
    let isVisible = false;
    
    const visibleIcon = btn.querySelector(".openEye-icon");
    const hiddenIcon = btn.querySelector(".closedEye-icon");
    
    const titleAttribute = titlePosition ? `data-title-${titlePosition}` : "data-title";
    
    const updateUI = () => {
        visibleIcon?.classList.toggle("d-none", !isVisible);
        hiddenIcon?.classList.toggle("d-none", isVisible);
        btn.setAttribute(titleAttribute, isVisible ? hideTitle : showTitle);
    };
    
    btn.addEventListener("click", async () => {
        isVisible = !isVisible;
        toggleElementsDisabled(btn);
        if (isVisible) {
            await onShow?.();
        } else {
            await onHide?.();
        }
        toggleElementsDisabled(btn, false);
        
        updateUI();
    });
    
    updateUI();
    
    return {
        get isVisible() {
            return isVisible;
        },
        
        set isVisible(show) {
            isVisible = show;
            updateUI();
        },
    };
}
/**
 * Sends a GET request and returns response data.
 * @template T
 * @param {string} url
 * @returns {Promise<T|null>}
 */
async function getData(url) {
    try {
        const res = await axios.get(url);
        return res.status === 200 ? res.data : null;
    } catch (err) {
        console.error(err);
        toastifyError("خطا در دریافت اطلاعات");
        return null;
    }
}


/**
 * @typedef {Object} DeleteItemOptions
 * @property {string} url
 * @property {string} [title="حذف!"]
 * @property {string} [text="آیا مطمئن هستید که میخواهید حذف کنید؟"]
 * @property {(data: any) => (void|Promise<void>)} [callback]
 */

/**
 * Shows confirmation dialog and sends a DELETE request.
 * @param {DeleteItemOptions} options
 * @returns {Promise<boolean>}
 */
async function deleteItem(options) {
    const { url, callback = null, title = "حذف!", text = "آیا مطمئن هستید که میخواهید حذف کنید؟" } = options;
    
    return await confirmAlert({
        title,
        text,
        icon: "warning",
        confirmText: "بله، حذف شود",
        
        onConfirm: async () => {
            await sendApiRequest({
                method: "DELETE",
                url,
                onSuccess: async (res) => await callback?.(res),
            });
        },
    });
}

/**
 * Displays a customizable SweetAlert2 message without confirmation actions.
 *
 * @async
 * @param {Object} options Alert configuration.
 * @param {string} [options.title=""] Dialog title.
 * @param {string} [options.text=""] Dialog message.
 * @param {"warning"|"error"|"success"|"info"|"question"} [options.icon="info"] Alert icon.
 * @param {"light"|"dark"} [options.theme="light"] Theme of the popup.
 * @param {string} [options.confirmText="باشه"] Close button text.
 * @param {string} [options.confirmButtonColor="#0d6efd"] Confirm button color.
 * @returns {Promise<SweetAlertResult>} SweetAlert2 result.
 */
async function showAlert(
    {
        title = "",
        text = "",
        icon = "info",
        theme = "light",
        confirmText = "باشه",
        confirmButtonColor = "#0d6efd",
    } = {}) {
    
    return await Swal.fire({
        title,
        text,
        icon,
        theme,
        confirmButtonColor,
        confirmButtonText: confirmText,
    });
}

/**
 * Displays a customizable confirmation dialog using SweetAlert2.
 *
 * @async
 * @param {Object} options Alert configuration.
 * @param {string} [options.title="آیا مطمئن هستید؟"] Dialog title.
 * @param {string} [options.text=""] Dialog message.
 * @param {"warning"|"error"|"success"|"info"|"question"} [options.icon="warning"] Alert icon.
 * @param {"light"|"dark"} [options.theme="light"] Theme of the popup.
 * @param {string} [options.confirmText] Confirm button text. If omitted, a default text is used based on the icon.
 * @param {string} [options.cancelText="انصراف"] Cancel button text.
 * @param {string} [options.confirmButtonColor] Overrides the default confirm button color.
 * @param {string} [options.cancelButtonColor="#adadad"] Cancel button color.
 * @param {(result: SweetAlertResult) => (void|Promise<void>)} [options.onConfirm] Executed when the user confirms.
 * @param {(result: SweetAlertResult) => (void|Promise<void>)} [options.onCancel] Executed when the user cancels.
 * @returns {Promise<boolean>} Returns true if confirmed; otherwise false.
 */
async function confirmAlert(
    {
        title = "آیا مطمئن هستید؟",
        text = "",
        icon = "warning",
        theme = "light",
        confirmText,
        cancelText = "انصراف",
        confirmButtonColor,
        cancelButtonColor = "#adadad",
        onConfirm,
        onCancel,
    } = {}) {
    
    const primaryButtonTheme = {
        color: "#0d6efd",
        confirmText: "تایید",
    };
    
    const dangerButtonTheme = {
        color: "#e64942",
        confirmText: "بله، انجام شود",
    };
    
    const buttonThemes = {
        success: primaryButtonTheme,
        info: primaryButtonTheme,
        question: primaryButtonTheme,
        warning: dangerButtonTheme,
        error: dangerButtonTheme,
    };
    
    const buttonTheme = buttonThemes[icon] ?? dangerButtonTheme;
    
    const result = await Swal.fire({
        title,
        text,
        icon,
        theme,
        showCancelButton: true,
        confirmButtonColor: confirmButtonColor ?? buttonTheme.color,
        cancelButtonColor,
        confirmButtonText: confirmText ?? buttonTheme.confirmText,
        cancelButtonText: cancelText,
    });
    
    if (result.isConfirmed) {
        if (typeof onConfirm === "function")
            await onConfirm(result);
        
        return true;
    }
    
    if (typeof onCancel === "function")
        await onCancel(result);
    
    return false;
}

async function confirmDangerousDelete(
    {
        title = "حذف دائمی اطلاعات",
        text = "این عملیات قابل بازگشت نیست. آیا از حذف این اطلاعات اطمینان دارید؟",
        confirmText = "بله، حذف دائمی شود",
        cancelText = "انصراف",
    } = {}) {
    
    const result = await Swal.fire({
        title,
        html: `
            <div style="font-size:15px; line-height:2;">
                <div style="color:#dc3545; font-weight:bold; margin-bottom:8px;">
                    ⚠️ هشدار مهم
                </div>

                <div>${text}</div>

                <div style="
                    margin-top:15px;
                    padding:10px;
                    background:#fff3cd;
                    border:1px solid #ffe69c;
                    border-radius:6px;
                    color:#664d03;
                ">
                    این عملیات <strong>قابل بازگشت نیست</strong>.
                </div>

                <div style="margin-top:18px;">
                    برای تأیید، عبارت
                    <strong>حذف</strong>
                    را وارد کنید:
                </div>
            </div>
        `,
        input: "text",
        inputPlaceholder: "حذف",
        inputAttributes: {
            autocomplete: "off",
        },
        showCancelButton: true,
        
        confirmButtonColor: "#dc3545",
        cancelButtonColor: "#adadad",
        
        confirmButtonText: "بله، حذف دائمی شود",
        cancelButtonText: "انصراف",
        
        allowOutsideClick: false,
        allowEscapeKey: false,
        
        preConfirm: (value) => {
            if (value?.trim() !== "حذف") {
                Swal.showValidationMessage(
                    "برای تأیید باید عبارت «حذف» را وارد کنید.",
                );
                return false;
            }
            
            return true;
        },
    });
    
    return result.isConfirmed;
}


function renderMoneyHtml(value, { currency = "ريال", empty = "-", className = "" } = {}) {
    if (value == null || Number.isNaN(Number(value)))
        return empty;
    
    return `
        <span class="d-inline-flex gap-1 align-items-baseline ${className}">
            <span dir="ltr">${Number(value).toLocaleString()}</span>
            <span class="fs-small">${currency}</span>
        </span>
    `;
}
function renderDateTimeHtml(value, { empty = "-", className = "" } = {}) {
    if (!value) return empty;
    
    return `<span dir="ltr" class="${className}">${value.slice(0, -3).split(" ").join(" | ")}</span>`;
}


/**
 * Resolves a DOM element from an element reference, ID, or CSS selector.
 *
 * Supports:
 * - Element
 * - ID with or without "#"
 * - CSS selector
 *
 * @param {Element|string|null|undefined} target - Element, element ID, or CSS selector.
 * @returns {Element|null} The resolved element, or null if not found.
 */
function resolveElement(target) {
    if (target instanceof Element) {
        return target;
    }
    
    if (typeof target !== "string" || !target.trim()) {
        return null;
    }
    
    const value = target.trim();
    
    // ID
    const id = value.replace(/^#/, "");
    const elementById = document.getElementById(id);
    
    if (elementById) {
        return elementById;
    }
    
    // CSS selector
    try {
        return document.querySelector(value);
    } catch {
        return null;
    }
}


/**
 * Initializes a search input and handles search on Enter key
 * or by clicking the search icon inside its search group.
 *
 * @param {string|HTMLInputElement} input - Search input element or its ID.
 *
 * @param {(event: KeyboardEvent|MouseEvent, input: HTMLInputElement) => void|Promise<void>} callback
 * Callback function executed when a search is triggered.
 *
 * @returns {void}
 */
function initSearchInput(input, callback) {
    input = resolveElement(input);
    
    if (!input) return;
    
    
    const handleSearch = async (event) => {
        await callback(event, input);
    };
    
    input.addEventListener("keyup", async (event) => {
        if (event.key === "Enter") {
            await handleSearch(event);
        }
    });
    
    const searchGroup = input.closest(".search-group");
    if (!searchGroup) {
        return;
    }
    
    const searchIcon = searchGroup.querySelector(".search-icon");
    searchIcon?.addEventListener("click", async (event) => {
        await handleSearch(event);
    });
}

/**
 * Initializes change event handling for a radio button group.
 * Supports both direct event listeners and event delegation.
 *
 * @param {string} name - The name attribute of the radio button group.
 *
 * @param {(event: Event, radio: HTMLInputElement) => void|Promise<void>} callback
 * Callback function executed when a radio button changes.
 *
 * @param {boolean} [delegation=false] - Whether to use event delegation.
 *
 * @returns {void}
 */
function initChangeRadio(name, callback, delegation = false) {
    if (!name || typeof callback !== "function") {
        return;
    }
    
    if (delegation) {
        document.addEventListener("change", async (event) => {
            const radio = event.target.closest(`input[type="radio"][name="${name}"]`);
            
            if (!radio) return;
            
            await callback(event, radio);
        });
        
        return;
    }
    
    const radios = document.querySelectorAll(`input[type="radio"][name="${name}"]`);
    
    if (!radios.length) return;
    
    radios.forEach(radio => {
        radio.addEventListener("change", async (event) => {
            await callback(event, radio);
        });
    });
}


class DynamicSkeleton {
    /**
     * @param {Object} options
     * @param {string|HTMLElement} options.container - سلکتور یا المنت والد که اسکلتون ها در آن قرار میگیرند
     * @param {string} options.template - ساختار HTML یک آیتم اسکلتون
     * @param {number} options.count - تعداد اسکلتون‌هایی که باید ساخته شود (پیش‌فرض 3)
     */
    constructor(options) {
        this.container = typeof options.container === "string"
            ? document.querySelector(options.container)
            : options.container;
        this.template = options.template;
        this.count = options.count || 3;
    }
    
    // متد نمایش لودینگ
    render() {
        if (!this.container) return;
        
        let skeletonsHTML = "";
        for (let i = 0; i < this.count; i++) {
            skeletonsHTML += this.template;
        }
        
        this.container.innerHTML = skeletonsHTML;
    }
    
    // متد پاک کردن لودینگ (البته وقتی دیتای واقعی رو innerHTML کنید خود به خود پاک میشه)
    clear() {
        if (this.container) {
            this.container.innerHTML = "";
        }
    }
}

const Skeleton = (() => {
    
    const templates = {};
    
    function register(name, builder) {
        templates[name] = builder;
    }
    
    function show({
                      container,
                      count = 1,
                      template,
                  }) {
        
        const el = typeof container === "string"
            ? document.querySelector(container)
            : container;
        
        if (!el) return;
        
        el.dataset.skeleton = "true";
        
        const html = [];
        
        const builder =
            typeof template === "function"
                ? template
                : templates[template];
        
        for (let i = 0; i < count; i++) {
            
            html.push(builder(i));
            
        }
        
        el.innerHTML = html.join("");
        
    }
    
    function hide(container) {
        
        const el = typeof container === "string"
            ? document.querySelector(container)
            : container;
        
        if (el)
            el.innerHTML = "";
        
    }
    
    return {
        show,
        hide,
        register,
    };
    
})();
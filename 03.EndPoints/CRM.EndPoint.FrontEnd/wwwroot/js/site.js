/**
 * Sends HTTP requests with loading management,
 * toast notifications and callbacks.
 *
 * @param {Object} options
 * @param {string} options.url
 * @param {"GET"|"POST"|"PUT"|"DELETE"|"PATCH"} [options.method="GET"]
 * @param {Object|Array|FormData|null} [options.data=null]
 * @param {Object|null} [options.params=null]
 * @param {Object|null} [options.headers=null]
 * @param {string} [options.responseType="json"]
 * @param {Function|null} [options.onSuccess=null]
 * @param {Function|null} [options.onError=null]
 * @param {string|null} [options.successMsg]
 * @param {boolean} [options.showLoading=true]
 * @param {boolean} [options.showError=true]
 * @param {number} [options.timeout=30000]
 * @param {AbortSignal|null} [options.signal=null]
 * @param {Object|string|null} [options.skeleton=null] Local read-only content loading target.
 * @param {boolean} [options.returnFullResponse=false]
 * @param {Function|null} [options.onUploadProgress=null]
 * @param {Function|null} [options.onDownloadProgress=null]
 * @param {boolean} [options.withCredentials=false]
 * @returns {Promise<any|null>}
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
        showError = true,
        timeout = 30000,
        signal = null,
        skeleton = null,
        returnFullResponse = false,
        onUploadProgress = null,
        onDownloadProgress = null,
        withCredentials = false,
    }) {
    const originalOptions = arguments[0];
    const localLoading = skeleton && typeof PageSkeleton !== "undefined" ? PageSkeleton.begin(skeleton) : null;
    const retry = typeof skeleton?.retry === "function" ? skeleton.retry : () => sendApiRequest({ ...originalOptions, signal: null });
    const globalLoading = showLoading && !skeleton;
    method = method.toUpperCase();
    if (successMsg === undefined) successMsg = {
        GET: null,
        POST: "با موفقیت ثبت شد",
        PUT: "با موفقیت ویرایش شد",
        DELETE: "با موفقیت حذف شد",
        PATCH: "با موفقیت ویرایش شد",
    }[method] ?? null;
    const config = {
        url,
        method, 
        responseType,
        timeout, 
        withCredentials,
        signal: localLoading ? (signal ? AbortSignal.any([signal, localLoading.signal]) : localLoading.signal) : signal,
        headers: {
            "X-Requested-With": "XMLHttpRequest", 
            "X-Current-Page": window.location.pathname + window.location.search,
            ...(window.signalRConnectionId && { "X-SignalR-ConnectionId": window.signalRConnectionId }),
            ...(headers ?? {}),
        },
    };
    if (params !== null) config.params = params;
    if (data !== null) config.data = data;
    if (typeof onUploadProgress === "function") config.onUploadProgress = onUploadProgress;
    if (typeof onDownloadProgress === "function") config.onDownloadProgress = onDownloadProgress;
    const navigationPage = typeof PageRuntime !== "undefined" ? PageRuntime.current : null;
    if (navigationPage && /^(GET|HEAD)$/.test(method)) {
        config.signal = config.signal ? AbortSignal.any([config.signal, navigationPage.signal]) : navigationPage.signal;
    }
    let response = null, requestError = null;
    if (globalLoading) updateApiLoading(true);
    try {
        response = await axios(config);
    } catch (error) {
        requestError = error;
    } finally {
        if (globalLoading) updateApiLoading(false);
    }
    if (navigationPage && !navigationPage.isActive && /^(GET|HEAD)$/.test(method)) return null;
    if (localLoading && !localLoading.isCurrent()) return null;
    
    if (requestError) {
        if (axios.isCancel(requestError) || requestError.code === "ERR_CANCELED") {
            localLoading?.cancel();
            return null;
        }
        console.error("API Error:", requestError);
        const errorResponse = requestError.response;
        if (errorResponse?.status === 401 && errorResponse.headers?.["x-auth-expired"] === "true") {
            localLoading?.finish();
            window.location.href = errorResponse.headers?.["x-login-url"] || "/Account/Login";
            return null;
        }
        try {
            if (showError && !localLoading) await handleApiError(requestError);
            if (typeof onError === "function") await onError(requestError);
        } catch (callbackError) {
            console.error("API Error Callback Error:", callbackError);
        }
        localLoading?.fail(retry);
        return null;
    }
    
    try {
        if (successMsg) toastifySuccess(successMsg);
        if (typeof onSuccess === "function") await onSuccess(response);
        localLoading?.finish();
        return returnFullResponse ? response : response?.data;
    } catch (error) {
        localLoading?.fail(retry);
        throw error;
    }
}

let activeApiLoadingCount = 0;

function updateApiLoading(show) {
    if (show) {
        activeApiLoadingCount++;
    } else {
        activeApiLoadingCount = Math.max(0, activeApiLoadingCount - 1);
    }
    
    if (typeof FLshowLoading === "function") {
        FLshowLoading(activeApiLoadingCount > 0);
    }
}

async function handleApiError(err) {
    const response = err?.response;
    const status = response?.status ?? err?.status ?? null;
    
    let data = response?.data;
    const contentType = response?.headers?.["content-type"] ?? "";
    
    if (data instanceof Blob) {
        try {
            const text = await data.text();
            
            data = contentType.includes("json") ? JSON.parse(text) : text;
        } catch {
            data = null;
        }
    }
    
    let errorMessage = null;
    
    if (status >= 500 || contentType.includes("text/html")) {
        errorMessage = null;
    } else if (typeof data === "string") {
        errorMessage = data;
    } else if (data && typeof data === "object") {
        if (data.errors && typeof data.errors === "object") {
            errorMessage = Object.values(data.errors).flat().filter(Boolean).join("\n");
        }
        
        errorMessage ??= data.detail ?? data.message ?? data.title ?? null;
    }
    
    if (!errorMessage) {
        if (err?.code === "ECONNABORTED" || err?.code === "ETIMEDOUT") {
            errorMessage = "زمان انتظار پاسخ سرور به پایان رسید.";
        } else if (!response) {
            errorMessage = "ارتباط با سرور برقرار نشد.";
        } else {
            errorMessage = `خطایی در ارتباط با سرور رخ داده است (${status})`;
        }
    }
    
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
        
        this.searchInput?.classList.toggle("d-none", config.showSearch === false);
        
        if (config.clearSearch !== false && this.searchInput)
            this.searchInput.value = "";
        
        this.setModalSize(config.size);
        
        this.updateSubmitButton();
        
        await this.load();
        
        ModalToggle(this.modal);
    }
    
    /**
     * Sets the modal dialog size.
     * @param {string} [size="lg"] Modal size.
     * @returns {void}
     */
    setModalSize(size = "lg") {
        const dialog = this.modal.querySelector(".modal-dialog");
        
        dialog.classList.remove("modal-sm", "modal-lg", "modal-xl", "modal-xxl");
        dialog.classList.add(`modal-${size}`);
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
        
        this.modal.addEventListener("hidden.bs.modal", () => {
            this.setModalSize();
        });
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
        // this.submitButton.classList.toggle("d-none", this.tempSelectedIds.length === 0);
        this.submitButton.disabled = this.tempSelectedIds.length === 0;
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


function createToast(type, text, options = {}) {
    const styles = {
        success: "linear-gradient(135deg, rgba(2,81,75,1) 21%, rgba(0,59,54,1) 100%)",
        error: "linear-gradient(135deg, rgba(121,9,9,1) 21%, rgba(78,0,0,0.9) 100%)",
        info: "linear-gradient(135deg, rgba(110,73,0,1) 18%, rgba(185,124,0,1) 100%)",
    };
    
    const toast = Toastify({
        text,
        node: options.node,
        duration: options.duration ?? (type === "error" ? 5000 : 3000),
        close: options.close ?? false,
        gravity: options.gravity ?? "top",
        position: options.position ?? "center",
        stopOnFocus: options.stopOnFocus ?? false,
        escapeMarkup: options.escapeMarkup ?? true,
        style: {
            background: styles[type] ?? styles.info,
            borderRadius: "8px",
            ...(options.style || {}),
        },
    });
    
    toast.showToast();
    
    return toast;
}

function toastifySuccess(text, duration = 3000) {
    return createToast("success", text, {
        duration,
        escapeMarkup: false,
    });
}

function toastifyError(text, duration = 5000) {
    return createToast("error", text, { duration });
}

function toastifyInfo(text, duration = 3000) {
    return createToast("info", text, { duration });
}


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


const goTo = (url, blank = false) => {
    if (blank) {
        window.open(url);
    } else {
        window.location.href = url;
    }
};


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
    
    let value = toEnglishDigits(el.value)
        .replace(/[٬,]/g, "")
        .replace(/٫/g, ".")
        .trim();
    
    if (value === "" || !/^-?\d*\.?\d*$/.test(value)) return;
    
    if (value === "-" || value.endsWith(".")) {
        el.value = value;
        return;
    }
    
    let numValue = Number(value);
    
    if (!Number.isFinite(numValue)) return;
    
    const minValue = min !== null && min.trim() !== "" ? Number(min) : null;
    const maxValue = max !== null && max.trim() !== "" ? Number(max) : null;
    
    if (minValue !== null && Number.isFinite(minValue) && numValue < minValue) {
        numValue = minValue;
    }
    
    if (maxValue !== null && Number.isFinite(maxValue) && numValue > maxValue) {
        numValue = maxValue;
    }
    
    const newValue = el.classList.contains("currencyNumber")
        ? numValue.toLocaleString()
        : String(numValue);
    
    if (el.value !== newValue) {
        el.value = newValue;
    }
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
}

function getNumericValue(value) {
    if (!value) return "";
    
    return toEnglishDigits(value)
        .replace(/\D/g, "");
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

const SeparatedNumberRemove = (val) => {
    if (typeof val !== "string") return val;
    return val.replace(/[,\u066C\u060C]/g, "").trim();
};

//////////////////////////////////////////////////

const fLshowLoadingElm = document.getElementById("FLshowLoading");
function FLshowLoading(show = true) {
    fLshowLoadingElm.style.display = show ? "" : "none";
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
    static #monthNames = {
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
    
    getCurrentWeek = async () => {
        const data = await this.#fetchDate(1);
        return data.split("_");
    };
    
    getCurrentMonth = async () => {
        const data = await this.#fetchDate(2);
        const result = data.split("_");
        
        result.push(
            GetDate.#monthNames[result[0].slice(5, 7)],
            result[0].slice(0, 4),
        );
        
        return result;
    };
    
    getCurrentYear = async () => {
        const data = await this.#fetchDate(3);
        const result = data.split("_");
        
        result.push(result[0].slice(0, 4));
        
        return result;
    };
    
    getTodayDate = () => this.#fetchDate(4);
    
    getTodayDateTime = () => this.#fetchDate(5);
    
    getTomorrowDate = () => this.#fetchDate(6);
    
    #fetchDate = async (type) => {
        const response = await fetch(`/Home/GetDate?type=${type}`);
        
        if (!response.ok) {
            throw new Error(`GetDate failed: ${response.status}`);
        }
        
        return await response.text();
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
    
    convertJsonToForm(js, formId, blackListArray = [], flashFilled = false) {
        const form = document.getElementById(formId);
        if (!form) return;
        
        const flash = elm => {
            if (flashFilled) flashItem(elm, { duration: 1600 });
        };
        
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
                        flash(elm);
                        if (elm.dataset.currencyHint) {
                            updateCurrencyHint(elm);
                        }
                        break;
                    case "select-one":
                    case "select-multiple":
                        if (elm.tomselect) {
                            elm.tomselect.setValue(value ?? [], true);
                            flash(elm.tomselect.control);
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
                            flash(elm);
                        }
                        
                        break;
                    case "radio":
                        const radioBtn = form.querySelector(`input[name="${key}"][value="${value}"]`);
                        if (radioBtn) radioBtn.checked = true;
                        if (radioBtn.checked) radioBtn.dispatchEvent(new Event("change"));
                        flash(elm);
                        break;
                    case "checkbox":
                        elm.checked = Boolean(value);
                        elm.dispatchEvent(new Event("change"));
                        flash(elm);
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


const toEnglishDigits = (val) => {
    if (val === null || val === undefined) return val;
    
    return String(val)
        .replace(/[۰-۹]/g, d => "۰۱۲۳۴۵۶۷۸۹".indexOf(d))
        .replace(/[٠-٩]/g, d => "٠١٢٣٤٥٦٧٨٩".indexOf(d));
};


const toPersianDigits = (val) => {
    if (val === null || val === undefined) return val;
    
    return String(val)
        .replace(/[0-9]/g, d => "۰۱۲۳۴۵۶۷۸۹"[d])
        .replace(/[٠-٩]/g, d => "۰۱۲۳۴۵۶۷۸۹"["٠١٢٣٤٥٦٧٨٩".indexOf(d)]);
};

function normalizePersianText(text) {
    if (!text) return "";
    
    return text
        .replace(/[يى]/g, "ی")
        .replace(/ك/g, "ک");
}

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
 * @param {{duration?: number, color?: string, className?: string}} options
 * @param {number} options.duration - animation duration in ms
 * @param {string} options.color - Flash color
 * @param {string} options.className - CSS class to apply
 */
function flashItem(el, options = {}) {
    if (!el) return;
    
    const {
        duration = 1200,
        color = "#e3f2fd",
        className = "fill-flash",
    } = options;
    
    el.style.setProperty("--flash-duration", `${duration}ms`);
    el.style.setProperty("--flash-color", color);
    
    el.classList.remove(className);
    void el.offsetWidth;
    el.classList.add(className);
    
    setTimeout(() => el.classList.remove(className), duration);
}


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
            ${currency ? `<span class="fs-small">${currency}</span>` : ""}
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

/**
 * Creates a reusable inline alert message.
 * @param {Object} options
 * @param {"error"|"warning"|"success"|"info"} options.type
 * @param {string} options.message
 * @param {boolean} [options.dismissible=false]
 * @param {string} [options.icon]
 * @returns {HTMLDivElement}
 */
function createInlineAlert({ type = "info", message, dismissible = false, icon }) {
    const icons = {
        error: "fa-exclamation-circle",
        warning: "fa-exclamation-triangle",
        success: "fa-check-circle",
        info: "fa-info-circle",
    };
    
    const alert = document.createElement("div");
    alert.className = `inline-alert inline-alert--${type}`;
    
    alert.innerHTML = `
        <i class="fa ${icon || icons[type]}"></i>
        <span>${message}</span>
        ${dismissible ? `
            <button type="button" class="inline-alert__close" aria-label="بستن">
                <i class="fa fa-times"></i>
            </button>
        ` : ""}
    `;
    
    if (dismissible) {
        alert.querySelector(".inline-alert__close").addEventListener("click", () => {
            alert.remove();
        });
    }
    
    return alert;
}


const momentumScrollInstances = new WeakMap();

/** Opt-in wheel inertia for a local scroll region. Returns an idempotent cleanup. */
function enableMomentumScroll(element, { signal = window.PageRuntime?.current?.signal } = {}) {
    if (!(element instanceof HTMLElement) || signal?.aborted) return () => {};
    if (momentumScrollInstances.has(element)) return momentumScrollInstances.get(element);
    const controller = new AbortController();
    const options = { signal: controller.signal };
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    let frame = 0;
    let target = 0;
    let previousTime = 0;
    let direction = 0;
    const stop = () => {
        cancelAnimationFrame(frame);
        frame = 0;
        direction = 0;
    };
    const destroy = () => {
        if (controller.signal.aborted) return;
        stop();
        controller.abort();
        momentumScrollInstances.delete(element);
    };
    const animate = time => {
        if (!element.isConnected) {
            destroy();
            return;
        }
        if (!element.clientHeight || reducedMotion.matches || document.hidden) {
            stop();
            return;
        }
        const elapsed = Math.min(time - previousTime, 40);
        previousTime = time;
        target = Math.max(0, Math.min(target, element.scrollHeight - element.clientHeight));
        const remaining = target - element.scrollTop;
        if (Math.abs(remaining) < 1) {
            element.scrollTop = target;
            stop();
            return;
        }
        const movement = remaining * (1 - Math.exp(-elapsed / 240));
        element.scrollTop += Math.sign(remaining) * Math.max(1, Math.abs(movement));
        frame = requestAnimationFrame(animate);
    };
    element.addEventListener("wheel", event => {
        if (event.defaultPrevented || !event.cancelable || reducedMotion.matches ||
            event.ctrlKey || event.metaKey || event.shiftKey ||
            event.target.closest?.("input, textarea, select, [contenteditable='true']") ||
            Math.abs(event.deltaX) > Math.abs(event.deltaY) ||
            (event.deltaMode === 0 && Math.abs(event.deltaY) < 40)) {
            stop();
            return;
        }
        for (let child = event.target; child && child !== element; child = child.parentElement) {
            if (child instanceof HTMLElement && child.scrollHeight > child.clientHeight &&
                /auto|scroll/.test(getComputedStyle(child).overflowY)) {
                stop();
                return;
            }
        }
        const nextDirection = Math.sign(event.deltaY);
        const maxScroll = element.scrollHeight - element.clientHeight;
        if (!nextDirection || maxScroll <= 0 ||
            (nextDirection < 0 && element.scrollTop <= 0) ||
            (nextDirection > 0 && element.scrollTop >= maxScroll - 1)) {
            stop();
            return;
        }
        event.preventDefault();
        if (!frame || nextDirection !== direction) target = element.scrollTop;
        direction = nextDirection;
        const unit = event.deltaMode === 1 ? 18 : event.deltaMode === 2 ? element.clientHeight : 1;
        const distance = Math.min(Math.abs(event.deltaY * unit) * 1.4, 220) * direction;
        target = Math.max(0, Math.min(maxScroll,
            Math.max(element.scrollTop - 420, Math.min(element.scrollTop + 420, target + distance))));
        if (!frame) {
            previousTime = performance.now();
            frame = requestAnimationFrame(animate);
        }
    }, { ...options, passive: false });
    element.addEventListener("pointerdown", stop, options);
    element.addEventListener("keydown", stop, options);
    element.addEventListener("dragstart", stop, options);
    reducedMotion.addEventListener("change", stop, options);
    document.addEventListener("visibilitychange", stop, options);
    window.addEventListener("blur", stop, options);
    window.addEventListener("pagehide", destroy, { ...options, once: true });
    signal?.addEventListener("abort", destroy, { ...options, once: true });
    momentumScrollInstances.set(element, destroy);
    return destroy;
}

const momentumScrollScopes = new WeakMap();

/** Covers current and AJAX-created vertical scroll regions without scanning every mutation. */
function enableMomentumScrollScope(root, { signal = window.PageRuntime?.current?.signal } = {}) {
    if (!(root instanceof HTMLElement) || signal?.aborted) return () => {};
    if (momentumScrollScopes.has(root)) return momentumScrollScopes.get(root);
    const controller = new AbortController();
    const attached = new Map();
    const destroy = () => {
        if (controller.signal.aborted) return;
        controller.abort();
        attached.forEach(cleanup => cleanup());
        attached.clear();
        momentumScrollScopes.delete(root);
    };
    root.addEventListener("wheel", event => {
        if (event.defaultPrevented) return;
        // Discard regions replaced by an AJAX refresh before binding another one.
        attached.forEach((cleanup, element) => {
            if (!root.contains(element)) {
                cleanup();
                attached.delete(element);
            }
        });
        for (let element = event.target instanceof Element ? event.target : null;
             element && root.contains(element); element = element.parentElement) {
            if (element instanceof HTMLElement && element.scrollHeight > element.clientHeight && element.clientHeight > 0 &&
                /^(auto|scroll|overlay)$/.test(getComputedStyle(element).overflowY)) {
                if (!attached.has(element)) {
                    attached.set(element, enableMomentumScroll(element, { signal: controller.signal }));
                }
                break;
            }
            if (element === root) break;
        }
        // Register non-passively up front so the browser keeps the first wheel event cancelable.
        // A passive scope with lazily attached listeners can begin a native scroll transaction first.
    }, { capture: true, passive: false, signal: controller.signal });
    window.addEventListener("pagehide", destroy, { once: true, signal: controller.signal });
    signal?.addEventListener("abort", destroy, { once: true, signal: controller.signal });
    momentumScrollScopes.set(root, destroy);
    return destroy;
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#039;");
}

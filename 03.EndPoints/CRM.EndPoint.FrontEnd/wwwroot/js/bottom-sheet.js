/**
 * @typedef {Object} BottomSheetAction
 * @property {string} [text] - The text label for the button.
 * @property {string} [html] - HTML content to inject inside the button (takes precedence over text).
 * @property {string} [className] - CSS classes to apply to the button.
 * @property {boolean} [close=true] - If false, prevents the BottomSheet from closing automatically when the button is clicked.
 * @property {function(MouseEvent, HTMLButtonElement): void} [onClick] - Callback function triggered on button click.
 */

/**
 * @typedef {Object} BottomSheetOptions
 * @property {string|HTMLElement} [title=""] - The title text or element shown in the header.
 * @property {string|HTMLElement} [content=""] - The main content body text or element.
 * @property {boolean} [closeOnOverlay=true] - Close the sheet when clicking on the dark backdrop.
 * @property {boolean} [closeOnEsc=true] - Close the sheet when pressing the 'Escape' key.
 * @property {boolean} [showCloseButton=true] - Render the default 'X' close button in the header.
 * @property {boolean} [destroyOnClose=false] - Completely remove DOM elements and events from memory upon closing.
 * @property {string} [className=""] - Custom CSS class added to the `.bs-sheet` container.
 * @property {number[]} [snapPoints=[0.8, 1]] - Sorted array of heights (decimals between 0 and 1) representing viewport ratios.  Default: [0.8, 1]
 */

/**
 * A highly performant, production-ready BottomSheet component featuring smooth
 * momentum-based snapping, touch/mouse dragging gestures, and flexible lifecycle events.
 */
class BottomSheet {
    
    /** @type {BottomSheet|null} Reference to the currently active/open sheet instance in the DOM. */
    static activeSheet = null;
    
    /** @type {number} Duration of the opening/closing CSS transition animations in milliseconds. */
    static ANIMATION = 350;
    
    /**
     * Creates an instance of BottomSheet.
     * @param {BottomSheetOptions} [options={}] - Configuration options for the instance.
     */
    constructor(options = {}) {
        /** @type {BottomSheetOptions} */
        this.options = {
            title: "",
            content: "",
            closeOnOverlay: true,
            closeOnEsc: true,
            showCloseButton: true,
            destroyOnClose: false,
            className: "",
            snapPoints: [0.8, 1],
            ...options,
        };
        
        /** @type {Object.<string, Function[]>} Dictionary storing registered event callbacks. */
        this.events = {};
        
        /** @type {boolean} Indicates whether the sheet is currently open. */
        this.isOpen = false;
        
        this.#create();
        this.#setSnapPoints(this.options.snapPoints);
        this.#initGesture();
        this.#bindEvents();
    }
    
    /**
     * Dynamically generates the HTML template structure and appends it to the DOM body.
     * @private
     */
    #create() {
        this.root = document.createElement("div");
        this.root.className = "bs";
        
        this.root.innerHTML = `
            <div class="bs-overlay"></div>
            <div class="bs-sheet ${this.options.className}">
                <div class="bs-handle"></div>
                <div class="bs-header">
                    <div class="bs-title"></div>
                    ${this.options.showCloseButton ? `
                        <button class="bs-close" type="button">
                            <svg viewBox="0 0 24 24" fill="currentColor"><path fill-rule="evenodd" d="M5.47 5.47a.75.75 0 0 1 1.06 0L12 10.94l5.47-5.47a.75.75 0 1 1 1.06 1.06L13.06 12l5.47 5.47a.75.75 0 1 1-1.06 1.06L12 13.06l-5.47 5.47a.75.75 0 0 1-1.06-1.06L10.94 12 5.47 6.53a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd" /></svg>
                        </button>
                    ` : ""}
                </div>
                <div class="bs-content"></div>
                <div class="bs-footer"></div>
            </div>
        `;
        
        document.body.appendChild(this.root);
        
        this.overlay = this.root.querySelector(".bs-overlay");
        this.sheet = this.root.querySelector(".bs-sheet");
        this.title = this.root.querySelector(".bs-title");
        this.content = this.root.querySelector(".bs-content");
        this.footer = this.root.querySelector(".bs-footer");
        this.closeBtn = this.root.querySelector(".bs-close");
        
        this.setTitle(this.options.title);
        this.setContent(this.options.content);
    }
    
    /**
     * Dispatches a custom event, invoking all registered listener callbacks.
     * @param {string} name - The event name (`beforeOpen`, `open`, `beforeClose`, `close`, `drag`, `snap`, `contentChanged`).
     * @param {*} [data] - Contextual data payload forwarded to the listener functions.
     */
    emit(name, data) {
        const list = this.events[name];
        if (!list) return;
        for (const fn of list) {
            try {
                fn(data);
            } catch (e) {
                console.error(e);
            }
        }
    }
    
    /**
     * Opens the BottomSheet, triggers transitions, locks body scrolling, and sets the initial snap height.
     * Closes any other globally active sheet instance beforehand.
     * @fires BottomSheet#beforeOpen
     * @fires BottomSheet#open
     */
    open() {
        if (this.isOpen) return;
        
        if (BottomSheet.activeSheet && BottomSheet.activeSheet !== this) {
            BottomSheet.activeSheet.close();
        }
        
        BottomSheet.activeSheet = this;
        this.emit("beforeOpen");
        
        document.body.classList.add("bs-lock");
        this.root.style.display = "block";
        
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                this.root.classList.add("show");
                this.#animateSnap();
            });
        });
        
        this.isOpen = true;
        this.emit("open");
        window.addEventListener("resize", this.#resizeHandler);
    }
    
    /**
     * Closes the BottomSheet with a slide-down animation and unlocks body scrolling.
     * Respects `destroyOnClose` option setting.
     * @fires BottomSheet#beforeClose
     * @fires BottomSheet#close
     */
    close() {
        if (!this.isOpen || this.isClosing) return;
        this.isClosing = true;
        
        this.emit("beforeClose");
        this.root.classList.remove("show");
        
        const currentHeight = this.#getSnapHeight();
        this.sheet.style.transform = `translateY(${currentHeight}px)`;
        document.body.classList.remove("bs-lock");
        
        this.isOpen = false;
        BottomSheet.activeSheet = null;
        window.removeEventListener("resize", this.#resizeHandler);
        
        setTimeout(() => {
            this.emit("close");
            this.isClosing = false;
            if (this.options.destroyOnClose) {
                this.destroy();
            } else {
                this.root.style.display = "none";
            }
        }, BottomSheet.ANIMATION);
    }
    
    /**
     * Attaches core window, keyboard, and overlay interaction event handlers.
     * @private
     */
    #bindEvents() {
        if (this.options.showCloseButton && this.closeBtn) {
            this.closeBtn.onclick = () => this.close();
        }
        if (this.options.closeOnOverlay) {
            this.overlay.onclick = () => this.close();
        }
        this._escHandler = e => {
            if (this.options.closeOnEsc && e.key === "Escape" && this.isOpen) {
                this.close();
            }
        };
        document.addEventListener("keydown", this._escHandler);
    }
    
    /**
     * Window resizing event listener callback to guarantee proper layout alignment on viewport adjustments.
     * @private
     */
    #resizeHandler = () => {
        if (!this.isOpen) return;
        this.#animateSnap();
    };
    
    /*==================================================
        Gesture Engine
    ==================================================*/
    
    /**
     * Initializes unified drag data objects and binds pointerdown hooks to grab points.
     * @private
     */
    #initGesture() {
        this.drag = {
            active: false,
            startY: 0,
            currentY: 0,
            velocity: 0,
            lastY: 0,
            lastTime: 0,
            pointerId: null,
        };
        
        const handle = this.sheet.querySelector(".bs-handle");
        const header = this.sheet.querySelector(".bs-header");
        
        if (handle) handle.addEventListener("pointerdown", this.#pointerDown.bind(this));
        if (header) header.addEventListener("pointerdown", this.#pointerDown.bind(this));
    }
    
    /**
     * Coordinates the beginning of a pointer dragging sequence. Sets state captures.
     * @param {PointerEvent} e - The pointerdown event object.
     * @private
     */
    #pointerDown(e) {
        if (e.button !== undefined && e.button !== 0) return;
        if (this.isLocked()) return;
        
        const target = e.target;
        if (target.closest(".bs-close") || target.closest("button") || target.closest("a")) {
            return;
        }
        
        this.drag.pointerId = e.pointerId;
        this.drag.active = true;
        this.drag.startY = e.clientY;
        this.drag.lastY = e.clientY;
        this.drag.lastTime = performance.now();
        this.drag.velocity = 0;
        
        this.drag.captureTarget = e.currentTarget;
        this.drag.captureTarget.setPointerCapture(e.pointerId);
        this.sheet.classList.add("bs-dragging");
        
        this._boundPointerMove = this.#pointerMove.bind(this);
        this._boundPointerUp = this.#pointerUp.bind(this);
        
        window.addEventListener("pointermove", this._boundPointerMove);
        window.addEventListener("pointerup", this._boundPointerUp);
        window.addEventListener("pointercancel", this._boundPointerUp);
    }
    
    /**
     * Tracking cycle computing drag delta offsets, resistance factors, and tracking velocity vector paths.
     * @param {PointerEvent} e - The pointermove event object.
     * @private
     * @fires BottomSheet#drag
     */
    #pointerMove(e) {
        if (!this.drag.active || e.pointerId !== this.drag.pointerId) return;
        
        const deltaY = e.clientY - this.drag.startY;
        
        this.#updateSheetDragStyle(deltaY);
        
        const now = performance.now();
        const dt = now - this.drag.lastTime;
        if (dt > 0) {
            this.drag.velocity = (e.clientY - this.drag.lastY) / dt;
        }
        
        this.drag.lastY = e.clientY;
        this.drag.lastTime = now;
        
        this.emit("drag", { translate: deltaY, velocity: this.drag.velocity });
    }
    
    /**
     * Handles cleanup operations when drag tracking ends.
     * @param {PointerEvent} e - The pointerup or pointercancel event object.
     * @private
     */
    #pointerUp(e) {
        if (!this.drag.active || e.pointerId !== this.drag.pointerId) return;
        this.drag.active = false;
        
        try {
            this.drag.captureTarget.releasePointerCapture(this.drag.pointerId);
        } catch (err) {}
        
        window.removeEventListener("pointermove", this._boundPointerMove);
        window.removeEventListener("pointerup", this._boundPointerUp);
        window.removeEventListener("pointercancel", this._boundPointerUp);
        
        this.sheet.classList.remove("bs-dragging");
        
        const finalTranslate = e.clientY - this.drag.startY;
        this.#finishGesture(finalTranslate);
    }
    
    /**
     * Controls real-time DOM styling rendering changes (transform manipulation) during drag moves.
     * @param {number} translate - Raw vertical tracking pixel offset delta.
     * @private
     */
    #updateSheetDragStyle(translate) {
        if (translate < 0) {
            translate *= 0.2; // Rubber-banding ceiling elasticity effect
        }
        
        if (translate > 1) {
            this.sheet.classList.remove("bs-full");
        }
        
        this.sheet.style.transform = `translateY(${translate}px)`;
    }
    
    /**
     * Determines whether to close, settle or move to another snap point after a drag gesture finishes.
     * @param {number} translate - Final total accumulated drag displacement pixel offset.
     * @private
     */
    #finishGesture(translate) {
        const velocity = this.drag.velocity;
        const currentHeight = this.#getSnapHeight();
        
        if (velocity > 0.8) {
            this.close();
            return;
        }
        
        if (velocity < -0.8 && this.currentSnap < this.snapPoints.length - 1) {
            this.currentSnap++;
            this.#animateSnap();
            return;
        }
        
        const targetVisualHeight = currentHeight - translate;
        const totalHeight = window.innerHeight;
        
        if (targetVisualHeight < totalHeight * (this.snapPoints[0] / 2)) {
            this.close();
            return;
        }
        
        let nearestSnap = 0;
        let minDist = Infinity;
        
        this.snapPoints.forEach((point, idx) => {
            const snapHeight = totalHeight * point;
            const dist = Math.abs(snapHeight - targetVisualHeight);
            if (dist < minDist) {
                minDist = dist;
                nearestSnap = idx;
            }
        });
        
        this.currentSnap = nearestSnap;
        this.#animateSnap();
    }
    
    /*==================================================
        Snap Points Engine
    ==================================================*/
    
    /**
     * Cleans up, filters, and saves valid viewport snap ratio coordinates.
     * @param {number[]} points - Array containing height percentages (0 to 1).
     * @private
     */
    #setSnapPoints(points) {
        this.snapPoints = [...points].filter(v => v > 0 && v <= 1).sort((a, b) => a - b);
        if (!this.snapPoints.length) this.snapPoints = [1];
        this.currentSnap = 0;
    }
    
    /**
     * Converts a ratio into an exact pixel height based on current viewport bounds.
     * @param {number} [index=this.currentSnap] - Target index mapping within snap points array configuration.
     * @returns {number} Derived layout height scale in absolute pixels.
     * @private
     */
    #getSnapHeight(index = this.currentSnap) {
        return Math.min(window.innerHeight, window.innerHeight * this.snapPoints[index]);
    }
    
    /**
     * Toggles CSS layout fullscreen properties if the height covers 100% of the screen.
     * @private
     */
    #checkFullScreenCondition() {
        const currentTargetPoint = this.snapPoints[this.currentSnap];
        const isViewportFullScreen = currentTargetPoint === 1;
        this.sheet.classList.toggle("bs-full", isViewportFullScreen);
    }
    
    /**
     * Updates height properties and resets positional rendering coordinates smoothly.
     * @private
     * @fires BottomSheet#snap
     */
    #animateSnap() {
        const height = this.#getSnapHeight();
        
        this.#checkFullScreenCondition();
        
        this.sheet.style.height = `${height}px`;
        this.sheet.style.transform = "translateY(0)";
        
        this.emit("snap", { index: this.currentSnap, height });
    }
    
    /**
     * Programmatically forces the component to snap to a specific target snap points array index.
     * @param {number} index - Targeted index tracking inside snap configuration bounds.
     */
    snap(index) {
        if (index < 0) index = 0;
        if (index >= this.snapPoints.length) index = this.snapPoints.length - 1;
        this.currentSnap = index;
        this.#animateSnap();
    }
    
    /** Programmatically shifts layout heights upward to the largest defined snap coordinate option. */
    expand() {
        this.snap(this.snapPoints.length - 1);
    }
    
    /** Programmatically minimizes heights down to the smallest baseline snap coordinate level. */
    collapse() {
        this.snap(0);
    }
    
    /** Toggles sheet positions dynamically between maximum extension bounds and baseline targets. */
    toggle() {
        if (this.currentSnap === this.snapPoints.length - 1) this.collapse();
        else this.expand();
    }
    
    /**
     * Replaces content injected within the scrolling workspace wrapper body.
     * @param {string|HTMLElement} content - Plain string text, target HTML raw layout string, or an HTMLElement node.
     * @returns {BottomSheet} The instance reference for fluent method chaining pattern.
     * @fires BottomSheet#contentChanged
     */
    setContent(content) {
        this.content.innerHTML = "";
        if (content instanceof HTMLElement) this.content.appendChild(content);
        else this.content.innerHTML = content ?? "";
        this.emit("contentChanged");
        return this;
    }
    
    /**
     * Modifies text contents displaying inside header title labels.
     * @param {string} title - Target raw title text string.
     * @returns {BottomSheet} The instance reference for fluent method chaining pattern.
     */
    setTitle(title) {
        this.title.innerHTML = title;
        return this;
    }
    
    /**
     * Populates primary footer CTA workspace slots with configurable interactive elements.
     * @param {BottomSheetAction[]} [actions=[]] - Configurable settings arrays building context action paths.
     * @returns {BottomSheet} The instance reference for fluent method chaining pattern.
     */
    setActions(actions = []) {
        this.footer.innerHTML = "";
        actions.forEach(action => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = action.className || "";
            button.innerHTML = action.html || action.text || "";
            
            button.addEventListener("click", e => {
                if (action.close !== false) this.close();
                action.onClick?.call(this, e, button);
            });
            this.footer.appendChild(button);
        });
        return this;
    }
    
    /**
     * Registers a custom callback event listener hook.
     * @param {string} event - Target lifecycle event moniker.
     * @param {Function} callback - Execution callback fired on matching event broadcasts.
     * @returns {BottomSheet} The instance reference for fluent method chaining pattern.
     */
    on(event, callback) {
        this.events[event] ??= [];
        this.events[event].push(callback);
        return this;
    }
    
    /**
     * Unregisters or filters out a registered lifecycle event listener callback.
     * @param {string} event - Target lifecycle event name.
     * @param {Function} callback - The specific function reference to remove.
     * @returns {BottomSheet} The instance reference for fluent method chaining pattern.
     */
    off(event, callback) {
        if (!this.events[event]) return this;
        this.events[event] = this.events[event].filter(x => x !== callback);
        return this;
    }
    
    /** Completely locks out and suspends dynamic dragging gesture interactions. */
    lock() {
        this.drag.active = false;
        this.sheet.classList.add("bs-locked");
    }
    
    /** Restores dragging interaction tracking capabilities. */
    unlock() {
        this.sheet.classList.remove("bs-locked");
    }
    
    /**
     * Inspects active interaction statuses.
     * @returns {boolean} True if drawing interactions are suspended.
     */
    isLocked() {
        return this.sheet.classList.contains("bs-locked");
    }
    
    /**
     * Destroys active instances, clears out listeners globally, and tears nodes out of document structures.
     */
    destroy() {
        window.removeEventListener("resize", this.#resizeHandler);
        document.removeEventListener("keydown", this._escHandler);
        window.removeEventListener("pointermove", this._boundPointerMove);
        window.removeEventListener("pointerup", this._boundPointerUp);
        window.removeEventListener("pointercancel", this._boundPointerUp);
        
        if (BottomSheet.activeSheet === this) {
            BottomSheet.activeSheet = null;
        }
        this.root.remove();
        this.events = {};
    }
}
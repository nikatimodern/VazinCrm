/**
 * @typedef {Object} BottomSheetAction
 * @property {string} [text] - The text label for the button.
 * @property {string} [html] - HTML content to inject inside the button (takes precedence over text).
 * @property {string} [className] - CSS classes to apply to the button.
 * @property {boolean} [close=true] - If false, prevents the BottomSheet from closing automatically when the button is clicked.
 * @property {function(BottomSheet, MouseEvent, HTMLButtonElement): void} [onClick] - Callback function triggered on button click.
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
 * @property {number[]} [snapPoints=[0.8, 1]] - Sorted array of heights (decimals between 0 and 1) representing viewport ratios.
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
        
        this.events = {};
        this.isOpen = false;
        
        this.#create();
        this.#setSnapPoints(this.options.snapPoints);
        this.#initGesture();
        this.#bindEvents();
    }
    
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
    
    close() {
        if (!this.isOpen || this.isClosing) return;
        this.isClosing = true;
        
        this.emit("beforeClose");
        this.root.classList.remove("show");
        
        // هدایت مستقیم به پایین صفحه بر اساس محاسبات پیکسل
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
    
    #bindEvents() {
        if (this.options.showCloseButton) {
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
    
    #resizeHandler = () => {
        if (!this.isOpen) return;
        this.#animateSnap();
    };
    
    /*==================================================
        Gesture Engine
    ==================================================*/
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
        
        this.emit("drag", { translate, velocity: this.drag.velocity });
    }
    
    #pointerUp(e) {
        if (!this.drag.active || e.pointerId !== this.drag.pointerId) return;
        this.drag.active = false;
        
        try {
            this.drag.captureTarget.releasePointerCapture(this.drag.pointerId);
        } catch (err) {
        }
        
        window.removeEventListener("pointermove", this._boundPointerMove);
        window.removeEventListener("pointerup", this._boundPointerUp);
        window.removeEventListener("pointercancel", this._boundPointerUp);
        
        this.sheet.classList.remove("bs-dragging");
        
        // محاسبه جابه‌جایی نهایی بر اساس تفاوت مختصات کنونی و اولیه
        const finalTranslate = e.clientY - this.drag.startY;
        this.#finishGesture(finalTranslate);
    }
    
    #updateSheetDragStyle(translate) {
        if (translate < 0) {
            translate *= 0.2; // افکت کشسانی سقف (Rubber-banding)
        }
        
        // بهبود تجربه کاربری: اگر کاربر حتی ۱ پیکسل رو به پایین بکشد، بورد ردیوس فوراً برمی‌گردد
        if (translate > 1) {
            this.sheet.classList.remove("bs-full");
        }
        
        this.sheet.style.transform = `translateY(${translate}px)`;
    }
    
    #finishGesture(translate) {
        const velocity = this.drag.velocity;
        const currentHeight = this.#getSnapHeight();
        
        // ۱. اگر سرعت حرکت رو به پایین زیاد بود -> بستن فوری
        if (velocity > 0.8) {
            this.close();
            return;
        }
        
        // ۲. اگر سرعت حرکت رو به بالا زیاد بود -> جهش به اسنپ‌پوینت بالاتر
        if (velocity < -0.8 && this.currentSnap < this.snapPoints.length - 1) {
            this.currentSnap++;
            this.#animateSnap();
            return;
        }
        
        // ۳. تصمیم‌گیری بر اساس موقعیت (آیا از مرز اسنپ‌پوینت‌ها رد شده‌ایم؟)
        const targetVisualHeight = currentHeight - translate;
        const totalHeight = window.innerHeight;
        
        // اگر کاربر کاملاً کشیده پایین، بجای قفل شدن روی اسنپ‌پوینت صفر، بسته شود
        if (targetVisualHeight < totalHeight * (this.snapPoints[0] / 2)) {
            this.close();
            return;
        }
        
        // پیدا کردن نزدیک‌ترین اسنپ پوینت به موقعیت فعلی دست کاربر
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
    #setSnapPoints(points) {
        this.snapPoints = [...points].filter(v => v > 0 && v <= 1).sort((a, b) => a - b);
        if (!this.snapPoints.length) this.snapPoints = [1];
        this.currentSnap = 0;
    }
    
    #getSnapHeight(index = this.currentSnap) {
        return Math.min(window.innerHeight, window.innerHeight * this.snapPoints[index]);
    }
    
    #checkFullScreenCondition() {
        const currentTargetPoint = this.snapPoints[this.currentSnap];
        const isViewportFullScreen = currentTargetPoint === 1;
        this.sheet.classList.toggle("bs-full", isViewportFullScreen);
    }
    
    #animateSnap() {
        const height = this.#getSnapHeight();
        
        this.#checkFullScreenCondition();
        
        // اعمال هم‌زمان تغییر ارتفاع و ریست کردن ترنسلیت
        this.sheet.style.height = `${height}px`;
        this.sheet.style.transform = "translateY(0)";
        
        this.emit("snap", { index: this.currentSnap, height });
    }
    
    snap(index) {
        if (index < 0) index = 0;
        if (index >= this.snapPoints.length) index = this.snapPoints.length - 1;
        this.currentSnap = index;
        this.#animateSnap();
    }
    
    expand() {
        this.snap(this.snapPoints.length - 1);
    }
    
    collapse() {
        this.snap(0);
    }
    
    toggle() {
        if (this.currentSnap === this.snapPoints.length - 1) this.collapse();
        else this.expand();
    }
    
    setContent(content) {
        this.content.innerHTML = "";
        if (content instanceof HTMLElement) this.content.appendChild(content);
        else this.content.innerHTML = content ?? "";
        this.emit("contentChanged");
        return this;
    }
    
    setTitle(title) {
        this.title.innerHTML = title;
        return this;
    }
    
    setActions(actions = []) {
        this.footer.innerHTML = "";
        actions.forEach(action => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = action.className || "";
            button.innerHTML = action.html || action.text;
            button.addEventListener("click", e => {
                if (action.close !== false) this.close();
                action.onClick?.call(this, e, button);
            });
            this.footer.appendChild(button);
        });
        return this;
    }
    
    on(event, callback) {
        this.events[event] ??= [];
        this.events[event].push(callback);
        return this;
    }
    
    off(event, callback) {
        if (!this.events[event]) return this;
        this.events[event] = this.events[event].filter(x => x !== callback);
        return this;
    }
    
    lock() {
        this.drag.active = false;
        this.sheet.classList.add("bs-locked");
    }
    
    unlock() {
        this.sheet.classList.remove("bs-locked");
    }
    
    isLocked() {
        return this.sheet.classList.contains("bs-locked");
    }
    
    destroy() {
        window.removeEventListener("resize", this.#resizeHandler);
        document.removeEventListener("keydown", this._escHandler);
        if (BottomSheet.activeSheet === this) BottomSheet.activeSheet = null;
        this.root.remove();
        this.events = {};
    }
}
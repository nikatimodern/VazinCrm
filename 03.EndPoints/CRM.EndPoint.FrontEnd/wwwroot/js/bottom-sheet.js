class BottomSheet {
    
    static activeSheet = null;
    
    constructor(options = {}) {
        this.options = {
            title: "",
            content: "",
            closeOnOverlay: true,
            closeOnEsc: true,
            destroyOnClose: false,
            className: "",
            snapPoints: [0.5, 0.85], // نقاط توقف (مثلاً ۵۰٪ و ۸۵٪ ارتفاع صفحه)
            ...options,
        };
        
        this.events = {};
        this.isOpen = false;
        
        this._create();
        this._setSnapPoints(this.options.snapPoints);
        this._initGesture();
        this._initScrollController();
        this._bindEvents();
    }
    
    /*==============================
        Create
    ==============================*/
    _create() {
        this.root = document.createElement("div");
        this.root.className = "bs";
        
        this.root.innerHTML = `
            <div class="bs-overlay"></div>
            <div class="bs-sheet ${this.options.className}">
                <div class="bs-handle"></div>
                <div class="bs-header">
                    <div class="bs-title"></div>
                    <button class="bs-close" type="button">
                        <svg viewBox="0 0 24 24" fill="currentColor"><path fill-rule="evenodd" d="M5.47 5.47a.75.75 0 0 1 1.06 0L12 10.94l5.47-5.47a.75.75 0 1 1 1.06 1.06L13.06 12l5.47 5.47a.75.75 0 1 1-1.06 1.06L12 13.06l-5.47 5.47a.75.75 0 0 1-1.06-1.06L10.94 12 5.47 6.53a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd" /></svg>
                    </button>
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
    
    /*==============================
        Events
    ==============================*/
    emit(name, data = null) {
        if (!this.events[name]) return;
        this.events[name].forEach(fn => fn(data));
    }
    
    /*==============================
        Public Methods
    ==============================*/
    open() {
        if (BottomSheet.activeSheet && BottomSheet.activeSheet !== this) {
            BottomSheet.activeSheet.close();
        }
        
        BottomSheet.activeSheet = this;
        this.emit("beforeOpen");
        
        document.body.classList.add("bs-lock");
        this.root.style.display = "block"; // اطمینان از دیده شدن دیاموند
        
        requestAnimationFrame(() => {
            this.root.classList.add("show");
            this._animateSnap(); // اعمال ارتفاع اولین Snap Point
        });
        
        this.isOpen = true;
        this.emit("open");
        
        window.addEventListener("resize", this._resizeHandler);
    }
    
    close() {
        if (!this.isOpen) return;
        
        this.emit("beforeClose");
        
        this.root.classList.remove("show");
        this.sheet.style.transform = "translateY(100%)"; // هدایت به پایین صفحه
        document.body.classList.remove("bs-lock");
        
        this.isOpen = false;
        BottomSheet.activeSheet = null;
        
        window.removeEventListener("resize", this._resizeHandler);
        
        setTimeout(() => {
            this.emit("close");
            if (this.options.destroyOnClose) {
                this.destroy();
            } else {
                this.root.style.display = "none";
            }
        }, 300);
    }
    
    /*==============================
        Private & Bindings
    ==============================*/
    _bindEvents() {
        this.closeBtn.onclick = () => this.close();
        
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
    
    _resizeHandler = () => {
        if (!this.isOpen) return;
        this._animateSnap();
    };
    
    /*==================================================
        Gesture & Dragging
    ==================================================*/
    /*==================================================
        Gesture & Dragging (اصلاح شده)
    ==================================================*/
    _initGesture() {
        this.drag = {
            active: false,
            draggingSheet: false,
            startY: 0,
            currentY: 0,
            startTranslate: 0,
            translate: 0,
            velocity: 0,
            lastY: 0,
            lastTime: 0,
            pointerId: null,
        };
        
        const handle = this.sheet.querySelector(".bs-handle");
        const header = this.sheet.querySelector(".bs-header");
        
        if (handle) handle.addEventListener("pointerdown", this._pointerDown.bind(this));
        if (header) header.addEventListener("pointerdown", this._pointerDown.bind(this));
    }
    _pointerDown(e) {
        if (e.button !== undefined && e.button !== 0) return;
        if (this.isLocked()) return;
        
        const target = e.target;
        if (target.closest(".bs-close") || target.closest("button") || target.closest("a")) {
            return;
        }
        
        this.drag.pointerId = e.pointerId;
        this.drag.active = true;
        this.drag.draggingSheet = false;
        this.drag.startY = e.clientY;
        this.drag.currentY = e.clientY;
        this.drag.lastY = e.clientY;
        this.drag.lastTime = performance.now();
        this.drag.velocity = 0;
        this.drag.startTranslate = this.drag.translate || 0;
        
        this.sheet.setPointerCapture(e.pointerId);
        this.sheet.classList.add("bs-dragging");
        
        this._boundPointerMove = this._pointerMove.bind(this);
        this._boundPointerUp = this._pointerUp.bind(this);
        
        window.addEventListener("pointermove", this._boundPointerMove);
        window.addEventListener("pointerup", this._boundPointerUp);
        window.addEventListener("pointercancel", this._boundPointerUp);
    }
    
    _pointerMove(e) {
        if (!this.drag.active) return;
        
        const delta = e.clientY - this.drag.startY;
        
        if (!this.drag.draggingSheet) {
            if (!this._canStartDrag(delta)) return;
            this.drag.draggingSheet = true;
        }
        
        let translate = this.drag.startTranslate + delta;
        
        // حالت کشسانی به سمت بالا (مقاومت تاچ)
        if (translate < 0) {
            translate *= 0.25;
        }
        
        this.drag.translate = translate;
        this.sheet.style.transition = "none";
        this.sheet.style.transform = `translateY(${translate}px)`;
        
        const now = performance.now();
        const dt = now - this.drag.lastTime;
        if (dt > 0) {
            this.drag.velocity = (e.clientY - this.drag.lastY) / dt;
        }
        
        this.drag.lastY = e.clientY;
        this.drag.lastTime = now;
        
        this.emit("drag", { translate, velocity: this.drag.velocity });
    }
    
    _pointerUp() {
        if (!this.drag.active) return;
        
        this.drag.active = false;
        this.drag.draggingSheet = false;
        
        try {
            this.sheet.releasePointerCapture(this.drag.pointerId);
        } catch (err) {
        }
        
        window.removeEventListener("pointermove", this._boundPointerMove);
        window.removeEventListener("pointerup", this._boundPointerUp);
        window.removeEventListener("pointercancel", this._boundPointerUp);
        
        this.sheet.classList.remove("bs-dragging");
        this.sheet.style.transition = "";
        
        this._finishGesture();
    }
    _finishGesture() {
        const velocity = this.drag.velocity;
        const translate = this.drag.translate;
        const height = this.sheet.offsetHeight;
        
        // اگر با سرعت به سمت پایین کشید -> بسته شود
        if (velocity > 0.5) {
            this.close();
            return;
        }
        
        // اگر با سرعت به سمت بالا پرتاب کرد (Flick کرد) -> رفتن به حداکثر ارتفاع
        if (velocity < -0.5) {
            this.expand();
            return;
        }
        
        // اگر بیش از 45 درصد ارتفاع کل شیت به پایین کشیده شد -> بسته شود
        if (translate > height * 0.45) {
            this.close();
            return;
        }
        
        // در غیر این صورت، بر اساس متد حساسیت جدید، نزدیک‌ترین اسنپ را پیدا کن
        this.currentSnap = this._findNearestSnap(translate);
        this._animateSnap();
    }/*
    _finishGesture() {
        const velocity = this.drag.velocity;
        const translate = this.drag.translate;
        const height = this.sheet.offsetHeight;
        
        // اگر با سرعت زیاد به پایین کشیده شد -> بسته شود
        if (velocity > 0.8) {
            this.close();
            return;
        }
        
        // اگر بیش از ۴۵ درصد کل ارتفاع به پایین کشیده شد -> بسته شود
        if (translate > height * 0.45) {
            this.close();
            return;
        }
        
        this.currentSnap = this._findNearestSnap(translate);
        this._animateSnap();
    }
    */
    /*==================================================
        Snap Points Engine
    ==================================================*/
    _setSnapPoints(points = [0.5, 1]) {
        this.snapPoints = [...points].filter(v => v > 0 && v <= 1).sort((a, b) => a - b);
        if (!this.snapPoints.length) this.snapPoints = [1];
        this.currentSnap = 0; // شروع از کمترین ارتفاع هندل شده
    }
    
    _getSnapHeight(index = this.currentSnap) {
        return window.innerHeight * this.snapPoints[index];
    }
    _findNearestSnap(translate) {
        const vh = window.innerHeight;
        
        const startHeight = this._getSnapHeight(this.currentSnap);
        const currentHeight = startHeight - translate;
        
        let nearest = this.currentSnap;
        
        const movementRatio = translate / vh;
        const threshold = 0.05;
        
        if (movementRatio < -threshold) {
            if (this.currentSnap < this.snapPoints.length - 1) {
                nearest = this.currentSnap + 1;
            }
        } else if (movementRatio > threshold) {
            if (this.currentSnap > 0) {
                nearest = this.currentSnap - 0;
            }
        }
        
        return nearest;
    }
    /*_findNearestSnap(translate) {
        const vh = window.innerHeight;
        const currentHeight = this._getSnapHeight(this.currentSnap) - translate;
        let nearest = 0;
        let distance = Infinity;
        
        this.snapPoints.forEach((p, i) => {
            const h = vh * p;
            const d = Math.abs(h - currentHeight);
            if (d < distance) {
                distance = d;
                nearest = i;
            }
        });
        return nearest;
    }
    */
    _animateSnap() {
        const height = this._getSnapHeight();
        this.sheet.style.transition = "height .3s cubic-bezier(.25, 1, .5, 1), transform .3s cubic-bezier(.25, 1, .5, 1)";
        this.sheet.style.height = `${height}px`;
        this.sheet.style.transform = "translateY(0)";
        this.drag.translate = 0;
        
        this.emit("snap", { index: this.currentSnap, height });
    }
    
    snap(index) {
        if (index < 0) index = 0;
        if (index >= this.snapPoints.length) index = this.snapPoints.length - 1;
        this.currentSnap = index;
        this._animateSnap();
    }
    
    expand() {
        this.snap(this.snapPoints.length - 1);
    }
    
    collapse() {
        this.snap(0);
    }
    
    toggle() {
        if (this.currentSnap === this.snapPoints.length - 1) this.collapse(); else this.expand();
    }
    
    /*==================================================
        Nested Scroll Engine
    ==================================================*/
    _initScrollController() {
        this.scrollContainer = this.content;
    }
    
    /*_canStartDrag(deltaY) {
        if (!this.scrollContainer) return true;
        const scrollTop = this.scrollContainer.scrollTop;
        const maxScroll = this.scrollContainer.scrollHeight - this.scrollContainer.clientHeight;
        
        if (deltaY > 0) return scrollTop <= 0; // کشیدن به پایین فقط در صورت بالای اسکرول بودن
        if (deltaY < 0) return scrollTop >= maxScroll;
        return true;
    }*/
    _canStartDrag(deltaY) {
        if (!this.scrollContainer) return true;
        
        const scrollTop = this.scrollContainer.scrollTop;
        
        // اگر کاربر در حال اسکرول به سمت پایین (مشاهده بندهای بعدی) است، شیت نباید درگ شود
        if (deltaY > 0) {
            return scrollTop <= 0;
        }
        
        // اگر کاربر به سمت بالا اسکرول می‌کند، فقط زمانی درگ فعال شود که اسکرول باکس کاملاً در بالا (0) باشد
        if (deltaY < 0) {
            return scrollTop <= 0;
        }
        
        return true;
    }
    
    /*==================================================
        Content & Helpers
    ==================================================*/
    setContent(content) {
        this.content.innerHTML = "";
        if (content instanceof HTMLElement) this.content.appendChild(content);
        else this.content.innerHTML = content;
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
            button.className = "bs-btn " + (action.className || "");
            if (action.primary) button.classList.add("bs-btn-primary");
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
        this.sheet.classList.add("bs-locked");
    }
    
    unlock() {
        this.sheet.classList.remove("bs-locked");
    }
    
    isLocked() {
        return this.sheet.classList.contains("bs-locked");
    }
    
    destroy() {
        window.removeEventListener("resize", this._resizeHandler);
        document.removeEventListener("keydown", this._escHandler);
        this.root.remove();
        this.events = {};
    }
}
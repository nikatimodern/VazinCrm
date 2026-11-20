(() => {
    const prototype = window.TomSelect?.prototype;
    if (!prototype) return;

    const nativePositionDropdown = prototype.positionDropdown;
    const sessions = new WeakMap();
    const clips = /^(auto|scroll|overlay|hidden|clip)$/;
    const gap = 4;

    function clippingAncestors(control) {
        const ancestors = [];
        for (let parent = control.parentElement; parent && parent !== document.body; parent = parent.parentElement) {
            const style = getComputedStyle(parent);
            const x = clips.test(style.overflowX);
            const y = clips.test(style.overflowY);
            if (x || y) ancestors.push({ element: parent, x, y });
        }
        return ancestors;
    }

    function openOverlay(tom, ancestors) {
        const { control, dropdown, dropdown_content: content } = tom;
        const viewport = window.visualViewport;
        const modal = control.closest(".modal");
        const listeners = new AbortController();
        const options = { passive: true, signal: listeners.signal };
        const rtl = getComputedStyle(control).direction === "rtl";
        const configuredMaxHeight = parseFloat(getComputedStyle(content).maxHeight);
        const maxListHeight = Number.isFinite(configuredMaxHeight) ? configuredMaxHeight : Infinity;
        const originalMaxHeight = content.style.maxHeight;
        const originalPosition = {
            left: dropdown.style.left,
            top: dropdown.style.top,
            width: dropdown.style.width,
            direction: dropdown.style.direction,
        };
        const originalParent = dropdown.parentNode;
        const originalSibling = dropdown.nextSibling;
        const usePopover = typeof dropdown.showPopover === "function";
        let frame = 0;

        dropdown.classList.add("crm-tom-select-overlay");
        dropdown.style.direction = rtl ? "rtl" : "ltr";
        if (usePopover) {
            // The top layer escapes clipping without changing the dropdown's DOM ancestry.
            dropdown.setAttribute("popover", "manual");
            dropdown.showPopover();
        } else {
            (modal ?? document.body).append(dropdown);
        }

        function position(initial = false) {
            const anchor = control.getBoundingClientRect();
            const left = viewport?.offsetLeft ?? 0;
            const top = viewport?.offsetTop ?? 0;
            const right = left + (viewport?.width ?? document.documentElement.clientWidth);
            const bottom = top + (viewport?.height ?? document.documentElement.clientHeight);
            let visibleLeft = Math.max(left, anchor.left);
            let visibleRight = Math.min(right, anchor.right);
            let visibleTop = Math.max(top, anchor.top);
            let visibleBottom = Math.min(bottom, anchor.bottom);

            for (const { element, x, y } of ancestors) {
                const bounds = element.getBoundingClientRect();
                if (x) {
                    visibleLeft = Math.max(visibleLeft, bounds.left);
                    visibleRight = Math.min(visibleRight, bounds.right);
                }
                if (y) {
                    visibleTop = Math.max(visibleTop, bounds.top);
                    visibleBottom = Math.min(visibleBottom, bounds.bottom);
                }
            }

            if (!control.isConnected || !dropdown.isConnected ||
                visibleLeft >= visibleRight || visibleTop >= visibleBottom) {
                // Tom Select is still completing open() on the first positioning call.
                if (initial) schedule();
                else tom.close();
                return;
            }

            const width = Math.min(anchor.width, Math.max(0, right - left - 2 * gap));
            const alignedLeft = rtl ? anchor.right - width : anchor.left;
            dropdown.style.width = `${width}px`;
            dropdown.style.left = `${Math.max(left + gap, Math.min(alignedLeft, right - gap - width))}px`;

            const heightOutsideList = dropdown.offsetHeight - content.offsetHeight;
            const desiredHeight = Math.min(content.scrollHeight, maxListHeight) + heightOutsideList;
            const above = anchor.top - top - 2 * gap;
            const below = bottom - anchor.bottom - 2 * gap;
            const opensAbove = below < desiredHeight && above > below;
            dropdown.classList.toggle("crm-tom-select-overlay-above", opensAbove);
            dropdown.style.top = `${opensAbove ? anchor.top - gap : anchor.bottom + gap}px`;
            const availableListHeight = (opensAbove ? above : below) - heightOutsideList;
            content.style.maxHeight = `${Math.max(0, Math.min(maxListHeight, availableListHeight))}px`;
        }

        function schedule() {
            if (frame) return;
            frame = requestAnimationFrame(() => {
                frame = 0;
                position();
            });
        }

        window.addEventListener("scroll", event => {
            if (event.target === document || event.target.contains?.(control)) schedule();
        }, { ...options, capture: true });
        viewport?.addEventListener("resize", schedule, options);
        viewport?.addEventListener("scroll", schedule, options);
        const closeForOtherModal = event => {
            if (event.target !== modal) tom.close();
        };
        document.addEventListener("show.bs.modal", closeForOtherModal, options);
        // Bootstrap can refocus the previous modal's control while activating the new focus trap.
        document.addEventListener("shown.bs.modal", closeForOtherModal, options);
        modal?.addEventListener("hide.bs.modal", event => {
            if (event.target === modal) tom.close();
        }, options);

        const resizeObserver = new ResizeObserver(schedule);
        resizeObserver.observe(control);
        resizeObserver.observe(content);
        position(true);

        return {
            schedule,
            close() {
                listeners.abort();
                cancelAnimationFrame(frame);
                resizeObserver.disconnect();
                if (usePopover) {
                    if (dropdown.matches(":popover-open")) dropdown.hidePopover();
                    dropdown.removeAttribute("popover");
                } else {
                    originalParent.insertBefore(dropdown, originalSibling?.parentNode === originalParent ? originalSibling : null);
                }
                dropdown.classList.remove("crm-tom-select-overlay", "crm-tom-select-overlay-above");
                Object.assign(dropdown.style, originalPosition);
                content.style.maxHeight = originalMaxHeight;
            },
        };
    }

    prototype.positionDropdown = function () {
        if (!this.isOpen || !this.control.isConnected || !this.dropdown.isConnected) {
            return nativePositionDropdown.call(this);
        }

        let session = sessions.get(this);
        if (session?.overlay) {
            session.overlay.schedule();
            return;
        }

        const ancestors = clippingAncestors(this.control);
        if (!ancestors.length || this.dropdown.hasAttribute("popover")) return nativePositionDropdown.call(this);

        if (!session) {
            session = { overlay: null };
            sessions.set(this, session);
            const close = () => {
                session.overlay?.close();
                session.overlay = null;
            };
            this.on("dropdown_close", close);
            this.on("destroy", close);
        }
        session.overlay = openOverlay(this, ancestors);
    };
})();

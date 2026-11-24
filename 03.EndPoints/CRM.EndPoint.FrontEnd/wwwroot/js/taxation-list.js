(() => {
    "use strict";

    const root = document.getElementById("taxationInvoices");
    if (!root) return;

    const lifetime = new AbortController();
    const page = typeof PageRuntime !== "undefined" ? PageRuntime.current : null;
    const scroll = root.querySelector("[data-taxation-scroll]");
    const releaseScroll = scroll && typeof enableMomentumScrollScope === "function"
        ? enableMomentumScrollScope(scroll) : null;
    const dispose = () => {
        lifetime.abort();
        releaseScroll?.();
    };
    if (page) page.cleanups.push(dispose);
    else window.addEventListener("pagehide", dispose, { once: true });

    root.addEventListener("click", async event => {
        const button = event.target.closest("button[data-preview-url]");
        if (!button || button.disabled) return;
        const result = button.closest("[data-taxation-validation]").querySelector("[data-validation-result]");
        button.disabled = true;
        result.textContent = "در حال بررسی…";
        try {
            const preview = await sendApiRequest({
                url: button.dataset.previewUrl,
                showLoading: false,
                showError: false,
                signal: lifetime.signal
            });
            if (lifetime.signal.aborted) return;
            if (!preview || !Array.isArray(preview.errors)) {
                result.textContent = "اعتبارسنجی انجام نشد. دوباره تلاش کنید.";
                return;
            }
            if (!preview.errors.length) {
                result.textContent = "پیش‌نمایش معتبر است.";
                return;
            }
            const messages = document.createElement("ul");
            for (const error of preview.errors) {
                const item = document.createElement("li");
                item.textContent = error;
                messages.append(item);
            }
            result.replaceChildren(messages);
        } finally {
            button.disabled = false;
        }
    }, { signal: lifetime.signal });
})();

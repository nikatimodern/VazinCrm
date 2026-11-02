(() => {
    "use strict";
    // Keep document dialogs isolated from the calling page's forms and globals.
    document.addEventListener("click", event => {
        const link = event.target.closest("a[data-deal-details]");
        if (!link || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button !== 0) return;
        const url = new URL(link.href, location.href);
        if (url.origin !== location.origin) return;
        event.preventDefault();
        const modal = document.getElementById("sharedDealDetailsModal");
        const frame = modal.querySelector("iframe");
        const feedback = modal.querySelector("[data-deal-frame-feedback]");
        frame.hidden = true;
        feedback.hidden = false;
        frame.onload = () => {
            feedback.hidden = true;
            frame.hidden = false;
        };
        url.searchParams.set("embedded", "true");
        frame.src = url.href;
        const instance = bootstrap.Modal.getOrCreateInstance(modal);
        modal.addEventListener("hidden.bs.modal", () => {
            frame.onload = null;
            frame.removeAttribute("src");
            frame.hidden = true;
            link.focus();
        }, { once: true });
        instance.show();
    });
})();

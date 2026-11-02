(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const page = document.getElementById("dealDetailsPage");
        if (!page) return;
        const feedback = page.querySelector("#dealDetailsFeedback");
        const content = page.querySelector("#dealDetailsContent");
        const retry = page.querySelector("#dealDetailsRetry");
        const signal = typeof PageRuntime !== "undefined" ? PageRuntime.current?.signal : null;
        const number = new Intl.NumberFormat("fa-IR");
        let request;
        let deal;
        let opening = false;
        const active = () => page.isConnected && !signal?.aborted;

        async function load() {
            request?.abort();
            const controller = request = new AbortController();
            retry.hidden = true;
            content.hidden = true;
            deal = null;
            feedback.textContent = "در حال دریافت اطلاعات سفارش…";
            page.setAttribute("aria-busy", "true");
            try {
                // Reuse the existing server-side deal visibility and document counts.
                const result = await sendApiRequest({
                    url: "/Deal/GetAllDeals",
                    method: "POST",
                    data: { EntityId: page.dataset.dealId },
                    successMsg: null,
                    showLoading: false,
                    signal: controller.signal,
                });
                if (!active() || controller.signal.aborted) return;
                if (!result?.items) throw new Error("Deal request failed");
                deal = [result.items.open, result.items.success, result.items.failed]
                    .flat().find(item => item?.id?.toLowerCase() === page.dataset.dealId.toLowerCase());
                if (!deal) {
                    feedback.textContent = "سفارش یافت نشد یا به آن دسترسی ندارید.";
                    return;
                }
                page.querySelector("#dealDetailsTitle").textContent = deal.title || "سفارش بدون عنوان";
                page.querySelectorAll("[data-deal-field]").forEach(element => {
                    const value = deal[element.dataset.dealField];
                    element.textContent = value == null || value === "" ? "—" : typeof value === "number" ? number.format(value) : value;
                });
                page.querySelector("#dealDetailsFinancial").textContent = deal.financialStatus === 1 ? "تأیید شده" : "تأیید نشده";
                const description = page.querySelector("#dealDetailsDescription");
                description.textContent = deal.description || "";
                description.hidden = !deal.description;
                page.querySelectorAll("[data-deal-count]").forEach(element => {
                    element.textContent = number.format(deal.counts?.[element.dataset.dealCount] ?? 0);
                });
                feedback.textContent = "";
                content.hidden = false;
            } catch {
                if (!active() || controller.signal.aborted) return;
                feedback.textContent = "دریافت اطلاعات سفارش انجام نشد. دوباره تلاش کنید.";
                retry.hidden = false;
            } finally {
                if (active() && request === controller) page.setAttribute("aria-busy", "false");
            }
        }

        page.addEventListener("click", async event => {
            const button = event.target.closest("[data-deal-document]");
            if (!button || !deal || opening) return;
            const actions = {
                prefactors: showModalDealPreFactors,
                factors: showModalDealFactors,
                payments: showModalDealInvoices,
                orders: showModalDealOrders,
                reports: showModalDealReports,
            };
            const action = actions[button.dataset.dealDocument];
            if (!action) return;
            opening = true;
            button.disabled = true;
            try {
                await action(deal);
            } catch {
                if (active()) toastifyError("نمایش اطلاعات انجام نشد. دوباره تلاش کنید.");
            } finally {
                opening = false;
                button.disabled = false;
            }
        });
        retry.addEventListener("click", load);
        signal?.addEventListener("abort", () => request?.abort(), { once: true });
        load();
    });
})();

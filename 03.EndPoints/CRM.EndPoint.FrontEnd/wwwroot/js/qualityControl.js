(() => {
    "use strict";

    document.addEventListener("DOMContentLoaded", () => {
        const page = document.getElementById("qualityControlPage");
        if (!page) return;

        const recordsElement = page.querySelector("#qualityControlRecords");
        const modal = page.querySelector("#qualityControlModal");
        const form = page.querySelector("#qualityControlForm");
        const fields = page.querySelector("#qcFormFields");
        const questions = page.querySelector("#qcQuestions");
        const description = page.querySelector("#qcDescription");
        const summary = page.querySelector("#qcOrderSummary");
        const feedback = page.querySelector("#qcFormFeedback");
        const submit = page.querySelector("#qcSubmit");
        const questionsTab = page.querySelector("#qcQuestionsTab");
        const detailsTab = page.querySelector("#qcDetailsTab");
        const detailsJson = page.querySelector("#qcDetailsJson");
        const detailsFeedback = page.querySelector("#qcDetailsFeedback");
        const pageSignal = typeof PageRuntime !== "undefined" ? PageRuntime.current?.signal : null;
        const isActive = () => page.isConnected && !pageSignal?.aborted;
        const statuses = {
            0: { label: "بررسی نشده", color: "gray" },
            1: { label: "بدون مغایرت", color: "green" },
            2: { label: "دارای مغایرت", color: "red" },
        };
        let tableRequest = null;
        let formRequest = null;
        let factorSummaryRequest = null;
        let formGeneration = 0;
        let selectedDealId = null;
        let rowVersion = null;
        let formReady = false;
        let saving = false;
        let viewingDocument = false;

        const body = page.querySelector("#qcRecordList");
        const paginations = page.querySelectorAll("#qcPagination, #qcPaginationTop");
        const tableInfo = page.querySelector("#qcTableInfo");
        const tableFeedback = page.querySelector("#qcTableFeedback");
        const retry = page.querySelector("#qcRetry");
        const searchForm = page.querySelector("#qcSearchForm");
        const searchInput = page.querySelector("#qcSearch");
        const pageSizes = page.querySelectorAll("#qcPageSize, #qcPageSizeTop");
        const number = new Intl.NumberFormat("fa-IR");
        let start = 0;
        let length = Number(pageSizes[0].value);
        let search = "";
        let total = 0;
        let draw = 0;
        let loading = false;

        function renderPagination() {
            const count = Math.ceil(total / length);
            const current = Math.floor(start / length);
            const fragment = document.createDocumentFragment();
            function button(label, index, disabled = false) {
                const element = document.createElement("button");
                element.type = "button";
                element.textContent = label;
                element.dataset.qcPage = String(index);
                element.disabled = loading || disabled;
                if (index === current && !disabled) element.setAttribute("aria-current", "page");
                fragment.append(element);
            }
            button("قبلی", current - 1, current === 0);
            const indices = new Set([0, count - 1]);
            for (let i = Math.max(0, current - 2); i <= Math.min(count - 1, current + 2); i++) indices.add(i);
            let previous = -1;
            [...indices].filter(i => i >= 0 && i < count).sort((a, b) => a - b).forEach(i => {
                if (i > previous + 1) {
                    const gap = document.createElement("span");
                    gap.textContent = "…";
                    fragment.append(gap);
                }
                button(number.format(i + 1), i);
                previous = i;
            });
            button("بعدی", current + 1, current + 1 >= count);
            paginations.forEach(pagination => pagination.replaceChildren(fragment.cloneNode(true)));
        }

        function renderRows(rows) {
            body.innerHTML = rows.map((row, index) => {
                const status = statuses[row.status] ?? statuses[0];
                return `<article class="qc-record-card" role="listitem">
                    <div class="qc-record-field "><span class="qc-field-label">ردیف</span>${number.format(start + index + 1)}</div>
                    <div class="qc-record-field "><span class="qc-field-label">شماره سفارش</span>${escapeHtml(row.serialNumber ?? "-")}</div>
                    <div class="qc-record-field qc-title"><span class="qc-field-label">عنوان سفارش</span>${escapeHtml(row.title || "-")}</div>
                    <div class="qc-record-field qc-customer"><span class="qc-field-label">مشتری</span>${escapeHtml(row.customerName || "-")}${row.customerCode != null ? `<small class="d-block text-muted">کد مشتری: ${escapeHtml(row.customerCode)}</small>` : ""}</div>
                    <div class="qc-record-field "><span class="qc-field-label">تاریخ ثبت</span>${renderDateTimeHtml(escapeHtml(row.createDate || ""))}</div>
                    <div class="qc-record-field "><span class="qc-field-label">ثبت‌کننده</span>${escapeHtml(row.userCreator || "-")}</div>
                    <div class="qc-record-field "><span class="qc-field-label">وضعیت سفارش</span><span class="qc-badge qc-badge-${row.financialApproved ? "green" : "gray"}">${row.financialApproved ? "تحویل شده" : "تحویل نشده"}</span></div>
                    <div class="qc-record-field "><span class="qc-field-label">وضعیت کنترل کیفیت</span><span class="qc-badge qc-quality-status qc-badge-${status.color}">${status.label}</span></div>
                    <div class="qc-record-field qc-actions"><span class="qc-field-label">عملیات</span><button type="button" class="qc-button" data-qc-deal-id="${escapeHtml(row.dealId)}">کنترل کیفیت</button></div>
                </article>`;
            }).join("");
        }

        async function loadTable(reset = false) {
            if (!isActive()) return;
            if (reset) start = 0;
            tableRequest?.abort();
            const controller = tableRequest = new AbortController();
            loading = true;
            retry.hidden = true;
            recordsElement.setAttribute("aria-busy", "true");
            body.replaceChildren();
            tableInfo.textContent = "";
            tableFeedback.textContent = "در حال دریافت سفارش‌ها…";
            renderPagination();
            let result;
            try {
                result = await sendApiRequest({
                    url: "/QualityControl/GetDeals",
                    params: {
                        draw: ++draw, start, length, search,
                        financialStatus: page.querySelector('[name="qcFinancialStatus"]:checked').value,
                        status: page.querySelector('[name="qcStatus"]:checked').value,
                    },
                    signal: controller.signal,
                    showLoading: false,
                });
            } catch {
                result = null;
            }
            if (!isActive() || controller.signal.aborted) return;
            loading = false;
            recordsElement.setAttribute("aria-busy", "false");
            if (!result || !Array.isArray(result.data)) {
                tableFeedback.textContent = "دریافت سفارش‌ها انجام نشد. دوباره تلاش کنید.";
                retry.hidden = false;
                paginations.forEach(pagination => pagination.replaceChildren());
                return;
            }
            total = result.recordsFiltered;
            if (start >= total && start > 0) {
                start = Math.max(0, (Math.ceil(total / length) - 1) * length);
                return loadTable();
            }
            renderRows(result.data);
            tableFeedback.textContent = total ? "" : "سفارشی با این شرایط یافت نشد.";
            tableInfo.textContent = total
                ? `نمایش ${number.format(start + 1)} تا ${number.format(start + result.data.length)} از ${number.format(total)} سفارش`
                : "۰ سفارش";
            renderPagination();
        }

        searchForm.addEventListener("submit", event => {
            event.preventDefault();
            search = searchInput.value.trim();
            loadTable(true);
        });
        searchInput.addEventListener("input", () => {
            if (!searchInput.value && search) {
                search = "";
                loadTable(true);
            }
        });
        pageSizes.forEach(pageSize => pageSize.addEventListener("change", () => {
            length = Number(pageSize.value);
            pageSizes.forEach(select => { select.value = pageSize.value; });
            loadTable(true);
        }));
        retry.addEventListener("click", () => loadTable());
        paginations.forEach(pagination => pagination.addEventListener("click", event => {
            const button = event.target.closest("button[data-qc-page]");
            if (!button || button.disabled || loading) return;
            start = Number(button.dataset.qcPage) * length;
            loadTable();
        }));
        page.addEventListener("change", event => {
            if (event.target.matches('[name="qcFinancialStatus"], [name="qcStatus"]')) loadTable(true);
        });
        page.addEventListener("click", event => {
            const button = event.target.closest("[data-qc-deal-id]");
            if (button && !saving) openForm(button.dataset.qcDealId);
        });
        pageSignal?.addEventListener("abort", () => {
            tableRequest?.abort();
            formRequest?.abort();
            factorSummaryRequest?.abort();
        }, { once: true });
        loadTable();

        modal.addEventListener("hide.bs.modal", event => {
            if (viewingDocument) return;
            if (saving) {
                event.preventDefault();
                return;
            }
            formRequest?.abort();
            factorSummaryRequest?.abort();
            formGeneration++;
        });
        form.addEventListener("submit", saveForm);

        detailsTab.addEventListener("shown.bs.tab", () => {
            submit.hidden = true;
            if (!selectedDealId || !formReady) return;
            detailsFeedback.hidden = true;
            detailsJson.hidden = false;
            renderDealDetails(resultDeal);
        });
        questionsTab.addEventListener("shown.bs.tab", () => { submit.hidden = false; });
        modal.addEventListener("hidden.bs.modal", () => {
            if (viewingDocument) return;
            detailsJson.replaceChildren();
            detailsJson.hidden = true;
        });

        async function openForm(dealId) {
            formRequest?.abort();
            factorSummaryRequest?.abort();
            const controller = formRequest = new AbortController();
            const generation = ++formGeneration;
            selectedDealId = dealId;
            detailsJson.replaceChildren();
            detailsJson.hidden = true;
            detailsFeedback.hidden = false;
            bootstrap.Tab.getOrCreateInstance(questionsTab).show();
            submit.hidden = false;
            rowVersion = null;
            formReady = false;
            form.reset();
            fields.disabled = true;
            submit.disabled = true;
            questions.replaceChildren();
            summary.textContent = "در حال دریافت اطلاعات سفارش…";
            feedback.textContent = "در حال دریافت سؤالات کنترل کیفیت…";
            form.setAttribute("aria-busy", "true");
            ModalToggle(modal, "show");


            const result = await sendApiRequest({
                url: "/QualityControl/GetForm",
                params: { dealId },
                signal: controller.signal,
            });
            if (!isActive() || generation !== formGeneration || controller.signal.aborted) return;
            form.setAttribute("aria-busy", "false");
            if (!result?.deal || !result.questions?.length) {
                feedback.textContent = "دریافت فرم انجام نشد. مودال را ببندید و دوباره تلاش کنید.";
                return;
            }

            const deal = result.deal;
            resultDeal = deal;
            summary.textContent = `سفارش شماره ${deal.serialNumber} — ${deal.title || "بدون عنوان"} | ${deal.customerName || "بدون نام"}${deal.customerCode != null ? ` (کد مشتری: ${deal.customerCode})` : ""}`;
            rowVersion = result.rowVersion ?? null;
            description.value = result.description ?? "";
            renderQuestions(result.questions);
            feedback.textContent = "پاسخ به همهٔ سؤال‌ها الزامی است.";
            formReady = true;
            fields.disabled = false;
            submit.disabled = false;
        }

        let resultDeal = null;
        function paymentSummaryHtml(deal) {
            return `<span><strong>واریزی‌های مشتری</strong><small>تعداد: ${number.format(deal.customerPaymentCount ?? 0)}</small></span><span class="qc-payment-total">جمع کل: ${(deal.customerPaymentTotals ?? []).map(total => `${escapeHtml(total.amount)} ${escapeHtml(total.currency)}`).join(" · ") || "۰"}</span>`;
        }

        function factorSummaryHtml(deal) {
            return `<span><strong>فاکتورها</strong><small>تعداد: ${number.format(deal.counts?.factor ?? 0)}</small></span><span class="qc-payment-total">جمع کل: ${(deal.factorTotals ?? []).map(total => `${escapeHtml(total.amount)} ${escapeHtml(total.currency)}`).join(" · ") || "۰"}</span>`;
        }

        async function refreshDealSummaries() {
            if (!isActive() || !formReady || !selectedDealId || !modal.classList.contains("show")) return;
            factorSummaryRequest?.abort();
            const controller = factorSummaryRequest = new AbortController();
            const generation = formGeneration;
            const dealId = selectedDealId;
            const button = detailsJson.querySelector('[data-qc-document="factor"]');
            button?.setAttribute("aria-busy", "true");
            try {
                const result = await sendApiRequest({
                    url: "/QualityControl/GetForm",
                    params: { dealId },
                    signal: controller.signal,
                    showLoading: false,
                });
                if (!isActive() || controller.signal.aborted || generation !== formGeneration || dealId !== selectedDealId) return;
                if (!result?.deal) return;
                // Keep unsaved quality-control answers, description and row version intact.
                resultDeal = result.deal;
                const currentButton = detailsJson.querySelector('[data-qc-document="factor"]');
                if (currentButton) currentButton.innerHTML = factorSummaryHtml(resultDeal);
                const paymentButton = detailsJson.querySelector('[data-qc-document="customerPayments"]');
                if (paymentButton) paymentButton.innerHTML = paymentSummaryHtml(resultDeal);
            } finally {
                if (factorSummaryRequest === controller) {
                    button?.removeAttribute("aria-busy");
                    factorSummaryRequest = null;
                }
            }
        }

        document.addEventListener("crm:deal-factors-changed", event => {
            if (event.detail?.dealId === selectedDealId) refreshDealSummaries();
        }, pageSignal ? { signal: pageSignal } : undefined);

        document.addEventListener("crm:customer-payments-changed", event => {
            if (event.detail?.customerId !== resultDeal?.customerId) return;
            refreshDealSummaries();
        }, pageSignal ? { signal: pageSignal } : undefined);

        function renderDealDetails(deal) {
            if (!deal) return;
            const fields = [
                ["شماره سفارش", deal.serialNumber], ["مشتری", deal.customerName], ["کد مشتری", deal.customerCode],
                ["کارشناس مسئول", deal.userCreator], ["تاریخ ثبت", deal.createDate],
                ["وضعیت سفارش", deal.financialApproved ? "تحویل شده" : "تحویل نشده"],
            ];
            const documents = [["پیش‌فاکتور", "preFactor"], ["فاکتور", "factor"], ["واریزی‌ها", "payments"], ["دستور تولید", "orderHead"], ["گزارش", "report"]];
            const documentButton = (label, key) => `<button type="button" data-qc-document="${key}" ${key === "customerPayments" && !deal.customerId ? 'disabled title="سفارش به مشتری متصل نیست"' : ""}><strong>${label}</strong>${key === "customerPayments" ? "" : `<b>${number.format(deal.counts?.[key] ?? 0)}</b>`}</button>`;
            detailsJson.innerHTML = `<dl>${fields.map(([label, value]) => `<div><dt>${label}</dt><dd>${escapeHtml(value == null || value === "" ? "—" : value)}</dd></div>`).join("")}</dl><div class="qc-json-documents">${documents.map(([label, key]) => key === "payments"
                ? `<button type="button" class="qc-customer-payments" data-qc-document="customerPayments" ${!deal.customerId ? 'disabled title="سفارش به مشتری متصل نیست"' : ""}>${paymentSummaryHtml(deal)}</button>`
                : key === "factor" ? `<button type="button" class="qc-customer-payments" data-qc-document="factor">${factorSummaryHtml(deal)}</button>`
                : documentButton(label, key)).join("")}</div>`;
        }

        detailsJson.addEventListener("click", async event => {
            const button = event.target.closest("[data-qc-document]");
            if (!button || button.disabled || !resultDeal || viewingDocument || saving) return;
            const deal = resultDeal;
            const actions = {
                preFactor: ["dealPreFactorsModal", async () => {
                    currentDealDocumentData = deal;
                    document.getElementById("dealPreFactorsTitle").textContent = `پیش‌فاکتورهای سفارش ${deal.serialNumber}`;
                    document.getElementById("dealPreFactorsAdd").classList.add("d-none");
                    await getPreFactorFromDeal(deal.id, true);
                    ModalToggle("dealPreFactorsModal", "show");
                }],
                factor: ["FactorsModal", () => showModalDealFactors(deal)],
                orderHead: ["dealOrdersModal", () => showModalDealOrders(deal)],
                report: ["dealReportsModal", async () => {
                    currentDealDocumentData = deal;
                    document.getElementById("dealReportsTitle").textContent = deal.customerName || "";
                    document.getElementById("dealReportsAdd").classList.add("d-none");
                    await getReportFromDeal(deal.id);
                    ModalToggle("dealReportsModal", "show");
                }],
                accountancy: ["InvoiceListModalId", async () => {
                    await ShowModalListInvoice(deal.id, 3, null, 4, true);
                    document.getElementById("TitleListInvoiceId").textContent = `واریزی‌های سفارش ${deal.serialNumber}`;
                }],
                customerPayments: ["InvoiceListModalId", async () => {
                    await ShowModalListInvoice(deal.customerId, 5, null, 4, true);
                    document.getElementById("TitleListInvoiceId").textContent = "واریزی‌های کل مشتری";
                }],
            };
            const action = actions[button.dataset.qcDocument];
            if (!action) return;
            viewingDocument = true;
            const child = document.getElementById(action[0]);
            modal.inert = true;
            const restore = () => {
                child.removeEventListener("hidden.bs.modal", restore);
                viewingDocument = false;
                modal.inert = false;
                if (isActive()) button.focus();
            };
            child.addEventListener("hidden.bs.modal", restore);
            try { await action[1](); }
            catch {
                restore();
                if (isActive()) toastifyError("نمایش اسناد انجام نشد. دوباره تلاش کنید.");
            }
        });

        function renderQuestions(items) {
            const fragment = document.createDocumentFragment();
            [...items].sort((a, b) => a.sortOrder - b.sortOrder).forEach(item => {
                const fieldset = document.createElement("fieldset");
                fieldset.className = "qc-question";
                const title = document.createElement("legend");
                title.textContent = item.title;
                const text = document.createElement("p");
                text.textContent = item.questionText;
                const options = document.createElement("div");
                options.className = "qc-options";
                [item.zeroAnswerText, item.oneAnswerText].forEach((labelText, answer) => {
                    const label = document.createElement("label");
                    label.className = "qc-option";
                    const radio = document.createElement("input");
                    radio.type = "radio";
                    radio.name = `qc-answer-${item.id}`;
                    radio.dataset.questionId = item.id;
                    radio.value = String(answer);
                    radio.required = true;
                    radio.checked = item.answer === answer;
                    const caption = document.createElement("span");
                    caption.textContent = labelText;
                    label.append(radio, caption);
                    options.append(label);
                });
                fieldset.append(title, text, options);
                fragment.append(fieldset);
            });
            questions.replaceChildren(fragment);
        }

        function setSaving(value) {
            saving = value;
            fields.disabled = value;
            submit.disabled = value;
            submit.textContent = value ? "در حال ثبت…" : "ثبت کنترل کیفیت";
            form.setAttribute("aria-busy", String(value));
            modal.querySelectorAll("[data-bs-dismiss]").forEach(button => { button.disabled = value; });
        }

        async function saveForm(event) {
            event.preventDefault();
            if (saving || !formReady || !selectedDealId || !form.reportValidity()) return;
            const answers = Array.from(questions.querySelectorAll('input[type="radio"]:checked'), radio => ({
                questionId: radio.dataset.questionId,
                answer: Number(radio.value),
            }));
            if (answers.length !== questions.children.length || answers.some(answer => answer.answer !== 0 && answer.answer !== 1)) {
                toastifyError("پاسخ معتبر به همهٔ سؤال‌ها الزامی است.");
                return;
            }
            const data = { dealId: selectedDealId, description: description.value, rowVersion, answers };
            setSaving(true);
            let result;
            try {
                result = await sendApiRequest({
                    url: "/QualityControl/Save",
                    method: "POST",
                    headers: { RequestVerificationToken: form.querySelector('[name="__RequestVerificationToken"]').value },
                    data,
                    successMsg: "کنترل کیفیت با موفقیت ثبت شد.",
                });
            } finally {
                setSaving(false);
            }
            if (!result || !isActive()) return;
            rowVersion = result.rowVersion ?? null;
            ModalToggle(modal, "hide");
            loadTable();
        }
    });
})();

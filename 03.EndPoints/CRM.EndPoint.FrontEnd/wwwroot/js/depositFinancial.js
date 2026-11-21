async function openDepositFinancial(id) {
    const history = document.getElementById("depositFinancialHistory");
    if (!history || history.dataset.active) return;
    history.dataset.active = "true";
    const records = document.getElementById("depositFinancialRecords");
    const send = document.getElementById("sendDepositFinancial");
    const wizard = new ExternalFinancialWizard({
        modal: "depositFinancialWizard", container: "depositFinancialBody",
        back: "depositFinancialBack", confirm: "depositFinancialConfirm"
    });
    const chooser = document.getElementById("chooseExternalSoftwareModal");
    const lifetime = new AbortController();
    const { signal } = lifetime;
    const headers = ExternalFinancialWizard.antiforgeryHeaders(history);
    const api = (action, data, method = "POST", showLoading = false) => sendApiRequest({
        url: "/DepositFinancial/" + action, method, successMsg: null, signal, showLoading,
        ...(method === "GET" ? { params: data } : { data, headers })
    });
    const make = (tag, value, classes) => {
        const element = document.createElement(tag);
        if (value != null) element.textContent = value;
        if (classes) element.className = classes;
        return element;
    };
    let destinations = [], index = 0, source, draft, busy = false, closing = false, choosing = false;
    let action, goBack;
    const controls = [];
    const cleanup = () => {
        controls.splice(0).forEach(control => control.destroy());
        disposeDateTimePickers(wizard.container);
    };
    const label = type => type === 4 ? "اعلامیه بدهکار/بستانکار" : "رسید دریافت";
    async function refresh() {
        send.disabled = true;
        const result = await api("History", { id }, "GET");
        if (signal.aborted) return;
        records.replaceChildren();
        if (!result) {
            records.append(make("div", "دریافت سوابق انجام نشد.", "external-pf-empty"));
            return;
        }
        const rows = result.entries.filter(row => row.document || row.missing || row.pending);
        for (const row of rows) {
            const card = make("div", null, "external-pf-card" + (row.missing ? " border-danger" : ""));
            const info = make("div", null, "external-pf-info");
            for (const [name, value] of [
                ["نرم‌افزار", row.title], ["شماره " + label(result.payType), row.document?.number],
                ["تاریخ ثبت", row.dateText], ["مشتری", result.customerName]
            ]) {
                const field = make("div", null, "external-pf-field");
                field.append(make("span", name + ":", "external-pf-label"),
                    make("span", value ?? "—", "external-pf-value"));
                info.append(field);
            }
            card.append(info);
            if (row.missing || row.pending) {
                const actions = make("div", null, "external-pf-actions");
                if (row.missing)
                    actions.append(make("span", "سند در نرم‌افزار مقصد پیدا نشد.", "text-danger small"));
                const button = make("button", row.pending ? "ادامه ثبت" : "حذف سابقه",
                    "external-pf-action " + (row.missing ? "text-danger" : "external-pf-view"));
                button.type = "button";
                button.onclick = async () => {
                    if (busy || choosing || isModalOpen(wizard.modal)) return;
                    if (row.pending) {
                        destinations = [row];
                        index = 0;
                        if (row.document) {
                            busy = true;
                            try {
                                if (await api("Submit", { id, softwareId: row.id }, "POST", true)) await refresh();
                            } finally { busy = false; }
                        } else {
                            choosing = true;
                            try { await prepare(true); }
                            finally { choosing = false; }
                        }
                        return;
                    }
                    busy = true;
                    try {
                        const answer = await Swal.fire({
                            target: history, title: "حذف سابقه ارسال", icon: "warning", showCancelButton: true,
                            confirmButtonText: "حذف سابقه", cancelButtonText: "انصراف", focusCancel: true
                        });
                        if (answer.isConfirmed && (await api("DeleteMissing", { id, softwareId: row.id }))?.deleted)
                            await refresh();
                    } finally { busy = false; }
                };
                actions.append(button);
                card.append(actions);
            }
            records.append(card);
        }
        if (!rows.length) records.append(make("div", "این واریزی هنوز در نرم‌افزار مالی ثبت نشده است.", "external-pf-empty"));
        send.disabled = !result.entries.length;
        send.onclick = () => choose(result.entries);
    }

    async function choose(entries) {
        if (choosing || busy || isModalOpen(wizard.modal)) return;
        choosing = true;
        const events = new AbortController();
        const chooserSignal = AbortSignal.any([signal, events.signal]);
        let selected = [];
        let shown = false;
        chooser.addEventListener("shown.bs.modal", () => {
            shown = true;
            if (selected.length) ModalToggle(chooser, "hide");
        }, { once: true, signal: chooserSignal });
        chooser.addEventListener("hidden.bs.modal", async () => {
            try {
                destinations = selected.map(softwareId => entries.find(item => item.id === softwareId)).filter(Boolean);
                index = 0;
                if (destinations.length) await prepare(true);
                else send.focus();
            } finally { choosing = false; }
        }, { once: true, signal: chooserSignal });
        const result = await showExternalSoftwareModal(id, {
            mapType: 11,
            onConfirm: ({ externalSoftwareIds }) => {
                selected = externalSoftwareIds;
                if (shown) ModalToggle(chooser, "hide");
            }
        });
        if (!result) {
            events.abort();
            choosing = false;
        }
    }
    async function prepare(open) {
        const destination = destinations[index];
        draft = { id, softwareId: destination.id };
        source = await api("Prepare", draft);
        if (!source || signal.aborted) {
            if (!open) close();
            return;
        }
        Object.assign(draft, { customerId: source.customerId, bankAccountId: source.bankAccountId,
            dateText: source.dateText, dueText: source.dueText });
        partyStage();
        if (open) ModalToggle(wizard.modal, "show");
    }
    function stage(title, subtitle, step, icon, body, confirmText, next, previous, large = false) {
        cleanup();
        action = next;
        goBack = previous;
        wizard.render({ destination: destinations[index].title, title, subtitle, step, icon, content: body, large,
            position: "نرم‌افزار " + (index + 1) + " از " + destinations.length });
        wizard.setActions({ confirmText, confirmDisabled: false, backHidden: !previous, backDisabled: false });
        dateTimePicker(wizard.container);
    }
    function select(key, caption, options, valueId = "id", title = item => item.title) {
        return ExternalFinancialWizard.selectField({
            id: "deposit-field-" + key, label: caption, placeholder: "جستجو و انتخاب",
            selected: draft[key], options: (options ?? []).map(item => ({ value: item[valueId], label: title(item) }))
        });
    }
    function enhance(...keys) {
        for (const key of keys)
            controls.push(new TomSelect(document.getElementById("deposit-field-" + key), {
                maxOptions: 80,
                score(query) {
                    return this.getScoreFunction(normalizePersianText(query));
                }
            }));
    }
    function capture(keys) {
        for (const key of keys) {
            const input = document.getElementById("deposit-field-" + key);
            draft[key] = input.tagName === "SELECT" && key !== "customerAccount" ? Number(input.value) : input.value.trim();
        }
    }
    function step(number) {
        const total = source.type === 1 ? 5 : source.type === 2 ? 4 : 3;
        return "مرحله " + number.toLocaleString("fa-IR") + " از " + total.toLocaleString("fa-IR");
    }
    function partyStage() {
        const body = ExternalFinancialWizard.crmCustomer(source.customerName)
            + select("customerId", "مشتری معادل در نرم‌افزار مالی", source.options.customers);
        stage("تطبیق مشتری", "معادل مشتری را در نرم‌افزار مقصد مشخص کنید", step(1),
            '<i class="fas fa-users" aria-hidden="true"></i>', body, "تأیید و ادامه", async () => {
                capture(["customerId"]);
                if (!draft.customerId) return toastifyError("مشتری معادل را انتخاب کنید.");
                const saved = await api("SaveCustomerMapping", draft);
                if (!saved || signal.aborted) return;
                draft.customerId = saved.id;
                if (source.type === 1) bankStage();
                else if (source.type === 2) cashStage();
                else detailsStage();
            }, null);
        enhance("customerId");
    }
    function bankStage() {
        const body = '<div class="external-mapping-entity"><span class="external-mapping-label">حساب بانکی در CRM</span> <strong>'
            + escapeHtml(source.bankAccountTxt || "—") + '</strong></div>'
            + select("bankAccountId", "حساب بانکی معادل در نرم‌افزار مالی", []);
        stage("تطبیق حساب بانکی", "حساب بانکی مقصد این حواله را مشخص کنید", step(2),
            '<i class="fas fa-university" aria-hidden="true"></i>', body, "تأیید و ادامه", async () => {
                capture(["bankAccountId"]);
                if (!draft.bankAccountId) return toastifyError("حساب بانکی معادل را انتخاب کنید.");
                const saved = await api("SaveBankMapping", draft);
                if (!saved || signal.aborted) return;
                draft.bankAccountId = saved.id;
                cashStage();
            }, partyStage);
        controls.push(createSupplierTomSelect(
            document.getElementById("deposit-field-bankAccountId"), source.options.accounts,
            draft.bankAccountId || null, { showOfficial: false }));
    }
    function cashStage() {
        stage("انتخاب صندوق", "صندوق رسید دریافت را انتخاب کنید", step(source.type === 1 ? 3 : 2),
            '<i class="fas fa-cash-register" aria-hidden="true"></i>', select("cashId", "صندوق", source.options.cash),
            "تأیید و ادامه", () => {
                capture(["cashId"]);
                if (!draft.cashId) return toastifyError("صندوق را انتخاب کنید.");
                detailsStage();
            }, source.type === 1 ? bankStage : partyStage);
        enhance("cashId");
    }
    function input(key, caption, max, date = false) {
        return '<div class="external-financial-field"><label class="form-label mb-1" for="deposit-field-' + key + '">'
            + caption + '</label><input type="text" id="deposit-field-' + key + '" value="' + escapeHtml(draft[key] ?? "")
            + '" maxlength="' + max + '" autocomplete="off" class="' + (date ? "sdate ex-preview-invoice-number" : "form-control form-control-sm")
            + '"' + (date ? ' dir="ltr" data-datepicker-change' : '') + '></div>';
    }
    function detailsStage() {
        const note = source.type === 4, cheque = source.type === 2;
        const selects = ["customerAccount", ...(note ? ["vendorId"] : cheque ? ["bankId"] : [])];
        const detailKeys = note ? [] : cheque ? ["dueText", "reference", "accountNo"] : ["reference"];
        const optionalKeys = cheque ? ["sayad", "secondNumber", "chequeOwner", "branchCode", "branchTitle"] : [];
        const required = [...selects, ...detailKeys];
        const fields = [...required, ...optionalKeys, "description"];
        const body = '<div class="d-grid gap-2">'
            + select("customerAccount", note ? "معین بستانکار" : "معین دریافت", source.options.creditAccounts, "id", account => account.code + " - " + account.title)
            + (note ? '<div class="external-mapping-entity"><span class="external-mapping-label">بابت بدهی به</span> <strong>'
                + escapeHtml(source.beneficiary || "—") + '</strong></div>'
                + select("vendorId", "تأمین‌کننده بدهکار", source.options.vendors, "id",
                    item => [item.code, item.title].filter(Boolean).join(" - ")) : "")
            + (note && source.toName ? '<div class="external-mapping-entity">واریز به نام ' + escapeHtml(source.toName) + '</div>' : "")
            + (cheque ? input("dueText", "سررسید چک", 10, true) : "")
            + (cheque ? select("bankId", "بانک صادرکننده", source.options.banks)
                + input("sayad", "شناسه صیادی (اختیاری)", 16) + input("accountNo", "شماره حساب صادرکننده", 50)
                + input("secondNumber", "شماره دوم چک", 50) + input("chequeOwner", "صاحب چک", 250)
                + input("branchCode", "کد شعبه", 250) + input("branchTitle", "نام شعبه", 250) : "")
            + (!note ? input("reference", cheque ? "شماره چک" : "شماره پیگیری حواله", cheque ? 50 : 100) : "")
            + input("description", "شرح سند (اختیاری)", 250) + '</div>';
        stage("اطلاعات " + label(source.type), "اطلاعات لازم برای ثبت را تکمیل کنید", step(note ? 2 : cheque ? 3 : 4),
            '<i class="fas fa-file-invoice" aria-hidden="true"></i>', body, "پیش‌نمایش", async () => {
                capture(fields);
                if (required.some(key => !draft[key]))
                    return toastifyError("اطلاعات الزامی سند را کامل کنید.");
                const preview = await api("Preview", draft);
                if (preview) previewStage(preview);
            }, () => {
                capture(fields);
                if (note) partyStage();
                else cashStage();
            });
        enhance(...selects);
    }
    function previewStage(preview) {
        draft.previewToken = preview.previewToken;
        const note = source.type === 4, cheque = source.type === 2;
        const fields = [
            { label: "نوع واریزی", value: note ? "تهاتری" : cheque ? "چکی" : "حواله بانکی" },
            { label: note ? "معین بستانکار" : "معین دریافت", value: preview.creditTitle },
            { label: "تاریخ", value: preview.dateText },
            { label: note ? "مشتری بستانکار" : "مشتری", value: preview.customer, wide: true },
            { label: "نرم‌افزار مقصد", value: destinations[index].title, accent: true }
        ];
        let rows;
        if (note) {
            rows = [["تأمین‌کننده بدهکار", preview.debitTitle]];
        } else {
            const cash = source.options.cash.find(item => item.id === draft.cashId)?.title;
            if (cheque) {
                rows = [
                    ["شماره چک", draft.reference], ["سررسید", preview.dueText], ["صندوق", cash],
                    ["بانک صادرکننده", source.options.banks.find(bank => bank.id === draft.bankId)?.title],
                    ["شماره حساب صادرکننده", draft.accountNo], ["شناسه صیادی", draft.sayad],
                    ["شماره دوم چک", draft.secondNumber], ["صاحب چک", draft.chequeOwner],
                    ["کد شعبه", draft.branchCode], ["نام شعبه", draft.branchTitle]
                ];
            } else {
                const bank = source.options.accounts.find(account => account.value === draft.bankAccountId);
                rows = [
                    ["حساب بانکی مقصد", bank ? buildBankAccountUi(bank).displayText : preview.debitTitle],
                    ["شماره حواله", draft.reference], ["صندوق", cash]
                ];
            }
        }
        if (draft.description) rows.push(["شرح", draft.description]);
        const body = make("div");
        body.innerHTML = renderExternalFinancialPreview({
            fields, columns: ["مشخصات سند", "مقدار"], rows: rows.filter(([, value]) => value),
            totals: [["مبلغ واریزی", preview.amount]]
        })
            + '<p class="text-muted small mt-3 mb-0">' + (preview.autoVoucher
                ? "با ثبت این سند، سند حسابداری نیز به‌صورت خودکار صادر می‌شود."
                : "صدور خودکار سند حسابداری در مقصد غیرفعال است.") + "</p>";
        const dateField = body.querySelectorAll(".ex-preview-field")[2];
        const dateLabel = make("label", "تاریخ:", "ex-preview-label");
        dateLabel.htmlFor = "deposit-preview-date";
        const date = make("input", null, "sdate ex-preview-invoice-number");
        date.type = "text";
        date.id = dateLabel.htmlFor;
        date.dir = "ltr";
        date.autocomplete = "off";
        date.maxLength = 10;
        date.value = draft.dateText || preview.dateText;
        dateField.replaceChildren(dateLabel, date);
        stage("بررسی نهایی " + label(source.type), "اطلاعات سند را پیش از ثبت بررسی کنید", step(note ? 3 : cheque ? 4 : 5),
            '<i class="fas fa-clipboard-check" aria-hidden="true"></i>', body, "تأیید و ثبت در نرم‌افزار", async () => {
                const value = toEnglishDigits(date.value.trim());
                if (!/^[0-9]{4}\/[0-9]{2}\/[0-9]{2}$/.test(value))
                    return toastifyError("تاریخ سند معتبر نیست.");
                draft.dateText = value;
                const saved = await api("Submit", draft, "POST", true);
                if (!saved) return;
                toastifySuccess(label(source.type) + " شماره " + saved.number + " ثبت شد.");
                if (++index < destinations.length) await prepare(false);
                else close();
            }, () => {
                draft.dateText = date.value.trim();
                detailsStage();
            }, true);
    }
    function close() {
        closing = true;
        ModalToggle(wizard.modal, "hide");
    }
    wizard.confirm.addEventListener("click", async () => {
        if (busy || !action) return;
        busy = true;
        wizard.confirm.disabled = wizard.back.disabled = true;
        try { await action(); }
        finally { busy = false; wizard.confirm.disabled = wizard.back.disabled = false; }
    }, { signal });
    wizard.back.addEventListener("click", () => { if (!busy) goBack?.(); }, { signal });
    wizard.modal.addEventListener("hide.bs.modal", event => {
        if (busy && !closing) event.preventDefault();
    }, { signal });
    wizard.modal.addEventListener("hidden.bs.modal", async () => {
        closing = false;
        cleanup();
        wizard.container.replaceChildren();
        await refresh();
        if (!signal.aborted) (send.disabled ? history.querySelector(".btn-close") : send)?.focus();
    }, { signal });
    history.addEventListener("hidden.bs.modal", () => {
        lifetime.abort();
        cleanup();
        send.onclick = null;
        delete history.dataset.active;
    }, { once: true });
    enableMomentumScrollScope(history, { signal });
    enableMomentumScrollScope(wizard.modal, { signal });
    records.replaceChildren();
    ModalToggle(history, "show");
    await refresh();
}

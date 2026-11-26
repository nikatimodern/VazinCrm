async function openDepositFinancial(id) {
    const history = document.getElementById("depositFinancialHistory");
    if (!history || history.dataset.active) return;
    history.dataset.active = "true";
    const historyTitle = history.querySelector("#depositFinancialHistoryTitle");
    const titleText = "مدیریت ارسال واریزی";
    historyTitle.textContent = titleText;
    const records = document.getElementById("depositFinancialRecords");
    const historyCard = ExternalFinancialHistory;
    const historyUi = new historyCard({ modal: history, list: records });
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
    const noteParty = { supplier: 1, customer: 2, other: 3 };
    const chequeAction = { assign: 1, collect: 2, spend: 3 };
    const chequeActionTitle = {
        [chequeAction.assign]: "واگذاری به بانک",
        [chequeAction.collect]: "وصول چک",
        [chequeAction.spend]: "خرج کردن چک"
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
        historyTitle.textContent = result.serialNum ? titleText + " شماره " + result.serialNum : titleText;
        const cheque = result.payType === 2;
        send.querySelector("[data-financial-history-send-label]").textContent =
            cheque ? "ارسال رسید دریافت به نرم‌افزار مالی" : "ارسال به نرم‌افزار مالی";
        const rows = result.entries.filter(row => row.document || row.missing || row.pending);
        historyUi.render(rows, {
            emptyText: "این واریزی هنوز در نرم‌افزار مالی ثبت نشده است.",
            card: row => {
                const card = {
                    title: row.title, missing: row.missing, related: row.cheque ? chequeHistory(row) : null,
                    fields: [["شماره " + label(result.payType), row.document?.number],
                        ["مشتری", result.customerName]],
                    registration: { date: row.registeredAt, by: row.registeredBy },
                    status: row.missing ? "سند در نرم‌افزار مقصد پیدا نشد." : null,
                    discrepancy: depositDiscrepancy(row, 0, label(result.payType)),
                    operations: row.document && row.mappingId && !row.pending
                        ? maintenanceActions(row, 0, label(result.payType)) : [],
                };
                if (row.missing || row.pending) {
                    const button = historyCard.action(row.pending ? "ادامه ثبت" : "حذف سابقه", { danger: !row.pending });
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
                    card.action = button;
                }
                return card;
            },
        });
        send.disabled = !result.entries.length;
        send.onclick = () => choose(result.entries);
    }

    function chequeHistory(row) {
        const entries = [];
        let nextAction;
        const actionId = row.cheque.nextAction;
        const sync = row.synchronization;
        if (actionId && !row.pending && !sync?.changes.length && !sync?.assignmentRestriction &&
            !(actionId === chequeAction.collect && sync?.receiptRestriction)) {
            const button = historyCard.action("ثبت " + chequeActionTitle[actionId]);
            button.onclick = async () => {
                if (choosing || busy || isModalOpen(wizard.modal)) return;
                choosing = true;
                button.disabled = true;
                try { await prepareChequeOperation(row, actionId); }
                finally { choosing = false; button.disabled = false; }
            };
            nextAction = button;
        }
        for (const [operation, title, entry, dateLabel, detailLabel, detail] of [
            [chequeAction.assign, "واگذاری به بانک", row.cheque.assignment, "تاریخ واگذاری", "حساب بانکی", row.cheque.assignment?.bankAccount],
            [chequeAction.collect, "وصول چک", row.cheque.collection, "تاریخ وصول", "حساب بانکی", row.cheque.collection?.bankAccount],
            [chequeAction.spend, "اعلامیه پرداخت · خرج کردن چک", row.cheque.payment, "تاریخ اعلامیه", "طرف مقابل", row.cheque.payment?.payee]
        ]) {
            if (!entry) continue;
            const caption = title + " شماره " + entry.number +
                (entry.amount == null ? "" : " · " + entry.amount.toLocaleString() + " ریال");
            entries.push(historyCard.relatedItem({
                title: caption, fields: [[dateLabel, entry.dateText], [detailLabel, detail]],
                registration: { date: entry.registeredAt },
                discrepancy: depositDiscrepancy(row, operation, title),
                operations: row.mappingId && !row.pending
                    ? maintenanceActions(row, operation, title) : [],
            }));
        }
        return entries.length || nextAction
            ? { title: "عملیات مرتبط چک", entries, action: nextAction, open: true }
            : null;
    }

    function depositDiscrepancy(row, operation, title) {
        const sync = row.synchronization;
        if (!sync || row.pending || !row.mappingId) return null;
        if (operation === 0 && row.cheque?.bankCollectionRestriction)
            return { message: row.cheque.bankCollectionRestriction };
        const restriction = operation === chequeAction.assign ? sync.assignmentRestriction
            : operation === chequeAction.collect ? sync.collectionRestriction
                : operation === chequeAction.spend ? sync.paymentRestriction : sync.receiptRestriction;
        const changed = operation === 0 ? sync.changes.length > 0 : operation === chequeAction.spend && sync.paymentChanged;
        if (!restriction && !changed && (operation || !row.cheque?.message)) return null;
        return {
            message: restriction || (changed ? title + " با ERP مغایرت دارد." : row.cheque.message),
            onSynchronize: changed && !restriction ? event => openSynchronization(row, operation, title, event.currentTarget) : null,
        };
    }

    async function openSynchronization(row, operation, title, button) {
        if (choosing || busy || isModalOpen(wizard.modal)) return;
        choosing = button.disabled = true;
        try {
            const key = { id, softwareId: row.id, mappingId: row.mappingId, action: operation };
            const preview = await api("PreviewSynchronization", key);
            if (!preview || signal.aborted || !isModalOpen(history)) return;
            const { impact, changes } = preview;
            const blocked = impact.blockers.length > 0;
            const content = make("div", null, "external-prefactor-preview d-grid gap-3");
            appendExternalFinancialPreviewSection(content, title + " شماره " + impact.number,
                ["عنوان", "مقدار فعلی در سپیدار", "مقدار جایگزین"], changes.map(change => [change.title, change.current, change.proposed]));
            const notes = make("ul", null, "small ps-4 mb-0");
            if (blocked) impact.blockers.forEach(text => notes.append(make("li", text)));
            else if (impact.voucherNumber != null)
                notes.append(make("li", "سند حسابداری شماره " + impact.voucherNumber +
                    (impact.autoVoucher ? " بازسازی می‌شود." : " حذف می‌شود و صدور خودکار در مقصد غیرفعال است.")));
            else notes.append(make("li", impact.autoVoucher
                ? "سند حسابداری صادر می‌شود." : "صدور خودکار سند حسابداری در مقصد غیرفعال است."));
            if (notes.childElementCount) content.append(notes);
            destinations = [row];
            index = 0;
            stage("بررسی نهایی همگام‌سازی",
                blocked ? "برای این سند امکان همگام‌سازی وجود ندارد." : "تغییرات روی همان سند سپیدار اعمال می‌شوند.",
                "", '<i class="fas fa-sync-alt" aria-hidden="true"></i>', content,
                blocked ? "بستن" : "تأیید و همگام‌سازی",
                blocked ? close : async () => {
                    const result = await api("SubmitSynchronization", { ...key, token: impact.token }, "POST", true);
                    if (!result?.saved) return;
                    toastifySuccess(title + " همگام شد.");
                    close();
                }, blocked ? null : close, true);
            ModalToggle(wizard.modal, "show");
        } finally { choosing = button.disabled = false; }
    }

    function maintenanceActions(row, operation, title) {
        const actions = [];
        const add = (edit, caption, icon) => actions.push(historyCard.action(caption, {
            icon, danger: !edit,
            onClick: () => openMaintenance(row, operation, title, edit),
        }));
        if (operation === 0 || operation === chequeAction.spend)
            add(true, "ویرایش انتقال", "fa-pencil-alt");
        add(false, "حذف از مقصد", "fa-trash-alt");
        return actions;
    }

    async function openMaintenance(row, operation, title, edit) {
        if (choosing || busy || isModalOpen(wizard.modal)) return;
        if (edit && (operation === 0 && row.synchronization?.changes.length ||
            operation === chequeAction.spend && row.synchronization?.paymentChanged))
            return toastifyError("ابتدا مغایرت سند را از روی کارت سوابق برطرف کنید.");
        choosing = true;
        try {
            const key = { id, softwareId: row.id, mappingId: row.mappingId, action: operation, edit };
            const data = await api("PreviewMaintenance", key);
            if (!data || signal.aborted || !isModalOpen(history)) return;
            const impact = data.impact;
            if (impact.blockers.length)
                return toastifyError(impact.blockers[0]);
            if (edit) {
                destinations = [row];
                index = 0;
                showMaintenanceEdit(key, title, data);
                ModalToggle(wizard.modal, "show");
                return;
            }
            const content = make("div", null, "text-start");
            content.append(make("p", title + " شماره " + impact.number + " از مقصد حذف شود؟", "mb-2"));
            if (impact.voucherNumber != null)
                content.append(make("p", "سند حسابداری شماره " + impact.voucherNumber + " هم حذف می‌شود.", "small mb-1"));
            if (operation)
                content.append(make("p", operation === chequeAction.collect
                    ? "چک به وضعیت واگذارشده به بانک برمی‌گردد." : "چک به وضعیت نزد صندوق برمی‌گردد.", "small mb-0"));
            const answer = await Swal.fire({
                target: history, title: "حذف سند از مقصد", html: content, icon: "warning", showCancelButton: true,
                confirmButtonText: "حذف سند", cancelButtonText: "انصراف", focusCancel: true,
            });
            if (!answer.isConfirmed || signal.aborted || !isModalOpen(history)) return;
            if ((await api("SubmitMaintenance", { ...key, token: impact.token }, "POST", true))?.deleted)
                toastifySuccess(title + " از نرم‌افزار مقصد حذف شد.");
            if (!signal.aborted) await refresh();
        } finally { choosing = false; }
    }

    function showMaintenanceEdit(key, title, data) {
        const payment = key.action === chequeAction.spend;
        const note = data.type === 4;
        const cheque = data.type === 2;
        const options = data.options;
        const draftInput = payment ? data.paymentInput : data.input;
        draft = { ...draftInput, dueText: data.dueText,
            creditType: draftInput.creditSide?.type, creditAccountId: draftInput.creditSide?.accountId, creditDetailId: draftInput.creditSide?.detailId,
            debitType: draftInput.debitSide?.type, debitAccountId: draftInput.debitSide?.accountId, debitDetailId: draftInput.debitSide?.detailId };
        const body = make("div", null, "d-grid gap-2");
        const summary = historyCard.fields([["تاریخ سند", data.dateText], ["مبلغ", Number(data.amount).toLocaleString() + " ریال"]]);
        summary.classList.add("external-pf-record-fields", "border", "rounded", "p-2");
        body.append(summary);
        let selects = [], fields;
        if (payment) {
            const kind = options.paymentTypes.find(type => type.id === draft.paymentType);
            const box = options.cash.find(item => item.id === draft.cashId);
            body.append(historyCard.fields([["نوع پرداخت", kind?.title], ["صندوق", box?.title]]));
            selects = ["payeeId", "accountId"];
            fields = [...selects, "description"];
            body.insertAdjacentHTML("beforeend", select("payeeId", "طرف مقابل", options.payees.filter(x => x.paymentTypes.includes(draft.paymentType)))
                + select("accountId", "معین پرداخت", options.accounts) + input("description", "شرح اعلامیه پرداخت", 250, { required: true }));
        } else if (note) {
            selects = ["creditType", "creditAccountId", "debitType", "debitAccountId"];
            fields = [...selects, "description", "noteDescription"];
            const sides = make("div", null, "row row-cols-1 row-cols-sm-2 g-2");
            for (const [side, name] of [["debit", "بدهکار"], ["credit", "بستانکار"]]) {
                const column = make("div", null, "d-grid");
                column.innerHTML = maintenanceEditSection(name,
                    select(side + "Type", "نوع " + name, options.partyTypes)
                    + select(side + "AccountId", "کد معین " + name, options.subsidiaryAccounts)
                    + '<div data-deposit-maintenance-detail="' + side + '" hidden></div>', "d-grid gap-2");
                sides.append(column);
            }
            body.append(sides);
            body.insertAdjacentHTML("beforeend", noteField("description", "شرح قلم", 250, true)
                + noteField("noteDescription", "شرح اعلامیه", 4000));
        } else {
            selects = ["customerId", "creditAccountId", "cashId"];
            fields = [...selects, "reference", "description"];
            const receiptFields = select("customerId", "مشتری معادل", options.customers)
                + select("creditAccountId", "معین دریافت", options.subsidiaryAccounts)
                + select("cashId", "صندوق", options.cash)
                + (cheque ? "" : select("bankAccountId", "حساب بانکی مقصد", [])
                    + input("reference", "شماره پیگیری حواله", 100, { required: true }))
                + receiptDescriptionField();
            body.insertAdjacentHTML("beforeend", maintenanceEditSection("اطلاعات رسید دریافت", receiptFields));
            if (cheque) {
                selects.push("bankId");
                fields.push("bankId", "dueText", "sayad", "accountNo", ...chequeExtraKeys);
                body.insertAdjacentHTML("beforeend", maintenanceEditSection("مشخصات چک",
                    input("reference", "شماره چک", 50, { required: true })
                    + input("dueText", "سررسید چک", 10, { date: true, required: true })
                    + select("bankId", "بانک صادرکننده", options.banks)
                    + input("accountNo", "شماره حساب صادرکننده", 50, { required: true })
                    + input("sayad", "شناسه صیادی", 16))
                    + chequeExtraFields("اطلاعات تکمیلی چک", "row row-cols-1 row-cols-sm-2 g-2 mt-1"));
            } else {
                fields.push("bankAccountId");
            }
        }
        stage("ویرایش انتقال " + title, "اطلاعات انتقال را بررسی کنید.", "سند شماره " + data.impact.number,
            '<i class="fas fa-pencil-alt" aria-hidden="true"></i>', body, "ذخیره تغییرات", async () => {
                capture(fields);
                if (note && !draft.description) return toastifyError("شرح قلم اعلامیه را وارد کنید.");
                const impact = data.impact;
                const details = [];
                if (impact.voucherNumber != null)
                    details.push("سند حسابداری شماره " + impact.voucherNumber + (impact.autoVoucher ? " بازسازی می‌شود." : " حذف می‌شود."));
                else if (impact.autoVoucher)
                    details.push("سند حسابداری صادر می‌شود.");
                if (!impact.autoVoucher)
                    details.push("صدور خودکار در سپیدار غیرفعال است و سند حسابداری جدید صادر نمی‌شود.");
                if (!await historyCard.confirmEdit(wizard.modal, "تغییرات " + title + " شماره " + impact.number + " ذخیره شود؟", details)
                    || signal.aborted) return;
                const payload = payment ? { payeeId: draft.payeeId, accountId: draft.accountId, description: draft.description }
                    : { document: draft };
                const saved = await api("SubmitMaintenance", { ...key, ...payload, token: impact.token }, "POST", true);
                if (!saved?.saved) return;
                toastifySuccess(saved.voucherIssued ? "تغییرات سند ذخیره شد."
                    : "تغییرات ذخیره شد. صدور خودکار در سپیدار غیرفعال است و سند حسابداری جدید صادر نشد.", 6000);
                close();
            }, close, true);
        wizard.setActions({ backText: "انصراف" });
        enhance(...selects);
        if (!payment && !note && !cheque)
            controls.push(createSupplierTomSelect(document.getElementById("deposit-field-bankAccountId"),
                options.accounts, draft.bankAccountId || null, { showOfficial: false }));
        if (note)
            bindMaintenanceNoteDetails(body, options);
    }

    function maintenanceEditSection(title, fields, layout = "row row-cols-1 row-cols-sm-2 g-2") {
        return '<section class="border rounded p-2"><h6 class="form-label fbold mb-2">' + title
            + '</h6><div class="' + layout + '">' + fields + '</div></section>';
    }

    function bindMaintenanceNoteDetails(body, options) {
        const choices = { [noteParty.supplier]: options.vendors, [noteParty.customer]: options.customers,
            [noteParty.other]: options.otherDetails };
        for (const [side, title] of [["credit", "بستانکار"], ["debit", "بدهکار"]]) {
            const typeKey = side + "Type", accountKey = side + "AccountId", detailKey = side + "DetailId";
            const host = body.querySelector('[data-deposit-maintenance-detail="' + side + '"]');
            const refreshDetail = () => {
                const old = host.querySelector("select")?.tomselect;
                if (old) {
                    controls.splice(controls.indexOf(old), 1);
                    old.destroy();
                }
                const required = options.subsidiaryAccounts.find(x => x.id === draft[accountKey])?.hasDl;
                host.hidden = !required;
                if (!required) {
                    draft[detailKey] = 0;
                    host.replaceChildren();
                    return;
                }
                host.innerHTML = select(detailKey, "کد تفصیلی " + title, choices[draft[typeKey]]);
                capture([detailKey]);
                enhance(detailKey);
                host.querySelector("select").addEventListener("change", () => capture([detailKey]));
            };
            document.getElementById("deposit-field-" + typeKey).addEventListener("change", () => {
                capture([typeKey]);
                draft[detailKey] = 0;
                refreshDetail();
            });
            document.getElementById("deposit-field-" + accountKey).addEventListener("change", () => {
                capture([accountKey]);
                refreshDetail();
            });
            refreshDetail();
        }
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
            dueText: source.dueText, chequeAmount: source.defaultChequeAmount });
        if (source.type === 4) {
            Object.assign(draft, { creditType: noteParty.customer, debitType: noteParty.supplier,
                creditDetailId: source.customerId });
            noteSideStage(true);
        } else {
            partyStage();
        }
        if (open) ModalToggle(wizard.modal, "show");
    }
    function stage(title, subtitle, step, icon, body, confirmText, next, previous, large = false) {
        cleanup();
        action = next;
        goBack = previous;
        wizard.render({ destination: destinations[index].title, title, subtitle, step, icon, content: body, large,
            position: "نرم‌افزار " + (index + 1) + " از " + destinations.length });
        wizard.setActions({ confirmText, confirmDisabled: false, backText: "برگشت", backHidden: !previous, backDisabled: false });
        dateTimePicker(wizard.container);
    }
    function select(key, caption, options) {
        return ExternalFinancialWizard.selectField({
            id: "deposit-field-" + key, label: caption, placeholder: "جستجو و انتخاب",
            required: true, selected: draft[key],
            options: (options ?? []).map(item => ({ value: item.id, label: item.title })),
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
            draft[key] = input.tagName === "SELECT" ? Number(input.value) : input.value.trim();
        }
    }
    function step(number) {
        const total = source.type === 4 ? 3 : 5;
        return "مرحله " + number.toLocaleString("fa-IR") + " از " + total.toLocaleString("fa-IR");
    }
    function creditAccountField(caption) {
        return select("creditAccountId", caption, source.options.subsidiaryAccounts);
    }
    function partyStage() {
        const needsAccount = source.type === 2;
        const body = ExternalFinancialWizard.crmCustomer(source.customerName)
            + select("customerId", "مشتری معادل در نرم‌افزار مالی", source.options.customers)
            + (needsAccount ? creditAccountField("معین دریافت") : "");
        const keys = needsAccount ? ["customerId", "creditAccountId"] : ["customerId"];
        stage("تطبیق مشتری",
            needsAccount ? "معادل مشتری و حساب معین را مشخص کنید" : "معادل مشتری را در نرم‌افزار مقصد مشخص کنید", step(1),
            '<i class="fas fa-users" aria-hidden="true"></i>', body, "تأیید و ادامه", async () => {
                capture(keys);
                if (!draft.customerId) return toastifyError("مشتری معادل را انتخاب کنید.");
                if (needsAccount && !draft.creditAccountId) return toastifyError("حساب معین را انتخاب کنید.");
                const saved = await api("SaveCustomerMapping", draft);
                if (!saved || signal.aborted) return;
                draft.customerId = saved.id;
                if (source.type === 1) bankStage();
                else cashStage();
            }, null);
        enhance(...keys);
    }
    function bankStage() {
        const body = '<div class="external-mapping-entity"><span class="external-mapping-label">حساب بانکی در ERP</span> <strong>'
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
                if (source.type === 2) chequeStage();
                else detailsStage();
            }, () => {
                capture(["cashId"]);
                if (source.type === 1) bankStage();
                else partyStage();
            });
        enhance("cashId");
    }
    function input(key, caption, max, { date = false, required = false } = {}) {
        return '<div class="external-financial-field"><label class="form-label mb-1" for="deposit-field-' + key + '">'
            + caption + '</label><input type="text" id="deposit-field-' + key + '" value="' + escapeHtml(draft[key] ?? "")
            + '" maxlength="' + max + '" autocomplete="off" class="form-control form-control-sm' + (date ? " sdate text-center" : "")
            + '"' + (date ? ' dir="ltr" data-datepicker-change' : '') + (required ? ' required' : '') + '></div>';
    }
    function receiptDescriptionField() {
        return input("description", "شرح رسید دریافت", 250, { required: true });
    }
    const chequeExtraKeys = ["secondNumber", "chequeOwner", "branchCode", "branchTitle"];
    const secondChequeNumberLabel = "شماره دوم چک (پشت نمره)";
    function chequeExtraFields(title, gridClass) {
        return '<details class="external-financial-note external-financial-note-panel"'
            + (chequeExtraKeys.some(key => draft[key]) ? " open" : "") + '>'
            + '<summary>' + title + '</summary><div class="' + gridClass + '">'
            + input("secondNumber", secondChequeNumberLabel, 50) + input("chequeOwner", "صاحب چک", 250)
            + input("branchCode", "کد شعبه", 250) + input("branchTitle", "نام شعبه", 250)
            + '</div></details>';
    }
    function noteField(key, caption, maxLength, required = false) {
        const value = draft[key] ?? "";
        const id = "deposit-field-" + key;
        const label = '<label class="' + (required ? 'form-label mb-1' : 'visually-hidden') + '" for="' + id + '">' + caption + '</label>';
        const field = label + '<textarea id="' + id + '" class="form-control form-control-sm" rows="1" maxlength="' + maxLength
            + '"' + (required ? ' required' : '') + '>' + escapeHtml(value) + '</textarea>';
        if (required) return '<div class="external-financial-field">' + field + '</div>';
        return '<details class="external-financial-note external-financial-note-panel"' + (value ? " open" : "") + '>'
            + '<summary>' + (value ? "ویرایش " : "افزودن ") + caption + '</summary>'
            + field + '</details>';
    }
    function noteSideStage(isCredit) {
        const side = isCredit ? "credit" : "debit";
        const title = isCredit ? "بستانکار" : "بدهکار";
        const typeKey = side + "Type";
        const accountKey = side + "AccountId";
        const detailKey = side + "DetailId";
        const detailOptions = {
            [noteParty.supplier]: source.options.vendors,
            [noteParty.customer]: source.options.customers,
            [noteParty.other]: source.options.otherDetails
        };
        const account = () => source.options.subsidiaryAccounts.find(item => item.id === draft[accountKey]);
        const sourceParty = isCredit ? ExternalFinancialWizard.crmCustomer(source.customerName)
            : source.beneficiary ? '<div class="external-mapping-entity"><span class="external-mapping-label">طرف تهاتر در ERP</span>'
                + '<strong class="external-mapping-entity-name">' + escapeHtml(source.beneficiary) + '</strong></div>' : "";
        const body = '<div class="d-grid gap-3">' + sourceParty
            + '<div class="d-grid gap-2">'
            + select(typeKey, "نوع " + title, source.options.partyTypes)
            + select(accountKey, "کد معین " + title, source.options.subsidiaryAccounts)
            + '<div class="deposit-note-detail" hidden></div></div>'
            + (isCredit ? "" : noteField("description", "شرح قلم", 250, true)) + '</div>';
        stage(title + " تهاتر", "نوع و حساب " + title + " را مشخص کنید", step(isCredit ? 1 : 2),
            '<i class="fas fa-user-tag" aria-hidden="true"></i>', body, isCredit ? "تأیید و ادامه" : "پیش‌نمایش", async () => {
                if (!draft[typeKey] || !account()) return toastifyError("نوع و حساب معین " + title + " را انتخاب کنید.");
                if (account().hasDl && !draft[detailKey]) return toastifyError("کد تفصیلی " + title + " را انتخاب کنید.");
                if (isCredit) {
                    if (draft[typeKey] === noteParty.customer && draft[detailKey]) {
                        const saved = await api("SaveCustomerMapping", draft);
                        if (!saved || signal.aborted) return;
                        draft.customerId = saved.id;
                    }
                    noteSideStage(false);
                    return;
                }
                capture(["description"]);
                if (!draft.description) return toastifyError("شرح قلم اعلامیه را وارد کنید.");
                const preview = await api("Preview", draft);
                if (preview && !signal.aborted) previewStage(preview);
            }, isCredit ? null : () => {
                capture(["description"]);
                noteSideStage(true);
            });
        enhance(typeKey, accountKey);
        const detailHost = wizard.container.querySelector(".deposit-note-detail");
        function updateDetail() {
            const requiresDetail = Boolean(account()?.hasDl && draft[typeKey]);
            detailHost.hidden = !requiresDetail;
            const detail = detailHost.querySelector("select");
            if (detail?.tomselect) {
                const control = detail.tomselect;
                controls.splice(controls.indexOf(control), 1);
                control.destroy();
            }
            if (!requiresDetail) {
                draft[detailKey] = 0;
                detailHost.replaceChildren();
                return;
            }
            if (isCredit && draft[typeKey] === noteParty.customer && !draft[detailKey]) {
                draft[detailKey] = draft.customerId;
            }
            detailHost.innerHTML = select(detailKey, "کد تفصیلی " + title, detailOptions[draft[typeKey]]);
            capture([detailKey]);
            enhance(detailKey);
            detailHost.querySelector("select").addEventListener("change", () => capture([detailKey]));
        }
        document.getElementById("deposit-field-" + typeKey).addEventListener("change", () => {
            capture([typeKey]);
            draft[detailKey] = 0;
            updateDetail();
        });
        document.getElementById("deposit-field-" + accountKey).addEventListener("change", () => {
            capture([accountKey]);
            updateDetail();
        });
        updateDetail();
    }
    function chequeStage() {
        const fields = ["reference", "dueText", "sayad"];
        if (source.needsOriginalChequeAmount) fields.push("chequeAmount");
        const body = '<div class="d-grid gap-2">'
            + input("reference", "شماره چک", 50, { required: true })
            + input("dueText", "سررسید چک", 10, { date: true, required: true })
            + input("sayad", "شناسه صیادی", 16)
            + (source.needsOriginalChequeAmount ? input("chequeAmount", "مبلغ اولیه چک (ریال)", 26, { required: true })
                + '<p class="small text-muted mb-0">مبلغ وصول در ERP ' + renderMoneyHtml(source.amount) + ' است.</p>' : "")
            + '</div>';
        stage("مشخصات چک", "شماره و سررسید چک را وارد کنید", step(3),
            '<i class="fas fa-file-invoice" aria-hidden="true"></i>', body, "تأیید و ادامه", () => {
                capture(fields);
                if (!draft.reference || !draft.dueText) return toastifyError("شماره و سررسید چک را وارد کنید.");
                if (source.needsOriginalChequeAmount) {
                    draft.chequeAmount = toEnglishDigits(draft.chequeAmount).replace(/[٬,]/g, "").replace("٫", ".");
                    if (!/^\d+(\.\d{1,4})?$/.test(draft.chequeAmount) || Number(draft.chequeAmount) <= 0)
                        return toastifyError("مبلغ اولیه چک را به ریال وارد کنید.");
                }
                if (draft.sayad && !/^[0-9]{16}$/.test(toEnglishDigits(draft.sayad)))
                    return toastifyError("شناسه صیادی باید ۱۶ رقم باشد.");
                chequeIssuerStage();
            }, () => {
                capture(fields);
                cashStage();
            });
    }
    function chequeIssuerStage() {
        const fields = ["description", "bankId", "accountNo", ...chequeExtraKeys];
        const body = '<div class="d-grid gap-2">'
            + receiptDescriptionField()
            + select("bankId", "بانک صادرکننده", source.options.banks)
            + input("accountNo", "شماره حساب صادرکننده", 50, { required: true })
            + chequeExtraFields("اطلاعات تکمیلی", "d-grid gap-2 mt-2") + '</div>';
        stage("اطلاعات رسید دریافت", "شرح رسید و حساب صادرکننده چک را وارد کنید", step(4),
            '<i class="fas fa-university" aria-hidden="true"></i>', body, "پیش‌نمایش", async () => {
                capture(fields);
                if (!draft.bankId || !draft.accountNo) return toastifyError("بانک و شماره حساب صادرکننده را وارد کنید.");
                if (!draft.description) return toastifyError("شرح رسید دریافت را وارد کنید.");
                const preview = await api("Preview", draft);
                if (preview) previewStage(preview);
            }, () => {
                capture(fields);
                chequeStage();
            });
        enhance("bankId");
    }
    function detailsStage() {
        const required = ["creditAccountId", "reference"];
        const fields = [...required, "description"];
        const body = '<div class="d-grid gap-2">'
            + creditAccountField("معین دریافت")
            + input("reference", "شماره پیگیری حواله", 100, { required: true })
            + receiptDescriptionField() + '</div>';
        stage("اطلاعات " + label(source.type), "اطلاعات لازم برای ثبت را تکمیل کنید", step(4),
            '<i class="fas fa-file-invoice" aria-hidden="true"></i>', body, "پیش‌نمایش", async () => {
                capture(fields);
                if (required.some(key => !draft[key]))
                    return toastifyError("اطلاعات الزامی سند را کامل کنید.");
                if (!draft.description) return toastifyError("شرح رسید دریافت را وارد کنید.");
                const preview = await api("Preview", draft);
                if (preview) previewStage(preview);
            }, () => {
                capture(fields);
                cashStage();
            });
        enhance("creditAccountId");
    }
    function renderNotePreview(preview) {
        const partyTitle = type => source.options.partyTypes.find(item => item.id === type)?.title;
        const side = (title, type, account, detail) => renderExternalFinancialPreviewTable(
            [title, "مقدار"], [["نوع", partyTitle(type)], ["معین", account], ["تفصیلی", detail]]);
        const detailsHtml = `<div class="deposit-note-sides">
            ${side("بدهکار", draft.debitType, preview.debitAccountTitle, preview.debitTitle)}
            ${side("بستانکار", draft.creditType, preview.creditTitle, preview.creditDetailTitle)}
        </div>
        ${draft.description ? `<p class="ex-preview-field mt-3 mb-0">
            <span class="ex-preview-label">شرح قلم</span><strong>${escapeHtml(draft.description)}</strong></p>` : ""}`;
        return renderExternalFinancialPreview({
            fields: [
                { label: "نوع واریزی", value: "تهاتری" },
                { label: "تاریخ", value: preview.dateText },
                { label: "نرم‌افزار مقصد", value: destinations[index].title, accent: true }
            ],
            detailsHtml,
            totals: [["مبلغ واریزی", preview.amount]]
        }) + '<div class="mt-3">' + noteField("noteDescription", "شرح اعلامیه", 4000) + '</div>';
    }
    function renderReceiptPreview(preview, cheque) {
        const customer = source.options.customers.find(item => item.id === draft.customerId);
        const fields = [
            { label: "نوع واریزی", value: cheque ? "چکی" : "حواله بانکی" },
            { label: "معین دریافت", value: preview.creditTitle },
            { label: "تاریخ", value: preview.dateText },
            { label: "مشتری", value: customer?.title ?? preview.creditDetailTitle, wide: true },
            { label: "نرم‌افزار مقصد", value: destinations[index].title, accent: true }
        ];
        const cash = source.options.cash.find(item => item.id === draft.cashId)?.title;
        let rows;
        if (cheque) {
            rows = [
                ["شماره چک", draft.reference], ["سررسید", preview.dueText], ["صندوق", cash],
                ["بانک صادرکننده", source.options.banks.find(bank => bank.id === draft.bankId)?.title],
                ["شماره حساب صادرکننده", draft.accountNo], ["شناسه صیادی", draft.sayad],
                [secondChequeNumberLabel, draft.secondNumber], ["صاحب چک", draft.chequeOwner],
                ["کد شعبه", draft.branchCode], ["نام شعبه", draft.branchTitle]
            ];
        } else {
            const bank = source.options.accounts.find(account => account.value === draft.bankAccountId);
            rows = [
                ["حساب بانکی مقصد", bank ? buildBankAccountUi(bank).displayText : preview.debitTitle],
                ["شماره حواله", draft.reference], ["صندوق", cash]
            ];
        }
        if (draft.description) rows.push(["شرح رسید دریافت", draft.description]);
        return renderExternalFinancialPreview({
            fields, columns: ["مشخصات سند", "مقدار"], rows: rows.filter(([, value]) => value),
            totals: preview.crmCollectedAmount == null ? [["مبلغ واریزی", preview.amount]] :
                [["مبلغ وصول در ERP", preview.crmCollectedAmount], ["مبلغ اولیه رسید دریافت", preview.amount]]
        });
    }
    function previewStage(preview) {
        draft.previewToken = preview.previewToken;
        const note = source.type === 4, cheque = source.type === 2;
        const body = make("div", null, note ? "deposit-note-preview" : "");
        body.innerHTML = (note ? renderNotePreview(preview) : renderReceiptPreview(preview, cheque))
            + voucherFootnote(preview.autoVoucher);
        stage("بررسی نهایی " + label(source.type), "اطلاعات سند را پیش از ثبت بررسی کنید", step(note ? 3 : 5),
            '<i class="fas fa-clipboard-check" aria-hidden="true"></i>', body, "تأیید و ثبت در نرم‌افزار", async () => {
                if (note) capture(["noteDescription"]);
                const saved = await api("Submit", draft, "POST", true);
                if (!saved) return;
                toastifySuccess(label(source.type) + " شماره " + saved.number + " ثبت شد.");
                if (++index < destinations.length) await prepare(false);
                else close();
            }, () => {
                if (note) {
                    capture(["noteDescription"]);
                    noteSideStage(false);
                }
                else if (cheque) chequeIssuerStage();
                else detailsStage();
            }, true);
    }

    function voucherFootnote(autoVoucher) {
        return '<p class="text-muted small mt-3 mb-0">' + (autoVoucher
            ? "با ثبت این سند، سند حسابداری نیز به‌صورت خودکار صادر می‌شود."
            : "صدور خودکار سند حسابداری در مقصد غیرفعال است.") + "</p>";
    }

    async function prepareChequeOperation(destination, actionId) {
        const request = { id, softwareId: destination.id, action: actionId };
        const prepared = await api("PrepareChequeOperation", request);
        if (!prepared || signal.aborted) return;
        destinations = [destination];
        index = 0;
        source = prepared;
        draft = { ...request, receiptId: prepared.receiptId, bankAccountId: prepared.bankId,
            dateText: actionId === chequeAction.assign ? "" : prepared.dateText,
            cashId: prepared.cashId ?? 0, payeeId: prepared.payeeId,
            paymentType: prepared.options.defaultPaymentType, accountId: prepared.options.defaultPayable };
        if (actionId === chequeAction.assign) {
            chequeAssignmentStage();
        } else if (actionId === chequeAction.spend) {
            chequePayeeStage();
        } else {
            if (source.collectedAmount !== source.amount) {
                const body = renderExternalFinancialPreview({
                    fields: [{ label: "شماره چک", value: source.chequeNumber }],
                    detailsHtml: '<p class="text-danger mb-0">برای ثبت وصول، اختلاف مبلغ چک با مبلغ وصول در ERP را برطرف کنید.</p>',
                    totals: [["مبلغ چک در سپیدار", source.amount], ["مبلغ وصول در ERP", source.collectedAmount]]
                });
                stage("اختلاف مبلغ چک", "", "", '<i class="fas fa-exclamation-circle" aria-hidden="true"></i>',
                    body, "بستن", close, null, true);
            } else {
                const preview = await api("PreviewChequeOperation", draft);
                if (!preview || signal.aborted) return;
                chequeOperationPreviewStage(preview);
            }
        }
        ModalToggle(wizard.modal, "show");
    }

    function chequeAssignmentStage() {
        const body = '<div class="d-grid gap-2">'
            + (source.bankAccountTxt ? '<div class="external-mapping-entity"><span class="external-mapping-label">حساب بانکی در ERP</span>'
                + '<strong>' + escapeHtml(source.bankAccountTxt) + '</strong></div>' : "")
            + select("bankAccountId", "حساب بانکی مقصد", []) + '</div>';
        stage("واگذاری به بانک", "حساب بانکی مقصد را مشخص کنید", "مرحله ۱ از ۲",
            '<i class="fas fa-university" aria-hidden="true"></i>', body, "پیش‌نمایش", async () => {
                capture(["bankAccountId"]);
                if (!draft.bankAccountId) return toastifyError("حساب بانکی مقصد را انتخاب کنید.");
                const preview = await api("PreviewChequeOperation", draft);
                if (!preview || signal.aborted) return;
                if (source.hasBankMappingSource && !await api("SaveBankMapping", draft)) return;
                if (!signal.aborted) chequeOperationPreviewStage(preview, chequeAssignmentStage);
            }, null);
        controls.push(createSupplierTomSelect(document.getElementById("deposit-field-bankAccountId"),
            source.options.bankAccounts, draft.bankAccountId || null, { showOfficial: false }));
    }

    function chequePayeeStage() {
        const fields = ["paymentType", "payeeId"];
        const paymentType = () => source.options.paymentTypes.find(item => item.id === draft.paymentType);
        const getPayees = () => source.options.payees.filter(item => item.paymentTypes.includes(draft.paymentType));
        const unsupported = "ثبت خرج کردن چک برای این نوع اعلامیه در این گردش پشتیبانی نمی‌شود.";
        const beneficiary = [source.beneficiary, source.payeeName].filter(Boolean).join(" / ");
        const body = '<div class="d-grid gap-2">'
            + (beneficiary ? '<div class="external-mapping-entity"><span class="external-mapping-label">طرف انتقال در ERP</span>'
                + '<strong>' + escapeHtml(beneficiary) + '</strong></div>' : "")
            + select("paymentType", "نوع اعلامیه پرداخت", source.options.paymentTypes)
            + select("payeeId", "طرف مقابل در نرم‌افزار مالی", getPayees())
            + '<p class="text-danger small mb-0" data-cheque-type-message role="status" hidden></p></div>';
        stage("طرف مقابل اعلامیه پرداخت", "نوع پرداخت و طرف مقابل را انتخاب کنید", "مرحله ۱ از ۳",
            '<i class="fas fa-users" aria-hidden="true"></i>', body, "تأیید و ادامه", () => {
                capture(fields);
                if (!draft.paymentType) return toastifyError("نوع پرداخت را انتخاب کنید.");
                if (!paymentType()?.canSpendCheque) return toastifyError(unsupported);
                if (!draft.payeeId) return toastifyError("طرف مقابل را انتخاب کنید.");
                chequePaymentDetailsStage();
            }, null);
        enhance(...fields);
        const type = document.getElementById("deposit-field-paymentType");
        const payee = document.getElementById("deposit-field-payeeId").tomselect;
        const message = wizard.container.querySelector("[data-cheque-type-message]");
        function updatePaymentTypeNotice() {
            const blocked = paymentType()?.canSpendCheque === false;
            message.textContent = blocked ? unsupported : "";
            message.hidden = !blocked;
            if (blocked) payee.disable();
            else payee.enable();
        }
        type.addEventListener("change", () => {
            capture(["paymentType", "payeeId"]);
            draft.accountId = draft.paymentType === source.options.defaultPaymentType ? source.options.defaultPayable : 0;
            const payees = getPayees();
            const selected = payees.some(item => item.id === draft.payeeId) ? draft.payeeId : 0;
            payee.clear(true);
            payee.clearOptions();
            payee.addOptions(payees.map(item => ({ value: String(item.id), text: item.title })));
            draft.payeeId = selected;
            if (selected) payee.setValue(String(selected), true);
            updatePaymentTypeNotice();
        });
        updatePaymentTypeNotice();
    }

    function chequePaymentDetailsStage() {
        const fields = ["accountId", "cashId", "dateText", "description"];
        const cash = source.cashId ? source.options.cash.filter(item => item.id === source.cashId) : source.options.cash;
        const body = '<div class="d-grid gap-2">'
            + select("accountId", "معین پرداخت", source.options.accounts)
            + select("cashId", "صندوق", cash)
            + input("dateText", "تاریخ اعلامیه پرداخت", 10, { date: true, required: true })
            + input("description", "شرح اعلامیه پرداخت", 250, { required: true }) + '</div>';
        stage("اطلاعات اعلامیه پرداخت", "معین، صندوق و تاریخ خرج کردن چک را مشخص کنید", "مرحله ۲ از ۳",
            '<i class="fas fa-file-invoice" aria-hidden="true"></i>', body, "پیش‌نمایش", async () => {
                capture(fields);
                if (!draft.accountId || !draft.cashId || !draft.dateText || !draft.description)
                    return toastifyError("معین، صندوق، تاریخ و شرح اعلامیه پرداخت را وارد کنید.");
                const preview = await api("PreviewChequeOperation", draft);
                if (preview && !signal.aborted) chequeOperationPreviewStage(preview, chequePaymentDetailsStage);
            }, () => {
                capture(fields);
                chequePayeeStage();
            });
        enhance("accountId", "cashId");
    }

    function chequeOperationPreviewStage(preview, previous = null) {
        draft.previewToken = preview.previewToken;
        const title = chequeActionTitle[draft.action];
        const payment = draft.action === chequeAction.spend;
        const assignment = draft.action === chequeAction.assign;
        const fields = [
            { label: "شماره چک", value: preview.number },
            { label: assignment ? "تاریخ واگذاری" : "تاریخ عملیات", value: preview.dateText },
            { label: "نرم‌افزار مقصد", value: destinations[index].title, accent: true }
        ];
        const rows = [
            [payment ? "طرف مقابل" : "حساب بانکی مقصد", preview.targetTitle]
        ];
        if (payment) rows.unshift(["نوع اعلامیه پرداخت", source.options.paymentTypes.find(item => item.id === draft.paymentType)?.title]);
        if (preview.debitTitle) rows.push([payment && !preview.autoVoucher ? "معین پرداخت" : "معین بدهکار", preview.debitTitle]);
        if (preview.creditTitle) rows.push(["معین بستانکار", preview.creditTitle]);
        if (payment) rows.push(["صندوق", source.options.cash.find(item => item.id === draft.cashId)?.title]);
        if (draft.description) rows.push(["شرح", draft.description]);
        const totals = [["مبلغ چک", preview.amount]];
        if (preview.collectedAmount != null) totals.unshift(["مبلغ وصول در ERP", preview.collectedAmount]);
        const body = make("div");
        body.innerHTML = renderExternalFinancialPreview({ fields, columns: ["مشخصات عملیات", "مقدار"], rows, totals })
            + voucherFootnote(preview.autoVoucher);
        let dateInput;
        if (assignment) {
            const dateField = body.querySelectorAll(".ex-preview-summary > .ex-preview-field")[1];
            const dateLabel = make("label", "تاریخ واگذاری", "ex-preview-label");
            dateLabel.htmlFor = "deposit-cheque-assignment-date";
            dateInput = make("input", null, "sdate ex-preview-invoice-number");
            Object.assign(dateInput, { id: dateLabel.htmlFor, type: "text", dir: "ltr", autocomplete: "off",
                maxLength: 10, required: true, value: draft.dateText || "" });
            dateField.replaceChildren(dateLabel, dateInput);
        }
        stage("بررسی نهایی " + title, "اطلاعات عملیات را پیش از ثبت بررسی کنید",
            payment ? "مرحله ۳ از ۳" : previous ? "مرحله ۲ از ۲" : "مرحله ۱ از ۱",
            '<i class="fas fa-clipboard-check" aria-hidden="true"></i>', body, "تأیید و ثبت در نرم‌افزار", async () => {
                if (dateInput) {
                    draft.dateText = toEnglishDigits(dateInput.value.trim());
                    if (!draft.dateText) {
                        dateInput.focus();
                        return toastifyError("تاریخ واگذاری را وارد کنید.");
                    }
                }
                const saved = await api("SubmitChequeOperation", draft, "POST", true);
                if (!saved) return;
                toastifySuccess(title + " شماره " + saved.number + " ثبت شد.");
                close();
            }, previous && (() => {
                if (dateInput) draft.dateText = dateInput.value.trim();
                previous();
            }), true);
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
    enableMomentumScrollScope(wizard.modal, { signal });
    records.replaceChildren();
    historyUi.show();
    await refresh();
}

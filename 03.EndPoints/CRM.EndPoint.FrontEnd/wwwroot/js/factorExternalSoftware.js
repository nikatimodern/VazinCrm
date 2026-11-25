let externalFactorHistory = { factorId: null, serialNumber: null };

function getFactorFinancialHistory() {
    return new ExternalFinancialHistory({
        modal: "externalSoftwareFactorsModal",
        list: "externalSoftwareFactors",
    });
}

async function showFactorExternalSoftwaresModal(factorId, serialNumber) {
    if (!factorId) return;

    externalFactorHistory = { factorId, serialNumber };
    const modal = document.getElementById("externalSoftwareFactorsModal");
    if (!modal) return;
    getFactorFinancialHistory().show({
        title: `مدیریت ارسال فاکتور ${serialNumber}`,
        loadingText: "در حال دریافت سوابق ثبت...",
    });
    await loadFactorExternalSoftwares(factorId);
}

async function loadFactorExternalSoftwares(factorId) {
    await sendApiRequest({
        url: "/Factor/GetExternalSoftwareFactors",
        params: { factorId },
        showLoading: false,
        onSuccess: result => {
            if (externalFactorHistory.factorId === factorId)
                renderExternalSoftwareFactors(result.data ?? []);
        },
    });
}

function renderExternalSoftwareFactors(items = []) {
    const history = getFactorFinancialHistory();
    const sourceId = externalFactorHistory.factorId;
    const ui = ExternalFinancialHistory;
    history.render(items, {
        emptyText: "این فاکتور هنوز در نرم‌افزار مالی ثبت نشده است.",
        card: item => ({
            title: item.externalSoftwareTitle,
            missing: item.isMissing,
            status: item.isMissing ? "فاکتور در نرم‌افزار مقصد پیدا نشد."
                : item.requiresSynchronization ? "اقلام یا مبالغ فاکتور با ERP مغایرت دارد." : "",
            fields: [
                ["شماره فاکتور", item.externalFactorNumber],
                ["مشتری", item.customerName],
            ],
            registration: { date: item.creationDate, by: item.registeredBy },
            view: !item.isMissing && item.viewUrl
                ? ui.action("مشاهده سند", { href: item.viewUrl, icon: "fa-eye" }) : null,
            operations: !item.isMissing && item.canMaintainDestination && item.mappingId
                ? factorDestinationOperations(history, { sourceId, externalSoftwareId: item.externalSoftwareId, mappingId: item.mappingId }, item.requiresSynchronization)
                : [],
            action: item.isMissing && item.mappingId && item.canDeleteMissing
                ? ui.missingMappingAction({
                    modal: history.modal,
                    sourceId,
                    softwareId: item.externalSoftwareId,
                    mappingId: item.mappingId,
                    softwareTitle: item.externalSoftwareTitle,
                    url: "/Factor/DeleteMissingExternalFactorMapping",
                    onDeleted: () => loadFactorExternalSoftwares(sourceId),
                }) : !item.isMissing && item.canMaintainDestination && item.requiresSynchronization && item.mappingId
                ? ui.action("همگام‌سازی با ERP", { icon: "fa-sync-alt", onClick: event => openFactorDestinationSync(
                    event.currentTarget, { sourceId, externalSoftwareId: item.externalSoftwareId, mappingId: item.mappingId }, history.modal) })
                : null,
        }),
    });
}

function factorDestinationOperations(history, key, requiresSynchronization) {
    const ui = ExternalFinancialHistory;
    const edit = ui.action("ویرایش انتقال", { icon: "fa-pencil-alt",
        onClick: event => requiresSynchronization ? toastifyError("ابتدا فاکتور را با ERP همگام‌سازی کنید.")
            : openFactorDestinationEdit(event.currentTarget, key, history.modal) });
    const remove = ui.action("حذف از مقصد", { icon: "fa-trash-alt", danger: true,
        onClick: event => deleteFactorDestination(event.currentTarget, key, history.modal) });
    return [edit, remove];
}

async function openFactorDestinationSync(button, key, historyModal) {
    const modal = document.getElementById("factorDestinationSyncModal");
    if (!modal || modal.dataset.active || button.disabled) return;
    modal.dataset.active = "true";
    button.disabled = true;
    const release = () => {
        delete modal.dataset.active;
        button.disabled = false;
        if (button.isConnected && isModalOpen(historyModal)) button.focus();
    };
    let opened = false;
    try {
        const data = await sendApiRequest({ url: "/Factor/GetExternalFactorSync", params: key });
        if (!data?.preview || !button.isConnected || !isModalOpen(historyModal)) return;
        renderFactorDestinationSync(modal, key, data);
        modal.addEventListener("hidden.bs.modal", release, { once: true });
        ModalToggle(modal, "show");
        opened = true;
    } finally {
        if (!opened) release();
    }
}

function renderFactorDestinationSync(modal, key, { preview, items, settings }) {
    const wizard = new ExternalFinancialWizard({ modal, container: "factorDestinationSyncBody",
        back: "factorDestinationSyncBack", confirm: "factorDestinationSyncSave" });
    const { impact } = preview;
    const element = ExternalFinancialHistory.element;
    const options = items.map(item => ({ value: item.id, label: [item.code, item.title].filter(Boolean).join(" - ") }));
    const labels = new Map(options.map(item => [item.value, item.label]));
    const cache = new Map();
    const rows = preview.sourceRows.map(row => ({ row, itemId: labels.has(row.suggestedItemId) ? row.suggestedItemId : 0,
        stockId: row.suggestedStockId, stockTitle: row.stockTitle, loading: false }));
    const close = modal.querySelector(".btn-close");
    const hide = () => ModalToggle(modal, "hide");
    const scroll = enableMomentumScrollScope(modal);
    let saving = false;
    const guard = event => { if (saving) event.preventDefault(); };
    modal.addEventListener("hide.bs.modal", guard);
    modal.addEventListener("hidden.bs.modal", () => {
        modal.removeEventListener("hide.bs.modal", guard);
        scroll();
        wizard.destroy();
        wizard.container.replaceChildren();
        wizard.confirm.onclick = wizard.back.onclick = null;
    }, { once: true });

    const stage = (title, subtitle, step, content) => wizard.render({
        destination: impact.softwareTitle, position: "فاکتور شماره " + impact.number, large: true, title, subtitle, step,
        icon: '<i class="fas fa-sync-alt" aria-hidden="true"></i>', content,
    });
    if (impact.blockers.length) {
        const content = element("ul", "ps-4 mb-0");
        for (const blocker of impact.blockers) content.append(element("li", null, blocker));
        stage("همگام‌سازی فاکتور", "همگام‌سازی این فاکتور امکان‌پذیر نیست.", "", content);
        wizard.setActions({ confirmText: "بستن", confirmDisabled: false, backHidden: true });
        wizard.confirm.onclick = hide;
        return;
    }

    function renderItems() {
        const { content, toolbar, list } = createExternalItemMatching();
        toolbar.remove();
        const update = () => wizard.setActions({ confirmDisabled: rows.some(x => x.loading || !x.itemId || !x.stockId) });
        const targets = rows.map(({ row, itemId }, index) => {
            const target = appendExternalItemMatchRow(list, row.productTitle, row.quantity, row.unitTxt);
            target.innerHTML = ExternalFinancialWizard.selectField({
                id: "factorSyncItem_" + index, label: "کالای معادل در نرم‌افزار مالی", options, selected: itemId,
                placeholder: "انتخاب کالا", required: true,
            });
            return target;
        });
        stage("تطبیق کالاها و انبارها", "کالا و انبار مقصد را برای هر قلم بررسی کنید.", "مرحله ۱ از ۲", content);
        wizard.setActions({ confirmText: "بررسی تغییرات", backText: "انصراف", backHidden: false, backDisabled: false });
        wizard.confirm.onclick = renderPreview;
        wizard.back.onclick = hide;
        for (const [index, state] of rows.entries()) {
            const target = targets[index];
            const item = target.querySelector("select");
            const stock = ExternalFinancialWizard.linkedStockField(target, {
                id: "factorSyncStock_" + index, softwareId: key.externalSoftwareId, cache, required: true,
                isCurrent: () => content.isConnected, onChange: readStock,
                onLoading: loading => { state.loading = loading; update(); },
            });
            function readStock() {
                state.stockId = Number(stock.select.value);
                state.stockTitle = stock.select.selectedOptions[0]?.textContent ?? "";
                update();
            }
            async function load(preferred) {
                await stock.update(state.itemId, preferred);
                if (content.isConnected) readStock();
            }
            new TomSelect(item, { create: false, placeholder: "جستجو و انتخاب کالا", onChange: value => {
                state.itemId = Number(value);
                state.stockId = 0;
                void load("");
            } });
            void load(state.stockId);
        }
        update();
    }

    function renderPreview() {
        if (wizard.confirm.disabled) return;
        const content = element("div", "external-prefactor-preview d-grid gap-3");
        const format = value => Number(value).toLocaleString("fa-IR", { maximumFractionDigits: 4 });
        const columns = ["کالا", "انبار", "مقدار", "قیمت واحد", "مبلغ", "تخفیف", "مالیات"];
        const values = (row, item, stock) => [item || "—", stock || "—",
            ...[row.quantity, row.fee, row.price, row.discount, row.tax].map(format)];
        const section = (title, headings, data) => {
            const node = element("section");
            node.append(element("h6", "fbold mb-2 external-pf-software", title));
            node.insertAdjacentHTML("beforeend", renderExternalFinancialPreviewTable(headings, data));
            content.append(node);
        };
        section("اقلام فعلی سپیدار", columns, preview.destinationRows.map(row => values(row, row.itemTitle, row.stockTitle)));
        section("اقلام جایگزین از ERP", columns, rows.map(state => values(state.row, labels.get(state.itemId), state.stockTitle)));
        const totals = [["جمع مبلغ کالاها", "price"], ["تخفیف کل", "discount"], ["مالیات کل", "tax"], ["مبلغ نهایی", "total"]];
        section("مقایسه مبالغ (ریال)", ["عنوان", "سپیدار", "ERP"], totals.map(([label, field]) =>
            [label, format(preview.destinationAmounts[field]), format(preview.sourceAmounts[field])]));
        const notes = element("ul", "small ps-4 mb-0");
        for (const number of impact.voucherNumbers)
            notes.append(element("li", null, "سند حسابداری شماره " + number + (settings.autoVoucher ? " بازسازی می‌شود." : " حذف می‌شود.")));
        for (const delivery of impact.deliveries)
            notes.append(element("li", null, `خروج انبار شماره ${delivery.number} (انبار ${delivery.stockCode}) ${settings.autoDelivery ? "بازسازی می‌شود." : "حذف می‌شود."}`));
        if (!impact.voucherNumbers.length && settings.autoVoucher)
            notes.append(element("li", null, "سند حسابداری صادر می‌شود."));
        if (!impact.deliveries.length && settings.autoDelivery)
            notes.append(element("li", null, "خروج انبار صادر می‌شود."));
        if (preview.isQuotationBased) {
            for (const { row, itemId, stockId } of rows) {
                if (!row.quotationItemId)
                    notes.append(element("li", null, `قلم «${row.productTitle}» بدون ارتباط با پیش‌فاکتور ثبت می‌شود.`));
                else if (itemId !== row.suggestedItemId || stockId !== row.suggestedStockId)
                    notes.append(element("li", null, `ارتباط قلم «${row.productTitle}» با پیش‌فاکتور قطع می‌شود.`));
            }
        }
        if (notes.childElementCount) content.append(notes);
        stage("بررسی نهایی همگام‌سازی", "تغییرات روی همان فاکتور سپیدار اعمال می‌شوند.", "مرحله ۲ از ۲", content);
        wizard.setActions({ confirmText: "تأیید و همگام‌سازی", backText: "برگشت", confirmDisabled: false });
        wizard.back.onclick = renderItems;
        wizard.confirm.onclick = async () => {
            if (saving) return;
            saving = close.disabled = true;
            wizard.setActions({ confirmDisabled: true, backDisabled: true });
            let result;
            try {
                result = await sendApiRequest({
                    url: "/Factor/SynchronizeExternalFactor", method: "POST", successMsg: "فاکتور فروش همگام شد.",
                    headers: ExternalFinancialWizard.antiforgeryHeaders(modal),
                    data: { key, token: impact.token, ...settings,
                        items: rows.map(({ row, itemId, stockId }) => ({ sourceRowId: row.id, itemId, stockId })) },
                });
            } finally {
                saving = close.disabled = false;
                wizard.setActions({ confirmDisabled: false, backDisabled: false });
            }
            if (!result?.saved) return;
            hide();
            if (externalFactorHistory.factorId === key.sourceId && isModalOpen("externalSoftwareFactorsModal"))
                await loadFactorExternalSoftwares(key.sourceId);
        };
    }
    renderItems();
}

async function openFactorDestinationEdit(button, key, historyModal) {
    const wizard = getFactorFinancialWizard();
    if (button.disabled || externalFactorExportState || isModalOpen(wizard.modal)) return;
    button.disabled = true;
    const lifetime = new AbortController();
    const { signal } = lifetime;
    let opened = false, busy = false, closing = false;
    try {
        const data = await sendApiRequest({ url: "/Factor/GetExternalFactorEdit", params: key, showLoading: false });
        if (!data || !button.isConnected || !isModalOpen(historyModal) || externalFactorExportState || isModalOpen(wizard.modal)) return;
        const ui = ExternalFinancialHistory;
        const { impact } = data;
        const locked = impact.blockers.length > 0;
        const direct = data.quotationId == null;
        const { content, toolbar, list } = createExternalItemMatching();
        toolbar.remove();
        const general = ui.element("div", "d-grid gap-2");
        content.prepend(general);

        const field = (parent, caption, control, id) => {
            const group = ui.element("div");
            const label = ui.element("label", "form-label mb-0", caption);
            control.id = id;
            label.htmlFor = id;
            group.append(label, control);
            parent.append(group);
            return control;
        };
        const options = (control, choices, selected, fallback) => {
            control.append(new Option("", ""));
            for (const option of choices)
                control.append(new Option([option.code, option.title].filter(Boolean).join(" - "), option.id));
            if (!choices.some(option => option.id === selected))
                control.append(new Option(fallback || String(selected), selected));
            control.value = String(selected);
            return control;
        };
        const invoiceFields = ui.element("div", "row row-cols-2 gx-2 mb-2");
        general.append(invoiceFields);
        const number = field(invoiceFields, "شماره فاکتور", ui.element("input", "form-control form-control-sm"), "editFactorNumber");
        number.inputMode = "numeric";
        number.dir = "ltr";
        number.value = String(impact.number);
        number.disabled = locked;
        const date = field(invoiceFields, "تاریخ فاکتور", ui.element("input", "sdate form-control form-control-sm"), "editFactorDate");
        date.dir = "ltr";
        date.autocomplete = "off";
        date.maxLength = 10;
        date.value = data.dateText;
        date.disabled = locked;
        date.setAttribute("data-datepicker-change", "");
        const customer = direct && !locked ? options(field(general, "مشتری مقصد",
            ui.element("select", "show-dropdown-arrow"), "editFactorCustomer"), data.customers, data.customerId, data.customerName) : null;
        const saleType = direct && !locked ? options(field(general, "نوع فروش",
            ui.element("select", "show-dropdown-arrow"), "editFactorSaleType"), data.saleTypes, data.saleTypeId) : null;
        const brokerHost = ui.element("div");
        general.append(brokerHost);
        let includeBroker = data.includeBroker, broker = null, brokerPending = false;
        const renderBroker = () => {
            brokerHost.innerHTML = renderFactorBroker({
                id: "editFactorBroker", salespersonName: data.salespersonName, includeBroker, broker,
                disabled: locked || busy || brokerPending,
            });
        };
        renderBroker();
        const rows = data.rows.map(row => {
            const target = appendExternalItemEditRow(list, row);
            if (locked) {
                target.append(ui.element("p", "small mb-0", row.stockTitle));
                return { row };
            }
            const item = options(field(target, "کالای معادل", ui.element("select", "show-dropdown-arrow"),
                "editFactorItem_" + row.id), data.items, row.itemId, row.itemTitle);
            return { row, target, item, stock: null };
        });

        const getDate = () => toEnglishDigits(date.value.trim());
        const getNumber = () => Number(toEnglishDigits(number.value.trim()));
        const validNumber = () => /^[0-9]+$/.test(toEnglishDigits(number.value.trim())) &&
            getNumber() > 0 && getNumber() <= 2147483647;
        const ready = () => !locked && !busy && !brokerPending && (!includeBroker || broker != null) &&
            validNumber() && /^[0-9]{4}\/[0-9]{2}\/[0-9]{2}$/.test(getDate()) &&
            (!direct || (customer?.value && saleType?.value)) && rows.length > 0 &&
            rows.every(x => x.item.value && x.stock?.select.value && !x.stock.select.disabled);
        const update = () => wizard.setActions({ backDisabled: busy, confirmDisabled: !ready() });
        const checkBroker = async (keepSelection = false) => {
            brokerPending = true;
            renderBroker();
            update();
            try {
                const result = await sendApiRequest({
                    url: "/Factor/GetExternalFactorEditBroker", params: { ...key, token: impact.token },
                    showLoading: false, successMsg: null, signal,
                });
                if (signal.aborted) return;
                broker = result?.broker ?? null;
                if (!broker && !keepSelection) includeBroker = false;
            } catch {
                if (signal.aborted) return;
                if (!keepSelection) includeBroker = false;
                toastifyError("دریافت اطلاعات واسط امکان‌پذیر نیست.");
            } finally {
                brokerPending = false;
                if (!signal.aborted) {
                    renderBroker();
                    update();
                }
            }
        };
        brokerHost.addEventListener("change", event => {
            if (event.target.id !== "editFactorBroker" || busy || brokerPending) return;
            includeBroker = event.target.checked;
            broker = null;
            if (includeBroker) void checkBroker();
            else {
                renderBroker();
                update();
            }
        }, { signal });
        wizard.render({
            destination: impact.softwareTitle, position: "نرم‌افزار ۱ از ۱",
            title: "ویرایش انتقال فاکتور فروش",
            subtitle: locked ? impact.blockers.join(" ") : "اطلاعات انتقال فاکتور را در همان سال مالی بررسی کنید.",
            icon: '<i class="fas fa-pencil-alt" aria-hidden="true"></i>', step: "فاکتور شماره " + impact.number,
            content, large: true,
        });
        wizard.setActions({ backHidden: false, backText: locked ? "بستن" : "انصراف", confirmText: "ذخیره تغییرات" });
        content.addEventListener("input", update, { signal });
        content.addEventListener("change", update, { signal });
        wizard.back.addEventListener("click", () => ModalToggle(wizard.modal, "hide"), { signal });
        wizard.modal.addEventListener("hide.bs.modal", event => {
            if (busy && !closing) event.preventDefault();
        }, { signal });
        wizard.modal.addEventListener("hidden.bs.modal", () => {
            disposeDateTimePickers(wizard.container);
            lifetime.abort();
            button.disabled = false;
        }, { once: true, signal });
        historyModal.addEventListener("hidden.bs.modal", () => {
            closing = true;
            if (isModalOpen(wizard.modal)) ModalToggle(wizard.modal, "hide");
            lifetime.abort();
            button.disabled = false;
        }, { once: true, signal });

        if (!locked) {
            for (const select of [customer, saleType].filter(Boolean))
                new TomSelect(select, { placeholder: "جستجو و انتخاب", onChange: update });
            const cache = new Map();
            for (const entry of rows) {
                const { row, target, item } = entry;
                entry.stock = ExternalFinancialWizard.linkedStockField(target, {
                    id: "editFactorStock_" + row.id, softwareId: key.externalSoftwareId, cache,
                    retainedStock: { itemId: row.itemId, stockId: row.stockId, title: row.stockTitle },
                    isCurrent: () => !signal.aborted && content.isConnected,
                    onChange: update, onLoading: update,
                });
                const loadStock = async value => {
                    await entry.stock.update(value, Number(value) === row.itemId ? row.stockId : "");
                    if (!signal.aborted) update();
                };
                new TomSelect(item, { placeholder: "جستجو و انتخاب کالا", onChange: loadStock });
                void loadStock(row.itemId);
            }
            dateTimePicker(wizard.container);
        }

        wizard.confirm.addEventListener("click", async () => {
            if (!ready()) return;
            busy = true;
            renderBroker();
            update();
            try {
                const items = rows.map(({ row, item, stock }) => ({
                    id: row.id, itemId: Number(item.value), stockId: Number(stock.select.value),
                }));
                const details = getSalesEditImpactDetails(impact, data.autoVoucher, data.autoDelivery);
                if (rows.some(({ row }, i) => row.linkedToQuotation &&
                    (row.itemId !== items[i].itemId || row.stockId !== items[i].stockId)))
                    details.push("ارتباط ردیف‌هایی که کالا یا انبارشان تغییر کرده با اقلام پیش‌فاکتور قطع می‌شود.");
                if (!await ui.confirmEdit(wizard.modal, "تغییرات فاکتور فروش شماره " + impact.number + " ذخیره شود؟", details)
                    || signal.aborted) return;
                const saved = await sendApiRequest({
                    url: "/Factor/EditExternalFactor", method: "POST", showLoading: false, signal,
                    headers: ExternalFinancialWizard.antiforgeryHeaders(wizard.modal),
                    data: {
                        key, token: impact.token, dateText: getDate(), number: getNumber(), includeBroker, items,
                        previewBrokerPartyId: broker?.partyId ?? null,
                        autoVoucher: data.autoVoucher, autoDelivery: data.autoDelivery,
                        ...(direct && { customerId: Number(customer.value), saleTypeId: Number(saleType.value) }),
                    },
                });
                if (!saved?.saved) return;
                toastifySuccess("تغییرات فاکتور فروش در نرم‌افزار مقصد ذخیره شد.");
                closing = true;
                ModalToggle(wizard.modal, "hide");
                if (externalFactorHistory.factorId === key.sourceId)
                    await loadFactorExternalSoftwares(key.sourceId);
            } finally {
                busy = false;
                if (!signal.aborted) {
                    renderBroker();
                    update();
                }
            }
        }, { signal });
        update();
        ModalToggle(wizard.modal, "show");
        opened = true;
        if (!locked && includeBroker) void checkBroker(true);
    } finally {
        if (!opened) {
            lifetime.abort();
            button.disabled = false;
        }
    }
}

function getSalesEditImpactDetails(impact, autoVoucher, autoDelivery) {
    const vouchers = impact.voucherNumbers.map(number => "سند حسابداری شماره " + number);
    const deliveries = impact.deliveries.map(row => `خروج انبار شماره ${row.number} (انبار ${row.stockCode})`);
    const rebuild = [...(autoVoucher ? vouchers : []), ...(autoDelivery ? deliveries : [])];
    const remove = [...(!autoVoucher ? vouchers : []), ...(!autoDelivery ? deliveries : [])];
    const create = [autoVoucher && !vouchers.length ? "سند حسابداری" : "",
        autoDelivery && !deliveries.length ? "خروج انبار" : ""].filter(Boolean);
    const details = [];
    if (rebuild.length) details.push(rebuild.join(" و ") + (rebuild.length === 1 ? " بازسازی می‌شود." : " بازسازی می‌شوند."));
    if (remove.length) details.push(remove.join(" و ") + (remove.length === 1 ? " حذف می‌شود." : " حذف می‌شوند."));
    if (create.length) details.push(create.join(" و ") + (create.length === 1 ? " صادر می‌شود." : " صادر می‌شوند."));
    if (deliveries.length || autoDelivery)
        details[details.length - 1] = details.at(-1).slice(0, -1) + " و موجودی انبار به‌روز می‌شود.";
    return details;
}

async function deleteFactorDestination(button, key, historyModal) {
    if (button.disabled) return;
    button.disabled = true;
    try {
        const plan = await sendApiRequest({ url: "/Factor/PreviewExternalFactorDeletion", params: key, showLoading: false });
        if (!plan || !button.isConnected || !isModalOpen(historyModal)) return;

        const ui = ExternalFinancialHistory;
        const content = ui.element("div", "text-start");
        const blocked = plan.blockers.length > 0;
        content.append(ui.element("p", "mb-2", blocked
            ? `فاکتور شماره ${plan.number} در «${plan.softwareTitle}» قابل حذف نیست.`
            : `فاکتور فروش شماره ${plan.number} از «${plan.softwareTitle}» حذف شود؟`));
        const entries = blocked ? plan.blockers : [
            ...plan.voucherNumbers.map(number => `سند حسابداری فروش شماره ${number}`),
            ...plan.deliveries.map(item => `خروج انبار شماره ${item.number} (انبار ${item.stockCode})`),
        ];
        if (entries.length) {
            if (!blocked) content.append(ui.element("p", "mb-1 small", "این اسناد هم همراه فاکتور حذف می‌شوند:"));
            const list = ui.element("ul", "mb-2 small");
            for (const entry of entries) list.append(ui.element("li", null, entry));
            content.append(list);
        }
        if (!blocked && plan.deliveries.length)
            content.append(ui.element("p", "mb-0 small", "موجودی انبار پس از حذف به‌روزرسانی می‌شود."));

        const answer = await Swal.fire({
            target: historyModal, title: "حذف فاکتور فروش از مقصد", html: content, icon: "warning",
            showCancelButton: !blocked, focusCancel: true,
            confirmButtonText: blocked ? "بستن" : "حذف فاکتور", cancelButtonText: "انصراف",
        });
        if (blocked || !answer.isConfirmed || !button.isConnected || !isModalOpen(historyModal)) return;
        try {
            await sendApiRequest({
                url: "/Factor/DeleteExternalFactor", method: "POST", showLoading: false,
                headers: ExternalFinancialWizard.antiforgeryHeaders(historyModal), data: { key, token: plan.token },
                successMsg: "فاکتور فروش از نرم‌افزار مقصد حذف شد.",
            });
        } finally {
            if (externalFactorHistory.factorId === key.sourceId) await loadFactorExternalSoftwares(key.sourceId);
        }
    } finally { button.disabled = false; }
}

document.addEventListener("click", async event => {
    if (!event.target.closest("#sendExternalFactorBtn")) return;
    if (!externalFactorHistory.factorId) return;

    if (isModalOpen("sendFactorToExternalSoftwareModal") ||
        isModalOpen("chooseExternalSoftwareModal")) return;
    // Keep the history dialog open below the destination chooser and export wizard.
    await sendFactorToExternalSoftware(externalFactorHistory.factorId);
});

let externalFactorExportState = null;
let factorFinancialWizard = null;
function getFactorFinancialWizard() {
    if (!factorFinancialWizard?.modal.isConnected) {
        factorFinancialWizard = new ExternalFinancialWizard({
            modal: "sendFactorToExternalSoftwareModal",
            container: "externalFactorPreviewContent",
            back: "backExternalFactorStepBtn",
            confirm: "confirmSendExternalFactorBtn",
        });
    }
    return factorFinancialWizard;
}

/** Start a single destination without losing the user's remaining selected software IDs. */
async function prepareFactorDestination(state) {
    const result = await sendApiRequest({
        url: "/Factor/PrepareExternalFactorModes",
        method: "POST",
        data: state.request,
        successMsg: null,
    });
    if (!result?.softwareTitle || externalFactorExportState !== state) return false;
    state.softwareTitle = result.softwareTitle;
    state.hasQuotation = result.hasQuotation === true;
    factorDirectWizard.showMode(state);
    return true;
}

function resetFactorDestination(state, softwareId) {
    state.request = {
        factorId: state.request.factorId,
        externalSoftwareId: softwareId,
        usePreFactor: true,
        includeBroker: false,
        previewBrokerPartyId: null,
    };
    state.direct = { saleTypeId: "", customerId: "", items: {} };
    state.quotation = null;
    state.saleTypes = null;
    state.customerData = null;
    state.items = null;
    state.sepidarItems = null;
    state.stocksByItem = null;
    state.stockCache = null;
    state.stockPending = null;
    state.hasLoadedPreview = false;
    state.step = "mode";
    state.sourceChoice = null;
}

async function continueFactorDestinations(state) {
    const wizard = getFactorFinancialWizard();
    if (!wizard.hasNextDestination) return false;
    resetFactorDestination(state, wizard.nextDestination());

    const loaded = await prepareFactorDestination(state);
    if (!loaded && externalFactorExportState === state) {
        state.step = "retryDestination";
        wizard.render({
            destination: "نرم‌افزار مالی",
            title: "دریافت اطلاعات مقصد",
            subtitle: "اطلاعات نرم‌افزار بعدی دریافت نشد",
            icon: '<i class="fas fa-exclamation-circle" aria-hidden="true"></i>',
            step: "ادامه انتقال",
            content: '<p class="mb-0">برای ادامه ارسال فاکتور به نرم‌افزار بعدی، دوباره تلاش کنید.</p>',
        });
        wizard.setActions({
            confirmText: "تلاش دوباره", confirmDisabled: false,
            backHidden: true, backDisabled: true,
        });
    }
    return true;
}

async function loadExternalFactorPreview(state, includeBroker) {
    const button = document.getElementById("confirmSendExternalFactorBtn");
    const previousInvoiceNumber = state.hasLoadedPreview
        ? document.getElementById("externalFactorInvoiceNumber")?.value
        : null;
    const dateInput = document.getElementById("externalFactorInvoiceDate");
    if (state.hasLoadedPreview && dateInput)
        state.request.invoiceDateText = toEnglishDigits(dateInput.value.trim());
    state.processing = true;
    toggleElementsDisabled(button, true);

    try {
        const result = await sendApiRequest({
            url: "/Factor/PrepareExternalFactor",
            method: "POST",
            data: { ...state.request, includeBroker, previewBrokerPartyId: null },
            successMsg: null,
            showLoading: false,
        });
        if (!result?.preview?.items?.length || externalFactorExportState !== state) return false;
        if (includeBroker && (!result.broker || !Number.isInteger(Number(result.broker.partyId)))) {
            toastifyError("اطلاعات واسط فروش در پیش‌نمایش معتبر نیست.");
            return false;
        }

        state.request.includeBroker = includeBroker;
        state.request.previewBrokerPartyId = includeBroker ? Number(result.broker.partyId) : null;
        state.request.previewToken = result.previewToken;
        state.request.invoiceDateText = result.preview.invoiceDateText;
        state.hasLoadedPreview = true;
        state.step = "preview";
        getFactorFinancialWizard().render({
            destination: result.softwareTitle,
            title: "بررسی نهایی فاکتور",
            subtitle: state.request.usePreFactor
                ? "پیش‌فاکتور سپیدار شماره " + result.preview.quotationNumber + "؛ اطلاعات را بررسی کنید"
                : "صدور مستقیم بدون پیش‌فاکتور سپیدار؛ اطلاعات را بررسی کنید",
            icon: '<i class="fas fa-clipboard-check" aria-hidden="true"></i>',
            step: state.request.usePreFactor ? "مرحله ۲ از ۲" : "مرحله ۴ از ۴",
            content: renderExternalFactorPreview(result, state),
            large: true,
        });
        getFactorFinancialWizard().setActions({
            confirmText: "تأیید و ثبت در نرم‌افزار",
            confirmDisabled: false,
            backHidden: false,
            backDisabled: false,
        });
        if (previousInvoiceNumber != null) {
            document.getElementById("externalFactorInvoiceNumber").value = previousInvoiceNumber;
        }
        const invoiceDateInput = document.getElementById("externalFactorInvoiceDate");
        invoiceDateInput.setAttribute("data-datepicker-change", "");
        dateTimePicker(document.getElementById("sendFactorToExternalSoftwareModal"));
        return true;
    } finally {
        if (externalFactorExportState === state) {
            state.processing = false;
            toggleElementsDisabled(button, false);
            const checkbox = document.getElementById("externalFactorIncludeBroker");
            if (checkbox) checkbox.disabled = false;
        }
    }
}

async function sendFactorToExternalSoftware(factorId) {
    if (!factorId) return;
    const selected = await ExternalFinancialHistory.chooseDestinations({
        ids: factorId, mapType: 3,
    });
    if (!selected?.length) return;

    const wizard = getFactorFinancialWizard();
    wizard.setDestinations(selected);
    if (!wizard.currentDestination) return;
    const state = {
        request: {
            factorId,
            externalSoftwareId: wizard.currentDestination,
            usePreFactor: true,
            includeBroker: false,
            previewBrokerPartyId: null,
        },
        direct: { saleTypeId: "", customerId: "", items: {} },
        processing: false,
        hasLoadedPreview: false,
        completedCount: 0,
        finished: false,
        step: "mode",
    };
    externalFactorExportState = state;
    if (!await prepareFactorDestination(state)) {
        if (externalFactorExportState === state) externalFactorExportState = null;
        return;
    }
    ModalToggle("sendFactorToExternalSoftwareModal", "show");
}

document.addEventListener("change", async event => {
    if (event.target.id !== "externalFactorIncludeBroker") return;
    const state = externalFactorExportState;
    if (!state || state.processing) return;

    const checkbox = event.target;
    checkbox.disabled = true;
    try {
        const loaded = await loadExternalFactorPreview(state, checkbox.checked);
        if (!loaded && externalFactorExportState === state)
            checkbox.checked = state.request.includeBroker;
    } catch {
        if (externalFactorExportState === state) checkbox.checked = state.request.includeBroker;
        toastifyError("دریافت اطلاعات واسط امکان‌پذیر نیست.");
    }
});

function renderFactorBroker({ id, salespersonName, includeBroker, broker, disabled = false }) {
    const escape = value => escapeHtml(String(value ?? "-"));
    const number = value => Number(value ?? 0).toLocaleString("fa-IR", { maximumFractionDigits: 4 });
    return `
    <section class="external-factor-broker" aria-label="تنظیمات واسط فروش">
        <label for="${escape(id)}" class="external-factor-broker-toggle ${includeBroker ? "is-active" : ""}">
            <input id="${escape(id)}" type="checkbox" role="switch"
                   class="external-factor-broker-input" ${includeBroker ? "checked" : ""} ${disabled ? "disabled" : ""}>
            <span class="external-factor-broker-switch" aria-hidden="true"></span>
            <span class="external-factor-broker-toggle-text">
                <span class="external-factor-broker-title">ثبت واسط برای این فاکتور</span>
                <span class="external-factor-broker-seller">
                    کارشناس فروش: <strong>${escape(salespersonName || "نامشخص")}</strong>
                </span>
            </span>
            <i class="fas fa-user-tie external-factor-broker-icon" aria-hidden="true"></i>
        </label>
        ${includeBroker && broker ? `
            <div class="external-factor-broker-details">
                <div class="external-factor-broker-detail">
                    <span>واسط سپیدار</span>
                    <strong>${escape(broker.fullName)}</strong>
                </div>
                <div class="external-factor-broker-detail">
                    <span>نرخ پورسانت</span>
                    <strong>${number(broker.commissionRate)}٪</strong>
                </div>
                <div class="external-factor-broker-detail">
                    <span>مبلغ پورسانت</span>
                    <strong>${number(broker.commission)} ریال</strong>
                </div>
            </div>
        ` : ""}
    </section>`;
}

function renderExternalFactorPreview(data, state) {
    const preview = data.preview;
    const escape = value => escapeHtml(String(value ?? "-"));
    const number = value => Number(value ?? 0).toLocaleString("fa-IR", {
        maximumFractionDigits: 4,
    });
    const money = value => `${number(value)} ریال`;

    const date = preview.invoiceDateText || preview.invoiceDate?.split("T")[0] || "-";
    return `
        <div class="external-prefactor-preview">
            <div class="ex-preview-summary">
                <div class="ex-preview-field">
                    <span class="ex-preview-label">نوع فروش:</span>
                    <strong>${escape(preview.saleTypeTitle)}</strong>
                </div>
                <div class="ex-preview-field">
                    <label class="ex-preview-label" for="externalFactorInvoiceNumber">شماره فاکتور:</label>
                    <input id="externalFactorInvoiceNumber" type="text" inputmode="numeric"
                           autocomplete="off" maxlength="15" dir="ltr"
                           class="ex-preview-invoice-number digitNumber" placeholder="-">
                </div>
                <div class="ex-preview-field">
                    <label class="ex-preview-label" for="externalFactorInvoiceDate">تاریخ:</label>
                    <input id="externalFactorInvoiceDate" type="text" dir="ltr" autocomplete="off"
                           maxlength="10" class="sdate ex-preview-invoice-number" value="${escape(date)}" required>
                </div>
                <div class="ex-preview-field ex-preview-customer">
                    <span class="ex-preview-label">مشتری:</span>
                    <strong>${escape(preview.customerName)}</strong>
                </div>
                <div class="ex-preview-field">
                    <span class="ex-preview-label">نرم‌افزار مقصد:</span>
                    <strong class="ex-preview-software">${escape(data.softwareTitle)}</strong>
                </div>
            </div>
            ${renderFactorBroker({
                id: "externalFactorIncludeBroker", salespersonName: data.salespersonName,
                includeBroker: state.request.includeBroker, broker: data.broker,
            })}
                        <div class="table-responsive">
                <table class="table table-sm align-middle">
                    <thead>
                        <tr>
                            <th>ردیف</th>
                            <th>محصول ERP</th>
                            <th>کالای مقصد</th>
                            <th>انبار</th>
                            <th>تعداد</th>
                            <th>قیمت واحد</th>
                            <th>مبلغ کالا</th>
                            <th>تخفیف</th>
                            <th>مالیات</th>
                            <th>مبلغ نهایی</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${preview.items.map((item, index) => `
                            <tr>
                                <td>${index + 1}</td>
                                <td>${escape(item.crmProductTitle)}</td>
                                <td>${escape(item.itemTitle)}</td>
                                <td>${escape(item.stockTitle)}</td>
                                <td>${number(item.quantity)}</td>
                                <td>${number(item.fee)}</td>
                                <td>${number(item.price)}</td>
                                <td>${number(item.discount)}</td>
                                <td>${number(item.tax)}</td>
                                <td class="fbold">${number(item.netPrice)}</td>
                            </tr>`).join("")}
                    </tbody>
                </table>
            </div>
            <div class="external-mapping-entity mt-3">
                <div class="d-flex justify-content-between mb-1">
                    <span>جمع مبلغ کالاها</span><strong>${money(preview.price)}</strong>
                </div>
                <div class="d-flex justify-content-between mb-1">
                    <span>تخفیف کل</span><strong>${money(preview.discount)}</strong>
                </div>
                <div class="d-flex justify-content-between mb-1">
                    <span>مالیات کل</span><strong>${money(preview.tax)}</strong>
                </div>
                <hr>
                <div class="d-flex justify-content-between">
                    <strong>مبلغ نهایی</strong><strong>${money(preview.netPrice)}</strong>
                </div>
            </div>
        </div>
        <p class="text-muted small mb-0">
            با ثبت فاکتور، خروج انبار و سند حسابداری مطابق تنظیمات نرم‌افزار مالی صادر می‌شوند.
        </p>`;
}


function parseExternalFactorInvoiceNumber() {
    const input = document.getElementById("externalFactorInvoiceNumber");
    if (!input) return { error: "فیلد شماره فاکتور پیدا نشد." };
    
    const normalized = input.value.trim()
                            .replace(/[۰-۹]/g, char => String(char.charCodeAt(0) - 0x06F0))
                            .replace(/[٠-٩]/g, char => String(char.charCodeAt(0) - 0x0660))
                            .replace(/[٬,\s]/g, "");
    
    if (!normalized) return { number: null };
    if (!/^\d+$/.test(normalized)) {
        return { error: "شماره فاکتور باید فقط شامل رقم باشد." };
    }
    
    const number = Number(normalized);
    if (!Number.isInteger(number) || number <= 0 || number > 2147483647) {
        return { error: "شماره فاکتور باید عدد صحیح بین ۱ تا ۲٬۱۴۷٬۴۸۳٬۶۴۷ باشد." };
    }
    
    return { number };
}

function setExternalFactorInvoiceNumberInvalid(invalid) {
    document.getElementById("externalFactorInvoiceNumber")?.classList.toggle("is-invalid", invalid);
}

async function validateExternalFactorInvoiceNumber() {
    const state = externalFactorExportState;
    if (!state) return false;
    
    const parsed = parseExternalFactorInvoiceNumber();
    if (parsed.error) {
        setExternalFactorInvoiceNumberInvalid(true);
        toastifyError(parsed.error);
        return false;
    }
    
    if (parsed.number == null) {
        setExternalFactorInvoiceNumberInvalid(false);
        return true;
    }
    
    try {
        const result = await sendApiRequest({
            url: "/Factor/CheckExternalFactorInvoiceNumber",
            method: "POST",
            data: { ...state.request, invoiceNumber: parsed.number },
            showLoading: false,
            showError: false,
            successMsg: null,
        });
        
        // Do not display a stale error after the number changes or the modal closes.
        if (externalFactorExportState !== state ||
            parseExternalFactorInvoiceNumber().number !== parsed.number) return false;
        
        if (!result || typeof result.available !== "boolean") {
            setExternalFactorInvoiceNumberInvalid(true);
            toastifyError("امکان بررسی شماره فاکتور وجود ندارد.");
            return false;
        }
        
        setExternalFactorInvoiceNumberInvalid(!result.available);
        if (!result.available) {
            toastifyError(result.message ||
                `شماره فاکتور ${parsed.number} قبلاً در این سال مالی ثبت شده است.`);
        }
        return result.available;
    } catch {
        if (externalFactorExportState === state) {
            setExternalFactorInvoiceNumberInvalid(true);
            toastifyError("امکان بررسی شماره فاکتور وجود ندارد.");
        }
        return false;
    }
}

document.addEventListener("input", event => {
    if (event.target.id !== "externalFactorInvoiceNumber") return;
    setExternalFactorInvoiceNumberInvalid(false);
});

document.addEventListener("change", async event => {
    if (event.target.id !== "externalFactorInvoiceNumber") return;
    // A blank input preserves automatic numbering and needs no backend check.
    await validateExternalFactorInvoiceNumber();
});

document.addEventListener("click", async event => {
    if (!event.target.closest("#confirmSendExternalFactorBtn")) return;
    
    const state = externalFactorExportState;
    if (!state || state.processing) return;
    if (state.step !== "preview") {
        await factorDirectWizard.confirm(state);
        return;
    }

    const dateText = toEnglishDigits(document.getElementById("externalFactorInvoiceDate").value.trim());
    if (!/^[0-9]{4}\/[0-9]{2}\/[0-9]{2}$/.test(dateText)) {
        toastifyError("تاریخ فاکتور معتبر نیست.");
        return;
    }
    state.request.invoiceDateText = dateText;
    const parsed = parseExternalFactorInvoiceNumber();
    if (parsed.error) {
        setExternalFactorInvoiceNumberInvalid(true);
        toastifyError(parsed.error);
        return;
    }
    
    state.processing = true;
    const button = document.getElementById("confirmSendExternalFactorBtn");
    toggleElementsDisabled(button, true);
    
    try {
        // Early check improves UX; the server checks again inside the write transaction.
        if (parsed.number != null && !await validateExternalFactorInvoiceNumber()) return;
        
        const result = await sendApiRequest({
            url: "/Factor/SendFactorToExternalSoftware",
            method: "POST",
            data: { ...state.request, invoiceNumber: parsed.number },
            headers: ExternalFinancialWizard.antiforgeryHeaders("sendFactorToExternalSoftwareModal"),
            timeout: 0,
            successMsg: "فاکتور با موفقیت در سپیدار ثبت شد",
        });
        if (!result?.invoiceId) return;
        
        state.completedCount++;
        // Keep the underlying history current even if the remaining destinations are cancelled.
        if (externalFactorHistory.factorId === state.request.factorId) {
            await loadFactorExternalSoftwares(state.request.factorId);
        }
        // A successful financial write must not be repeated when another destination is pending.
        if (await continueFactorDestinations(state)) return;

        state.finished = true;
        ModalToggle("sendFactorToExternalSoftwareModal", "hide");
        if (typeof refreshFactorPartial === "function") await refreshFactorPartial();
    } finally {
        state.processing = false;
        toggleElementsDisabled(button, false);
    }
});

document.addEventListener("hidden.bs.modal", event => {
    if (event.target.id === "sendFactorToExternalSoftwareModal") {
        const state = externalFactorExportState;
        externalFactorExportState = null;
        factorFinancialWizard?.destroy();
        if (state?.completedCount && !state.finished &&
            typeof refreshFactorPartial === "function") {
            void refreshFactorPartial();
        }
        const historyModal = document.getElementById("externalSoftwareFactorsModal");
        if (historyModal && isModalOpen(historyModal))
            historyModal.querySelector("#sendExternalFactorBtn")?.focus({ preventScroll: true });
    }
});

const factorDirectWizard = (function buildFactorDirectWizard() {
    const modal = () => document.getElementById("sendFactorToExternalSoftwareModal");
    const body = () => document.getElementById("externalFactorPreviewContent");
    const button = () => document.getElementById("confirmSendExternalFactorBtn");
    const html = value => escapeHtml(String(value ?? ""));
    const get = (state) => state === externalFactorExportState;
    const setButton = (text, disabled = false) =>
        getFactorFinancialWizard().setActions({ confirmText: text, confirmDisabled: disabled });
    const isReady = state => {
        if (state.step === "saleType") return !!state.direct.saleTypeId;
        if (state.step === "customer") return !!state.direct.customerId;
        if (state.step === "items") {
            const source = state.request.usePreFactor ? state.quotation : state.direct;
            const items = state.request.usePreFactor ? source?.sourceItems : state.items;
            return !source?.stockPending?.size && items?.length > 0 &&
                items.every(item => source.items[item.factorItemId]?.externalItemId &&
                    source.items[item.factorItemId]?.externalStockId);
        }
        return true;
    };
    const update = state => {
        const confirmText = state.step === "items" ? "تأیید کالاها و انبارها و ادامه"
            : state.step === "retryDestination" ? "تلاش دوباره" : "تأیید و ادامه";
        setButton(confirmText, state.step !== "retryDestination" && !isReady(state));
    };
    const stage = (state, stageId, title, subtitle, icon, count, content, large = false) => {
        if (!get(state)) return;
        state.step = stageId;
        getFactorFinancialWizard().render({
            destination: state.softwareTitle,
            title, subtitle, icon, step: count, content, large,
        });
        getFactorFinancialWizard().setActions({
            backHidden: stageId === "mode",
            backDisabled: stageId === "mode",
        });
        update(state);
    };
    const query = async (action, data) => sendApiRequest({
        url: "/Factor/" + action, method: "POST", data,
        headers: action === "SaveExternalFactorCustomer"
            ? ExternalFinancialWizard.antiforgeryHeaders(modal()) : undefined,
        successMsg: null, showLoading: false,
    });

    function showMode(state) {
        if (!["quotation", "direct"].includes(state.sourceChoice) ||
            (state.sourceChoice === "quotation" && !state.hasQuotation)) {
            state.sourceChoice = state.hasQuotation ? "quotation" : "direct";
        }
        stage(state, "mode", "روش صدور فاکتور", "روش ثبت فاکتور در نرم‌افزار مالی را انتخاب کنید",
            '<i class="fas fa-file-invoice"></i>', "انتخاب روش", `
            <div class="factor-export-source-options" role="radiogroup" aria-required="true" aria-label="روش صدور فاکتور">
                <label class="factor-export-source">
                    <input type="radio" name="factorExportSource" value="quotation"
                           ${!state.hasQuotation ? "disabled" : state.sourceChoice === "quotation" ? "checked" : ""}>
                    <span>
                        <strong>صدور از پیش‌فاکتور نرم‌افزار مالی</strong>
                        <small>${state.hasQuotation
                            ? "استفاده از اطلاعات پیش‌فاکتور ثبت‌شده در نرم‌افزار مالی"
                            : "پیش‌فاکتور این فاکتور در نرم‌افزار مالی ثبت نشده است"}</small>
                    </span>
                </label>
                <label class="factor-export-source">
                    <input type="radio" name="factorExportSource" value="direct"
                           ${state.sourceChoice === "direct" ? "checked" : ""}>
                    <span>
                        <strong>صدور مستقیم فاکتور</strong>
                        <small>ثبت فاکتور در نرم‌افزار مالی بدون وابستگی به پیش‌فاکتور آن</small>
                    </span>
                </label>
            </div>`);
        body().querySelectorAll('input[name="factorExportSource"]').forEach(input => {
            input.addEventListener("change", () => { if (input.checked) state.sourceChoice = input.value; });
        });
    }

    async function loadSaleType(state) {
        if (!state.saleTypes) {
            const data = await query("PrepareExternalSaleType", {
                externalSoftwareIds: [state.request.externalSoftwareId],
            });
            if (!data || !get(state)) return;
            state.saleTypes = data.saleTypes || [];
        }
        renderSaleType(state);
    }
    function renderSaleType(state) {
        stage(state, "saleType", "انتخاب نوع فروش", "نوع فروش فاکتور را مشخص کنید",
            '<i class="fas fa-file-invoice"></i>', "مرحله ۱ از ۴",
            ExternalFinancialWizard.selectField({
                id: "factorDirectSaleType",
                label: "نوع فروش",
                required: true,
                placeholder: "انتخاب کنید",
                selected: state.direct.saleTypeId,
                options: state.saleTypes.map(x => ({
                    value: x.saleTypeId,
                    label: x.number ? x.number + " - " + x.title : x.title,
                })),
            }));
        const control = body().querySelector("#factorDirectSaleType");
        new TomSelect(control, {
            placeholder: "جستجو و انتخاب نوع فروش",
            onChange: value => { state.direct.saleTypeId = value; update(state); },
        });
        update(state);
    }

    async function loadCustomer(state) {
        if (!state.customerData) {
            const data = await query("PrepareExternalFactorCustomer", state.request);
            if (!data || !get(state)) return;
            state.customerData = data;
            const mapped = data.mappedExternalCustomerId;
            if (!state.direct.customerId && mapped && data.customers?.some(x => String(x.id) === String(mapped)))
                state.direct.customerId = String(mapped);
        }
        renderCustomer(state);
    }
    function renderCustomer(state) {
        const data = state.customerData;
        stage(state, "customer", "تطبیق مشتری", "مشتری ERP را به شخص متناظر در نرم‌افزار مالی متصل کنید",
            '<i class="fas fa-users"></i>', "مرحله ۲ از ۴",
            ExternalFinancialWizard.crmCustomer(data.crmCustomerTitle || "—") +
            ExternalFinancialWizard.selectField({
                id: "factorDirectCustomer",
                label: "مشتری معادل در نرم‌افزار مالی",
                required: true,
                placeholder: "جستجو و انتخاب مشتری",
                selected: state.direct.customerId,
                options: (data.customers || []).map(x => ({
                    value: x.id,
                    label: [x.dlCode, x.fullName].filter(Boolean).join(" - "),
                })),
            }));
        new TomSelect(body().querySelector("#factorDirectCustomer"), {
            placeholder: "جستجو و انتخاب مشتری",
            onChange: value => { state.direct.customerId = value; update(state); },
        });
        update(state);
    }

    async function loadItems(state, refresh = false) {
        if (refresh || !state.items || !state.sepidarItems) {
            const data = await query("PrepareExternalFactorItems", state.request);
            if (!data?.items?.length || !get(state)) return false;
            state.items = data.items;
            state.sepidarItems = data.sepidarItems || [];
        }
        renderItems(state);
        return true;
    }
    async function loadQuotationItems(state, refresh = false) {
        if (refresh || !state.quotation?.sourceItems) {
            const data = await query("PrepareExternalFactorQuotationItems", state.request);
            if (!data?.items?.length || !get(state)) return false;
            const previous = state.quotation;
            const selections = previous?.items ?? {};
            for (const item of data.items) {
                selections[item.factorItemId] ??= {
                    externalItemId: String(item.externalItemId),
                    externalStockId: String(item.externalStockId),
                };
            }
            state.quotation = {
                sourceItems: data.items,
                sepidarItems: data.sepidarItems || [],
                items: selections,
                stockCache: previous?.stockCache ?? new Map(),
                stocksByItem: previous?.stocksByItem ?? [],
            };
        }
        renderItems(state);
        return true;
    }

    function renderItems(state) {
        const quotationBased = state.request.usePreFactor;
        const source = quotationBased ? state.quotation : state.direct;
        const items = quotationBased ? source?.sourceItems : state.items;
        const externalItems = quotationBased ? source?.sepidarItems : state.sepidarItems;
        if (!source || !items?.length || !externalItems) return;

        const { content, add, refresh, list } = createExternalItemMatching();
        refresh.addEventListener("click", () => refreshExternalItemMatching(refresh, () =>
            quotationBased ? loadQuotationItems(state, true) : loadItems(state, true)));
        const rows = [];
        source.stockPending = new Set();
        source.stockCache ??= new Map();
        source.stocksByItem ??= [];

        add.addEventListener("click", async () => {
            add.disabled = true;
            try {
                await openSepidarItemCreation({
                    softwareId: state.request.externalSoftwareId,
                    softwareTitle: state.softwareTitle,
                    afterCreate: () => quotationBased
                        ? loadQuotationItems(state, true) : loadItems(state, true),
                    canOpen: () => get(state) && state.step === "items" &&
                        state.request.usePreFactor === quotationBased && isModalOpen(modal()),
                });
            } finally {
                add.disabled = false;
            }
        });

        for (const item of items) {
            const itemId = String(item.factorItemId);
            const target = appendExternalItemMatchRow(
                list, item.productTitle, item.weightCount, item.unitTxt);
            const label = document.createElement("label");
            label.htmlFor = "factorMappedItem_" + itemId;
            label.className = "form-label mb-0";
            label.textContent = "کالای معادل در نرم‌افزار مالی";

            const select = document.createElement("select");
            select.id = label.htmlFor;
            select.required = true;
            select.dataset.itemId = itemId;
            select.className = "factor-direct-item-select show-dropdown-arrow";
            select.append(new Option("", ""));
            const selected = source.items[itemId]?.externalItemId;
            for (const entry of externalItems) {
                select.append(new Option(
                    [entry.code, entry.title].filter(Boolean).join(" - "),
                    entry.id, false, String(entry.id) === String(selected ?? ""),
                ));
            }
            // Preserve the original quotation item even if Sepidar later deactivated it.
            if (quotationBased && selected && !externalItems.some(x => String(x.id) === String(selected))) {
                select.append(new Option(
                    item.defaultItemTitle || String(selected), selected, true, true));
            }
            target.append(label, select);
            rows.push({ itemId, target, select, item });
        }

        stage(state, "items", "تطبیق کالاها و انبارها",
            quotationBased
                ? "کالاها و انبارهای پیش‌فاکتور سپیدار انتخاب شده‌اند؛ در صورت نیاز تغییر دهید"
                : "برای هر ردیف فاکتور، کالای معادل و انبار مرتبط را انتخاب کنید",
            '<i class="fas fa-boxes"></i>',
            quotationBased ? "مرحله ۱ از ۲" : "مرحله ۳ از ۴", content, true);

        for (const { itemId, target, select, item } of rows) {
            const stock = ExternalFinancialWizard.linkedStockField(target, {
                id: "factorMappedStock_" + itemId,
                required: true,
                softwareId: state.request.externalSoftwareId,
                cache: source.stockCache,
                retainedStock: quotationBased ? {
                    itemId: item.externalItemId,
                    stockId: item.externalStockId,
                    title: item.defaultStockTitle,
                } : null,
                isCurrent: () => get(state) && state.step === "items" &&
                    state.request.usePreFactor === quotationBased,
                onChange: value => {
                    const mapping = source.items[itemId];
                    if (!mapping) return;
                    mapping.externalStockId = value;
                    update(state);
                },
                onLoading: loading => {
                    if (loading) source.stockPending.add(itemId);
                    else source.stockPending.delete(itemId);
                    update(state);
                },
                onLoaded: (externalItemId, stocks) => {
                    const index = source.stocksByItem.findIndex(
                        entry => String(entry.itemRef) === externalItemId);
                    const entry = { itemRef: Number(externalItemId), stocks };
                    if (index < 0) source.stocksByItem.push(entry);
                    else source.stocksByItem[index] = entry;
                },
            });

            new TomSelect(select, {
                placeholder: "جستجو و انتخاب کالا",
                onChange: value => {
                    const previous = source.items[itemId] || {};
                    if (!value) {
                        delete source.items[itemId];
                    } else {
                        source.items[itemId] = {
                            externalItemId: value,
                            externalStockId: String(previous.externalItemId) === String(value)
                                ? previous.externalStockId ?? "" : "",
                        };
                    }
                    void stock.update(value, source.items[itemId]?.externalStockId ?? "");
                    update(state);
                },
            });

            const mapping = source.items[itemId];
            void stock.update(mapping?.externalItemId ?? "", mapping?.externalStockId ?? "");
        }
        update(state);
    }

    async function preview(state) {
        const source = state.request.usePreFactor ? state.quotation : state.direct;
        const items = state.request.usePreFactor ? source.sourceItems : state.items;
        if (!state.request.usePreFactor) {
            state.request.saleTypeId = Number(state.direct.saleTypeId);
            state.request.externalCustomerId = Number(state.direct.customerId);
        }
        state.request.itemMappings = items.map(item => ({
            factorItemId: item.factorItemId,
            externalItemId: Number(source.items[item.factorItemId].externalItemId),
            externalStockId: Number(source.items[item.factorItemId].externalStockId),
        }));
        await loadExternalFactorPreview(state, false);
    }

    async function confirm(state) {
        if (!get(state) || state.processing || state.busy) return;
        state.busy = true;
        toggleElementsDisabled(button(), true);
        try {
            switch (state.step) {
                case "retryDestination":
                    await prepareFactorDestination(state);
                    break;
                case "mode":
                    state.request.usePreFactor = state.sourceChoice === "quotation";
                    if (state.request.usePreFactor) await loadQuotationItems(state);
                    else await loadSaleType(state);
                    break;
                case "saleType":
                    if (!state.direct.saleTypeId) return toastifyError("نوع فروش را انتخاب کنید.");
                    await loadCustomer(state);
                    break;
                case "customer":
                    if (!state.direct.customerId) return toastifyError("مشتری مقصد را انتخاب کنید.");
                    const saved = await query("SaveExternalFactorCustomer", {
                        ...state.request, externalCustomerId: Number(state.direct.customerId),
                    });
                    if (saved?.saved) await loadItems(state);
                    break;
                case "items":
                    if (!isReady(state)) return toastifyError("برای تمام ردیف‌های فاکتور کالا و انبار را انتخاب کنید.");
                    await preview(state);
                    break;
            }
        } finally {
            if (get(state)) {
                state.busy = false;
                if (state.step !== "preview") update(state);
            }
        }
    }

    function back(state) {
        if (!get(state) || state.processing || state.busy) return;
        switch (state.step) {
            case "saleType": showMode(state); break;
            case "customer": renderSaleType(state); break;
            case "items":
                if (state.request.usePreFactor) showMode(state);
                else renderCustomer(state);
                break;
            case "preview":
                state.request.includeBroker = false;
                state.request.previewBrokerPartyId = null;
                state.hasLoadedPreview = false;
                renderItems(state);
                break;
        }
    }
    document.addEventListener("click", event => {
        if (event.target.closest("#backExternalFactorStepBtn"))
            back(externalFactorExportState);
    });

    return {showMode, confirm, back};
})();

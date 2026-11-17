async function openSupplyFinancial(sourceId) {
    const historyModal = document.getElementById("supplyFinancialModal");
    const wizard = document.getElementById("sendSupplyToExternalSoftwareModal");
    if (!historyModal || !wizard || historyModal.dataset.active === "true") return;
    historyModal.dataset.active = "true";

    const token = historyModal.querySelector('[name="__RequestVerificationToken"]').value;
    const historyList = document.getElementById("externalSoftwareSupplyFactors");
    const send = document.getElementById("sendSupplyFinancialBtn");
    const sendLabel = send.querySelector("[data-financial-history-send-label]");
    const historyHeading = historyModal.querySelector("[data-financial-history-heading]");
    const stageContainer = document.getElementById("supplyFinancialStage");
    const back = document.getElementById("backSupplyFinancialBtn");
    const confirm = document.getElementById("confirmSupplyFinancialBtn");
    const chooser = document.getElementById("chooseExternalSoftwareModal");
    const lifecycle = new AbortController();
    const { signal } = lifecycle;
    const wizardUi = new ExternalFinancialWizard({
        modal: wizard, container: stageContainer, back, confirm,
    });
    let version = 0, fieldId = 0, busy = false, choosing = false, closing = false;
    let destinations = [], position = 0;
    let onBack, onConfirm, canConfirm;

    const make = (tag, text, classes) => {
        const node = document.createElement(tag);
        if (text != null) node.textContent = text;
        if (classes) node.className = classes;
        return node;
    };
    const label = option => [option.code, option.title].filter(Boolean).join(" - ");
    const request = async (action, data, method = "POST", showLoading = true) => {
        const response = await sendApiRequest({
            url: "/Supply/" + action, method, successMsg: null, returnFullResponse: true,
            signal, showLoading,
            ...(method === "GET" ? { params: data } : { data, headers: { RequestVerificationToken: token } }),
        });
        return response?.data ?? null;
    };
    const fields = (name, value) => {
        const entity = make("div", null, "external-mapping-entity");
        entity.append(make("span", name, "external-mapping-label"),
            make("span", value ?? "—", "external-mapping-entity-name fbold"));
        return entity;
    };
    const clearSelects = () => wizardUi.clearSelects();
    const updateButtons = () => wizardUi.setActions({
        backDisabled: busy || !onBack,
        confirmDisabled: busy || !onConfirm || !canConfirm(),
    });
    const stage = ({ software, title, subtitle, icon, step, content, large, backAction, confirmAction,
        confirmText = "تأیید و ادامه", backText = "برگشت", enabled = () => true }) => {
        disposeDateTimePickers(stageContainer);
        wizardUi.render({
            destination: software.title, position: "نرم‌افزار " + (position + 1) + " از " + destinations.length,
            title, subtitle, icon: `<i class="fas ${icon}" aria-hidden="true"></i>`, step, content, large,
        });
        onBack = backAction;
        onConfirm = confirmAction;
        canConfirm = enabled;
        wizardUi.setActions({ confirmText, backText, backHidden: false });
        updateButtons();
    };
    const field = (parent, text, control) => {
        control.id = "supply-financial-field-" + (++fieldId);
        const caption = make("label", text, "form-label mb-0");
        caption.htmlFor = control.id;
        const group = make("div");
        group.append(caption, control);
        parent.append(group);
        return control;
    };
    const descriptionField = (parent, text, value, max) => {
        const control = make("textarea", null, "form-control form-control-sm");
        control.rows = 1;
        control.maxLength = max;
        control.value = value ?? "";
        return field(parent, text, control);
    };
    const optionalDescription = (parent, label, value, max) => {
        const details = make("details", null, "supply-financial-note");
        details.open = Boolean(value?.trim());
        details.append(make("summary", details.open ? "ویرایش " + label : "افزودن " + label));
        const control = descriptionField(details, label, value, max);
        control.previousElementSibling.classList.add("visually-hidden");
        parent.append(details);
        return control;
    };
    const setupSelect = (control, placeholder, options = {}) => {
        new TomSelect(control, { placeholder, onChange: updateButtons, ...options });
    };
    const normalizeOptionSearch = value => normalizePersianText(value)
        .replace(/[۰-۹٠-٩]/g, digit => String(digit.charCodeAt(0) & 15))
        .replace(/\u200c/g, " ")
        .trim().toLowerCase();
    const searchableOption = row => {
        const text = label(row);
        return { value: String(row.id), text, searchText: normalizeOptionSearch(text) };
    };
    const setupSearchableSelect = (control, placeholder, options, selectedId) => {
        setupSelect(control, placeholder, {
            options: options.map(item => ({ ...item })),
            items: selectedId ? [String(selectedId)] : [],
            searchField: ["searchText"],
            score(query) {
                return this.getScoreFunction(normalizeOptionSearch(query));
            },
        });
    };
    const loadItemStage = async (data, source, software, refresh = false) => {
        if (refresh || !source.itemOptions) {
            const rows = await request("FinancialOptions", { externalSoftwareId: software.id, items: true }, "GET");
            if (!rows || signal.aborted || !isModalOpen(wizard)) return;
            source.itemOptions = rows.map(searchableOption);
        }
        documentStage(data, source, software);
    };

    const historyFields = entries => {
        const info = make("div", null, "external-pf-info");
        for (const [caption, value, accent] of entries) {
            const item = make("div", null, "external-pf-field");
            const text = make("span", value ?? "—", "external-pf-value" + (accent ? " external-pf-software" : ""));
            if (value) text.title = value;
            item.append(make("span", caption + ":", "external-pf-label"), text);
            info.append(item);
        }
        return info;
    };
    const historyActions = button => {
        const actions = make("div", null, "external-pf-actions");
        actions.append(button);
        return actions;
    };
    const missingHistory = (software, entry, receipt) => {
        const remove = make("button", "حذف سابقه", "external-pf-action supply-financial-remove");
        remove.type = "button";
        remove.addEventListener("click", async () => {
            remove.disabled = true;
            try {
                const result = await Swal.fire({
                    target: historyModal,
                    title: "حذف سابقه ارسال",
                    text: "ارتباط این سند با " + software.title + " از CRM حذف شود؟",
                    icon: "warning",
                    showCancelButton: true,
                    confirmButtonText: "حذف سابقه",
                    cancelButtonText: "انصراف",
                    focusCancel: true,
                });
                if (!result.isConfirmed || signal.aborted) return;
                const deleted = await request("DeleteMissingFinancialMapping", {
                    sourceId, softwareId: software.id, mappingId: entry.mappingId, receipt,
                });
                if (!deleted?.deleted) return;
                toastifySuccess("سابقه ارسال حذف شد.");
                if (!signal.aborted) await loadHistory();
            } finally {
                remove.disabled = false;
            }
        }, { signal });
        return historyActions(remove);
    };
    const loadHistory = async () => {
        const current = ++version;
        send.disabled = true;
        const result = await request("FinancialHistory", { sourceId }, "GET", false);
        if (signal.aborted || current !== version) return;
        historyList.replaceChildren();
        if (!result) {
            historyList.append(make("div", "دریافت سوابق انجام نشد.", "external-pf-empty"));
            return;
        }
        historyModal.querySelector(".modal-title").textContent = "مدیریت ارسال فاکتور تأمین " + (result.serialNumber ?? "");
        const records = result.softwares.filter(row => row.purchase || row.receipt);
        for (const row of records) {
            const { purchase, receipt } = row;
            const invoice = purchase?.document;
            const group = make("div", null, "supply-financial-document-group");
            const card = make("div", null, "external-pf-card" + (!invoice ? " supply-financial-missing" : ""));
            const info = historyFields([
                ["نرم‌افزار", row.title, true], ["تاریخ ثبت", purchase?.dateText],
                ["شماره فاکتور خرید", invoice?.number], ["تأمین‌کننده", invoice?.partyTitle],
            ]);
            if (!invoice)
                info.append(make("p", purchase ? "فاکتور خرید در نرم‌افزار مقصد پیدا نشد." :
                    "سابقه فاکتور خرید برای این مقصد موجود نیست.", "supply-financial-missing-message"));
            card.append(info);
            if (purchase && !invoice && purchase.mappingId)
                card.append(missingHistory(row, purchase, false));
            if (invoice && !receipt) {
                const register = make("button", "ثبت رسید انبار", "external-pf-action external-pf-view");
                register.type = "button";
                register.addEventListener("click", async () => {
                    if (choosing || send.disabled || isModalOpen(wizard)) return;
                    choosing = true;
                    register.disabled = true;
                    try {
                        destinations = [row];
                        position = 0;
                        await prepare(row, true, true, invoice.id);
                    } finally {
                        register.disabled = false;
                        choosing = false;
                    }
                }, { signal });
                card.append(historyActions(register));
            }
            group.append(card);
            if (receipt) {
                const document = receipt.document;
                const unrelated = !!invoice && !!document && document.basePurchaseId !== invoice.id;
                const panel = make("div", null, "supply-financial-receipt" +
                    (!document || unrelated ? " supply-financial-missing" : ""));
                panel.append(make("div", !document || unrelated ? "رسید انبار" : "رسید انبار ثبت شده",
                    "supply-financial-receipt-title"));
                if (document && !unrelated) {
                    panel.append(historyFields([
                        ["شماره رسید", document.number], ["تاریخ ثبت", receipt.dateText],
                        ["تحویل‌دهنده", document.partyTitle],
                    ]));
                } else {
                    panel.append(make("p", unrelated ? "رسید ثبت‌شده به این فاکتور خرید مربوط نیست." :
                        "رسید انبار در نرم‌افزار مقصد پیدا نشد.", "supply-financial-missing-message"));
                    if (!document && receipt.mappingId) panel.append(missingHistory(row, receipt, true));
                }
                group.append(panel);
            }
            historyList.append(group);
        }
        if (!records.length) {
            historyList.append(make("div", "این فاکتور خرید هنوز در نرم‌افزار مالی ثبت نشده است.", "external-pf-empty"));
        }
        send.disabled = result.softwares.length === 0;
        send.onclick = () => choose(result.softwares);
    };

    const choose = async softwares => {
        if (choosing || isModalOpen(wizard)) return;
        choosing = true;
        const chooserEvents = new AbortController();
        const chooserSignal = AbortSignal.any([signal, chooserEvents.signal]);
        let selected = [];
        let chooserShown = false;
        chooser.addEventListener("shown.bs.modal", () => {
            chooserShown = true;
            if (selected.length) ModalToggle(chooser, "hide");
        }, { once: true, signal: chooserSignal });
        chooser.addEventListener("hidden.bs.modal", async () => {
            try {
                if (!selected.length) {
                    send.focus();
                    return;
                }
                destinations = selected.map(id => softwares.find(software => software.id === id)).filter(Boolean);
                if (!destinations.length) return;
                position = 0;
                await prepare(destinations[0], false, true);
            } finally {
                choosing = false;
            }
        }, { once: true, signal: chooserSignal });
        const result = await showExternalSoftwareModal(sourceId, {
            mapType: 8,
            onConfirm: ({ externalSoftwareIds }) => {
                selected = externalSoftwareIds;
                if (chooserShown) ModalToggle(chooser, "hide");
            },
        });
        if (!result) {
            chooserEvents.abort();
            choosing = false;
        }
    };
    const next = () => {
        position++;
        if (position < destinations.length) return prepare(destinations[position], false);
        closing = true;
        ModalToggle(wizard, "hide");
    };

    const prepare = async (software, receipt, open = false, purchaseId) => {
        const data = { supplyFactorId: sourceId, externalSoftwareId: software.id, receipt, purchaseId };
        const title = receipt ? "رسید انبار" : "فاکتور خرید";
        onBack = onConfirm = null;
        updateButtons();
        const pendingVersion = ++version;
        const source = await request("PrepareFinancial", data, "POST", false);
        if (signal.aborted || pendingVersion !== version) return;
        if (!source || source.recover || source.document) {
            const message = source?.document ? "سند شماره " + source.document.number + " در سپیدار ثبت شده است."
                : "آماده‌سازی سند انجام نشد.";
            stage({ software, title, subtitle: message, icon: "fa-clipboard-check", step: "وضعیت ثبت",
                confirmText: source?.recover ? "تکمیل ارتباط" : "ادامه",
                confirmAction: async () => {
                    if (!source?.recover) return next();
                    const document = await request("SubmitFinancial", data);
                    if (!document) return;
                    if (receipt)
                        toastifySuccess("سند شماره " + document.number + " در نرم‌افزار مالی ثبت شد.");
                    await next();
                },
            });
        } else if (receipt) {
            data.dateText = source.dateText;
            receiptStockStage(data, source, software);
        } else {
            vendorStage(data, source, software);
        }
        if (open) ModalToggle(wizard, "show");
    };

    const vendorStage = (data, source, software) => {
        const content = make("div", null, "d-flex flex-column gap-3");
        content.append(fields("تأمین‌کننده در CRM", source.supplierTxt));
        const select = field(content, "تأمین‌کننده معادل در نرم‌افزار مالی", make("select", null, "show-dropdown-arrow"));
        stage({ software, title: "تطبیق تأمین‌کننده", subtitle: "اتصال تأمین‌کننده CRM به نرم‌افزار مالی",
            icon: "fa-users", step: "مرحله ۱ از ۳", content,
            backAction: () => ModalToggle(wizard, "hide"), enabled: () => !!select.value,
            confirmAction: async () => {
                data.vendorId = Number(select.value);
                source.vendor = await request("SaveFinancialVendor", data);
                if (source.vendor) await loadItemStage(data, source, software);
            },
        });
        setupSearchableSelect(select, "جستجو و انتخاب تأمین‌کننده",
            source.vendorOptions.map(searchableOption), source.vendor?.id);
        updateButtons();
    };

    const receiptStockStage = (data, source, software) => {
        const content = make("div", null, "d-grid gap-2");
        content.append(fields("شماره فاکتور خرید", source.number));
        const stock = field(content, "انبار", make("select", null, "show-dropdown-arrow"));
        stock.append(new Option("", ""));
        source.stocks.forEach(row => stock.append(new Option(label(row), row.id)));
        stock.value = String(data.stockId ?? (source.stocks.length === 1 ? source.stocks[0].id : ""));
        stage({ software, title: "انتخاب انبار", subtitle: "انبار مقصد برای دریافت همه اقلام را مشخص کنید",
            icon: "fa-warehouse", step: "مرحله ۱ از ۳", content,
            backAction: () => ModalToggle(wizard, "hide"),
            confirmText: "تأیید انبار و ادامه", enabled: () => !!stock.value,
            confirmAction: () => {
                data.stockId = Number(stock.value);
                documentStage(data, source, software);
            },
        });
        setupSelect(stock, "جستجو و انتخاب انبار");
        updateButtons();
    };

    const documentStage = (data, source, software) => {
        const { receipt } = data;
        const { content, toolbar, add, list } = createExternalItemMatching();
        if (receipt) toolbar.remove();
        const rows = source.items.map(row => {
            const previous = data.items?.find(x => x.supplyFactorItemId === row.id);
            const target = appendExternalItemMatchRow(list, row.product, row.weightCount, row.unitTxt, receipt ? "مقدار:" : null);
            const select = receipt ? null : field(target, "کالای معادل در نرم‌افزار مالی",
                make("select", null, "show-dropdown-arrow"));
            const text = optionalDescription(target, "توضیحات قلم",
                receipt ? (data.rowDescriptions?.[row.id] ?? row.description) : previous?.description, receipt ? 4000 : 500);
            return { row, select, text, previous };
        });
        const description = optionalDescription(content, "توضیحات سند", data.description ?? source.description, receipt ? 4000 : 400);
        const captureDraft = () => {
            data.description = description.value;
            if (receipt) {
                data.rowDescriptions = Object.fromEntries(rows.map(x => [x.row.id, x.text.value]));
            } else {
                data.items = rows.map(x => ({
                    supplyFactorItemId: x.row.id, itemId: Number(x.select.value), description: x.text.value,
                }));
            }
        };
        if (!receipt) add.addEventListener("click", () => openSepidarItemCreation({
            softwareId: software.id,
            softwareTitle: software.title,
            canOpen: () => !signal.aborted && content.isConnected && isModalOpen(wizard),
            afterCreate: () => {
                captureDraft();
                return loadItemStage(data, source, software, true);
            },
        }));
        stage({ software, title: receipt ? "اقلام و توضیحات رسید" : "تطبیق کالاها",
            subtitle: receipt ? "اقلام خرید را بررسی کنید و در صورت نیاز توضیحات را ویرایش کنید" : "انتخاب کالای معادل برای ردیف‌های فاکتور تأمین",
            icon: "fa-boxes", step: "مرحله ۲ از ۳", content, large: true,
            backAction: () => {
                captureDraft();
                if (receipt) receiptStockStage(data, source, software);
                else vendorStage(data, source, software);
            },
            confirmText: receipt ? "پیش‌نمایش رسید" : "تأیید کالاها و ادامه",
            enabled: () => rows.length > 0 && (receipt || rows.every(row => row.select.value)),
            confirmAction: async () => {
                captureDraft();
                const preview = await request("PreviewFinancial", data);
                if (preview) previewStage(preview, data, source, software);
            },
        });
        if (!receipt) rows.forEach(row => setupSearchableSelect(row.select, "جستجو و انتخاب کالا",
            source.itemOptions, row.previous?.itemId));
        updateButtons();
    };

    const previewStage = (preview, data, source, software) => {
        const { receipt } = data;
        const title = receipt ? "رسید انبار" : "فاکتور خرید";
        const info = [
            { label: receipt ? "شماره فاکتور خرید" : "شماره فاکتور تأمین‌کننده",
                value: receipt ? source.number : preview.serialNumber },
            ...(!receipt ? [{ label: "تاریخ", value: preview.dateText }] : []),
            { label: "نرم‌افزار مقصد", value: software.title, accent: true },
            { label: "تأمین‌کننده", value: preview.supplierTxt, wide: true },
        ];
        if (receipt) info.push({ label: "انبار", value: preview.stock });
        if (preview.description) info.push({ label: "توضیحات", value: preview.description, wide: true });
        const columns = ["ردیف", ...(receipt ? [] : ["محصول CRM"]), "کالای مقصد", "مقدار", "مبلغ کالا",
            ...(receipt ? [] : ["تخفیف"]), "مالیات", "توضیحات", "مبلغ نهایی"];
        const content = renderExternalFinancialPreview({
            fields: info, columns,
            rows: preview.items.map((row, index) => [
                index + 1, ...(receipt ? [] : [source.items[index]?.product]), row.product, row.quantity,
                (row.price ?? 0).toLocaleString(), ...(receipt ? [] : [(row.discount ?? 0).toLocaleString()]),
                (row.tax ?? 0).toLocaleString(), row.description || "—",
                (Number(row.price ?? 0) - Number(row.discount ?? 0) + Number(row.tax ?? 0)).toLocaleString(),
            ]),
            totals: [["جمع مبلغ کالاها", preview.totalPrice], ["تخفیف کل", preview.discount],
                ["مالیات کل", preview.totalTax], ["مبلغ نهایی", preview.total]],
        });
        let previewContent = content;
        let date;
        const getDateText = () => toEnglishDigits(date.value.trim());
        if (receipt) {
            previewContent = make("div", null, "d-grid gap-2");
            date = make("input", null, "sdate ex-preview-invoice-number");
            date.id = "supplyFinancialReceiptDate";
            date.type = "text";
            date.dir = "ltr";
            date.autocomplete = "off";
            date.value = preview.dateText;
            date.maxLength = 10;
            date.setAttribute("data-datepicker-change", "");
            const updateDateButton = () => {
                confirm.textContent = getDateText() === data.dateText
                    ? "تأیید و ثبت در نرم‌افزار" : "به‌روزرسانی پیش‌نمایش";
                updateButtons();
            };
            date.addEventListener("input", updateDateButton);
            date.addEventListener("change", updateDateButton);
            previewContent.insertAdjacentHTML("beforeend", content + '<p class="text-muted small mb-0">' + (preview.voucher
                ? "سند حسابداری خودکار صادر می‌شود." : "صدور خودکار سند حسابداری در مقصد غیرفعال است.") + "</p>");
            const dateField = make("div", null, "ex-preview-field");
            const dateLabel = make("label", "تاریخ رسید:", "ex-preview-label");
            dateLabel.htmlFor = date.id;
            dateField.append(dateLabel, date);
            previewContent.querySelector(".ex-preview-summary").firstElementChild.after(dateField);
        }
        data.previewToken = preview.previewToken;
        stage({ software, title: "بررسی نهایی " + title, subtitle: "قبل از ثبت در نرم‌افزار مالی، اطلاعات را بررسی کنید",
            icon: "fa-clipboard-check", step: "مرحله ۳ از ۳", large: true, content: previewContent,
            backAction: () => documentStage(data, source, software),
            confirmText: "تأیید و ثبت در نرم‌افزار",
            enabled: () => !receipt || /^[0-9]{4}\/[0-9]{2}\/[0-9]{2}$/.test(getDateText()),
            confirmAction: async () => {
                if (receipt && getDateText() !== data.dateText) {
                    const dateText = getDateText();
                    const updatedPreview = await request("PreviewFinancial", { ...data, dateText });
                    if (updatedPreview) {
                        data.dateText = dateText;
                        previewStage(updatedPreview, data, source, software);
                    }
                    return;
                }
                const document = await request("SubmitFinancial", data);
                if (!document) return;
                const message = "سند شماره " + document.number + " در نرم‌افزار مالی ثبت شد.";
                toastifySuccess(message);
                if (receipt) return next();

                stage({ software, title: "ثبت " + title, icon: "fa-check", step: "ثبت موفق",
                    subtitle: message,
                    content: make("p", "برای این فاکتور رسید انبار ثبت شود؟", "mb-0"),
                    backAction: next,
                    backText: "بعداً",
                    confirmText: "ثبت رسید انبار",
                    confirmAction: () => prepare(software, true, false, document.id),
                });
            },
        });
        if (receipt) dateTimePicker(stageContainer);
    };

    confirm.addEventListener("click", async () => {
        if (confirm.disabled) return;
        busy = true;
        updateButtons();
        try { await onConfirm(); }
        finally { busy = false; updateButtons(); }
    }, { signal });
    back.addEventListener("click", () => onBack?.(), { signal });
    wizard.addEventListener("hide.bs.modal", event => {
        if (busy && !closing) event.preventDefault();
    }, { signal });
    wizard.addEventListener("hidden.bs.modal", async () => {
        closing = false;
        clearSelects();
        disposeDateTimePickers(stageContainer);
        stageContainer.replaceChildren();
        await loadHistory();
        if (!signal.aborted) (send.disabled ? historyModal.querySelector(".btn-close") : send)?.focus();
    }, { signal });
    historyModal.addEventListener("hidden.bs.modal", () => {
        lifecycle.abort();
        wizardUi.destroy();
        send.onclick = null;
        historyModal.dataset.active = "";
    }, { once: true });
    enableMomentumScrollScope(historyModal, { signal });
    enableMomentumScrollScope(wizard, { signal });
    historyHeading.textContent = "سوابق فاکتور خرید";
    sendLabel.textContent = "ارسال فاکتور خرید به نرم‌افزار مالی";
    historyList.replaceChildren();
    ModalToggle(historyModal, "show");
    await loadHistory();
}

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
    const lifecycle = new AbortController();
    const { signal } = lifecycle;
    const historyUi = new ExternalFinancialHistory({
        modal: historyModal, list: historyList,
    });
    const historyCard = ExternalFinancialHistory;
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
        confirmText = "تأیید و ادامه", backText = "برگشت", backHidden = false,
        positionText = "نرم‌افزار " + (position + 1) + " از " + destinations.length, enabled = () => true }) => {
        disposeDateTimePickers(stageContainer);
        wizardUi.render({
            destination: software.title, position: positionText,
            title, subtitle, icon: `<i class="fas ${icon}" aria-hidden="true"></i>`, step, content, large,
        });
        onBack = backAction;
        onConfirm = confirmAction;
        canConfirm = enabled;
        wizardUi.setActions({ confirmText, backText, backHidden });
        updateButtons();
    };
    const field = (parent, text, control, required = false) => {
        control.id = "supply-financial-field-" + (++fieldId);
        control.required = required;
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
        const details = make("details", null, "external-financial-note" +
            (parent.classList.contains("external-item-matching") ? " external-financial-note-panel" : ""));
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
            if (!rows || signal.aborted || !isModalOpen(wizard)) return false;
            source.itemOptions = rows.map(searchableOption);
        }
        documentStage(data, source, software);
        return true;
    };

    const missingHistory = (software, entry, receipt) => {
        const remove = historyCard.action("حذف سابقه", { danger: true });
        remove.addEventListener("click", async () => {
            remove.disabled = true;
            try {
                const result = await Swal.fire({
                    target: historyModal,
                    title: "حذف سابقه ارسال",
                    text: "ارتباط این سند با " + software.title + " از ERP حذف شود؟",
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
        return remove;
    };

    const destinationKey = (software, purchase, receipt) => {
        const key = { sourceId, softwareId: software.id, purchaseMappingId: purchase.mappingId };
        if (receipt) {
            key.receiptId = receipt.document.id;
            if (receipt.mappingId) key.receiptMappingId = receipt.mappingId;
        }
        return key;
    };

    const destinationOperations = (software, purchase, receipt) => {
        const key = destinationKey(software, purchase, receipt);
        const edit = historyCard.action("ویرایش انتقال", { icon: "fa-pencil-alt",
            onClick: () => {
                if (purchase.requiresSynchronization) return toastifyError("ابتدا فاکتور خرید را همگام‌سازی کنید.");
                if (receipt?.requiresSynchronization) return toastifyError("ابتدا رسید انبار را همگام‌سازی کنید.");
                return receipt ? openReceiptEdit(software, key) : openPurchaseEdit(software, key);
            } });
        const remove = historyCard.action("حذف از مقصد", { icon: "fa-trash-alt", danger: true,
            onClick: event => deleteDestination(event.currentTarget, software, key) });
        return [edit, remove];
    };

    const discrepancy = (software, purchase, receipt) => ({
        message: receipt ? "رسید انبار با فاکتور خرید مغایرت دارد." : "اقلام یا مبالغ فاکتور خرید با ERP مغایرت دارد.",
        onSynchronize: event => openSynchronization(event.currentTarget, software, purchase, receipt),
    });

    const openSynchronization = async (button, software, purchase, receipt) => {
        if (choosing || busy || isModalOpen(wizard)) return;
        choosing = button.disabled = true;
        try {
            const key = destinationKey(software, purchase, receipt);
            const receiptOnly = Boolean(receipt);
            const data = await request("GetFinancialSynchronization", { ...key, receiptOnly }, "GET", false);
            if (!data || signal.aborted || !isModalOpen(historyModal)) return;
            const { preview, autoVoucher } = data;
            const title = receiptOnly ? "رسید انبار" : "فاکتور خرید";
            const number = receiptOnly ? preview.receiptNumber ?? receipt.document.number : preview.purchaseNumber;
            const syncStage = options => stage({
                software, positionText: `${title} شماره ${number}`, icon: "fa-sync-alt", large: true, ...options,
            });
            const close = () => ModalToggle(wizard, "hide");
            const finish = () => {
                closing = true;
                close();
            };
            if (preview.blockers.length) {
                const errors = make("ul", null, "ps-4 mb-0");
                preview.blockers.forEach(message => errors.append(make("li", message)));
                syncStage({
                    title: "همگام‌سازی " + title, subtitle: "همگام‌سازی این سند امکان‌پذیر نیست.", content: errors,
                    confirmText: "بستن", confirmAction: finish, backHidden: true,
                });
                ModalToggle(wizard, "show");
                return;
            }

            const options = data.items.map(searchableOption);
            const labels = new Map(options.map(option => [option.value, option.text]));
            const lookupOptions = options.map(option => ({ value: option.value, label: option.text }));
            const selected = new Map(preview.sourceRows.map(row => [row.id,
                labels.has(String(row.suggestedItemId)) ? row.suggestedItemId : null]));
            const format = value => Number(value ?? 0).toLocaleString("fa-IR", { maximumFractionDigits: 4 });
            const renderPreview = () => {
                const content = make("div", null, "external-prefactor-preview d-grid gap-3");
                const section = (heading, columns, rows) => appendExternalFinancialPreviewSection(content, heading, columns, rows);
                const amounts = [["جمع مبلغ کالاها", "price"], ["تخفیف کل", "discount"],
                    ["مالیات کل", "tax"], ["مبلغ نهایی", "total"]];
                const compare = (documentTitle, current, replacement, sourceLabel) => {
                    const columns = ["کالا", "مقدار", "قیمت واحد", "مبلغ", "تخفیف", "مالیات"];
                    const values = rows => rows.map(({ itemLabel, amounts }) => [
                        itemLabel, ...[amounts.quantity, amounts.fee, amounts.price, amounts.discount, amounts.tax].map(format),
                    ]);
                    const sum = (rows, field) => format(rows.reduce((total, row) => total + Number(row.amounts[field] ?? 0), 0));
                    section(`اقلام فعلی ${documentTitle} در سپیدار`, columns, values(current));
                    section(`اقلام جایگزین ${documentTitle} از ${sourceLabel}`, columns, values(replacement));
                    section(`مقایسه مبالغ ${documentTitle} (ریال)`, ["عنوان", "سپیدار", sourceLabel],
                        amounts.map(([label, field]) => [label, sum(current, field), sum(replacement, field)]));
                };
                const replacement = receiptOnly ? preview.purchaseRows : preview.sourceRows.map(row => ({
                    itemLabel: labels.get(String(selected.get(row.id))),
                    amounts: row.amounts,
                }));
                if (!receiptOnly) compare("فاکتور خرید", preview.purchaseRows, replacement, "ERP");
                if (preview.receiptNumber != null) {
                    if (!receiptOnly) content.append(make("hr", null, "my-0 border-secondary opacity-50"));
                    const receiptReplacement = replacement.map(row => {
                        const amounts = row.amounts;
                        const price = amounts.price - amounts.discount;
                        return { ...row, amounts: { ...amounts, price, discount: 0,
                            fee: amounts.quantity ? price / amounts.quantity : 0 } };
                    });
                    compare("رسید انبار", preview.receiptRows, receiptReplacement, receiptOnly ? "فاکتور خرید" : "ERP");
                    const notes = make("ul", null, "small ps-4 mb-0");
                    if (!receiptOnly || preview.stockTitle)
                        notes.append(make("li", `رسید انبار شماره ${preview.receiptNumber}${preview.stockTitle ? ` (انبار ${preview.stockTitle})` : ""} به‌روزرسانی می‌شود.`));
                    const voucherText = preview.voucherNumber != null
                        ? `سند حسابداری شماره ${preview.voucherNumber} ${autoVoucher ? "بازسازی می‌شود." : "حذف می‌شود. صدور خودکار در مقصد خاموش است."}`
                        : autoVoucher ? "سند حسابداری رسید صادر می‌شود." : "صدور خودکار سند حسابداری در مقصد خاموش است.";
                    notes.append(make("li", voucherText));
                    content.append(notes);
                }
                syncStage({ title: "بررسی نهایی همگام‌سازی",
                    subtitle: receiptOnly ? "تغییرات روی همان رسید انبار سپیدار اعمال می‌شوند."
                        : preview.receiptNumber != null ? "فاکتور خرید و رسید انبار مرتبط در سپیدار به‌روزرسانی می‌شوند."
                            : "تغییرات روی همان فاکتور خرید سپیدار اعمال می‌شوند.",
                    step: receiptOnly ? null : "مرحله ۲ از ۲", content,
                    backAction: receiptOnly ? close : renderMatching, backText: receiptOnly ? "انصراف" : "برگشت",
                    confirmText: "تأیید و همگام‌سازی",
                    confirmAction: async () => {
                        const result = await request("SynchronizeFinancialDocument", {
                            key, receiptOnly, token: preview.token, autoVoucher,
                            items: receiptOnly ? [] : preview.sourceRows.map(row => ({
                                supplyFactorItemId: row.id, itemId: selected.get(row.id),
                            })),
                        });
                        if (!result?.saved) return;
                        toastifySuccess(title + " همگام شد.");
                        finish();
                    },
                });
            };
            const renderMatching = () => {
                const { content, toolbar, list } = createExternalItemMatching();
                toolbar.remove();
                const controls = preview.sourceRows.map(row => {
                    const target = appendExternalItemMatchRow(list, row.product, row.amounts.quantity, row.unit);
                    target.innerHTML = ExternalFinancialWizard.selectField({
                        id: "supply-financial-field-" + (++fieldId), label: "کالای معادل در نرم‌افزار مالی",
                        options: lookupOptions, selected: selected.get(row.id), placeholder: "انتخاب کالا", required: true,
                    });
                    return target.querySelector("select");
                });
                syncStage({ title: "تطبیق کالاها",
                    subtitle: "کالای مقصد را برای هر قلم بررسی کنید.", step: "مرحله ۱ از ۲", content,
                    backAction: close, backText: "انصراف", confirmText: "بررسی تغییرات",
                    confirmAction: renderPreview,
                    enabled: () => preview.sourceRows.every(row => selected.get(row.id)),
                });
                controls.forEach((control, index) => {
                    const row = preview.sourceRows[index];
                    setupSearchableSelect(control, "جستجو و انتخاب کالا", options, selected.get(row.id));
                    control.addEventListener("change", () => {
                        selected.set(row.id, Number(control.value) || null);
                        updateButtons();
                    });
                });
            };
            if (receiptOnly) renderPreview();
            else renderMatching();
            ModalToggle(wizard, "show");
        } finally {
            choosing = button.disabled = false;
        }
    };

    const deleteDestination = async (button, software, key) => {
        if (choosing || busy || isModalOpen(wizard)) return;
        choosing = true;
        button.disabled = true;
        try {
            const plan = await request("PreviewFinancialDeletion", key, "GET", false);
            if (!plan || signal.aborted || !isModalOpen(historyModal)) return;
            const receipt = Boolean(key.receiptId);
            const title = receipt ? "رسید انبار" : "فاکتور خرید";
            const blocked = plan.blockers.length > 0;
            const content = make("div", null, "text-start");
            content.append(make("p", blocked ? `${title} شماره ${plan.number} قابل حذف نیست.`
                : `${title} شماره ${plan.number} از «${software.title}» حذف شود؟`, "mb-2"));
            if (blocked) {
                const list = make("ul", null, "mb-0 small");
                plan.blockers.forEach(message => list.append(make("li", message)));
                content.append(list);
            } else {
                if (plan.voucherNumber != null)
                    content.append(make("p", `سند حسابداری شماره ${plan.voucherNumber} هم حذف می‌شود.`, "mb-2 small"));
                if (receipt)
                    content.append(make("p", "موجودی انبار و مانده اقلام فاکتور خرید به‌روزرسانی می‌شوند.", "mb-0 small"));
            }
            const answer = await Swal.fire({
                target: historyModal, title: "حذف " + title + " از مقصد", html: content, icon: "warning",
                showCancelButton: !blocked, focusCancel: true,
                confirmButtonText: blocked ? "بستن" : "حذف " + title, cancelButtonText: "انصراف",
            });
            if (blocked || !answer.isConfirmed || signal.aborted || !isModalOpen(historyModal)) return;
            try {
                const result = await request("DeleteFinancialDocument", { key, token: plan.token });
                if (result?.deleted) toastifySuccess(title + " از نرم‌افزار مقصد حذف شد.");
            } finally {
                if (!signal.aborted) await loadHistory();
            }
        } finally {
            choosing = false;
            button.disabled = false;
        }
    };

    const openReceiptEdit = async (software, key) => {
        if (choosing || busy || isModalOpen(wizard)) return;
        choosing = true;
        try {
            const data = await request("GetFinancialReceiptEdit", key, "GET", false);
            if (!data || signal.aborted || !isModalOpen(historyModal)) return;
            const { impact } = data;
            const locked = impact.blockers.length > 0;
            const { content, toolbar, list } = createExternalItemMatching();
            toolbar.remove();
            const general = make("div", null, "d-grid gap-2");
            content.prepend(general);
            const date = field(general, "تاریخ رسید انبار", make("input", null, "sdate form-control form-control-sm"));
            date.dir = "ltr";
            date.autocomplete = "off";
            date.maxLength = 10;
            date.value = data.dateText;
            date.disabled = locked;
            date.setAttribute("data-datepicker-change", "");
            const getDate = () => toEnglishDigits(date.value.trim());
            date.addEventListener("input", updateButtons);
            date.addEventListener("change", updateButtons);
            const stock = field(general, "انبار مقصد", make("select", null, "show-dropdown-arrow"));
            const options = data.stocks.map(searchableOption);
            if (!options.some(x => x.value === String(data.stockId)))
                options.push(searchableOption({ id: data.stockId, title: data.stockTitle }));
            const description = optionalDescription(general, "توضیحات سند", data.description, 4000);
            description.disabled = locked;
            const rows = data.rows.map(row => {
                const target = appendExternalItemMatchRow(list, row.itemLabel, row.quantity, row.unit, "مقدار:");
                const note = optionalDescription(target, "توضیحات قلم", row.description, 4000);
                note.disabled = locked;
                return { id: row.id, note };
            });
            destinations = [software];
            position = 0;
            stage({
                software, title: "ویرایش انتقال رسید انبار", icon: "fa-pencil-alt", large: true,
                subtitle: locked ? impact.blockers.join(" ") : "تاریخ، انبار و توضیحات رسید را بررسی کنید.",
                step: "رسید شماره " + impact.number, content,
                backAction: () => ModalToggle(wizard, "hide"), backText: locked ? "بستن" : "انصراف",
                confirmText: "ذخیره تغییرات",
                enabled: () => !locked && !!stock.value && /^[0-9]{4}\/[0-9]{2}\/[0-9]{2}$/.test(getDate()),
                confirmAction: async () => {
                    const stockId = Number(stock.value);
                    const details = [];
                    if (impact.voucherNumber != null) {
                        const action = data.autoVoucher ? "بازسازی می‌شود." : "حذف می‌شود.";
                        details.push(`سند حسابداری شماره ${impact.voucherNumber} ${action}`);
                    } else if (data.autoVoucher) {
                        details.push("سند حسابداری صادر می‌شود.");
                    }
                    if (stockId !== data.stockId)
                        details.push("موجودی هر دو انبار به‌روز و شمارهٔ رسید در انبار مقصد تعیین می‌شود.");
                    if (!await historyCard.confirmEdit(wizard, "تغییرات رسید انبار شماره " + impact.number + " ذخیره شود؟", details)
                        || signal.aborted) return;
                    const result = await request("EditFinancialReceipt", {
                        key, token: impact.token, dateText: getDate(), stockId, autoVoucher: data.autoVoucher,
                        description: description.value, rowDescriptions: Object.fromEntries(rows.map(x => [x.id, x.note.value])),
                    });
                    if (!result?.saved) return;
                    toastifySuccess("تغییرات رسید انبار در نرم‌افزار مقصد ذخیره شد.");
                    closing = true;
                    ModalToggle(wizard, "hide");
                },
            });
            setupSearchableSelect(stock, "جستجو و انتخاب انبار", options, data.stockId);
            if (locked) stock.tomselect.disable();
            else dateTimePicker(stageContainer);
            updateButtons();
            ModalToggle(wizard, "show");
        } finally {
            choosing = false;
        }
    };

    const openPurchaseEdit = async (software, key) => {
        if (choosing || busy || isModalOpen(wizard)) return;
        choosing = true;
        try {
            const data = await request("GetFinancialPurchaseEdit", key, "GET", false);
            if (!data || signal.aborted || !isModalOpen(historyModal)) return;
            const locked = Boolean(data.restriction);
            const lookupsLocked = locked || data.hasLinkedReceipt;
            const { content, toolbar, list } = createExternalItemMatching();
            const general = make("div", null, "d-grid gap-2");
            content.prepend(general);
            toolbar.remove();
            const vendor = field(general, "تأمین‌کننده مقصد", make("select", null, "show-dropdown-arrow"));
            if (!lookupsLocked && !data.vendorId && data.vendorName)
                general.append(fields("تأمین‌کننده فعلی", data.vendorName));
            const description = optionalDescription(general, "توضیحات سند", data.description, 400);
            description.disabled = locked;
            const options = data.items.map(searchableOption);
            const rows = data.rows.map(row => {
                const target = appendExternalItemMatchRow(list, row.itemLabel, row.quantity, row.unit, "مقدار:");
                const item = field(target, "کالای معادل در نرم‌افزار مالی", make("select", null, "show-dropdown-arrow"));
                const note = optionalDescription(target, "توضیحات قلم", row.description, 500);
                note.disabled = locked;
                target.prepend(make("div", "قیمت واحد " + row.fee.toLocaleString() + " ریال", "small"));
                return { row, item, note };
            });
            destinations = [software];
            position = 0;
            stage({
                software, title: "ویرایش انتقال فاکتور خرید", icon: "fa-pencil-alt", large: true,
                subtitle: data.restriction ?? (data.hasLinkedReceipt
                    ? "برای این فاکتور رسید انبار ثبت شده است. توضیحات سند و اقلام قابل ویرایش است."
                    : "تأمین‌کننده، کالاهای معادل و توضیحات انتقال را بررسی کنید."),
                step: "فاکتور شماره " + data.number, content,
                backAction: () => ModalToggle(wizard, "hide"), backText: locked ? "بستن" : "انصراف",
                confirmText: "ذخیره تغییرات",
                enabled: () => !locked && rows.length > 0 &&
                    (data.hasLinkedReceipt || Boolean(vendor.value && rows.every(x => x.item.value))),
                confirmAction: async () => {
                    if (locked) return;
                    const result = await request("EditFinancialPurchase", {
                        key, token: data.token, description: description.value,
                        ...(!lookupsLocked && { vendorId: Number(vendor.value) }),
                        items: rows.map(x => ({
                            rowNumber: x.row.rowNumber, description: x.note.value,
                            ...(!lookupsLocked && { itemId: Number(x.item.value) }),
                        })),
                    });
                    if (!result?.saved) return;
                    toastifySuccess("تغییرات فاکتور خرید در نرم‌افزار مقصد ذخیره شد.");
                    closing = true;
                    ModalToggle(wizard, "hide");
                },
            });
            const mount = (control, choices, id, text) => {
                const current = String(id);
                setupSearchableSelect(control, "جستجو و انتخاب", choices.some(x => x.value === current)
                    ? choices : [...choices, { value: current, text: text || "—", searchText: normalizeOptionSearch(text ?? "") }], id);
                if (lookupsLocked) control.tomselect.disable();
            };
            mount(vendor, data.vendors.map(searchableOption), data.vendorId || (lookupsLocked ? -1 : 0), data.vendorName);
            rows.forEach(({ row, item }) => mount(item, options, row.itemId, row.itemLabel));
            updateButtons();
            ModalToggle(wizard, "show");
        } finally {
            choosing = false;
        }
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
        const records = result.softwares.filter(row => row.purchase || row.receipts.length);
        historyUi.render(records, {
            emptyText: "این فاکتور خرید هنوز در نرم‌افزار مالی ثبت نشده است.",
            card: row => {
                const { purchase, receipts } = row;
                const invoice = purchase?.document;
                let related;
                if (invoice && receipts.length === 0) {
                    const register = historyCard.action("ثبت رسید انبار");
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
                    related = { title: "رسید انبار", action: register };
                }
                if (receipts.length) {
                    const entries = receipts.map(receipt => {
                        const document = receipt.document;
                        const unrelated = !!invoice && !!document && document.basePurchaseId !== invoice.id;
                        const missing = !document || unrelated;
                        return historyCard.relatedItem({
                            title: "رسید انبار",
                            missing,
                            fields: [["شماره رسید انبار", missing ? null : document.number],
                                ["تحویل‌دهنده", missing ? null : document.partyTitle]],
                            registration: unrelated ? null : { date: receipt.registeredAt, by: receipt.registeredBy },
                            status: unrelated ? "رسید ثبت‌شده به این فاکتور خرید مربوط نیست." :
                                !document ? "رسید انبار در نرم‌افزار مقصد پیدا نشد." : "",
                            discrepancy: invoice && purchase.mappingId && !missing && receipt.requiresSynchronization
                                ? discrepancy(row, purchase, receipt) : null,
                            action: !document && receipt.mappingId ? missingHistory(row, receipt, true) : null,
                            operations: invoice && purchase.mappingId && !missing
                                ? destinationOperations(row, purchase, receipt) : [],
                        });
                    });
                    related = { title: "رسید انبار", entries, open: true };
                }
                return {
                    title: row.title, missing: !invoice, related,
                    fields: [["شماره فاکتور خرید", invoice?.number], ["تأمین‌کننده", invoice?.partyTitle]],
                    registration: { date: purchase?.registeredAt, by: purchase?.registeredBy },
                    status: !invoice ? (purchase ? "فاکتور خرید در نرم‌افزار مقصد پیدا نشد." :
                        "سابقه فاکتور خرید برای این مقصد موجود نیست.") : "",
                    discrepancy: invoice && purchase.mappingId && purchase.requiresSynchronization
                        ? discrepancy(row, purchase) : null,
                    operations: invoice && purchase.mappingId ? destinationOperations(row, purchase) : [],
                    action: purchase && !invoice && purchase.mappingId ? missingHistory(row, purchase, false) : null,
                };
            },
        });
        send.disabled = result.softwares.length === 0;
        send.onclick = () => choose(result.softwares);
    };

    const choose = async softwares => {
        if (choosing || isModalOpen(wizard)) return;
        choosing = true;
        try {
            const selected = await ExternalFinancialHistory.chooseDestinations({
                ids: sourceId, mapType: 8, signal,
            });
            if (!selected?.length || signal.aborted) return;
            destinations = selected.map(id => softwares.find(software => software.id === id))
                .filter(Boolean);
            if (!destinations.length) return;
            position = 0;
            await prepare(destinations[0], false, true);
        } finally {
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
        content.append(fields("تأمین‌کننده در ERP", source.supplierTxt));
        const select = field(content, "تأمین‌کننده معادل در نرم‌افزار مالی", make("select", null, "show-dropdown-arrow"), true);
        stage({ software, title: "تطبیق تأمین‌کننده", subtitle: "اتصال تأمین‌کننده ERP به نرم‌افزار مالی",
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
        const stock = field(content, "انبار", make("select", null, "show-dropdown-arrow"), true);
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
        const { content, toolbar, add, refresh, list } = createExternalItemMatching();
        if (receipt) toolbar.remove();
        const rows = source.items.map(row => {
            const previous = data.items?.find(x => x.supplyFactorItemId === row.id);
            const target = appendExternalItemMatchRow(list, row.product, row.weightCount, row.unitTxt, receipt ? "مقدار:" : null);
            const select = receipt ? null : field(target, "کالای معادل در نرم‌افزار مالی",
                make("select", null, "show-dropdown-arrow"), true);
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
        if (!receipt) refresh.addEventListener("click", () => {
            if (busy || !content.isConnected) return;
            return refreshExternalItemMatching(refresh, () => {
                captureDraft();
                return loadItemStage(data, source, software, true);
            });
        });
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
        const columns = ["ردیف", ...(receipt ? [] : ["محصول ERP"]), "کالای مقصد", "مقدار", "مبلغ کالا",
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
            date.required = true;
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
    enableMomentumScrollScope(wizard, { signal });
    historyHeading.textContent = "سوابق فاکتور خرید";
    sendLabel.textContent = "ارسال فاکتور خرید به نرم‌افزار مالی";
    historyUi.show({
        title: "مدیریت ارسال فاکتور تأمین",
        loadingText: "در حال دریافت سوابق ثبت...",
    });
    await loadHistory();
}

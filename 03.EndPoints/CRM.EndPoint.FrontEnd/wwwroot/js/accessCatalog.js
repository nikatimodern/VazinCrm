(() => {
    "use strict";

    const root = document.getElementById("accessCatalog");
    if (!root || root.dataset.initialized === "1") return;
    root.dataset.initialized = "1";

    const page = typeof PageRuntime !== "undefined" ? PageRuntime.current : null;
    const lifetime = new AbortController();
    const signal = lifetime.signal;
    const entityForm = root.querySelector("[data-entity-form]");
    const permissionForm = root.querySelector("[data-permission-form]");
    const entityModal = root.querySelector("#acEntityModal");
    const permissionModal = root.querySelector("#acPermissionModal");
    const entityList = root.querySelector("[data-entities]");
    const permissionList = root.querySelector("[data-permissions]");
    const modelInput = root.querySelector("[data-model-select]");
    const csrf = root.querySelector("input[name=__RequestVerificationToken]").value;
    let entities = [], permissions = [], currentEntityId = null, entityEditId = null, permissionEditId = null;
    let saving = false, requestGeneration = 0, modelSelect = null;
    const cleanupScroll = typeof enableMomentumScroll === "function" ? enableMomentumScroll(entityList) : null;

    const dispose = () => {
        lifetime.abort();
        requestGeneration++;
        modelSelect?.destroy();
        if (typeof cleanupScroll === "function") cleanupScroll();
        bootstrap.Modal.getInstance(entityModal)?.dispose();
        bootstrap.Modal.getInstance(permissionModal)?.dispose();
    };
    if (page) page.cleanups.push(dispose);
    else window.addEventListener("pagehide", dispose, { once: true });

    const field = (form, name) => form.elements.namedItem(name);
    const text = (tag, value, cls) => {
        const el = document.createElement(tag);
        if (value !== undefined && value !== null) el.textContent = String(value);
        if (cls) el.className = cls;
        return el;
    };
    const actionButton = (label, action, id, danger = false) => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "ac-icon-button" + (danger ? " is-danger" : "");
        btn.dataset.action = action;
        btn.dataset.id = id;
        btn.setAttribute("aria-label", label);
        btn.title = label;
        const icon = text("i");
        icon.className = danger ? "fas fa-trash-alt" : "fas fa-pen";
        icon.setAttribute("aria-hidden", "true");
        btn.append(icon);
        return btn;
    };
    const selectedEntity = () => entities.find(x => x.id === currentEntityId);
    const showEmpty = (target, message, icon = "fa-shield-alt") => {
        const container = text("div", null, "ac-empty");
        container.append(text("i", null, "fas " + icon), text("p", message));
        target.replaceChildren(container);
    };
    const showError = (formType, message) => {
        const el = root.querySelector("[data-" + formType + "-error]");
        el.textContent = message;
        el.hidden = !message;
    };
    const post = async (action, data, id) => sendApiRequest({
        url: "/AccessCatalogAdmin/" + action,
        method: "POST",
        params: id ? { id } : null,
        data,
        headers: { RequestVerificationToken: csrf },
        successMsg: null,
        showLoading: false,
        showError: false,
        onError: err => {
            const message = err.response?.data?.message || "عملیات انجام نشد. دوباره تلاش کنید.";
            toastifyError(message);
            showError(action.includes("Permission") ? "permission" : "entity", message);
        }
    });

    function renderEntities() {
        const search = root.querySelector("[data-entity-search]").value.trim().toLocaleLowerCase();
        const filtered = entities.filter(x => [x.title, x.key, x.modelFullName].some(t => (t || "").toLocaleLowerCase().includes(search)));
        root.querySelector("[data-entity-count]").textContent = String(entities.length);
        entityList.replaceChildren();
        if (!filtered.length) {
            showEmpty(entityList, entities.length ? "انتیتی مطابق جست‌وجو پیدا نشد." : "هنوز انتیتی تعریف نشده است.", "fa-folder-open");
            return;
        }
        for (const item of filtered) {
            const row = text("div", null, "ac-entity-item" + (item.id === currentEntityId ? " is-selected" : ""));
            const select = document.createElement("button");
            select.type = "button";
            select.className = "ac-entity-select";
            select.dataset.selectEntity = item.id;
            select.setAttribute("aria-pressed", String(item.id === currentEntityId));
            select.setAttribute("aria-label", "نمایش دسترسی‌های " + item.title);
            const meta = text("span", null, "ac-entity-meta");
            meta.append(text("strong", item.title), text("small", item.key + " · " + item.permissionCount + " دسترسی"));
            const badge = text("span", item.isActive ? "فعال" : "غیرفعال", "ac-pill" + (item.isActive ? "" : " is-off"));
            select.append(meta, badge);
            const actions = text("div", null, "ac-entity-actions");
            actions.append(
                actionButton("ویرایش انتیتی " + item.title, "edit-entity", item.id),
                actionButton("حذف انتیتی " + item.title, "delete-entity", item.id, true)
            );
            row.append(select, actions);
            entityList.append(row);
        }
    }

    function renderPermissions() {
        permissionList.replaceChildren();
        const item = selectedEntity();
        const hasEntity = !!item;
        root.querySelector("[data-selected-title]").textContent = hasEntity ? "دسترسی‌های " + item.title : "دسترسی‌های انتیتی";
        root.querySelector("[data-selected-key]").textContent = hasEntity ? item.key + " · " + item.modelFullName : "برای شروع یک انتیتی انتخاب کنید.";
        root.querySelector("[data-action='new-permission']").disabled = !hasEntity;
        if (!hasEntity) { showEmpty(permissionList, "یک انتیتی را از فهرست انتخاب کنید."); return; }
        if (!permissions.length) {
            showEmpty(permissionList, "هنوز دسترسی تعریف نشده است. می‌توانید یکی از مجوزهای پیشنهادی را انتخاب کنید.", "fa-key");
            const presets = text("div", null, "ac-presets");
            for (const item of [{ key: "View", title: "مشاهده" }, { key: "Create", title: "ایجاد" },
                                { key: "Edit", title: "ویرایش" }, { key: "Delete", title: "حذف" }]) {
                const btn = text("button", "+ " + item.title, "ac-btn ac-btn-muted");
                btn.type = "button";
                btn.dataset.action = "preset-permission";
                btn.dataset.key = item.key;
                btn.dataset.title = item.title;
                presets.append(btn);
            }
            permissionList.append(presets);
            return;
        }
        for (const permission of permissions) {
            const card = text("div", null, "ac-permission-item");
            const info = text("div");
            info.append(text("strong", permission.title), text("small", permission.fullKey));
            if (permission.description) info.append(text("p", permission.description, "ac-muted"));
            const actions = text("div", null, "ac-permission-actions");
            if (!permission.isActive) actions.append(text("span", "غیرفعال", "ac-pill is-off"));
            actions.append(actionButton("ویرایش دسترسی", "edit-permission", permission.id),
                           actionButton("حذف دسترسی", "delete-permission", permission.id, true));
            card.append(info, actions);
            permissionList.append(card);
        }
    }

    async function loadModels() {
        const data = await sendApiRequest({ url: "/AccessCatalogAdmin/Models", showLoading: false });
        if (!Array.isArray(data) || signal.aborted) return;
        modelInput.replaceChildren(new Option("انتخاب مدل...", ""));
        for (const model of data) {
            const option = new Option(model.modelName + "  (" + model.dbContextName + ")", model.dbContextName + "|" + model.modelFullName);
            modelInput.append(option);
        }
        if (typeof TomSelect === "function") {
            modelSelect = new TomSelect(modelInput, {
                create: false, allowEmptyOption: true, maxItems: 1, searchField: ["text", "value"]
            });
        }
    }

    async function loadEntities(preserve = true) {
        const previous = preserve ? currentEntityId : null;
        const data = await sendApiRequest({ url: "/AccessCatalogAdmin/Entities", showLoading: false });
        if (!Array.isArray(data) || signal.aborted) return;
        entities = data;
        const id = entities.some(x => x.id === previous) ? previous : (entities[0]?.id || null);
        currentEntityId = null;
        permissions = [];
        if (id) await selectEntity(id);
        else { renderEntities(); renderPermissions(); }
    }

    async function selectEntity(id) {
        currentEntityId = id;
        permissions = [];
        renderEntities();
        renderPermissions();
        if (!id) return;
        const generation = ++requestGeneration;
        const data = await sendApiRequest({ url: "/AccessCatalogAdmin/Permissions", params: { entityId: id }, showLoading: false });
        if (signal.aborted || generation !== requestGeneration || currentEntityId !== id) return;
        permissions = Array.isArray(data) ? data : [];
        renderPermissions();
    }

    function setModel(value, locked) {
        if (modelSelect) {
            modelSelect.enable();
            modelSelect.setValue(value || "", true);
            if (locked) modelSelect.disable();
        } else {
            modelInput.disabled = false;
            modelInput.value = value || "";
            modelInput.disabled = locked;
        }
    }

    function openEntityEditor(entity = null) {
        entityEditId = entity?.id || null;
        entityForm.reset();
        showError("entity", "");
        root.querySelector("#acEntityModalTitle").textContent = entity ? "ویرایش انتیتی" : "انتیتی جدید";
        setModel(entity ? entity.dbContextName + "|" + entity.modelFullName : "", !!entity);
        field(entityForm, "key").value = entity?.key || "";
        field(entityForm, "key").readOnly = !!entity;
        field(entityForm, "title").value = entity?.title || "";
        field(entityForm, "description").value = entity?.description || "";
        field(entityForm, "isActive").checked = entity ? entity.isActive : true;
        ModalToggle(entityModal, "show");
    }

    function updateKeyPreview() {
        root.querySelector("[data-key-preview]").textContent = (selectedEntity()?.key || "") + "." + field(permissionForm, "key").value.trim();
    }

    function openPermissionEditor(permission = null) {
        const entity = selectedEntity();
        if (!entity) return;
        permissionEditId = permission?.id || null;
        permissionForm.reset();
        showError("permission", "");
        root.querySelector("#acPermissionModalTitle").textContent = permission ? "ویرایش دسترسی" : "دسترسی جدید";
        root.querySelector("[data-permission-entity]").textContent = entity.title + " (" + entity.key + ")";
        field(permissionForm, "key").value = permission?.key || "";
        field(permissionForm, "key").readOnly = !!permission;
        field(permissionForm, "title").value = permission?.title || "";
        field(permissionForm, "description").value = permission?.description || "";
        field(permissionForm, "isActive").checked = permission ? permission.isActive : true;
        updateKeyPreview();
        ModalToggle(permissionModal, "show");
    }

    async function saveEntity(event) {
        event.preventDefault();
        if (saving || !entityForm.reportValidity()) return;
        const model = modelInput.value.split("|");
        if (model.length !== 2) { showError("entity", "یک مدل معتبر انتخاب کنید."); return; }
        saving = true;
        const submit = entityForm.querySelector("[type=submit]");
        submit.disabled = true;
        try {
            const response = await post("SaveEntity", {
                dbContextName: model[0], modelFullName: model[1],
                key: field(entityForm, "key").value.trim(),
                title: field(entityForm, "title").value.trim(),
                description: field(entityForm, "description").value.trim(),
                isActive: field(entityForm, "isActive").checked
            }, entityEditId);
            if (response?.success) {
                ModalToggle(entityModal, "hide");
                toastifySuccess("انتیتی ذخیره شد.");
                currentEntityId = response.id;
                await loadEntities();
            } else if (response === null && !root.querySelector("[data-entity-error]").textContent)
                showError("entity", "ذخیره انجام نشد.");
        } finally { saving = false; submit.disabled = false; }
    }

    async function savePermission(event) {
        event.preventDefault();
        if (saving || !permissionForm.reportValidity() || !selectedEntity()) return;
        saving = true;
        const submit = permissionForm.querySelector("[type=submit]");
        submit.disabled = true;
        try {
            const response = await post("SavePermission", {
                accessEntityId: currentEntityId,
                key: field(permissionForm, "key").value.trim(),
                title: field(permissionForm, "title").value.trim(),
                description: field(permissionForm, "description").value.trim(),
                isActive: field(permissionForm, "isActive").checked
            }, permissionEditId);
            if (response?.success) {
                ModalToggle(permissionModal, "hide");
                toastifySuccess("دسترسی ذخیره شد.");
                await loadEntities();
            } else if (response === null && !root.querySelector("[data-permission-error]").textContent)
                showError("permission", "ذخیره انجام نشد.");
        } finally { saving = false; submit.disabled = false; }
    }

    async function deleteItem(kind, id) {
        if (saving) return;
        const isPermission = kind === "Permission";
        const allowed = await confirmAlert({
            title: isPermission ? "حذف دسترسی" : "حذف انتیتی",
            text: isPermission ? "آیا این دسترسی حذف شود؟" : "انتیتی فقط زمانی حذف می‌شود که هیچ دسترسی مرتبطی نداشته باشد.",
            confirmText: "حذف", cancelText: "انصراف", icon: "warning"
        });
        if (!allowed || signal.aborted) return;
        saving = true;
        try {
            const response = await post("Delete" + kind, null, id);
            if (response?.success) {
                toastifySuccess("حذف انجام شد.");
                await loadEntities();
            }
        } finally { saving = false; }
    }

    root.addEventListener("click", event => {
        const target = event.target.closest("[data-action]");
        if (!target) {
            const select = event.target.closest("[data-select-entity]");
            if (select) void selectEntity(select.dataset.selectEntity);
            return;
        }
        if (target.disabled) return;
        const action = target.dataset.action;
        if (action === "new-entity") openEntityEditor();
        else if (action === "edit-entity") openEntityEditor(entities.find(x => x.id === target.dataset.id));
        else if (action === "delete-entity") void deleteItem("Entity", target.dataset.id);
        else if (action === "new-permission") openPermissionEditor();
        else if (action === "preset-permission") {
            openPermissionEditor();
            field(permissionForm, "key").value = target.dataset.key;
            field(permissionForm, "title").value = target.dataset.title;
            updateKeyPreview();
        }
        else if (action === "edit-permission") openPermissionEditor(permissions.find(x => x.id === target.dataset.id));
        else if (action === "delete-permission") void deleteItem("Permission", target.dataset.id);
        else if (action === "reload") void loadEntities();
    }, { signal });
    root.querySelector("[data-entity-search]").addEventListener("input", renderEntities, { signal });
    modelInput.addEventListener("change", () => {
        if (entityEditId || field(entityForm, "key").value) return;
        const name = modelInput.value.split("|")[1]?.split(".").pop();
        if (name) field(entityForm, "key").value = name;
    }, { signal });
    field(permissionForm, "key").addEventListener("input", updateKeyPreview, { signal });
    entityForm.addEventListener("submit", event => { void saveEntity(event); }, { signal });
    permissionForm.addEventListener("submit", event => { void savePermission(event); }, { signal });

    void (async () => {
        await loadModels();
        if (!signal.aborted) await loadEntities(false);
    })();
})();

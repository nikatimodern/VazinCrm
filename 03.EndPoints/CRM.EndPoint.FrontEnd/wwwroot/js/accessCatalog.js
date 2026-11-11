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
    const rolesList = root.querySelector("[data-roles-list]");
    const roleGroups = root.querySelector("[data-role-groups]");
    let roles = [], roleCatalog = [], currentRoleId = null, activeTab = "catalog";
    let roleChosen = new Set(), roleOriginal = new Set();
    let roleReady = false, roleSaving = false, roleLoadGeneration = 0, roleSelectionGeneration = 0;
    const usersList = root.querySelector("[data-users-list]");
    const userPermissions = root.querySelector("[data-user-permissions]");
    let userAvailableRoles = [], userCatalog = [], userGrantSources = new Map(), userPageItems = [];
    let userPage = 1, userPages = 1, userCount = 0, userSearchTerm = "";
    let currentUserId = null, userRoleIds = new Set(), userOriginalRoles = new Set();
    let userOverrides = new Map(), userOriginalOverrides = new Map();
    let userReady = false, userSaving = false, userLoading = false;
    let userTabGeneration = 0, userPageGeneration = 0, userSelectionGeneration = 0, userSearchTimer = null;
    const cleanupScroll = typeof enableMomentumScroll === "function" ? enableMomentumScroll(entityList) : null;
    const cleanupRoleListScroll = typeof enableMomentumScroll === "function" ? enableMomentumScroll(rolesList) : null;
    const cleanupRoleGroupsScroll = typeof enableMomentumScroll === "function" ? enableMomentumScroll(roleGroups) : null;
    const cleanupUsersScroll = typeof enableMomentumScroll === "function" ? enableMomentumScroll(usersList) : null;
    const cleanupUserPermissionsScroll = typeof enableMomentumScroll === "function" ? enableMomentumScroll(userPermissions) : null;

    const dispose = () => {
        lifetime.abort();
        requestGeneration++;
        modelSelect?.destroy();
        if (typeof cleanupScroll === "function") cleanupScroll();
        if (typeof cleanupRoleListScroll === "function") cleanupRoleListScroll();
        if (typeof cleanupRoleGroupsScroll === "function") cleanupRoleGroupsScroll();
        if (typeof cleanupUsersScroll === "function") cleanupUsersScroll();
        if (typeof cleanupUserPermissionsScroll === "function") cleanupUserPermissionsScroll();
        if (userSearchTimer !== null) clearTimeout(userSearchTimer);
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
            showError(action === "SaveRolePermissions" ? "role" :
                action === "SaveUserAccess" ? "user" :
                action.includes("Permission") ? "permission" : "entity", message);
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


    // This tab only stores role assignments; enforcing them is intentionally a separate phase.
    const currentRole = () => roles.find(x => x.id === currentRoleId);
    const roleDirty = () => roleChosen.size !== roleOriginal.size ||
        [...roleChosen].some(id => !roleOriginal.has(id));

    function renderRoles() {
        const query = root.querySelector("[data-role-search]").value.trim().toLocaleLowerCase();
        rolesList.replaceChildren();
        root.querySelector("[data-role-count]").textContent = String(roles.length);
        const visible = roles.filter(x => (x.name || "").toLocaleLowerCase().includes(query));
        if (!visible.length) {
            showEmpty(rolesList, roles.length ? "نقشی با این نام پیدا نشد." : "نقشی تعریف نشده است.", "fa-user-tag");
            return;
        }
        for (const role of visible) {
            const button = text("button", null, "ac-role-item" + (role.id === currentRoleId ? " is-selected" : ""));
            button.type = "button";
            button.dataset.selectRole = role.id;
            button.setAttribute("aria-pressed", String(role.id === currentRoleId));
            button.append(text("strong", role.name));
            rolesList.append(button);
        }
    }

    function syncRoleState() {
        const total = roleChosen.size;
        root.querySelector("[data-role-selected]").textContent = total + " دسترسی انتخاب شده";
        root.querySelector("[data-action='save-role']").disabled = !roleReady || roleSaving || !roleDirty();
        root.querySelector("[data-action='reset-role']").disabled = !roleReady || roleSaving || !roleDirty();
        for (const input of roleGroups.querySelectorAll("[data-role-permission-id], [data-role-entity-toggle]"))
            input.disabled = !roleReady || roleSaving;
        root.querySelector("[data-role-subtitle]").textContent = !currentRole() ? "برای شروع یک نقش انتخاب کنید." :
            !roleReady ? "در حال دریافت مجوزها..." :
            roleDirty() ? "تغییرات ذخیره‌نشده" : "همه تغییرات ذخیره شده‌اند.";
        for (const group of roleGroups.querySelectorAll("[data-role-entity]")) {
            const entity = roleCatalog.find(x => x.id === group.dataset.roleEntity);
            if (!entity) continue;
            const count = entity.permissions.filter(p => roleChosen.has(p.id)).length;
            const selectAll = group.querySelector("[data-role-entity-toggle]");
            selectAll.checked = entity.permissions.length > 0 && count === entity.permissions.length;
            selectAll.indeterminate = count > 0 && count < entity.permissions.length;
            group.querySelector("[data-role-group-count]").textContent = count + " از " + entity.permissions.length;
        }
    }

    function renderRolePermissions() {
        roleGroups.replaceChildren();
        root.querySelector("[data-role-title]").textContent = currentRole() ? "دسترسی‌های " + currentRole().name : "دسترسی‌های نقش";
        if (!currentRole()) {
            showEmpty(roleGroups, "یک نقش را انتخاب کنید.", "fa-user-shield");
            syncRoleState();
            return;
        }
        if (!roleReady) {
            showEmpty(roleGroups, "در حال دریافت دسترسی‌های نقش...", "fa-spinner");
            syncRoleState();
            return;
        }
        const term = root.querySelector("[data-role-permission-search]").value.trim().toLocaleLowerCase();
        const visible = roleCatalog.map(entity => {
            const matchedEntity = [entity.title, entity.key].some(x => x.toLocaleLowerCase().includes(term));
            const selected = matchedEntity ? entity.permissions :
                entity.permissions.filter(p => [p.title, p.key].some(x => x.toLocaleLowerCase().includes(term)));
            return { entity, selected };
        }).filter(x => x.selected.length > 0);
        if (!visible.length) {
            showEmpty(roleGroups, roleCatalog.length ? "دسترسی مطابق جست‌وجو پیدا نشد." :
                "هنوز انتیتی و دسترسی فعالی تعریف نشده است.", "fa-key");
            syncRoleState();
            return;
        }
        for (const {entity, selected} of visible) {
            const section = text("section", null, "ac-role-group");
            section.dataset.roleEntity = entity.id;
            const header = text("header", null, "ac-role-group-header");
            const allLabel = text("label", null, "ac-role-group-all");
            const toggle = document.createElement("input");
            toggle.type = "checkbox";
            toggle.dataset.roleEntityToggle = entity.id;
            toggle.setAttribute("aria-label", "انتخاب همه مجوزهای " + entity.title);
            const heading = text("span");
            heading.append(text("strong", entity.title), text("small", entity.key));
            allLabel.append(toggle, heading);
            const groupCount = text("span", null, "ac-role-group-count");
            groupCount.dataset.roleGroupCount = entity.id;
            header.append(allLabel, groupCount);
            const grid = text("div", null, "ac-role-choices");
            for (const permission of selected) {
                const label = text("label", null, "ac-role-choice");
                const input = document.createElement("input");
                input.type = "checkbox";
                input.dataset.rolePermissionId = permission.id;
                input.checked = roleChosen.has(permission.id);
                const description = text("span");
                description.append(text("strong", permission.title), text("small", permission.key));
                label.append(input, description);
                grid.append(label);
            }
            section.append(header, grid);
            roleGroups.append(section);
        }
        syncRoleState();
    }

    async function selectRole(roleId) {
        if (!roles.some(x => x.id === roleId)) return;
        const generation = ++roleSelectionGeneration;
        currentRoleId = roleId;
        roleReady = false;
        roleChosen = new Set();
        roleOriginal = new Set();
        showError("role", "");
        renderRoles();
        renderRolePermissions();
        const ids = await sendApiRequest({
            url: "/AccessCatalogAdmin/RolePermissions",
            params: { roleId },
            showLoading: false
        });
        if (signal.aborted || generation !== roleSelectionGeneration || currentRoleId !== roleId) return;
        if (!Array.isArray(ids)) {
            showEmpty(roleGroups, "دریافت دسترسی‌های نقش موفق نبود. مجدداً تلاش کنید.", "fa-exclamation-circle");
            return;
        }
        const permitted = new Set(roleCatalog.flatMap(x => x.permissions.map(p => p.id)));
        roleChosen = new Set(ids.filter(id => permitted.has(id)));
        roleOriginal = new Set(roleChosen);
        roleReady = true;
        renderRolePermissions();
    }

    async function confirmDiscardRoleChanges() {
        if (!roleReady || !roleDirty()) return true;
        return !!(await confirmAlert({
            title: "تغییرات ذخیره نشده",
            text: "تغییرات مجوزهای این نقش ذخیره نشده‌اند. بدون ذخیره ادامه می‌دهید؟",
            confirmText: "ادامه بدون ذخیره",
            cancelText: "انصراف",
            icon: "warning"
        }));
    }

    async function loadRoleTab() {
        const generation = ++roleLoadGeneration;
        ++roleSelectionGeneration;
        roleReady = false;
        roleChosen = new Set();
        roleOriginal = new Set();
        currentRoleId = null;
        root.querySelector("[data-role-title]").textContent = "دسترسی‌های نقش";
        showEmpty(rolesList, "در حال دریافت نقش‌ها...", "fa-spinner");
        showEmpty(roleGroups, "در حال دریافت انتیتی‌ها و دسترسی‌ها...", "fa-spinner");
        syncRoleState();
        const [loadedRoles, catalog] = await Promise.all([
            sendApiRequest({ url: "/AccessCatalogAdmin/Roles", showLoading: false }),
            sendApiRequest({ url: "/AccessCatalogAdmin/RoleCatalog", showLoading: false })
        ]);
        if (signal.aborted || generation !== roleLoadGeneration) return;
        if (!Array.isArray(loadedRoles) || !Array.isArray(catalog)) {
            showEmpty(rolesList, "دریافت نقش‌ها یا دسترسی‌ها موفق نبود.", "fa-exclamation-circle");
            showEmpty(roleGroups, "برای دریافت دوباره از دکمه تازه‌سازی استفاده کنید.", "fa-exclamation-circle");
            return;
        }
        roles = loadedRoles;
        roleCatalog = catalog;
        renderRoles();
        if (roles.length) await selectRole(roles[0].id);
        else renderRolePermissions();
    }

    async function switchMainTab(next) {
        if (next === activeTab || roleSaving || userSaving) return;
        if (activeTab === "roles" && !await confirmDiscardRoleChanges()) {
            root.querySelector("[data-main-tab='roles']").focus();
            return;
        }
        if (activeTab === "users" && !await confirmDiscardUserChanges()) {
            root.querySelector("[data-main-tab='users']").focus();
            return;
        }
        if (activeTab === "users" && userSearchTimer !== null) {
            clearTimeout(userSearchTimer);
            userSearchTimer = null;
        }
        activeTab = next;
        for (const tab of root.querySelectorAll("[data-main-tab]")) {
            const active = tab.dataset.mainTab === next;
            tab.classList.toggle("is-active", active);
            tab.setAttribute("aria-selected", String(active));
            tab.tabIndex = active ? 0 : -1;
        }
        for (const panel of ["catalog", "roles", "users"]) {
            root.querySelector("[id='ac" + (panel === "catalog" ? "Catalog" : panel === "roles" ? "Roles" : "Users") + "Panel']").hidden = panel !== next;
        }
        if (next === "roles") await loadRoleTab();
        if (next === "users") await loadUserTab();
    }

    async function switchRole(roleId) {
        if (roleSaving || roleId === currentRoleId || !await confirmDiscardRoleChanges()) return;
        await selectRole(roleId);
    }

    function editRolePermission(input) {
        if (!roleReady || roleSaving) {
            input.checked = !input.checked;
            return;
        }
        if (input.dataset.rolePermissionId) {
            if (input.checked) roleChosen.add(input.dataset.rolePermissionId);
            else roleChosen.delete(input.dataset.rolePermissionId);
        } else if (input.dataset.roleEntityToggle) {
            const entity = roleCatalog.find(x => x.id === input.dataset.roleEntityToggle);
            if (!entity) return;
            for (const permission of entity.permissions) {
                if (input.checked) roleChosen.add(permission.id);
                else roleChosen.delete(permission.id);
            }
            for (const checkbox of roleGroups.querySelectorAll("[data-role-permission-id]")) {
                if (entity.permissions.some(p => p.id === checkbox.dataset.rolePermissionId))
                    checkbox.checked = input.checked;
            }
        }
        syncRoleState();
    }

    async function saveRolePermissions() {
        if (roleSaving || !roleReady || !roleDirty() || !currentRoleId) return;
        roleSaving = true;
        const roleId = currentRoleId;
        try {
            syncRoleState();
            showError("role", "");
            const result = await post("SaveRolePermissions", {
                roleId, permissionIds: [...roleChosen]
            });
            if (signal.aborted || currentRoleId !== roleId) return;
            if (result?.success) {
                roleOriginal = new Set(roleChosen);
                toastifySuccess("دسترسی‌های نقش ذخیره شدند.");
            } else if (!root.querySelector("[data-role-error]").textContent) {
                showError("role", "ذخیره دسترسی‌های نقش انجام نشد.");
            }
        } finally {
            roleSaving = false;
            if (!signal.aborted) syncRoleState();
        }
    }


    // User permissions are NOT copied from roles. Effective access is computed from
    // current role memberships + the live role grants, then overridden per user.
    const selectedUser = () => userPageItems.find(x => x.id === currentUserId);
    const userDirty = () =>
        userRoleIds.size !== userOriginalRoles.size ||
        [...userRoleIds].some(id => !userOriginalRoles.has(id)) ||
        userOverrides.size !== userOriginalOverrides.size ||
        [...userOverrides].some(([id, allowed]) => userOriginalOverrides.get(id) !== allowed);

    function effectivePermission(permissionId) {
        const fromRoles = userAvailableRoles.filter(role =>
            userRoleIds.has(role.id) && userGrantSources.get(permissionId)?.has(role.id));
        const custom = userOverrides.get(permissionId);
        return {
            allowed: custom === undefined ? fromRoles.length > 0 : custom,
            state: custom === false ? "محدودیت اختصاصی" :
                   custom === true ? "اجازه اختصاصی" :
                   fromRoles.length ? "مجاز از نقش" : "بدون دسترسی",
            sources: fromRoles.map(role => role.name),
            isCustom: custom !== undefined
        };
    }

    function renderUsers() {
        const list = usersList;
        list.replaceChildren();
        root.querySelector("[data-users-count]").textContent = String(userCount);
        root.querySelector("[data-users-page]").textContent = "صفحه " + userPage + " از " + userPages;
        root.querySelector("[data-action='user-prev']").disabled = userLoading || userSaving || userPage <= 1;
        root.querySelector("[data-action='user-next']").disabled = userLoading || userSaving || userPage >= userPages;
        if (!userPageItems.length) {
            showEmpty(list, userCount ? "کاربری در این صفحه یافت نشد." : "کاربری مطابق جست‌وجو یافت نشد.", "fa-users");
            return;
        }
        for (const person of userPageItems) {
            const button = text("button", null, "ac-user-item" + (person.id === currentUserId ? " is-selected" : ""));
            button.type = "button";
            button.dataset.selectUser = person.id;
            button.setAttribute("aria-pressed", String(person.id === currentUserId));
            button.append(text("strong", person.name),
                text("small", [person.position || "سمت کاری ثبت نشده", person.userName].filter(Boolean).join(" · ")));
            list.append(button);
        }
    }

    function renderUserRoles() {
        const target = root.querySelector("[data-user-roles]");
        target.replaceChildren();
        if (!userReady) {
            showEmpty(target, currentUserId ? "در حال دریافت نقش‌های کاربر..." : "یک کاربر انتخاب کنید.", "fa-user-tag");
            return;
        }
        if (!userAvailableRoles.length) {
            showEmpty(target, "نقشی در سیستم تعریف نشده است.", "fa-user-tag");
            return;
        }
        for (const role of userAvailableRoles) {
            const label = text("label", null, "ac-user-role-choice" + (userRoleIds.has(role.id) ? " is-selected" : ""));
            const input = document.createElement("input");
            input.type = "checkbox";
            input.dataset.userRoleId = role.id;
            input.checked = userRoleIds.has(role.id);
            input.disabled = userSaving;
            label.append(input, text("span", role.name));
            target.append(label);
        }
    }

    function syncUserPermissionRows() {
        for (const row of userPermissions.querySelectorAll("[data-user-permission-id]")) {
            const id = row.dataset.userPermissionId;
            const effective = effectivePermission(id);
            const status = row.querySelector("[data-user-effective]");
            status.textContent = effective.state;
            status.classList.toggle("is-allowed", effective.allowed);
            status.classList.toggle("is-denied", !effective.allowed);
            row.querySelector("[data-user-sources]").textContent =
                effective.sources.length ? "از نقش: " + effective.sources.join("، ") : "بدون مجوز از نقش‌ها";
            row.classList.toggle("is-custom", effective.isCustom);
            row.querySelector("[data-user-override-id]").disabled = !userReady || userSaving;
        }
        for (const group of userPermissions.querySelectorAll("[data-user-entity]")) {
            const entity = userCatalog.find(x => x.id === group.dataset.userEntity);
            if (!entity) continue;
            const granted = entity.permissions.filter(p => effectivePermission(p.id).allowed).length;
            group.querySelector("[data-user-entity-count]").textContent =
                granted + " از " + entity.permissions.length + " مجوز مؤثر";
        }
    }

    function syncUserState() {
        const person = selectedUser();
        root.querySelector("[data-user-title]").textContent = person ? "دسترسی‌های " + person.name : "دسترسی‌های کاربر";
        root.querySelector("[data-user-position]").textContent = person ?
            "سمت کاری: " + (person.position || "ثبت نشده") : "برای شروع یک کاربر انتخاب کنید.";
        root.querySelector("[data-user-subtitle]").textContent = !person ? "" :
            !userReady ? "در حال دریافت تنظیمات..." :
            userDirty() ? "تغییرات ذخیره‌نشده" : "تغییرات ذخیره شده‌اند.";
        root.querySelector("[data-user-roles-count]").textContent = userRoleIds.size + " نقش";
        const total = userCatalog.reduce((sum, e) =>
            sum + e.permissions.filter(p => effectivePermission(p.id).allowed).length, 0);
        root.querySelector("[data-user-effective-count]").textContent = total + " مجوز مؤثر";
        root.querySelector("[data-action='save-user']").disabled = !userReady || userSaving || !userDirty();
        root.querySelector("[data-action='reset-user']").disabled = !userReady || userSaving || !userDirty();
        root.querySelector("[data-action='user-clear-overrides']").disabled = !userReady || userSaving || !userOverrides.size;
        root.querySelector("[data-users-search]").disabled = userSaving;
        for (const checkbox of root.querySelectorAll("[data-user-role-id]"))
            checkbox.disabled = !userReady || userSaving;
        for (const label of root.querySelectorAll(".ac-user-role-choice"))
            label.classList.toggle("is-selected", userRoleIds.has(label.querySelector("[data-user-role-id]").dataset.userRoleId));
        syncUserPermissionRows();
    }

    function renderUserPermissions() {
        userPermissions.replaceChildren();
        if (!currentUserId) {
            showEmpty(userPermissions, "یک کاربر را از فهرست انتخاب کنید.", "fa-user-shield");
            syncUserState();
            return;
        }
        if (!userReady) {
            showEmpty(userPermissions, "در حال دریافت مجوزهای کاربر...", "fa-spinner");
            syncUserState();
            return;
        }
        const term = root.querySelector("[data-user-permission-search]").value.trim().toLocaleLowerCase();
        const onlyCustom = root.querySelector("[data-user-only-overrides]").checked;
        let shown = 0;
        for (const entity of userCatalog) {
            const wholeEntity = [entity.title, entity.key].some(x => x.toLocaleLowerCase().includes(term));
            const visible = entity.permissions.filter(permission =>
                (!onlyCustom || userOverrides.has(permission.id)) &&
                (wholeEntity || [permission.title, permission.key].some(x => x.toLocaleLowerCase().includes(term))));
            if (!visible.length) continue;
            shown++;
            const section = text("section", null, "ac-role-group");
            section.dataset.userEntity = entity.id;
            const header = text("div", null, "ac-role-group-header");
            const name = text("span", null, "ac-role-group-all");
            name.append(text("strong", entity.title), text("small", entity.key));
            const count = text("span", null, "ac-role-group-count");
            count.dataset.userEntityCount = entity.id;
            header.append(name, count);
            const grid = text("div", null, "ac-user-permission-grid");
            for (const permission of visible) {
                const item = text("div", null, "ac-user-permission-item");
                item.dataset.userPermissionId = permission.id;
                const title = text("div", null, "ac-user-permission-title");
                title.append(text("strong", permission.title), text("small", permission.key));
                const controls = text("div", null, "ac-user-permission-row");
                const select = document.createElement("select");
                select.className = "ac-user-override-select";
                select.dataset.userOverrideId = permission.id;
                select.setAttribute("aria-label", "وضعیت اختصاصی " + permission.title + " در " + entity.title);
                select.append(new Option("از نقش‌ها", "inherit"),
                    new Option("اجازه اختصاصی", "allow"),
                    new Option("محدودیت اختصاصی", "deny"));
                const override = userOverrides.get(permission.id);
                select.value = override === undefined ? "inherit" : override ? "allow" : "deny";
                const effective = text("span", null, "ac-user-effective");
                effective.dataset.userEffective = permission.id;
                controls.append(select, effective);
                const sources = text("small", null, "ac-user-sources");
                sources.dataset.userSources = permission.id;
                item.append(title, controls, sources);
                grid.append(item);
            }
            section.append(header, grid);
            userPermissions.append(section);
        }
        if (!shown) {
            showEmpty(userPermissions,
                onlyCustom && !userOverrides.size ? "برای این کاربر هنوز دسترسی اختصاصی تعریف نشده است." :
                "مجوز مطابق فیلتر پیدا نشد.", "fa-key");
        }
        syncUserState();
    }

    async function selectUser(userId) {
        if (!userPageItems.some(x => x.id === userId)) return;
        const generation = ++userSelectionGeneration;
        currentUserId = userId;
        userReady = false;
        userRoleIds = new Set();
        userOverrides = new Map();
        userOriginalRoles = new Set();
        userOriginalOverrides = new Map();
        showError("user", "");
        renderUsers();
        renderUserRoles();
        renderUserPermissions();
        const result = await sendApiRequest({
            url: "/AccessCatalogAdmin/UserAccess",
            params: { userId }, showLoading: false
        });
        if (signal.aborted || generation !== userSelectionGeneration || currentUserId !== userId || activeTab !== "users") return;
        if (!result || !Array.isArray(result.roleIds) || !Array.isArray(result.overrides)) {
            showEmpty(userPermissions, "دریافت تنظیمات کاربر ناموفق بود. دوباره تلاش کنید.", "fa-exclamation-circle");
            return;
        }
        const available = new Set(userAvailableRoles.map(x => x.id));
        const active = new Set(userCatalog.flatMap(x => x.permissions.map(p => p.id)));
        userRoleIds = new Set(result.roleIds.filter(id => available.has(id)));
        userOverrides = new Map(result.overrides.filter(x => active.has(x.accessPermissionId))
            .map(x => [x.accessPermissionId, x.isAllowed]));
        userOriginalRoles = new Set(userRoleIds);
        userOriginalOverrides = new Map(userOverrides);
        userReady = true;
        renderUserRoles();
        renderUserPermissions();
    }

    async function loadUsersPage(pageNumber, keepUser = currentUserId) {
        const generation = ++userPageGeneration;
        ++userSelectionGeneration;
        userLoading = true;
        userReady = false;
        root.querySelector("[data-action='user-prev']").disabled = true;
        root.querySelector("[data-action='user-next']").disabled = true;
        showEmpty(usersList, "در حال دریافت کاربران...", "fa-spinner");
        const data = await sendApiRequest({
            url: "/AccessCatalogAdmin/Users",
            params: { search: root.querySelector("[data-users-search]").value.trim(), page: pageNumber },
            showLoading: false
        });
        if (signal.aborted || generation !== userPageGeneration || activeTab !== "users") return;
        userLoading = false;
        if (!data || !Array.isArray(data.items)) {
            showEmpty(usersList, "دریافت کاربران ناموفق بود. دوباره تلاش کنید.", "fa-exclamation-circle");
            syncUserState();
            return;
        }
        userPageItems = data.items;
        userPage = data.page;
        userPages = Math.max(1, data.totalPages);
        userCount = data.total;
        userSearchTerm = root.querySelector("[data-users-search]").value;
        const id = userPageItems.some(x => x.id === keepUser) ? keepUser : userPageItems[0]?.id;
        currentUserId = null;
        renderUsers();
        if (id) await selectUser(id);
        else {
            renderUserRoles();
            renderUserPermissions();
        }
    }

    async function loadUserTab() {
        const generation = ++userTabGeneration;
        ++userPageGeneration;
        ++userSelectionGeneration;
        userReady = false;
        showError("user", "");
        showEmpty(usersList, "در حال دریافت کاربران...", "fa-spinner");
        showEmpty(userPermissions, "در حال دریافت دسترسی‌ها...", "fa-spinner");
        const [allRoles, catalog, grants] = await Promise.all([
            sendApiRequest({ url: "/AccessCatalogAdmin/Roles", showLoading: false }),
            sendApiRequest({ url: "/AccessCatalogAdmin/RoleCatalog", showLoading: false }),
            sendApiRequest({ url: "/AccessCatalogAdmin/RoleGrants", showLoading: false })
        ]);
        if (signal.aborted || generation !== userTabGeneration || activeTab !== "users") return;
        if (!Array.isArray(allRoles) || !Array.isArray(catalog) || !Array.isArray(grants)) {
            showEmpty(userPermissions, "دریافت نقش‌ها یا مجوزها ناموفق بود. دوباره تلاش کنید.", "fa-exclamation-circle");
            return;
        }
        userAvailableRoles = allRoles;
        userCatalog = catalog;
        userGrantSources = new Map();
        for (const grant of grants) {
            if (!userGrantSources.has(grant.accessPermissionId))
                userGrantSources.set(grant.accessPermissionId, new Set());
            userGrantSources.get(grant.accessPermissionId).add(grant.roleId);
        }
        await loadUsersPage(userPage);
    }

    async function confirmDiscardUserChanges() {
        if (!userReady || !userDirty()) return true;
        return !!(await confirmAlert({
            title: "تغییرات ذخیره نشده",
            text: "تغییرات نقش‌ها و مجوزهای این کاربر ذخیره نشده‌اند. ادامه می‌دهید؟",
            confirmText: "ادامه بدون ذخیره", cancelText: "انصراف", icon: "warning"
        }));
    }

    async function switchUser(userId) {
        if (userSaving || userLoading || currentUserId === userId) return;
        if (!await confirmDiscardUserChanges()) return;
        await selectUser(userId);
    }

    async function turnUsersPage(direction) {
        if (userSaving || userLoading || !await confirmDiscardUserChanges()) return;
        const next = userPage + direction;
        if (next < 1 || next > userPages) return;
        await loadUsersPage(next, null);
    }

    async function reloadUsers() {
        if (userSaving || !await confirmDiscardUserChanges()) return;
        await loadUserTab();
    }

    function setUserOverride(id, value) {
        if (!userReady || userSaving) return;
        if (value === "inherit") userOverrides.delete(id);
        else userOverrides.set(id, value === "allow");
        if (root.querySelector("[data-user-only-overrides]").checked)
            renderUserPermissions();
        else
            syncUserState();
    }

    async function saveUserAccess() {
        if (userSaving || !userReady || !userDirty() || !currentUserId) return;
        userSaving = true;
        const userId = currentUserId;
        try {
            syncUserState();
            showError("user", "");
            const result = await post("SaveUserAccess", {
                userId, roleIds: [...userRoleIds],
                overrides: [...userOverrides].map(([accessPermissionId, isAllowed]) =>
                    ({ accessPermissionId, isAllowed }))
            });
            if (signal.aborted || currentUserId !== userId) return;
            if (result?.success) {
                userOriginalRoles = new Set(userRoleIds);
                userOriginalOverrides = new Map(userOverrides);
                const person = selectedUser();
                if (person) person.roles = userAvailableRoles.filter(r => userRoleIds.has(r.id)).map(r => r.name);
                renderUsers();
                toastifySuccess("نقش‌ها و دسترسی‌های اختصاصی کاربر ذخیره شدند.");
            } else if (!root.querySelector("[data-user-error]").textContent) {
                showError("user", "ذخیره تنظیمات کاربر انجام نشد.");
            }
        } finally {
            userSaving = false;
            if (!signal.aborted) {
                renderUsers();
                syncUserState();
            }
        }
    }

    root.addEventListener("click", event => {
        const tab = event.target.closest("[data-main-tab]");
        if (tab) { void switchMainTab(tab.dataset.mainTab); return; }
        const role = event.target.closest("[data-select-role]");
        if (role) { void switchRole(role.dataset.selectRole); return; }
        const person = event.target.closest("[data-select-user]");
        if (person) { void switchUser(person.dataset.selectUser); return; }
        const target = event.target.closest("[data-action]");
        if (!target) {
            const select = event.target.closest("[data-select-entity]");
            if (select) void selectEntity(select.dataset.selectEntity);
            return;
        }
        if (target.disabled) return;
        const action = target.dataset.action;
        if (action === "reload-users") void reloadUsers();
        else if (action === "user-prev") void turnUsersPage(-1);
        else if (action === "user-next") void turnUsersPage(1);
        else if (action === "save-user") void saveUserAccess();
        else if (action === "reset-user") {
            userRoleIds = new Set(userOriginalRoles);
            userOverrides = new Map(userOriginalOverrides);
            renderUserRoles();
            renderUserPermissions();
        }
        else if (action === "user-clear-overrides") {
            userOverrides.clear();
            renderUserPermissions();
        }
        else if (action === "reload-roles") {
            void (async () => {
                if (!roleSaving && await confirmDiscardRoleChanges()) await loadRoleTab();
            })();
        }
        else if (action === "save-role") void saveRolePermissions();
        else if (action === "reset-role") {
            roleChosen = new Set(roleOriginal);
            renderRolePermissions();
        }
        else if (action === "new-entity") openEntityEditor();
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
    root.querySelector(".ac-main-tabs").addEventListener("keydown", event => {
        const tab = event.target.closest("[data-main-tab]");
        if (!tab || !["ArrowRight", "ArrowLeft", "Home", "End"].includes(event.key)) return;
        event.preventDefault();
        const tabs = [...root.querySelectorAll("[data-main-tab]")];
        const position = tabs.indexOf(tab);
        const nextIndex = event.key === "Home" ? 0 :
            event.key === "End" ? tabs.length - 1 :
            (position + (event.key === "ArrowLeft" ? 1 : -1) + tabs.length) % tabs.length;
        tabs[nextIndex].focus();
        void switchMainTab(tabs[nextIndex].dataset.mainTab);
    }, { signal });

    root.querySelector("[data-entity-search]").addEventListener("input", renderEntities, { signal });
    root.querySelector("[data-role-search]").addEventListener("input", renderRoles, { signal });
    root.querySelector("[data-role-permission-search]").addEventListener("input", renderRolePermissions, { signal });
    root.querySelector("[data-users-search]").addEventListener("input", () => {
        if (userSearchTimer !== null) clearTimeout(userSearchTimer);
        userSearchTimer = setTimeout(() => {
            userSearchTimer = null;
            if (activeTab !== "users" || signal.aborted) return;
            void (async () => {
                if (!await confirmDiscardUserChanges()) {
                    root.querySelector("[data-users-search]").value = userSearchTerm;
                    return;
                }
                await loadUsersPage(1, null);
            })();
        }, 260);
    }, { signal });
    root.querySelector("[data-user-permission-search]").addEventListener("input", renderUserPermissions, { signal });
    root.querySelector("[data-user-only-overrides]").addEventListener("change", renderUserPermissions, { signal });
    root.addEventListener("change", event => {
        if (event.target.matches("[data-role-permission-id], [data-role-entity-toggle]"))
            editRolePermission(event.target);
        if (event.target.matches("[data-user-role-id]")) {
            if (!userReady || userSaving) return;
            const roleId = event.target.dataset.userRoleId;
            if (event.target.checked) userRoleIds.add(roleId);
            else userRoleIds.delete(roleId);
            syncUserState();
        }
        if (event.target.matches("[data-user-override-id]"))
            setUserOverride(event.target.dataset.userOverrideId, event.target.value);
    }, { signal });
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

(() => {
    'use strict';

    const root = document.getElementById('targetPage');
    if (!root) return;

    const el = id => document.getElementById(id);
    const months = ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور', 'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'];
    const html = value => escapeHtml(String(value ?? ''));
    const key = (code, month = 0, userId = '') => [code, month, userId].join('|');
    const emptyState = () => ({
        id: null, year: Number(root.dataset.currentYear), version: 0, status: 0,
        yearly: [], monthly: [], assignments: []
    });

    let state = emptyState();
    let setup = { metrics: [], experts: [] };
    let activeExperts = [];
    let step = 0;
    let activeMonth = 1;
    let activeExpertId = '';
    let tableMode = false;
    let sending = false;
    let changed = false;
    let modalInstance;
    const form = el('targetWizardForm');
    const modal = el('targetWizard');
    const panels = [...root.querySelectorAll('[data-target-panel]')];
    const steps = [...root.querySelectorAll('[data-target-step]')];

    const metricList = () => setup.metrics.filter(metric => metric.isActive ||
        state.yearly.some(item => item.metricCode === metric.code));
    const normalize = value => toEnglishDigits(String(value ?? ''))
        .replace(/[٬,،\s]/g, '').replace(/٫/g, '.').trim();
    const validNumber = raw => /^\d+(?:\.\d{1,2})?$/.test(raw);

    // All calculations use integer hundredths, avoiding floating-point money rounding.
    const scaled = raw => {
        const s = normalize(raw);
        if (!validNumber(s)) return null;
        const [whole, fraction = ''] = s.split('.');
        return BigInt(whole) * 100n + BigInt((fraction + '00').slice(0, 2));
    };
    const fromScaled = value => {
        const whole = (value / 100n).toString();
        const frac = (value % 100n).toString().padStart(2, '0');
        return frac === '00' ? whole : whole + '.' + frac.replace(/0$/, '');
    };
    const formatDate = value => {
        if (!value) return '—';
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? '—' :
            new Intl.DateTimeFormat('fa-IR-u-ca-persian', { year: 'numeric', month: '2-digit', day: '2-digit' }).format(date);
    };
    const formatted = value => {
        const s = normalize(value);
        if (!validNumber(s)) return s;
        const [whole, fraction] = s.split('.');
        return whole.replace(/\B(?=(\d{3})+(?!\d))/g, ',') + (fraction ? '.' + fraction : '');
    };
    // Accept only digits for counts, and numeric decimals (up to two places) for money and percentages.
    const sanitizeMetricInput = (raw, metric) => {
        const source = toEnglishDigits(String(raw ?? '')).replace(/٫/g, '.');
        let result = '';
        let decimal = false;
        let fractionDigits = 0;

        for (const char of source) {
            if (char >= '0' && char <= '9') {
                if (decimal && fractionDigits >= 2) continue;
                result += char;
                if (decimal) fractionDigits++;
            } else if (char === '.' && metric.kind !== 1 && !decimal) {
                if (!result) result = '0';
                result += '.';
                decimal = true;
            }
        }
        return result;
    };

    // Group only monetary amounts; counts and percentages remain ungrouped.
    const displayMetricInput = (value, metric) => {
        const text = sanitizeMetricInput(value, metric);
        if (metric.kind !== 0 || !text) return text;
        const decimalIndex = text.indexOf('.');
        const whole = decimalIndex < 0 ? text : text.slice(0, decimalIndex);
        const suffix = decimalIndex < 0 ? '' : text.slice(decimalIndex);
        return whole.replace(/\B(?=(\d{3})+(?!\d))/g, ',') + suffix;
    };

    // Keep the caret near the edited digit when commas are added or removed.
    function formatTypedInput(input, metric) {
        const caret = input.selectionStart ?? input.value.length;
        const prefix = sanitizeMetricInput(input.value.slice(0, caret), metric);
        const value = sanitizeMetricInput(input.value, metric);
        input.value = displayMetricInput(value, metric);
        const nextCaret = displayMetricInput(prefix, metric).length;
        input.setSelectionRange(nextCaret, nextCaret);
        return value;
    }

    const findItem = (kind, code, month = 0, userId = '') =>
        state[kind].find(item => key(item.metricCode, item.month, item.userId) === key(code, month, userId));
    const valueOf = (kind, code, month = 0, userId = '') =>
        findItem(kind, code, month, userId)?.value ?? '';
    const sum = items => items.reduce((total, item) => total + (scaled(item.value) ?? 0n), 0n);
    const metricValueValid = (metric, value) => {
        const amount = scaled(value);
        return amount !== null && amount >= 0n &&
            (metric.kind !== 1 || amount % 100n === 0n) &&
            (metric.kind !== 2 || amount <= 10000n);
    };

    function setAmount(kind, code, month, userId, value) {
        const idx = state[kind].findIndex(item =>
            key(item.metricCode, item.month, item.userId) === key(code, month, userId));
        if (value === '') {
            if (idx >= 0) state[kind].splice(idx, 1);
            return;
        }
        const item = { metricCode: code, month, value };
        if (kind === 'assignments') item.userId = userId;
        if (idx >= 0) state[kind][idx] = item;
        else state[kind].push(item);
    }

    function field(metric, kind, month = 0, userId = '') {
        const value = valueOf(kind, metric.code, month, userId);
        return `<label class="target-field">
            <span class="target-field-label"><span class="target-field-title">${html(metric.title)}</span> <span class="target-field-unit">(${html(metric.unit)})</span></span>
            <input type="text" inputmode="${metric.kind === 1 ? 'numeric' : 'decimal'}" autocomplete="off"
                data-metric-kind="${metric.kind}" data-kind="${kind}" data-code="${html(metric.code)}"
                data-month="${month}" data-user="${html(userId)}"
                value="${html(displayMetricInput(value, metric))}" placeholder="مقدار را وارد کنید" />
        </label>`;
    }

    // Initial distribution does not overwrite any manually edited monthly values.
    function fillMonthlyDefaults(overwrite = false) {
        for (const metric of metricList()) {
            const annual = scaled(valueOf('yearly', metric.code));
            if (annual === null) continue;
            if (!overwrite && state.monthly.some(item => item.metricCode === metric.code)) continue;
            const parts = metric.kind === 2
                ? Array(12).fill(valueOf('yearly', metric.code))
                : Array.from({ length: 12 }, (_, i) => {
                    if (metric.kind === 1) {
                        const amount = annual / 100n;
                        return ((amount / 12n) + (BigInt(i) < amount % 12n ? 1n : 0n)).toString();
                    }
                    return fromScaled((annual / 12n) + (BigInt(i) < annual % 12n ? 1n : 0n));
                });
            for (let month = 1; month <= 12; month++)
                setAmount('monthly', metric.code, month, '', parts[month - 1]);
        }
    }

    function fillAssignmentDefaults() {
        for (const metric of metricList())
            for (let month = 1; month <= 12; month++)
                for (const expert of setup.experts)
                    if (!findItem('assignments', metric.code, month, expert.id))
                        setAmount('assignments', metric.code, month, expert.id, '0');
    }

    function monthlyIssues() {
        const errors = [];
        for (const metric of metricList()) {
            const annual = valueOf('yearly', metric.code);
            const values = state.monthly.filter(item => item.metricCode === metric.code);
            if (!metricValueValid(metric, annual)) {
                errors.push(`تارگت سالانه «${metric.title}» کامل نیست.`);
                continue;
            }
            if (values.length !== 12 || values.some(item => item.month < 1 || item.month > 12 ||
                !metricValueValid(metric, item.value))) {
                errors.push(`تارگت ۱۲ ماه «${metric.title}» کامل نیست.`);
                continue;
            }
            if (metric.kind !== 2 && sum(values) !== scaled(annual))
                errors.push(`مجموع ۱۲ ماه «${metric.title}» با هدف سالانه برابر نیست.`);
        }
        return errors;
    }

    function allocationIssues(monthOnly = null) {
        const errors = [];
        const targetMonths = monthOnly ? [monthOnly] : Array.from({ length: 12 }, (_, i) => i + 1);
        if (!setup.experts.length) return ['هیچ کارشناس فروشی برای تخصیص یافت نشد.'];
        for (const month of targetMonths) {
            for (const metric of metricList()) {
                const monthTarget = valueOf('monthly', metric.code, month);
                const allocations = state.assignments.filter(item =>
                    item.metricCode === metric.code && item.month === month);
                if (!metricValueValid(metric, monthTarget) ||
                    setup.experts.some(expert => !allocations.some(item => item.userId === expert.id)) ||
                    allocations.some(item => !metricValueValid(metric, item.value))) {
                    errors.push(`تخصیص «${metric.title}» برای ${months[month - 1]} کامل نیست.`);
                    continue;
                }
                if (metric.kind !== 2 && sum(allocations) !== scaled(monthTarget))
                    errors.push(`جمع تخصیص «${metric.title}» در ${months[month - 1]} با تارگت ماه برابر نیست.`);
            }
        }
        return errors;
    }

    function showFeedback(message = '') {
        el('targetFormFeedback').textContent = message;
        el('targetFormFeedback').hidden = !message;
    }

    function renderSummary() {
        const box = el('targetPreviousSummary');
        box.hidden = step === 0;
        const annual = metricList().map(metric =>
            `<span>${html(metric.title)}: <b>${html(formatted(valueOf('yearly', metric.code) || '—'))}</b> ${html(metric.unit)}</span>`).join('');
        const current = step >= 1 && step <= 2 ? `<span>ماه ${months[activeMonth - 1]}</span>` : '';
        el('targetSummaryContent').innerHTML = `<div class="target-summary-content">
            <span>سال <b>${html(state.year)}</b></span>${annual}${current}</div>`;
    }

    function renderAnnual() {
        el('targetYear').value = state.year;
        el('targetAnnualFields').innerHTML = metricList().map(metric => field(metric, 'yearly')).join('');
    }

    function renderMonthTabs(containerId, allocation = false) {
        el(containerId).innerHTML = months.map((name, index) => {
            const month = index + 1;
            const complete = allocation
                ? allocationIssues(month).length === 0
                : metricList().every(metric => metricValueValid(metric,
                    valueOf('monthly', metric.code, month)));
            return `<button type="button" data-target-month="${month}"
                class="target-month-button${activeMonth === month ? ' active' : ''}${complete ? ' complete' : ''}"
                aria-pressed="${activeMonth === month}" aria-label="ماه ${name}${complete ? '، تکمیل‌شده' : ''}">
                ${name}${complete ? '<span aria-hidden="true">✓</span>' : ''}
            </button>`;
        }).join('');
    }

    function renderMonthly() {
        el('targetMonthlyFields').innerHTML = `<div class="target-month-single">
            <h4>اهداف ${months[activeMonth - 1]}</h4>
            <div class="target-form-grid">${metricList()
                .map(metric => field(metric, 'monthly', activeMonth)).join('')}</div>
        </div>`;
        refreshMonthlyFeedback();
    }

    function refreshMonthlyFeedback() {
        const issues = monthlyIssues();
        const box = el('targetMonthlyFeedback');
        box.textContent = issues.length ? `${issues.length} مورد نیازمند اصلاح` : 'تقسیم ماهانه کامل است.';
        box.style.color = issues.length ? '#c2185b' : '#004944';
        renderMonthTabs('targetMonthlyTabs');
    }

    function renderExperts() {
        const experts = setup.experts;
        if (!experts.some(expert => expert.id === activeExpertId))
            activeExpertId = experts[0]?.id ?? '';

        const select = el('targetExpertSelect');
        select.innerHTML = experts.length
            ? experts.map(expert =>
                `<option value="${html(expert.id)}">${html(expert.name)}</option>`).join('')
            : '<option value="">کارشناسی یافت نشد</option>';
        select.value = activeExpertId;
        select.disabled = !experts.length;

        const index = experts.findIndex(expert => expert.id === activeExpertId);
        el('targetExpertProgress').textContent = index < 0
            ? 'بدون کارشناس' : `کارشناس ${index + 1} از ${experts.length}`;

        el('targetCurrentMonthTargets').innerHTML = metricList().map(metric =>
            `<span>${html(metric.title)}: <b>${html(formatted(valueOf('monthly', metric.code, activeMonth) || '—'))}</b></span>`).join('');

        const expert = experts.find(item => item.id === activeExpertId);
        el('targetExpertFields').innerHTML = expert
            ? `<article class="target-expert-card">
                <div class="target-mini-grid">${metricList().map(metric =>
                    field(metric, 'assignments', activeMonth, expert.id)).join('')}</div>
            </article>`
            : '<p>کارشناس فروش ثبت‌شده‌ای یافت نشد.</p>';
        refreshAllocationFeedback();
    }

    function refreshAllocationFeedback() {
        const issues = allocationIssues(activeMonth);
        el('targetAllocationFeedback').className = 'target-validation' + (issues.length ? '' : ' ok');
        el('targetAllocationFeedback').textContent = issues.length
            ? `${issues.length} شاخص نیازمند تکمیل تخصیص در ${months[activeMonth - 1]} است.`
            : 'تخصیص تمام کارشناسان این ماه کامل است.';
        el('targetAllocationFeedback').title = issues.join('\n');
        renderMonthTabs('targetExpertTabs', true);
    }

    function renderReview() {
        const problems = [...monthlyIssues(), ...allocationIssues()];
        const metricRows = metricList().map(metric =>
            `<div class="target-review-line"><span>${html(metric.title)}</span>
                <b>${html(formatted(valueOf('yearly', metric.code) || '—'))} ${html(metric.unit)}</b></div>`).join('');
        const monthRows = months.map((monthName, index) => {
            const month = index + 1;
            const totals = metricList().map(metric =>
                `<span>${html(metric.title)}: ${html(formatted(valueOf('monthly', metric.code, month) || '—'))}</span>`).join(' | ');
            const expertRows = setup.experts.map(expert => `<div class="target-review-line">
                <span>${html(expert.name)}</span>
                <span>${metricList().map(metric =>
                    `${html(metric.title)}: ${html(formatted(valueOf('assignments', metric.code, month, expert.id) || '—'))}`).join(' | ')}</span>
                </div>`).join('');
            return `<details class="target-review-block"><summary><b>${monthName}</b> — ${totals}</summary>${expertRows}</details>`;
        }).join('');
        const annualExperts = setup.experts.map(expert => {
            const info = metricList().filter(metric => metric.kind !== 2).map(metric => {
                const rows = state.assignments.filter(item =>
                    item.metricCode === metric.code && item.userId === expert.id);
                return `${html(metric.title)}: ${html(formatted(fromScaled(sum(rows))))}`;
            }).join(' | ');
            return `<div class="target-review-line"><span>${html(expert.name)}</span><span>${info}</span></div>`;
        }).join('');
        el('targetReview').innerHTML = `
            <div class="target-validation ${problems.length ? '' : 'ok'}" role="status">
                ${problems.length ? `${problems.length} مغایرت برای ثبت نهایی وجود دارد.` : 'تمام اطلاعات برای ثبت نهایی معتبر هستند.'}
            </div>
            ${problems.length ? `<div class="target-review-block">${problems.map(issue =>
                `<p>${html(issue)}</p>`).join('')}</div>` : ''}
            <div class="target-review-block"><h4>اهداف سال ${html(state.year)}</h4>${metricRows}</div>
            <div class="target-review-block"><h4>مجموع تارگت سالانه کارشناسان</h4>${annualExperts}</div>
            <h4>جزئیات ۱۲ ماه و تخصیص‌ها</h4>${monthRows}`;
        el('targetFinish').disabled = problems.length > 0 || state.status === 1;
    }


    const displayValue = value => value === '' || value === null || value === undefined
        ? '—' : formatted(value);

    function flatTable(caption, headers, rows) {
        return `<div class="target-table-scroll">
            <table class="target-data-table">
                <caption>${html(caption)}</caption>
                <thead><tr>${headers.map(header => `<th scope="col">${html(header)}</th>`).join('')}</tr></thead>
                <tbody>${rows.map(cells => `<tr>${cells.map((cell, index) =>
                    index === 0 ? `<th scope="row">${html(cell)}</th>` :
                        `<td>${html(cell)}</td>`).join('')}</tr>`).join('')}</tbody>
            </table>
        </div>`;
    }

    function annualFlatTable() {
        return flatTable('تعاریف تارگت سالانه', ['شاخص', 'هدف سالانه', 'واحد'],
            metricList().map(metric => [metric.title,
                displayValue(valueOf('yearly', metric.code)), metric.unit]));
    }

    function monthlyFlatTable() {
        const metrics = metricList();
        const rows = months.map((monthName, index) =>
            [monthName, ...metrics.map(metric =>
                displayValue(valueOf('monthly', metric.code, index + 1)))]);
        rows.push(['هدف سالانه', ...metrics.map(metric =>
            displayValue(valueOf('yearly', metric.code)))]);
        rows.push(['جمع ۱۲ ماه', ...metrics.map(metric => metric.kind === 2
            ? '—' : displayValue(fromScaled(sum(state.monthly.filter(item =>
                item.metricCode === metric.code)))))]);
        rows.push(['اختلاف با سالانه', ...metrics.map(metric => {
            if (metric.kind === 2) return '—';
            const yearly = scaled(valueOf('yearly', metric.code));
            return yearly === null ? '—' : fromScaled(yearly -
                sum(state.monthly.filter(item => item.metricCode === metric.code)));
        })]);
        return flatTable('تعاریف تارگت ۱۲ ماه', ['ماه', ...metrics.map(x => x.title)], rows);
    }

    function allocationFlatTable() {
        const metrics = metricList();
        const rows = [];
        for (let month = 1; month <= 12; month++) {
            rows.push([months[month - 1], 'هدف ماه',
                ...metrics.map(metric => displayValue(valueOf('monthly', metric.code, month)))]);
            for (const expert of setup.experts) {
                rows.push([months[month - 1], expert.name,
                    ...metrics.map(metric => displayValue(
                        valueOf('assignments', metric.code, month, expert.id)))]);
            }
            rows.push([months[month - 1], 'جمع تخصیص',
                ...metrics.map(metric => metric.kind === 2 ? '—' :
                    displayValue(fromScaled(sum(state.assignments.filter(item =>
                        item.metricCode === metric.code && item.month === month)))))]);
        }
        return flatTable('تخصیص تمام ماه‌ها به کارشناسان',
            ['ماه', 'کارشناس / نوع', ...metrics.map(x => x.title)], rows);
    }

    function setTableMode(visible) {
        tableMode = visible;
        const panel = panels[step];
        panels.forEach(item => item.classList.toggle('target-table-mode', item === panel && visible));
        panel.querySelectorAll('[data-target-editor]').forEach(item => {
            item.hidden = visible;
        });
        const tableView = panel.querySelector('[data-target-table-view]');
        tableView.hidden = !visible;
        if (visible) {
            tableView.innerHTML = step === 0 ? annualFlatTable()
                : step === 1 ? monthlyFlatTable()
                    : step === 2 ? allocationFlatTable()
                        : annualFlatTable() + monthlyFlatTable() + allocationFlatTable();
        }
        const toggle = panel.querySelector('[data-target-table-toggle]');
        toggle.textContent = visible ? 'بازگشت به فرم' : 'نمایش همه به‌صورت جدول';
        toggle.setAttribute('aria-pressed', String(visible));
    }

    function navigate(nextStep) {
        if (nextStep < 0 || nextStep > 3) return;
        step = nextStep;
        if (step >= 1 && state.status !== 1) fillMonthlyDefaults();
        if (step >= 2 && state.status !== 1) fillAssignmentDefaults();
        panels.forEach((panel, index) => { panel.hidden = index !== step; });
        steps.forEach((button, index) => {
            button.classList.toggle('active', index === step);
            button.setAttribute('aria-current', index === step ? 'step' : 'false');
        });
        el('targetBack').disabled = step === 0;
        el('targetNext').hidden = step === 3;
        el('targetFinish').hidden = step !== 3 || state.status === 1;
        el('targetSaveDraft').hidden = state.status === 1;
        el('targetModalStatus').textContent = state.status === 1 ? 'نهایی‌شده' : 'پیش‌نویس';
        renderSummary();
        if (step === 0) renderAnnual();
        if (step === 1) renderMonthly();
        if (step === 2) renderExperts();
        if (step === 3) renderReview();
        setTableMode(false);
        form.querySelectorAll('[data-target-panel] input[data-kind], #targetYear')
            .forEach(input => { input.disabled = state.status === 1; });
        el('targetRebalance').disabled = state.status === 1;
        const body = root.querySelector('.target-modal-body');
        body?.scrollTo({ top: 0, behavior: 'instant' });
        if (typeof enableMomentumScrollScope === 'function')
            enableMomentumScrollScope(body);
        showFeedback();
    }

    async function loadList() {
        const box = el('targetList');
        el('targetListFeedback').textContent = 'در حال دریافت تارگت‌ها...';
        const rows = await sendApiRequest({ url: '/Target/GetPlans', method: 'GET', showLoading: false });
        if (!rows) {
            el('targetListFeedback').textContent = 'دریافت لیست ناموفق بود.';
            return;
        }
        el('targetListFeedback').textContent = rows.length ? '' : 'هنوز تارگتی ثبت نشده است.';
        box.innerHTML = rows.map(row => `<article class="target-card">
            <div class="target-card-header">
                <h3>تارگت سال ${html(row.year)}</h3>
                <span class="target-pill ${row.status === 1 ? 'final' : ''}">${row.status === 1 ? 'نهایی' : 'پیش‌نویس'}</span>
            </div>
            <div class="target-card-footer">
                <span>فروش سالانه: <b>${html(formatted(row.salesTarget))}</b> ریال</span>
                <span>ماه‌های دارای تخصیص: ${html(row.allocatedMonths)} از ۱۲</span>
                <span>کارشناسان: ${html(row.expertCount)}</span>
                <span>تاریخ ثبت: ${html(formatDate(row.createdAtUtc))}</span>
                <span>آخرین ویرایش: ${html(formatDate(row.updatedAtUtc))}</span>
                <button type="button" class="target-btn target-btn-outline" data-open-id="${html(row.id)}">
                    ${row.status === 1 ? 'مشاهده' : 'ادامه / ویرایش'}
                </button>
            </div>
        </article>`).join('');
    }

    function openNew() {
        setup.experts = [...activeExperts];
        state = emptyState();
        step = 0;
        activeMonth = 1;
        activeExpertId = setup.experts[0]?.id ?? '';
        changed = false;
        el('targetWizardTitle').textContent = 'تارگت جدید';
        navigate(0);
        ModalToggle(modal, 'show');
    }

    async function openExisting(id) {
        const data = await sendApiRequest({ url: '/Target/GetPlan', params: { id }, showLoading: true });
        if (!data) return;
        state = {
            id: data.id, year: data.year, version: data.version, status: data.status,
            yearly: data.yearly || [], monthly: data.monthly || [], assignments: data.assignments || []
        };
        // Keep previous assignments visible for experts whose role has changed.
        setup.experts = [...activeExperts];
        const missing = [...new Set(state.assignments.map(x => x.userId))]
            .filter(id => !setup.experts.some(x => x.id === id));
        setup.experts.push(...missing.map(id => ({ id, name: 'کارشناس سابق (' + id + ')' })));
        activeMonth = 1;
        activeExpertId = setup.experts[0]?.id ?? '';
        changed = false;
        el('targetWizardTitle').textContent = `تارگت سال ${state.year}`;
        navigate(0);
        ModalToggle(modal, 'show');
    }

    async function save(finalize) {
        if (sending || state.status === 1) return;
        if (finalize) {
            const problems = [...monthlyIssues(), ...allocationIssues()];
            if (problems.length) { showFeedback(problems[0]); return; }
        }
        sending = true;
        ['targetSaveDraft', 'targetFinish'].forEach(id => { el(id).disabled = true; });
        try {
            const data = await sendApiRequest({
                url: finalize ? '/Target/Finalize' : '/Target/SaveDraft',
                method: 'POST',
                data: state,
                headers: { RequestVerificationToken: form.querySelector('[name="__RequestVerificationToken"]').value },
                successMsg: finalize ? 'تارگت نهایی شد.' : 'پیش‌نویس ذخیره شد.'
            });
            if (!data) { showFeedback('ذخیره انجام نشد. خطا را بررسی کنید.'); return; }
            state.id = data.id;
            state.version = data.version;
            if (finalize) state.status = 1;
            changed = false;
            await loadList();
            if (finalize) ModalToggle(modal, 'hide');
            else navigate(step);
        } finally {
            sending = false;
            el('targetSaveDraft').disabled = false;
            el('targetFinish').disabled = false;
        }
    }

    root.addEventListener('input', event => {
        const input = event.target;
        if (input.id === 'targetYear') {
            input.value = toEnglishDigits(input.value).replace(/\D/g, '').slice(0, 4);
            state.year = Number(input.value) || 0;
            changed = true;
            renderSummary();
            return;
        }
        if (!input.matches('[data-kind]') || event.isComposing) return;
        const metric = setup.metrics.find(item => item.code === input.dataset.code);
        if (!metric) return;
        const value = formatTypedInput(input, metric);
        input.setCustomValidity(value && !metricValueValid(metric, value)
            ? 'مقدار عددی معتبر وارد کنید.' : '');
        setAmount(input.dataset.kind, input.dataset.code,
            Number(input.dataset.month), input.dataset.user, value);
        changed = true;
        renderSummary();
        if (step === 1) refreshMonthlyFeedback();
        if (step === 2) refreshAllocationFeedback();
    });

    root.addEventListener('focusout', event => {
        const input = event.target;
        if (!input.matches('[data-kind]')) return;
        const metric = setup.metrics.find(item => item.code === input.dataset.code);
        if (!metric) return;
        const value = sanitizeMetricInput(input.value, metric).replace(/\.$/, '');
        input.value = displayMetricInput(value, metric);
        input.setCustomValidity(value && !metricValueValid(metric, value)
            ? 'مقدار عددی معتبر وارد کنید.' : '');
        setAmount(input.dataset.kind, input.dataset.code,
            Number(input.dataset.month), input.dataset.user, value);
    });

    root.addEventListener('compositionend', event => {
        if (event.target.matches('[data-kind]'))
            event.target.dispatchEvent(new Event('input', { bubbles: true }));
    });

    root.addEventListener('click', event => {
        const open = event.target.closest('[data-open-id]');
        if (open) { openExisting(open.dataset.openId); return; }
        const monthButton = event.target.closest('[data-target-month]');
        if (monthButton) {
            activeMonth = Number(monthButton.dataset.targetMonth);
            if (step === 1) renderMonthly();
            if (step === 2) renderExperts();
            renderSummary();
            return;
        }
        const tableToggle = event.target.closest('[data-target-table-toggle]');
        if (tableToggle) { setTableMode(!tableMode); return; }
        const button = event.target.closest('[data-target-step]');
        if (button) navigate(Number(button.dataset.targetStep));
    });
    el('targetNew').addEventListener('click', openNew);
    el('targetRefresh').addEventListener('click', loadList);
    el('targetBack').addEventListener('click', () => navigate(step - 1));
    el('targetNext').addEventListener('click', () => navigate(step + 1));
    el('targetSaveDraft').addEventListener('click', () => save(false));
    el('targetFinish').addEventListener('click', () => save(true));
    el('targetExpertSelect').addEventListener('change', event => {
        activeExpertId = event.target.value;
        renderExperts();
    });

    // Keep one select element; position it in the header on tablet/mobile widths.
    const expertPickerLayout = window.matchMedia('(max-width: 1050px)');
    function positionExpertPicker() {
        const destination = el(expertPickerLayout.matches ? 'targetExpertHeaderActions' : 'targetExpertPickerSlot');
        const picker = el('targetExpertPicker');
        if (picker.parentElement !== destination)
            destination.appendChild(picker);
    }
    expertPickerLayout.addEventListener('change', positionExpertPicker);
    positionExpertPicker();
    el('targetRebalance').addEventListener('click', async () => {
        const approved = await confirmAlert({
            title: 'تقسیم مجدد اهداف',
            text: 'همه مقادیر ماهانه با تقسیم پیش‌فرض بازنویسی می‌شوند. ادامه می‌دهید؟',
            confirmText: 'بله، تقسیم کن'
        });
        if (!approved) return;
        fillMonthlyDefaults(true);
        changed = true;
        renderMonthly();
    });
    modal.addEventListener('hide.bs.modal', event => {
        if (!changed) return;
        const approved = window.confirm('تغییرات ذخیره نشده‌اند. مودال بسته شود؟');
        if (!approved) event.preventDefault();
        else changed = false;
    });
    form.addEventListener('submit', event => event.preventDefault());

    async function init() {
        const data = await sendApiRequest({ url: '/Target/GetSetup', method: 'GET', showLoading: false });
        if (!data) {
            el('targetListFeedback').textContent = 'بارگذاری شاخص‌ها و کارشناسان ممکن نشد.';
            el('targetNew').disabled = true;
            return;
        }
        setup = data;
        activeExperts = [...data.experts];
        await loadList();
    }
    init();
})();

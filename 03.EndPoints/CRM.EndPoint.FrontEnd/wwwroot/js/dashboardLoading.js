/* Dashboard-only loading states. Shared task requests opt in when this is present. */
const dashboardLoading = (() => {
    const listIds = new Set(['allReportsDashboardId', 'todayTaskDashId', 'LogSystemId']);
    const skeleton = document.querySelector('.dashboard-skeleton-list')?.outerHTML || '';
    const pending = new Map();

    async function run(id, load) {
        const target = document.getElementById(id);
        if (!target) return;
        if (pending.has(id)) return pending.get(id).then(() => run(id, load));
        const card = target.closest('.dashGridItem');
        const isList = listIds.has(id);
        const request = (async () => {
            card?.classList.add('dashboard-loading');
            card?.setAttribute('aria-busy', 'true');
            card?.querySelector('.dashboard-load-error')?.remove();
            if (isList) target.innerHTML = skeleton;
            if (id === 'todayTaskDashId') document.getElementById('noTaskImg').style.display = 'none';
            if (id === 'LogSystemId') document.getElementById('noSysActionImg')?.classList.add('d-none');
            try {
                await load();
            } catch (error) {
                console.error('Dashboard section failed:', id, error);
                if (isList) target.replaceChildren();
                else card?.querySelectorAll('.dashboard-value').forEach(value => { value.textContent = '—'; });
                const feedback = document.createElement('div');
                feedback.className = 'dashboard-load-error';
                feedback.setAttribute('role', 'status');
                feedback.textContent = 'دریافت اطلاعات انجام نشد. ';
                const retry = document.createElement('button');
                retry.type = 'button';
                retry.textContent = 'تلاش دوباره';
                retry.addEventListener('click', () => run(id, load));
                feedback.appendChild(retry);
                (isList ? target : target.parentElement).appendChild(feedback);
            } finally {
                card?.classList.remove('dashboard-loading');
                card?.setAttribute('aria-busy', 'false');
                pending.delete(id);
            }
        })();
        pending.set(id, request);
        return request;
    }

    return { run };
})();

/* Shared-shell navigation: prepare off-DOM, then commit only the page snapshot. */
(() => {
    'use strict';
    const content = document.getElementById('mainContent');
    if (!content || !document.querySelector('template[data-page-body]')) return;
    const normalize = url => url.pathname.toLowerCase().replace(/\/$/, '');
    const supported = url => url.origin === location.origin && /^https?:$/.test(url.protocol) &&
        !/\./.test(url.pathname.split('/').at(-1)) &&
        !/^(?:login|logout|logoff|signout|download|export|print|delete|remove|approve|reject|toggle|upload|save|submit)/i.test(url.pathname.split('/').filter(Boolean).at(-1) || '');
    let pending = null, generation = 0, currentUrl = location.href, nativeNavigation = false;
    let pageStyles = [];
    const normal = url => { nativeNavigation = true; location.assign(url.href); };
    const executable = script => !script.type || /^(?:text|application)\/javascript$/.test(script.type);

    function packet(doc, url) {
        const body = doc.querySelector('template[data-page-body]');
        if (!body) return null;
        const section = doc.querySelector('template[data-page-scripts]');
        const scripts = [...body.content.querySelectorAll('script'), ...section?.content.querySelectorAll('script') || []].filter(executable);
        const root = doc.createElement('div');
        root.dataset.pageRoot = '';
        root.dataset.pageKey = url.pathname + url.search;
        root.append(body.content.cloneNode(true));
        return { snapshot: PageDom.snapshot(root), scripts, styles: doc.querySelector('template[data-page-styles]')?.content, title: doc.title };
    }
    function menu(url) {
        document.querySelectorAll('.menu-item.active-menu,.menu-item.active-ancestor,.menu-item.menu-click-orbit').forEach(item => {
            item.classList.remove('active-menu','active-ancestor','menu-click-orbit'); item.removeAttribute('aria-current');
        });
        document.querySelectorAll('.menu-item[href]').forEach(item => {
            if (normalize(new URL(item.href)) !== normalize(url)) return;
            item.classList.add('active-menu','menu-click-orbit'); item.setAttribute('aria-current','page');
            let group = item.closest('.has-submenu');
            while (group) {
                group.classList.add('open');
                const toggle = group.querySelector(':scope > .submenu-toggle');
                toggle?.classList.add('active-ancestor'); toggle?.setAttribute('aria-expanded','true');
                group = group.parentElement?.closest('.has-submenu');
            }
        });
    }
    function styles(fragment) {
        pageStyles.forEach(element => element.remove()); pageStyles = [];
        if (!fragment) return;
        Array.from(fragment.children).forEach(element => {
            const clone = document.importNode(element, true);
            document.head.append(clone); pageStyles.push(clone);
        });
    }
    function cancel() {
        pending?.controller.abort(); pending?.status.remove(); pending = null;
        content.removeAttribute('aria-busy');
    }
    async function commit(data, modules, url, push, version) {
        if (version !== generation) return;
        if (typeof bootstrap !== 'undefined') await Promise.all(Array.from(document.querySelectorAll('.modal.show'), element => new Promise(resolve => {
            const instance = bootstrap.Modal.getInstance(element);
            if (!instance) { resolve(); return; }
            const timeout = setTimeout(resolve, 1000);
            element.addEventListener('hidden.bs.modal', () => { clearTimeout(timeout); resolve(); }, { once: true });
            instance.hide();
        })));
        if (version !== generation) return;
        PageRuntime.unmount();
        styles(data.styles);
        const root = PageDom.patch(content.firstElementChild, data.snapshot);
        if (root.parentNode !== content) content.replaceChildren(root);
        if (push) history.pushState({ pageNavigation: true }, '', url.href);
        currentUrl = url.href;
        if (data.title) document.title = data.title;
        menu(url);
        window.scrollTo(0, 0);
        await PageRuntime.mount(root, modules);
        if (version === generation) {
            cancel();
            root.setAttribute('tabindex','-1'); root.focus({ preventScroll: true });
        }
    }
    async function navigate(url, push = true, initial = false) {
        if (!initial && url.href === currentUrl && !pending) return;
        cancel();
        const version = ++generation;
        const controller = new AbortController();
        const status = document.createElement('div'); status.className = 'workspace-navigation-status'; status.setAttribute('role','status'); status.textContent = 'در حال باز کردن صفحه…';
        document.body.append(status); content.setAttribute('aria-busy','true'); pending = { controller, status };
        try {
            let doc = document;
            if (!initial) {
                const response = await fetch(url, { credentials: 'same-origin', signal: controller.signal, headers: { Accept: 'text/html', 'X-Page-Navigation': 'true' } });
                if (version !== generation) return;
                const destination = new URL(response.url);
                if (response.redirected && !supported(destination)) { normal(destination); return; }
                if (!response.ok || !response.headers.get('content-type')?.includes('text/html')) throw new Error('Invalid page response.');
                doc = new DOMParser().parseFromString(await response.text(), 'text/html');
                url = destination;
            }
            const data = packet(doc, url);
            if (!data) { normal(url); return; }
            const modules = await PageRuntime.prepare(data.scripts, url);
            if (controller.signal.aborted || version !== generation) return;
            await commit(data, modules, url, push, version);
        } catch (error) {
            if (error.name === 'AbortError' || version !== generation) return;
            console.error('Page navigation failed.', error);
            cancel();
            if (initial) { content.textContent = 'بارگذاری صفحه انجام نشد؛ صفحه را دوباره بارگذاری کنید.'; }
            else normal(url);
        }
    }
    document.addEventListener('click', event => {
        if (nativeNavigation || event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        const link = event.target.closest('a[href]');
        if (!link || link.hasAttribute('download') || link.hasAttribute('data-bs-toggle') || (link.target && link.target !== '_self') || link.getAttribute('href').trim().startsWith('#')) return;
        const url = new URL(link.href);
        if (!supported(url) || (url.pathname === location.pathname && url.search === location.search && url.hash)) return;
        event.preventDefault(); navigate(url);
        if (typeof toggleMobileDrawer === 'function') toggleMobileDrawer(false);
    });
    window.navigation?.addEventListener('navigate', event => {
        if (nativeNavigation || !event.cancelable || !event.canIntercept || event.destination.sameDocument || event.navigationType === 'reload' || event.formData || event.downloadRequest) return;
        const url = new URL(event.destination.url);
        if (supported(url)) { event.preventDefault(); navigate(url); }
    });
    window.addEventListener('popstate', () => supported(new URL(location.href)) ? navigate(new URL(location.href), false) : location.reload());
    window.addEventListener('pagehide', () => { cancel(); PageRuntime.unmount(); });
    const start = () => navigate(new URL(location.href), false, true);
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true }); else start();
})();

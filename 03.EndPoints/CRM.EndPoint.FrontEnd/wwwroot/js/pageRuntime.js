/* Page-local scopes preserve legacy inline handlers without reloading shared libraries. */
window.PageRuntime = (() => {
    const sourceCache = new Map();
    const loadedLibraries = new Set(Array.from(document.scripts).filter(s => s.src).map(s => s.src));
    let current = null, nextId = 0;
    const report = error => { if (error?.name !== 'AbortError') console.error('Page lifecycle error.', error); };
    function source(url) {
        if (!sourceCache.has(url)) sourceCache.set(url, fetch(url, { credentials: 'same-origin' }).then(response => {
            if (!response.ok) throw new Error('Page script could not be loaded.');
            return response.text();
        }).catch(error => { sourceCache.delete(url); throw error; }));
        return sourceCache.get(url);
    }
    async function prepare(scripts, baseUrl) {
        return Promise.all(scripts.map(async script => {
            const url = script.getAttribute('src') ? new URL(script.getAttribute('src'), baseUrl).href : null;
            const library = url && !new URL(url).pathname.startsWith('/js/');
            const text = library ? '' : url ? await source(url) : script.textContent;
            const names = (script.dataset.pageBindings || '').split(',').filter(Boolean);
            if (names.some(name => !/^[$A-Z_a-z][$\w]*$/.test(name))) throw new Error('Invalid page binding.');
            const exports = names.map(name => `${JSON.stringify(name)}:{get:()=>${name},set:value=>${name}=value}`).join(',');
            const factory = library ? null : new Function('__page', `const {document,window,$,jQuery,setTimeout,setInterval,clearTimeout,clearInterval,fetch}=__page;\n__page.expose({${exports}});\n${text}\n//# sourceURL=${url || 'crm-page-inline.js'}`);
            return { url, library, factory };
        }));
    }
    class PageScope {
        constructor(root) {
            this.root = root;
            this.controller = new AbortController();
            this.cleanups = [];
            this.publications = [];
            this.readyCallbacks = [];
            this.id = `crmPage${++nextId}`;
            this.timers = new Set();
            const self = this;
            this.document = new Proxy(document, { get(target, name) {
                if (name === 'addEventListener') return (type, callback, options) => type === 'DOMContentLoaded' ? self.ready(callback) : self.listen(target, type, callback, options);
                if (name === 'getElementById') return id => self.isActive ? target.getElementById(id) : root.querySelector(`#${CSS.escape(id)}`);
                if (name === 'querySelector' || name === 'querySelectorAll') return selector => (self.isActive ? target : root)[name](selector);
                const value = Reflect.get(target, name, target);
                return typeof value === 'function' ? value.bind(target) : value;
            }});
            this.window = new Proxy(window, { get(target, name) {
                if (name === 'document') return self.document;
                if (name === 'addEventListener') return (type, callback, options) => self.listen(target, type, callback, options);
                if (['setTimeout','setInterval','clearTimeout','clearInterval','fetch'].includes(name)) return self[name];
                return Reflect.get(target, name, target);
            }, set(target, name, value) { self.assign(name, value); return true; } });
            this.setTimeout = (callback, delay, ...args) => {
                if (!self.isActive) return 0;
                const timer = window.setTimeout(() => { self.timers.delete(timer); if (self.isActive) callback(...args); }, delay);
                self.timers.add(timer); return timer;
            };
            this.setInterval = (callback, delay, ...args) => {
                if (!self.isActive) return 0;
                const timer = window.setInterval(() => { if (self.isActive) callback(...args); }, delay);
                self.timers.add(timer); return timer;
            };
            this.clearTimeout = this.clearInterval = timer => { window.clearTimeout(timer); window.clearInterval(timer); self.timers.delete(timer); };
            this.fetch = (input, options = {}) => {
                const method = options.method || (input instanceof Request ? input.method : 'GET');
                return window.fetch(input, /^(GET|HEAD)$/i.test(method) ? { ...options, signal: options.signal ? AbortSignal.any([options.signal, self.signal]) : self.signal } : options);
            };
            if (window.jQuery) this.$ = this.jQuery = new Proxy(window.jQuery, { apply(target, receiver, args) {
                if (typeof args[0] === 'function') { self.ready(args[0]); return; }
                const first = args[0] === self.document ? document : args[0] === self.window ? window : args[0];
                const result = Reflect.apply(target, receiver, [first, ...args.slice(1)]);
                if (first !== document && first !== window) return result;
                return new Proxy(result, { get(object, name) {
                    if (name === 'ready') return callback => { self.ready(callback); return object; };
                    if (name === 'on' || name === 'one') return (events, ...rest) => {
                        if (!self.isActive) return object;
                        if (typeof events === 'string') events = events.split(/\s+/).map(event => `${event}.${self.id}`).join(' ');
                        self.cleanups.push(() => object.off(`.${self.id}`));
                        object[name](events, ...rest); return object;
                    };
                    const value = object[name]; return typeof value === 'function' ? value.bind(object) : value;
                }});
            }});
        }
        get signal() { return this.controller.signal; }
        get isActive() { return !this.signal.aborted; }
        ready(callback) { if (this.isActive) this.readyCallbacks.push(callback); }
        listen(target, type, callback, options) {
            if (!this.isActive) return;
            const opts = typeof options === 'boolean' ? { capture: options } : { ...options };
            opts.signal = opts.signal ? AbortSignal.any([opts.signal, this.signal]) : this.signal;
            target.addEventListener(type, callback, opts);
        }
        assign(name, value) {
            const previous = Object.getOwnPropertyDescriptor(window, name);
            window[name] = value;
            this.cleanups.push(() => { if (previous) Object.defineProperty(window, name, previous); else delete window[name]; });
        }
        expose(bindings) {
            Object.entries(bindings).forEach(([name, descriptor]) => {
                const previous = Object.getOwnPropertyDescriptor(window, name);
                if (previous && !previous.configurable) {
                    if (!previous.writable) throw new Error(`Page binding collides with shared state: ${name}`);
                    this.publications.push(() => { window[name] = descriptor.get(); });
                    this.cleanups.push(() => { window[name] = previous.value; });
                    return;
                }
                Object.defineProperty(window, name, { ...descriptor, configurable: true });
                this.cleanups.push(() => { if (previous) Object.defineProperty(window, name, previous); else delete window[name]; });
            });
        }
        dispose() {
            if (!this.isActive) return;
            this.controller.abort();
            this.timers.forEach(timer => { window.clearTimeout(timer); window.clearInterval(timer); });
            this.timers.clear();
            if (typeof disposeTaskCardPickers === 'function') disposeTaskCardPickers(this.root);
            this.root.querySelectorAll('*').forEach(element => {
                element.dropdownInstance?.close();
                element.tomselect?.destroy();
                if (window.bootstrap) ['Tooltip','Popover','Modal'].forEach(name => bootstrap[name]?.getInstance(element)?.dispose());
                if (window.Sortable?.get(element)) Sortable.get(element).destroy();
                if (window.jQuery?.fn?.dataTable?.isDataTable(element)) window.jQuery(element).DataTable().destroy();
            });
            this.cleanups.reverse().forEach(cleanup => { try { cleanup(); } catch (error) { report(error); } });
            document.getElementById('dropdown-portal')?.replaceChildren();
            document.getElementById('layoutPortal')?.replaceChildren();
        }
    }
    async function mount(root, modules) {
        const page = current = new PageScope(root);
        for (const module of modules) {
            if (!page.isActive) return;
            if (module.library) {
                if (!loadedLibraries.has(module.url)) {
                    await new Promise((resolve, reject) => {
                        const script = document.createElement('script'); script.src = module.url;
                        script.onload = resolve; script.onerror = reject; document.head.append(script);
                    });
                    loadedLibraries.add(module.url);
                }
            } else {
                module.factory(page);
                page.publications.forEach(publish => publish());
            }
        }
        page.readyCallbacks.forEach(callback => {
            if (!page.isActive) return;
            try { Promise.resolve(callback()).catch(report); } catch (error) { report(error); }
        });
        if (typeof dateTimePicker === 'function') dateTimePicker(root);
        return page;
    }
    return Object.freeze({ prepare, mount, get current() { return current; }, unmount() { current?.dispose(); current = null; } });
})();

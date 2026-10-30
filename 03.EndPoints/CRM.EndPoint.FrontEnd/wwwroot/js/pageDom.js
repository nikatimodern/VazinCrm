/* Keyed virtual snapshots; scripts are mounted separately by PageRuntime. */
window.PageDom = (() => {
    const key = node => node.nodeType === 1 ? node.getAttribute('data-page-key') || node.id : null;
    const executable = node => node.nodeType === 1 && node.tagName === 'SCRIPT' && (!node.type || /^(?:text|application)\/javascript$/.test(node.type));
    function snapshot(node) {
        if (executable(node)) return null;
        if (node.nodeType === 3 || node.nodeType === 8) return { type: node.nodeType, text: node.nodeValue };
        if (node.nodeType !== 1) return null;
        return { type: 1, tag: node.tagName, namespace: node.namespaceURI, key: key(node), attributes: Array.from(node.attributes, a => [a.name, a.value]), children: Array.from(node.childNodes, snapshot).filter(Boolean) };
    }
    function create(vnode) {
        if (vnode.type !== 1) return vnode.type === 3 ? document.createTextNode(vnode.text) : document.createComment(vnode.text);
        const element = vnode.namespace === 'http://www.w3.org/1999/xhtml'
            ? document.createElement(vnode.tag.toLowerCase()) : document.createElementNS(vnode.namespace, vnode.tag);
        vnode.attributes.forEach(([name, value]) => element.setAttribute(name, value));
        vnode.children.forEach(child => element.append(create(child)));
        return element;
    }
    function patch(node, vnode) {
        if (!node || node.nodeType !== vnode.type || (vnode.type === 1 && (node.tagName !== vnode.tag || key(node) !== vnode.key))) {
            const replacement = create(vnode);
            if (node) node.replaceWith(replacement);
            return replacement;
        }
        if (vnode.type !== 1) { if (node.nodeValue !== vnode.text) node.nodeValue = vnode.text; return node; }
        const attributes = new Map(vnode.attributes);
        Array.from(node.attributes).forEach(a => { if (!attributes.has(a.name)) node.removeAttribute(a.name); });
        attributes.forEach((value, name) => { if (node.getAttribute(name) !== value) node.setAttribute(name, value); });
        const keyed = new Map(Array.from(node.childNodes).map(child => [key(child), child]).filter(([id]) => id));
        vnode.children.forEach((child, index) => {
            const current = node.childNodes[index];
            const match = child.key ? keyed.get(child.key) : current;
            const updated = patch(match, child);
            if (node.childNodes[index] !== updated) node.insertBefore(updated, node.childNodes[index] || null);
        });
        while (node.childNodes.length > vnode.children.length) node.lastChild.remove();
        return node;
    }
    return Object.freeze({ snapshot, patch });
})();

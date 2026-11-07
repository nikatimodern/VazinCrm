(() => {
    const root = document.querySelector(".rn-page");
    if (!root) return;
    const cleanup = typeof enableMomentumScrollScope === "function" ? enableMomentumScrollScope(root) : null;
    if (typeof cleanup === "function" && typeof PageRuntime !== "undefined") PageRuntime.current?.cleanups.push(cleanup);
})();

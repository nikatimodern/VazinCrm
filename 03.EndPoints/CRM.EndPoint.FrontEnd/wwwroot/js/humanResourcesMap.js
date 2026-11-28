(() => {
    const root = document.getElementById("hrEmployeeWorkspace");
    if (!root) return;
    const clamp = (v, min, max) => Math.max(min, Math.min(max, v));
    const tileSize = 256;
    const toWorld = (lat, lon, zoom) => {
        const size = tileSize * 2 ** zoom;
        const r = clamp(lat, -85.05112878, 85.05112878) * Math.PI / 180;
        return [(lon + 180) / 360 * size, (1 - Math.asinh(Math.tan(r)) / Math.PI) / 2 * size];
    };
    const toGeo = (x, y, zoom) => {
        const size = tileSize * 2 ** zoom;
        return [Math.atan(Math.sinh(Math.PI * (1 - 2 * y / size))) * 180 / Math.PI,
            ((x / size * 360) % 360 + 360) % 360 - 180];
    };

    for (const picker of root.querySelectorAll(".hr-location")) {
        if (picker.dataset.initialized) continue;
        picker.dataset.initialized = "true";
        const latitude = picker.querySelector('[name="Latitude"]');
        const longitude = picker.querySelector('[name="Longitude"]');
        const panel = picker.querySelector("[data-hr-map-panel]");
        const canvas = picker.querySelector("[data-hr-map-canvas]");
        const label = picker.querySelector("[data-hr-map-value]");
        const load = picker.querySelector("[data-hr-map-load]");
        let zoom = 12, center = [35.6892, 51.3890], active = false;
        const chosen = () => latitude.value !== "" && longitude.value !== "" ?
            [Number(latitude.value), Number(longitude.value)] : null;
        if (chosen()) { center = chosen(); zoom = 16; }
        function showValue() {
            const point = chosen();
            label.textContent = point ? point.map(v => v.toFixed(6)).join(", ") : "موقعیتی انتخاب نشده";
        }
        function render() {
            if (!active || panel.hidden || !canvas.clientWidth) return;
            const width = canvas.clientWidth, height = canvas.clientHeight;
            const [cx, cy] = toWorld(center[0], center[1], zoom);
            const left = cx - width / 2, top = cy - height / 2;
            const count = 2 ** zoom;
            const fragment = document.createDocumentFragment();
            for (let y = Math.floor(top / tileSize); y <= Math.floor((top + height) / tileSize); y++) {
                if (y < 0 || y >= count) continue;
                for (let x = Math.floor(left / tileSize); x <= Math.floor((left + width) / tileSize); x++) {
                    const tile = document.createElement("img");
                    tile.alt = "";
                    tile.draggable = false;
                    tile.src = "https://tile.openstreetmap.org/" + zoom + "/" + ((x % count + count) % count) + "/" + y + ".png";
                    tile.style.left = (x * tileSize - left) + "px";
                    tile.style.top = (y * tileSize - top) + "px";
                    fragment.append(tile);
                }
            }
            const point = chosen();
            if (point) {
                const [px, py] = toWorld(point[0], point[1], zoom);
                const marker = document.createElement("span");
                marker.className = "hr-map-pin";
                marker.style.left = (px - left) + "px";
                marker.style.top = (py - top) + "px";
                marker.setAttribute("aria-hidden", "true");
                fragment.append(marker);
            }
            canvas.replaceChildren(fragment);
        }
        picker.querySelector("[data-hr-map-open]").addEventListener("click", () => {
            panel.hidden = !panel.hidden;
            if (!panel.hidden) render();
        });
        load.addEventListener("click", () => { active = true; load.hidden = true; render(); });
        picker.querySelector("[data-hr-map-clear]").addEventListener("click", () => {
            latitude.value = ""; longitude.value = ""; showValue(); render();
        });
        for (const button of picker.querySelectorAll("[data-hr-map-nav]")) {
            button.addEventListener("click", () => {
                if (!active) return;
                const direction = button.dataset.hrMapNav;
                if (direction === "plus" || direction === "minus") zoom = clamp(zoom + (direction === "plus" ? 1 : -1), 3, 19);
                else {
                    const [x, y] = toWorld(center[0], center[1], zoom);
                    const d = 2 ** zoom * tileSize;
                    center = toGeo(x + (direction === "left" ? -160 : direction === "right" ? 160 : 0),
                        clamp(y + (direction === "up" ? -160 : direction === "down" ? 160 : 0), 0, d), zoom);
                }
                render();
            });
        }
        canvas.addEventListener("click", event => {
            if (!active) return;
            const box = canvas.getBoundingClientRect();
            const [cx, cy] = toWorld(center[0], center[1], zoom);
            const point = toGeo(cx + event.clientX - box.left - canvas.clientWidth / 2,
                clamp(cy + event.clientY - box.top - canvas.clientHeight / 2, 0, tileSize * 2 ** zoom), zoom);
            latitude.value = point[0].toFixed(6); longitude.value = point[1].toFixed(6);
            showValue(); render();
        });
        const resize = new ResizeObserver(() => render());
        resize.observe(canvas);
        window.PageRuntime?.current?.signal?.addEventListener("abort", () => resize.disconnect(), { once: true });
        showValue();
    }
})();

# Responsive breakpoints

Use CSS viewport width, not physical screen resolution. Start with the narrowest layout and add `min-width` queries only where layout needs more space.

| Viewport | CSS width |
| --- | --- |
| Extra small mobile | below 576px |
| Large mobile | 576–767px |
| Tablet portrait | 768–991px |
| Tablet landscape / small laptop | 992–1199px |
| Laptop | 1200–1399px |
| Desktop | 1400–1919px |
| Large desktop | 1920px and above |

Standard query starts: `576px`, `768px`, `992px`, `1200px`, `1400px`, `1920px`, each with `@media (min-width: …)`. Add only the starts needed by a component. A different threshold requires an observed layout failure and a short explanation.

These are defaults for new work, not a mandate to replace existing queries. The current `wwwroot/css/compact.css` uses 768–1599.98px and 1600px for shell density and sidebar width, plus narrower modal ranges. Feature CSS also contains legacy `max-width` queries. Inspect cascade, stylesheet load order, and the actual component before editing; do not silently normalize existing ranges.

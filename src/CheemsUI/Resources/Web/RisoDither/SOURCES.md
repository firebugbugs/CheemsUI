# Riso Dither offline resource

- Reference markup and runtime supplied by the user: `https://cdn.aidesigner.ai/effects/runtime/v1.js`
- Effect script inspected on 2026-08-30: `https://cdn.aidesigner.ai/effects/fx/dither/v1.js`
- This folder contains an app-local WebGL implementation with the source effect's default values: colours `#2c41c4, #8b5cf6, #ff7ab6, #ffe3c2`, background `#0a0e23`, speed `0.3`, 4 px cells, 6 levels, scale `1.5`, contrast `1.2`, 30° flow, detail `0.4`, glow `0.5`, and 8× Bayer matrix.
- It intentionally does not bundle the provider runtime: that runtime fetches another CDN script and calls `api.aidesigner.ai` for licence/watermark state. The local implementation makes no network requests.
- The app-local implementation is licensed under the adjacent `LICENSE`; no provider code or provider licence text is redistributed.

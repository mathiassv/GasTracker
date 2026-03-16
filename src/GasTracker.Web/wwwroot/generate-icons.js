// Generates icon-192.png and icon-512.png in ./icons/
// Run once: node wwwroot/generate-icons.js
// Requires Node.js only (no npm packages).

const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

const outDir = path.join(__dirname, 'icons');
if (!fs.existsSync(outDir)) fs.mkdirSync(outDir);

// ---- PNG writer ----
function crc32(buf) {
    const table = (() => {
        const t = new Uint32Array(256);
        for (let i = 0; i < 256; i++) {
            let c = i;
            for (let k = 0; k < 8; k++) c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1);
            t[i] = c;
        }
        return t;
    })();
    let crc = 0xffffffff;
    for (const b of buf) crc = table[(crc ^ b) & 0xff] ^ (crc >>> 8);
    return (crc ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
    const typeBytes = Buffer.from(type, 'ascii');
    const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
    const crcBuf = crc32(Buffer.concat([typeBytes, data]));
    const crcBytes = Buffer.alloc(4); crcBytes.writeUInt32BE(crcBuf);
    return Buffer.concat([len, typeBytes, data, crcBytes]);
}

function makePng(pixels, size) {
    // pixels: Uint8Array of size*size*4 (RGBA)
    const sig = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);

    const ihdr = Buffer.alloc(13);
    ihdr.writeUInt32BE(size, 0);
    ihdr.writeUInt32BE(size, 4);
    ihdr[8] = 8;  // bit depth
    ihdr[9] = 6;  // RGBA
    // compression, filter, interlace = 0

    const raw = [];
    for (let y = 0; y < size; y++) {
        raw.push(0); // filter type None
        for (let x = 0; x < size; x++) {
            const i = (y * size + x) * 4;
            raw.push(pixels[i], pixels[i+1], pixels[i+2], pixels[i+3]);
        }
    }
    const idat = zlib.deflateSync(Buffer.from(raw));

    return Buffer.concat([
        sig,
        chunk('IHDR', ihdr),
        chunk('IDAT', idat),
        chunk('IEND', Buffer.alloc(0))
    ]);
}

// ---- Draw icon ----
// Blue background (#1b6ec2) with a white gas-pump silhouette

// Simple rasteriser: draw filled rectangles and circles
function drawIcon(size) {
    const pixels = new Uint8Array(size * size * 4);
    const s = size / 192; // scale factor

    function setPixel(x, y, r, g, b, a = 255) {
        x = Math.round(x); y = Math.round(y);
        if (x < 0 || x >= size || y < 0 || y >= size) return;
        const i = (y * size + x) * 4;
        pixels[i] = r; pixels[i+1] = g; pixels[i+2] = b; pixels[i+3] = a;
    }

    function fillRect(x, y, w, h, r, g, b) {
        for (let dy = 0; dy < h; dy++)
            for (let dx = 0; dx < w; dx++)
                setPixel(x + dx, y + dy, r, g, b);
    }

    function fillCircle(cx, cy, radius, r, g, b) {
        const rr = radius * radius;
        for (let dy = -radius; dy <= radius; dy++)
            for (let dx = -radius; dx <= radius; dx++)
                if (dx*dx + dy*dy <= rr)
                    setPixel(cx + dx, cy + dy, r, g, b);
    }

    function fillRoundRect(x, y, w, h, rx, r, g, b) {
        fillRect(x + rx, y, w - rx*2, h, r, g, b);
        fillRect(x, y + rx, rx, h - rx*2, r, g, b);
        fillRect(x + w - rx, y + rx, rx, h - rx*2, r, g, b);
        fillCircle(x + rx, y + rx, rx, r, g, b);
        fillCircle(x + w - rx, y + rx, rx, r, g, b);
        fillCircle(x + rx, y + h - rx, rx, r, g, b);
        fillCircle(x + w - rx, y + h - rx, rx, r, g, b);
    }

    // Background: #1b6ec2
    fillRect(0, 0, size, size, 0x1b, 0x6e, 0xc2);

    const W = 255 , G = 255, B = 255; // white

    // Scale everything to the icon size (designed for 192)
    function sc(v) { return Math.round(v * s); }

    // --- Gas pump body (main rectangle) ---
    fillRoundRect(sc(48), sc(60), sc(72), sc(96), sc(6), W, G, B);

    // --- Pump window (dark blue inset on the body) ---
    fillRoundRect(sc(56), sc(68), sc(56), sc(36), sc(4), 0x1b, 0x6e, 0xc2);

    // --- Nozzle arm (horizontal bar at top right) ---
    fillRect(sc(120), sc(72), sc(28), sc(10), W, G, B);

    // --- Nozzle vertical ---
    fillRect(sc(140), sc(72), sc(10), sc(32), W, G, B);

    // --- Nozzle tip ---
    fillRoundRect(sc(132), sc(100), sc(22), sc(14), sc(5), W, G, B);

    // --- Base plate ---
    fillRect(sc(40), sc(156), sc(88), sc(12), W, G, B);

    return pixels;
}

for (const size of [192, 512]) {
    const pixels = drawIcon(size);
    const png = makePng(pixels, size);
    const outPath = path.join(outDir, `icon-${size}.png`);
    fs.writeFileSync(outPath, png);
    console.log(`Written ${outPath} (${png.length} bytes)`);
}

"""Tasarım tuvalinin kaynaklarından (source/) çevrimdışı açılan tek dosyalık bir galeri (index.html) üretir.

Tuval claude.ai'de: https://claude.ai/artifact/N38DJr48ogJn8sZYS3HsoE
Tuval değişince: dosyaları source/ altına yeniden kopyala, sonra `python docs/design/build.py`.
"""

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
SOURCE = HERE / "source"

TEMPLATE = r"""<!doctype html>
<html lang="tr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Koy arayüz tasarımı</title>
<style>
  :root { --bg: #0F0F11; --panel: #18181B; --line: #2E2E33; --fg: #F4F4F5; --muted: #A1A1AA; --accent: #3B82F6; }
  * { box-sizing: border-box; }
  body.gallery { margin: 0; background: var(--bg); color: var(--fg); font-family: 'Segoe UI Variable Text', 'Segoe UI', system-ui, sans-serif; }
  .top { position: sticky; top: 0; z-index: 10; background: rgba(15,15,17,0.95); border-bottom: 1px solid var(--line); padding: 14px 24px; display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
  .top h1 { font-size: 16px; margin: 0 16px 0 0; }
  .tab { border: 1px solid var(--line); background: transparent; color: var(--muted); border-radius: 999px; padding: 7px 14px; font: inherit; font-size: 13px; cursor: pointer; }
  .tab.on { background: var(--fg); color: #0A0A0B; border-color: var(--fg); }
  .zoom { margin-left: auto; color: var(--muted); font-size: 13px; display: flex; gap: 6px; align-items: center; }
  .zoom select { background: var(--panel); color: var(--fg); border: 1px solid var(--line); border-radius: 6px; padding: 4px 6px; font: inherit; }
  main { padding: 24px; display: flex; flex-direction: column; gap: 32px; }
  .notes { display: flex; flex-wrap: wrap; gap: 12px; }
  .note { background: #3A2A12; border: 1px solid #7C4A0E; color: #FED7AA; border-radius: 10px; padding: 12px 14px; font-size: 13px; max-width: 520px; line-height: 1.45; }
  .note.title { background: transparent; border: none; color: var(--fg); font-size: 26px; font-weight: 700; padding: 0; max-width: none; width: 100%; }
  .board h2 { font-size: 14px; font-weight: 600; color: var(--muted); margin: 0 0 10px; }
  .frame { overflow: hidden; border-radius: 6px; box-shadow: 0 0 0 1px var(--line); background: #FFFFFF; }
  .src { font-size: 12px; color: var(--muted); margin-top: 6px; }
</style>
</head>
<body class="gallery">
<div class="top">
  <h1>Koy arayüz tasarımı</h1>
  <span id="tabs"></span>
  <label class="zoom">Boyut
    <select id="zoom"><option value="fit">Sığdır</option><option value="1">%100</option><option value="0.75">%75</option><option value="0.5">%50</option></select>
  </label>
</div>
<main id="main"></main>
<script>
const DATA = __DATA__;

class DCLogic {
  constructor(props) { this.props = props || {}; this.state = {}; }
  setState(p) {
    const next = typeof p === 'function' ? p(this.state) : p;
    this.state = Object.assign({}, this.state, next);
    if (this.__rerender) this.__rerender();
  }
  forceUpdate() { if (this.__rerender) this.__rerender(); }
}

const parsed = {};
const styled = new Set();
function parse(file) {
  if (parsed[file]) return parsed[file];
  const doc = new DOMParser().parseFromString(DATA.files[file], 'text/html');
  const xdc = doc.querySelector('x-dc');
  const helmet = xdc.querySelector('helmet');
  if (helmet) helmet.remove();
  const script = doc.querySelector('script[data-dc-script]');
  const Cls = new Function('DCLogic', script.textContent + '\nreturn Component;')(DCLogic);
  return (parsed[file] = { xdc, Cls });
}

const HOLE = /\{\{\s*([^}]+?)\s*\}\}/g;
function resolve(expr, scope) {
  expr = expr.trim();
  if (expr === 'true') return true;
  if (expr === 'false') return false;
  if (expr === 'null') return null;
  if (/^-?\d+(\.\d+)?$/.test(expr)) return Number(expr);
  if (/^(['"]).*\1$/.test(expr)) return expr.slice(1, -1);
  let v = scope;
  for (const part of expr.split('.')) { if (v == null) return undefined; v = v[part]; }
  return v;
}
function wholeHole(text) {
  const m = /^\s*\{\{\s*([^}]+?)\s*\}\}\s*$/.exec(text);
  return m ? m[1] : null;
}
function interp(text, scope) {
  return text.replace(HOLE, (_, e) => { const v = resolve(e, scope); return v == null ? '' : String(v); });
}
function camel(s) { return s.replace(/-([a-z])/g, (_, c) => c.toUpperCase()); }

function renderChildren(node, scope, out) {
  for (const child of node.childNodes) {
    const r = renderNode(child, scope);
    if (r) out.append(r);
  }
}

function renderNode(node, scope) {
  if (node.nodeType === 3) return document.createTextNode(interp(node.textContent, scope));
  if (node.nodeType !== 1) return null;
  const tag = node.localName;
  if (tag === 'sc-for') {
    const frag = document.createDocumentFragment();
    const list = resolve(wholeHole(node.getAttribute('list')) || '', scope) || [];
    const as = node.getAttribute('as') || 'item';
    list.forEach((item, i) => renderChildren(node, Object.assign({}, scope, { [as]: item, $index: i }), frag));
    return frag;
  }
  if (tag === 'sc-if') {
    const frag = document.createDocumentFragment();
    const raw = node.getAttribute('value') || '';
    const hole = wholeHole(raw);
    if (hole ? resolve(hole, scope) : raw) renderChildren(node, scope, frag);
    return frag;
  }
  if (tag === 'dc-import') {
    const props = {};
    for (const a of node.attributes) {
      if (a.name === 'name' || a.name.startsWith('hint-')) continue;
      const hole = wholeHole(a.value);
      props[camel(a.name)] = hole ? resolve(hole, scope) : interp(a.value, scope);
    }
    return mount(node.getAttribute('name') + '.dc.html', props);
  }
  const el = document.createElementNS(node.namespaceURI, node.tagName.toLowerCase() === node.tagName ? tag : node.tagName);
  for (const a of node.attributes) {
    const hole = wholeHole(a.value);
    if (a.name.startsWith('on') && hole) {
      const fn = resolve(hole, scope);
      if (typeof fn === 'function') el.addEventListener(a.name.slice(2).toLowerCase(), fn);
      continue;
    }
    el.setAttribute(a.name, a.value.includes('{{') ? interp(a.value, scope) : a.value);
  }
  renderChildren(node, scope, el);
  return el;
}

function mount(file, props) {
  const { xdc, Cls } = parse(file);
  const inst = new Cls(props || {});
  const host = document.createElement('div');
  host.style.display = 'contents';
  inst.__rerender = () => {
    const vals = inst.renderVals() || {};
    host.replaceChildren();
    renderChildren(xdc, vals, host);
  };
  inst.__rerender();
  return host;
}

const canvas = DATA.canvas;
let page = canvas.pages[0].id;
try { page = localStorage.getItem('koy-design-page') || page; } catch (e) {}

function draw() {
  const tabs = document.getElementById('tabs');
  tabs.replaceChildren();
  for (const p of canvas.pages) {
    const b = document.createElement('button');
    b.className = 'tab' + (p.id === page ? ' on' : '');
    b.textContent = p.name;
    b.onclick = () => { page = p.id; try { localStorage.setItem('koy-design-page', page); } catch (e) {} draw(); window.scrollTo(0, 0); };
    tabs.append(b);
  }

  const main = document.getElementById('main');
  main.replaceChildren();
  const notes = document.createElement('div');
  notes.className = 'notes';
  for (const id of canvas.noteOrder || Object.keys(canvas.notes || {})) {
    const n = canvas.notes[id];
    if (!n || (n.page || canvas.pages[0].id) !== page) continue;
    const d = document.createElement('div');
    d.className = 'note' + (n.kind === 'title1' ? ' title' : '');
    d.textContent = n.text;
    notes.append(d);
  }
  main.append(notes);

  const zoomSel = document.getElementById('zoom').value;
  const avail = Math.max(320, window.innerWidth - 48);
  for (const file of canvas.order) {
    const b = canvas.boards[file];
    if (!b || (b.page || canvas.pages[0].id) !== page) continue;
    const zoom = zoomSel === 'fit' ? Math.min(1, avail / b.w) : Number(zoomSel);
    const wrap = document.createElement('section');
    wrap.className = 'board';
    const h = document.createElement('h2');
    h.textContent = b.title || file;
    const frame = document.createElement('div');
    frame.className = 'frame';
    frame.style.width = (b.w * zoom) + 'px';
    frame.style.height = (b.h * zoom) + 'px';
    const inner = document.createElement('div');
    inner.style.width = b.w + 'px';
    inner.style.height = b.h + 'px';
    inner.style.zoom = zoom;
    inner.append(mount(file, {}));
    frame.append(inner);
    const src = document.createElement('div');
    src.className = 'src';
    src.textContent = 'source/' + file;
    wrap.append(h, frame, src);
    main.append(wrap);
  }
}
document.getElementById('zoom').onchange = draw;
window.addEventListener('resize', () => { clearTimeout(window.__rt); window.__rt = setTimeout(draw, 150); });
draw();
</script>
</body>
</html>
"""


def main() -> None:
    canvas = json.loads((SOURCE / "canvas.json").read_text(encoding="utf-8"))
    files = {p.name: p.read_text(encoding="utf-8") for p in sorted(SOURCE.glob("*.dc.html"))}
    data = json.dumps({"canvas": canvas, "files": files}, ensure_ascii=False).replace("</", "<\\/")
    out = HERE / "index.html"
    out.write_text(TEMPLATE.replace("__DATA__", data), encoding="utf-8")
    print(f"{out} yazıldı ({len(files)} artboard)")


if __name__ == "__main__":
    main()

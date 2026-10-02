// Compares two PROBE results (mock vs impl) key by key — shared by the noise-*-parity scripts, which used to
// carry three copies of this walk.
//
// ★ A probe that reports { missing: true } matched NOTHING on that side. It is recorded as absent, never as a
//   divergence: a page with no such element and a page with a different one are different failures.

const isPlainObject = (v) => typeof v === 'object' && v !== null && !Array.isArray(v);
const keysOf = (a, b) => new Set([...Object.keys(a || {}), ...Object.keys(b || {})]);
const join = (path, k) => (path ? `${path}.${k}` : k);

// lowContrast is a list of findings, not a property to match — the scripts report it separately.
const SKIPPED = new Set(['lowContrast']);

function compare(va, vb, at, out) {
  if (va?.missing || vb?.missing) { out.absent.push(at); return; }
  if (isPlainObject(va)) { walk(va, vb, at, out); return; }
  if (JSON.stringify(va) !== JSON.stringify(vb)) {
    out.diffs.push({ prop: at, mock: JSON.stringify(va), impl: JSON.stringify(vb) });
  }
}

function walk(a, b, path, out) {
  for (const k of keysOf(a, b)) {
    if (!SKIPPED.has(k)) compare(a?.[k], b?.[k], join(path, k), out);
  }
}

/** @returns {{ diffs: {prop: string, mock: string, impl: string}[], absent: string[] }} */
function diffProbes(mock, impl) {
  const out = { diffs: [], absent: [] };
  walk(mock, impl, '', out);
  return out;
}

module.exports = { diffProbes };

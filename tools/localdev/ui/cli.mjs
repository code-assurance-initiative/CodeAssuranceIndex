// What the localdev UI scripts share about being run: `--name value` flags, and the site they point at.

/** `--name value` pairs from the command line, as an object. A flag with no value reads as undefined. */
export const args = Object.fromEntries(
  process.argv.slice(2).reduce((pairs, arg, i, all) =>
    arg.startsWith('--') ? [...pairs, [arg.slice(2), all[i + 1]]] : pairs, []));

/** The site under test, without a trailing slash — the local imprint host unless `--base` says otherwise. */
export const base = (args.base ?? 'http://127.0.0.1:5098').replace(/\/$/, '');

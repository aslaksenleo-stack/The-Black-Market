const { chromium } = require('playwright');
(async () => {
  const [,, url, out, w, h] = process.argv;
  const b = await chromium.launch({ args: ['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader'] });
  const p = await b.newPage({ viewport: { width: +w || 1600, height: +h || 1000 } });
  p.on('console', m => console.log('console:', m.text())); p.on('pageerror', e => console.log('error:', e.message));
  await p.goto(url); await p.waitForFunction(() => document.title === 'done', null, { timeout: 120000 });
  await p.screenshot({ path: out, fullPage: true }); await b.close();
})();

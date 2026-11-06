import fs from 'node:fs/promises';
import crypto from 'node:crypto';
const base=process.argv[2]||'https://mdj0126.github.io/growing-city/';
const manifest=JSON.parse(await fs.readFile('web/release-manifest.json','utf8'));
async function read(file){console.log('Checking '+file);const r=await fetch(new URL(file,base),{signal:AbortSignal.timeout(30000)});if(!r.ok)throw Error(file+' HTTP '+r.status);return Buffer.from(await r.arrayBuffer());}
for(const f of manifest.files){const b=await read(f.path);if(crypto.createHash('sha256').update(b).digest('hex')!==f.sha256)throw Error('Hash mismatch '+f.path);console.log('PASS '+f.path);}
for(const code of ['en','ko','ja','zh-Hans','zh-Hant','de','fr','es','pt']){if((await read('Documentation/'+code+'/index.html')).length<100)throw Error('Missing documentation '+code);}
for(const file of ['Documentation/images/gameplay.webp','Documentation/images/bombing.webp'])await read(file);
console.log('PASS: '+manifest.files.length+' exact release hashes, 9 documentation pages and 2 screenshots.');

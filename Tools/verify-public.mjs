import fs from 'node:fs/promises';
import crypto from 'node:crypto';
const base=process.argv[2]||'https://mdj0126.github.io/growing-city/';
const manifest=JSON.parse(await fs.readFile('web/release-manifest.json','utf8'));
for(const f of manifest.files){const r=await fetch(new URL(f.path,base),{signal:AbortSignal.timeout(60000)});if(!r.ok)throw Error(f.path+' HTTP '+r.status);const b=Buffer.from(await r.arrayBuffer());if(crypto.createHash('sha256').update(b).digest('hex')!==f.sha256)throw Error('Hash mismatch '+f.path);}
for(const code of ['en','ko','ja','zh-Hans','zh-Hant','de','fr','es','pt']){const r=await fetch(new URL('Documentation/'+code+'/index.html',base));if(!r.ok||(await r.text()).length<100)throw Error('Missing documentation '+code);}
for(const file of ['Documentation/images/gameplay.webp','Documentation/images/bombing.webp']){const r=await fetch(new URL(file,base));if(!r.ok)throw Error('Missing screenshot '+file);}
console.log('PASS: '+manifest.files.length+' exact release hashes, 9 documentation pages and 2 screenshots.');

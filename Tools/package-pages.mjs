import fs from 'node:fs/promises';
import path from 'node:path';
const root=path.resolve('.'),output=path.join(root,'site');
if(path.relative(root,output)!=='site')throw Error('Unexpected output');
await fs.rm(output,{recursive:true,force:true});await fs.mkdir(output,{recursive:true});
await fs.cp('web',output,{recursive:true});await fs.cp('Documentation',path.join(output,'Documentation'),{recursive:true});
await fs.writeFile(path.join(output,'.nojekyll'),'');console.log('Packaged only compiled game and documentation.');

import fs from 'node:fs/promises';
import path from 'node:path';
import {spawn} from 'node:child_process';
import {serve} from './serve-web.mjs';
const dir=path.resolve('QA/WebPublish'), profile=path.join(dir,'chrome-'+Date.now());
await fs.mkdir(profile,{recursive:true});
const local=process.argv[2]?.startsWith('http')?null:await serve('site');
const url=local?.url||process.argv[2];
const child=spawn('C:/Program Files/Google/Chrome/Application/chrome.exe',['--headless=new','--no-first-run','--no-default-browser-check','--disable-extensions','--user-data-dir='+profile,'--remote-debugging-port=0','--remote-debugging-address=127.0.0.1','--enable-webgl','--ignore-gpu-blocklist','--use-angle=swiftshader','--enable-unsafe-swiftshader','--lang=ko-KR','about:blank'],{windowsHide:true,stdio:'ignore'});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));let ws;const failures=[];let session;let serial=0;const pending=new Map();
try{
 let port;for(let i=0;i<100;i++){try{port=(await fs.readFile(path.join(profile,'DevToolsActivePort'),'utf8')).split('\n')[0];break;}catch{await sleep(200);}}if(!port)throw Error('Chrome debugging endpoint unavailable');
 const info=await(await fetch('http://127.0.0.1:'+port+'/json/version')).json();ws=new WebSocket(info.webSocketDebuggerUrl);await new Promise((r,j)=>{ws.onopen=r;ws.onerror=j;});
 ws.onmessage=e=>{const m=JSON.parse(e.data);if(m.id){const p=pending.get(m.id);pending.delete(m.id);m.error?p?.reject(Error(JSON.stringify(m.error))):p?.resolve(m.result);}else if(m.method==='Runtime.exceptionThrown')failures.push(m.params.exceptionDetails.text+' '+(m.params.exceptionDetails.exception?.description||''));else if(m.method==='Network.responseReceived'&&m.params.response.status>=400)failures.push(m.params.response.status+' '+m.params.response.url);};
 const send=(method,params={},browser=false)=>new Promise((resolve,reject)=>{const id=++serial;pending.set(id,{resolve,reject});ws.send(JSON.stringify({id,method,params,...(!browser&&session?{sessionId:session}:{})}));});
 const target=await send('Target.createTarget',{url:'about:blank'},true);session=(await send('Target.attachToTarget',{targetId:target.targetId,flatten:true},true)).sessionId;
 await send('Page.enable');await send('Runtime.enable');await send('Network.enable');await send('Emulation.setDeviceMetricsOverride',{width:1280,height:720,deviceScaleFactor:1,mobile:false});await send('Page.navigate',{url});
 let ready=false;for(let i=0;i<180;i++){const r=await send('Runtime.evaluate',{expression:'Boolean(window.growingCityReady)',returnByValue:true});if(r.result.value){ready=true;break;}await sleep(1000);}if(!ready)throw Error('Unity startup timeout');
 await sleep(2500);await fs.mkdir('Documentation/images',{recursive:true});
 async function capture(name){const shot=await send('Page.captureScreenshot',{format:'webp',quality:85,fromSurface:true,captureBeyondViewport:false});const file=path.join(dir,name+'.webp');await fs.writeFile(file,Buffer.from(shot.data,'base64'));return file;}
 const before=await capture(local?'gameplay':'public-gameplay');
 await send('Runtime.evaluate',{expression:"window.growingCityInstance.SendMessage('City Prototype — select for tuning','SelectNuke')"});
 await send('Input.dispatchMouseEvent',{type:'mousePressed',x:400,y:380,button:'left',clickCount:1});await sleep(500);await send('Input.dispatchMouseEvent',{type:'mouseMoved',x:850,y:380,button:'left',buttons:1});await sleep(500);await send('Input.dispatchMouseEvent',{type:'mouseReleased',x:850,y:380,button:'left',clickCount:1});await sleep(14000);
 const after=await capture(local?'bombing':'public-bombing');const state=(await send('Runtime.evaluate',{expression:'JSON.stringify({ready:window.growingCityReady,errors:window.growingCityErrors,language:navigator.language,canvas:[document.querySelector("canvas").width,document.querySelector("canvas").height]})',returnByValue:true})).result.value;
 const result={url,state:JSON.parse(state),failures,before,after};await fs.writeFile(path.join(dir,local?'smoke-local.json':'smoke-public.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));if(failures.length||result.state.errors.length)process.exitCode=1;
 await send('Browser.close',{},true);
}finally{ws?.close();if(child.exitCode===null)await Promise.race([new Promise(r=>child.once('exit',r)),sleep(5000)]);if(child.exitCode===null)child.kill();local?.server.close();}

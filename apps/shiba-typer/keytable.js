// desk.js + 설정으로 C# 키 표 생성: node keytable.js cfg.json
const fs=require('fs');const cfg=JSON.parse(fs.readFileSync(process.argv[2]));
global.window={DESK_CFG:cfg.web||{}};global.Image=class{};global.performance={now:()=>0};global.localStorage={getItem:()=>null,setItem:()=>{}};global.addEventListener=()=>{};global.setInterval=()=>{};
require('/home/claude/binder-games/games/shiba-typer/desk.js');const W=window.DeskWidget,K=W.K;
const vk={Escape:0x1B,Backquote:0xC0,Minus:0xBD,Equal:0xBB,Backspace:0x08,Tab:0x09,BracketLeft:0xDB,BracketRight:0xDD,Backslash:0xDC,CapsLock:0x14,Semicolon:0xBA,Quote:0xDE,Enter:0x0D,ShiftLeft:0xA0,ShiftRight:0xA1,Comma:0xBC,Period:0xBE,Slash:0xBF,ControlLeft:0xA2,ControlRight:0xA3,MetaLeft:0x5B,AltLeft:0xA4,AltRight:0xA5,Space:0x20,Lang1:0x15,Lang2:0x19,ArrowLeft:0x25,ArrowUp:0x26,ArrowRight:0x27,ArrowDown:0x28,Insert:0x2D,Home:0x24,PageUp:0x21,Delete:0x2E,End:0x23,PageDown:0x22};
for(let i=1;i<=12;i++)vk['F'+i]=0x6F+i;for(let d=0;d<10;d++){vk['Digit'+d]=0x30+d;vk['Numpad'+d]=0x60+d}for(let c=65;c<=90;c++)vk['Key'+String.fromCharCode(c)]=c;
const L=[];const e=(v,k)=>L.push(`{0x${v.toString(16)},new KeyPos(${k.u.toFixed(4)}f,${k.v.toFixed(4)}f,${k.w}f)}`);
for(const [c,k] of Object.entries(K)){if(vk[c]!==undefined)e(vk[c],k)}e(0x10,K.ShiftLeft);e(0x11,K.ControlLeft);e(0x12,K.AltLeft);
process.stdout.write(JSON.stringify({keys:L.join(',\n            '),kw:(W.KL.U1-W.KL.U0)/15,kh:W.KL.KH||.065}));

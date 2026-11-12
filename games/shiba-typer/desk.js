// 타자 위젯 엔진 — 책상 뒤 캐릭터, 마우스 손은 커서를 따라가고 키보드 손은 누른 키를 침
(function(){
  const IMG={};['scene','mouse','hand_grip','hand_point','hand_up'].forEach(n=>{const i=new Image();i.src=n+'.png';IMG[n]=i});
  const SW=1536,SH=1024;
  // 장면 좌표(1536×1024) 기준 사각형: [왼위, 오른위, 오른아래, 왼아래]
  const PAD=[[254,641],[665,542],[801,713],[370,822]];
  const KB=[[772,566],[1355,417],[1496,550],[892,709]];
  const SH_MOUSE=[575,525], SH_KEY=[905,488];   // 어깨 위치
  const UP_SLEEVE=[1015,430];                   // 손 들었을 때 소매 위치
  const P={mouse:{s:.36,c:[.5,.5]},grip:{s:.40,palm:[.36,.62],sleeve:[.80,.30]},point:{s:.36,tip:[.14,.94],sleeve:[.74,.17]},up:{s:.34,sleeve:[.42,.95]}};
  const lerp=(a,b,t)=>[a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t];
  const quad=(q,u,v)=>lerp(lerp(q[0],q[1],u),lerp(q[3],q[2],u),v);

  // 키 배치 (u: 키보드 가로 0~1, v: 세로 0~1, w: 키 폭 단위)
  const U0=.035,U1=.715,UN=15;const ku=x=>U0+(U1-U0)*x/UN;
  const K={};const row=(v,list)=>list.forEach(([code,x,w])=>K[code]={u:ku(x),v,w:w||1});
  row(.13,[['Escape',.5],...[1,2,3,4,5,6,7,8,9,10,11,12].map(i=>['F'+i,1.6+i*1.05])]);
  row(.29,[['Backquote',.5],...'1234567890'.split('').map((d,i)=>['Digit'+d,1.5+i]),['Minus',11.5],['Equal',12.5],['Backspace',14,2]]);
  row(.44,[['Tab',.75,1.5],...'QWERTYUIOP'.split('').map((c,i)=>['Key'+c,2+i]),['BracketLeft',12],['BracketRight',13],['Backslash',14.25,1.5]]);
  row(.58,[['CapsLock',.9,1.8],...'ASDFGHJKL'.split('').map((c,i)=>['Key'+c,2.25+i]),['Semicolon',11.25],['Quote',12.25],['Enter',14,2.2]]);
  row(.72,[['ShiftLeft',1.1,2.2],...'ZXCVBNM'.split('').map((c,i)=>['Key'+c,2.75+i]),['Comma',9.75],['Period',10.75],['Slash',11.75],['ShiftRight',13.8,2.4]]);
  row(.87,[['ControlLeft',.6,1.2],['MetaLeft',1.9,1.2],['AltLeft',3.2,1.2],['Space',7.2,6],['AltRight',10.9,1.2],['Lang1',12.1,1.1],['Lang2',3.2,1.2],['ControlRight',14.2,1.2]]);
  const RX=(x)=>.74+.23*x;// 오른쪽 방향키·숫자패드 영역
  Object.assign(K,{ArrowUp:{u:RX(.25),v:.72,w:1},ArrowDown:{u:RX(.25),v:.87,w:1},ArrowLeft:{u:RX(.08),v:.87,w:1},ArrowRight:{u:RX(.42),v:.87,w:1},
    Insert:{u:RX(.08),v:.29,w:1},Home:{u:RX(.25),v:.29,w:1},PageUp:{u:RX(.42),v:.29,w:1},Delete:{u:RX(.08),v:.44,w:1},End:{u:RX(.25),v:.44,w:1},PageDown:{u:RX(.42),v:.44,w:1},
    NumpadEnter:{u:RX(.92),v:.8,w:1}});
  ['7','8','9','4','5','6','1','2','3'].forEach((d,i)=>K['Numpad'+d]={u:RX(.6+(i%3)*.15),v:.44+Math.floor(i/3)*.14,w:1});K.Numpad0={u:RX(.68),v:.87,w:1.6};
  const KW=(U1-U0)/UN;

  const st={hits:[],press:null,pressUntil:0,mx:.5,my:.5,tx:.5,ty:.5,click:0};
  function key(code){const t=performance.now();let k=K[code];if(!k){const c=Object.keys(K);k=K[c[(code||'').length*7%c.length]]||K.KeyF}
    st.hits.push({k,t});if(st.hits.length>12)st.hits.shift();st.press=k;st.pressUntil=t+120}
  function cursor(x,y){st.tx=Math.max(0,Math.min(1,x));st.ty=Math.max(0,Math.min(1,y))}
  function click(){st.click=performance.now()}

  function drawSprite(g,img,s,anchor,at,flip){const w=img.naturalWidth*s,h=img.naturalHeight*s;const ax=(flip?1-anchor[0]:anchor[0])*w,ay=anchor[1]*h;
    g.save();g.translate(at[0]-ax,at[1]-ay);if(flip){g.translate(w,0);g.scale(-1,1)}g.drawImage(img,0,0,w,h);g.restore();return{w,h}}
  function sprAnchor(img,s,anchor,at,flip,other){// 같은 스프라이트의 다른 기준점 위치
    const w=img.naturalWidth*s,h=img.naturalHeight*s;const ax=(flip?1-anchor[0]:anchor[0])*w,ay=anchor[1]*h;const ox=(flip?1-other[0]:other[0])*w,oy=other[1]*h;return[at[0]-ax+ox,at[1]-ay+oy]}
  function arm(g,a,b,bend){const mx=(a[0]+b[0])/2,my=(a[1]+b[1])/2-bend;g.lineCap='round';
    g.strokeStyle='#111';g.lineWidth=40;g.beginPath();g.moveTo(...a);g.quadraticCurveTo(mx,my,...b);g.stroke();
    g.strokeStyle='#fbfbff';g.lineWidth=30;g.beginPath();g.moveTo(...a);g.quadraticCurveTo(mx,my,...b);g.stroke();
    g.strokeStyle='rgba(180,170,210,.55)';g.lineWidth=8;g.beginPath();g.moveTo(a[0],a[1]+8);g.quadraticCurveTo(mx,my+8,b[0],b[1]+8);g.stroke()}

  function draw(g,W,H){g.clearRect(0,0,W,H);if(!IMG.scene.complete||!IMG.scene.naturalWidth)return;
    const s=Math.min(W/SW,H/SH);g.save();g.translate((W-SW*s)/2,(H-SH*s)/2);g.scale(s,s);
    g.drawImage(IMG.scene,0,0,SW,SH);
    const t=performance.now();
    // 눌린 키 하늘색으로
    for(const h of st.hits){const a=1-(t-h.t)/220;if(a<=0)continue;const k=h.k;const u0=k.u-KW*k.w/2,u1=k.u+KW*k.w/2,v0=k.v-.065,v1=k.v+.065;
      const c=[quad(KB,u0,v0),quad(KB,u1,v0),quad(KB,u1,v1),quad(KB,u0,v1)];g.fillStyle=`rgba(64,224,255,${.9*a})`;g.beginPath();g.moveTo(...c[0]);c.slice(1).forEach(p=>g.lineTo(...p));g.closePath();g.fill()}
    st.hits=st.hits.filter(h=>t-h.t<220);
    // 마우스: 커서를 부드럽게 따라감
    st.mx+=(st.tx-st.mx)*.35;st.my+=(st.ty-st.my)*.35;
    const mc=quad(PAD,.2+.6*st.mx,.22+.56*st.my);const ce=(t-st.click)/1000;const press=ce<.12?6:0;
    drawSprite(g,IMG.mouse,P.mouse.s,P.mouse.c,mc,false);
    const palm=[mc[0],mc[1]-14+press];const gs=sprAnchor(IMG.hand_grip,P.grip.s,P.grip.palm,palm,false,P.grip.sleeve);
    arm(g,SH_MOUSE,gs,30);drawSprite(g,IMG.hand_grip,P.grip.s,P.grip.palm,palm,false);
    // 키보드 손: 누르는 중이면 그 키로, 아니면 번쩍 든 손
    if(st.press&&t<st.pressUntil){const kp=quad(KB,st.press.u,st.press.v);const flip=kp[0]>SH_KEY[0]+40;
      const sl=sprAnchor(IMG.hand_point,P.point.s,P.point.tip,kp,flip,P.point.sleeve);arm(g,SH_KEY,sl,20);drawSprite(g,IMG.hand_point,P.point.s,P.point.tip,kp,flip)}
    else{const wob=Math.sin(t/260)*4;const at=[UP_SLEEVE[0],UP_SLEEVE[1]+wob];arm(g,SH_KEY,at,-10);drawSprite(g,IMG.hand_up,P.up.s,P.up.sleeve,at,false)}
    g.restore()}
  window.DeskWidget={key,cursor,click,draw,SW,SH,K};
})();

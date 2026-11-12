// 시바 타자왕 공통 애니메이션 코어 (데스크톱 앱 + 웹 버전 공용)
(function(){
  const FR=[0,1,2,3,4,5].map(i=>{const im=new Image();im.src='f'+i+'.png';return im});
  const st={lU:0,rU:0,mU:0,bounce:0,last:'',alt:0,showStats:true};
  const DAY=()=>new Date().toISOString().slice(0,10);
  let today=load();let stamps=[];
  function load(){try{const d=JSON.parse(localStorage.getItem('shiba-stats')||'{}');if(d.day===DAY())return d}catch(e){}return{day:DAY(),count:0,best:0}}
  function save(){try{localStorage.setItem('shiba-stats',JSON.stringify(today))}catch(e){}}
  function key(side){const t=performance.now();if(today.day!==DAY())today={day:DAY(),count:0,best:0};
    if(side==='both'){st.lU=st.rU=t+110}else if(side==='L')st.lU=t+100;else if(side==='R')st.rU=t+100;else{(st.alt^=1)?st.lU=t+100:st.rU=t+100}
    st.bounce=t;today.count++;stamps.push(t);cpm();if(today.count%20===0)save()}
  function mouse(kind){const t=performance.now();st.mU=t+(kind==='click'?900:650);if(kind==='click')st.bounce=t}
  function cpm(){const t=performance.now();while(stamps.length&&stamps[0]<t-60000)stamps.shift();const span=Math.max(10,Math.min(60,(t-(stamps[0]||t))/1000));const v=Math.round(stamps.length*60/span);if(stamps.length>=8&&v>today.best){today.best=v}return stamps.length<3?0:v}
  function frame(){const t=performance.now();const L=t<st.lU,R=t<st.rU,M=t<st.mU;
    if(R)return L?3:2; if(M)return L?5:4; return L?1:0}
  function draw(ctx,W,H){ctx.clearRect(0,0,W,H);const f=FR[frame()];if(!f.complete||!f.naturalWidth)return;
    const t=performance.now();const e=(t-st.bounce)/1000;const b=e<.12?Math.sin(e/.12*Math.PI)*.018:0;
    const top=st.showStats?H*.13:0;const s=Math.min(W/f.naturalWidth,(H-top)/f.naturalHeight);const w=f.naturalWidth*s,h=f.naturalHeight*s;
    ctx.save();ctx.translate(W/2,H);ctx.scale(1+b*.4,1-b);ctx.drawImage(f,-w/2,-h,w,h);ctx.restore();
    if(st.showStats){const c=cpm();const txt=`오늘 ${today.count.toLocaleString()}타 · ${c}타/분`;ctx.font=`700 ${Math.round(H*.055)}px "Noto Sans KR","Malgun Gothic",sans-serif`;const tw=ctx.measureText(txt).width;const px=W/2,py=H*.065;
      ctx.fillStyle='rgba(255,255,255,.93)';ctx.strokeStyle='#222';ctx.lineWidth=Math.max(1.5,H*.008);const bw=tw+H*.07,bh=H*.085;
      ctx.beginPath();ctx.roundRect(px-bw/2,py-bh/2,bw,bh,bh/2);ctx.fill();ctx.stroke();ctx.fillStyle=c>=300?'#e8590c':'#222';ctx.textAlign='center';ctx.textBaseline='middle';ctx.fillText(txt,px,py+1)}}
  window.ShibaCore={key,mouse,draw,reset(){today={day:DAY(),count:0,best:0};stamps=[];save()},setStats(v){st.showStats=v},get stats(){return{...today,cpm:cpm()}},save};
  addEventListener('beforeunload',save);setInterval(save,10000);
})();

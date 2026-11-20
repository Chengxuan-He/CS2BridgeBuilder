// Runs the actual CardGrid/CSS in an offline Chromium CLI, without controlling game UI.
// node tools/CheckUiLayout.mjs <browser.exe> <node_modules> <output-directory> <native-index.css>
import fs from 'node:fs';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
const [browser,modules,out,nativeCssPath]=process.argv.slice(2);
fs.mkdirSync(out,{recursive:true});
const read=p=>fs.readFileSync(p,'utf8');
const source=read('src/BridgeBuilder/UI/BridgeBuilder.mjs').replace('export { hasCSS, register as default };','');
const css=read('src/BridgeBuilder/UI/BridgeBuilder.css');
const harness=`
let trackVisible=false;
window['cs2/ui']={Scrollable:window.React.forwardRef(({children,className,trackVisibility},ref)=>React.createElement('div',{className:'scrollable_DXr y_SMM '+(trackVisible?'track-visible-y_RCA':'track-reserve-x_KAJ')+' '+className},React.createElement('div',{ref,className:'content_gqa'},children),React.createElement('div',{className:'track_e3O y_SMM'})))};window['cs2/input']={FOCUS_DISABLED:0};window['cs2/modding']={getModule:()=>({})};
let loading=false;
window['cs2/api']={bindValue:(group,key,value)=>({key,value}),useValue:b=>b.key==='CatalogLoading'?loading:b.value,trigger:()=>{}};
`;
const test=`
window.addEventListener("error",event=>{document.getElementById("result").textContent=JSON.stringify({cases:0,failures:[{reason:event.message}]});});
const root=ReactDOM.createRoot(document.getElementById('fixture'));
const items=Array.from({length:8},(_,i)=>({id:'item'+i,name:['带顶棚人行桥','桁架高架桥','Truss viaduct','Passerelle couverte','Überdachte Fußgängerbrücke','歩道橋','보행교','Пешеходный мост'][i],icon:'data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg"/>',iconRatio:1}));
const cases=[], failures=[];
for(const width of [240,320.25,480.75,600,987.5]) for(const scale of [.75,1,1.25,1.5,2]) for(const rem of [1,1.25,2]) {
 document.documentElement.style.fontSize=rem+'px';
 const fixture=document.getElementById('fixture');fixture.style.width=width+'px';fixture.style.transform='scale('+scale+')';
 let initialWidth=null, initialContent=null;
 for(const count of [2,8,0,8]) for(const visible of [false,true]) for(const busy of [false,true]) {
  trackVisible=visible;loading=busy;
  ReactDOM.flushSync(()=>root.render(h(ScrollArea,{key:width+"/"+scale+"/"+rem,filterKey:String(count)},h(CardGrid,{items:items.slice(0,count),selectedId:'',onSelect:()=>{},emptyText:'empty'}))));
  for(const card of fixture.querySelectorAll('.bb-card')) {
   const view=card.querySelector('.bb-card-name'), text=card.querySelector('.bb-card-name-run');
   if(text.parentElement.style.width || text.parentElement.style.marginLeft)throw Error('JS changed static title geometry');
   if(text.style.transform)throw Error('Inactive name was translated');
   const vb=view.getBoundingClientRect(),tb=text.getBoundingClientRect();
   if(tb.left<vb.left-1)throw Error('Name start clipped at scale '+scale);
   if(text.offsetWidth<=view.clientWidth*.84 && tb.right>vb.right+1)throw Error('Fitting name clipped at scale '+scale);
   // Font size changes stand in for a late font's changed glyph metrics. The
   // static baseline must remain correct without any observer or JS remeasure.
   for(const size of [8,24,48]) {
    text.style.fontSize=size+'px';
    const ar=text.parentElement.getBoundingClientRect(),tr=text.getBoundingClientRect();
    if(tr.left<ar.left-1)throw Error('Late font caused left clipping');
    const overflow=text.scrollWidth>text.clientWidth+1;
    if(overflow ? Math.abs(tr.left-ar.left)>1 : Math.abs((tr.left+tr.right)-(ar.left+ar.right))>1)throw Error('Static title alignment incorrect');
   }
   text.style.fontSize='';
  }
  const content=fixture.querySelector('.content_gqa');
  const contentWidth=content.clientWidth;
  if(initialContent && initialContent!==content) throw Error('Scroll content remounted');
  initialContent=content;
  if(initialWidth!==null && initialWidth!==contentWidth) failures.push({width,scale,rem,count,visible,busy,reason:'content width changed',initialWidth,contentWidth});
  initialWidth=contentWidth;
  const cells=[...fixture.querySelectorAll('.bb-card-cell')].map(e=>e.getBoundingClientRect());
  const rows=[];for(const b of cells){let r=rows.find(r=>Math.abs(r.top-b.top)<.1);if(!r)rows.push(r={top:b.top,count:0});r.count++;}
  if([...fixture.querySelectorAll('.bb-card-break')].some(e=>e.getBoundingClientRect().height!==0))throw Error('Break added vertical space');
  const counts=rows.map(r=>r.count);const result={width,scale,rem,count,visible,busy,counts,contentWidth};cases.push(result);
  if(counts.some(n=>n!==3) || counts.length!==Math.ceil(count/3))failures.push(result);
 }
}
// Drive actual rendered text through both endpoints with a controlled frame clock.
const activeView=document.querySelector('.bb-card-name'), activeText=activeView.querySelector('.bb-card-name-run');
activeText.style.fontSize='96px';
const realRAF=window.requestAnimationFrame, realCancel=window.cancelAnimationFrame;
const queued=new Map();let next=0;
window.requestAnimationFrame=fn=>{queued.set(++next,fn);return next;};window.cancelAnimationFrame=id=>queued.delete(id);
const advance=t=>{const batch=[...queued.values()];queued.clear();batch.forEach(fn=>fn(t));};
const originalX=activeText.getBoundingClientRect().left;
const stopActive=bindCardName(activeView,activeText,true);
const travel=Math.max(activeText.offsetWidth,activeText.scrollWidth)-activeView.clientWidth*.84;
if(!(travel>0))throw Error('Long-title animation fixture did not overflow');
const leg=Math.max(700,1000*travel/Math.max(1,activeText.offsetHeight*1.15));
advance(0);advance(leg);
if(Math.abs(activeText.getBoundingClientRect().left-(originalX-travel*2))>1)throw Error('Right endpoint did not reach the rendered target');
advance(leg+2999);
if(Math.abs(activeText.getBoundingClientRect().left-(originalX-travel*2))>1)throw Error('Right endpoint did not pause');
advance(2*leg+3000);
if(Math.abs(activeText.getBoundingClientRect().left-originalX)>1)throw Error('Return did not reach left origin');
stopActive();if(queued.size)throw Error('Animation callbacks leaked');
// Exercise selected-state text replacement through real React layout effects.
activeText.style.fontSize='';
const renderSelected=name=>ReactDOM.flushSync(()=>root.render(h(ScrollArea,{filterKey:'selected-title'},h(CardGrid,{items:[{...items[0],name}],selectedId:items[0].id,onSelect:()=>{},emptyText:'empty'}))));
renderSelected('A');
const selectedText=document.querySelector('.bb-card-name-run');
const selectedView=document.querySelector('.bb-card-name');
for(let i=0;i<10;i++)advance(i*16);
if(selectedText.style.transform || queued.size)throw Error('Selected short title animates');
const centered=()=>{const a=selectedText.parentElement.getBoundingClientRect(),b=selectedText.getBoundingClientRect();return Math.abs(a.left+a.right-b.left-b.right)<1;};
if(!centered())throw Error('Selected short title is not centered');
renderSelected('A very long bridge title with many characters to exercise the actual final glyph');
if(document.querySelector('.bb-card-name-run')!==selectedText)throw Error('Title replacement remounted selected card');
const range=document.createRange();range.selectNodeContents(selectedText);
const fullWidth=range.getBoundingClientRect().width/2;
if(selectedText.scrollWidth<=selectedText.offsetWidth || Math.abs(selectedText.scrollWidth-fullWidth)>1)throw Error('Constrained long text lost intrinsic width');
const selectedTravel=Math.max(selectedText.offsetWidth,selectedText.scrollWidth)-selectedView.clientWidth*.84;
const selectedLeg=Math.max(700,1000*selectedTravel/Math.max(1,selectedText.offsetHeight*1.15));
advance(1000);advance(1000+selectedLeg);
const region=selectedText.parentElement.getBoundingClientRect(),glyphs=range.getBoundingClientRect();
if(Math.abs(glyphs.right-region.right)>2 || glyphs.right>selectedView.getBoundingClientRect().right)throw Error('Actual final glyph missed normal-region endpoint');
advance(1000+selectedLeg+2999);
if(Math.abs(range.getBoundingClientRect().right-region.right)>2)throw Error('Selected long title endpoint pause lost');
advance(1000+2*selectedLeg+3000);
if(Math.abs(range.getBoundingClientRect().left-region.left)>2)throw Error('Selected long title did not return to left fade boundary');
renderSelected('A');
if(selectedText.style.transform || !centered())throw Error('Long to short replacement retained displacement');
for(let i=0;i<10;i++)advance(20000+i*16);
if(queued.size)throw Error('Selected short replacement retained animation');
ReactDOM.flushSync(()=>root.render(h(ScrollArea,{filterKey:'selected-title'},h(CardGrid,{items:[{...items[0],name:'A'}],selectedId:'',onSelect:()=>{},emptyText:'empty'}))));
if(selectedText.style.transform || queued.size || !centered())throw Error('Deselect did not preserve static centered baseline');
window.requestAnimationFrame=realRAF;window.cancelAnimationFrame=realCancel;
const beforeFrame=[...document.querySelectorAll('.bb-card')].map(e=>{const r=e.getBoundingClientRect();return [r.x,r.y,r.width,r.height];});
requestAnimationFrame(()=>{
 const afterFrame=[...document.querySelectorAll('.bb-card')].map(e=>{const r=e.getBoundingClientRect();return [r.x,r.y,r.width,r.height];});
 if(JSON.stringify(beforeFrame)!==JSON.stringify(afterFrame))failures.push({reason:'new card moved after first frame',beforeFrame,afterFrame});
 ReactDOM.flushSync(()=>root.unmount());
 const menuHost=document.createElement('div');menuHost.className='bb-panel';
 menuHost.style.cssText='position:fixed;display:block;left:20px;right:auto;bottom:auto;width:300px;pointer-events:auto';document.body.appendChild(menuHost);
 const menuRoot=ReactDOM.createRoot(menuHost);let dropdownCases=0;
 for(const rem of [1,1.25,2]) for(const y of [10,window.innerHeight-50]) for(const count of [2,7,12]) {
  document.documentElement.style.fontSize=rem+'px';menuHost.style.top=y+'px';
  const options=Array.from({length:count},(_,i)=>({id:String(i),name:items[i%items.length].name}));
  ReactDOM.flushSync(()=>menuRoot.render(h(FilterBox,{key:rem+'/'+y+'/'+count,value:'0',onChange:()=>{},options,label:'Filter'})));
  ReactDOM.flushSync(()=>menuHost.querySelector('.bb-filter > button').click());
  const popup=menuHost.querySelector('.bb-filter-menu');const box=popup.getBoundingClientRect();
  if(box.top<-.5 || box.bottom>window.innerHeight+.5)throw Error('Dropdown leaves screen '+JSON.stringify({rem,y,count,box}));
  const rows=[...popup.querySelectorAll('[role=option]')];
  if(rows.some(row=>Math.abs(row.getBoundingClientRect().height-36*rem)>.01))throw Error('Dropdown row height mismatch');
  const viewport=popup.querySelector('.content_gqa');
  if(viewport) {
   if(Math.abs(viewport.getBoundingClientRect().height-7*36*rem)>.01)throw Error('Not exactly seven row viewport');
   for(const end of [0,1]) {
    viewport.scrollTop=end?viewport.scrollHeight:0;
    const vb=viewport.getBoundingClientRect();
    const fully=rows.filter(row=>{const b=row.getBoundingClientRect();return b.top>=vb.top-1&&b.bottom<=vb.bottom+1;});
    if(fully.length!==7 || fully[0]!==rows[end?count-7:0] || fully[6]!==rows[end?count-1:6])throw Error('Dropdown endpoint lost complete rows');
   }
  } else {
   const style=getComputedStyle(popup);
   const outer=['paddingTop','paddingBottom','borderTopWidth','borderBottomWidth'].reduce((n,k)=>n+parseFloat(style[k]),0);
   if(Math.abs(box.height-count*36*rem-outer)>.01)throw Error('Short dropdown has extra or clipped space');
  }
  if(rows.some(row=>row.title!==row.textContent))throw Error('Dropdown loses full localized label');
  dropdownCases++;
 }
 ReactDOM.flushSync(()=>menuRoot.unmount());menuHost.remove();
 document.getElementById('result').textContent=JSON.stringify({cases:cases.length,dropdownCases,failures});
});
`;
for(const [name,code] of [['after',source]]){
 const html='<!doctype html><html><head><link rel="stylesheet" href="'+new URL('file:///'+nativeCssPath.replaceAll('\\','/')).href+'"><style>html{font-size:1px}.bordered .bb-grid{border:3.25px solid;padding:5.25px}.scroll{height:180px!important;overflow-y:scroll!important}'+css+'</style></head><body><div id="fixture" class="bb-panel" style="position:relative;display:flex;height:200px;top:0;left:0;transform-origin:top left"></div><pre id="result"></pre><script>'+read(path.join(modules,'react/umd/react.development.js'))+'</script><script>'+read(path.join(modules,'react-dom/umd/react-dom.development.js'))+'</script><script>'+harness+code+test+'</script></body></html>';
 const file=path.resolve(out,name+'.html');fs.writeFileSync(file,html);
 const run=spawnSync(browser,['--headless','--window-size=1280,1000','--virtual-time-budget=2000','--disable-gpu','--no-first-run','--no-default-browser-check','--user-data-dir='+path.resolve(out,'profile-'+name),'--dump-dom','file:///'+file.replaceAll('\\','/')],{encoding:'utf8',timeout:60000,maxBuffer:8*1024*1024});
 if(run.error)throw run.error;
 fs.writeFileSync(path.join(out,name+'-browser.log'),run.stderr||'');
 const match=run.stdout?.match(/<pre id="result">(.*?)<\/pre>/s);if(!match)throw Error('Missing result: '+run.status);
 const result=JSON.parse(match[1].replaceAll('&quot;','"').replaceAll('&amp;','&'));
 fs.writeFileSync(path.join(out,name+'.json'),JSON.stringify(result,null,2));console.log(name,JSON.stringify({cases:result.cases,dropdownCases:result.dropdownCases,failures:result.failures.length,firstFailure:result.failures[0]}));
 if(name==='after'&&result.failures.length)process.exitCode=1;

}

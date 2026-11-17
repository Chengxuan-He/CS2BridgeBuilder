// Symmetric construction-space geometry, orthographic projection and polygon BSP visibility.
// Run from the repository root. No raster artwork or runtime asset is read or rewritten.
const fs = require('fs');
const add=(a,b)=>a.map((x,i)=>x+b[i]), sub=(a,b)=>a.map((x,i)=>x-b[i]);
const mul=(a,s)=>a.map(x=>x*s), dot=(a,b)=>a.reduce((s,x,i)=>s+x*b[i],0);
const cross=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];
const unit=a=>mul(a,1/Math.hypot(...a));
const view=unit([.7,.85,.638]), eps=1e-7;
const project=([x,y,z])=>[12+.85*x-.7*y,24.5+.45*x+.38*y-z];
let faces=[], cablePairs=[];
function face(v,c){if(Math.hypot(...cross(sub(v[1],v[0]),sub(v[2],v[0])))>eps)faces.push({v,c});}
function box(x,y,z,dx,dy,dz,color){
 const p=[[x,y,z],[x+dx,y,z],[x+dx,y+dy,z],[x,y+dy,z],[x,y,z+dz],[x+dx,y,z+dz],[x+dx,y+dy,z+dz],[x,y+dy,z+dz]];
 for(const [ids,c] of [[[0,1,2,3],'#999999'],[[0,4,5,1],'#999999'],[[1,5,6,2],'#999999'],[[2,6,7,3],'#c6c6c6'],[[3,7,4,0],'#c6c6c6'],[[4,7,6,5],'#dedede']])face(ids.map(i=>p[i]),color||c);
}
function tube(a,b,r,color){
 const d=unit(sub(b,a)), u=unit(cross(d,Math.abs(d[2])>.95?[0,1,0]:[0,0,1])), v=cross(d,u);
 const ring=p=>Array.from({length:6},(_,i)=>add(p,add(mul(u,r*Math.cos(i*Math.PI/3)),mul(v,r*Math.sin(i*Math.PI/3)))));
 const ar=ring(a),br=ring(b);face(ar,color);face(br.slice().reverse(),color);
 for(let i=0;i<6;i++)face([ar[i],ar[(i+1)%6],br[(i+1)%6],br[i]],color);
}
function deck(){
 box(0,0,-2,50,12,2,'#555555');
 face([[0,0,0],[50,0,0],[50,12,0],[0,12,0]],'#686868');
 for(const y of [.25,11.35])box(0,y,.015,50,.4,.08,'#d8d8d8');
 box(0,5.85,.015,50,.3,.025,'#aa9a59');
}
function column(profile){
 for(let i=1;i<profile.length;i++){
  const [ya,za]=profile[i-1],[yb,zb]=profile[i];
  const a=[[23.75,ya-1,za],[26.25,ya-1,za],[26.25,ya+1,za],[23.75,ya+1,za]],b=[[23.75,yb-1,zb],[26.25,yb-1,zb],[26.25,yb+1,zb],[23.75,yb+1,zb]];
  face(a,'#999999');face(b,'#dedede');
  for(let j=0;j<4;j++)face([a[j],a[(j+1)%4],b[(j+1)%4],b[j]],j===1?'#999999':'#c6c6c6');
 }
}
function nanpu(){
 const profile=[[0,-10],[-3,0],[1,15.25],[0,27]];
 column(profile);column(profile.map(([y,z])=>[12-y,z]));
 for(const y of [0,12])box(23.5,y-1.7,-11.5,3,3.4,1.5);
 box(23.75,-3,-4,2.5,18,1.5);
 box(23.75,0,14.25,2.5,12,2);
 // Every end pair is reflected in 3D around x=25; both cable planes around y=6.
 for(const y of [0,12])for(const distance of [6,12,18,24]){
  const z=17+8*distance/24;
  const a=[25,y,z],b=[25-distance,y,.04],c=[25+distance,y,.04];
  tube(a,b,.20,'#303030');tube(a,c,.20,'#303030');cablePairs.push({a,b,c});
 }
 deck();
}
function lupu(){
 const height=x=>20*(1-((x-25)/20)**2);
 for(const y of [0,12]){
  const stations=[2,6,10,14,18,22,25,28,32,36,40,44,48];
  const ring=x=>[[x,y-.65,height(x)-.9],[x,y+.65,height(x)-.9],[x,y+.65,height(x)+.9],[x,y-.65,height(x)+.9]];
  for(let i=1;i<stations.length;i++){
   const a=ring(stations[i-1]),b=ring(stations[i]);
   for(let j=0;j<4;j++)face([a[j],a[(j+1)%4],b[(j+1)%4],b[j]],j===1?'#c6c6c6':'#dedede');
  }
  face(ring(stations[0]),'#999999');face(ring(stations.at(-1)),'#999999');
  for(const x of [8,14,20,25,30,36,42])tube([x,y,-2],[x,y,height(x)],.27,'#888888');
  for(const x of [2,48])box(x-1.3,y-1.3,-8,2.6,2.6,3);
 }
 for(const x of [12,20,30,38])tube([x,0,height(x)],[x,12,height(x)],.40,'#a8a8a8');
 deck();
}
function plane(p){const n=unit(cross(sub(p.v[1],p.v[0]),sub(p.v[2],p.v[0])));return {n,w:dot(n,p.v[0])};}
function split(p,pl){
 const d=p.v.map(v=>dot(pl.n,v)-pl.w),pos=d.some(x=>x>eps),neg=d.some(x=>x< -eps);
 if(!pos&&!neg)return [null,null,p];if(!neg)return [p,null,null];if(!pos)return [null,p,null];
 const f=[],b=[];
 for(let i=0;i<p.v.length;i++){
  const j=(i+1)%p.v.length,a=p.v[i],q=p.v[j];if(d[i]>=-eps)f.push(a);if(d[i]<=eps)b.push(a);
  if((d[i]>eps&&d[j]<-eps)||(d[i]<-eps&&d[j]>eps)){const v=add(a,mul(sub(q,a),d[i]/(d[i]-d[j])));f.push(v);b.push(v);}
 }
 return [f.length>=3?{v:f,c:p.c}:null,b.length>=3?{v:b,c:p.c}:null,null];
}
function bsp(items){
 if(!items.length)return null;
 // A broad face keeps common deck intersections shallow; subsequent crossing polygons split.
 let pivot=0,best=-1;for(let i=0;i<items.length;i++){const p=items[i];const area=Math.hypot(...cross(sub(p.v[1],p.v[0]),sub(p.v[2],p.v[0])));if(area>best){best=area;pivot=i;}}
 const pl=plane(items[pivot]),same=[],front=[],back=[];
 for(const p of items){const [f,b,c]=split(p,pl);if(f)front.push(f);if(b)back.push(b);if(c)same.push(c);}
 return {pl,same,front:bsp(front),back:bsp(back)};
}
function order(node,result){if(!node)return;const positive=dot(view,node.pl.n)>0;order(positive?node.back:node.front,result);result.push(...node.same);order(positive?node.front:node.back,result);}
for(const [name,build] of [['Nanpu',nanpu],['Lupu',lupu]]){
 faces=[];cablePairs=[];build();
 for(const {a,b,c} of cablePairs){if(Math.abs(b[0]+c[0]-2*a[0])>eps||b[1]!==c[1]||b[2]!==c[2])throw Error('Cable reflection');}
 const vertices=faces.flatMap(p=>p.v);
 // Check construction-space symmetry, independent of projection and depth sorting.
 for(const v of vertices)for(const reflected of [[50-v[0],v[1],v[2]],[v[0],12-v[1],v[2]]])
  if(!vertices.some(q=>Math.hypot(...sub(q,reflected))<1e-6))throw Error(`${name}: asymmetric 3D vertex`);
 const ordered=[];order(bsp(faces),ordered);
 const points=ordered.flatMap(p=>p.v.map(project));if(points.some(p=>p.some(x=>x<1||x>63)))throw Error('Icon clipping');
 const polygons=ordered.map(p=>`<polygon points="${p.v.map(v=>project(v).map(x=>x.toFixed(3)).join(',')).join(' ')}" fill="${p.c}" stroke="${p.c}" stroke-width="0.025" stroke-linejoin="round"/>`);
 fs.writeFileSync(`assets/Bridge${name}.svg`,`<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 64 64" role="img" aria-labelledby="title desc">\n<title id="title">${name} Bridge</title>\n<desc id="desc">Symmetric three-dimensional bridge geometry projected isometrically with split-face depth ordering. Transparent native catalogue palette.</desc>\n${polygons.join('\n')}\n</svg>\n`);
 console.log(`${name}: ${faces.length} construction faces, ${ordered.length} depth-split SVG faces; ${cablePairs.length} symmetric cable pairs.`);
}





/*
 * Local Riso Dither WebGL implementation.
 * Fixed to the public effect's documented default attributes so the Demo never
 * loads a CDN script or calls a licensing/API endpoint at runtime.
 */
(() => {
  const host = document.querySelector('[data-aifx="dither"]');
  if (!host) return;

  const vertex = `attribute vec2 a_pos;varying vec2 v_uv;void main(){v_uv=a_pos*.5+.5;gl_Position=vec4(a_pos,0.,1.);}`;
  const fragment = `
precision highp float;
uniform vec2 u_res;uniform float u_time,u_speed,u_px,u_levels,u_scale,u_contrast,u_angle,u_detail,u_matrix,u_glow,u_bg_alpha;
uniform vec3 u_bg,u_c0,u_c1,u_c2,u_c3;
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453123);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.-2.*f);return mix(mix(hash(i),hash(i+vec2(1.,0.)),f.x),mix(hash(i+vec2(0.,1.)),hash(i+vec2(1.,1.)),f.x),f.y);}
float fbm(vec2 p){float v=0.,a=.5;for(int i=0;i<4;i++){v+=a*noise(p);p=p*2.13+vec2(11.3,7.7);a*=.5;}return v;}
float bayer2(vec2 a){a=floor(a);return fract(a.x*.5+a.y*a.y*.75);}float bayer4(vec2 a){return bayer2(.5*a)*.25+bayer2(a);}float bayer8(vec2 a){return bayer4(.5*a)*.25+bayer2(a);}
vec3 pick(float i){if(i<.5)return u_c0;if(i<1.5)return u_c1;if(i<2.5)return u_c2;return u_c3;}
vec3 ramp(float x){float f=clamp(x,0.,1.)*4.,i=floor(min(f,3.999)),fr=f-i;vec3 a=i<.5?u_bg:pick(i-1.);return sqrt(mix(a*a,pick(i)*pick(i),fr));}
void main(){float px=max(u_px,1.);vec2 cell=floor(gl_FragCoord.xy/px),suv=(cell+.5)*px/u_res;float aspect=u_res.x/max(u_res.y,1.),tt=u_time*u_speed;
vec2 uvA=vec2(suv.x*aspect,suv.y),dir=vec2(cos(u_angle),sin(u_angle)),rp=vec2(dot(uvA,dir),dot(uvA,vec2(-dir.y,dir.x))),p=vec2(rp.x*.62,rp.y*1.45)*u_scale;p.x-=tt*.24;
vec2 q=vec2(fbm(p+vec2(0.,tt*.07)),fbm(p+vec2(4.7,2.3)-tt*.05));float v=fbm(p+(q-.5)*1.6+vec2(tt*.03,0.)),d=fbm(p*2.6+vec2(-tt*.17,0.)+(q-.5)*1.4);v+=(d-.5)*u_detail*.5;
vec2 sun=vec2(aspect*.5,.5)+vec2(.32*aspect*sin(tt*.4+1.7),.25*cos(tt*.31));v+=exp(-3.*length(uvA-sun))*u_glow*.55;vec2 ctr=(uvA-vec2(aspect*.5,.5))/vec2(max(aspect,1.)*.62,.62);v-=smoothstep(.45,1.5,length(ctr))*.34;
v=clamp((v-.58)*u_contrast+.40,0.,1.);v=mix(v,v*v*(3.-2.*v),.6);float bay=u_matrix<3.?bayer2(cell):(u_matrix<6.?bayer4(cell):bayer8(cell));float L=max(u_levels,2.)-1.,vq=clamp(floor(v*L+bay)/L,0.,1.);vec3 col=ramp(vq);col*=1.+(hash(cell*.37+7.)-.5)*.04;float alpha=mix(u_bg_alpha,1.,vq);gl_FragColor=vec4(col,alpha);}`;

  const gl = host.appendChild(document.createElement('canvas')).getContext('webgl', { alpha: true, antialias: false, premultipliedAlpha: false });
  if (!gl) return;
  gl.canvas.style.cssText = 'position:absolute;inset:0;width:100%;height:100%;display:block;';
  const compile = (type, source) => { const shader = gl.createShader(type); gl.shaderSource(shader, source); gl.compileShader(shader); return shader; };
  const program = gl.createProgram(); gl.attachShader(program, compile(gl.VERTEX_SHADER, vertex)); gl.attachShader(program, compile(gl.FRAGMENT_SHADER, fragment)); gl.linkProgram(program); gl.useProgram(program);
  const buffer = gl.createBuffer(); gl.bindBuffer(gl.ARRAY_BUFFER, buffer); gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1,-1,3,-1,-1,3]), gl.STATIC_DRAW);
  const position = gl.getAttribLocation(program, 'a_pos'); gl.enableVertexAttribArray(position); gl.vertexAttribPointer(position, 2, gl.FLOAT, false, 0, 0);
  const uniform = name => gl.getUniformLocation(program, name);
  const color = (name, hex) => { const n = parseInt(hex.slice(1), 16); gl.uniform3f(uniform(name), (n >> 16 & 255) / 255, (n >> 8 & 255) / 255, (n & 255) / 255); };
  [['u_bg','#0a0e23'],['u_c0','#2c41c4'],['u_c1','#8b5cf6'],['u_c2','#ff7ab6'],['u_c3','#ffe3c2']].forEach(([name, hex]) => color(name, hex));
  [['u_speed',.3],['u_px',4],['u_levels',6],['u_scale',1.5],['u_contrast',1.2],['u_angle',Math.PI/6],['u_detail',.4],['u_glow',.5],['u_matrix',8],['u_bg_alpha',1]].forEach(([name, value]) => gl.uniform1f(uniform(name), value));
  const resize = () => { const dpr = Math.min(devicePixelRatio || 1, 1.5), width = Math.max(1, Math.round(host.clientWidth * dpr)), height = Math.max(1, Math.round(host.clientHeight * dpr)); if (gl.canvas.width !== width || gl.canvas.height !== height) { gl.canvas.width = width; gl.canvas.height = height; gl.viewport(0, 0, width, height); gl.uniform2f(uniform('u_res'), width, height); } };
  const resizeObserver = new ResizeObserver(resize); resizeObserver.observe(host); resize();
  let frame, animationEnabled = true, elapsed = 0, previous = performance.now(), previewFrozen = false;
  const draw = now => { resize(); elapsed += animationEnabled ? (now - previous) / 1000 : 0; previous = now; gl.uniform1f(uniform('u_time'), elapsed); gl.drawArrays(gl.TRIANGLES, 0, 3); frame = animationEnabled ? requestAnimationFrame(draw) : null; };
  window.__cheemsUpdate = settings => {
    if (previewFrozen) return;
    color('u_c1', settings.color);
    gl.uniform1f(uniform('u_speed'), settings.speed);
    gl.uniform1f(uniform('u_bg_alpha'), settings.backgroundAlpha);
    gl.uniform1f(uniform('u_px'), settings.pixelSize);
    gl.uniform1f(uniform('u_levels'), settings.levels);
    gl.uniform1f(uniform('u_scale'), settings.scale);
    gl.uniform1f(uniform('u_contrast'), settings.contrast);
    gl.uniform1f(uniform('u_angle'), settings.flowAngle * Math.PI / 180);
    gl.uniform1f(uniform('u_detail'), settings.detail);
    gl.uniform1f(uniform('u_glow'), settings.glow);
    const wasEnabled = animationEnabled;
    animationEnabled = settings.enabled;
    previous = performance.now();
    if (animationEnabled && (!wasEnabled || frame === null)) frame = requestAnimationFrame(draw);
    if (!animationEnabled) {
      if (frame !== null) cancelAnimationFrame(frame);
      frame = null;
      draw(previous);
    }
  };
  frame = requestAnimationFrame(draw);
  if (window.__cheemsPreview) {
    setTimeout(() => requestAnimationFrame(() => {
      if (previewFrozen) return;
      try {
        resize();
        gl.drawArrays(gl.TRIANGLES, 0, 3);
        gl.finish();
        const image = new Image();
        image.src = gl.canvas.toDataURL('image/png');
        image.style.cssText = 'width:100%;height:100%;display:block;object-fit:cover';
        if (frame !== null) cancelAnimationFrame(frame);
        frame = null;
        resizeObserver.disconnect();
        host.replaceChildren(image);
        gl.getExtension('WEBGL_lose_context')?.loseContext();
        previewFrozen = true;
        window.chrome?.webview?.postMessage('preview-frozen');
      } catch (_) {
        if (frame !== null) cancelAnimationFrame(frame);
        frame = null;
        animationEnabled = false;
        window.chrome?.webview?.postMessage('preview-frozen');
      }
    }), 1000);
  }
  addEventListener('pagehide', () => cancelAnimationFrame(frame), { once: true });
})();

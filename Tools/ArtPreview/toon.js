import * as THREE from './node_modules/three/build/three.module.js';
// Approximation of Assets/Shaders/Toon.shader: banded light with tinted shadow side + vertex-alpha glow + ambient.
export function toonMaterial(lightDir) {
  return new THREE.ShaderMaterial({
    
    uniforms: { uLight: { value: lightDir.clone().normalize() }, uLightColor: { value: new THREE.Color(1.0, 0.86, 0.68).multiplyScalar(1.15) },
      uShadow: { value: new THREE.Color(0.42, 0.33, 0.40) }, uSky: { value: new THREE.Color(0.42, 0.33, 0.30) }, uGround: { value: new THREE.Color(0.16, 0.11, 0.09) },
      uRim: { value: new THREE.Vector4(1.0, 0.82, 0.6, 0.0) } },
    vertexShader: `attribute vec4 vcol; varying vec4 vC; varying vec3 vN; varying vec3 vV;
      void main(){ vC = vcol; vN = normalize(mat3(modelMatrix) * normal); vec4 wp = modelMatrix * vec4(position,1.0); vV = normalize(cameraPosition - wp.xyz); gl_Position = projectionMatrix * viewMatrix * wp; }`,
    fragmentShader: `uniform vec3 uLight, uLightColor, uShadow, uSky, uGround; uniform vec4 uRim; varying vec4 vC; varying vec3 vN; varying vec3 vV;
      void main(){ vec3 n = normalize(vN); float ndl = dot(n, uLight)*0.5+0.5; float ramp = smoothstep(0.48-0.09, 0.48+0.09, ndl);
        vec3 albedo = vC.rgb; float glow = clamp(1.0 - vC.a, 0.0, 1.0);
        vec3 amb = mix(uGround, uSky, n.y*0.5+0.5);
        vec3 c = albedo*(1.0-glow*0.5) * (uLightColor * mix(uShadow, vec3(1.0), ramp) + amb) + albedo*glow*1.4;
        float rim = pow(1.0 - clamp(dot(vV, n),0.0,1.0), 3.0); c += uRim.rgb*uRim.a*rim;
        gl_FragColor = vec4(pow(c, vec3(1.0/2.2)), 1.0); }`
  });
}
export function glassMaterial() {
  return new THREE.ShaderMaterial({ transparent: true, depthWrite: false, 
    vertexShader: `attribute vec4 vcol; varying vec4 vC; varying vec3 vN; varying vec3 vV; void main(){ vC=vcol; vN=normalize(mat3(modelMatrix)*normal); vec4 wp=modelMatrix*vec4(position,1.0); vV=normalize(cameraPosition-wp.xyz); gl_Position=projectionMatrix*viewMatrix*wp; }`,
    fragmentShader: `varying vec4 vC; varying vec3 vN; varying vec3 vV; void main(){ vec3 n=normalize(vN); float rim=pow(1.0-clamp(dot(n,vV),0.0,1.0),2.5)*0.55;
      vec3 h=normalize(normalize(vec3(-0.35,1.0,0.25))+vV); float spec=smoothstep(0.92,0.97,clamp(dot(n,h),0.0,1.0))*0.55;
      vec4 c=vec4(0.75,0.88,0.92,0.18)*vC; c.rgb+=vec3(1.0,0.95,0.85)*rim+spec; c.a=clamp(c.a+rim*0.6+spec,0.0,1.0); gl_FragColor=vec4(pow(c.rgb,vec3(1.0/2.2)),c.a);} `});
}
export function buildPart(part, toon, glass) {
  const g = new THREE.BufferGeometry();
  g.setAttribute('position', new THREE.Float32BufferAttribute(part.p, 3));
  g.setAttribute('normal', new THREE.Float32BufferAttribute(part.n, 3));
  g.setAttribute("vcol", new THREE.Uint8BufferAttribute(part.c, 4, true));
  g.setIndex(part.i);
  const m = new THREE.Mesh(g, part.mat === "Glass" ? glass : toon);
  // Unity is left-handed (z forward), three.js right-handed: mirror Z and flip winding via scale.
  m.position.set(part.off[0], part.off[1], part.off[2]);
  const d = Math.PI/180; m.rotation.set(part.rot[0]*d, part.rot[1]*d, part.rot[2]*d, 'YXZ');
  return m;
}

async function fetchJSON(url){
  const res=await fetch(url);
  if(!res.ok) throw new Error(`HTTP ${res.status} for ${url}`);
  return res.json();
}

function gateBadge(g){
  const tags=(g.tags||[]).map(t=>`<span class="badge">${t}</span>`).join(" ");
  const jetway=g.has_jetway?"<span class=badge>jetway</span>":"";
  return `${tags}${jetway}`;
}

function shortPos(p){
  if(!p) return "(n/a)";
  const lat=p.lat?.toFixed(6), lon=p.lon?.toFixed(6), hdg=p.heading??"";
  return `${lat}, ${lon} ${hdg!==""?`(${hdg}°)`:""}`;
}

function summarize(data){
  const n=(data.gates||[]).length;
  return `Airport: ${data.airport||"Unknown"} — Gates: ${n}`;
}

function renderGates(list){
  return list.map(g=>{
    const svc=(g.services||[]).map(s=>s.type).join(", ")||"none";
    const stop=shortPos(g.parking_system_stop_position||g.position);
    return `<div class="gate">
      <h3>${g.ui_name||g.gate_id||"Gate"}</h3>
      <div class="meta">Type: ${g.gate_type??"?"} | Jetway: ${g.has_jetway?"Yes":"No"} | Services: ${svc}</div>
      <div class="meta">Pos: ${stop}</div>
      <div>${gateBadge(g)}</div>
      <div style="margin-top:6px">
        <button data-action="request-pushback" data-gate="${g.gate_id}">Request Pushback</button>
        <button data-action="request-fuel" data-gate="${g.gate_id}">Request Fuel</button>
      </div>
    </div>`;
  }).join("");
}

async function requestAction(action, payload){
  // Bridge endpoint (stub): later this will call local http://localhost:8787/api/action
  console.log("Action", action, payload);
  try{
    await fetch("http://localhost:8787/api/action",{
      method:"POST",
      headers:{"Content-Type":"application/json"},
      body:JSON.stringify({action, payload})
    });
  }catch(e){
    console.warn("Bridge not running; action is a no-op for now.");
  }
}

async function main(){
  const icaoSpan=document.getElementById("icao");
  const refreshBtn=document.getElementById("refresh");
  const select=document.getElementById("profileSelect");
  const summary=document.getElementById("summary");
  const gatesEl=document.getElementById("gates");

  async function loadProfile(url){
    const data=await fetchJSON(url);
    summary.textContent=summarize(data);
    icaoSpan.textContent=`ICAO: ${data.airport||"N/A"}`;
    gatesEl.innerHTML=renderGates(data.gates||[]);
  }

  select.addEventListener("change", ()=>loadProfile(select.value));
  refreshBtn.addEventListener("click", ()=>loadProfile(select.value));
  gatesEl.addEventListener("click", (e)=>{
    const btn=e.target.closest("button[data-action]");
    if(!btn) return;
    const action=btn.getAttribute("data-action");
    const gate=btn.getAttribute("data-gate");
    requestAction(action,{gate});
  });

  await loadProfile(select.value);
}

main();

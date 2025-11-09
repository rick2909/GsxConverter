console.log('[GroundEquipment] Script loading...');

function initialize() {
  console.log('[GroundEquipment] Starting initialization...');

  const icaoSpan = document.getElementById("icao");
  const refreshBtn = document.getElementById("refresh");
  const select = document.getElementById("profileSelect");
  const summary = document.getElementById("summary");
  const gatesEl = document.getElementById("gates");

  console.log('[GroundEquipment] DOM elements:', {
    icao: !!icaoSpan,
    refresh: !!refreshBtn,
    select: !!select,
    summary: !!summary,
    gates: !!gatesEl
  });

  if (!icaoSpan || !refreshBtn || !select || !summary || !gatesEl) {
    console.error('[GroundEquipment] Missing required DOM elements!');
    setTimeout(initialize, 100); // Retry
    return;
  }

  console.log('[GroundEquipment] All DOM elements found, setting up handlers...');

    async function fetchJSON(url) {
      const res = await fetch(url);
      if (!res.ok) throw new Error(`HTTP ${res.status} for ${url}`);
      return res.json();
    }

    function gateBadge(g) {
      const tags = (g.tags || []).map(t => `<span class="badge">${t}</span>`).join(" ");
      const jetway = g.has_jetway ? '<span class="badge">jetway</span>' : "";
      return `${tags}${jetway}`;
    }

    function shortPos(p) {
      if (!p) return "(n/a)";
      const lat = (p.lat && p.lat.toFixed) ? p.lat.toFixed(6) : "?";
      const lon = (p.lon && p.lon.toFixed) ? p.lon.toFixed(6) : "?";
      const hdg = (p.heading !== undefined && p.heading !== null) ? p.heading : "";
      return `${lat}, ${lon}${hdg !== "" ? ` (${hdg}°)` : ""}`;
    }

    function summarize(data) {
      const n = (data.gates || []).length;
      return `Airport: ${data.airport || "Unknown"} — Gates: ${n}`;
    }

    function renderGates(list) {
      return list.map(g => {
        const svc = (g.services || []).map(s => s.type).join(", ") || "none";
        const stop = shortPos(g.parking_system_stop_position || g.position);
        const gateType = (g.gate_type !== undefined && g.gate_type !== null) ? g.gate_type : "?";
        return `<div class="gate">
          <h3>${g.ui_name || g.gate_id || "Gate"}</h3>
          <div class="meta">Type: ${gateType} | Jetway: ${g.has_jetway ? "Yes" : "No"} | Services: ${svc}</div>
          <div class="meta">Pos: ${stop}</div>
          <div>${gateBadge(g)}</div>
          <div style="margin-top:6px">
            <button data-action="request-pushback" data-gate="${g.gate_id}">Request Pushback</button>
            <button data-action="request-fuel" data-gate="${g.gate_id}">Request Fuel</button>
          </div>
        </div>`;
      }).join("");
    }

    async function requestAction(action, payload) {
      console.log("Action", action, payload);
      try {
        await fetch("http://localhost:8787/api/action", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ action, payload })
        });
      } catch (e) {
        console.warn("Bridge not running; action is a no-op for now.");
      }
    }

    async function loadProfile(url) {
      console.log('[GroundEquipment] Loading profile from:', url);
      try {
        const data = await fetchJSON(url);
        console.log('[GroundEquipment] Profile data loaded:', data);
        
        summary.textContent = summarize(data);
        icaoSpan.textContent = `ICAO: ${data.airport || "N/A"}`;
        gatesEl.innerHTML = renderGates(data.gates || []);
        
        console.log('[GroundEquipment] UI updated successfully');
      } catch (error) {
        console.error('[GroundEquipment] Failed to load profile:', error);
        summary.textContent = 'Error loading profile: ' + error.message;
      }
    }

    // Event listeners
    select.addEventListener("change", () => {
      console.log('[GroundEquipment] Profile changed');
      loadProfile(select.value);
    });
    
    refreshBtn.addEventListener("click", () => {
      console.log('[GroundEquipment] Refresh clicked');
      loadProfile(select.value);
    });
    
    gatesEl.addEventListener("click", (e) => {
      const btn = e.target.closest("button[data-action]");
      if (!btn) return;
      const action = btn.getAttribute("data-action");
      const gate = btn.getAttribute("data-gate");
      requestAction(action, { gate });
    });

  console.log('[GroundEquipment] Event listeners attached, loading initial profile...');
  
  // Load initial profile
  loadProfile(select.value);
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initialize);
} else {
  initialize();
}

console.log('[GroundEquipment] Script loaded successfully');

# Ground Equipment Handler - MSFS EFB App

An iframe-based EFB application for Microsoft Flight Simulator        ├── index-[hash].js          # Contains EHAM gates JSON (794 KB)
       └── index-[hash].css         # Styles (10 KB)
   ├── Assets/                          # EFB assets
   └── GroundEquipmentApp.js            # EFB wrapper (153 KB) provides ground service management for GSX Pro airports.

## 🏗️ Architecture

This project uses a **hybrid architecture** that separates the webapp from the MSFS EFB wrapper:

- **Webapp**: Standard React app with hot-reload development (no MSFS dependencies)
- **EFB Wrapper**: Minimal MSFS component that loads the webapp in an iframe
- **Communication**: postMessage API for bidirectional messaging
- **Build**: Unified build system that outputs directly to MSFS Packages folder

```
Ground Equipment Handler/
├── webapp/                                    # Standard React webapp (develop here!)
│   ├── src/
│   │   ├── components/                        # React components
│   │   │   ├── GateList.tsx                   # Gate browsing view
│   │   │   ├── GateSelection.tsx              # Gate selection options
│   │   │   └── GateOperations.tsx             # At-gate service management
│   │   ├── data/
│   │   │   └── eham-gates.json                # Real EHAM airport gate data
│   │   ├── types/
│   │   │   └── GateData.ts                    # TypeScript interfaces
│   │   ├── App.tsx                            # Main app with routing
│   │   └── main.tsx                           # Entry point
│   ├── vite.config.ts                         # Vite config with --embedded flag support
│   └── package.json
│
└── EFB app/
    ├── Packages/                              # MSFS project loader reads from here!
    │   └── xperiaplay-efb-groundequipmentapp/
    │       └── TemplateApp/                   # Build outputs here
    │           ├── webapp/                    # Built webapp
    │           ├── Assets/                    # EFB assets
    │           └── GroundEquipmentApp-iframe.js
    │
    └── PackageSources/TemplateApp/
        ├── src/
        │   ├── Assets/
        │   │   └── eham-gates.json            # Source gate data
        │   └── GroundEquipmentApp.tsx         # EFB wrapper (loads webapp in iframe)
        ├── build.js                           # Build script
        └── package.json
```

## ⚠️ Latest Update (Nov 16, 2025)

**XML Configuration Fixed!** The package definition was pointing to the old build location. This has been corrected:
- ✅ XML now points to `Packages/xperiaplay-efb-groundequipmentapp/TemplateApp/`
- ✅ MSFS will mount at `coui://html_ui/efb_ui/efb_apps/GroundEquipmentHandler/`
- ✅ Fresh build completed with real EHAM gate data
- **Next step**: Restart MSFS and test!

---

## 🚀 Development Workflow

### 1. Develop the Webapp (Fast Iteration with Hot-Reload)

```powershell
cd "d:\Projects\GSX-Project\Ground Equipment Handler\webapp"
npm run dev
```

- Opens http://localhost:3000 in your browser
- Changes update instantly (no MSFS restart needed!)
- Uses real EHAM gates data from `eham-gates.json`
- Full React DevTools support

### 2. Build for MSFS

```powershell
# Build both webapp and EFB wrapper (builds directly to Packages folder!)
cd "d:\Projects\GSX-Project\Ground Equipment Handler\EFB app\PackageSources\TemplateApp"
npm run build

# That's it! MSFS will load directly from the Packages folder.
# Just restart MSFS to see your changes.
```

Or use the quick rebuild command (cleans first):
```powershell
cd "d:\Projects\GSX-Project\Ground Equipment Handler\EFB app\PackageSources\TemplateApp"
npm run rebuild
```

### What the Build Does

1. **Builds the webapp** (Vite with `--embedded` flag)
   - Compiles TypeScript
   - Bundles React components with real EHAM gates data (794 KB)
   - Optimizes assets
   - Outputs directly to `EFB app/Packages/xperiaplay-efb-groundequipmentapp/TemplateApp/webapp/`

2. **Builds EFB wrapper** (esbuild)
   - Compiles `GroundEquipmentApp.tsx`
   - Creates minimal iframe wrapper (153 KB)
   - Copies Assets folder
   - Outputs directly to `EFB app/Packages/xperiaplay-efb-groundequipmentapp/TemplateApp/`

3. **Final structure in Packages folder (MSFS loads from here):**
   ```
   EFB app/Packages/xperiaplay-efb-groundequipmentapp/TemplateApp/
   ├── webapp/                          # React webapp
   │   ├── index.html
   │   └── assets/
   │       ├── index-[hash].js          # Contains EHAM gates JSON (794 KB)
   │       └── index-[hash].css         # Styles (10 KB)
   ├── Assets/                          # EFB assets
   └── GroundEquipmentApp-iframe.js     # EFB wrapper (153 KB)
   ```

4. **No deployment needed!**
   - MSFS project loader reads directly from Packages folder
   - Just build and restart MSFS
   - The XML files configure MSFS to mount the app from this location

## 📡 Communication Architecture

The webapp and EFB wrapper communicate via the postMessage API:

### EFB → Webapp Messages

```typescript
// Send airport data to webapp
window.postMessage({
  type: 'AIRPORT_DATA',
  data: airportData
}, '*');

// Navigate webapp
window.postMessage({
  type: 'NAVIGATE',
  view: 'gateSelection'
}, '*');
```

### Webapp → EFB Messages

```typescript
// Request airport data
parent.postMessage({
  type: 'REQUEST_AIRPORT_DATA'
}, '*');

// Request ground service
parent.postMessage({
  type: 'GROUND_SERVICE',
  action: 'FOLLOW_ME',
  gateId: 'D1'
}, '*');
```

## 🎯 Features

### Current Features
- ✅ Real EHAM airport gate data (105+ gates)
- ✅ Gate browsing with filtering and search
- ✅ Gate grouping (Piers D, E, F, G, H, M)
- ✅ Pre-gate options (Follow Me, Show Spot, Warp)
- ✅ At-gate service management UI
- ✅ Hot-reload development environment
- ✅ Direct build to MSFS Packages folder

### Pending Implementation
- 🔄 SimConnect integration for real aircraft data
- 🔄 Ground service spawning (Follow Me, GSE vehicles)
- 🔄 Real-time service progress tracking
- 🔄 Multi-airport support (currently EHAM only)
- 🔄 Aircraft position tracking

## 🛠️ Tech Stack

- **Webapp**: React 18.3.1, TypeScript 5.6.3, Vite 5.4.21
- **EFB**: MSFS SDK, esbuild 0.21.3, SCSS
- **Communication**: postMessage API
- **Data**: Real GSX Pro gate configurations (JSON)

## 📝 Adding New Airports

1. Export gate data from GSXConverter to JSON
2. Copy JSON to `webapp/src/data/[airport]-gates.json`
3. Update `App.tsx` to load the new airport data
4. Rebuild and test in MSFS

## 🐛 Debugging

### Webapp (Standalone)
- Use browser DevTools at http://localhost:3000
- Full React DevTools support
- Console.log works normally

### Webapp (In MSFS)
- Open Coherent debugger (Ctrl+Alt+D in MSFS)
- Look for console messages from webapp
- Messages from both EFB and webapp appear in the same console

### EFB Wrapper
- Open Coherent debugger
- Check for postMessage communication logs
- Verify iframe is loading correctly

## 📦 Build Outputs

**Webapp Bundle:** 794 KB (includes all EHAM gate data)
**EFB Wrapper:** 153 KB (minimal iframe loader)
**Total Size:** ~950 KB

## 🔗 Related Projects

- **GsxConverter**: Converts GSX configs to JSON format
- **MSFS/gsx-bridge**: SimConnect bridge for real-time aircraft data

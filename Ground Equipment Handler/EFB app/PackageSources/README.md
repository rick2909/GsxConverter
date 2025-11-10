# Ground Equipment Handler EFB App

### Optional
  Open the `GroundEquipmentApp.code-workspace` file inside of the GroundEquipmentApp\ folder with Visual Studio Code.


______________
### Install

You should have node JS `(> v18.0)` and NPM `(> v5.0)`

**Run this command in `efb_api\` and `GroundEquipmentApp\` folders:**
```bash
$ npm install
```

______________
### Build

Run this command in GroundEquipmentApp\ folder to build the app once:
```bash
$ npm run build
```
______________
### Watch

Run this command in GroundEquipmentApp\ folder to build the app each time you make a modification:
```bash
$ npm run watch
```
______________
### Test in Browser

To test the app in a regular web browser (easier debugging with DevTools):

1. Build the app: `npm run build`
2. Start a local server in the `dist` folder:
   ```bash
   cd dist
   npx http-server -p 8080 --cors
   ```
3. Open your browser to: `http://localhost:8080`
4. Open DevTools (F12) to see console logs and inspect elements

**Note:** Some EFB-specific features may not work in browser, but layout and most functionality will.

______________
### Environment variables

You can edit your environment variables in `GroundEquipmentApp\.env`

| Name | Description | Should be used in | Defaults to |
| ----------- | ----------- | ----------- | ----------- |
| TYPECHECKING | Whether or not to typecheck files on build | development | true
| SOURCE_MAPS | Source maps are useful when debugging because it links the output files to the source files. | development | true
| MINIFY | Minify abstracts variables names and make code ligther. | production | false
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Check if building for MSFS embedded or standalone
let msfsEmbedded = false;
process.argv.forEach((val) => {
  if (val === '--embedded') {
    msfsEmbedded = true;
  }
});

// Output directory based on mode
const output_dir = msfsEmbedded 
  ? path.resolve(__dirname, '../EFB app/Packages/xperiaplay-efb-groundequipmentapp/TemplateApp/webapp')
  : path.resolve(__dirname, 'dist');

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  base: msfsEmbedded ? './' : '/', // Use relative paths for MSFS embedded mode
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@components': path.resolve(__dirname, './src/components'),
      '@types': path.resolve(__dirname, './src/types'),
      '@data': path.resolve(__dirname, './src/data'),
    },
  },
  json: {
    stringify: false, // Allow importing JSON as JavaScript objects
  },
  build: {
    outDir: output_dir,
    assetsDir: 'assets',
    sourcemap: true,
    emptyOutDir: true,
    target: 'es2017', // Coherent GT in MSFS uses older JavaScript
    minify: 'terser', // Better compatibility than esbuild
  },
  server: {
    port: 3000,
    open: true,
  },
});

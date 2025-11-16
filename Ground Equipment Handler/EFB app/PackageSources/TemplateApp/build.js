const copyStaticFiles = require("esbuild-copy-static-files");
const globalExternals = require("@fal-works/esbuild-plugin-global-externals");
const { typecheckPlugin } = require("@jgoz/esbuild-plugin-typecheck");
const esbuild = require("esbuild");
const postcss = require("postcss");
const postCssUrl = require("postcss-url");
const postcssPrefixSelector = require("postcss-prefix-selector");
const sassPlugin = require("esbuild-sass-plugin");
const { execSync } = require("child_process");
const fs = require("fs-extra");
const path = require("path");

require("dotenv").config({ path: __dirname + "/.env" });

const env = {
  typechecking: process.env.TYPECHECKING === "true",
  sourcemaps: process.env.SOURCE_MAPS === "true",
  minify: process.env.MINIFY === "true",
};

// Build directly to MSFS Packages folder (MSFS will load from here)
const output_dir = path.resolve(__dirname, "../../Packages/xperiaplay-efb-groundequipmentapp/TemplateApp");

// Build the webapp first
async function buildWebapp() {
  console.log("📦 Building webapp (embedded mode)...");
  const webappDir = path.resolve(__dirname, "../../../webapp");
  
  try {
    // Check if webapp directory exists
    if (!fs.existsSync(webappDir)) {
      console.log("⚠️ Webapp directory not found, skipping webapp build");
      return;
    }

    // Build the webapp with --embedded flag
    execSync("npm run build:embedded", {
      cwd: webappDir,
      stdio: "inherit",
    });

    console.log("✅ Webapp built to build/ground-equipment-handler/dist/webapp");
  } catch (error) {
    console.error("❌ Error building webapp:", error);
    throw error;
  }
}

const baseConfig = {
  entryPoints: ["src/GroundEquipmentApp.tsx"],
  keepNames: true,
  bundle: true,
  outdir: output_dir,
  sourcemap: env.sourcemaps,
  minify: env.minify,
  logLevel: "info",
  loader: {
    ".html": "copy",
    ".json": "copy",
  },
  target: "es2017",
  define: { 
    BASE_URL: `"coui://html_ui/efb_ui/efb_apps/GroundEquipmentHandler"` 
  },
  plugins: [
    copyStaticFiles({
      src: "./src/Assets",
      dest: path.join(output_dir, "Assets"),
    }),
    globalExternals.globalExternals({
      "@microsoft/msfs-sdk": {
        varName: "msfssdk",
        type: "cjs",
      },
      "@workingtitlesim/garminsdk": {
        varName: "garminsdk",
        type: "cjs",
      },
    }),
    sassPlugin.sassPlugin({
      async transform(source) {
        const { css } = await postcss([
          postCssUrl({
            url: "copy",
          }),
          postcssPrefixSelector({
            prefix: `.efb-view.GroundEquipmentApp`,
          }),
        ]).process(source, { from: undefined });
        return css;
      },
    }),
  ],
};

if (env.typechecking) {
  baseConfig.plugins.push(
    typecheckPlugin({ watch: process.env.SERVING_MODE === "WATCH" })
  );
}

async function build() {
  try {
    // Build webapp first
    await buildWebapp();
    
    // Then build EFB wrapper
    console.log("📦 Building EFB wrapper...");
    
    if (process.env.SERVING_MODE === "WATCH") {
      const ctx = await esbuild.context(baseConfig);
      await ctx.watch();
    } else if (process.env.SERVING_MODE === "SERVE") {
      const ctx = await esbuild.context(baseConfig);
      await ctx.serve({ port: process.env.PORT_SERVER });
    } else if (["", undefined].includes(process.env.SERVING_MODE)) {
      await esbuild.build(baseConfig);
      console.log("✅ EFB wrapper built successfully");
      console.log(`📁 Output directory: ${output_dir}`);
    } else {
      console.error(`MODE ${process.env.SERVING_MODE} is unknown`);
    }
  } catch (error) {
    console.error("❌ Build failed:", error);
    process.exit(1);
  }
}

console.log("Building Ground Equipment Handler...");
build();

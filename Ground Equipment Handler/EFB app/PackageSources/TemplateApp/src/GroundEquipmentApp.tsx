import {
  App,
  AppBootMode,
  AppInstallProps,
  AppSuspendMode,
  AppView,
  AppViewProps,
  Efb,
  RequiredProps,
  TVNode,
} from "@efb/efb-api";
import { FSComponent, VNode } from "@microsoft/msfs-sdk";
import { GateList } from "./Components/GateList";
import { GateDetail } from "./Components/GateDetail";
import { DeIceList } from "./Components/DeIceList";
import { DeIceDetail } from "./Components/DeIceDetail";
import { MetadataView } from "./Components/MetadataView";
import { AirportData } from "./types/GateData";

import "./GroundEquipmentApp.scss";

/**
 * BASE_URL is a global var defined in build.js
 * It points to the dist folder of the app when builded.
 * Mainly used to load assets (icons, fonts, etc)
 */
declare const BASE_URL: string;

interface GroundEquipmentAppViewProps extends RequiredProps<AppViewProps, "bus"> {
  airportData: AirportData;
}

class GroundEquipmentAppView extends AppView<GroundEquipmentAppViewProps> {
  /**
   * Optional property
   * Default view key to show if using AppViewService
   */
  protected defaultView = "GateList";

  /**
   * Optional method
   * Views (page or popup) to register if using AppViewService
   * Default behavior : nothing
   */
  protected registerViews(): void {
    this.appViewService.registerPage("GateList", () => (
      <GateList appViewService={this.appViewService} airportData={this.props.airportData} />
    ));
    this.appViewService.registerPage("GateDetail", () => (
      <GateDetail appViewService={this.appViewService} />
    ));
    this.appViewService.registerPage("DeIceList", () => (
      <DeIceList appViewService={this.appViewService} airportData={this.props.airportData} />
    ));
    this.appViewService.registerPage("DeIceDetail", () => (
      <DeIceDetail appViewService={this.appViewService} />
    ));
    this.appViewService.registerPage("MetadataView", () => (
      <MetadataView appViewService={this.appViewService} airportData={this.props.airportData} />
    ));
  }

  /**
   * Optional method
   * Method called when AppView is open after it's creation
   * Default behavior : nothing
   */
  public onOpen(): void {
    console.log("GroundEquipmentAppView onOpen - airportData:", {
      airport: this.props.airportData.airport,
      gates: this.props.airportData.gates?.length,
      groups: this.props.airportData.gate_groups?.length
    });
    
    // Force open the default view to ensure it re-renders with loaded data
    this.appViewService.open(this.defaultView);
  }

  /**
   * Optional method
   * Method called when AppView is closed
   * Default behavior : nothing
   */
  public onClose(): void {
    //
  }

  /**
   * Optional method
   * Method called when AppView is resumed (equivalent of onOpen but happen every time we go back to this app)
   * Default behavior : nothing
   */
  public onResume(): void {
    //
  }

  /**
   * Optional method
   * Method called when AppView is paused (equivalent of onClose but happen every time we switch to another app)
   * Default behavior : nothing
   */
  public onPause(): void {
    //
  }

  /**
   * Optional method
   * Default behavior is rendering AppContainer which works with AppViewService
   * We usually surround it with <div class="ground-equipment-app">{super.render}</div>
   * Can render anything (JSX, Component) so it doesn't require to use AppViewService and/or AppContainer
   * @returns VNode
   */
  public render(): VNode {
    return <div class="ground-equipment-app">{super.render()}</div>;
  }
}

class GroundEquipmentApp extends App {
  private airportData: AirportData = {
    airport: "Loading...",
    version: "0.1",
    gates: []
  };

  /**
   * Required getter for friendly app-name.
   * Used by the EFB as App's name shown to the user.
   * @returns string
   */
  public get name(): string {
    return "Ground Equipment Handler";
  }

  /**
   * Required getter for app's icon url.
   * Used by the EFB as App's icon shown to the user.
   * @returns string
   */
  public get icon(): string {
    return `${BASE_URL}/Assets/app-icon.svg`;
  }

  /**
   * Optional attribute
   * Allow to choose BootMode between COLD / WARM / HOT
   * Default behavior : AppBootMode.COLD
   *
   * COLD : No dom preloaded in memory
   * WARM : App -> AppView are loaded but not rendered into DOM
   * HOT : App -> AppView -> Pages are rendered and injected into DOM
   */
  public BootMode = AppBootMode.HOT;

  /**
   * Optional attribute
   * Allow to choose SuspendMode between SLEEP / TERMINATE
   * Default behavior : AppSuspendMode.SLEEP
   *
   * SLEEP : Default behavior, does nothing, only hiding and sleeping the app if switching to another one
   * TERMINATE : Hiding the app, then killing it by removing it from DOM (BootMode is checked on next frame to reload it and/or to inject it, see BootMode)
   */
  public SuspendMode = AppSuspendMode.SLEEP;

  /**
   * Optional method
   * Allow to resolve some dependencies, install external data, check an api key, etc...
   * @param _props props used when app has been setted up.
   * @returns Promise<void>
   */
  public async install(_props: AppInstallProps): Promise<void> {
    console.log("Ground Equipment Handler: Starting installation...");
    console.log("BASE_URL:", BASE_URL);
    
    try {
      const cssPath = `${BASE_URL}/GroundEquipmentApp.css`;
      console.log("Loading CSS from:", cssPath);
      await Efb.loadCss(cssPath);
      console.log("✅ CSS loaded successfully");
    } catch (error) {
      console.error("❌ Failed to load CSS:", error);
    }
    
    // Load the airport gate data
    try {
      console.log("Fetching gate data from:", `${BASE_URL}/Assets/eham-gates.json`);
      const response = await fetch(`${BASE_URL}/Assets/eham-gates.json`);
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      this.airportData = await response.json();
      console.log("✅ Loaded airport data successfully!");
      console.log("Airport:", this.airportData?.airport);
      console.log("Gates count:", this.airportData?.gates?.length);
      console.log("Groups count:", this.airportData?.gate_groups?.length);
    } catch (error) {
      console.error("❌ Failed to load airport data:", error);
      // Provide fallback empty data
      this.airportData = {
        airport: "Error Loading Data",
        version: "1.0",
        gates: []
      };
    }
    
    return Promise.resolve();
  }

  /**
   * Optional method
   * Allows to specify an array of compatible ATC MODELS.
   * Your app will be visible but greyed out if the aircraft is not compatible.
   * if undefined or method not implemented, the app will be visible for all aircrafts.
   * @returns string[] | undefined
   */
  public get compatibleAircraftModels(): string[] | undefined {
    return undefined;
  }

  /*
   * @returns {AppView} created above
   */
  public render(): TVNode<GroundEquipmentAppView> {
    return <GroundEquipmentAppView bus={this.bus} airportData={this.airportData} />;
  }
}

/**
 * App definition to be injected into EFB
 */
Efb.use(GroundEquipmentApp);

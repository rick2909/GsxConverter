import {
  App,
  AppBootMode,
  AppInstallProps,
  AppSuspendMode,
  AppView,
  AppViewProps,
  Efb,
  GamepadUiView,
  RequiredProps,
  TVNode,
} from "@efb/efb-api";
import { FSComponent, NodeReference, VNode } from "@microsoft/msfs-sdk";
import { AirportData } from "./types/GateData";

import "./GroundEquipmentApp.scss";

/**
 * BASE_URL is a global var defined in build.js
 */
declare const BASE_URL: string;

interface MainPageProps extends RequiredProps<AppViewProps, "bus"> {
  airportData: AirportData;
}

/**
 * MainPage - The main view that renders the iframe to the webapp
 */
class MainPage extends GamepadUiView<HTMLDivElement, MainPageProps> {
  public readonly tabName = MainPage.name;
  private iframeRef = FSComponent.createRef<HTMLIFrameElement>();
  private messageQueue: any[] = [];

  public onAfterRender(): void {
    // Listen for messages from the webapp
    window.addEventListener('message', this.handleWebappMessage.bind(this));
    
    // Wait for iframe to load, then send initial data
    if (this.iframeRef.instance) {
      this.iframeRef.instance.addEventListener('load', () => {
        console.log('Iframe loaded, sending airport data...');
        this.sendToWebapp({
          type: 'AIRPORT_DATA',
          data: this.props.airportData
        });
        
        // Send any queued messages
        this.messageQueue.forEach(msg => this.sendToWebapp(msg));
        this.messageQueue = [];
      });
    }
  }

  private handleWebappMessage(event: MessageEvent): void {
    const message = event.data;
    console.log('EFB received message from webapp:', message);

    switch (message.type) {
      case 'REQUEST_AIRPORT_DATA':
        this.sendToWebapp({
          type: 'AIRPORT_DATA',
          data: this.props.airportData
        });
        break;
        
      case 'GROUND_SERVICE':
        console.log(`Ground service requested: ${message.action} for gate ${message.gateId}`);
        // TODO: Implement actual ground service logic via SimConnect
        // For now, just log it
        break;
        
      default:
        console.log('Unknown message from webapp:', message.type);
    }
  }

  private sendToWebapp(message: any): void {
    if (this.iframeRef.instance && this.iframeRef.instance.contentWindow) {
      console.log('Sending to webapp:', message);
      this.iframeRef.instance.contentWindow.postMessage(message, '*');
    } else {
      // Queue the message if iframe isn't ready yet
      this.messageQueue.push(message);
    }
  }

  public render(): VNode {
    // Use the deployed webapp URL
    // For development, you can change this to 'http://localhost:3000'
    const webappUrl = `${BASE_URL}/webapp/index.html`;

    return (
      <div ref={this.gamepadUiViewRef} className="main-page" style="height: 100%; width: 100%;">
        <iframe 
          ref={this.iframeRef}
          src={webappUrl}
          title="Ground Equipment Handler"
          style="width: 100%; height: 100%; border: none;"
        />
      </div>
    );
  }
}

interface GroundEquipmentAppViewProps extends RequiredProps<AppViewProps, "bus"> {
  airportData: AirportData;
}

class GroundEquipmentAppView extends AppView<GroundEquipmentAppViewProps> {
  protected defaultView = "MainPage";

  protected registerViews(): void {
    this.appViewService.registerPage("MainPage", () => (
      <MainPage 
        bus={this.props.bus}
        appViewService={this.appViewService} 
        airportData={this.props.airportData} 
      />
    ));
  }

  public onOpen(): void {
    console.log("GroundEquipmentAppView onOpen");
    this.appViewService.open(this.defaultView);
  }

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

  public get name(): string {
    return "Ground Equipment Handler";
  }

  public get icon(): string {
    return `${BASE_URL}/Assets/app-icon.svg`;
  }

  public BootMode = AppBootMode.WARM;
  public SuspendMode = AppSuspendMode.SLEEP;

  public async install(_props: AppInstallProps): Promise<void> {
    console.log("Ground Equipment Handler: Starting installation...");
    console.log("BASE_URL:", BASE_URL);
    
    // Load the CSS for the EFB wrapper
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
      this.airportData = {
        airport: "Error Loading Data",
        version: "1.0",
        gates: []
      };
    }
    
    return Promise.resolve();
  }

  public get compatibleAircraftModels(): string[] | undefined {
    return undefined;
  }

  public render(): TVNode<GroundEquipmentAppView> {
    return <GroundEquipmentAppView bus={this.bus} airportData={this.airportData} />;
  }
}

/**
 * App definition to be injected into EFB
 */
Efb.use(GroundEquipmentApp);

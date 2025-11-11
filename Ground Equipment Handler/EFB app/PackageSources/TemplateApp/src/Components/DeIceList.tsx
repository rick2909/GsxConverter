import { TTButton, GamepadUiView, RequiredProps, TVNode, UiViewProps } from "@efb/efb-api";
import { FSComponent } from "@microsoft/msfs-sdk";
import { AirportData, DeIceArea } from "../types/GateData";
import "./DeIceList.scss";

interface DeIceListProps extends RequiredProps<UiViewProps, "appViewService"> {
  /** The airport data */
  airportData: AirportData;
}

export class DeIceList extends GamepadUiView<HTMLDivElement, DeIceListProps> {
  public readonly tabName = DeIceList.name;

  private filterText = "";

  private getFilteredDeIceAreas(): DeIceArea[] {
    const deices = this.props.airportData.deices || [];
    
    if (!this.filterText) {
      return deices;
    }
    
    const searchTerm = this.filterText.toLowerCase();
    return deices.filter(deice => 
      deice.id.toLowerCase().includes(searchTerm) ||
      deice.ui_name.toLowerCase().includes(searchTerm) ||
      deice.parking_system.toLowerCase().includes(searchTerm)
    );
  }

  public render(): TVNode<HTMLDivElement> {
    const filteredDeIces = this.getFilteredDeIceAreas();
    const hasDeIces = this.props.airportData.deices && this.props.airportData.deices.length > 0;
    
    return (
      <div ref={this.gamepadUiViewRef} class="deice-list">
        <div class="header">
          <TTButton
            key="Go back"
            type="secondary"
            callback={(): void => {
              this.props.appViewService.goBack();
            }}
          />
          <div class="header-content">
            <h1>De-Ice Areas</h1>
            <div class="header-info">
              <span class="deice-count">{filteredDeIces.length} areas</span>
              <span class="airport">{this.props.airportData.airport}</span>
            </div>
          </div>
        </div>

        {!hasDeIces && (
          <div class="no-data-message">
            <p>No de-ice areas available for this airport.</p>
          </div>
        )}

        {hasDeIces && (
          <>
            <div class="search-bar">
              <input
                type="text"
                placeholder="Search by ID, name, or parking system..."
                onInput={(e: any): void => {
                  this.filterText = (e.target as HTMLInputElement).value;
                  this.props.appViewService.open("DeIceList");
                }}
              />
            </div>

            <div class="deices-container">
              <div class="deices-grid">
                {filteredDeIces.map((deice) => (
                  <div
                    class="deice-card"
                    onClick={(): void => {
                      (window as any).selectedDeIce = deice;
                      this.props.appViewService.open("DeIceDetail");
                    }}
                  >
                    <div class="deice-header">
                      <h3>{deice.ui_name}</h3>
                      <span class="deice-badge">De-Ice</span>
                    </div>
                    
                    <div class="deice-info">
                      <div class="info-row">
                        <span class="label">ID:</span>
                        <span class="value">{deice.id}</span>
                      </div>
                      
                      <div class="info-row">
                        <span class="label">Parking System:</span>
                        <span class="value">{deice.parking_system}</span>
                      </div>
                      
                      <div class="info-row">
                        <span class="label">Radius:</span>
                        <span class="value">{deice.radius}m</span>
                      </div>
                      
                      <div class="info-row">
                        <span class="label">Position:</span>
                        <span class="value coordinates">
                          {deice.position.lat.toFixed(4)}, {deice.position.lon.toFixed(4)}
                        </span>
                      </div>
                    </div>

                    <div class="deice-features">
                      {deice.user_customized && <span class="feature">Customized</span>}
                      {deice.type && <span class="feature">{deice.type}</span>}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </>
        )}
      </div>
    );
  }
}

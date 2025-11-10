import { TTButton, GamepadUiView, RequiredProps, TVNode, UiViewProps } from "@efb/efb-api";
import { FSComponent } from "@microsoft/msfs-sdk";
import { DeIceArea } from "../types/GateData";
import "./DeIceDetail.scss";

interface DeIceDetailProps extends RequiredProps<UiViewProps, "appViewService"> {
}

export class DeIceDetail extends GamepadUiView<HTMLDivElement, DeIceDetailProps> {
  public readonly tabName = DeIceDetail.name;

  private getDeIce(): DeIceArea | null {
    return (window as any).selectedDeIce || null;
  }

  public render(): TVNode<HTMLDivElement> {
    const deice = this.getDeIce();
    
    if (!deice) {
      return (
        <div ref={this.gamepadUiViewRef} class="deice-detail">
          <div class="header">
            <TTButton
              key="Go back"
              type="secondary"
              callback={(): void => {
                this.props.appViewService.goBack();
              }}
            />
            <h2>No De-Ice Area Selected</h2>
          </div>
          <div class="content">
            <p>Please select a de-ice area from the list.</p>
          </div>
        </div>
      );
    }

    return (
      <div ref={this.gamepadUiViewRef} class="deice-detail">
        <div class="header">
          <TTButton
            key="Go back"
            type="secondary"
            callback={(): void => {
              this.props.appViewService.goBack();
            }}
          />
          <div class="header-content">
            <h2>{deice.ui_name}</h2>
            <span class="deice-badge">De-Ice Area</span>
          </div>
        </div>

        <div class="content">
          <div class="detail-section">
            <h3>General Information</h3>
            <div class="detail-grid">
              <div class="detail-item">
                <span class="label">Area ID:</span>
                <span class="value">{deice.id}</span>
              </div>
              <div class="detail-item">
                <span class="label">Display Name:</span>
                <span class="value">{deice.ui_name}</span>
              </div>
              <div class="detail-item">
                <span class="label">Parking System:</span>
                <span class="value">{deice.parking_system}</span>
              </div>
              <div class="detail-item">
                <span class="label">Radius:</span>
                <span class="value">{deice.radius} meters</span>
              </div>
              {deice.type && (
                <div class="detail-item">
                  <span class="label">Type:</span>
                  <span class="value">{deice.type}</span>
                </div>
              )}
              <div class="detail-item">
                <span class="label">User Customized:</span>
                <span class="value">{deice.user_customized ? 'Yes' : 'No'}</span>
              </div>
            </div>
          </div>

          <div class="detail-section">
            <h3>Main Position</h3>
            <div class="detail-grid">
              <div class="detail-item">
                <span class="label">Latitude:</span>
                <span class="value coordinates">{deice.position.lat.toFixed(8)}</span>
              </div>
              <div class="detail-item">
                <span class="label">Longitude:</span>
                <span class="value coordinates">{deice.position.lon.toFixed(8)}</span>
              </div>
              <div class="detail-item">
                <span class="label">Heading:</span>
                <span class="value">{deice.position.heading.toFixed(2)}°</span>
              </div>
            </div>
          </div>

          <div class="detail-section">
            <h3>Stop Position</h3>
            <div class="detail-grid">
              <div class="detail-item">
                <span class="label">Latitude:</span>
                <span class="value coordinates">{deice.parking_system_stop_position.lat.toFixed(8)}</span>
              </div>
              <div class="detail-item">
                <span class="label">Longitude:</span>
                <span class="value coordinates">{deice.parking_system_stop_position.lon.toFixed(8)}</span>
              </div>
              <div class="detail-item">
                <span class="label">Heading:</span>
                <span class="value">{deice.parking_system_stop_position.heading.toFixed(2)}°</span>
              </div>
            </div>
          </div>

          <div class="detail-section">
            <h3>Parking System Object Position</h3>
            <div class="detail-grid">
              <div class="detail-item">
                <span class="label">Latitude:</span>
                <span class="value coordinates">{deice.parking_system_object_position.lat.toFixed(8)}</span>
              </div>
              <div class="detail-item">
                <span class="label">Longitude:</span>
                <span class="value coordinates">{deice.parking_system_object_position.lon.toFixed(8)}</span>
              </div>
              <div class="detail-item">
                <span class="label">Heading:</span>
                <span class="value">{deice.parking_system_object_position.heading.toFixed(2)}°</span>
              </div>
              {deice.parking_system_object_position.height !== undefined && (
                <div class="detail-item">
                  <span class="label">Height:</span>
                  <span class="value">{deice.parking_system_object_position.height.toFixed(3)}m</span>
                </div>
              )}
            </div>
          </div>

          {deice.properties && Object.keys(deice.properties).length > 0 && (
            <div class="detail-section">
              <h3>Additional Properties</h3>
              <div class="properties-list">
                {Object.entries(deice.properties).map(([key, value]) => (
                  <div class="property-item">
                    <span class="property-key">{key}</span>
                    <span class="property-value">{String(value)}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          <div class="detail-section feature-highlight">
            <h3>Coverage Information</h3>
            <div class="coverage-info">
              <div class="coverage-item">
                <span class="coverage-icon">📏</span>
                <div class="coverage-content">
                  <span class="coverage-label">Coverage Radius</span>
                  <span class="coverage-value">{deice.radius} meters</span>
                  <span class="coverage-desc">Area available for de-icing operations</span>
                </div>
              </div>
              <div class="coverage-item">
                <span class="coverage-icon">🎯</span>
                <div class="coverage-content">
                  <span class="coverage-label">Parking System</span>
                  <span class="coverage-value">{deice.parking_system}</span>
                  <span class="coverage-desc">Visual guidance system type</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }
}

import { TTButton, GamepadUiView, RequiredProps, TVNode, UiViewProps } from '@efb/efb-api';
import { FSComponent } from '@microsoft/msfs-sdk';
import './MetadataView.scss';
import { AirportData } from '../types/GateData';

interface MetadataViewProps extends RequiredProps<UiViewProps, 'appViewService'> {
  airportData: AirportData;
}

/**
 * MetadataView component displays airport configuration metadata and jetway information
 */
export class MetadataView extends GamepadUiView<HTMLDivElement, MetadataViewProps> {
  public readonly tabName = MetadataView.name;

  /**
   * Renders the metadata view
   */
  public render(): TVNode {
    const { airportData } = this.props;
    const metadata = airportData.metadata || {};
    const jetwayHeights = airportData.jetway_rootfloor_heights || {};

    const jetwayCount = Object.keys(jetwayHeights).length;
    const avgHeight = jetwayCount > 0 
      ? (Object.values(jetwayHeights).reduce((sum: number, h: number) => sum + h, 0) / jetwayCount).toFixed(2)
      : '0.00';

    return (
      <div ref={this.gamepadUiViewRef} class="metadata-view">
        <div class="metadata-header">
          <TTButton 
            key="Back to Gates"
            type="secondary"
            callback={(): void => {
              this.props.appViewService.goBack();
            }}
          />
          <h1>Airport Configuration</h1>
          <div class="airport-info">
            <span class="airport-code">{airportData.airport}</span>
            <span class="version">Version {airportData.version}</span>
          </div>
        </div>

        <div class="metadata-content">
          {/* General Information */}
          <div class="metadata-section">
            <h2>
              <span class="icon">ℹ️</span>
              General Information
            </h2>
            <div class="info-grid">
              {metadata['general.creator'] && (
                <div class="info-item">
                  <span class="label">Creator</span>
                  <span class="value">{metadata['general.creator']}</span>
                </div>
              )}
              {metadata['general.scenario'] && (
                <div class="info-item">
                  <span class="label">Scenario</span>
                  <span class="value">{metadata['general.scenario']}</span>
                </div>
              )}
              {metadata['general.disable_static_docks'] && (
                <div class="info-item">
                  <span class="label">Static Docks Disabled</span>
                  <span class="value">
                    {metadata['general.disable_static_docks'] === '1' ? 'Yes' : 'No'}
                  </span>
                </div>
              )}
            </div>
            {metadata['general.notes'] && (
              <div class="notes">
                <span class="label">Notes</span>
                <p class="notes-content">{metadata['general.notes']}</p>
              </div>
            )}
          </div>

          {/* Statistics */}
          <div class="metadata-section stats-section">
            <h2>
              <span class="icon">📊</span>
              Statistics
            </h2>
            <div class="stats-grid">
              <div class="stat-card">
                <div class="stat-value">{airportData.gates.length}</div>
                <div class="stat-label">Gates</div>
              </div>
              {airportData.deices && airportData.deices.length > 0 && (
                <div class="stat-card deice">
                  <div class="stat-value">{airportData.deices.length}</div>
                  <div class="stat-label">De-Ice Areas</div>
                </div>
              )}
              {jetwayCount > 0 && (
                <div class="stat-card jetway">
                  <div class="stat-value">{jetwayCount}</div>
                  <div class="stat-label">Jetway Types</div>
                </div>
              )}
            </div>
          </div>

          {/* Jetway Heights */}
          {jetwayCount > 0 && (
            <div class="metadata-section jetway-section">
              <h2>
                <span class="icon">🚶</span>
                Jetway Root Floor Heights
              </h2>
              <div class="jetway-info">
                <div class="info-item highlight">
                  <span class="label">Average Height</span>
                  <span class="value">{avgHeight} meters</span>
                </div>
                <div class="info-item">
                  <span class="label">Total Jetway Types</span>
                  <span class="value">{jetwayCount}</span>
                </div>
              </div>
              <div class="jetway-list">
                {Object.entries(jetwayHeights)
                  .sort(([a], [b]) => a.localeCompare(b))
                  .map(([jetwayId, height]) => (
                    <div class="jetway-item" key={jetwayId}>
                      <span class="jetway-id">{jetwayId}</span>
                      <span class="jetway-height">{height.toFixed(2)}m</span>
                    </div>
                  ))}
              </div>
            </div>
          )}

          {/* Additional Metadata */}
          {Object.keys(metadata).some(key => !key.startsWith('general.')) && (
            <div class="metadata-section">
              <h2>
                <span class="icon">⚙️</span>
                Additional Configuration
              </h2>
              <div class="info-grid">
                {Object.entries(metadata)
                  .filter(([key]) => !key.startsWith('general.'))
                  .sort(([a], [b]) => a.localeCompare(b))
                  .map(([key, value]) => (
                    <div class="info-item" key={key}>
                      <span class="label">{key}</span>
                      <span class="value">{value || 'N/A'}</span>
                    </div>
                  ))}
              </div>
            </div>
          )}
        </div>
      </div>
    );
  }
}

import { TTButton, GamepadUiView, RequiredProps, TVNode, UiViewProps } from "@efb/efb-api";
import { FSComponent, Subject } from "@microsoft/msfs-sdk";
import { AirportData, Gate, GateGroup } from "../types/GateData";
import "./GateList.scss";

interface GateListProps extends RequiredProps<UiViewProps, "appViewService"> {
  /** The airport data */
  airportData: AirportData;
}

export class GateList extends GamepadUiView<HTMLDivElement, GateListProps> {
  public readonly tabName = GateList.name;

  private filterText = Subject.create("");
  private selectedGroupId = Subject.create<string | null>(null);

  /** @inheritdoc */
  public onAfterRender(node: TVNode): void {
    super.onAfterRender(node);
    
    // Subscribe to state changes and force re-render
    this.filterText.sub(() => {
      this.props.appViewService.open("GateList");
    });
    
    this.selectedGroupId.sub(() => {
      this.props.appViewService.open("GateList");
    });
  }

  private getFilteredGates(): Gate[] {
    let gates = this.props.airportData.gates;

    // Filter by selected group if groups are defined and one is selected
    const selectedGroupId = this.selectedGroupId.get();
    if (selectedGroupId && this.props.airportData.gate_groups) {
      const selectedGroup = this.props.airportData.gate_groups.find(g => g.id === selectedGroupId);
      if (selectedGroup) {
        gates = gates.filter(gate => selectedGroup.members.includes(gate.gate_id));
      }
    }

    // Apply search filter
    const filterText = this.filterText.get();
    if (filterText) {
      const searchTerm = filterText.toLowerCase();
      gates = gates.filter(gate => 
        gate.gate_id.toLowerCase().includes(searchTerm) ||
        gate.ui_name.toLowerCase().includes(searchTerm) ||
        gate.airline_codes.toLowerCase().includes(searchTerm)
      );
    }

    return gates;
  }

  private hasGroups(): boolean {
    const result = !!(this.props.airportData.gate_groups && this.props.airportData.gate_groups.length > 0);
    console.log("hasGroups check:", {
      gateGroups: this.props.airportData.gate_groups?.length,
      result: result
    });
    return result;
  }

  private getGatesByGroup(groupId: string): Gate[] {
    const group = this.props.airportData.gate_groups?.find(g => g.id === groupId);
    if (!group) {
      console.log("Group not found:", groupId);
      return [];
    }
    
    return this.props.airportData.gates.filter(gate => group.members.includes(gate.gate_id));
  }

  private getGateType(gate: Gate): 'jetway' | 'bus' | 'walk' {
    if (gate.has_jetway) return 'jetway';
    if (!gate.no_passenger_bus) return 'bus';
    return 'walk';
  }

  public render(): TVNode<HTMLDivElement> {
    try {
      console.log("=== GateList Render Start ===");
      console.log("airportData:", {
        airport: this.props.airportData.airport,
        gatesCount: this.props.airportData.gates?.length,
        groupsCount: this.props.airportData.gate_groups?.length,
        deicesCount: this.props.airportData.deices?.length
      });
      
      const filteredGates = this.getFilteredGates();
      // Only show loading if explicitly in loading state, not just because gates are empty
      const isLoading = this.props.airportData.airport === "Loading..." || this.props.airportData.airport === "Error Loading Data";
      const hasGroups = this.hasGroups();
      
      // Determine view mode: if groups exist and no specific group/search is selected, show grouped view
      const showGroupedView = hasGroups && !this.selectedGroupId.get() && !this.filterText.get();
      
      console.log("GateList render - isLoading:", isLoading, "hasGroups:", hasGroups, "showGroupedView:", showGroupedView, "filteredGates:", filteredGates.length);
    
      return (
        <div ref={this.gamepadUiViewRef} class="gate-list">
        <div class="header">
          <h1>{this.props.airportData.airport}</h1>
          <div class="header-info">
            <span class="gate-count">{filteredGates.length} gates</span>
            <span class="version">Version: {this.props.airportData.version}</span>
          </div>
          <div class="header-buttons">
            {this.props.airportData.deices && this.props.airportData.deices.length > 0 && (
              <TTButton
                key="De-Ice Areas"
                type="primary"
                callback={(): void => {
                  console.log("✅ De-Ice Areas button clicked");
                  (window as any).Coherent?.call("FOCUS_INPUT_FIELD", "");
                  alert("De-Ice button clicked!");
                  this.props.appViewService.open("DeIceList");
                }}
              />
            )}
            {this.props.airportData.metadata && (
              <TTButton
                key="Metadata"
                type="secondary"
                callback={(): void => {
                  console.log("✅ Metadata button clicked");
                  alert("Metadata button clicked!");
                  this.props.appViewService.open("MetadataView");
                }}
              />
            )}
          </div>
        </div>

        {isLoading && (
          <div class="loading-message">
            <p>Loading gate data...</p>
            <p style="font-size: 12px; color: #999; margin-top: 10px;">
              Airport: {this.props.airportData.airport}
            </p>
          </div>
        )}

        {!isLoading && this.props.airportData.gates.length === 0 && (
          <div class="loading-message">
            <p>No gate data available</p>
            <p style="font-size: 12px; color: #999; margin-top: 10px;">
              Airport: {this.props.airportData.airport} | Gates: {this.props.airportData.gates.length}
            </p>
          </div>
        )}

        {/* Back to Groups button when viewing specific group */}
        {!isLoading && hasGroups && this.selectedGroupId.get() && (
          <div class="back-to-groups">
            <TTButton
              key="back-to-groups"
              class="back-button"
              type="secondary"
              label="← Back to Groups"
              callback={(): void => {
                console.log("Back to Groups button clicked");
                this.selectedGroupId.set(null);
                this.filterText.set("");
              }}
            />
          </div>
        )}

        <div class="search-bar">
          <input
            type="text"
            placeholder="Search by gate ID, name, or airline..."
            onInput={(e: any): void => {
              const value = (e.target as HTMLInputElement).value;
              console.log("Search input changed:", value);
              this.filterText.set(value);
            }}
          />
        </div>

        {/* GROUPED VIEW - Show group cards */}
        {!isLoading && showGroupedView && this.props.airportData.gate_groups && (
          <div class="gates-container">
            <div class="groups-grid">
              {this.props.airportData.gate_groups.map((group) => {
                const groupGates = this.getGatesByGroup(group.id);
                const previewGates = groupGates.slice(0, 10);
                
                return (
                  <div key={group.id} class="group-card">
                    <div class="group-card-header">
                      <div class="group-title-section">
                        <h3>{group.id}</h3>
                        <span class="member-count">{group.members.length} gates</span>
                      </div>
                      <TTButton
                        key={`viewall-${group.id}`}
                        class="view-all-button"
                        type="primary"
                        label="View All →"
                        callback={(): void => {
                          console.log("View All button clicked for group:", group.id);
                          this.selectedGroupId.set(group.id);
                        }}
                      />
                    </div>
                    
                    <div class="group-gates-preview">
                      {previewGates.map((gate) => {
                        const gateType = this.getGateType(gate);
                        const buttonKey = `${gate.gate_id}-${gateType}-${gate.max_wingspan}`;
                        return (
                          <div key={gate.gate_id} class="mini-gate-card">
                            <div class="mini-gate-id">{gate.gate_id.toUpperCase()}</div>
                            <div class="mini-gate-badges">
                              <span class={`badge badge-${gateType}`}>
                                {gateType === 'jetway' ? 'Jetway' : gateType === 'bus' ? 'Bus' : 'Walk'}
                              </span>
                              <span class="badge badge-wingspan">{gate.max_wingspan}m</span>
                            </div>
                            <TTButton
                              key={buttonKey}
                              class="full-overlay-button"
                              label=""
                              callback={(): void => {
                                console.log("Mini gate card clicked:", gate.gate_id);
                                (window as any).selectedGate = gate;
                                console.log("Opening GateSelection view");
                                this.props.appViewService.open("GateSelection");
                              }}
                            />
                          </div>
                        );
                      })}
                      {groupGates.length > 10 && (
                        <div class="more-gates-indicator">
                          +{groupGates.length - 10} more
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        )}

        {/* GATE VIEW - Show individual gate cards (for specific group, search, or no groups) */}
        {!isLoading && !showGroupedView && (
          <div class="gates-container">
            <div class="gates-grid-compact">
              {filteredGates.map((gate) => {
                const gateType = this.getGateType(gate);
                const buttonKey = `${gate.gate_id}-${gateType}-${gate.max_wingspan}`;
                return (
                  <div key={gate.gate_id} class="compact-gate-card">
                    <div class="compact-gate-header">
                      <h4>{gate.gate_id.toUpperCase()}</h4>
                    </div>
                    
                    <div class="compact-gate-badges">
                      <span class={`badge badge-${gateType}`}>
                        {gateType === 'jetway' ? 'Jetway' : gateType === 'bus' ? 'Bus' : 'Walk'}
                      </span>
                      <span class="badge badge-wingspan">{gate.max_wingspan}m</span>
                    </div>
                    
                    {gate.airline_codes && (
                      <div class="compact-gate-airlines">{gate.airline_codes}</div>
                    )}
                    
                    <TTButton
                      key={buttonKey}
                      class="full-overlay-button"
                      label=""
                      type="secondary"
                      callback={(): void => {
                        console.log("Compact gate card clicked:", gate.gate_id);
                        (window as any).selectedGate = gate;
                        console.log("Opening GateSelection view");
                        this.props.appViewService.open("GateSelection");
                      }}
                    />
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </div>
      );
    } catch (error) {
      console.error("Error rendering GateList:", error);
      return (
        <div ref={this.gamepadUiViewRef} class="gate-list">
          <div class="header">
            <h1>Error</h1>
          </div>
          <div class="loading-message">
            <p>Failed to render gate list. Check console for details.</p>
          </div>
        </div>
      );
    }
  }
}

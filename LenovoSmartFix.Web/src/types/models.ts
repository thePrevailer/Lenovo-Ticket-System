export type DiagnosisPath = 'AutoResolve' | 'GuidedResolution' | 'Escalate';
export type RiskLevel = 'Low' | 'Medium' | 'High';
export type UpdateState = 'UpToDate' | 'UpdateAvailable' | 'Critical' | 'Unknown';
export type RemediationResult = 'Pending' | 'Success' | 'PartialSuccess' | 'Failed' | 'Skipped';
export type SafetyLevel = 'Safe' | 'Consent' | 'Guided';

export interface DeviceProfile {
  model: string;
  machineType: string;
  serialNumber: string;
  osVersion: string;
  osBuild: string;
  biosVersion: string;
  biosDate: string;
  installedLenovoUtilities: Record<string, string>;
  driverInventory: Record<string, string>;
}

export interface HealthSnapshot {
  batteryHealthPercent: number;
  batteryCycleCount: number;
  isOnAcPower: boolean;
  powerPlanName: string;
  isRecommendedPowerPlan: boolean;
  diskTotalBytes: number;
  diskFreeBytes: number;
  diskUsedPercent: number;
  ramTotalBytes: number;
  ramUsedPercent: number;
  pageFaultsPerSec: number;
  cpuLoadPercent: number;
  startupItemCount: number;
  highImpactBackgroundProcessCount: number;
  cpuTemperatureCelsius: number | null;
  thermalThrottlingDetected: boolean;
  wifiAdapterPresent: boolean;
  wifiAdapterName: string;
  wifiSignalStrengthPercent: number;
  wifiReconnectsLast24h: number;
  wifiDriverUpToDate: boolean;
  appCrashesLast7Days: number;
  systemCrashesLast7Days: number;
  recentCrashSignatures: string[];
}

export interface ComponentUpdateInfo {
  componentName: string;
  currentVersion: string;
  recommendedVersion: string;
  state: UpdateState;
  isCritical: boolean;
}

export interface UpdateStatus {
  bios: ComponentUpdateInfo;
  ecFirmware: ComponentUpdateInfo;
  drivers: ComponentUpdateInfo[];
  lenovoUtilities: ComponentUpdateInfo[];
  windowsUpdateState: UpdateState;
  pendingWindowsUpdates: number;
  hasCriticalUpdates: boolean;
}

export interface DiagnosisDecision {
  path: DiagnosisPath;
  confidence: number;
  riskLevel: RiskLevel;
  userFacingReason: string;
  technicalSummary: string;
  triggeredRuleIds: string[];
  evidenceItems: string[];
}

export interface RemediationAction {
  actionInstanceId: string;  // per-scan UUID — unique across all scans
  actionId: string;          // stable library code, e.g. "REM-TEMP-CLEANUP"
  actionName: string;
  description: string;
  safetyLevel: SafetyLevel;
  consentRequired: boolean;
  isRollbackable: boolean;
  result: RemediationResult;
  resultDetail: string;
  userConsented: boolean;
}

export interface EscalationPacket {
  packetId: string;
  primarySymptom: string;
  deviceProfile: DeviceProfile;
  healthSnapshot: HealthSnapshot;
  updateStatus: UpdateStatus;
  diagnosisDecision: DiagnosisDecision;
  actionsAttempted: RemediationAction[];
  outcome: string;
  unresolvedReason: string;
  isRedacted: boolean;
  exportedAt: string;
}

export interface ScanResult {
  scanId: string;
  symptom: string;
  deviceProfile: DeviceProfile;
  healthSnapshot: HealthSnapshot;
  updateStatus: UpdateStatus;
  decision: DiagnosisDecision;
  actions: RemediationAction[];
  escalationPacket?: EscalationPacket;
}

export type Page = 'overview' | 'scanning' | 'findings' | 'resolution' | 'escalation';

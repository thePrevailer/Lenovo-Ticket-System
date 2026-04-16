import type { ScanResult } from '../types/models';

function uuid() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
    const r = (Math.random() * 16) | 0;
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
  });
}

export const SYMPTOMS = [
  'Slow performance',
  'Battery draining fast',
  'Overheating',
  'Unstable Wi-Fi',
  'App or system crashes',
  'Storage almost full',
  'Device feels sluggish after update',
  'Other / general instability',
];

export const SCAN_STEPS = [
  { percent: 8,  label: 'Identifying device and hardware profile…' },
  { percent: 22, label: 'Reading battery and power state…' },
  { percent: 36, label: 'Checking disk, memory, and CPU metrics…' },
  { percent: 50, label: 'Scanning Wi-Fi stability and crash history…' },
  { percent: 64, label: 'Validating BIOS, firmware, and driver versions…' },
  { percent: 78, label: 'Running diagnostic rules engine…' },
  { percent: 88, label: 'Applying safe automatic fixes…' },
  { percent: 100, label: 'Scan complete.' },
];

/// Simulates the start/poll IPC pattern: resolves after the progress animation.
export async function simulateScanAsync(
  symptom: string,
  onProgress: (percent: number, step: string) => void
): Promise<ScanResult> {
  for (const step of SCAN_STEPS) {
    onProgress(step.percent, step.label);
    await new Promise(r => setTimeout(r, 900));
  }
  return buildMockScanResult(symptom);
}

// Returns a mock scan result that exercises several rule families
export function buildMockScanResult(symptom: string): ScanResult {
  const isPerf = symptom.toLowerCase().includes('slow') || symptom.toLowerCase().includes('sluggish');
  const isBattery = symptom.toLowerCase().includes('battery');
  const isWifi = symptom.toLowerCase().includes('wi-fi');
  const isCrash = symptom.toLowerCase().includes('crash');

  return {
    scanId: 'mock-' + Math.random().toString(36).slice(2, 10),
    symptom,

    deviceProfile: {
      model: 'ThinkPad X1 Carbon Gen 11',
      machineType: 'ThinkPad X1',
      serialNumber: 'PF4R2X••',
      osVersion: 'Windows 11 Pro',
      osBuild: '22631',
      biosVersion: 'N3HET72W (1.52)',
      biosDate: '2024-01-15',
      installedLenovoUtilities: {
        'Lenovo Vantage': '10.2310.11.0',
        'Lenovo System Update': '5.08.01.26',
        'Lenovo Smart Appearance': '3.1.0',
      },
      driverInventory: {
        Display: '31.0.101.5379',
        Net: '23.60.0.0',
        AudioEndpoint: '10.0.22621.3007',
        USB: '3.0.0.0',
      },
    },

    healthSnapshot: {
      batteryHealthPercent: isBattery ? 51 : 74,
      batteryCycleCount: isBattery ? 312 : 187,
      isOnAcPower: !isBattery,
      powerPlanName: isBattery ? 'High performance' : 'Balanced',
      isRecommendedPowerPlan: !isBattery,
      diskTotalBytes: 512_000_000_000,
      diskFreeBytes: isPerf ? 18_000_000_000 : 142_000_000_000,
      diskUsedPercent: isPerf ? 96.5 : 72.3,
      ramTotalBytes: 16_000_000_000,
      ramUsedPercent: isPerf ? 91 : 58,
      pageFaultsPerSec: isPerf ? 340 : 12,
      cpuLoadPercent: isPerf ? 84 : 22,
      startupItemCount: isPerf ? 22 : 8,
      highImpactBackgroundProcessCount: isPerf ? 6 : 1,
      cpuTemperatureCelsius: isPerf ? 91 : 52,
      thermalThrottlingDetected: isPerf,
      wifiAdapterPresent: true,
      wifiAdapterName: 'Intel Wi-Fi 6E AX211',
      wifiSignalStrengthPercent: isWifi ? 42 : 88,
      wifiReconnectsLast24h: isWifi ? 14 : 0,
      wifiDriverUpToDate: !isWifi,
      appCrashesLast7Days: isCrash ? 9 : 1,
      systemCrashesLast7Days: isCrash ? 2 : 0,
      recentCrashSignatures: isCrash
        ? [
            'Faulting application: chrome.exe (v120.0.6099.130)',
            'Faulting application: Outlook.exe (v16.0.17127.20000)',
            'Kernel-Power event 41 — unexpected restart',
          ]
        : [],
    },

    updateStatus: {
      // BIOS and EC catalog: Unknown in V1 (no live catalog wired) — rules must not fire
      bios: {
        componentName: 'BIOS',
        currentVersion: 'N3HET72W (1.52)',
        recommendedVersion: '',
        state: 'Unknown',
        isCritical: true,
      },
      ecFirmware: {
        componentName: 'EC Firmware',
        currentVersion: '1.20',
        recommendedVersion: '',
        state: 'Unknown',
        isCritical: false,
      },
      // Windows Update via WUA: available if not a crash-only symptom
      drivers: [
        {
          componentName: 'Display Driver (Intel Iris Xe)',
          currentVersion: '31.0.101.5379',
          recommendedVersion: '31.0.101.5762',
          state: isPerf ? 'UpdateAvailable' : 'UpToDate',
          isCritical: true,
        },
        {
          componentName: 'Net Driver (Intel Wi-Fi 6E AX211)',
          currentVersion: '23.60.0.0',
          recommendedVersion: isWifi ? '23.80.0.0' : '23.60.0.0',
          state: isWifi ? 'UpdateAvailable' : 'UpToDate',
          isCritical: true,
        },
        {
          componentName: 'Audio Driver',
          currentVersion: '10.0.22621.3007',
          recommendedVersion: '10.0.22621.3007',
          state: 'UpToDate',
          isCritical: false,
        },
      ],
      lenovoUtilities: [
        {
          componentName: 'Lenovo Vantage',
          currentVersion: '10.2310.11.0',
          recommendedVersion: '10.2402.15.0',
          state: 'UpdateAvailable',
          isCritical: false,
        },
      ],
      windowsUpdateState: isCrash ? 'Unknown' : 'UpdateAvailable',
      pendingWindowsUpdates: isCrash ? 0 : 3,
      hasCriticalUpdates: isPerf || isWifi,
    },

    decision: {
      path: (isPerf && isCrash) || (isCrash && symptom.toLowerCase().includes('crash'))
        ? 'Escalate'
        : isPerf || isBattery || isWifi
        ? 'GuidedResolution'
        : 'AutoResolve',
      confidence: 0.87,
      riskLevel: isCrash ? 'High' : isPerf ? 'Medium' : 'Low',
      userFacingReason: buildUserFacingReason(symptom, isPerf, isBattery, isWifi, isCrash),
      technicalSummary: buildTechnicalSummary(isPerf, isBattery, isWifi, isCrash),
      triggeredRuleIds: buildTriggeredRules(isPerf, isBattery, isWifi, isCrash),
      evidenceItems: buildEvidence(isPerf, isBattery, isWifi, isCrash),
    },

    actions: buildActions(isPerf, isBattery, isWifi, isCrash),
  };
}

function buildUserFacingReason(
  symptom: string,
  isPerf: boolean, isBattery: boolean, isWifi: boolean, isCrash: boolean
): string {
  if (isCrash) return 'Your device has had repeated crashes recently. SmartFix has flagged this for support review.';
  if (isPerf) return 'Your device is running hot and the startup load is high. SmartFix cleaned temporary files and will guide you through the remaining steps.';
  if (isBattery) return 'Your battery health has declined to 51%. SmartFix switched your power plan to Balanced and recommends reviewing charging habits.';
  if (isWifi) return 'Your Wi-Fi connection dropped 14 times in the last 24 hours. A driver update and network service restart may resolve this.';
  return 'SmartFix found a BIOS update and display driver update available. Keeping your software stack current will improve stability.';
}

function buildTechnicalSummary(isPerf: boolean, isBattery: boolean, isWifi: boolean, isCrash: boolean): string {
  const rules: string[] = [];
  if (isPerf) rules.push('PERF-001', 'PERF-003', 'STAB-003');
  if (isBattery) rules.push('BATT-001', 'BATT-003');
  if (isWifi) rules.push('NET-001', 'NET-002');
  if (isCrash) rules.push('STAB-001', 'STAB-002');
  rules.push('UPD-001', 'UPD-002');
  return `${rules.length} rule(s) triggered: ${rules.join(', ')}`;
}

function buildTriggeredRules(isPerf: boolean, isBattery: boolean, isWifi: boolean, isCrash: boolean): string[] {
  const rules: string[] = [];
  if (isPerf) rules.push('PERF-001', 'PERF-003', 'STAB-003');
  if (isBattery) rules.push('BATT-001', 'BATT-003');
  if (isWifi) rules.push('NET-001', 'NET-002');
  if (isCrash) rules.push('STAB-001', 'STAB-002');
  rules.push('UPD-001', 'UPD-002');
  return rules;
}

function buildEvidence(isPerf: boolean, isBattery: boolean, isWifi: boolean, isCrash: boolean): string[] {
  const ev: string[] = [];
  if (isPerf) {
    ev.push('CPU at 84% — 22 startup items detected');
    ev.push('Disk 96.5% full — only 18 GB free');
    ev.push('CPU temperature 91°C — thermal throttling active');
    ev.push('RAM at 91% — 340 page faults/sec');
  }
  if (isBattery) {
    ev.push('Battery health 51% (312 charge cycles)');
    ev.push("Power plan 'High performance' active — not recommended");
  }
  if (isWifi) {
    ev.push('Wi-Fi reconnected 14 times in the last 24 hours');
    ev.push('Network driver update available: 23.60.0.0 → 23.80.0.0');
  }
  if (isCrash) {
    ev.push('9 app crashes detected in the last 7 days');
    ev.push('2 unexpected system restarts (Kernel-Power event 41)');
  }
  ev.push('BIOS update available: 1.52 → 1.54');
  ev.push('Display driver update available: 31.0.101.5379 → 31.0.101.5762');
  return ev;
}

function buildActions(isPerf: boolean, isBattery: boolean, isWifi: boolean, isCrash: boolean) {
  const actions = [];

  if (isPerf) {
    actions.push({
      actionInstanceId: uuid(),
      actionId: 'REM-TEMP-CLEANUP',
      actionName: 'Clear Temporary Files',
      description: 'Remove temporary files from %TEMP% to free disk space.',
      safetyLevel: 'Safe' as const,
      consentRequired: false,
      isRollbackable: false,
      result: 'Success' as const,
      resultDetail: 'Freed 3.2 GB of temporary files.',
      userConsented: false,
    });
    actions.push({
      actionInstanceId: uuid(),
      actionId: 'REM-STARTUP-OPT',
      actionName: 'Review Startup Programs',
      description: 'Open the startup manager so you can disable high-impact items.',
      safetyLevel: 'Consent' as const,
      consentRequired: true,
      isRollbackable: true,
      result: 'Pending' as const,
      resultDetail: '',
      userConsented: false,
    });
  }

  if (isBattery) {
    actions.push({
      actionInstanceId: uuid(),
      actionId: 'REM-POWER-PLAN',
      actionName: 'Switch to Balanced Power Plan',
      description: 'Set the active power plan to Balanced for optimal battery life.',
      safetyLevel: 'Safe' as const,
      consentRequired: false,
      isRollbackable: true,
      result: 'Success' as const,
      resultDetail: 'Power plan set to Balanced.',
      userConsented: false,
    });
  }

  if (isWifi) {
    actions.push({
      actionInstanceId: uuid(),
      actionId: 'REM-DNS-FLUSH',
      actionName: 'Flush DNS Cache',
      description: 'Clear the DNS resolver cache to resolve stale address mappings.',
      safetyLevel: 'Safe' as const,
      consentRequired: false,
      isRollbackable: false,
      result: 'Success' as const,
      resultDetail: 'DNS cache flushed successfully.',
      userConsented: false,
    });
    actions.push({
      actionInstanceId: uuid(),
      actionId: 'REM-NET-SVC',
      actionName: 'Restart Network Services',
      description: 'Restart DNS Client, NLA, and WLAN AutoConfig services.',
      safetyLevel: 'Consent' as const,
      consentRequired: true,
      isRollbackable: false,
      result: 'Pending' as const,
      resultDetail: '',
      userConsented: false,
    });
  }

  actions.push({
    actionInstanceId: uuid(),
    actionId: 'REM-UPDATE-VANTAGE',
    actionName: 'Apply Available Updates via Lenovo Vantage',
    description: 'Open the Lenovo Vantage update flow to apply BIOS and driver updates.',
    safetyLevel: 'Consent' as const,
    consentRequired: true,
    isRollbackable: false,
    result: 'Pending' as const,
    resultDetail: '',
    userConsented: false,
  });

  return actions;
}

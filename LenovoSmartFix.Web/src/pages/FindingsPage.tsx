import { useState } from 'react';
import type { ScanResult } from '../types/models';
import { IconWarning, IconTool, IconCircleCheck } from '../components/Icons';

interface Props {
  result: ScanResult;
  onResolution: () => void;
  onEscalation: () => void;
}

function barColor(val: number, warn: number, crit: number) {
  if (val >= crit) return 'var(--red)';
  if (val >= warn) return 'var(--warning)';
  return 'var(--success)';
}

function batteryBarColor(val: number) {
  if (val <= 40) return 'var(--red)';
  if (val <= 60) return 'var(--warning)';
  return 'var(--success)';
}

function updateDotColor(state: string) {
  if (state === 'Critical')       return 'var(--red)';
  if (state === 'UpdateAvailable') return 'var(--warning)';
  if (state === 'Unknown')         return '#D0D0D0';
  return 'var(--success)';
}

export default function FindingsPage({ result, onResolution, onEscalation }: Props) {
  const { deviceProfile: d, healthSnapshot: h, updateStatus: u, decision: dec } = result;
  const [crashExpanded, setCrashExpanded] = useState(false);

  const verdictClass = dec.path === 'Escalate' ? '' : dec.path === 'GuidedResolution' ? 'guided' : 'resolve';
  const VerdictIcon  = dec.path === 'Escalate'
    ? () => <IconWarning size={22} />
    : dec.path === 'GuidedResolution'
    ? () => <IconTool size={22} />
    : () => <IconCircleCheck size={22} />;
  const verdictLabel = dec.path === 'Escalate'
    ? 'Escalate to Support'
    : dec.path === 'GuidedResolution'
    ? 'Guided Resolution'
    : 'Auto-Resolved';

  const pathBadgeClass = dec.path === 'Escalate' ? 'badge-red'
    : dec.path === 'GuidedResolution' ? 'badge-amber'
    : 'badge-green';

  return (
    <div className="content">
      <div className="page-header">
        <h1 className="page-h1">Findings</h1>
        <p className="page-sub">Here is what SmartFix found on your device.</p>
      </div>

      {/* Verdict */}
      <div className={`verdict ${verdictClass}`}>
        <span className="verdict-icon"><VerdictIcon /></span>
        <div className="verdict-body">
          <div className="verdict-path">
            <span className={`badge ${pathBadgeClass}`}>{verdictLabel}</span>
            <span style={{ fontSize: 11, color: 'var(--muted)' }}>
              Confidence {Math.round(dec.confidence * 100)}%  ·  Risk: {dec.riskLevel}
            </span>
          </div>
          <p className="verdict-reason">{dec.userFacingReason}</p>
        </div>
      </div>

      {/* Device */}
      <div className="card">
        <div className="card-title"><span className="card-title-icon" />Device</div>
        <div className="data-row">
          <span className="data-label">Model</span>
          <span className="data-value">{d.model}</span>
        </div>
        <div className="data-row">
          <span className="data-label">OS</span>
          <span className="data-value">{d.osVersion} <span style={{ color: 'var(--muted)' }}>(Build {d.osBuild})</span></span>
        </div>
        <div className="data-row">
          <span className="data-label">BIOS</span>
          <span className="data-value">{d.biosVersion}</span>
        </div>
        <div className="data-row">
          <span className="data-label">Lenovo Apps</span>
          <span className="data-value" style={{ lineHeight: 1.7 }}>
            {Object.entries(d.installedLenovoUtilities).map(([k, v]) => (
              <span key={k} style={{ display: 'inline-block', marginRight: 12 }}>
                {k} <span style={{ color: 'var(--muted)', fontSize: 11 }}>{v}</span>
              </span>
            ))}
          </span>
        </div>
      </div>

      {/* Health Snapshot */}
      <div className="card">
        <div className="card-title"><span className="card-title-icon" />Health Snapshot</div>

        {/* Battery */}
        <div className="data-row">
          <span className="data-label">Battery</span>
          <div className="data-value">
            <span style={{ color: batteryBarColor(h.batteryHealthPercent), fontWeight: 600 }}>
              {h.batteryHealthPercent}% health
            </span>
            <span style={{ color: 'var(--muted)', marginLeft: 8 }}>
              {h.batteryCycleCount} cycles · {h.isOnAcPower ? 'AC power' : 'On battery'}
            </span>
            {!h.isRecommendedPowerPlan && (
              <span style={{ color: 'var(--warning)', marginLeft: 8, display: 'inline-flex', alignItems: 'center', gap: 3 }}>
                <IconWarning size={12} /> {h.powerPlanName}
              </span>
            )}
            <div className="mini-bar-wrap">
              <div className="mini-bar-track">
                <div className="mini-bar-fill" style={{ width: `${h.batteryHealthPercent}%`, background: batteryBarColor(h.batteryHealthPercent) }} />
              </div>
              <span style={{ fontSize: 10.5, color: 'var(--muted)' }}>{h.batteryHealthPercent}%</span>
            </div>
          </div>
        </div>

        {/* Disk */}
        <div className="data-row">
          <span className="data-label">Disk</span>
          <div className="data-value">
            <span style={{ color: barColor(h.diskUsedPercent, 85, 95), fontWeight: 600 }}>
              {h.diskUsedPercent.toFixed(1)}% used
            </span>
            <span style={{ color: 'var(--muted)', marginLeft: 8 }}>
              {(h.diskFreeBytes / 1e9).toFixed(0)} GB free of {(h.diskTotalBytes / 1e9).toFixed(0)} GB
            </span>
            <div className="mini-bar-wrap">
              <div className="mini-bar-track">
                <div className="mini-bar-fill" style={{ width: `${h.diskUsedPercent}%`, background: barColor(h.diskUsedPercent, 85, 95) }} />
              </div>
              <span style={{ fontSize: 10.5, color: 'var(--muted)' }}>{h.diskUsedPercent.toFixed(0)}%</span>
            </div>
          </div>
        </div>

        {/* RAM */}
        <div className="data-row">
          <span className="data-label">RAM</span>
          <div className="data-value">
            <span style={{ color: barColor(h.ramUsedPercent, 85, 95), fontWeight: 600 }}>
              {h.ramUsedPercent.toFixed(0)}% used
            </span>
            <span style={{ color: 'var(--muted)', marginLeft: 8 }}>
              of {(h.ramTotalBytes / 1e9).toFixed(0)} GB · {h.pageFaultsPerSec} pg faults/sec
            </span>
            <div className="mini-bar-wrap">
              <div className="mini-bar-track">
                <div className="mini-bar-fill" style={{ width: `${h.ramUsedPercent}%`, background: barColor(h.ramUsedPercent, 85, 95) }} />
              </div>
              <span style={{ fontSize: 10.5, color: 'var(--muted)' }}>{h.ramUsedPercent.toFixed(0)}%</span>
            </div>
          </div>
        </div>

        {/* CPU */}
        <div className="data-row">
          <span className="data-label">CPU</span>
          <div className="data-value">
            <span style={{ color: barColor(h.cpuLoadPercent, 70, 90), fontWeight: 600 }}>
              {h.cpuLoadPercent}% load
            </span>
            <span style={{ color: 'var(--muted)', marginLeft: 8 }}>{h.startupItemCount} startup items</span>
            {h.thermalThrottlingDetected && (
              <span style={{ color: 'var(--red)', marginLeft: 8, display: 'inline-flex', alignItems: 'center', gap: 3 }}>
                <IconWarning size={12} /> Throttling at {h.cpuTemperatureCelsius}°C
              </span>
            )}
            <div className="mini-bar-wrap">
              <div className="mini-bar-track">
                <div className="mini-bar-fill" style={{ width: `${h.cpuLoadPercent}%`, background: barColor(h.cpuLoadPercent, 70, 90) }} />
              </div>
              <span style={{ fontSize: 10.5, color: 'var(--muted)' }}>{h.cpuLoadPercent}%</span>
            </div>
          </div>
        </div>

        {/* Wi-Fi */}
        <div className="data-row">
          <span className="data-label">Wi-Fi</span>
          <div className="data-value">
            {h.wifiAdapterPresent ? (
              <>
                <span style={{ color: h.wifiReconnectsLast24h >= 5 ? 'var(--red)' : 'var(--text)', fontWeight: h.wifiReconnectsLast24h >= 5 ? 600 : 400 }}>
                  {h.wifiReconnectsLast24h} drops (24 h)
                </span>
                <span style={{ color: 'var(--muted)', marginLeft: 8 }}>
                  {h.wifiAdapterName} · {h.wifiSignalStrengthPercent}% signal
                </span>
              </>
            ) : 'No wireless adapter'}
          </div>
        </div>

        {/* Crashes */}
        <div className="data-row">
          <span className="data-label">Crashes</span>
          <div className="data-value">
            <span style={{ color: (h.appCrashesLast7Days >= 3 || h.systemCrashesLast7Days >= 1) ? 'var(--red)' : 'var(--text)', fontWeight: 600 }}>
              {h.appCrashesLast7Days} app · {h.systemCrashesLast7Days} system
            </span>
            <span style={{ color: 'var(--muted)', marginLeft: 6, fontSize: 11 }}>(last 7 days)</span>

            {h.recentCrashSignatures.length > 0 && (
              <>
                <button
                  onClick={() => setCrashExpanded(v => !v)}
                  style={{
                    display: 'block', marginTop: 5, background: 'none', border: 'none',
                    padding: 0, fontSize: 11, color: 'var(--info)', cursor: 'pointer', fontFamily: 'var(--font)',
                  }}
                >
                  {crashExpanded ? '− Hide' : `+ Show ${h.recentCrashSignatures.length} crash signatures`}
                </button>
                {crashExpanded && (
                  <div style={{ marginTop: 6 }}>
                    {h.recentCrashSignatures.map((s, i) => (
                      <div key={i} style={{
                        fontSize: 11, color: 'var(--muted)', padding: '3px 0 3px 10px',
                        borderLeft: '2px solid var(--border)', marginBottom: 3,
                      }}>
                        {s}
                      </div>
                    ))}
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      </div>

      {/* Software Stack */}
      <div className="card">
        <div className="card-title">
          <span className="card-title-icon" />
          Software Stack
          {u.hasCriticalUpdates && <span className="badge badge-amber" style={{ marginLeft: 4 }}>Updates available</span>}
        </div>

        {/* BIOS */}
        <div className="update-row">
          <span className="update-dot" style={{ background: updateDotColor(u.bios.state) }} />
          <span className="update-name">BIOS</span>
          <span className="update-ver">{u.bios.currentVersion}</span>
          <span className={`badge ${u.bios.state === 'UpdateAvailable' || u.bios.state === 'Critical' ? 'badge-amber' : u.bios.state === 'Unknown' ? 'badge-gray' : 'badge-green'}`}>
            {u.bios.state === 'UpdateAvailable' ? `→ ${u.bios.recommendedVersion}`
              : u.bios.state === 'Critical'       ? `Critical → ${u.bios.recommendedVersion}`
              : u.bios.state === 'Unknown'         ? 'Status unknown'
              : 'Up to date'}
          </span>
        </div>

        {/* Drivers */}
        {u.drivers.map(dr => (
          <div key={dr.componentName} className="update-row">
            <span className="update-dot" style={{ background: updateDotColor(dr.state) }} />
            <span className="update-name">{dr.componentName}</span>
            <span className="update-ver">{dr.currentVersion}</span>
            <span className={`badge ${dr.state === 'UpdateAvailable' || dr.state === 'Critical' ? 'badge-amber' : dr.state === 'Unknown' ? 'badge-gray' : 'badge-green'}`}>
              {dr.state === 'UpdateAvailable' ? `→ ${dr.recommendedVersion}`
                : dr.state === 'Critical'     ? `Critical → ${dr.recommendedVersion}`
                : dr.state === 'Unknown'       ? 'Status unknown'
                : 'Up to date'}
            </span>
          </div>
        ))}

        {/* Windows Update */}
        <div className="update-row">
          <span className="update-dot" style={{ background: updateDotColor(u.windowsUpdateState) }} />
          <span className="update-name">Windows Update</span>
          <span className="update-ver">
            {u.windowsUpdateState === 'Unknown' ? '—' : `${u.pendingWindowsUpdates} pending`}
          </span>
          <span className={`badge ${u.windowsUpdateState === 'Unknown' ? 'badge-gray' : u.pendingWindowsUpdates > 0 ? 'badge-amber' : 'badge-green'}`}>
            {u.windowsUpdateState === 'Unknown' ? 'Check manually'
              : u.pendingWindowsUpdates > 0     ? 'Updates waiting'
              : 'Up to date'}
          </span>
        </div>
      </div>

      {/* Evidence */}
      {dec.evidenceItems.length > 0 && (
        <div className="card">
          <div className="card-title"><span className="card-title-icon" />Evidence collected</div>
          <ul className="evidence-list">
            {dec.evidenceItems.map((e, i) => (
              <li key={i} className="evidence-item">
                <span className="evidence-bullet">·</span>
                {e}
              </li>
            ))}
          </ul>
          <div style={{ marginTop: 12, paddingTop: 10, borderTop: '1px solid var(--border-lt)' }}>
            <span style={{ fontSize: 11, color: 'var(--muted)' }}>
              Rules triggered: {dec.triggeredRuleIds.map((id, i) => (
                <span key={id}>
                  {i > 0 && <span style={{ margin: '0 4px', opacity: .4 }}>·</span>}
                  <span style={{ color: 'var(--text)', fontWeight: 500 }}>{id}</span>
                </span>
              ))}
            </span>
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="footer-actions">
        <button className="btn btn-primary" onClick={onResolution}>
          View Resolution Options →
        </button>
        <button className="btn btn-secondary" onClick={onEscalation}>
          Prepare Support Packet
        </button>
      </div>
    </div>
  );
}

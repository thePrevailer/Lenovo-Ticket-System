import { useState } from 'react';
import type { ScanResult } from '../types/models';
import { IconWarning, IconTool, IconCheck, IconDownload, IconShield } from '../components/Icons';

interface Props {
  result: ScanResult;
}

// ── Data builder ─────────────────────────────────────────────────────────────

function buildPacketData(result: ScanResult, redact: boolean) {
  const d = result.deviceProfile;
  return {
    packetId:      result.scanId,
    schemaVersion: '1.0',
    primarySymptom: result.symptom,
    exportedAt:    new Date().toISOString(),
    isRedacted:    redact,
    deviceProfile: {
      model:        d.model,
      machineType:  d.machineType,
      serialNumber: redact ? '[redacted]' : d.serialNumber,
      osVersion:    d.osVersion,
      osBuild:      d.osBuild,
      biosVersion:  d.biosVersion,
      installedLenovoUtilities: d.installedLenovoUtilities,
    },
    healthSnapshot: {
      batteryHealthPercent:     result.healthSnapshot.batteryHealthPercent,
      batteryCycleCount:        result.healthSnapshot.batteryCycleCount,
      diskUsedPercent:          result.healthSnapshot.diskUsedPercent,
      ramUsedPercent:           result.healthSnapshot.ramUsedPercent,
      cpuLoadPercent:           result.healthSnapshot.cpuLoadPercent,
      startupItemCount:         result.healthSnapshot.startupItemCount,
      thermalThrottlingDetected:result.healthSnapshot.thermalThrottlingDetected,
      cpuTemperatureCelsius:    result.healthSnapshot.cpuTemperatureCelsius,
      wifiReconnectsLast24h:    result.healthSnapshot.wifiReconnectsLast24h,
      appCrashesLast7Days:      result.healthSnapshot.appCrashesLast7Days,
      systemCrashesLast7Days:   result.healthSnapshot.systemCrashesLast7Days,
      recentCrashSignatures:    result.healthSnapshot.recentCrashSignatures,
    },
    updateStatus: {
      bios:                result.updateStatus.bios,
      hasCriticalUpdates:  result.updateStatus.hasCriticalUpdates,
      pendingWindowsUpdates: result.updateStatus.pendingWindowsUpdates,
      outdatedDrivers:     result.updateStatus.drivers.filter(d => d.state === 'UpdateAvailable'),
    },
    diagnosisDecision: result.decision,
    actionsAttempted:  result.actions.map(a => ({
      actionId:     a.actionId,
      actionName:   a.actionName,
      result:       a.result,
      resultDetail: a.resultDetail,
      userConsented: a.userConsented,
    })),
    outcome: result.actions.some(a => a.result === 'Success')
      ? 'Partially resolved'
      : 'Unresolved — escalation recommended',
    unresolvedReason: result.decision.userFacingReason,
  };
}

// ── PDF builder ───────────────────────────────────────────────────────────────

async function exportPdf(result: ScanResult, redact: boolean) {
  const { jsPDF } = await import('jspdf');
  const doc  = new jsPDF({ unit: 'pt', format: 'a4' });
  const W    = doc.internal.pageSize.getWidth();
  const MARGIN = 48;
  const COL    = W - MARGIN * 2;
  let y        = MARGIN;

  const RED   = [225, 20, 10]  as [number, number, number];
  const BLACK = [26,  26, 26]  as [number, number, number];
  const GRAY  = [107, 107, 107] as [number, number, number];
  const LGRAY = [220, 220, 220] as [number, number, number];

  function rule(color = LGRAY) {
    doc.setDrawColor(...color);
    doc.setLineWidth(0.5);
    doc.line(MARGIN, y, MARGIN + COL, y);
    y += 14;
  }

  function heading(text: string, small = false) {
    doc.setFontSize(small ? 7.5 : 8);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(...RED);
    doc.text(text.toUpperCase(), MARGIN, y);
    y += small ? 12 : 14;
    doc.setTextColor(...BLACK);
  }

  function row(label: string, value: string, muted = false) {
    doc.setFontSize(9);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(...GRAY);
    doc.text(label, MARGIN, y);
    doc.setFont('helvetica', 'normal');
    if (muted) doc.setTextColor(...GRAY); else doc.setTextColor(...BLACK);
    const wrapped = doc.splitTextToSize(value, COL - 110);
    doc.text(wrapped, MARGIN + 110, y);
    y += wrapped.length * 13;
  }

  function bullet(text: string, done?: boolean) {
    doc.setFontSize(9);
    doc.setFont('helvetica', done === true ? 'bold' : 'normal');
    if (done === true)       doc.setTextColor(30, 120, 50);   // success green
    else if (done === false) doc.setTextColor(...GRAY);
    else                     doc.setTextColor(...BLACK);
    const prefix = done === true ? '[Done]  ' : done === false ? '[--]    ' : '         ';
    const wrapped = doc.splitTextToSize(prefix + text, COL);
    doc.text(wrapped, MARGIN, y);
    y += wrapped.length * 13;
  }

  function gap(n = 10) { y += n; }

  function newPageIfNeeded(space = 80) {
    if (y + space > doc.internal.pageSize.getHeight() - MARGIN) {
      doc.addPage();
      y = MARGIN;
    }
  }

  const d = result.deviceProfile;
  const h = result.healthSnapshot;
  const packet = buildPacketData(result, redact);

  // ── Cover header ──────────────────────────────────────────────────────────
  doc.setFillColor(...RED);
  doc.rect(0, 0, W, 56, 'F');

  doc.setFontSize(18);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(255, 255, 255);
  doc.text('LENOVO SMARTFIX', MARGIN, 30);

  doc.setFontSize(9);
  doc.setFont('helvetica', 'normal');
  doc.setTextColor(255, 200, 200);
  doc.text('Diagnostic Support Packet', MARGIN, 46);

  y = 78;
  doc.setTextColor(...BLACK);

  // ── Packet metadata ───────────────────────────────────────────────────────
  heading('Packet Information');
  row('Packet ID',   packet.packetId);
  row('Generated',   new Date().toLocaleString());
  row('Redacted',    redact ? 'Yes — serial numbers removed' : 'No — full device identity included');
  gap();
  rule();

  // ── Device ────────────────────────────────────────────────────────────────
  heading('Device');
  row('Model',        d.model);
  row('OS',           `${d.osVersion}  (Build ${d.osBuild})`);
  row('BIOS',         d.biosVersion);
  if (!redact) row('Serial',      d.serialNumber);
  const utils = Object.entries(d.installedLenovoUtilities).map(([k,v]) => `${k} ${v}`).join(', ');
  row('Lenovo Apps',  utils);
  gap();
  rule();

  // ── Reported issue + diagnosis ────────────────────────────────────────────
  newPageIfNeeded(100);
  heading('Reported Issue & Diagnosis');
  row('Symptom',      result.symptom);
  row('Path',         result.decision.path === 'Escalate'
    ? 'Escalate to Support'
    : result.decision.path === 'GuidedResolution'
    ? 'Guided Resolution'
    : 'Auto-Resolved');
  row('Confidence',   `${Math.round(result.decision.confidence * 100)}%  ·  Risk: ${result.decision.riskLevel}`);
  row('Summary',      result.decision.userFacingReason);
  gap();
  rule();

  // ── Health metrics ────────────────────────────────────────────────────────
  newPageIfNeeded(120);
  heading('Health Snapshot');
  row('Battery',      `${h.batteryHealthPercent}% health  ·  ${h.batteryCycleCount} cycles  ·  ${h.isOnAcPower ? 'AC power' : 'Battery'}`);
  row('Disk',         `${h.diskUsedPercent.toFixed(1)}% used  ·  ${(h.diskFreeBytes / 1e9).toFixed(0)} GB free`);
  row('RAM',          `${h.ramUsedPercent.toFixed(0)}% used  ·  ${h.pageFaultsPerSec} page faults/sec`);
  row('CPU',          `${h.cpuLoadPercent}% load  ·  ${h.startupItemCount} startup items${h.thermalThrottlingDetected ? `  ·  Throttling at ${h.cpuTemperatureCelsius}°C` : ''}`);
  row('Wi-Fi',        `${h.wifiAdapterName}  ·  ${h.wifiSignalStrengthPercent}% signal  ·  ${h.wifiReconnectsLast24h} drops (24 h)`);
  row('Crashes',      `${h.appCrashesLast7Days} app  ·  ${h.systemCrashesLast7Days} system  (last 7 days)`);
  if (h.recentCrashSignatures.length > 0) {
    h.recentCrashSignatures.forEach(s => bullet(s));
  }
  gap();
  rule();

  // ── Evidence & rules ─────────────────────────────────────────────────────
  newPageIfNeeded(100);
  heading('Evidence Collected');
  result.decision.evidenceItems.forEach(e => bullet(e));
  gap(6);
  doc.setFontSize(8.5);
  doc.setFont('helvetica', 'normal');
  doc.setTextColor(...GRAY);
  doc.text(`Rules triggered: ${result.decision.triggeredRuleIds.join('  ·  ')}`, MARGIN, y);
  y += 14;
  gap();
  rule();

  // ── Actions ───────────────────────────────────────────────────────────────
  newPageIfNeeded(100);
  heading('Actions Attempted');
  if (result.actions.length === 0) {
    bullet('No automated actions were run.');
  } else {
    result.actions.forEach(a => {
      const done = a.result === 'Success' || a.result === 'PartialSuccess';
      bullet(
        done
          ? `${a.actionName}  —  ${a.resultDetail || a.result}`
          : `${a.actionName}  (${a.result})`,
        done
      );
    });
  }
  gap();
  rule();

  // ── Outcome ───────────────────────────────────────────────────────────────
  newPageIfNeeded(60);
  heading('Outcome');
  row('Status',       packet.outcome);
  row('Next step',    'Share this packet with Lenovo Support to expedite your case.');
  gap(16);

  // ── Footer ────────────────────────────────────────────────────────────────
  const pageCount = doc.getNumberOfPages();
  for (let i = 1; i <= pageCount; i++) {
    doc.setPage(i);
    const py = doc.internal.pageSize.getHeight() - 22;
    doc.setFontSize(7.5);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(...GRAY);
    doc.text('Lenovo SmartFix  ·  Confidential diagnostic data', MARGIN, py);
    doc.text(`Page ${i} of ${pageCount}`, W - MARGIN, py, { align: 'right' });
  }

  doc.save(`SmartFix_Packet_${result.scanId}.pdf`);
}

// ── JSON line coloriser ───────────────────────────────────────────────────────

function JsonLine({ text }: { text: string }) {
  const parts = text.split(/("(?:[^"\\]|\\.)*"|\b\d+\.?\d*\b|\b(?:true|false|null)\b)/g);
  return (
    <div>
      {parts.map((part, i) => {
        if (/^"/.test(part) && text.indexOf(part + ':') >= 0)
          return <span key={i} className="json-key">{part}</span>;
        if (/^"/.test(part))    return <span key={i} className="json-string">{part}</span>;
        if (/^\d/.test(part))   return <span key={i} className="json-number">{part}</span>;
        if (/^(true|false|null)$/.test(part)) return <span key={i} className="json-bool">{part}</span>;
        return <span key={i}>{part}</span>;
      })}
    </div>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function EscalationPacketPage({ result }: Props) {
  const [redact,   setRedact]   = useState(true);
  const [exported, setExported] = useState<'json' | 'pdf' | null>(null);
  const [showJson, setShowJson] = useState(false);
  const [pdfBusy,  setPdfBusy]  = useState(false);

  const packet  = buildPacketData(result, redact);
  const jsonStr = JSON.stringify(packet, null, 2);

  const autoFixed = result.actions.filter(a => a.result === 'Success');
  const pending   = result.actions.filter(a => a.result === 'Pending');

  function handleExportJson() {
    const blob = new Blob([jsonStr], { type: 'application/json' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = `SmartFix_Packet_${result.scanId}.json`;
    a.click();
    URL.revokeObjectURL(url);
    setExported('json');
  }

  async function handleExportPdf() {
    setPdfBusy(true);
    try {
      await exportPdf(result, redact);
      setExported('pdf');
    } finally {
      setPdfBusy(false);
    }
  }

  return (
    <div className="content">
      <div className="page-header">
        <h1 className="page-h1">Support Packet</h1>
        <p className="page-sub">
          SmartFix has compiled everything it found into a support packet.
          Send it to Lenovo Support to skip first-line diagnostics and go straight to resolution.
        </p>
      </div>

      {/* Outcome verdict */}
      <div className={`verdict ${result.decision.path === 'Escalate' ? '' : 'guided'}`}>
        <span className="verdict-icon">
          {result.decision.path === 'Escalate' ? <IconWarning size={22} /> : <IconTool size={22} />}
        </span>
        <div className="verdict-body">
          <div className="verdict-path">
            <span className={`badge ${result.decision.path === 'Escalate' ? 'badge-red' : 'badge-amber'}`}>
              {result.decision.path === 'Escalate' ? 'Escalation required' : 'Guided resolution'}
            </span>
          </div>
          <p className="verdict-reason">{result.decision.userFacingReason}</p>
        </div>
      </div>

      {/* What was found */}
      <div className="card">
        <div className="card-title"><span className="card-title-icon" />What SmartFix found</div>
        <div className="data-row">
          <span className="data-label">Device</span>
          <span className="data-value">{result.deviceProfile.model}</span>
        </div>
        <div className="data-row">
          <span className="data-label">Reported issue</span>
          <span className="data-value">{result.symptom}</span>
        </div>
        <div className="data-row">
          <span className="data-label">Rules triggered</span>
          <span className="data-value">
            <div className="tag-row">
              {result.decision.triggeredRuleIds.map(id => (
                <span key={id} className="badge badge-gray" style={{ fontSize: 10 }}>{id}</span>
              ))}
            </div>
          </span>
        </div>
        <div className="data-row">
          <span className="data-label">Evidence</span>
          <div className="data-value">
            {result.decision.evidenceItems.map((e, i) => (
              <div key={i} style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 3 }}>· {e}</div>
            ))}
          </div>
        </div>
      </div>

      {/* Actions attempted */}
      <div className="card">
        <div className="card-title"><span className="card-title-icon" />Actions attempted</div>

        {autoFixed.length > 0 && (
          <>
            <div style={{ fontSize: 11, fontWeight: 600, color: 'var(--success)', marginBottom: 8, textTransform: 'uppercase', letterSpacing: '.04em' }}>
              Completed
            </div>
            {autoFixed.map(a => (
              <div key={a.actionInstanceId} style={{ display: 'flex', gap: 8, alignItems: 'flex-start', marginBottom: 8, fontSize: 12 }}>
                <span style={{ color: 'var(--success)' }}><IconCheck size={13} /></span>
                <div>
                  <div style={{ fontWeight: 600 }}>{a.actionName}</div>
                  {a.resultDetail && <div style={{ color: 'var(--muted)' }}>{a.resultDetail}</div>}
                </div>
              </div>
            ))}
          </>
        )}

        {pending.length > 0 && (
          <>
            <div style={{ fontSize: 11, fontWeight: 600, color: 'var(--muted)', marginTop: autoFixed.length ? 12 : 0, marginBottom: 8, textTransform: 'uppercase', letterSpacing: '.04em' }}>
              Not yet run
            </div>
            {pending.map(a => (
              <div key={a.actionInstanceId} style={{ display: 'flex', gap: 8, alignItems: 'flex-start', marginBottom: 8, fontSize: 12 }}>
                <span style={{ color: 'var(--muted)' }}>–</span>
                <div style={{ fontWeight: 600, color: 'var(--muted)' }}>{a.actionName}</div>
              </div>
            ))}
          </>
        )}

        {autoFixed.length === 0 && pending.length === 0 && (
          <p style={{ fontSize: 12, color: 'var(--muted)' }}>No actions were attempted.</p>
        )}
      </div>

      {/* Privacy */}
      <div className="card">
        <div className="card-title">
          <span className="card-title-icon" />
          <IconShield size={14} />
          Privacy
        </div>
        <label className="toggle-row" onClick={() => setRedact(r => !r)}>
          <div className={`toggle ${!redact ? 'on' : ''}`} />
          <div>
            <div style={{ fontSize: 13, fontWeight: 500 }}>Include serial number and device identifiers</div>
            <div style={{ fontSize: 11, color: 'var(--muted)', marginTop: 2 }}>
              {redact
                ? 'Serial numbers and device IDs are redacted — only diagnostic metadata included.'
                : 'Full device identity included in the packet.'}
            </div>
          </div>
        </label>
      </div>

      {/* ── Send to Lenovo Support — next-step action panel ── */}
      <div style={{
        background: '#1A1A1A',
        border: '1px solid #333',
        padding: '24px',
        marginBottom: 16,
      }}>
        {/* Header */}
        <div style={{ display: 'flex', alignItems: 'flex-start', gap: 14, marginBottom: 20 }}>
          <div style={{
            width: 40, height: 40, background: 'var(--red)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            flexShrink: 0,
          }}>
            <IconDownload size={18} />
          </div>
          <div>
            <div style={{ fontSize: 15, fontWeight: 700, color: '#FFFFFF', marginBottom: 3 }}>
              Send this packet to Lenovo Support
            </div>
            <div style={{ fontSize: 12, color: '#888', lineHeight: 1.5 }}>
              Export your diagnostic report and attach it when opening a support case.
              Lenovo Support can skip first-line questions and go straight to resolution.
            </div>
          </div>
        </div>

        {/* Numbered steps */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 22 }}>
          {[
            ['01', 'Export your report below (PDF or JSON)'],
            ['02', 'Open a case at support.lenovo.com or call Lenovo Support'],
            ['03', 'Attach the exported file to your case'],
          ].map(([n, text]) => (
            <div key={n} style={{ display: 'flex', alignItems: 'center', gap: 12, fontSize: 12, color: '#CCCCCC' }}>
              <span style={{
                minWidth: 26, height: 26, background: '#333',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                fontSize: 10, fontWeight: 700, color: 'var(--red)', flexShrink: 0,
              }}>
                {n}
              </span>
              {text}
            </div>
          ))}
        </div>

        {/* Export buttons */}
        <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap', alignItems: 'center' }}>
          <button
            className="btn btn-primary"
            style={{ gap: 8 }}
            disabled={pdfBusy}
            onClick={handleExportPdf}
          >
            <IconDownload size={14} />
            {pdfBusy ? 'Generating PDF…' : 'Export PDF Report'}
          </button>
          <button
            className="btn btn-secondary"
            style={{ color: '#CCC', borderColor: '#444', background: 'transparent', gap: 8 }}
            onClick={handleExportJson}
          >
            <IconDownload size={14} />
            Export JSON
          </button>
          <button
            className="btn btn-ghost"
            style={{ color: '#666', fontSize: 12 }}
            onClick={() => setShowJson(v => !v)}
          >
            {showJson ? 'Hide preview' : 'Preview data'}
          </button>
        </div>

        {/* Success confirmation */}
        {exported && (
          <div style={{ marginTop: 16, display: 'flex', alignItems: 'center', gap: 8, fontSize: 12, color: '#6EE7A0' }}>
            <IconCheck size={13} />
            {exported === 'pdf'
              ? 'PDF saved — attach it when opening your Lenovo Support case.'
              : 'JSON saved — attach it when opening your Lenovo Support case.'}
          </div>
        )}
      </div>

      {/* JSON preview */}
      {showJson && (
        <div className="json-preview">
          {jsonStr.split('\n').map((line, i) => (
            <JsonLine key={i} text={line} />
          ))}
        </div>
      )}

      {/* Packet ID */}
      <p style={{ fontSize: 11, color: 'var(--muted)', textAlign: 'center', marginTop: 8 }}>
        Packet ID: {result.scanId}  ·  Generated {new Date().toLocaleString()}
      </p>
    </div>
  );
}

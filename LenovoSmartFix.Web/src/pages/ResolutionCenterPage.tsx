import { useState } from 'react';
import type { RemediationAction, ScanResult } from '../types/models';
import { IconCheck, IconCircleCheck, IconTool } from '../components/Icons';

interface Props {
  result: ScanResult;
  onUpdate: (updated: ScanResult) => void;
  onEscalation: () => void;
}

export default function ResolutionCenterPage({ result, onUpdate, onEscalation }: Props) {
  const [actions, setActions] = useState<RemediationAction[]>(result.actions);
  const [running, setRunning] = useState<string | null>(null);

  const dec        = result.decision;
  const autoFixed  = actions.filter(a => !a.consentRequired && a.result === 'Success');
  const allDone    = actions.every(a => a.result === 'Success' || a.result === 'PartialSuccess');

  async function execute(actionInstanceId: string, actionId: string) {
    setRunning(actionInstanceId);
    await new Promise(r => setTimeout(r, 1400));
    setActions(prev => {
      const next = prev.map(a =>
        a.actionInstanceId === actionInstanceId
          ? { ...a, result: 'Success' as const, resultDetail: mockDetail(actionId), userConsented: a.consentRequired }
          : a
      );
      onUpdate({ ...result, actions: next });
      return next;
    });
    setRunning(null);
  }

  return (
    <div className="content">
      <div className="page-header">
        <h1 className="page-h1">Resolution Center</h1>
        <p className="page-sub">{dec.userFacingReason}</p>
      </div>

      {/* Auto-resolved banner */}
      {autoFixed.length > 0 && (
        <div className="auto-resolved-banner">
          <span style={{ color: 'var(--success)', flexShrink: 0, marginTop: 1 }}>
            <IconCheck size={15} />
          </span>
          <div>
            <strong>SmartFix automatically applied {autoFixed.length} safe fix{autoFixed.length > 1 ? 'es' : ''}</strong>
            <span style={{ color: 'var(--muted)', marginLeft: 4 }}>— no action required on your part.</span>
          </div>
        </div>
      )}

      {/* Empty state */}
      {actions.length === 0 && (
        <div className="empty-state">
          <div className="empty-state-icon"><IconTool size={32} /></div>
          <div className="empty-state-title">No automated fixes available</div>
          <div className="empty-state-sub">
            SmartFix could not identify a safe automated action for this issue.
            Use the support packet to escalate with full diagnostic context.
          </div>
          <button className="btn btn-primary" style={{ marginTop: 16 }} onClick={onEscalation}>
            Prepare Support Packet →
          </button>
        </div>
      )}

      {/* Action list */}
      {actions.map(action => {
        const isRunning = running === action.actionInstanceId;
        const isDone    = action.result === 'Success' || action.result === 'PartialSuccess';
        const isSafe    = !action.consentRequired;

        const stripeColor = isDone
          ? 'var(--success)'
          : isSafe
          ? '#9CA3AF'
          : 'var(--warning)';

        return (
          <div key={action.actionInstanceId} className="action-card">
            <div className="action-stripe" style={{ background: stripeColor }} />

            <div className="action-body">
              <div className="action-name">
                {action.actionName}
                {isSafe && !isDone && (
                  <span className="badge badge-gray" style={{ fontSize: 10 }}>Auto</span>
                )}
                {!isSafe && !isDone && (
                  <span className="badge badge-amber" style={{ fontSize: 10 }}>Requires confirmation</span>
                )}
                {isDone && (
                  <span className="badge badge-green" style={{ fontSize: 10, display: 'inline-flex', alignItems: 'center', gap: 3 }}>
                    <IconCheck size={10} /> Done
                  </span>
                )}
              </div>
              <div className="action-desc">{action.description}</div>

              {isDone && action.resultDetail && (
                <div className="action-result success">
                  <IconCheck size={12} /> {action.resultDetail}
                </div>
              )}
              {isRunning && (
                <div className="action-result running">
                  <span style={{
                    display: 'inline-block', width: 11, height: 11,
                    border: '2px solid currentColor', borderTopColor: 'transparent',
                    borderRadius: '50%', animation: 'spin .7s linear infinite',
                  }} />
                  Running…
                </div>
              )}
            </div>

            <div className="action-right">
              {!isDone && !isSafe && (
                <button
                  className="btn btn-primary btn-sm"
                  disabled={isRunning || running !== null}
                  onClick={() => execute(action.actionInstanceId, action.actionId)}
                >
                  {isRunning ? 'Running…' : 'Run Fix'}
                </button>
              )}
              {isDone && (
                <span style={{ color: 'var(--success)' }}><IconCircleCheck size={20} /></span>
              )}
              {!isDone && action.isRollbackable && (
                <span style={{ fontSize: 10, color: 'var(--muted)' }}>Reversible</span>
              )}
            </div>
          </div>
        );
      })}

      {/* All done */}
      {allDone && actions.length > 0 && (
        <div style={{
          background: '#F0FDF4', border: '1px solid #BBF7D0',
          padding: '14px 18px', marginTop: 4, fontSize: 12.5,
          display: 'flex', gap: 10, alignItems: 'center',
        }}>
          <span style={{ color: 'var(--success)' }}><IconCircleCheck size={16} /></span>
          <span>All available fixes have been applied. If the issue persists, prepare a support packet.</span>
        </div>
      )}

      {/* What happens next */}
      {actions.length > 0 && (
        <div className="card" style={{ marginTop: 12 }}>
          <div className="card-title"><span className="card-title-icon" />What happens next?</div>
          {dec.path === 'Escalate' ? (
            <p style={{ fontSize: 12, color: 'var(--muted)', lineHeight: 1.65 }}>
              This issue is marked for support escalation. SmartFix was unable to resolve it
              automatically. Generate a support packet to hand to Lenovo Support — it includes
              all findings and actions already attempted.
            </p>
          ) : (
            <p style={{ fontSize: 12, color: 'var(--muted)', lineHeight: 1.65 }}>
              After running the fixes above, restart your device and monitor for improvement
              over the next 24 hours. If the issue persists, generate a support packet to
              escalate with full diagnostic context already attached.
            </p>
          )}
        </div>
      )}

      <div className="footer-actions">
        <button className="btn btn-primary" onClick={onEscalation}>
          Prepare Support Packet →
        </button>
      </div>
    </div>
  );
}

function mockDetail(id: string): string {
  const map: Record<string, string> = {
    'REM-STARTUP-OPT':    'Startup manager opened. Review and disable high-impact items.',
    'REM-NET-SVC':        'DNS Client, NLA, and WLAN AutoConfig restarted.',
    'REM-UPDATE-VANTAGE': 'Lenovo Vantage update flow launched.',
    'REM-TEMP-CLEANUP':   'Freed 3.2 GB of temporary files.',
    'REM-POWER-PLAN':     'Power plan set to Balanced.',
    'REM-DNS-FLUSH':      'DNS cache flushed.',
  };
  return map[id] ?? 'Completed successfully.';
}

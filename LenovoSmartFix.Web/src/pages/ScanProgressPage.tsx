import { useEffect, useRef, useState } from 'react';
import { simulateScanAsync, SCAN_STEPS } from '../data/mockScan';
import type { ScanResult } from '../types/models';
import { IconCheck } from '../components/Icons';

interface Props {
  symptom: string;
  onDone: (result: ScanResult) => void;
}

export default function ScanProgressPage({ symptom, onDone }: Props) {
  const [percent,   setPercent]   = useState(0);
  const [stepLabel, setStepLabel] = useState(SCAN_STEPS[0].label);
  const [stepIdx,   setStepIdx]   = useState(0);
  const abortRef = useRef(false);

  useEffect(() => {
    abortRef.current = false;

    simulateScanAsync(symptom, (p, label) => {
      if (abortRef.current) return;
      setPercent(p);
      setStepLabel(label);
      setStepIdx(SCAN_STEPS.findIndex(s => s.label === label));
    }).then(result => {
      if (!abortRef.current) onDone(result);
    });

    return () => { abortRef.current = true; };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [symptom]);

  const STEPS = SCAN_STEPS.filter((_, i) => i < SCAN_STEPS.length - 1);

  return (
    <div className="scan-wrap">
      {/* Dual-ring spinner */}
      <div className="scan-spinner">
        <div className="scan-spinner-ring outer" />
        <div className="scan-spinner-ring inner" />
      </div>

      <h2 style={{ fontSize: 18, fontWeight: 700, marginBottom: 6 }}>
        Scanning Your Device
      </h2>
      <p style={{ fontSize: 12, color: 'var(--muted)', marginBottom: 28, maxWidth: 400 }}>
        Reported issue: <strong>{symptom}</strong>
      </p>

      {/* Progress bar + percent */}
      <div style={{ width: '100%', maxWidth: 480, marginBottom: 6 }}>
        <div className="progress-track">
          <div className="progress-fill" style={{ width: `${percent}%` }} />
        </div>
      </div>
      <div style={{ display: 'flex', justifyContent: 'space-between', width: '100%', maxWidth: 480, marginBottom: 24 }}>
        <span style={{ fontSize: 11.5, color: 'var(--muted)' }}>{stepLabel}</span>
        <span style={{ fontSize: 11.5, color: 'var(--red)', fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
          {percent}%
        </span>
      </div>

      {/* Animated step checklist */}
      <div style={{
        background: 'var(--surface)', border: '1px solid var(--border)',
        padding: '14px 18px', width: '100%', maxWidth: 480, textAlign: 'left',
      }}>
        {STEPS.map((step, i) => {
          const done   = i < stepIdx;
          const active = i === stepIdx;
          if (i > stepIdx + 1) return null; // only show completed + current + next
          return (
            <div
              key={i}
              className="step-item"
              style={{ animationDelay: `${i * 40}ms` }}
            >
              <span
                className="step-dot"
                style={{
                  background:  done ? 'var(--success)' : active ? 'transparent' : '#F0F0F0',
                  border:      done ? 'none' : active ? '2px solid var(--red)' : '1.5px solid #D0D0D0',
                  color:       'white',
                }}
              >
                {done && <IconCheck size={9} />}
              </span>
              <span style={{
                color:      done ? 'var(--success)' : active ? 'var(--text)' : 'var(--muted)',
                fontWeight: active ? 600 : 400,
                fontSize:   12,
                transition: 'color .2s',
              }}>
                {step.label}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

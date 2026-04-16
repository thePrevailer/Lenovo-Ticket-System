import { useState } from 'react';
import { SYMPTOMS } from '../data/mockScan';
import {
  IconMonitor, IconBattery, IconCpu, IconWifi,
  IconSliders, IconBolt, IconLock,
} from '../components/Icons';

interface Props {
  onStart: (symptom: string) => void;
}

const CHECKS = [
  { icon: <IconMonitor size={16} />, label: 'Device identity & hardware profile' },
  { icon: <IconBattery size={16} />, label: 'Battery health & power settings' },
  { icon: <IconCpu     size={16} />, label: 'Disk, RAM, and CPU metrics' },
  { icon: <IconWifi    size={16} />, label: 'Wi-Fi stability & crash history' },
  { icon: <IconSliders size={16} />, label: 'BIOS, firmware & driver versions' },
  { icon: <IconBolt    size={16} />, label: 'Safe automatic fixes where possible' },
];

export default function OverviewPage({ onStart }: Props) {
  const [selected, setSelected] = useState(SYMPTOMS[0]);

  return (
    <div className="content">
      <div className="page-header">
        <h1 className="page-h1">How can SmartFix help you today?</h1>
        <p className="page-sub">
          SmartFix scans your device, checks your software stack, and resolves common
          issues before you need to contact support.
        </p>
      </div>

      {/* Symptom selector */}
      <div className="card">
        <div className="card-title">
          <span className="card-title-icon" />
          What issue are you experiencing?
        </div>

        <div className="symptom-grid">
          {SYMPTOMS.map(s => (
            <div
              key={s}
              className={`symptom-tile ${selected === s ? 'selected' : ''}`}
              onClick={() => setSelected(s)}
              role="radio"
              aria-checked={selected === s}
              tabIndex={0}
              onKeyDown={e => e.key === 'Enter' && setSelected(s)}
            >
              <span className="symptom-tile-dot" />
              {s}
            </div>
          ))}
        </div>

        <button
          className="btn btn-primary btn-full"
          style={{ fontSize: 14, height: 44 }}
          onClick={() => onStart(selected)}
        >
          Scan My Device →
        </button>
      </div>

      {/* What SmartFix checks */}
      <div className="card">
        <div className="card-title">
          <span className="card-title-icon" />
          What SmartFix checks
        </div>
        <div className="feature-grid">
          {CHECKS.map(({ icon, label }) => (
            <div key={label} className="feature-item">
              <span className="feature-icon">{icon}</span>
              <span className="feature-label">{label}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Privacy notice */}
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        gap: 6, fontSize: 11, color: 'var(--muted)', marginTop: 4, lineHeight: 1.6,
      }}>
        <span style={{ color: 'var(--muted)', opacity: .7 }}><IconLock size={12} /></span>
        All scanning and diagnosis stays on your device. Nothing is uploaded unless
        you explicitly share a support packet.
      </div>
    </div>
  );
}

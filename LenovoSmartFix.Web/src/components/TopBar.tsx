import type { Page } from '../types/models';
import { IconCheck } from './Icons';

const STEPS: { key: Page; label: string }[] = [
  { key: 'overview',   label: 'Overview' },
  { key: 'scanning',   label: 'Scan' },
  { key: 'findings',   label: 'Findings' },
  { key: 'resolution', label: 'Resolution' },
  { key: 'escalation', label: 'Support Packet' },
];

const ORDER: Page[] = ['overview', 'scanning', 'findings', 'resolution', 'escalation'];

interface Props {
  current: Page;
  onNavigate: (p: Page) => void;
  scanDone: boolean;
}

export default function TopBar({ current, onNavigate, scanDone }: Props) {
  const currentIdx = ORDER.indexOf(current);

  return (
    <div className="page-shell" style={{ minHeight: 0 }}>
      <header className="topbar">
        <div className="topbar-accent" />
        <span className="topbar-title">Lenovo SmartFix</span>
        <span className="topbar-sub">Pre-Support Resolution · Prototype</span>
      </header>

      <nav className="breadcrumb">
        {STEPS.map((step, i) => {
          const idx = ORDER.indexOf(step.key);
          const isDone = idx < currentIdx && scanDone;
          const isActive = step.key === current;
          const isReachable = step.key === 'overview'
            || (scanDone && idx <= ORDER.indexOf('escalation'))
            || (step.key === 'scanning' && !scanDone);

          return (
            <span key={step.key} className="breadcrumb-step-wrap" style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              {i > 0 && <span className="breadcrumb-sep">›</span>}
              <span
                className={`breadcrumb-step ${isActive ? 'active' : ''} ${isDone ? 'done' : ''}`}
                style={{ cursor: isReachable && !isActive ? 'pointer' : 'default' }}
                onClick={() => isReachable && !isActive && onNavigate(step.key)}
              >
                {isDone && <span style={{ color: 'var(--success)', marginRight: 3 }}><IconCheck size={11} /></span>}
                {step.label}
              </span>
            </span>
          );
        })}
      </nav>
    </div>
  );
}

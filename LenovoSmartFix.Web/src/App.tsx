import { useState, useCallback } from 'react';
import type { Page, ScanResult } from './types/models';
import TopBar from './components/TopBar';
import OverviewPage from './pages/OverviewPage';
import ScanProgressPage from './pages/ScanProgressPage';
import FindingsPage from './pages/FindingsPage';
import ResolutionCenterPage from './pages/ResolutionCenterPage';
import EscalationPacketPage from './pages/EscalationPacketPage';

export default function App() {
  const [page, setPage] = useState<Page>('overview');
  const [symptom, setSymptom] = useState('');
  const [result, setResult] = useState<ScanResult | null>(null);

  const handleStart = useCallback((s: string) => {
    setSymptom(s);
    setResult(null);
    setPage('scanning');
  }, []);

  const handleScanDone = useCallback((scanResult: ScanResult) => {
    setResult(scanResult);
    setPage('findings');
  }, []);

  const handleUpdateResult = useCallback((updated: ScanResult) => {
    setResult(updated);
  }, []);

  return (
    <div style={{ minHeight: '100vh', background: 'var(--bg)' }}>
      <TopBar
        current={page}
        onNavigate={setPage}
        scanDone={result !== null}
      />

      {page === 'overview' && (
        <OverviewPage onStart={handleStart} />
      )}

      {page === 'scanning' && (
        <ScanProgressPage symptom={symptom} onDone={handleScanDone} />
      )}

      {page === 'findings' && result && (
        <FindingsPage
          result={result}
          onResolution={() => setPage('resolution')}
          onEscalation={() => setPage('escalation')}
        />
      )}

      {page === 'resolution' && result && (
        <ResolutionCenterPage
          result={result}
          onUpdate={handleUpdateResult}
          onEscalation={() => setPage('escalation')}
        />
      )}

      {page === 'escalation' && result && (
        <EscalationPacketPage result={result} />
      )}
    </div>
  );
}

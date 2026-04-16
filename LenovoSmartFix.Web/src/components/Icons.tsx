/**
 * Lenovo SmartFix icon set
 * 20×20 viewBox · stroke-based · currentColor · no fill by default
 * Matches Lenovo Vantage / Lenovo Design Language aesthetic:
 *   - 1.5 px stroke weight
 *   - Round line-caps (clean at small sizes)
 *   - Geometric, minimal, no decoration
 */

interface P { size?: number }

const svg = (size: number) => ({
  width:            size,
  height:           size,
  viewBox:          '0 0 20 20',
  fill:             'none' as const,
  stroke:           'currentColor',
  strokeWidth:      1.5,
  strokeLinecap:    'round' as const,
  strokeLinejoin:   'round' as const,
  display:          'inline-block' as const,
  verticalAlign:    'middle' as const,
  flexShrink:       0,
});

// ── Device / hardware ────────────────────────────────────────────────────────

/** Monitor screen — device identity */
export function IconMonitor({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <rect x="2" y="3" width="16" height="11" rx="1" />
      <line x1="7"  y1="14" x2="7"  y2="17" />
      <line x1="13" y1="14" x2="13" y2="17" />
      <line x1="5"  y1="17" x2="15" y2="17" />
    </svg>
  );
}

/** Battery — battery health & power */
export function IconBattery({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <rect x="1" y="6.5" width="14" height="7" rx="1" />
      {/* terminal */}
      <line x1="15" y1="9"  x2="18" y2="9"  />
      <line x1="18" y1="9"  x2="18" y2="11" />
      <line x1="18" y1="11" x2="15" y2="11" />
      {/* charge level */}
      <rect x="3" y="8.5" width="7" height="3" rx="0.5" fill="currentColor" stroke="none" />
    </svg>
  );
}

/** Cylinder — disk / storage */
export function IconDisk({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <ellipse cx="10" cy="5.5" rx="7.5" ry="2.5" />
      <line x1="2.5" y1="5.5"  x2="2.5" y2="14.5" />
      <line x1="17.5" y1="5.5" x2="17.5" y2="14.5" />
      <ellipse cx="10" cy="14.5" rx="7.5" ry="2.5" />
    </svg>
  );
}

/** Wi-Fi arcs — connectivity */
export function IconWifi({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <path d="M2 8.5 C4.5 5.5 7 4.5 10 4.5 S15.5 5.5 18 8.5" />
      <path d="M5 11.5 C6.5 9.5 8 8.5 10 8.5 S13.5 9.5 15 11.5" />
      <path d="M7.5 14.5 C8.5 13 9.2 12.5 10 12.5 S11.5 13 12.5 14.5" />
      <circle cx="10" cy="17" r="1" fill="currentColor" stroke="none" />
    </svg>
  );
}

/** Chip / CPU — CPU & RAM metrics */
export function IconCpu({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <rect x="5.5" y="5.5" width="9" height="9" />
      {/* top pins */}
      <line x1="8"  y1="2"   x2="8"  y2="5.5" />
      <line x1="12" y1="2"   x2="12" y2="5.5" />
      {/* bottom pins */}
      <line x1="8"  y1="14.5" x2="8"  y2="18" />
      <line x1="12" y1="14.5" x2="12" y2="18" />
      {/* left pins */}
      <line x1="2"   y1="8"  x2="5.5" y2="8"  />
      <line x1="2"   y1="12" x2="5.5" y2="12" />
      {/* right pins */}
      <line x1="14.5" y1="8"  x2="18" y2="8"  />
      <line x1="14.5" y1="12" x2="18" y2="12" />
    </svg>
  );
}

/** Lightning bolt — automatic / fast fixes */
export function IconBolt({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <polygon
        points="12,1 4,11 10,11 8,19 17,9 11,9"
        fill="currentColor"
        stroke="none"
      />
    </svg>
  );
}

/** Horizontal sliders — BIOS / driver tuning */
export function IconSliders({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <line x1="2" y1="5"  x2="18" y2="5"  />
      <line x1="2" y1="10" x2="18" y2="10" />
      <line x1="2" y1="15" x2="18" y2="15" />
      <circle cx="6"  cy="5"  r="2" fill="currentColor" stroke="none" />
      <circle cx="13" cy="10" r="2" fill="currentColor" stroke="none" />
      <circle cx="8"  cy="15" r="2" fill="currentColor" stroke="none" />
    </svg>
  );
}

// ── Status / verdict ─────────────────────────────────────────────────────────

/** Warning triangle — escalation / attention needed */
export function IconWarning({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <path d="M10 2 L18.5 17.5 H1.5 Z" />
      <line x1="10" y1="8.5" x2="10" y2="13" />
      <circle cx="10" cy="15.5" r="0.75" fill="currentColor" stroke="none" />
    </svg>
  );
}

/** Wrench — guided / manual resolution */
export function IconTool({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <path d="M14.5 2C12 2 10.5 3.5 10.5 5.5C10.5 6.2 10.8 6.9 11.2 7.4L4.5 14C3.7 14.8 3.7 16.1 4.5 16.9 5.3 17.7 6.6 17.7 7.4 16.9L14.1 10.2C14.6 10.6 15.2 10.8 15.9 10.8 17.9 10.8 19.4 9.3 19.4 7.3L17.2 9.5 14.9 7.2 17.1 5C16.6 3.3 15.7 2 14.5 2Z" />
    </svg>
  );
}

/** Circle with checkmark — auto-resolved / success */
export function IconCircleCheck({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <circle cx="10" cy="10" r="8" />
      <polyline points="6.5,10 9,12.5 13.5,7.5" />
    </svg>
  );
}

/** Checkmark — done / confirmed */
export function IconCheck({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <polyline points="3,10 8,15 17,5" />
    </svg>
  );
}

// ── Actions ──────────────────────────────────────────────────────────────────

/** Padlock — privacy / local-only */
export function IconLock({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <rect x="3.5" y="9" width="13" height="9" rx="1" />
      <path d="M7 9 V7 a3 3 0 0 1 6 0 V9" />
    </svg>
  );
}

/** Shield with check — security / privacy  */
export function IconShield({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <path d="M10 2 L18 5.5 V11 C18 15 14 17.5 10 19 C6 17.5 2 15 2 11 V5.5 Z" />
      <polyline points="7,10 9,12.5 13.5,7.5" />
    </svg>
  );
}

/** Download arrow — export */
export function IconDownload({ size = 16 }: P) {
  return (
    <svg {...svg(size)} aria-hidden="true">
      <line x1="10" y1="2"  x2="10" y2="13" />
      <polyline points="6,9.5 10,13.5 14,9.5" />
      <line x1="3"  y1="17" x2="17" y2="17" />
    </svg>
  );
}

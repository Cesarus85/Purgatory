"""Read-only report from one V17 diagnostic directory. Never infers a hardware pass."""
import csv
import json
import math
import sys
from collections import defaultdict
from pathlib import Path

def number(row, key):
    value = row.get(key, '')
    if value == '':
        return None
    result = float(value)
    return result if math.isfinite(result) else None

def analyze(directory):
    directory = Path(directory)
    metadata = (directory / 'metadata.txt').read_text()
    with (directory / 'frames.csv').open(newline='') as handle:
        rows = list(csv.DictReader(handle))
    with (directory / 'events.csv').open(newline='') as handle:
        events = list(csv.DictReader(handle))
    groups = defaultdict(list)
    for row in rows:
        if number(row, 'frames') and row.get('phase'):
            groups[row['phase']].append(row)
    report = {'metadata': metadata, 'hardware_gate': 'NOT_AUTOMATICALLY_VERIFIED',
              'session_end': next((e['detail'] for e in reversed(events) if e.get('event') == 'session_end'), 'missing'),
              'phases': {}, 'warnings': []}
    for phase, samples in groups.items():
        frames = sum(number(r, 'frames') or 0 for r in samples)
        def weighted(metric, weights):
            valid = [(number(r, metric), number(r, weights)) for r in samples]
            valid = [(value, weight) for value, weight in valid if value is not None and weight and weight > 0]
            return sum(v*w for v,w in valid)/sum(w for _,w in valid) if valid else None
        def maximum(metric):
            values = [number(r, metric) for r in samples]
            values = [v for v in values if v is not None]
            return max(values) if values else None
        report['phases'][phase] = {
            'samples': len(samples), 'frames': frames,
            'frame_mean_ms': weighted('frame_mean_ms', 'frames'),
            'worst_window_p95_ms_NOT_global_p95': maximum('frame_p95_ms'),
            'worst_window_p99_ms_NOT_global_p99': maximum('frame_p99_ms'),
            'frame_max_ms': maximum('frame_max_ms'),
            'cpu_mean_ms': weighted('cpu_mean_ms', 'cpu_samples'),
            'gpu_mean_ms': weighted('gpu_mean_ms', 'gpu_samples'),
            'over_budget_percent': 100*sum(number(r, 'over_budget') or 0 for r in samples)/frames,
            'visible_portals_max': maximum('visible_portals'),
            'living_enemies_max': maximum('living_enemies'),
            'unity_memory_peak_bytes': maximum('unity_allocated_bytes'),
            'gc_collections': sum(number(r, 'gc_collections') or 0 for r in samples),
            'mesh_bakes': sum(number(r, 'mesh_bakes') or 0 for r in samples),
            'triangle_tests': sum(number(r, 'triangle_tests') or 0 for r in samples),
        }
    if 'stress_seed' in metadata:
        for phase in ('baseline', 'one_portal', 'two_portals', 'actors', 'combat_fx'):
            if phase not in groups: report['warnings'].append('Missing benchmark phase: ' + phase)
        for phase in ('two_portals', 'actors', 'combat_fx'):
            samples = groups.get(phase, [])
            if not samples or sum((number(r, 'visible_portals') or 0) >= 2 for r in samples) < len(samples)*.8:
                report['warnings'].append(phase + ': two visible portals not sustained in 80% of samples')
        if not any(e.get('event') == 'benchmark_hit' for e in events):
            report['warnings'].append('No confirmed scripted combat hit; effects phase incomplete')
    if any(r.get('overlay') == 'True' for r in rows): report['warnings'].append('Overlay observer overhead present')
    if not any(number(r, 'gpu_samples') for r in rows): report['warnings'].append('GPU timing unavailable, not zero')
    if not any(number(r, 'cpu_samples') for r in rows): report['warnings'].append('CPU timing unavailable, not zero')
    if any(r.get('refresh_source') != 'xr' for r in rows): report['warnings'].append('Refresh rate includes a target fallback, not a hardware measurement')
    if report['session_end'] != 'complete': report['warnings'].append('No normally completed benchmark confirmed')
    if any(e.get('event', '').endswith('_skipped') for e in events): report['warnings'].append('Room safety skipped requested content')
    return report

if __name__ == '__main__':
    print(json.dumps(analyze(sys.argv[1]), indent=2, ensure_ascii=False, allow_nan=False))

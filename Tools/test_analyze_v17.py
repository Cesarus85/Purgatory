import unittest
from pathlib import Path
from analyze_v17 import analyze

class AnalysisTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.report = analyze(Path(__file__).resolve().parents[1] / 'Verification/V17/AnalyzerFixture')

    def test_weighted_means(self):
        phase = self.report['phases']['baseline']
        self.assertEqual(phase['frame_mean_ms'], 13)
        self.assertEqual(phase['cpu_mean_ms'], 5)
        self.assertEqual(phase['gpu_mean_ms'], 4)
        self.assertEqual(phase['over_budget_percent'], 15)

    def test_missing_gpu_is_null(self):
        self.assertIsNone(self.report['phases']['two_portals']['gpu_mean_ms'])
        self.assertIsNone(self.report['phases']['two_portals']['cpu_mean_ms'])

    def test_not_a_global_percentile(self):
        self.assertEqual(self.report['phases']['baseline']['worst_window_p95_ms_NOT_global_p95'], 20)
        self.assertIsNone(self.report['phases']['baseline']['worst_window_p99_ms_NOT_global_p99'])

    def test_safety_skip_is_not_a_pass(self):
        self.assertEqual(self.report['hardware_gate'], 'NOT_AUTOMATICALLY_VERIFIED')
        self.assertTrue(any('safety skipped' in w for w in self.report['warnings']))
        self.assertTrue(any('two visible portals' in w for w in self.report['warnings']))
        self.assertTrue(any('Missing benchmark phase' in w for w in self.report['warnings']))
        self.assertTrue(any('No confirmed scripted combat hit' in w for w in self.report['warnings']))

    def test_observer_and_refresh_warnings(self):
        self.assertTrue(any('observer' in w for w in self.report['warnings']))
        self.assertTrue(any('fallback' in w for w in self.report['warnings']))

if __name__ == '__main__':
    unittest.main()

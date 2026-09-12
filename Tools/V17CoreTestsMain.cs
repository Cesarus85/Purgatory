using System;
using QuestDemonMR.Editor;

internal static class V17CoreTestsMain
{
    public static int Main(string[] args)
    {
        var count = 0;
        V17CoreChecks.Run((passed, reason) =>
        {
            if (!passed) throw new Exception(reason);
            count++; Console.WriteLine("PASS " + reason);
        }, args.Length > 0 ? args[0] : "Verification/V17/CoreChecks");
        Console.WriteLine("V17_CORE_TESTS_OK count=" + count + " (not Unity or headset tests)");
        return 0;
    }
}

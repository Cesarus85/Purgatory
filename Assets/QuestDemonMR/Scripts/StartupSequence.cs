using System;
using System.Collections;
using System.Collections.Generic;

namespace QuestDemonMR
{
    // Unity normally abandons a coroutine silently from the HUD's perspective.
    // Drive nested enumerators too, so asynchronous asset preparation is covered.
    public static class StartupSequence
    {
        public static IEnumerator Guard(IEnumerator root, Action<Exception> failed)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            try
            {
                while (stack.Count > 0)
                {
                    object current = null;
                    Exception error = null;
                    try
                    {
                        var active = stack.Peek();
                        if (!active.MoveNext()) { stack.Pop(); (active as IDisposable)?.Dispose(); continue; }
                        current = active.Current;
                    }
                    catch (Exception exception) { error = exception; }
                    if (error != null) { failed(error); yield break; }
                    if (current is IEnumerator nested) stack.Push(nested);
                    else yield return current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
        }
    }
}

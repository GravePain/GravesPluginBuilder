using UnityEngine;

namespace Graves.TestBed
{
    /// <summary>
    /// Forces <c>Application.runInBackground</c> on before the first frame of every Play session.
    /// </summary>
    /// <remarks>
    /// Without this, a Play session started by tooling rather than by a click has no real OS window
    /// focus, and the Editor's player loop silently freezes at <c>Time.frameCount == 1</c> while
    /// <c>EditorApplication.isPaused</c> still reports false. Every observation taken in that state is
    /// of the same stalled frame, so code that works reads as broken - a failure that once consumed an
    /// entire debugging budget across two sessions before anyone checked the frame counter.
    ///
    /// The Project Settings <c>Run In Background</c> option does NOT cover this: that governs built
    /// players, not the Editor's own loop.
    /// </remarks>
    public static class EditorPlayModeFix
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void KeepRunningWhenUnfocused()
        {
            Application.runInBackground = true;
        }
    }
}

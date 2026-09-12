using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace QuestDemonMR.Editor
{
    public static class LiveScanHotfixValidation
    {
        private static void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException("V18.1: " + name);
            Debug.Log("QDMR_HOTFIX_CHECK " + name);
        }

        private static IEnumerator Broken()
        { yield return null; throw new InvalidOperationException("injected startup failure"); }
        private static IEnumerator Outer(IEnumerator inner)
        { yield return inner; yield return "finished"; }

        public static void Validate()
        {
            QuestDemonProjectBuilder.ConfigurePlayer();
            var shader = QuestGun.WeaponEffectShader();
            Check(shader.name == "QuestDemonMR/WeaponUnlit", "weapon shader explicitly resolves from Resources");
            Check(!ShaderUtil.ShaderHasError(shader), "weapon shader has no compilation errors");
            var material = new Material(shader);
            try { Check(material.HasProperty("_Color"), "flash and trace color contract exists"); }
            finally { UnityEngine.Object.DestroyImmediate(material); }
            var errors = 0;
            var guarded = StartupSequence.Guard(Outer(Broken()), e => { if (e.Message == "injected startup failure") errors++; });
            Check(guarded.MoveNext() && guarded.Current == null, "nested startup preserves frame yield");
            Check(!guarded.MoveNext() && errors == 1, "nested startup failure is reported once and stops continuation");
            Check(!guarded.MoveNext() && errors == 1, "failed startup remains stopped");
            IEnumerator Successful() { yield return null; }
            guarded = StartupSequence.Guard(Outer(Successful()), _ => errors++);
            Check(guarded.MoveNext() && guarded.Current == null, "successful nested startup yields normally");
            Check(guarded.MoveNext() && Equals(guarded.Current, "finished"), "successful startup continues after nested routine");
            Check(!guarded.MoveNext() && errors == 1, "successful startup completes without error");
            FollowupV17Validation.Validate(); StartupValidation.Validate(); LiveScanValidation.Validate();
            AssetDatabase.SaveAssets();
            Debug.Log("QDMR_HOTFIX_REGRESSION_OK checks=9");
        }
        public static void ValidateAndBuild()
        {
            Validate();
            QuestDemonProjectBuilder.BuildAndroidLiveScanHotfixPrepared();
        }
    }
}

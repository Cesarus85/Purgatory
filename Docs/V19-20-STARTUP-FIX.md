# Purgatory V19.20 — Startabsturz nach V19.19

Stand: 8. September 2026. **V19.20 installiert; Nutzerstart und zwei kontrollierte Kaltstarts erfolgreich.** Alle 37 vorgeladenen Assets vorhanden, kein erneuter Startabsturz. [Abschließender Liefer- und Gerätenachweis](V19-20-DELIVERY.md).

## Gesicherter Befund

V19.19 wurde auf der verbundenen Quest mehrfach mit identischem nativen SIGTRAP reproduziert, auch während der Nutzer das Headset trug. Passende Symbole mit libunity-Build-ID `61e95ab3e3b56c70` zeigen `CachedReader::OutOfBoundsError` beim Lesen eines einzelnen Bytes in `TransferScriptingObject`, `SerializedFile::ReadObject` und `LoadSceneOperation::Perform`. Das geschieht vor der Spielinitialisierung und vor dem Laden einer gespeicherten Raumkarte. Kein belegter OOM oder ANR.

Der in V19.19 ergänzte öffentliche boolesche Diagnose-Schalter `UseCombatDirector` wurde unbeabsichtigt Teil des Unity-Szenenformats. Das wurde an der echten Main-Szene mit `SerializedObject` und Reflection bestätigt. Ein nicht zum Laufzeit-Schema passender Szenen-Build ist damit eine konkrete Ursachenhypothese; der Stack allein benennt weder das Feld noch den Cache als eindeutigen Verursacher.

Zusätzlicher Buildbefund: Der V19.19-Export verwendete `1300b0aESkipCompile.dag` / `BuildPlayerData/Editor/TypeDb-All.json` und meldete `QuestDemonGame` wiederholt mit einer bereits registrierten Definition mit **0 Feldern**, obwohl die aktuelle Editor-Definition den neuen booleschen Schalter enthielt. Diese zuvor als historische Warnungen behandelten Meldungen sind für einen Szenenformat-Absturz relevant. V19.20 erzwingt deshalb auch die Neuerstellung der Spieler-Assemblies statt nur einer erneuten Paketierung.

Im Cache lagen **101 nummerierte JSON-Kopien** neben den Originalen. Der direkte JSON-Vergleich belegt die abweichenden Klassenschemata: aktuelle Player/TypeDb-All.json enthält `UseCombatDirector`, die historische `TypeDb-All 28.json` enthält für dieselbe Klasse keine Felder. Alle nummerierten Kopien mit vorhandenem Original wurden unverändert nach `Verification/StartupCrashV1920/typedb-quarantine.mDKWHN` verschoben. Keine Quelldateien gelöscht. Der erste Korrekturexport wurde vor Abschluss beendet und Unity anschließend frisch gestartet, damit auch bereits eingelesene doppelte Definitionen verschwinden. Das Buildskript sichert solche Cache-Kopien nun vor dem Unity-Start und verweigert die Paketierung bei verbleibenden TypeDB-Doppelregistrierungen.

## Korrektur und Regressionsschutz

- `UseCombatDirector` bleibt aktiv, wird aber ausdrücklich nicht serialisiert. Kein Abschalten des neuen Kampfregisseurs und kein Eingriff in das Raumdateiformat.
- Echte Produktionsszene erneut importieren, öffnen, prüfen und speichern. Native Prüfung verlangt den Laufzeit-Schalter mit Standardwert true und ohne serialisiertes Feld.
- Unity-Spielerdaten mit `CleanBuildCache` neu erzeugen; Gradle-Paketierung in einem neuen temporären Verzeichnis. Kein Vermischen mit früheren Szenen- oder IL2CPP-Ausgaben.
- Fokussierte V19-Regressionssuite vor dem Export erneut ausführen. Anschließend APK-Metadaten/Signatur prüfen und echte Kaltstarts durchführen. Editorchecks allein sind kein Nachweis für das Laden der gepackten Android-Szene.

## Datenerhalt und Abnahme

Vier vorhandene `.qroom`-Dateien vor dem Update mit SHA-256 erfasst, siehe `Verification/StartupCrashV1920/room-profiles-before.txt`. Nach der Update-Installation mit `adb install -r` sind alle vier Hashes unverändert. Kein Deinstallieren, Löschen von App-Daten oder neuer Scan.

Installation und Startprüfung waren vom Nutzer ausdrücklich freigegeben und sind abgeschlossen. Der Nutzer bestätigt „Spiel läuft“. Beide kontrollierten Kaltstarts bleiben nach vollständiger Initialisierung mindestens 20 Sekunden fehlerfrei aktiv; Purgatory wurde danach offen gelassen. Raumwahl-/Kampf-/Langzeitabnahme von V19-D bleibt unabhängig davon offen. [Paket und Protokolle](../Verification/StartupCrashV1920/delivery.json).

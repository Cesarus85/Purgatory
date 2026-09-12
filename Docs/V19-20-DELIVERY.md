# V19.20 — installiert, Startkorrektur auf Quest bestätigt

Abschließender Stand vom **8. September 2026, 16:21 Uhr**. Dieser Nachweis ersetzt den vorläufigen Status in `V19-20-STARTUP-FIX.md` und `BUILD-REPORT.md`.

**Purgatory 0.19.20 / code59 ist als Update auf Quest 3 installiert.** Der Nutzer bestätigt „Spiel läuft“. Sein Start und zwei anschließende kontrollierte Kaltstarts erreichen die vollständige Initialisierung, jeweils mit 37 vorgeladenen Assets und keinem fehlenden Asset. Beide kontrollierten Starts bleiben danach mindestens 20 Sekunden mit unverändertem Prozess ohne Laufzeitfehler aktiv. Das Spiel bleibt nach dem letzten Start geöffnet. Der vorher reproduzierte native Startabsturz trat in diesen drei Versuchen nicht mehr auf.

Die erste Startanforderung nach dem Update war durch den Quest-Systemdialog „Controller erforderlich“ blockiert; dieser Versuch hatte noch keinen Spielprozess gestartet und zählt nicht als bestandener Start.

## Lieferumfang und Ursache

Unbeabsichtigt serialisierten Diagnose-Schalter vom Szenenformat getrennt, 101 widersprüchliche nummerierte Unity-Typdatenkopien wiederherstellbar aus dem generierten Cache ausgelagert, echte Main-Szene neu gespeichert sowie Spieler und APK vollständig neu gebaut. Keine doppelten Typregistrierungen mehr im finalen Export; das Buildskript verweigert die Paketierung, sollten sie wieder auftreten. Der Kampfregisseur bleibt eingeschaltet. Gespeicherte Raumformate und Spielinhalt wurden nicht geändert.

Der frühere native Stack und die widersprüchlichen Typdateien stützen den Befund einer inkonsistenten Startszene-/Laufzeit-Serialisierung. Die kombinierten Korrekturen bestehen jetzt den Gerätetest; sie wurden nicht einzeln als A/B-Ursachenexperiment geprüft.

## Paket und Datenerhalt

- APK: [Purgatory-v19.20-startup-fix.apk](../Builds/Purgatory-v19.20-startup-fix.apk), **134.828.830 Bytes**, ARM64, min29/target36, nicht debuggable.
- SHA-256: `749ffe6e41fc538b52e520b5b2f554dd395ff25cc73bd33b07f1639985131847`.
- ZIP und bestehende APK-v2-Entwicklungssignatur geprüft. Kein Store-Release.
- Finale 228 V19-Checks plus native Prüfung des Produktions-Szenenformats bestanden. Keine C#-/Shaderfehler, Exceptions oder TypeDB-Doppelregistrierungen im finalen Export; bekannte SDK-/Gradle-Warnungen bleiben.
- Ausschließlich `adb install -r`: kein Löschen, Deinstallieren, Berechtigungswechsel oder neuer Scan. Alle vier gespeicherten `.qroom`-Dateien wurden nach dem Update mit ihren vorherigen SHA-256-Werten verglichen und sind unverändert.
- Bisherige V19.18-/V19.19-APKs und Dämonen-FBX unverändert erhalten. Alte V19.19-APK wegen des belegten Startabsturzes nicht mehr installieren.

[Maschinenlesbarer Paket-/Gerätenachweis und Protokolle](../Verification/StartupCrashV1920/delivery.json).

**Offen bleibt die V19-D-Spielabnahme:** reale hintere Portalnischen, Schnellneustart-Bedienung, Kampfbalance sowie Langzeit-/Thermik-/Framezeitmessung. Erfolgreiche Menüstarts ersetzen diese Tests nicht.

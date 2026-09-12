# V17.2 – Inhalte vor dem Rundenstart vorbereiten

Stand: 5. September 2026. **Gebaut, signaturgeprüft, installiert und erster Quest-Messlauf vollständig abgeschlossen.** 179 native Unity-Prüfungen und fünf Auswertungstests bestanden. Kleinere Erzeugungsspitzen gemessen, allerdings nur zwei statt drei sicher platzierbare Gegner. Vorladedauer und GPU-/Compositor-Daten dieses Laufs fehlen; keine vollständige Performance-Abnahme. APK-/Installationsnachweis: [BUILD-REPORT.md](BUILD-REPORT.md).

## Ausgangspunkt

Der echte V17.1-Messlauf erreichte mit zwei Portalen und drei Gegnern rund 72 Hz im Mittel, hatte aber bei der ersten Portalerzeugung ein Maximum von 43,437 ms und beim Erzeugen von drei Gegnern 138,898 ms. Sparse-Wunden bleiben unverändert: nur 90 Vollhaut-Bakes in 15 Sekunden Kampf. Rohdaten und Grenzen: [Geräteprofil](V17-FIRST-DEVICE-PROFILE.md).

## Umsetzung

- Begrenzter Vorladeplan: fünf tatsächlich verwendete Modelle (Portalrahmen, Höllenwelt, Schwelle, Bodenmonster, Fledermaus) und vier explizit zur Laufzeit zugewiesene Haut-/Fledermaustexturen. Asynchrone ResourceRequests, jeweils mit zusätzlicher Frame-Grenze.
- Zwei Animationssammlungen werden einmal geladen und danach wiederverwendet; nicht bei jeder Gegnerinitialisierung ein neues `Resources.LoadAll`-Array.
- Prozedurale Portal-/Gegner-/Reload-Sounds werden einzeln auf getrennten Vorbereitungsframes erzeugt. Die vier vorhandenen Combat-Soundbanken werden ebenfalls vorab geladen, ohne einen Sound auszuwählen oder abzuspielen.
- Vorbereitung vor dem Startpult und vor der Diagnosekomponente, mit Fortschrittstext und weiterhin sichtbarem Passthrough. Keine laufende Runde, keine Gegner, keine Dummy-Physik, keine verbrauchte Munition oder Gameplay-Zufallswerte. Die Vorbereitung läuft auch bei `Time.timeScale=0`.
- Fertige Assets werden referenziert, nicht als unsichtbare Gegner vervielfältigt. Der Cache wird über Runden hinweg gehalten; fehlende Dateien bleiben fehlend und werden protokolliert. Bestehende Fallbacks bleiben erhalten.
- Im Benchmark werden bis zu drei Gegner nicht mehr im selben Frame konstruiert, sondern mit je einer Frame-Grenze. Der reale Wellenablauf hatte bereits einzelne, zeitlich getrennte Gegnerauftritte. Raum-/Spawn-Sicherheitsregeln bleiben identisch; kein erzwungener Spawn für eine schönere Messung.
- Kein neuer universeller Gegner-Pool: Wiederverwendung aktiver Ragdolls, Wunden und Flugzustände wäre ein gesondertes Lebenszyklus-Risiko. Erst messen, ob das noch nötig ist.
- Der neue Build-Einstieg bereitet das Projekt genau einmal vor. Nach Tests wird der bereits vorbereitete Stand gebaut, ohne die komplette zweite Import-Runde der bisherigen kombinierten Befehle.

Das verschiebt Arbeit in die sichtbare Vorbereitung und kann den Start verlängern. Ein asynchroner Ladeaufruf garantiert nicht, dass jede native Integration oder der erste GPU-Shadergebrauch ohne Spitze bleibt. Deshalb keine vollständige Beseitigung aller Ruckler aus dem Quellcode abgeleitet.

## Prüfungen

179 native Unity-Checks: die bisherigen 160 plus 19 neue Prüfungen zu Manifest/Assets, Cache-Identität, Clip-Wiederverwendung, fehlenden Dateien, pausierter Vorbereitung, Frame-Grenzen, unverändertem Zufallszustand, ausbleibenden Dummy-Gegnern/Portalen und wiederholbarem Aufruf. Log: `work/unity-v17-startup-build.log` im übergeordneten Workspace.

Die Editor-Warmup-Sequenz wird mit bereits geladenen Assets geprüft. Sie ersetzt ausdrücklich nicht die kalte ResourceRequest-Ausführung im Android-Player. Diese muss im Geräteprotokoll durch `QDMR_CONTENT_READY`, fehlende Assets, Vorbereitungsdauer und den nachfolgenden Benchmark geprüft werden.

Version `0.17.2`, versionCode weiterhin `17`. Reproduktion: `QuestDemonMR.Editor.StartupValidation.ValidateAndBuild`. Erzeugungsspitzen sind nur bei gleicher Last sinnvoll vergleichbar; die leicht gestaffelte Gegnererzeugung wird als Verfahrensänderung offengelegt. Speicher-/Vorbereitungsdauer ebenfalls erfassen, nicht nur die beste Framezahl.

## Android-Paketierung

Der erste Paketierungsversuch scheiterte nach bestandenen Spieltests an zwei nummerierten Gradle-Zwischendateien: `network_sec_config 3.xml` und `unity-classes 2.jar`. Die XML war byte-identisch zum regulären Original; die ältere JAR war nicht byte-identisch und wurde deshalb nicht als identisch ausgegeben. Beide Konfliktkopien wurden nach `Verification/GradleQuarantine/20260905-125719-c66ec1d1/` verschoben und sind wiederherstellbar. Keine Produktionsassets verändert.

Ein neuer, auf generierte Gradle-Verzeichnisse begrenzter Postprozessor verschiebt solche nummerierten Kopien nur dann, wenn das reguläre Original daneben existiert. Er läuft nach der Projekterzeugung; die bereits vorhandene Bereinigung vor dem Build konnte die diesmal aufgetauchten Konfliktkopien nicht verhindern. Der erste Aufruf des neuen Pfadschutzes war wegen eines doppelten Pfadtrenners zu streng und brach vor der Paketierung ab; korrigiert im nachfolgenden Lauf. Ursachen im Dateisynchronisationssystem werden daraus nicht als bewiesen abgeleitet.

## Erster tatsächlicher V17.2-Gerätelauf

Sitzung `v17-20260905-142615-2042f282`, gestartet am 5. September um 16:26:15 Ortszeit. Version 0.17.2, Quest 3, Vulkan, kein Development-Modus, Raum geladen. Nach **60,011 Sekunden normal abgeschlossen**. Originaldateien und reproduzierbare Auswertung: `Verification/V17/Quest-Measurement-Startup/` (`python3 Tools/analyze_v17.py Verification/V17/Quest-Measurement-Startup`).

Zwei sichtbare Portale, ein Emberfiend und eine RiftBat. Ein angeforderter weiterer Gegner wurde mit `no_safe_placement` ausgelassen. Die Raumregeln wurden nicht für den Test gelockert.

| Phase | V17.1 Maximum | V17.2 Maximum | V17.2 Frame-Mittel |
| --- | ---: | ---: | ---: |
| Baseline | 17,257 ms | 16,200 ms | 13,887 ms |
| Erstes Portal | 43,437 ms | 19,469 ms | 13,896 ms |
| Zwei Portale | 15,877 ms | 19,328 ms | 13,885 ms |
| Gegneraufbau/-bewegung | 138,898 ms | 18,219 ms | 13,887 ms |
| Kampf/Effekte | 18,281 ms | 18,843 ms | 13,886 ms |

Die Mittelwerte liegen weiterhin um 72 Bilder/s. Die erste Portalspitze ist in diesem Lauf deutlich kleiner; beim zweiten Portal und im Kampf liegt das Maximum dagegen etwas höher. Nicht alle Frame-Spitzen sind beseitigt. Der Gegnervergleich hat zusätzlich zur gestaffelten Konstruktion **geringere Last (zwei statt drei Gegner)** und andere räumliche Platzierungen; daraus keine isolierte prozentuale Optimierungswirkung ableiten.

Sparse-Wunden aktiv bei beiden Gegnern. Kampf: 91 Vollhaut-Bakes, 1.025.520 Dreieckstests, sechs Gen-0-Sammlungen. Gemessener Unity-Allocator-Höchstwert 129.723.230 Bytes gegenüber 129.790.119 Bytes im vorherigen Lauf; wegen anderer Last und fehlendem Vorbereitungsabschnitt kein Nachweis identischer Gesamtspeicherkosten. Das ist nicht der gesamte Android-Prozessspeicher.

Beim Auslesen war die Anwendung bereits beendet. Android meldet für Version 0.17.2 um 16:29:15 `EXIT_SELF`, Status 0; der Benchmark selbst endete vorher vollständig. Die Startup-Marker und zugehörigen VrApi-Zeilen waren nicht mehr im Ringpuffer. Deshalb sind Vorladedauer, `missing`-Anzahl, Startup-Speicherspitze und GPU-/Compositor-Zeiten dieses Laufs **nicht verfügbar**. Spätere VrApi-Zahlen eines anderen Prozesses wurden nicht verwendet. Der erreichte Benchmark belegt den Ablauf bis nach der Vorbereitung, nicht die vollständige Assetliste oder einen ruckelfreien Kaltstart.

## Noch offene Gates

Nächster Schritt: Kaltstart mit bereits laufender Log-Aufzeichnung und erneuter Test in einer Position/einem Raum mit drei sicher platzierbaren Gegnern. Danach gezielte Pause-/Fokus-/Abbruchprüfungen, weiterer Raum und mindestens 20 Minuten Laufzeit. V17 insgesamt bleibt bis zu diesen Prüfungen nicht vollständig abgenommen.

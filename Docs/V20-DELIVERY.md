# Purgatory V20.0 — gebaut und lokal geprüft, nicht installiert

Stand: 8. September 2026. Basis ist die auf Quest bestätigte Startkorrektur V19.20. Die Quest war für diesen Auftrag aus und wurde weder angesprochen noch verändert.

Nachtrag aus dem anschließenden Nutzertest: Schnellneustart, Katana-Schaden, Feuerball-Parade und Revolver-Rückkehr bestätigt. Der Griff saß jedoch an der Klinge; Schnittspuren wurden nicht wahrgenommen, mehrere Gegner kreisten. Die damalige Socket-Distanzprüfung erfasste den Fehler zwischen Mesh- und Socket-Koordinaten nicht. Siehe [V20.1-Korrekturen und gezielte Abnahme](V20.1-CORRECTIONS.md). Dieser Bericht beschreibt unverändert das historische V20.0-Paket, nicht den aktuellen Korrekturstand.

## Paket

- [Purgatory-v20.0-katana.apk](../Builds/Purgatory-v20.0-katana.apk), **141.411.664 Bytes**, Version **0.20.0 / code60**.
- Paket `de.stefanmaier.questdemonmr`, ARM64/IL2CPP, min29/target36, nicht debuggable. Bestehende APK-v2-Entwicklungssignatur bestätigt; kein Store-Release.
- SHA-256: `87146d3c6f8a586aed0b929e9c04dc82dfd607c34cb589dda6b4fb26b53cdaf7`.
- Neue `libil2cpp.so`: `e8b76da2257195761c775f127843bc73156a3e5460fa1a47535664593a301d60`. Katana-/Perk-/Kettenbüßer-Typen in der tatsächlich paketierten Laufzeitmetadatei nachgewiesen.
- ZIP-Prüfung, Android-Metadaten, Signatur, eingefrorener Quellstand sowie SHA-256-Erhalt der V19.20-APK, des ursprünglichen Dämonen-FBX und des Katana-Originals bestanden. Als Update installieren; nicht vorher deinstallieren.

## Implementiert

- Gemeinsame Perk-Verwaltung mit Shotgun-Vorrang, kontrolliertem Waffenwechsel und höchstens einem vorgemerkten Katana-Rest. Revolvermunition und freie Wurfsternhand bleiben erhalten. Pause, Tracking-Erholung, Handwechsel, Tod und Schnellneustart berücksichtigt.
- Katana aus dem Nutzer-Download tatsächlich in Blender überarbeitet: unbenötigte Scheide entfernt, 19.000 Dreiecke, Griffursprung und Vorwärtsrichtung korrigiert, 70,39 cm Klinge, ein Material mit neu gebackenen 2K-Maps statt schwarzer Eingangstexturen. Änderbare Projektkopie und reproduzierbare Skripte erhalten.
- Freiwilliges Katana-Angebot in passender Notlage; acht schadenswirksame Hiebe oder 25 aktive Sekunden. Kleine Schnittbewegungen, kontinuierliche echte Hautkontakte, Wand-/Portalverdeckung, maximal zwei Ziele je Hieb, Feuerball-Parade ohne Belohnungs-Farming. Eigene Sounds, Haptik, begrenzter Schweif und Ankunft/Abgang. Diagnoseknopf am pausierten Schrein.
- Nutzer-Nachtrag umgesetzt: gerichtete dunkle Schnittspuren direkt auf der deformierenden Haut, bis zu acht Sekunden bzw. bis zum Verschwinden des Körpers. Mitbewegung bei Angriff und Tod geprüft. Metallkerben/Funken auf Eisenplatten, bestehende begrenzte Blutspritzer/-ablagerungen wiederverwendet. Keine Körperzerteilung.
- Zusätzliche gehörnte Dämonen- und konturveränderte Fledermausvariante mit unabhängigem Variantenwechsel je Familie. Neuer Kettenbüßer-Prototyp: gebückter gepanzerter Nahkämpfer mit geschichtetem verrostetem Eisen, Fesseln, eigenem Klang und erreichbarer Brust-Schwachstelle im angekündigten Angriff. Er ergänzt ab Welle vier passende schwere Begegnungen und verdrängt den CinderBrute nicht vollständig.
- Vorhandene gute Assets und Portaltechnik beibehalten. Native Vergleichsserie für das Ensemble erzeugt und gesichtet; keine unbelegte pauschale Erhöhung der Portalauflösung.

## Prüfung und reproduzierbarer Build

Finaler Unity-Lauf: **577 V20-Checks**, **228 V19-Checks**, **138 Shotgun-Checks**, **30 Handrollen-Checks**, **14 Blutchecks** und 1.200 Dreiecksindex-Vergleichsstrahlen. Enthalten sind synthetische 30/72/90-Hz-Gesten, Wandsperre, echter Klingenkontakt, animierte Schnittbindung, sichtbarer Vorher-/Nachher-Render, Tod-Pose, einmalige Parade, Portalverdeckung und Perk-Lebenszyklus. Diese Zahlen enthalten parameterisierte Prüfpunkte, nicht 577 unterschiedliche getragene Spielszenarien.

Produktionsszene mit V19.20-Serialisierungsschutz neu gespeichert und mit `CleanBuildCache` exportiert. Keine C#-/Shaderfehler, Exceptions oder TypeDB-Doppelregistrierungen im finalen Export. Bekannte SDK-/Editor-Testaufbau-/Gradle-Warnungen bleiben. Eine neu entstandene nummerierte generierte TypeDB-Kopie vor dem Endexport wiederherstellbar ausgelagert; keine Originaldateien gelöscht. iCloud-Auslagerung verlangsamte lokale Cache-Lesevorgänge; keine globalen iCloud-Einstellungen geändert.

`Tools/build-v20.0.sh` → frischer Export `/private/tmp/qdmr-v200-export.HZT3tF` → frische Paketierung `/private/tmp/qdmr-v200-package.lWBDOs`. Android-Build erfolgreich in 10m06s, 117 ausgeführte Tasks. `Tools/verify-v20.py` verifiziert das fertige Paket unabhängig. Das Buildskript überschreibt eine bereits vorhandene V20-APK nicht.

Nach dem Export wurde ausschließlich der Editor-Helfer `V20EnsembleCapture` ergänzt; er ändert weder Produktionsassets noch den exportierten Spieler. Vergleichsbilder unter `Verification/V20/ensemble-*.png`, erfolgreicher Lauf `ensemble-3.log`. [Maschinenlesbarer Liefernachweis](../Verification/V20/delivery.json).

## Offene Abnahme

**Nicht installiert und nicht auf dem getragenen Headset getestet.** V19-D sowie V20-A/D bleiben physisch offen: Start, Raumsitzung/Schnellneustart, echte Controllerbewegungen links/rechts, Griff-/Reichweitengefühl, Paraden, Kampfbalance, neue Gegner in realen Engstellen, Stereo-/Portalwirkung, kombinierte Effektlast und Langzeit-/Thermik-/Framezeiten.

Die nächste Aktion ist der [kombinierte V19-/V20-Test](V19-V20-TESTPLAN.md), nicht automatisch der Beginn von V21. Nutzerquelle und bestehende Fremdasset-Provenienz siehe [ASSET-GENERATION.md](ASSET-GENERATION.md); keine offene Weiterveröffentlichungslizenz für das Nutzer-Katana behauptet.

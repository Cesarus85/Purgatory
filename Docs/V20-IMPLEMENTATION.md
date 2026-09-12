# V20 — Qualitätsabschluss, Gnaden-Katana und Bestiarium I

Begonnen am 8. September 2026 auf Basis von **V19.20 / code59**. Aktueller Nutzerauftrag: V20 jetzt implementieren, Katana aus Downloads prüfen/anpassen, APK ohne Installation liefern und eine kombinierte V19-/V20-Testfolge erstellen. Die Quest ist aus. V19-D und die physischen V20-Gates werden auf ausdrücklichen Wunsch gemeinsam nach der Umsetzung geprüft, nicht als bestanden vorausgesetzt.

## Verbindlicher Umfang

1. **A — Ensemble und Importqualität:** vorhandene gelungene Assets erhalten, native vergleichbare Ansichten und Material-/Skalierungs-/Texturbudgetprüfungen. Keine spekulative Änderung der Portaltechnik oder pauschale höhere Renderauflösung ohne Quest-Messung.
2. **B — Perk-Verwaltung:** eine Führungshand, kontrollierte Ankunft/Abgang, Shotgun-Priorität, höchstens ein vorgemerktes Katana, kein Munitions-/Sternverlust. Bestehende Fünf-Schuss-/Pump-/Himmel-/Rauchmechanik erhalten. Pause, Tracking, Handwechsel, Tod und Schnellneustart explizit behandeln.
3. **C — Katana:** Nutzer-Modell in Blender korrigieren/optimieren; handnaher Griff, positive Skalierung für beide Hände und etwa 70 cm Klinge. Sicher angebotene Notlagenhilfe, freiwillige Fernaufnahme, acht erfolgreiche Hiebe bzw. 25 aktive Sekunden, begrenzte Hilfe je Welle und 20 Sekunden Abstand. Kurze natürliche Schnitte, kontinuierliche Klingenerfassung, exakte animierte Trefferoberfläche, Vorrang realer Hindernisse, maximal zwei Gegner pro Hieb, kontrollierte Feuerball-Parade. Eigene Klang-/Haptik-/Schnitt-/Ankunft-/Abgangseffekte und Testaktivierung.
4. **E — Bestiarium I:** erkennbare geometrische/Bewegungs-/Klangvarianten der vorhandenen Dämonen- und Fledermausfamilien; Kettenbüßer mit eigenständiger gepanzerter Silhouette und angekündigtem, frontal konterbarem Schutz-/Schwachstellenwechsel. Vorhandene Archetypen, Raumhüllen, Portale und Crowd-Limit erhalten; höchstens ein neuer Spezialist je Gruppe. Höllenhund, Aschenrufer und Boss bleiben V21.
5. **D — Integration/Lieferung:** native Unity-Regressionsprüfungen inklusive 30/72/90-Hz-Schlagfälle, Wechsel-/Lebenszyklusmatrix, Kontakt-/Blockadefälle und Bestiarium. Produktionsszene und Typdaten separat prüfen. APK bauen und Integrität/Signatur/Version prüfen; nicht installieren. Kombinierte Testliste einschließlich der noch offenen V19-D-Hardwareabnahme.

## Katana-Eingangsbefund

Quelle: `/Users/stefanmaier/Downloads/Katana_VR_Asset`, vom Nutzer für dieses Spiel bereitgestellt. Originale nur lesen. README nennt 46.336 Dreiecke für das Schwert plus 18.608 für die unbenötigte Scheide und +X zur Spitze. Die Aussage über unproblematische Quest-Kosten ist keine Geräteabnahme. Alle gelieferten Texturdateien sind gleich groß; BaseColor und GLB-Kontrollbild zeigen ein schwarzes Material. Original-Blender-Szene/prozedurale Materialien prüfen, in einer Projektkopie neu aufbereiten, keine schwarzen Maps ungeprüft übernehmen. Keine externe Herkunft oder offene Weiterveröffentlichungslizenz behaupten; Nutzerfreigabe gilt für die Integration, nicht automatisch für eine öffentliche Asset-Weitergabe.

## Leitplanken und Nachweise

- Raumprofile, Einstellungen, vorhandene APKs und Originalmodelle erhalten; keine Quest-Aktionen in diesem Auftrag.
- Kein kameragesteuerter Auto-Nahkampf, kein Schaden durch Wände/Portalverdeckung, kein Sieg durch Zittern/Halten/Tracking-Sprünge. Virtuelle Hinderniswarnung ist kein physischer Schutz; große Schwünge oder Ausfallschritte sind nicht erforderlich.
- Bestehende gute Grafiken nicht pauschal ersetzen. Keine kostenpflichtigen Assets kaufen. Neue hochwertige Geometrie/UV-/Materialdaten mit Blender erzeugen und nativ prüfen.
- V19.20-Typdaten-Schutz übernehmen; keine neue unbeabsichtigte öffentliche MonoBehaviour-Serialisierung. Bereinigte Typdaten und echter Produktions-Szenenexport, keine Vermischung alter Szenen mit neuem Laufzeitcode.
- Fehlende getragene Stereo-/Griff-/Raum-/Balance-/Langzeit-/Thermikmessung ausdrücklich offen ausweisen. Editorbilder und APK-Prüfungen sind kein Headset-Nachweis.

## Fortschritt

- Katana in Blender neu gebacken/optimiert, neue riggebundene Varianten exportiert; gemeinsame Perk-Steuerung, Schnitt-/Pariermechanik, Testangebot, Audio/Haptik und Bestiarium im Code verbunden.
- Nachtrag des Nutzers: sichtbare, mitanimierte Schnittspuren an Dämonen/Fledermäusen und metallische Rüstungskerben ergänzt, mit begrenzter Lebensdauer/Anzahl und vorhandenen Blutbudgets.
- Native Unity-Prüfung erfolgreich: 577 V20-Checks, 228 V19-Regressionschecks, 138 Shotgun- und 30 Handrollenchecks, 14 Blutchecks sowie 1.200 Dreiecksindex-Vergleichsstrahlen. Schnittkontakt, Wandblockade, animierte Hautbindung, Tod-Pose, einmalige Parade ohne Belohnungs-Farming und verdeckte Portalhaut enthalten. Zusätzlich gerenderter Vorher-/Nachher-Nachweis der Schnittspur, unabhängiger Variantenwechsel je Familie, Metalltreffer auch bei offener Brust und Erhalt der bisherigen schweren Rolle. Keine Behauptung echter Controller-Abtastung durch synthetische 30/72/90-Hz-Fälle.
- APK-Export und unabhängige Paketprüfung erfolgreich: **0.20.0/code60**, 141.411.664 Bytes, bestehende Signatur und neue Laufzeittypen bestätigt. [Abschließender Liefernachweis](V20-DELIVERY.md). **Nicht installiert, noch keine Geräteabnahme**; Quest blieb aus. Gemeinsame Stereo-/Ensemble-/Leistungsabnahme aus V20-A/D bleibt Teil der kombinierten Quest-Testfolge.
- Ergänzende native Vergleichsserie `Verification/V20/ensemble-*.png` aus `V20EnsembleCapture`: Revolver, Shotgun, Schrein mit Testknopf, Lebens-/Munitionsrelikt, Schmiede, Brücke, Kathedrale und Deckenschacht. Gleiche Studio-Lichtwerte und bildfüllende Modellaufnahmen; keine Behauptung gleicher realer Sichtdistanz oder einer Headset-Stereoabnahme. Der nach dem Export ergänzte Helfer ist ausschließlich Editorcode und verändert keine Produktionsassets. Frühere Aufnahmeversuche enthielten leere Partikelgrenzen bzw. einen noch nicht platzierten Schrein und wurden im Testaufbau korrigiert.
- Kombinierte Abnahmefolge: [V19-V20-TESTPLAN.md](V19-V20-TESTPLAN.md).

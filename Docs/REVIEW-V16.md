# Quest Demon MR – V16: Controllerpose, Flug, Fall, Portal und Audio

Stand: 5. September 2026. Folgeänderungen zum erfolgreichen V15-Spieltest.

## Waffenpose

Der Controller-Offset ist jetzt `(0, +0.008, -0.023)` Meter statt `(0, -0.012, +0.012)`: 3,5 cm zurück, 2 cm höher. Gespeicherte seitliche und Vor-/Zurück-Kalibrierungsdeltas bleiben erhalten. Die gleiche Basis gilt auch beim direkten XR-Gerätepose-Fallback. Waffenmodell, Orientierung und echter Mündungssocket aus V15 bleiben unverändert.

## Fledermaus: unbekannt ist nicht blockiert

Die bisherige `EnvironmentRaycastManager.CheckBox`-Abfrage ist für Flugnavigation ungeeignet: Metas eingebundener Depth-Provider liefert bei allen Statuswerten außer `NoHit` eine Blockade, also auch bei verdecktem Strahlursprung oder nicht auswertbaren Sichtbereichen. Die boolesche `Raycast`-API bestätigt dagegen ausdrücklich nur `EnvironmentRaycastHitStatus.Hit`.

V16 prüft fünf kurze Strahlen um den Körper gegen bestätigte Live-Oberflächentreffer. Raumgrenzen, echte Szenenflächen und Möbelvolumen bleiben Hindernisse; großflächige semantische/global-mesh Volumen dienen nicht als pauschale Flugblockade. Kurze Ausweichschritte ergänzen die längeren Richtungsproben in engen Ecken. Unbekannte Live-Tiefe wird nicht als unsichtbare Wand behandelt. Das ist keine Behauptung, dass nicht beobachtete Bereiche garantiert frei sind oder dass die Quest alle Raumdetails ständig rundum erfasst.

Nachweis im verwendeten SDK: `EnvironmentDepthManagerRaycastExtensions.CheckBox` sowie `EnvironmentRaycastManager.Raycast`, Paket `com.meta.xr.mrutilitykit@2979546e7179`.

## Fledermaus-Tod

- Keine feste 1,95-s-Fallphase mit anschließendem Schweben; keine unabhängige Zerstörung nach 2,25 s.
- Schwerkraft mit kontinuierlicher Kollisionsprüfung zwischen altem und neuem Ort, auch bei größeren Zeitschritten.
- Nächstliegende Oberfläche aus Physikkollidern, MRUK-Szene und bestätigter Live-Tiefe. Andere Dämonenkollider werden ignoriert.
- Seitlicher Kontakt lässt den Körper an der Fläche heruntergleiten; eine tragende Fläche beendet den Fall. Ohne verwertbare Detailtiefe bleibt die bekannte Raum-Bodenhöhe als Fallback.
- Erst nach Landung: kurzer Aufprall-/Gluteffekt, 0,12 s Liegezeit, 0,22 s Verschwinden. Logmarker: `QDMR_V16_BAT_LANDED`.

## Portal: richtige Abbildung und richtige Geometrie

Das frühere Stereo-Verfahren renderte zweimal 672 × 672 Pixel für den kompletten Headset-Blickwinkel und zeigte im Portal nur einen kleinen Ausschnitt davon. Das verschenkte erheblich Schärfe.

V16 rendert pro Auge 1024 × 1240 Pixel in ein exakt an die Öffnung angepasstes asymmetrisches Sichtvolumen. Die gesamte Textur gehört dem Portal; keine Vollbild-UV-Abtastung mehr. Der Augenabstand kommt im XR-Betrieb aus den tatsächlichen Stereo-View-Matrizen. Kopftranslation verändert den Einblick; Kopfdrehung rotiert nicht die entfernte Welt. Die Near-Plane schneidet an der Portalöffnung, Kameras rendern vor der Hauptkamera, Aktualisierung zusätzlich unmittelbar vor dem Rendern. Nur sichtbare, von vorn betrachtete Portale rendern. Die Bildmitte wird nicht mehr verzerrt oder eingefärbt; schwache Verzerrung bleibt am Rand.

Zusätzlich tatsächlich in Blender bearbeitet:

1. `PortalThresholdV16.blend`: beidseitige Ketten mit einzelnen Gliedern, geschmiedete Pfosten und fünf unregelmäßig gebrochene Steinplatten mit eingelassenen Glutzeichen. Das ist echte Nahgeometrie direkt hinter der Öffnung, keine Bildkarte. Export in drei Materialgruppen, 6804 Blender-Polygone.
2. `InfernalWorldV16.blend`: Flächenausrichtung von 5760 Gelände- und 730 Lava-/Wasserfallpolygonen repariert. Sie waren zuvor nach unten beziehungsweise von der Blickrichtung weg orientiert und wurden durch Backface-Culling unsichtbar. Der schwarze Bereich unter der Burg war damit teilweise ein Modellfehler. Die Landschaft bleibt in sieben Materialgruppen zusammengefasst.

Reproduktion: `BlenderSource/build_portal_threshold_v16.py` und `BlenderSource/repair_world_v16.py`. V15/V10-Originale bleiben erhalten.

### Was Meta dazu liefert

Meta bietet Passthrough-Layer, selektive Flächen, Alpha-Komposition und Tiefenverdeckung. Diese Grundlagen sind nicht gleichbedeutend mit einer fertigen dreidimensionalen Höllenwelt. Unsere virtuelle Welt und ihre perspektivisch korrekte Stereo-Abbildung müssen zusätzlich gerendert werden. Die vorhandene Passthrough-Underlay-Architektur bleibt bestehen.

Primärquellen: [Meta: Passthrough AR und Alpha-Komposition](https://developers.meta.com/horizon/documentation/unity/unity-customize-passthrough-passthrough-ar/), [Meta: Passthrough Starter Sample](https://developers.meta.com/horizon/documentation/unity/unity-sample-starter-passthrough/).

## Audio

Zwanzig offline gemischte Mono-PCM-Sounds aus Kenneys CC0-Paketen: fünf Schüsse, fünf Körpertreffer, fünf harte Einschläge, fünf tödliche Treffer. Der Schuss verbindet Druck/Transient, rauen Ausklang, zurückhaltenden Energieanteil und mechanischen Rücklauf. Kein bloßer Austausch einer Sinusfrequenz.

Die Soundbank vermeidet direkte Wiederholungen. Einschläge erklingen räumlich am Trefferort aus einem Pool von acht Stimmen und übernehmen nicht die hohe Stimmlage der Fledermaus. Kleine Tonhöhenvariationen verwenden eine eigene Zufallsquelle, ohne den Gameplay-Zufallszustand zu verändern. PCM wird vorgeladen, nicht beim Schuss zusammengesetzt. Spitzenpegel der Quelldateien 0,87, keine digitalen Übersteuerungen in den Einzelclips. Die hörbare Gesamtmischung über Quest-Lautsprecher bleibt ein Hardwaretest.

Quellen: [Kenney Sci-Fi Sounds](https://kenney.nl/assets/sci-fi-sounds), [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds). Lizenztexte und reproduzierbarer Mix liegen im Projekt.

## Prüfung und offene Headset-Gates

Unity-Einstieg: `QuestDemonMR.Editor.FollowupV16Validation.ValidateAndBuild`. Er führt die 28 V15-Assertions und 39 zusätzliche V16-Assertions aus: Sofalandung vor Boden, kein Zwischenluft-Kontakt, Wandkontakt, Fall über das frühere Zeitlimit hinaus, Off-Axis-Eckpunkte, Augenparallaxe, Kopftranslation, Waffenoffset, Import der beiden Portalmodelle sowie Varianten und PCM-Pegel aller Sounds.

GPU-Sichtprüfung: `QuestDemonMR.Editor.VisualReviewCapture.Capture`, zehn Bilder in `Previews/V16/`: Gegner/Waffe, vier Portale, zwei Einzelaugenbilder und ein näherer seitlicher Einblick. Metal-Editorbilder sind keine Quest-Passthrough-Aufnahmen und kein Nachweis der empfundenen Stereotiefe.

Am getragenen Headset prüfen:

1. Waffenposition inklusive vorhandener Kalibrierung; Lauf/Controllerdeckung aus mehreren Handhaltungen.
2. Fledermäuse um Sofa/Ecke fliegen lassen, zwischendurch wegschauen und wieder hinsehen. Abschüsse in geringer und großer Höhe, über Sofa und freiem Boden; kein Schweben nach dem Tod.
3. Wand- und Deckenportal mit beiden Augen; langsam seitlich lehnen und näher treten, ohne reale Wände/Möbel zu übersehen. Ketten/Steine müssen sich gegenüber Burg und Lava nachvollziehbar verschieben. Keine vertikale Spiegelung, keine Stereo-Doppelbilder.
4. Mehrere schnelle Schüsse und Treffer über Headset-Lautsprecher/Kopfhörer. Verhältnis Schuss/Einschlag/Stimmen nach Gehör beurteilen.
5. Mindestens 15 Minuten mit zwei gleichzeitig sichtbaren Portalen: GPU-Frametime, Speicher und Thermik. Die höhere Portalauflösung ist bewusst eine Qualitätsänderung, keine ungemessene Garantie stabiler Bildrate.

Build-/Installationsnachweise stehen im aktuellen `BUILD-REPORT.md`.

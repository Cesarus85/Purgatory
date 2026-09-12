# Purgatory — Roadmap

Stand: 12. September 2026. Dieses Dokument ist der aktuelle Einstiegspunkt; die ausführlichen Liefer- und Entwurfsnotizen liegen unter `Docs/`.

## Aktueller Stand: V20.6

Purgatory ist ein raumgroßer Mixed-Reality-Shooter für Meta Quest 3. Der Spieler verteidigt sich im real erfassten Raum gegen Bodendämonen und Fledermäuse aus stereoskopischen Wand- und Deckenportalen. Der Spielstand enthält unter anderem:

- persistente Raumprofile, Live-Rekonstruktion und Schreinplatzierung;
- Revolver, Wurfsterne, situationsbedingte Shotgun und Katana;
- Feuerbälle, optionale Siegelportale, Relikte sowie Schnellneustart;
- animierte Gegner, 3D-Portalorte, räumlichen Ton, Blut-/Treffer- und Umgebungsreaktionen;
- Links-/Rechtshand-Auswahl und die bisherige Komfort-/Sicherheitslogik.

V20.6 ist lokal gebaut und verifiziert, aber noch nicht im Headset abgenommen. Leichte Katana-Kontakte verursachen nur kleine Wunden und wenig Schaden; bewusste Hiebe und Stiche bleiben wirksam. Bodenportale können näher erscheinen, der Gegner darf aber nur bei freiem, körperbreitem Ausstieg und sicherer Spieler-/Möbelprüfung austreten.

## Unmittelbar als Nächstes: V20.6 Headset-Abnahme

1. Leichten Katanakontakt gegen bewussten Hieb/Stich vergleichen; stillgehaltene Klinge und bloßes Anlaufen dürfen keinen Schaden auslösen.
2. Gegenhiebe, kleine Wunden und die acht Katana-Trefferbewegungen im normalen Kampf prüfen.
3. Nahe Wandportale testen: mittiger Austritt, möglicher kurzer Seitenschritt, Spielerblockade und anschließendes Freimachen.
4. In mindestens zwei unterschiedlich eingerichteten Räumen auf Durchdringen, Blockaden, Bildrate, Portalverteilung und Komfort achten.

Erst nach dieser Abnahme werden Grenzwerte oder der nächste große Block verändert.

## Nächste große Blöcke

| Stufe | Ziel | Ergebnis |
| --- | --- | --- |
| V21-A | MR-Inszenierung | Oberflächengebundene Risse, Portal-Schließreaktion, räumlich geschichtetes Ambiente und sichere Rauminteraktion. |
| V21-B | Torwächter | Ein Boss, der überwiegend hinter einem Portal bleibt und dadurch Größe ohne riskante Wohnraum-Navigation vermittelt. |
| V21-C | Bestiarium II | Höllenhund und Aschenrufer als klar unterschiedliche, raumsicher getestete Gegnerrollen. |
| V22 | Die letzte Prüfung | Fünf Akte bzw. etwa 20–25 Wellen, Urteils-/Endsequenz, Unterbrechungsspeicher, Upgrade-Entscheidungen und Balance. |
| V23 | Spielbare Alpha | Onboarding, Komfort- und Grafikregler, robuste Kaltstarts/Resume, Lizenzen/Credits und reproduzierbare Testpakete. |
| V24 | Koop-Prototyp | Getrennte Machbarkeitsstufe für insgesamt bis zu vier Spieler: gemeinsame Raumkoordinaten, Ankersynchronisierung, Netzwerkzustand und Sicherheit. |

## Leitplanken

- Raumgeometrie, realer Boden und Möbel bleiben sicherheitsrelevant: Unbekannte oder blockierte Bereiche werden nicht als Lauf- oder Spawnfläche freigegeben.
- Keine Aussage über Qualität, Leistung oder Komfort ersetzt den getragenen Quest-Test.
- Standalone Quest 3 bleibt das Zielsystem. Keine Engine- oder Render-Pipeline-Migration ohne messbaren Nutzen.
- Externe Modelle und Sounds nur mit dokumentierter Herkunft, Lizenz, Quest-Import- und Performance-Prüfung.
- Multiplayer wird nicht beiläufig in die lokale Kampfsteuerung eingebaut; er folgt erst einem eigenständigen Raum-/Netzwerkprototyp.

## Wichtige Dokumente

- [V20.6 Implementierung und Abnahme](Docs/V20.6-CONTACT-AND-CLOSE-PORTALS.md)
- [Build- und Prüfbericht](Docs/BUILD-REPORT.md)
- [Ausführliches Implementierungskonzept V19–V23](Docs/IMPLEMENTIERUNGSKONZEPT-V19-V23.md)
- [V19–V20 Testplan](Docs/V19-V20-TESTPLAN.md)
